using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages all UI in the MainGame scene.
/// Handles Win/Lose panels now (Step 5).
/// HUD (score, progress bars, constellation buttons) added in Step 9.
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
        // Both panels start hidden
        if (winPanel  != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Panel Control
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GameManager when all stars are collected/processed.
    /// </summary>
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

    /// <summary>
    /// Called by GameManager when mistake limit is exceeded.
    /// </summary>
    public void ShowLosePanel(int mistakeCount)
    {
        if (losePanel == null) { Debug.LogWarning("[UIManager] LosePanel not assigned."); return; }

        if (loseMistakesText != null)
            loseMistakesText.text = $"Mistakes: {mistakeCount}";

        losePanel.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button Callbacks (wire in Inspector)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reloads the MainGame scene. Wire to Play Again buttons.</summary>
    public void OnPlayAgainClicked()
    {
        SceneManager.LoadScene(MainMenuManager.SCENE_MAIN_GAME);
    }

    /// <summary>Loads the MainMenu scene. Wire to Main Menu buttons.</summary>
    public void OnMainMenuClicked()
    {
        SceneManager.LoadScene(MainMenuManager.SCENE_MAIN_MENU);
    }
}
