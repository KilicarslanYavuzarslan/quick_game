using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Mixer ve Arayüz")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // 1. Oyuncu daha önce ses ayarı yaptıysa onu yükle, yapmadıysa varsayılan (1) yap
        float savedMusicVol = PlayerPrefs.GetFloat("SavedMusicVol", 1f);
        float savedSFXVol = PlayerPrefs.GetFloat("SavedSFXVol", 1f);

        // 2. Slider'ların pozisyonunu kayıtlı değere getir
        if (musicSlider != null) musicSlider.value = savedMusicVol;
        if (sfxSlider != null) sfxSlider.value = savedSFXVol;

        // 3. Slider'lar değiştikçe çalışacak fonksiyonları bağla
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
            
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // İlk açılışta sesi uygula
        SetMusicVolume(savedMusicVol);
        SetSFXVolume(savedSFXVol);
    }

    public void SetMusicVolume(float sliderValue)
    {
        // Slider değerini (0.0001 - 1) Mixer'in anlayacağı desibel (dB) değerine çevir
        mainMixer.SetFloat("MusicVolume", Mathf.Log10(sliderValue) * 20);
        
        // Değeri telefona/bilgisayara kaydet
        PlayerPrefs.SetFloat("SavedMusicVol", sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        mainMixer.SetFloat("SFXVolume", Mathf.Log10(sliderValue) * 20);
        PlayerPrefs.SetFloat("SavedSFXVol", sliderValue);
    }
}