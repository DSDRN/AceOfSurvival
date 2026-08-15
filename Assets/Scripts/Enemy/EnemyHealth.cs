using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public static readonly List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>();
    public static readonly List<EnemyHealth> AllHittables = new List<EnemyHealth>();
    public static int RunKills = 0;

    [Header("Prop Ayari")]
    [Tooltip("Isaretlenirse dusman sayilmaz, wave bitisinde silinmez")]
    [SerializeField] private bool isProp = false;

    [Tooltip("WaveManager'in bunu tank olarak gormesi icin isaretle (Fedai vb.)")]
    public bool isTank = false;

    [Header("Can (balance 4_Dusmanlar)")]
    [SerializeField] private float maxHealth = 10f;

    [Header("Knockback & Stagger")]
    [Range(0f, 1f)]
    [SerializeField] private float knockbackResistance = 0f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Drop")]
    [SerializeField] private float chipsDropped = 5f;
    [SerializeField] private float xpDropped = 3f;

    // YENI STAGGER SISTEMI
    public bool IsKnockedBack => knockTimer > 0f;
    public bool IsStaggered => staggerTimer > 0f; // AI'lar artik buna da bakacak
    public float HealthPercent => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    private float currentHealth;
    private float baseMaxHealth;

    [SerializeField] private float hpPerWave = 3f;

    private SpriteRenderer sr;
    private Color originalColor;
    private Rigidbody2D rb;
    private Vector2 knockVelocity;
    private float knockTimer;
    private float staggerTimer; // Stagger icin sayac

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        originalColor = sr.color;
        rb = GetComponent<Rigidbody2D>();

        baseMaxHealth = maxHealth;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        sr.color = originalColor;
        knockTimer = 0f;
        staggerTimer = 0f; // Sifirla

        AllHittables.Add(this);
        if (!isProp) ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        AllHittables.Remove(this);
        if (!isProp) ActiveEnemies.Remove(this);
    }

    public void InitScaling(int waveNumber)
    {
        int scaleCount = Mathf.Max(0, waveNumber - 1);
        maxHealth = baseMaxHealth + (hpPerWave * scaleCount);
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        // Stagger suresini azalt
        if (staggerTimer > 0f)
        {
            staggerTimer -= Time.fixedDeltaTime;
        }

        if (knockTimer > 0f)
        {
            knockTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockVelocity;
        }
        else if (IsStaggered)
        {
            // Stagger aninda knockback yoksa dusmani oldugu yere cak!
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ApplyKnockback(Vector2 dir, float force)
    {
        if (rb == null) return;
        float etki = force * (1f - knockbackResistance);
        if (etki <= 0.01f) return;
        knockVelocity = dir.normalized * etki;
        knockTimer = knockbackDuration;
    }

    // YENI STAGGER FONKSIYONU
    // Silahlar (Zar, Eldiven vb.) dusmana vurdugunda cagirilir
    public void ApplyStagger(float duration)
    {
        if (isProp) return;

        // Stagger direncini knockback direnciyle ayni kabul ediyoruz
        // Yani bosslar (direnc = 1) asla stagger yemez
        float gercekSure = duration * (1f - knockbackResistance);

        if (gercekSure > 0.05f)
        {
            staggerTimer = Mathf.Max(staggerTimer, gercekSure); // Ust uste binerse en uzunu sec
        }
    }

    public void TakeDamage(float damage, bool isCrit)
    {
        if (!gameObject.activeInHierarchy || currentHealth <= 0f) return;

        currentHealth -= damage;

        DamageNumberManager.Instance?.Show(transform.position, damage, isCrit,
            isCrit ? DamageNumberManager.CritRenk : DamageNumberManager.NormalRenk);

        if (isCrit) HitStop.Do(0.05f);

        StopAllCoroutines();
        StartCoroutine(HitFlash());

        if (currentHealth <= 0f) Die();
    }

    private IEnumerator HitFlash()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.07f);
        sr.color = originalColor;
    }

    private void Die()
    {
        if (!isProp) RunKills++;
        PickupSpawner.Instance?.SpawnDrops(transform.position, chipsDropped, xpDropped);
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isProp && collision.gameObject.CompareTag("Player"))
        {
            TakeDamage(5f, false);
        }
    }
}