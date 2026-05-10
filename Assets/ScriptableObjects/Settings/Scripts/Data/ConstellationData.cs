using UnityEngine;
using UnityEngine.Video;

// ── Ability Types ─────────────────────────────────────────────────────────────
// Each constellation can have one special ability that activates during or
// at the end of its collection sequence.
//
// None         — standard constellation, no special effect.
// ReverseCompass — while this constellation is active, the compass needle
//                  points AWAY from the target (South points to correct star).
//                  The needle color changes to warn the player.
// AreaReveal   — when the LAST star of this constellation is collected,
//                  all Hidden cells within abilityRadius (Manhattan distance)
//                  are revealed. Star cells become Peeked (visible but uncollected).
public enum ConstellationAbilityType { None, ReverseCompass, AreaReveal }

/// <summary>
/// Defines a single constellation: its name, color, ordered star positions,
/// special ability, and optional completion video.
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

    [Header("Special Ability")]
    [Tooltip("The special effect this constellation provides.\n" +
             "ReverseCompass: needle points away from the target while this constellation is active.\n" +
             "AreaReveal: reveals cells around the last collected star on completion.")]
    public ConstellationAbilityType abilityType = ConstellationAbilityType.None;

    [Tooltip("Radius (Manhattan distance) for AreaReveal. 1 = 4 adjacent cells. Only used if abilityType = AreaReveal.")]
    public int abilityRadius = 1;

    [Header("Completion Video")]
    [Tooltip("Optional MP4 (VideoClip) that plays when all stars in this constellation are collected.")]
    public VideoClip completionVideo;

    // ── Computed Properties ───────────────────────────────────────────────────

    /// <summary>Total number of stars in this constellation.</summary>
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
