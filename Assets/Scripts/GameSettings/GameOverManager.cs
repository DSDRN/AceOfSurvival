using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Arayuz")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Ayarlar")]
    [Tooltip("Ana menu sahnesinin tam adi (Build Settings ile ayni olmali)")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isGameOver = false;

    private void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    // Karakterin cani 0 oldugunda PlayerHealth bu fonksiyonu cagiracak
    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Time.timeScale = 0f; // Oyunu dondur

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Debug.Log("Game Over ekrani acildi!");
    }

    // Buton OnClick'te bu fonksiyonu cagiracak
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Zamani normale dondur (kritik!)
        SceneManager.LoadScene(mainMenuSceneName);
    }
}