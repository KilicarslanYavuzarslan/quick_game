using UnityEngine;

/// <summary>
/// Singleton audio manager. Lives in MainMenu scene and persists across all scenes
/// via DontDestroyOnLoad.
///
/// Two AudioSources are created at runtime — one for looping music, one for SFX.
/// All SFX use PlayOneShot so overlapping sounds never cut each other off.
///
/// Assign clips in the Inspector. Any unassigned slot is silently skipped.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── SFX Clips ─────────────────────────────────────────────────────────────

    [Header("SFX Clips")]
    [Tooltip("Plays when the cursor enters a Hidden cell.")]
    [SerializeField] private AudioClip hoverClip;

    [Tooltip("Plays on any valid cell click (before result is resolved).")]
    [SerializeField] private AudioClip clickClip;

    [Tooltip("Plays when a correct star is collected.")]
    [SerializeField] private AudioClip correctClip;

    [Tooltip("Plays when a wrong star is clicked.")]
    [SerializeField] private AudioClip wrongClip;

    [Tooltip("Plays when the compass re-aims to a new target star.")]
    [SerializeField] private AudioClip compassReaimClip;

    [Tooltip("Plays when all stars in one constellation are collected.")]
    [SerializeField] private AudioClip constellationCompleteClip;

    [Tooltip("Plays on full game win (all constellations complete).")]
    [SerializeField] private AudioClip winClip;

    // ── Music ─────────────────────────────────────────────────────────────────

    [Header("Music")]
    [Tooltip("Background music loop. Starts on Awake and persists across scenes.")]
    [SerializeField] private AudioClip bgMusic;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.4f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Start()
    {
        // Music source — loops forever
        musicSource        = gameObject.AddComponent<AudioSource>();
        musicSource.clip   = bgMusic;
        musicSource.loop   = true;
        musicSource.volume = musicVolume;
        if (bgMusic != null) musicSource.Play();

        // SFX source — PlayOneShot, no looping
        sfxSource        = gameObject.AddComponent<AudioSource>();
        sfxSource.loop   = false;
        sfxSource.volume = sfxVolume;
    }

    // ── Public SFX Methods ────────────────────────────────────────────────────

    public void PlayHover()                 => PlaySFX(hoverClip);
    public void PlayClick()                 => PlaySFX(clickClip);
    public void PlayCorrect()               => PlaySFX(correctClip);
    public void PlayWrong()                 => PlaySFX(wrongClip);
    public void PlayCompassReaim()          => PlaySFX(compassReaimClip);
    public void PlayConstellationComplete() => PlaySFX(constellationCompleteClip);
    public void PlayWin()                   => PlaySFX(winClip);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
