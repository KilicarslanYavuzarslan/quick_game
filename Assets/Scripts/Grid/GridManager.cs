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
    /// Returns the LevelData asset this grid was built from.
    /// </summary>
    public LevelData LevelData => levelData;
}
