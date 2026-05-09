using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen overlay that plays a completion video when a constellation
/// is fully collected.
///
/// Hierarchy expected:
///   ConstellationCompletePanel (this component + CanvasGroup or plain panel)
///   └── VideoDisplay     (RawImage — displays the video render texture)
///   └── ConstellationNameText (TMP — shows which constellation was completed)
///   └── VideoPlayer      (VideoPlayer component on any child or this object)
///
/// The panel pauses the game (Time.timeScale = 0) while the video plays.
/// VideoPlayer is immune to timeScale, so it plays correctly regardless.
/// If no video is assigned to a constellation, the panel is silently skipped.
/// </summary>
public class ConstellationCompletePanel : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static ConstellationCompletePanel Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── References ────────────────────────────────────────────────────────────

    [Header("Panel Parts")]
    [Tooltip("The root panel GameObject to show/hide.")]
    [SerializeField] private GameObject panel;

    [Tooltip("RawImage that displays the video. A RenderTexture is created automatically at runtime.")]
    [SerializeField] private RawImage videoDisplay;

    [Tooltip("VideoPlayer component — attach it to any child of this panel.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("Optional TMP text showing the constellation name on completion.")]
    [SerializeField] private TextMeshProUGUI constellationNameText;

    [Header("Render Texture Size")]
    [Tooltip("Resolution of the render texture. Match your video resolution for best quality.")]
    [SerializeField] private int renderWidth  = 1920;
    [SerializeField] private int renderHeight = 1080;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private RenderTexture renderTexture;

    private void Start()
    {
        // Hide panel on start
        if (panel != null) panel.SetActive(false);

        if (videoPlayer != null && videoDisplay != null)
        {
            // Create a RenderTexture and wire it to both the VideoPlayer output
            // and the RawImage display
            renderTexture = new RenderTexture(renderWidth, renderHeight, 0);
            videoPlayer.renderMode    = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoDisplay.texture      = renderTexture;

            videoPlayer.loopPointReached += OnVideoEnd;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the panel and plays the constellation's completion video.
    /// Silently skipped if no video is assigned to this constellation.
    /// </summary>
    public void Play(ConstellationData constellation)
    {
        if (constellation.completionVideo == null)
        {
            Debug.Log($"[ConstellationCompletePanel] No video for '{constellation.constellationName}' — skipping.");
            return;
        }

        if (panel == null || videoPlayer == null)
        {
            Debug.LogWarning("[ConstellationCompletePanel] Panel or VideoPlayer reference not set.");
            return;
        }

        if (constellationNameText != null)
            constellationNameText.text = constellation.constellationName;

        panel.SetActive(true);

        // Pause the game — VideoPlayer ignores timeScale and continues playing
        Time.timeScale = 0f;

        videoPlayer.clip = constellation.completionVideo;
        videoPlayer.Play();

        Debug.Log($"[ConstellationCompletePanel] Playing video for '{constellation.constellationName}'");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Video End
    // ─────────────────────────────────────────────────────────────────────────

    private void OnVideoEnd(VideoPlayer source)
    {
        videoPlayer.Stop();
        panel.SetActive(false);

        // Resume game
        Time.timeScale = 1f;

        Debug.Log("[ConstellationCompletePanel] Video ended — resuming game.");
    }

    private void OnDestroy()
    {
        // Clean up the runtime render texture
        if (renderTexture != null) renderTexture.Release();
    }
}
