using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Paneller (Canvas altindaki objeler)")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Baglanti")]
    [SerializeField] private PlayerMovement player;
    [SerializeField] private PlayerResources resources; // Altin kaydi icin eklendi

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
        pausePanel.SetActive(false); // Ayarlar acilinca arkadaki pause menusu gizlensin
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pausePanel.SetActive(true); // Ayarlardan cikinca pause menusu geri gelsin
    }

    // GUNCELLEME: Pes Et (Give Up) fonksiyonu artik altinlari kaydediyor!
    public void GiveUpRun()
    {
        isPaused = false;
        Time.timeScale = 1f;

        // O ana kadar toplanan cip'leri ve sandiklari altina cevir ve KAYDET
        if (resources != null)
        {
            int waveReached = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveNumber : 0;
            int goldFromWaves = waveReached * 2;
            int goldFromChips = Mathf.FloorToInt(resources.TotalChipsEarned * 0.05f);
            int goldChests = resources.BonusGold;

            int totalGold = goldFromWaves + goldFromChips + goldChests;
            SaveSystem.AddGold(totalGold);
            Debug.Log($"Run iptal edildi. Kazanilan {totalGold} altin hesaba kaydedildi!");
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Cikis istendi");
    }
}