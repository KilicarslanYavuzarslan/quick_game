using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the game grid from a LevelData asset and manages cell lookup.
///
/// Spawns Cell prefabs into a GridLayoutGroup container.
/// Marks cells that are stars based on ConstellationData positions.
/// Provides GetCell(x, y) for any system that needs to reference a specific cell.
/// </summary>
public class GridManager : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────

    [Header("Data")]
    [SerializeField] private LevelData levelData;

    [Header("Prefab & Container")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private RectTransform gridContainer;

    // ── Runtime State ─────────────────────────────────────────────────────────

    // [x, y] — x = column (left to right), y = row (top to bottom)
    private Cell[,] grid;

    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GridManager Instance { get; private set; }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        BuildGrid();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Grid Construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        if (levelData == null)
        {
            Debug.LogError("[GridManager] No LevelData assigned. Cannot build grid.");
            return;
        }

        if (cellPrefab == null)
        {
            Debug.LogError("[GridManager] No Cell prefab assigned.");
            return;
        }

        int cols = levelData.gridSize.x;
        int rows = levelData.gridSize.y;
        grid = new Cell[cols, rows];

        // Adjust the GridLayoutGroup to match the level data
        var layout = gridContainer.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraintCount = cols; // forces correct column count
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                GameObject cellObj = Instantiate(cellPrefab, gridContainer);
                cellObj.name = $"Cell_{x}_{y}";

                Cell cell = cellObj.GetComponent<Cell>();
                cell.gridPosition = new Vector2Int(x, y);
                grid[x, y] = cell;

                // Check all constellations to see if this position is a star
                TryMarkAsStar(cell, x, y);
            }
        }

        Debug.Log($"[GridManager] Grid built: {cols}×{rows} | " +
                  $"Total stars: {levelData.TotalStarCount}");
    }

    /// <summary>
    /// Checks all constellation data and marks the cell as a star if its
    /// grid position matches any constellation's star position.
    /// </summary>
    private void TryMarkAsStar(Cell cell, int x, int y)
    {
        var pos = new Vector2Int(x, y);

        foreach (var constellation in levelData.constellations)
        {
            if (constellation == null) continue;

            for (int i = 0; i < constellation.starPositions.Length; i++)
            {
                if (constellation.starPositions[i] == pos)
                {
                    cell.isStar      = true;
                    cell.constellation = constellation;
                    cell.starIndex   = i;
                    return; // one cell can only belong to one constellation
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the Cell at the given grid coordinate, or null if out of bounds.
    /// </summary>
    public Cell GetCell(Vector2Int position)
    {
        if (!IsInBounds(position)) return null;
        return grid[position.x, position.y];
    }

    public Cell GetCell(int x, int y) => GetCell(new Vector2Int(x, y));

    /// <summary>
    /// Returns true if the position is within the grid.
    /// </summary>
    public bool IsInBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < levelData.gridSize.x &&
               position.y >= 0 && position.y < levelData.gridSize.y;
    }

    /// <summary>
    /// Iterates every cell and invokes the given action.
    /// Used by HintSystem to refresh all revealed cells at once.
    /// </summary>
    public void ForEachCell(System.Action<Cell> action)
    {
        if (grid == null) return;
        foreach (var cell in grid)
            if (cell != null) action(cell);
    }

    /// <summary>
    /// Returns the grid size defined in LevelData.
    /// </summary>
    public Vector2Int GridSize => levelData != null ? levelData.gridSize : Vector2Int.zero;

    /// <summary>
    /// Maps a screen point to the Cell under that point according to the
    /// GridLayoutGroup arrangement. Returns null if the point is outside
    /// the grid or on no cell.
    /// </summary>
    public Cell GetCellFromScreenPoint(Vector2 screenPoint, Camera uiCamera = null)
    {
        if (grid == null || gridContainer == null || levelData == null) return null;

        var layout = gridContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        int cols = levelData.gridSize.x;
        int rows = levelData.gridSize.y;

        // Convert screen -> local point in gridContainer space
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridContainer, screenPoint, uiCamera, out localPoint))
            return null;

        Rect rect = gridContainer.rect;

        // Use GridLayoutGroup metrics if available to compute cell bounds
        if (layout != null)
        {
            var padding = layout.padding;
            Vector2 cellSize = layout.cellSize;
            Vector2 spacing = layout.spacing;

            // top-left origin in local coordinates
            float startX = rect.xMin + padding.left;
            float startY = rect.yMax - padding.top;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    float cellXMin = startX + x * (cellSize.x + spacing.x);
                    float cellXMax = cellXMin + cellSize.x;

                    float cellYMax = startY - y * (cellSize.y + spacing.y);
                    float cellYMin = cellYMax - cellSize.y;

                    if (localPoint.x >= cellXMin && localPoint.x <= cellXMax &&
                        localPoint.y >= cellYMin && localPoint.y <= cellYMax)
                    {
                        // grid uses x=col, y=row
                        return GetCell(x, y);
                    }
                }
            }
        }
        else
        {
            // Fallback: split rect evenly
            float px = (localPoint.x - rect.xMin) / rect.width;
            float py = (rect.yMax - localPoint.y) / rect.height; // top-down

            int col = Mathf.FloorToInt(px * cols);
            int row = Mathf.FloorToInt(py * rows);

            if (col >= 0 && col < cols && row >= 0 && row < rows)
                return GetCell(col, row);
        }

        return null;
    }

    /// <summary>
    /// Returns the LevelData asset this grid was built from.
    /// </summary>
    public LevelData LevelData => levelData;

    /// <summary>
    /// Reveals all Hidden cells within Manhattan distance radius of center.
    /// Star cells become Peeked (star visible, still clickable in sequence).
    /// Empty cells become Revealed (hint numbers assigned by HintSystem afterward).
    /// Called by GameManager when AreaReveal ability fires.
    /// </summary>
    public void RevealArea(Vector2Int center, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius) continue; // Manhattan filter

                var pos = new Vector2Int(center.x + dx, center.y + dy);
                if (!IsInBounds(pos)) continue;

                var cell = GetCell(pos);
                if (cell == null || cell.state != Cell.CellState.Hidden) continue;

                // Stars become Peeked so the player can see where the star is
                // Empty cells become Revealed so hint numbers appear
                cell.SetState(cell.isStar ? Cell.CellState.Peeked : Cell.CellState.Revealed);
            }
        }

        Debug.Log($"[GridManager] RevealArea center={center} radius={radius}");
    }
}
