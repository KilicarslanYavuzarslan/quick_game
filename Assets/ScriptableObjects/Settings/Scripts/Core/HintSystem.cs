using UnityEngine;
using System;

/// <summary>
/// Manages purchasable distance hints on the game grid.
///
/// By default, revealed cells show NO distance numbers — numbers are hidden until
/// the player spends 75 points via TryUseHint(). On purchase, numbers appear on
/// every currently-Revealed cell based on Manhattan distance to the active target.
///
/// Numbers are automatically cleared (ClearAllHints) whenever the target changes
/// (correct star collected, constellation switched) so stale distances never persist.
/// The player must purchase a new hint to see fresh numbers after each target shift.
///
/// Display rules (when a hint is active):
///   Distance 1 → gold  "1"
///   Distance 2 → orange "2"
///   Distance 3 → gray  "3"
///   Distance 4+ → blank
/// </summary>
public class HintSystem : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static HintSystem Instance { get; private set; }

    [Header("Hint Charges")]
    [SerializeField] private int maxHintCharges = 3;

    [Header("Hint Cost")]
    [Tooltip("Score cost paid every time the Hint button is used.")]
    [SerializeField] private int hintScoreCost = 75;

    public int RemainingHintCharges { get; private set; }

    public event Action<int, int> OnHintChargesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ResetCharges();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resets the available hint charges to the configured maximum.
    /// Call this at the start of a new game.
    /// </summary>
    public void ResetCharges()
    {
        RemainingHintCharges = Mathf.Max(0, maxHintCharges);
        OnHintChargesChanged?.Invoke(RemainingHintCharges, maxHintCharges);
    }

    /// <summary>
    /// Consumes one hint charge and refreshes all hints if any remain.
    /// Returns true when the hint was shown.
    /// </summary>
    public bool TryUseHint()
    {
        if (GameManager.Instance == null || GridManager.Instance == null) return false;
        if (RemainingHintCharges <= 0)
        {
            Debug.Log("[HintSystem] No hint charges left.");
            return false;
        }

        // Check if player has enough score
        if (ScoreManager.Instance == null || ScoreManager.Instance.Score < hintScoreCost)
        {
            Debug.Log($"[HintSystem] Insufficient score. Need {hintScoreCost}, have {(ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0)}");
            return false;
        }

        if (ScoreManager.Instance != null)
        {
            int deducted = ScoreManager.Instance.SpendScore(hintScoreCost);
            Debug.Log($"[HintSystem] Hint cost paid: {deducted}/{hintScoreCost}. Current score: {ScoreManager.Instance.Score}");
        }

        RemainingHintCharges--;
        OnHintChargesChanged?.Invoke(RemainingHintCharges, maxHintCharges);

        RefreshAllHints();
        Debug.Log($"[HintSystem] Hint used. Remaining charges: {RemainingHintCharges}/{maxHintCharges}");
        return true;
    }

    /// <summary>
    /// Recalculates hints for every Revealed cell based on the current target.
    /// Non-Revealed cells are untouched (their hint text is already blank).
    /// </summary>
    public void RefreshAllHints()
    {
        if (GameManager.Instance == null || GridManager.Instance == null) return;

        Vector2Int targetPos = GameManager.Instance.GetCurrentTargetPosition();

        // No active target (constellation fully done) — clear everything
        if (targetPos.x < 0)
        {
            GridManager.Instance.ForEachCell(cell => cell.ClearHint());
            return;
        }

        GridManager.Instance.ForEachCell(cell =>
        {
            if (cell.state != Cell.CellState.Revealed)
            {
                // Keep non-revealed cells clean (they clear themselves on state change)
                return;
            }

            int dist = ManhattanDistance(cell.gridPosition, targetPos);
            cell.SetHintDisplay(dist);
        });

        Debug.Log($"[HintSystem] Hints refreshed → target at {targetPos}");
    }

    /// <summary>
    /// Clears all distance numbers from every cell on the grid.
    /// Called by GameManager whenever the target shifts (correct star collected,
    /// constellation switched) so stale distances never remain visible.
    /// </summary>
    public void ClearAllHints()
    {
        GridManager.Instance?.ForEachCell(cell => cell.ClearHint());
        Debug.Log("[HintSystem] All hints cleared.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
