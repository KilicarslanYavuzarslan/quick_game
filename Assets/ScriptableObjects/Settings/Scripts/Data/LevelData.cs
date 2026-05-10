using UnityEngine;

/// <summary>
/// Defines a single level: the grid dimensions and which constellations appear in it.
///
/// To create a new level: right-click in the Project window →
/// Create → StellarSweep → Level Data.
///
/// Assign ConstellationData assets to the constellations array.
/// GridManager and GameManager read from this asset at level start.
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "StellarSweep/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Grid")]
    [Tooltip("Width (x) and height (y) of the grid in cells. Default: 8x8.")]
    public Vector2Int gridSize = new Vector2Int(8, 8);

    [Header("Constellations")]
    [Tooltip("All constellations that appear in this level. " +
             "Order here determines the default active constellation (index 0 is selected first).")]
    public ConstellationData[] constellations;

    /// <summary>
    /// Total number of stars across all constellations in this level.
    /// </summary>
    public int TotalStarCount
    {
        get
        {
            int total = 0;
            if (constellations == null) return 0;
            foreach (var c in constellations)
                if (c != null) total += c.StarCount;
            return total;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gridSize.x < 2 || gridSize.y < 2)
        {
            Debug.LogWarning($"[LevelData] '{name}': Grid size {gridSize} is too small. Minimum is 2x2.");
        }

        if (constellations == null || constellations.Length == 0)
        {
            Debug.LogWarning($"[LevelData] '{name}': No constellations assigned.");
            return;
        }

        // Check that no two constellations share a star position — would create ambiguous cells.
        for (int i = 0; i < constellations.Length; i++)
        {
            if (constellations[i] == null) continue;
            for (int j = i + 1; j < constellations.Length; j++)
            {
                if (constellations[j] == null) continue;
                foreach (var posA in constellations[i].starPositions)
                {
                    foreach (var posB in constellations[j].starPositions)
                    {
                        if (posA == posB)
                        {
                            Debug.LogWarning($"[LevelData] '{name}': '{constellations[i].constellationName}' and " +
                                             $"'{constellations[j].constellationName}' both have a star at {posA}. " +
                                             "Overlapping stars will break the game.");
                        }
                    }
                }
            }

            // Check all star positions fit within the grid.
            foreach (var pos in constellations[i].starPositions)
            {
                if (pos.x < 0 || pos.x >= gridSize.x || pos.y < 0 || pos.y >= gridSize.y)
                {
                    Debug.LogWarning($"[LevelData] '{name}': Star at {pos} in '{constellations[i].constellationName}' " +
                                     $"is outside the grid bounds {gridSize}.");
                }
            }
        }
    }
#endif
}

//