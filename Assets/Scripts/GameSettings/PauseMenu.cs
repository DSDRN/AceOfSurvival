using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Ana menüye dönmek için eklendi

public class PauseMenu : MonoBehaviour
{
    [Header("Paneller (Canvas altindaki objeler)")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Baglanti")]
    [SerializeField] private PlayerMovement player;

    private InputAction pauseAction;
    private bool isPaused;

    private void Awake()
    {
        pauseAction = new InputAction("Pause", InputActionType.Button);
        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void OnEnable() { pauseAction.Enable(); }
    private void OnDisable() { pauseAction.Disable(); }
    private void OnDestroy() { pauseAction.Dispose(); }

    private void Update()
    {
        if (pauseAction.WasPressedThisFrame())
            TogglePause();
    }

    private void TogglePause()
    {
        if (!isPaused && Time.timeScale == 0f) return;

        if (isPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (player != null) player.SetControlsEnabled(false);
        pausePanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (player != null) player.SetControlsEnabled(true);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // YENI: Give Up fonksiyonu
    public void GiveUpRun()
    {
        isPaused = false;
        Time.timeScale = 1f; // Fiziği tekrar başlat
                             // TODO: İleride burada toplanan altını SaveSystem'e kaydedeceğiz.

        // Ana menü sahnesini yükle. (Unity'de Build Settings'e "MainMenu" sahnesini eklemelisin)
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Cikis istendi");
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
    }
}