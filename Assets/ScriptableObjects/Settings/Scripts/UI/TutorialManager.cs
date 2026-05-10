using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Controls the tutorial scene flow:
///   1. Shows an intro panel explaining the rules (including hints).
///   2. Once the player closes the panel the level plays normally.
///   3. On win  → loads MainGame scene.
///   4. On loss → restarts the Tutorial scene.
///
/// Also owns the in-tutorial HUD for score display and the Hint purchase button.
/// The Hint button calls HintSystem.TryUseHint() which deducts 75 points and
/// reveals distance numbers on all currently-revealed cells.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // ── Intro Panel ───────────────────────────────────────────────────────────

    [Header("Intro Panel")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonText;

    // ── In-Game HUD ───────────────────────────────────────────────────────────

    [Header("In-Game HUD")]
    [Tooltip("Label that shows the player's current score during the tutorial.")]
    [SerializeField] private TextMeshProUGUI scoreLabel;

    [Tooltip("Button the player presses to purchase a hint (costs 75 points).")]
    [SerializeField] private Button hintButton;

    [Tooltip("Label on the hint button showing cost and remaining charges, e.g. 'HINT (75 pts) x3'.")]
    [SerializeField] private TextMeshProUGUI hintButtonLabel;

    [Tooltip("Optional popup shown briefly when the player cannot afford a hint.")]
    [SerializeField] private GameObject insufficientFundsPopup;

    // ── Internal State ────────────────────────────────────────────────────────

    private bool isPlaying       = false;
    private bool isTransitioning = false;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    private void Start()
    {
        // ── Intro panel setup ─────────────────────────────────────────────────
        tutorialPanel.SetActive(true);
        isPlaying = false;

        if (tutorialText != null)
        {
            tutorialText.text =
                "<b>WELCOME, EXPLORER!</b>\n\n" +
                "Your mission is to map the lost <b>Cancer</b> constellation.\n\n" +
                "1. The <b>Compass</b> always points toward your NEXT correct star.\n" +
                "2. Click empty spaces to reveal <b>Distance Numbers</b> — the lower the number, the closer the star.\n" +
                "3. Stuck? Press the <b>HINT</b> button to reveal all distance numbers on explored tiles.\n" +
                "   <size=80%><i>Hints cost 75 points each. You earn points by collecting correct stars.</i></size>\n\n" +
                "Find all 5 stars to complete your training!";
        }

        if (startButtonText != null)
            startButtonText.text = "START TRAINING";

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartTutorialLevel);
        }

        // ── Hint button setup ─────────────────────────────────────────────────
        if (hintButton != null)
        {
            hintButton.onClick.RemoveAllListeners();
            hintButton.onClick.AddListener(OnHintButtonPressed);
            hintButton.interactable = false; // disabled until game starts
        }

        if (insufficientFundsPopup != null)
            insufficientFundsPopup.SetActive(false);

        // ── Subscribe to score / hint charge events ────────────────────────────
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged += OnScoreChanged;

        if (HintSystem.Instance != null)
            HintSystem.Instance.OnHintChargesChanged += OnHintChargesChanged;

        // Initial HUD state
        RefreshScoreLabel(ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0);
        RefreshHintLabel();
    }

    private void OnDestroy()
    {
        // Clean up delegates to avoid calls into a destroyed object after scene unload
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.OnScoreChanged -= OnScoreChanged;

        if (HintSystem.Instance != null)
            HintSystem.Instance.OnHintChargesChanged -= OnHintChargesChanged;
    }

    // ── Game Flow ─────────────────────────────────────────────────────────────

    private void StartTutorialLevel()
    {
        tutorialPanel.SetActive(false);
        isPlaying = true;

        // Enable hint button now that the level is live
        if (hintButton != null)
            hintButton.interactable = true;

        RefreshHintLabel();
    }

    private void Update()
    {
        if (!isPlaying || isTransitioning) return;
        if (GameManager.Instance == null) return;

        switch (GameManager.Instance.State)
        {
            case GameManager.GameState.Won:
                isTransitioning = true;
                Debug.Log("[TutorialManager] Tutorial complete! Loading Main Game...");
                SceneManager.LoadScene("MainGame");
                break;

            case GameManager.GameState.Lost:
                isTransitioning = true;
                Debug.Log("[TutorialManager] Tutorial failed! Restarting...");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }

    // ── Hint Button ───────────────────────────────────────────────────────────

    private void OnHintButtonPressed()
    {
        if (HintSystem.Instance == null) return;

        bool success = HintSystem.Instance.TryUseHint();

        if (!success)
            ShowInsufficientFunds();

        // Always refresh so the label stays accurate
        RefreshHintLabel();
    }

    private void ShowInsufficientFunds()
    {
        if (insufficientFundsPopup == null) return;

        insufficientFundsPopup.SetActive(true);
        CancelInvoke(nameof(HideInsufficientFunds));
        Invoke(nameof(HideInsufficientFunds), 1.5f);
    }

    private void HideInsufficientFunds()
    {
        if (insufficientFundsPopup != null)
            insufficientFundsPopup.SetActive(false);
    }

    // ── HUD Callbacks ─────────────────────────────────────────────────────────

    private void OnScoreChanged(int newScore) => RefreshScoreLabel(newScore);

    private void OnHintChargesChanged(int remaining, int max) => RefreshHintLabel();

    private void RefreshScoreLabel(int score)
    {
        if (scoreLabel != null)
            scoreLabel.text = $"Score: {score}";
    }

    private void RefreshHintLabel()
    {
        if (hintButtonLabel == null) return;

        int charges = HintSystem.Instance != null ? HintSystem.Instance.RemainingHintCharges : 0;
        hintButtonLabel.text = $"HINT (75 pts)  ×{charges}";

        // Disable button when player has no charges left (even if they could afford it)
        if (hintButton != null && isPlaying)
            hintButton.interactable = charges > 0;
    }
}
