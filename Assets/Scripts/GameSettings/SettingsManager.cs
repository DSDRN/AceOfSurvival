using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Ses Sistemi (Audio Mixer)")]
    [Tooltip("Unity'den olusturdugun AudioMixer'i buraya surukle")]
    [SerializeField] private AudioMixer audioMixer;

    // --- HER YERDEN OKUNABILIR STATIK AYARLAR ---
    // GAME
    public static bool EnableDamageNumbers { get; private set; } = true;
    public static int TextLanguage { get; private set; } = 0;
    public static int AudioLanguage { get; private set; } = 0;
    public static int ControlScheme { get; private set; } = 0;

    // ACCESSIBILITY
    public static float ScreenshakeMultiplier { get; private set; } = 1f;
    public static bool EnableVibration { get; private set; } = true;
    public static bool EnableHitStop { get; private set; } = true;
    public static bool EnableHitFlash { get; private set; } = true;
    public static int FontStyle { get; private set; } = 0;

    private Resolution[] resolutions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahne degisse bile ayarlar silinmez
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        GetResolutions();
        LoadAllSettings();
    }

    private void GetResolutions()
    {
        resolutions = Screen.resolutions;
    }

    // ==========================================
    // 1. VIDEO AYARLARI
    // ==========================================
    public void SetWindowMode(int modeIndex)
    {
        // 0: Fullscreen, 1: Borderless, 2: Windowed
        FullScreenMode mode = modeIndex switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.Windowed
        };
        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt("WindowMode", modeIndex);
    }

    public void SetResolution(int resIndex)
    {
        if (resolutions == null || resolutions.Length == 0) return;
        Resolution res = resolutions[Mathf.Clamp(resIndex, 0, resolutions.Length - 1)];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt("Resolution", resIndex);
    }

    public void SetVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
    }

    // ==========================================
    // 2. GAME AYARLARI
    // ==========================================
    public void SetTextLanguage(int index) { TextLanguage = index; PlayerPrefs.SetInt("TextLang", index); }
    public void SetAudioLanguage(int index) { AudioLanguage = index; PlayerPrefs.SetInt("AudioLang", index); }
    public void SetControls(int index) { ControlScheme = index; PlayerPrefs.SetInt("Controls", index); }

    public void SetDamageNumbers(bool isEnabled)
    {
        EnableDamageNumbers = isEnabled;
        PlayerPrefs.SetInt("DamageNumbers", isEnabled ? 1 : 0);
    }

    // ==========================================
    // 3. ACCESSIBILITY AYARLARI
    // ==========================================
    public void SetScreenshake(float value) { ScreenshakeMultiplier = value; PlayerPrefs.SetFloat("Screenshake", value); }
    public void SetVibration(bool isEnabled) { EnableVibration = isEnabled; PlayerPrefs.SetInt("Vibration", isEnabled ? 1 : 0); }
    public void SetHitStop(bool isEnabled) { EnableHitStop = isEnabled; PlayerPrefs.SetInt("HitStop", isEnabled ? 1 : 0); }
    public void SetHitFlash(bool isEnabled) { EnableHitFlash = isEnabled; PlayerPrefs.SetInt("HitFlash", isEnabled ? 1 : 0); }
    public void SetFontStyle(int index) { FontStyle = index; PlayerPrefs.SetInt("FontStyle", index); }

    // ==========================================
    // 4. AUDIO AYARLARI (0.0001 ile 1 arasi deger gelir)
    // ==========================================
    public void SetMainVolume(float sliderValue)
    {
        // Ses logaritmik oldugu icin Log10 kullanilir. Mikser parametresinin adi "MasterVolume" olmali.
        if (audioMixer != null) audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20);
        PlayerPrefs.SetFloat("MasterVol", sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        if (audioMixer != null) audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20);
        PlayerPrefs.SetFloat("SFXVol", sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        if (audioMixer != null) audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20);
        PlayerPrefs.SetFloat("MusicVol", sliderValue);
    }

    public void SetVoicesVolume(float sliderValue)
    {
        if (audioMixer != null) audioMixer.SetFloat("VoicesVolume", Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20);
        PlayerPrefs.SetFloat("VoicesVol", sliderValue);
    }

    // ==========================================
    // KAYITLARI YUKLEME (Oyun acildiginda cagirilir)
    // ==========================================
    private void LoadAllSettings()
    {
        SetWindowMode(PlayerPrefs.GetInt("WindowMode", 0));
        SetVSync(PlayerPrefs.GetInt("VSync", 1) == 1);
        SetDamageNumbers(PlayerPrefs.GetInt("DamageNumbers", 1) == 1);

        SetTextLanguage(PlayerPrefs.GetInt("TextLang", 0));
        SetAudioLanguage(PlayerPrefs.GetInt("AudioLang", 0));
        SetControls(PlayerPrefs.GetInt("Controls", 0));

        SetScreenshake(PlayerPrefs.GetFloat("Screenshake", 1f));
        SetVibration(PlayerPrefs.GetInt("Vibration", 1) == 1);
        SetHitStop(PlayerPrefs.GetInt("HitStop", 1) == 1);
        SetHitFlash(PlayerPrefs.GetInt("HitFlash", 1) == 1);
        SetFontStyle(PlayerPrefs.GetInt("FontStyle", 0));

        // Sesleri biraz gecikmeli yuklemek iyidir (Mikser uyanana kadar)
        Invoke(nameof(LoadAudioSettings), 0.1f);
    }

    private void LoadAudioSettings()
    {
        SetMainVolume(PlayerPrefs.GetFloat("MasterVol", 1f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVol", 1f));
        SetMusicVolume(PlayerPrefs.GetFloat("MusicVol", 1f));
        SetVoicesVolume(PlayerPrefs.GetFloat("VoicesVol", 1f));
    }
}