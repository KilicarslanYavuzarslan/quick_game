using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Represents a single cell on the game grid.
///
/// State machine:
///   Hidden    — default, dark tile, no information shown
///   Revealed  — empty cell was clicked, shows distance hint number
///   Peeked    — star cell exposed by AreaReveal ability: star visible but not yet collected
///   Collected — correct star clicked in sequence, dimmed star visual
///   Failed    — wrong star clicked, red star visual, permanently marked
///
/// Identity (isStar, constellation, starIndex) is set by GridManager at build time.
/// Click logic is handled here but deferred to GameManager for resolution.
/// Hover sound plays only on Hidden cells (AudioManager).
/// </summary>
public class Cell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    // ── State ────────────────────────────────────────────────────────────────

    public enum CellState { Hidden, Revealed, Peeked, Collected, Failed }

    [HideInInspector] public CellState state = CellState.Hidden;

    // Set by GridManager during grid construction
    [HideInInspector] public bool isStar;
    [HideInInspector] public ConstellationData constellation;
    [HideInInspector] public int starIndex;

    // Grid coordinate of this cell — set by GridManager
    [HideInInspector] public Vector2Int gridPosition;

    // ── Visual References ─────────────────────────────────────────────────────

    [Header("Visual References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image starImage;
    [SerializeField] private TextMeshProUGUI hintText;

    // ── Sprites ───────────────────────────────────────────────────────────────

    [Header("Sprites (assign in prefab)")]
    [SerializeField] private Sprite hiddenSprite;
    [SerializeField] private Sprite revealedSprite;
    [SerializeField] private Sprite collectedSprite;
    [SerializeField] private Sprite failedSprite;
    // Peeked reuses revealedSprite — no separate sprite needed unless art team wants one

    // ── Hint Text Colors ──────────────────────────────────────────────────────

    [Header("Hint Colors")]
    [SerializeField] private Color hintColor1 = new Color(0.937f, 0.624f, 0.153f); // Gold   — distance 1
    [SerializeField] private Color hintColor2 = new Color(0.937f, 0.537f, 0.184f); // Orange — distance 2
    [SerializeField] private Color hintColor3 = new Color(0.565f, 0.565f, 0.565f); // Gray   — distance 3

    // ── Background Colors (fallback when no sprite) ───────────────────────────

    [Header("Background Colors (fallback when no sprite)")]
    [SerializeField] private Color hiddenColor    = new Color(0.102f, 0.102f, 0.180f); // #1A1A2E
    [SerializeField] private Color revealedColor  = new Color(0.141f, 0.141f, 0.235f);
    [SerializeField] private Color collectedColor = new Color(0.102f, 0.102f, 0.180f);
    [SerializeField] private Color failedColor    = new Color(0.886f, 0.294f, 0.290f); // #E24B4A

    // ── Peeked (AreaReveal ability) ───────────────────────────────────────────

    [Header("Peeked (AreaReveal ability)")]
    [Tooltip("Star tint color when exposed by AreaReveal. Keep the star sprite white so tinting works.")]
    [SerializeField] private Color peekedStarColor = new Color(1f, 0.92f, 0.4f, 0.75f); // semi-transparent gold

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        UpdateVisuals();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Transitions the cell to a new state and updates all visuals immediately.
    /// </summary>
    public void SetState(CellState newState)
    {
        state = newState;
        UpdateVisuals();
    }

    /// <summary>
    /// Sets the hint number displayed on a Revealed cell.
    /// Called by HintSystem. Distance 4+ shows nothing.
    /// </summary>
    public void SetHintDisplay(int distance)
    {
        if (state != CellState.Revealed) return;
        if (hintText == null) return;

        switch (distance)
        {
            case 1:
                hintText.text  = "1";
                hintText.color = hintColor1;
                break;
            case 2:
                hintText.text  = "2";
                hintText.color = hintColor2;
                break;
            case 3:
                hintText.text  = "3";
                hintText.color = hintColor3;
                break;
            default:
                hintText.text = "";
                break;
        }
    }

    /// <summary>
    /// Clears the hint number (called when the active constellation switches).
    /// </summary>
    public void ClearHint()
    {
        if (hintText != null)
            hintText.text = "";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Click Handling
    // ─────────────────────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        // Forward click to the cell determined by GridManager when raycasts
        // and visual rects disagree. No verbose diagnostics here.
        var mapped = GridManager.Instance?.GetCellFromScreenPoint(eventData.position, eventData.pressEventCamera);
        if (mapped != null && mapped != this)
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.OnCellClicked(mapped);
            return;
        }

        // Hidden = normal undiscovered cell
        // Peeked = star exposed by AreaReveal ability — still needs to be clicked in order
        if (state != CellState.Hidden && state != CellState.Peeked) return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[Cell] GameManager.Instance is null.");
            return;
        }

        AudioManager.Instance?.PlayClick();
        GameManager.Instance.OnCellClicked(this);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hover Sound
    // ─────────────────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only Hidden cells produce hover sound — Peeked/Revealed/etc. are already "known"
        if (state == CellState.Hidden)
            AudioManager.Instance?.PlayHover();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Visuals
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        switch (state)
        {
            case CellState.Hidden:
                SetBackground(hiddenSprite, hiddenColor);
                SetStarVisible(false);
                if (hintText != null) hintText.text = "";
                break;

            case CellState.Revealed:
                SetBackground(revealedSprite, revealedColor);
                SetStarVisible(false);
                // Hint number is set separately by HintSystem
                break;

            case CellState.Peeked:
                // Exposed by AreaReveal — background like Revealed, star visible
                // so the player knows there's a star here and can click it in sequence
                SetBackground(revealedSprite, revealedColor);
                SetStarVisible(true, peekedStarColor);
                if (hintText != null) hintText.text = "";
                break;

            case CellState.Collected:
                SetBackground(collectedSprite, collectedColor);
                SetStarVisible(true, constellation != null ? constellation.color * 0.5f : Color.gray);
                if (hintText != null) hintText.text = "";
                break;

            case CellState.Failed:
                SetBackground(failedSprite, failedColor);
                SetStarVisible(true, new Color(0.886f, 0.294f, 0.290f)); // #E24B4A
                if (hintText != null) hintText.text = "";
                break;
        }
    }

    private void SetBackground(Sprite sprite, Color fallbackColor)
    {
        if (backgroundImage == null) return;
        if (sprite != null)
        {
            backgroundImage.sprite = sprite;
            backgroundImage.color  = Color.white;
        }
        else
        {
            backgroundImage.sprite = null;
            backgroundImage.color  = fallbackColor;
        }
    }

    private void SetStarVisible(bool visible, Color color = default)
    {
        if (starImage == null) return;
        starImage.gameObject.SetActive(visible);
        if (visible) starImage.color = color;
    }
}
