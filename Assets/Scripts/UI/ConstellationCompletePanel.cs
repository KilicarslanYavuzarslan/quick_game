using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Full-screen overlay that plays a per-constellation completion video.
///
/// Play() accepts an optional onComplete callback. GameManager passes a callback
/// only for the final constellation — that callback shows the win screen.
/// This guarantees the win panel never appears until after the last video finishes.
///
/// If no video is assigned to a constellation, the callback is fired immediately
/// so the game flow is never blocked.
///
/// The panel pauses the game (Time.timeScale = 0) while the video plays.
/// VideoPlayer ignores timeScale and plays normally regardless.
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

    [Tooltip("VideoPlayer component — attach to any child of this panel.")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("Optional TMP text showing the constellation name on completion.")]
    [SerializeField] private TextMeshProUGUI constellationNameText;

    [Header("Render Texture Size")]
    [Tooltip("Match your video resolution for best quality.")]
    [SerializeField] private int renderWidth  = 1920;
    [SerializeField] private int renderHeight = 1080;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private RenderTexture renderTexture;
    private Action        onComplete;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        if (videoPlayer != null && videoDisplay != null)
        {
            renderTexture             = new RenderTexture(renderWidth, renderHeight, 0);
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
    /// Plays this constellation's completion video, then fires onComplete when done.
    /// If no video is assigned, onComplete fires immediately so nothing is blocked.
    /// Pass onComplete = null for non-final constellations.
    /// </summary>
    public void Play(ConstellationData constellation, Action onComplete = null)
    {
        this.onComplete = onComplete;

        if (constellation.completionVideo == null)
        {
            Debug.Log($"[ConstellationCompletePanel] No video for '{constellation.constellationName}' — firing callback immediately.");
            FireCallback();
            return;
        }

        if (panel == null || videoPlayer == null)
        {
            Debug.LogWarning("[ConstellationCompletePanel] Panel or VideoPlayer not wired — firing callback immediately.");
            FireCallback();
            return;
        }

        if (constellationNameText != null)
            constellationNameText.text = constellation.constellationName;

        panel.SetActive(true);
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
        Time.timeScale = 1f;

        Debug.Log("[ConstellationCompletePanel] Video ended.");
        FireCallback();
    }

    private void FireCallback()
    {
        var cb = onComplete;
        onComplete = null; // clear before invoking to prevent double-fire
        cb?.Invoke();
    }

    private void OnDestroy()
    {
        if (renderTexture != null) renderTexture.Release();
    }
}
