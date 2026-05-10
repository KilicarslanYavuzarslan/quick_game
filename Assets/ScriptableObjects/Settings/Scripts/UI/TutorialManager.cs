using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Oyuncuya başlangıçta tek bir bilgi paneli gösterir.
/// Panel kapandıktan sonra oyuncu bölümü oynar.
/// Bölüm kazanıldığında otomatik olarak ana oyuna (MainGame) geçiş yapar.
/// Bölüm kaybedildiğinde tutorial sahnesini baştan başlatır.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI startButtonText;

    private bool isPlaying = false;
    private bool isTransitioning = false;

    private void Start()
    {
        
       
        
        // Paneli aktif et ve kuralları yazdır
        tutorialPanel.SetActive(true);
        isPlaying = false;

       
        
        if (tutorialText != null)
        {          
            tutorialText.text = "<b>WELCOME, EXPLORER!</b>\n\n" +
                                "Your mission is to map the lost constellations.\n\n" +
                                "1. The <b>Compass</b> always points toward your NEXT correct star.\n" +
                                "2. Click grid spaces to reveal <b>Distance Numbers</b> (1 = right next to you).\n" +
                                "3. Use both the compass and numbers to find the stars.\n\n" +
                                "Find the <i>Cancer</i> constellation to complete your training!";
        }

        if (startButtonText != null)
        {
            startButtonText.text = "START TRAINING";
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartTutorialLevel);
        }
    }

    private void StartTutorialLevel()
    {
        // Butona basıldığında paneli kapat ve oyunu başlat
        tutorialPanel.SetActive(false);
        isPlaying = true;
    }

    private void Update()
    {
        // Sadece oyuncu paneli kapattıysa ve oyun henüz geçiş yapmıyorsa kontrol et
        if (!isPlaying || isTransitioning) return;

        if (GameManager.Instance != null)
        {
            // KAZANMA DURUMU
            if (GameManager.Instance.State == GameManager.GameState.Won)
            {
                isTransitioning = true;
                Debug.Log("[TutorialManager] Tutorial complete! Loading Main Game...");
                
                // Ana oyuna geçiş yap (Sahne adının tam olarak MainGame olduğundan emin ol)
                SceneManager.LoadScene("MainGame"); 
            }
            // KAYBETME DURUMU
            else if (GameManager.Instance.State == GameManager.GameState.Lost)
            {
                isTransitioning = true;
                Debug.Log("[TutorialManager] Tutorial failed! Restarting tutorial...");
                
                // Kaybedilirse mevcut sahneyi (Tutorial) baştan yükle ki tekrar deneyebilsin
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }
}