using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // Sadece gercek dusmanlar (Wave sonu silinmek ve RunKills icin kullanilir)
    public static readonly List<EnemyHealth> ActiveEnemies = new List<EnemyHealth>();

    // YENİ: Hem dusmanlar hem proplar. Otomatik nisan alan silahlar (Zar vb.) BURAYA bakar.
    public static readonly List<EnemyHealth> AllHittables = new List<EnemyHealth>();

    public static int RunKills = 0;

    [Header("Prop Ayari")]
    [Tooltip("Isaretlenirse dusman sayilmaz, wave bitisinde silinmez")]
    [SerializeField] private bool isProp = false;

    [Header("Can (balance 4_Dusmanlar)")]
    [SerializeField] private float maxHealth = 10f;

    [Header("Knockback (balance 4_Dusmanlar 'Knockback direnci' kolonu)")]
    [Range(0f, 1f)]
    [SerializeField] private float knockbackResistance = 0f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Drop (balance 4_Dusmanlar)")]
    [SerializeField] private float chipsDropped = 5f;
    [SerializeField] private float xpDropped = 3f;

    public bool IsKnockedBack => knockTimer > 0f;
    public float HealthPercent => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
    public float MaxHealth => maxHealth;

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
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
        sr.color = originalColor;
        knockTimer = 0f;

        AllHittables.Add(this); // YENI: Proplar ve dusmanlar vurulabilir listesine eklenir

        if (!isProp) ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        AllHittables.Remove(this);
        if (!isProp) ActiveEnemies.Remove(this);
    }

    private void FixedUpdate()
    {
        if (knockTimer > 0f)
        {
            knockTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockVelocity;
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

    public void TakeDamage(float damage, bool isCrit)
    {
        currentHealth -= damage;
        DamageNumberManager.Instance?.Show(transform.position, damage, isCrit,
            isCrit ? DamageNumberManager.CritRenk : DamageNumberManager.NormalRenk);
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
        if (!isProp) RunKills++; // Sadece gercek dusmanlar kill sayilir
        PickupSpawner.Instance?.SpawnDrops(transform.position, chipsDropped, xpDropped);
        gameObject.SetActive(false);
    }

    // YENİ: Oyuncu (karakter) prop'a fiziksel olarak çarparsa 5 hasar vurur
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isProp && collision.gameObject.CompareTag("Player"))
        {
            TakeDamage(5f, false);
        }
    }
}