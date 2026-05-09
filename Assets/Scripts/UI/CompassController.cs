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
/// The needle sprite should point UP at 0° rotation.
/// Rotation is driven every frame in Update via MoveTowardsAngle.
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

    [Tooltip("Image on the needle — color changes to red on error.")]
    [SerializeField] private Image needleImage;

    [Tooltip("TextMeshPro showing the active constellation name below the compass.")]
    [SerializeField] private TextMeshProUGUI constellationLabel;

    // ── Settings ──────────────────────────────────────────────────────────────

    [Header("Rotation")]
    [Tooltip("Degrees per second the needle rotates toward its target.")]
    [SerializeField] private float rotationSpeed = 300f;

    [Header("Error Feedback")]
    [SerializeField] private Color normalColor = new Color(0.937f, 0.624f, 0.153f); // #EF9F27 gold
    [SerializeField] private Color errorColor  = new Color(0.886f, 0.294f, 0.290f); // #E24B4A red
    [SerializeField] private float errorDuration = 0.45f;

    // ── Runtime State ─────────────────────────────────────────────────────────

    private Vector2   targetScreenPos;
    private bool      hasTarget       = false;
    private Coroutine errorCoroutine;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        // Initialise color only — first target is pushed by GameManager.InitGame()
        // to avoid a Start() execution-order race where nextStarIndex is still null
        if (needleImage != null) needleImage.color = normalColor;
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

        // (-1,-1) means no more stars in this constellation
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

        // For Screen Space – Overlay canvas, camera param is null
        targetScreenPos = RectTransformUtility.WorldToScreenPoint(null, targetCell.transform.position);
        hasTarget = true;

        // Update constellation label
        if (constellationLabel != null && GameManager.Instance.ActiveConstellation != null)
            constellationLabel.text = GameManager.Instance.ActiveConstellation.constellationName;

        Debug.Log($"[CompassController] Target updated → {gridPos} | screen: {targetScreenPos}");
    }

    /// <summary>
    /// Flashes the needle red then returns to gold.
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
        // Use the compass widget center as the pivot reference point
        Vector2 fromPos = compassWidget != null
            ? RectTransformUtility.WorldToScreenPoint(null, compassWidget.position)
            : RectTransformUtility.WorldToScreenPoint(null, needle.position);

        Vector2 direction = target - fromPos;
        if (direction.sqrMagnitude < 1f) return; // already on target

        // atan2(x, y) gives angle where 0° = up, positive = clockwise
        float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        // Unity UI rotation: positive Z = counter-clockwise, so we negate
        float currentAngle = needle.eulerAngles.z;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, -targetAngle, rotationSpeed * Time.deltaTime);

        needle.eulerAngles = new Vector3(0f, 0f, newAngle);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Error Flash Coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ErrorFlash()
    {
        if (needleImage != null) needleImage.color = errorColor;
        yield return new WaitForSeconds(errorDuration);
        if (needleImage != null) needleImage.color = normalColor;
        errorCoroutine = null;
    }
}
