using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("Veri kaynaklari (surukle)")]
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerResources resources;
    [SerializeField] private WaveManager waveManager;

    [Header("Sol ust - Can")]
    [Tooltip("HealthBar altindaki Fill objesi (pivot X = 0!)")]
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Sol ust - XP + Chips")]
    [Tooltip("XPBar altindaki Fill objesi (pivot X = 0!)")]
    [SerializeField] private RectTransform xpFill;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text chipsText;

    [Header("Ust orta - Wave")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text timerText;

    [Header("Sag ust - Bekleyen leveller")]
    [SerializeField] private TMP_Text pendingText;

    [Header("Yetenekler")]
    [SerializeField] private Image dashRing;
    [SerializeField] private PlayerMovement movement; // Hiyerarşiden Player'ı buraya sürükleyeceksin

    private void Update()
    {
        float lerpSpeed = 10f * Time.unscaledDeltaTime; // unscaled kullanıyoruz ki oyun dursa bile UI akıcı kalsın

        // ---- CAN ----
        if (health != null)
        {
            float targetHealthPct = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f;

            // Mevcut doluluk oranını (X scale) hedef orana doğru yumuşakça kaydır (Lerp)
            float currentX = Mathf.Lerp(healthFill.localScale.x, targetHealthPct, lerpSpeed);

            healthFill.localScale = new Vector3(currentX, 1f, 1f);
            healthText.text = $"{Mathf.CeilToInt(health.CurrentHealth)}/{Mathf.CeilToInt(health.MaxHealth)}";
        }

        // ---- XP + LEVEL + CHIPS ----
        if (resources != null)
        {
            float targetXpPct = resources.XPToNext > 0f ? resources.CurrentXP / resources.XPToNext : 0f;

            // XP barını yumuşakça kaydır
            float currentXpX = Mathf.Lerp(xpFill.localScale.x, targetXpPct, lerpSpeed);

            xpFill.localScale = new Vector3(currentXpX, 1f, 1f);
            levelText.text = $"Lv {resources.Level}";
            chipsText.text = $"Chips: {resources.Chips}";

            pendingText.text = resources.PendingLevelUps > 0 ? $"+{resources.PendingLevelUps}" : "";
        }

        // ---- WAVE + SURE ----
        if (waveManager != null)
        {
            waveText.text = $"WAVE {waveManager.CurrentWaveNumber}";
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, waveManager.WaveTimeLeft)).ToString();
        }
    }
}
