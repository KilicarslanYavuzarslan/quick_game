using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Generates one button per constellation at runtime and handles switching.
/// Buttons are created entirely from code — no prefab required.
/// Place this component on a GameObject that has a child RectTransform
/// with a HorizontalLayoutGroup for the buttons.
/// </summary>
public class ConstellationSelector : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ConstellationSelector Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── References ────────────────────────────────────────────────────────────

    [Header("Data")]
    [SerializeField] private LevelData levelData;

    [Header("Button Container")]
    [Tooltip("A RectTransform with a HorizontalLayoutGroup — buttons are spawned here.")]
    [SerializeField] private RectTransform buttonContainer;

    [Header("Button Size")]
    [SerializeField] private Vector2 buttonSize = new Vector2(160f, 50f);
    [SerializeField] private int    fontSize    = 18;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private Button[] buttons;
    private Image[]  buttonImages;
    private int      activeIndex = 0;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        if (levelData == null)
        {
            Debug.LogError("[ConstellationSelector] No LevelData assigned.");
            return;
        }

        if (buttonContainer == null)
        {
            Debug.LogError("[ConstellationSelector] Button Container not assigned — drag SelectorContainer into the slot.");
            return;
        }

        GenerateButtons();
        RefreshVisuals(); // highlight default active (index 0)
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button Generation
    // ─────────────────────────────────────────────────────────────────────────

    private void GenerateButtons()
    {
        int count = levelData.constellations.Length;
        buttons      = new Button[count];
        buttonImages = new Image[count];

        for (int i = 0; i < count; i++)
        {
            CreateButton(i, levelData.constellations[i]);
        }
    }

    private void CreateButton(int index, ConstellationData constellation)
    {
        // ── Root object ───────────────────────────────────────────────────────
        var btnObj = new GameObject($"Btn_{constellation.constellationName}");
        btnObj.transform.SetParent(buttonContainer, false);

        var rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = buttonSize;

        // ── Background image ──────────────────────────────────────────────────
        var img = btnObj.AddComponent<Image>();
        img.color = constellation.color * 0.35f; // start dimmed

        // ── Button component ──────────────────────────────────────────────────
        var btn       = btnObj.AddComponent<Button>();
        var colors    = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor     = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        btn.targetGraphic = img;

        int capturedIndex = index; // capture for lambda closure
        btn.onClick.AddListener(() => OnButtonClicked(capturedIndex));

        buttons[index]      = btn;
        buttonImages[index] = img;

        // ── Label ─────────────────────────────────────────────────────────────
        var labelObj  = new GameObject("Label");
        labelObj.transform.SetParent(btnObj.transform, false);

        var labelRect         = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin   = Vector2.zero;
        labelRect.anchorMax   = Vector2.one;
        labelRect.offsetMin   = Vector2.zero;
        labelRect.offsetMax   = Vector2.zero;

        var tmp             = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text            = constellation.constellationName;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.fontSize        = fontSize;
        tmp.color           = Color.white;
        tmp.fontStyle       = FontStyles.Bold;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Switch Logic
    // ─────────────────────────────────────────────────────────────────────────

    private void OnButtonClicked(int index)
    {
        if (index == activeIndex) return; // already active, ignore

        activeIndex = index;
        GameManager.Instance?.SetActiveConstellation(index);
        RefreshVisuals();
    }

    /// <summary>
    /// Updates button colors — active constellation is bright, others are dimmed.
    /// </summary>
    private void RefreshVisuals()
    {
        for (int i = 0; i < buttonImages.Length; i++)
        {
            if (buttonImages[i] == null) continue;

            var color = levelData.constellations[i].color;
            buttonImages[i].color = (i == activeIndex)
                ? color                  // active: full color
                : color * 0.35f;         // inactive: dimmed
        }
    }

    /// <summary>
    /// Called externally if the active constellation is changed from code.
    /// </summary>
    public void SetActiveVisual(int index)
    {
        activeIndex = index;
        RefreshVisuals();
    }
}
