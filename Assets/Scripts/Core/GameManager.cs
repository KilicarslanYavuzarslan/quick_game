using UnityEngine;

/// <summary>
/// Central game controller. Owns game state, tracks star collection progress
/// per constellation, handles all click resolution, and detects win/lose.
///
/// Calls out to: CompassController, HintSystem, AudioManager, UIManager,
/// ConstellationCompletePanel, and GridManager (for AreaReveal ability).
/// ScoreManager wiring is reserved for Step 9.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Game State ────────────────────────────────────────────────────────────

    public enum GameState { WaitingToStart, Playing, Won, Lost }
    public GameState State { get; private set; } = GameState.WaitingToStart;

    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Data ──────────────────────────────────────────────────────────────────

    [Header("Data")]
    [SerializeField] private LevelData levelData;

    // ── Mistake Tracking ──────────────────────────────────────────────────────

    private const int MISTAKE_LIMIT = 2;
    public int MistakeCount { get; private set; }

    // ── Constellation Tracking ────────────────────────────────────────────────

    private int[] nextStarIndex;
    private int activeConstellationIndex = 0;

    /// <summary>The constellation the player is currently hunting.</summary>
    public ConstellationData ActiveConstellation =>
        levelData.constellations[activeConstellationIndex];

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        InitGame();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Initialisation
    // ─────────────────────────────────────────────────────────────────────────

    private void InitGame()
    {
        if (levelData == null)
        {
            Debug.LogError("[GameManager] No LevelData assigned!");
            return;
        }

        nextStarIndex = new int[levelData.constellations.Length];
        MistakeCount  = 0;
        activeConstellationIndex = 0;
        State = GameState.Playing;

        Debug.Log($"[GameManager] InitGame complete | State={State} | " +
                  $"Total stars: {levelData.TotalStarCount} | " +
                  $"Active: {ActiveConstellation.constellationName}");

        CompassController.Instance?.UpdateTarget();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Click Resolution (called by Cell.OnPointerClick)
    // ─────────────────────────────────────────────────────────────────────────

    public void OnCellClicked(Cell cell)
    {
        if (State != GameState.Playing) return;

        // ── Empty cell (or revealed-but-not-star) ─────────────────────────────
        if (!cell.isStar)
        {
            cell.SetState(Cell.CellState.Revealed);
            HintSystem.Instance?.RefreshCell(cell);
            return;
        }

        // ── Star cell ─────────────────────────────────────────────────────────
        int constellationIndex = GetConstellationIndex(cell.constellation);
        bool isActive  = constellationIndex == activeConstellationIndex;
        bool isCorrect = cell.starIndex == nextStarIndex[constellationIndex];

        if (isActive && isCorrect)
        {
            HandleCorrectStar(cell, constellationIndex);
        }
        else
        {
            HandleWrongStar(cell, constellationIndex, isCorrect);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Correct Star
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleCorrectStar(Cell cell, int constellationIndex)
    {
        cell.SetState(Cell.CellState.Collected);
        nextStarIndex[constellationIndex]++;
        AdvancePastFailedStars(constellationIndex);

        Debug.Log($"[GameManager] ✓ Correct! {cell.constellation.constellationName} " +
                  $"star {cell.starIndex + 1} collected.");

        AudioManager.Instance?.PlayCorrect();
        HintSystem.Instance?.RefreshAllHints();
        CompassController.Instance?.UpdateTarget();

        // ScoreManager.Instance?.AddScore(100);   // Step 9

        CheckConstellationComplete(constellationIndex, cell.gridPosition);
        CheckWinCondition();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Wrong Star
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleWrongStar(Cell cell, int constellationIndex, bool wasCurrentTarget)
    {
        cell.SetState(Cell.CellState.Failed);
        MistakeCount++;

        Debug.Log($"[GameManager] ✗ Wrong! {cell.constellation.constellationName} " +
                  $"star {cell.starIndex + 1} | Mistakes: {MistakeCount}/{MISTAKE_LIMIT + 1}");

        if (wasCurrentTarget)
        {
            nextStarIndex[constellationIndex]++;
            AdvancePastFailedStars(constellationIndex);
            CompassController.Instance?.UpdateTarget();
        }

        AudioManager.Instance?.PlayWrong();
        CompassController.Instance?.PlayErrorFeedback();

        // ScoreManager.Instance?.RegisterMistake();   // Step 9

        if (MistakeCount > MISTAKE_LIMIT)
        {
            State = GameState.Lost;
            Debug.Log("[GameManager] Game Over — too many mistakes!");
            UIManager.Instance?.ShowLosePanel(MistakeCount);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Constellation Complete
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called after a correct star is collected. Checks if this constellation
    /// is now fully complete and fires its special ability and completion video.
    /// lastCollectedPos is the grid position of the star just collected (needed for AreaReveal).
    /// </summary>
    private void CheckConstellationComplete(int constellationIndex, Vector2Int lastCollectedPos)
    {
        var constellation = levelData.constellations[constellationIndex];

        // Not done yet
        if (nextStarIndex[constellationIndex] < constellation.StarCount) return;

        Debug.Log($"[GameManager] ⭐ Constellation complete: {constellation.constellationName}");

        AudioManager.Instance?.PlayConstellationComplete();

        // ── Fire special ability ──────────────────────────────────────────────
        if (constellation.abilityType == ConstellationAbilityType.AreaReveal)
        {
            GridManager.Instance?.RevealArea(lastCollectedPos, constellation.abilityRadius);
            HintSystem.Instance?.RefreshAllHints();
            Debug.Log($"[GameManager] AreaReveal triggered at {lastCollectedPos} radius {constellation.abilityRadius}");
        }
        // ReverseCompass is passive (handled by CompassController each frame) — no action needed here

        // ── Play completion video (if assigned) ───────────────────────────────
        ConstellationCompletePanel.Instance?.Play(constellation);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Constellation Switching (called by ConstellationSelector)
    // ─────────────────────────────────────────────────────────────────────────

    public void SetActiveConstellation(int index)
    {
        if (State != GameState.Playing) return;
        if (index < 0 || index >= levelData.constellations.Length) return;

        activeConstellationIndex = index;

        Debug.Log($"[GameManager] Active constellation → {ActiveConstellation.constellationName}");

        CompassController.Instance?.UpdateTarget();
        HintSystem.Instance?.RefreshAllHints();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Win Condition
    // ─────────────────────────────────────────────────────────────────────────

    private void CheckWinCondition()
    {
        for (int i = 0; i < levelData.constellations.Length; i++)
        {
            if (nextStarIndex[i] < levelData.constellations[i].StarCount)
                return;
        }

        State = GameState.Won;
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        bool perfect   = MistakeCount == 0;
        Debug.Log($"[GameManager] You Win! Score={finalScore} Perfect={perfect}");

        AudioManager.Instance?.PlayWin();
        UIManager.Instance?.ShowWinPanel(finalScore, perfect);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void AdvancePastFailedStars(int constellationIndex)
    {
        var constellation = levelData.constellations[constellationIndex];

        while (nextStarIndex[constellationIndex] < constellation.StarCount)
        {
            var pos  = constellation.starPositions[nextStarIndex[constellationIndex]];
            var cell = GridManager.Instance?.GetCell(pos);

            if (cell != null && cell.state == Cell.CellState.Failed)
                nextStarIndex[constellationIndex]++;
            else
                break;
        }
    }

    /// <summary>
    /// Returns the grid position of the current target star for the active constellation.
    /// Returns (-1,-1) if all stars in the active constellation are done.
    /// </summary>
    public Vector2Int GetCurrentTargetPosition()
    {
        if (nextStarIndex == null) return new Vector2Int(-1, -1);

        int idx = nextStarIndex[activeConstellationIndex];
        if (idx >= ActiveConstellation.StarCount)
            return new Vector2Int(-1, -1);

        return ActiveConstellation.starPositions[idx];
    }

    private int GetConstellationIndex(ConstellationData constellation)
    {
        for (int i = 0; i < levelData.constellations.Length; i++)
            if (levelData.constellations[i] == constellation) return i;

        Debug.LogWarning($"[GameManager] Constellation '{constellation?.constellationName}' not found in LevelData.");
        return -1;
    }
}
