using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public static readonly List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>();

    /// Bu run'da oldurulen dusman sayisi (run sonu ekrani icin)
    public static int RunKills = 0;

    [Header("Can (balance 4_Dusmanlar)")]
    [SerializeField] private float maxHealth = 10f;

    [Header("Knockback (balance 4_Dusmanlar 'Knockback direnci' kolonu)")]
    [Tooltip("0 = tam itilir, 1 = hic itilemez. Sarhos 0, Krupiye 0.95, Fedai 0.9, Boss 1.0")]
    [Range(0f, 1f)]
    [SerializeField] private float knockbackResistance = 0f;

    [Tooltip("Itilmenin surdugu sure (his ayari)")]
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Drop (balance 4_Dusmanlar)")]
    [SerializeField] private float chipsDropped = 5f;
    [SerializeField] private float xpDropped = 3f;

    /// AI scriptleri bunu kontrol eder: itiliyorken kendi hareketlerini yapmazlar
    public bool IsKnockedBack => knockTimer > 0f;

    /// Can yuzdesi (0-1) - boss bari ve enrage icin
    public float HealthPercent => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);

    private float currentHealth;
    private SpriteRenderer sr;
    private Color originalColor;
    private Rigidbody2D rb;
    private Vector2 knockVelocity;
    private float knockTimer;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        originalColor = sr.color;
        rb = GetComponent<Rigidbody2D>();   // yoksa null kalir, knockback pas gecilir
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        sr.color = originalColor;
        knockTimer = 0f;
        ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    private void FixedUpdate()
    {
        // Itilme aktifken hiz BIZIM elimizde (AI'lar IsKnockedBack ile susar)
        if (knockTimer > 0f)
        {
            knockTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockVelocity;
        }
    }

    /// <summary>
    /// Silahlar vurdugunda iter. dir = itilme yonu, force = ham guc.
    /// Direnc formulu: gercek = force x (1 - direnc).
    /// NOT: negatif melee hasar kuralinda bile knockback CALISIR
    /// (GDD: "hasar veremezsin ama knockback kalir").
    /// </summary>
    public void ApplyKnockback(Vector2 dir, float force)
    {
        if (rb == null) return;

        float etki = force * (1f - knockbackResistance);
        if (etki <= 0.01f) return;   // direnc 1.0 (boss) = hic itilme

        knockVelocity = dir.normalized * etki;
        knockTimer = knockbackDuration;
    }

    public void TakeDamage(float damage, bool isCrit)
    {
        currentHealth -= damage;

        // HASAR SAYISI: beyaz normal / sari-buyuk crit ("?." = manager yoksa sessiz)
        DamageNumberManager.Instance?.Show(transform.position, damage, isCrit,
            isCrit ? DamageNumberManager.CritRenk : DamageNumberManager.NormalRenk);

        StopAllCoroutines();
        StartCoroutine(HitFlash());

        if (currentHealth <= 0f)
            Die();
    }

    private IEnumerator HitFlash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        sr.color = originalColor;
    }

    private void Die()
    {
        RunKills++;
        PickupSpawner.Instance?.SpawnDrops(transform.position, chipsDropped, xpDropped);
        gameObject.SetActive(false);
    }
}
