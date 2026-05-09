using UnityEngine;

/// <summary>
/// Defines a single constellation: its name, color, and the ordered positions
/// of its stars on the grid.
///
/// Star collection order = array index order.
/// Index 0 must be collected first, index 1 second, and so on.
///
/// Grid coordinates: (0,0) is top-left. X = column, Y = row.
/// </summary>
[CreateAssetMenu(fileName = "NewConstellation", menuName = "StellarSweep/Constellation Data")]
public class ConstellationData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in the HUD (e.g. Orion, Lyra).")]
    public string constellationName;

    [Tooltip("Color used for this constellation's stars and UI indicators.")]
    public Color color = Color.white;

    [Header("Star Layout")]
    [Tooltip("Grid positions of each star in COLLECTION ORDER. " +
             "Index 0 = first star the player must find, index 1 = second, etc. " +
             "Coordinates: X = column (0 = left), Y = row (0 = top). Max 7 on an 8x8 grid.")]
    public Vector2Int[] starPositions;

    // ── Post-Jam Hook ─────────────────────────────────────────────────────────
    // Constellation abilities — reserved for post-jam implementation.
    // Each constellation could grant a buff or reward on full completion.
    // See _dev/Notes/ideas.md for design directions.
    //
    // [Header("Ability (Post-Jam)")]
    // public ConstellationAbilityType abilityType;
    // public string abilityDescription;

    // ── Computed Properties ───────────────────────────────────────────────────

    /// <summary>
    /// Total number of stars in this constellation.
    /// </summary>
    public int StarCount => starPositions != null ? starPositions.Length : 0;

    /// <summary>
    /// Returns the grid position of the star at the given sequence index.
    /// Returns (-1,-1) if index is out of range.
    /// </summary>
    public Vector2Int GetStarPosition(int index)
    {
        if (starPositions == null || index < 0 || index >= starPositions.Length)
        {
            Debug.LogWarning($"[ConstellationData] Star index {index} out of range for {constellationName}.");
            return new Vector2Int(-1, -1);
        }
        return starPositions[index];
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Warn if any two stars share the same grid position — that would break the game.
        if (starPositions == null) return;
        for (int i = 0; i < starPositions.Length; i++)
        {
            for (int j = i + 1; j < starPositions.Length; j++)
            {
                if (starPositions[i] == starPositions[j])
                {
                    Debug.LogWarning($"[ConstellationData] '{constellationName}': Stars at index {i} and {j} " +
                                     $"share the same position {starPositions[i]}. This will cause bugs.");
                }
            }
        }
    }
#endif
}
