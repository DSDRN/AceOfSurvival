using System.Collections;
using UnityEngine;

public class GiantGloveHazard : MonoBehaviour
{
    [Header("Zamanlama (balance 9_Boss_Eldiven)")]
    [Tooltip("Min cooldown = 18 sn")]
    [SerializeField] private float minCooldown = 18f;

    [Tooltip("Max cooldown = 50 sn")]
    [SerializeField] private float maxCooldown = 50f;

    [Tooltip("Wave basi guvenli sure = 5 sn")]
    [SerializeField] private float safeTimeAtWaveStart = 5f;

    [Tooltip("Telegraph suresi = 4 sn")]
    [SerializeField] private float telegraphTime = 4f;

    [Header("Duvar ve hasar")]
    [SerializeField] private float wallTimeMin = 1f;
    [SerializeField] private float wallTimeMax = 2f;
    [SerializeField] private float cardDamage = 2.5f;
    [SerializeField] private Vector2 cardSize = new Vector2(5f, 3.5f);

    [Header("Gorsel baglantilar (child objeler)")]
    [SerializeField] private Transform shadow;
    [SerializeField] private Transform card;
    [SerializeField] private Transform warningIcon;
    [SerializeField] private Transform player;

    private BoxCollider2D cardCollider;
    private readonly Collider2D[] hitBuffer = new Collider2D[128];
    private ContactFilter2D noFilter;

    private float currentCooldownTimer;
    private float currentSafeTimer;
    private bool isWaveActive = false;
    private bool isSlamming = false;
    private bool pendingSlam = false; // YENI: Zaman yetmezse beklemeye alinan vurus

    private void Awake()
    {
        noFilter = ContactFilter2D.noFilter;
        if (card != null) cardCollider = card.GetComponent<BoxCollider2D>();
        ResetTimer();
        HideAll();
    }

    public void OnWaveStart()
    {
        isWaveActive = true;
        currentSafeTimer = safeTimeAtWaveStart;
    }

    public void StopCycle()
    {
        isWaveActive = false;
    }

    private void Update()
    {
        if (!isWaveActive || isSlamming) return;

        if (currentSafeTimer > 0f)
        {
            currentSafeTimer -= Time.deltaTime;
            return;
        }

        // EGER beklemede vurus varsa, guvenli sure bitince HEMEN vur!
        if (pendingSlam)
        {
            pendingSlam = false;
            StartCoroutine(SlamRoutine());
            return;
        }

        currentCooldownTimer -= Time.deltaTime;

        if (currentCooldownTimer <= 0f)
        {
            // Eldivenin isi bitirmesi icin gereken toplam zaman
            float neededTime = telegraphTime + wallTimeMax + 1.5f;

            // Eger wave'in bitmesine eldivenin dusmesinden DAHA AZ sure kaldiysa: PAS GEC, BEKLE!
            if (WaveManager.Instance != null && WaveManager.Instance.WaveTimeLeft < neededTime)
            {
                pendingSlam = true;
            }
            else
            {
                StartCoroutine(SlamRoutine());
            }
        }
    }

    private IEnumerator SlamRoutine()
    {
        isSlamming = true;

        if (player == null || !player.gameObject.activeInHierarchy)
        {
            ResetTimer();
            yield break;
        }

        Vector2 target = player.position;

        if (warningIcon != null)
        {
            warningIcon.position = target + Vector2.up * (cardSize.y / 2f + 0.6f);
            warningIcon.gameObject.SetActive(true);
        }

        if (shadow != null)
        {
            shadow.position = target;
            // GDD: Gölge ANINDA tam boyutta cikar!
            shadow.localScale = new Vector3(cardSize.x, cardSize.y, 1f);
            shadow.gameObject.SetActive(true);
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / telegraphTime;
            // Gölge tam boyutta, yalnizca surenin dolmasi bekleniyor
            yield return null;
        }

        if (warningIcon != null) warningIcon.gameObject.SetActive(false);

        if (card != null)
        {
            card.position = target;
            card.localScale = new Vector3(cardSize.x, cardSize.y, 1f);
            card.gameObject.SetActive(true);
        }

        int count = Physics2D.OverlapBox(target, cardSize, 0f, noFilter, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            EnemyHealth enemy = hitBuffer[i].GetComponent<EnemyHealth>();
            if (enemy != null) { enemy.TakeDamage(cardDamage, false); continue; }
            PlayerHealth ph = hitBuffer[i].GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(cardDamage);
        }

        if (cardCollider != null) cardCollider.enabled = true;

        yield return new WaitForSeconds(Random.Range(wallTimeMin, wallTimeMax));

        if (cardCollider != null) cardCollider.enabled = false;

        float ft = 0f;
        Vector3 startPos = card != null ? card.position : Vector3.zero;
        while (ft < 1f && card != null)
        {
            ft += Time.deltaTime / 0.3f;
            card.position = startPos + Vector3.up * (ft * 2f);
            card.localScale = new Vector3(cardSize.x * (1f - ft), cardSize.y * (1f - ft), 1f);
            yield return null;
        }

        HideAll();
        ResetTimer();
    }

    private void ResetTimer()
    {
        currentCooldownTimer = Random.Range(minCooldown, maxCooldown);
        isSlamming = false;
        pendingSlam = false;
    }

    private void HideAll()
    {
        if (shadow != null) shadow.gameObject.SetActive(false);
        if (card != null) card.gameObject.SetActive(false);
        if (warningIcon != null) warningIcon.gameObject.SetActive(false);
        if (cardCollider != null) cardCollider.enabled = false;
    }
}