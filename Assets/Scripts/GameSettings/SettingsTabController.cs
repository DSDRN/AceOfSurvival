using UnityEngine;
using UnityEngine.UI;

public class SettingsTabController : MonoBehaviour
{
    [Header("Kategori Panelleri (Ici dolu olan Category_ objeleri)")]
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject gamePanel;
    [SerializeField] private GameObject accessibilityPanel;
    [SerializeField] private GameObject audioPanel;

    // Start yerine OnEnable kullaniyoruz!
    private void OnEnable()
    {
        // Menu her acildiginda hepsi kapanip sadece Video sekmesi acilir
        OpenVideoTab();
    }

    // Bu fonksiyonlari Menudeki "VIDEO", "GAME" butonlarinin OnClick kismina baglayacagiz
    public void OpenVideoTab()
    {
        videoPanel.SetActive(true);
        gamePanel.SetActive(false);
        accessibilityPanel.SetActive(false);
        audioPanel.SetActive(false);
    }

    public void OpenGameTab()
    {
        videoPanel.SetActive(false);
        gamePanel.SetActive(true);
        accessibilityPanel.SetActive(false);
        audioPanel.SetActive(false);
    }

    public void OpenAccessibilityTab()
    {
        videoPanel.SetActive(false);
        gamePanel.SetActive(false);
        accessibilityPanel.SetActive(true);
        audioPanel.SetActive(false);
    }

    public void OpenAudioTab()
    {
        videoPanel.SetActive(false);
        gamePanel.SetActive(false);
        accessibilityPanel.SetActive(false);
        audioPanel.SetActive(true);
    }
}