using UnityEngine;

/// <summary>
/// Central game controller. Owns game state, tracks star collection progress
/// per constellation, handles all click resolution, and detects win/lose.
///
/// Other systems (CompassController, HintSystem, UIManager, ScoreManager)
/// are called from here via Instance references — stubs are commented out
/// and activated step by step as those systems are built.
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

    // Player is allowed this many misclicks. Game over when exceeded.
    private const int MISTAKE_LIMIT = 2;
    public int MistakeCount { get; private set; }

    // ── Constellation Tracking ────────────────────────────────────────────────

    // nextStarIndex[i] = index of the next star to collect in constellation i
    // Advances on correct collection AND when a target star is Failed (skips it)
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

        // Now that state is ready, push the first target to the compass
        CompassController.Instance?.UpdateTarget();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Click Resolution (called by Cell.OnPointerClick)
    // ─────────────────────────────────────────────────────────────────────────

    public void OnCellClicked(Cell cell)
    {
        if (State != GameState.Playing) return;

        // ── Empty cell ────────────────────────────────────────────────────────
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
        AdvancePastFailedStars(constellationIndex); // skip any previously-failed stars in sequence

        Debug.Log($"[GameManager] ✓ Correct! {cell.constellation.constellationName} " +
                  $"star {cell.starIndex + 1} collected.");

        // ScoreManager.Instance?.AddScore(100);                         // Step 9
        // AudioManager.Instance?.PlayCorrect();                         // Step 10
        // UIManager.Instance?.UpdateProgress(constellationIndex,        // Step 9
        //     nextStarIndex[constellationIndex]);
        HintSystem.Instance?.RefreshAllHints();
        CompassController.Instance?.UpdateTarget();

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

        // If the failed star WAS the current target of its constellation,
        // advance that constellation's index so the target skips forward.
        if (wasCurrentTarget)
        {
            nextStarIndex[constellationIndex]++;
            AdvancePastFailedStars(constellationIndex);
            CompassController.Instance?.UpdateTarget();
        }

        CompassController.Instance?.PlayErrorFeedback();
        // AudioManager.Instance?.PlayWrong();                           // Step 10
        // ScoreManager.Instance?.RegisterMistake();                     // Step 9

        if (MistakeCount > MISTAKE_LIMIT)
        {
            State = GameState.Lost;
            Debug.Log("[GameManager] Game Over — too many mistakes!");
            UIManager.Instance?.ShowLosePanel(MistakeCount);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Constellation Switching (called by ConstellationSelector — Step 6)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Switches the active constellation. Compass and hints update immediately.
    /// </summary>
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
                return; // at least one constellation still has stars left
        }

        State = GameState.Won;
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        bool perfect   = MistakeCount == 0;
        Debug.Log($"[GameManager] You Win! Score={finalScore} Perfect={perfect}");
        UIManager.Instance?.ShowWinPanel(finalScore, perfect);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// After a star is Collected or Failed, skip forward past any
    /// already-Failed stars so the target always points to a reachable cell.
    /// </summary>
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
    /// Used by CompassController to aim the needle.
    /// </summary>
    public Vector2Int GetCurrentTargetPosition()
    {
        // Guard: called before InitGame() runs (Script Execution Order race)
        if (nextStarIndex == null) return new Vector2Int(-1, -1);

        int idx = nextStarIndex[activeConstellationIndex];
        if (idx >= ActiveConstellation.StarCount)
            return new Vector2Int(-1, -1);

        return ActiveConstellation.starPositions[idx];
    }

    /// <summary>
    /// Returns the index of a ConstellationData in the level's array.
    /// Returns -1 if not found.
    /// </summary>
    private int GetConstellationIndex(ConstellationData constellation)
    {
        for (int i = 0; i < levelData.constellations.Length; i++)
            if (levelData.constellations[i] == constellation) return i;

        Debug.LogWarning($"[GameManager] Constellation '{constellation?.constellationName}' not found in LevelData.");
        return -1;
    }
}
