using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls the compass widget — rotates the needle toward the current target star
/// and flashes red on wrong clicks.
///
/// Hierarchy expected:
///   CompassWidget (Image — the dial background)
///   └── Needle     (Image — rotates around the dial center)
///   └── Label      (TextMeshPro — shows active constellation name)
///
/// The needle sprite should point UP at 0° rotation (North = up = toward target).
///
/// ReverseCompass ability: when the active constellation has this ability,
/// the needle points 180° away from the target (South points to correct star).
/// The needle turns reversedColor to warn the player that something is different.
/// </summary>
public class CompassController : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static CompassController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Inspector References ──────────────────────────────────────────────────

    [Header("Compass Parts")]
    [Tooltip("The compass dial root — used as the 'from' point for angle calculation.")]
    [SerializeField] private RectTransform compassWidget;

    [Tooltip("The needle child — this is what physically rotates.")]
    [SerializeField] private RectTransform needle;

    [Tooltip("Image on the needle — color changes based on state.")]
    [SerializeField] private Image needleImage;

    [Tooltip("TextMeshPro showing the active constellation name below the compass.")]
    [SerializeField] private TextMeshProUGUI constellationLabel;

    // ── Settings ──────────────────────────────────────────────────────────────

    [Header("Rotation")]
    [Tooltip("Degrees per second the needle rotates toward its target.")]
    [SerializeField] private float rotationSpeed = 300f;

    [Header("Needle Colors")]
    [Tooltip("Normal state — needle pointing at target.")]
    [SerializeField] private Color normalColor   = new Color(0.937f, 0.624f, 0.153f); // #EF9F27 gold

    [Tooltip("ReverseCompass ability active — needle points AWAY from target. Warns player.")]
    [SerializeField] private Color reversedColor = new Color(0.502f, 0.251f, 0.796f); // purple

    [Tooltip("Wrong click flash color.")]
    [SerializeField] private Color errorColor    = new Color(0.886f, 0.294f, 0.290f); // #E24B4A red

    [SerializeField] private float errorDuration = 0.45f;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private Vector2    targetScreenPos;
    private bool       hasTarget      = false;
    private Coroutine  errorCoroutine;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        // Color only — first target pushed by GameManager.InitGame() to avoid
        // Start() execution-order race where nextStarIndex is still null
        UpdateNeedleColor();
    }

    private void Update()
    {
        if (!hasTarget || needle == null) return;
        RotateNeedleToward(targetScreenPos);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API (called by GameManager)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Recalculates the target screen position from GameManager's current target.
    /// Call after: correct collection, constellation switch, target skip.
    /// </summary>
    public void UpdateTarget()
    {
        if (GameManager.Instance == null) return;

        Vector2Int gridPos = GameManager.Instance.GetCurrentTargetPosition();

        if (gridPos.x < 0)
        {
            hasTarget = false;
            if (constellationLabel != null) constellationLabel.text = "Done!";
            return;
        }

        Cell targetCell = GridManager.Instance?.GetCell(gridPos);
        if (targetCell == null)
        {
            hasTarget = false;
            return;
        }

        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, targetCell.transform.position);
        hasTarget = true;

        if (constellationLabel != null && GameManager.Instance.ActiveConstellation != null)
            constellationLabel.text = GameManager.Instance.ActiveConstellation.constellationName;

        // Update needle color in case constellation (and its ability) changed
        UpdateNeedleColor();

        AudioManager.Instance?.PlayCompassReaim();

        Debug.Log($"[CompassController] Target updated → {gridPos} | screen: {targetScreenPos}");
    }

    /// <summary>
    /// Flashes the needle red then returns to the correct state color.
    /// Called by GameManager on wrong click.
    /// </summary>
    public void PlayErrorFeedback()
    {
        if (errorCoroutine != null) StopCoroutine(errorCoroutine);
        errorCoroutine = StartCoroutine(ErrorFlash());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rotation
    // ─────────────────────────────────────────────────────────────────────────

    private void RotateNeedleToward(Vector2 target)
    {
        Vector2 fromPos = compassWidget != null
            ? RectTransformUtility.WorldToScreenPoint(null, compassWidget.position)
            : RectTransformUtility.WorldToScreenPoint(null, needle.position);

        Vector2 direction = target - fromPos;
        if (direction.sqrMagnitude < 1f) return;

        // atan2(x, y) → 0° = up, clockwise positive
        float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        // ReverseCompass ability: flip 180° so the SOUTH tip of the needle points at target
        if (IsReverseCompassActive())
            targetAngle += 180f;

        // Unity UI: positive Z = counter-clockwise, so negate
        float currentAngle = needle.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, -targetAngle, rotationSpeed * Time.deltaTime);

        needle.eulerAngles = new Vector3(0f, 0f, newAngle);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Needle Color
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets needle to the correct color for the current constellation ability state.
    /// Uses the active constellation's color. Darkened if ReverseCompass is active.
    /// Called on target update and after error flash ends.
    /// </summary>
    private void UpdateNeedleColor()
    {
        if (needleImage == null) return;

        // Get the active constellation's color
        Color constellationColor = Color.white;
        if (GameManager.Instance != null && GameManager.Instance.ActiveConstellation != null)
        {
            constellationColor = GameManager.Instance.ActiveConstellation.color;
        }

        // If ReverseCompass is active, darken the color as a warning
        if (IsReverseCompassActive())
        {
            needleImage.color = constellationColor * 0.6f;
        }
        else
        {
            needleImage.color = constellationColor;
        }
    }

    private bool IsReverseCompassActive()
    {
        return GameManager.Instance?.IsReverseCompassTriggered() ?? false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Error Flash Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ErrorFlash()
    {
        if (needleImage != null) needleImage.color = errorColor;
        yield return new WaitForSeconds(errorDuration);
        UpdateNeedleColor(); // returns to gold or purple depending on active constellation
        errorCoroutine = null;
    }
}
