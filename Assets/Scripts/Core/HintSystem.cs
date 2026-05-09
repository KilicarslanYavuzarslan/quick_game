using UnityEngine;

/// <summary>
/// Calculates and displays Manhattan distance hints on all currently-Revealed cells.
///
/// Distance is always relative to the active constellation's next target star.
/// Called by GameManager after:
///   - A correct star is collected (target advances)
///   - The active constellation switches
///
/// Display rules:
///   Distance 1 → gold  "1"
///   Distance 2 → orange "2"
///   Distance 3 → gray  "3"
///   Distance 4+ → blank (no number shown)
/// </summary>
public class HintSystem : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static HintSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

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
    /// Updates the hint on a single newly-revealed cell without iterating the whole grid.
    /// Call this when one cell transitions to Revealed (empty click).
    /// </summary>
    public void RefreshCell(Cell cell)
    {
        if (GameManager.Instance == null) return;

        Vector2Int targetPos = GameManager.Instance.GetCurrentTargetPosition();

        if (targetPos.x < 0)
        {
            cell.ClearHint();
            return;
        }

        int dist = ManhattanDistance(cell.gridPosition, targetPos);
        cell.SetHintDisplay(dist);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
