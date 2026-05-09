using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages all in-game UI: live HUD (score, mistakes, constellation progress)
/// and Win/Lose end-game panels.
///
/// Subscribes to ScoreManager events in Start so the HUD updates automatically
/// whenever the score or mistake count changes — no polling needed.
///
/// Manual wiring required in Inspector:
///   HUD:      scoreText, mistakesText, and one progressText per constellation
///   WinPanel: winPanel, winScoreText
///   LosePanel: losePanel, loseMistakesText
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    [Header("HUD")]
    [Tooltip("Live score display. Updated via ScoreManager.OnScoreChanged event.")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Live mistake counter. Format: 'Mistakes: 1 / 3'")]
    [SerializeField] private TextMeshProUGUI mistakesText;

    [Tooltip("One TMP text per constellation showing collected/total stars. " +
             "Array order must match LevelData.constellations order.")]
    [SerializeField] private TextMeshProUGUI[] constellationProgressTexts;

    [Tooltip("Optional TMP text that shows remaining hint charges.")]
    [SerializeField] private TextMeshProUGUI hintChargesText;

    private const int MISTAKE_LIMIT = 3; // mirrors GameManager — shown in HUD label

    // ── Win Panel ─────────────────────────────────────────────────────────────

    [Header("Win Panel")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winScoreText;

    // ── Lose Panel ────────────────────────────────────────────────────────────

    [Header("Lose Panel")]
    [SerializeField] private GameObject losePanel;
    [SerializeField] private TextMeshProUGUI loseMistakesText;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        // Hide end-game panels
        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        // Subscribe to ScoreManager events for live HUD updates
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged    += UpdateScoreText;
            ScoreManager.Instance.OnMistakesChanged += UpdateMistakesText;
        }

        if (HintSystem.Instance != null)
        {
            HintSystem.Instance.OnHintChargesChanged += UpdateHintChargesText;
            UpdateHintChargesText(HintSystem.Instance.RemainingHintCharges, 0);
        }

        // Initialise HUD to zero
        UpdateScoreText(0);
        UpdateMistakesText(0);
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks on scene reload
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged    -= UpdateScoreText;
            ScoreManager.Instance.OnMistakesChanged -= UpdateMistakesText;
        }

        if (HintSystem.Instance != null)
            HintSystem.Instance.OnHintChargesChanged -= UpdateHintChargesText;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HUD Updates (driven by ScoreManager events)
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateScoreText(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {newScore}";
    }

    private void UpdateMistakesText(int newMistakes)
    {
        if (mistakesText != null)
            mistakesText.text = $"Mistakes: {newMistakes} / {MISTAKE_LIMIT}";
    }

    private void UpdateHintChargesText(int remaining, int max)
    {
        if (hintChargesText != null)
            hintChargesText.text = max > 0 ? $"Hints: {remaining} / {max}" : "Hints: 0";
    }

    /// <summary>
    /// Updates the progress label for one constellation.
    /// Call from GameManager after each correct collection.
    /// e.g. "Orion: 3 / 5"
    /// </summary>
    public void UpdateProgress(int constellationIndex, int collectedCount, int totalCount, string name)
    {
        if (constellationProgressTexts == null) return;
        if (constellationIndex < 0 || constellationIndex >= constellationProgressTexts.Length) return;

        var text = constellationProgressTexts[constellationIndex];
        if (text != null)
            text.text = $"{name}: {collectedCount} / {totalCount}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // End-Game Panels
    // ─────────────────────────────────────────────────────────────────────────

    public void ShowWinPanel(int finalScore, bool isPerfect)
    {
        if (winPanel == null) { Debug.LogWarning("[UIManager] WinPanel not assigned."); return; }

        if (winScoreText != null)
        {
            string suffix = isPerfect ? "\n⭐ PERFECT ⭐" : "";
            winScoreText.text = $"Score: {finalScore}{suffix}";
        }

        winPanel.SetActive(true);
    }

    public void ShowLosePanel(int mistakeCount)
    {
        if (losePanel == null) { Debug.LogWarning("[UIManager] LosePanel not assigned."); return; }

        if (loseMistakesText != null)
            loseMistakesText.text = $"Mistakes: {mistakeCount}";

        losePanel.SetActive(true);
    }

    /// <summary>
    /// Wire this to a Hint button in the Inspector.
    /// </summary>
    public void OnHintClicked()
    {
        if (HintSystem.Instance == null)
        {
            Debug.LogWarning("[UIManager] HintSystem not found.");
            return;
        }

        HintSystem.Instance.TryUseHint();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button Callbacks (wire in Inspector)
    // ─────────────────────────────────────────────────────────────────────────

    public void OnPlayAgainClicked()  => SceneManager.LoadScene(MainMenuManager.SCENE_MAIN_GAME);
    public void OnMainMenuClicked()   => SceneManager.LoadScene(MainMenuManager.SCENE_MAIN_MENU);
}
