using System.Collections.Generic;
using UnityEngine;

/*
 * EnemyRanged.cs
 * ==============
 * BU DOSYA NE YAPAR?
 * Menzilli dusman davranisi - Hilebaz Krupiye (balance ID 4, Brotato: Spitter):
 *   - Oyuncu COK YAKLASIRSA kacar (fleeDistance)
 *   - Guvenli mesafedeyse durur ve belirli araliklarla oyuncuya kart firlatir
 *   - Mermiler TUM ranged dusmanlarin PAYLASTIGI ortak havuzdan gelir
 *     (static pool: 10 krupiye ayni mermi stogunu kullanir - israf yok)
 *
 * Fedai gibi tank icin YENI SCRIPT GEREKMEZ: o EnemyChaser'in farkli
 * sayilarla prefab'i. Bu script sadece FARKLI DAVRANIS gerektigi icin var.
 */
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRanged : MonoBehaviour
{
    [Header("Hareket (balance 4_Dusmanlar)")]
    [Tooltip("Krupiye onerisi: 2.5 (Excel 200 / ~80)")]
    [SerializeField] private float moveSpeed = 2.5f;

    [Tooltip("Oyuncu bundan yakinsa KAC")]
    [SerializeField] private float fleeDistance = 4f;

    [Header("Saldiri")]
    [Tooltip("Krupiye hasari = 3 (ham; zirh formulu oyuncuda islenir)")]
    [SerializeField] private float projectileDamage = 3f;

    [Tooltip("Atis araligi (sn)")]
    [SerializeField] private float fireInterval = 2f;

    [Tooltip("Bu mesafedeyken ates edebilir")]
    [SerializeField] private float fireRange = 9f;

    [Tooltip("Mermi ucus hizi (oyuncu mermisinden yavas olsun ki kacilabilsin)")]
    [SerializeField] private float projectileSpeed = 6f;

    [Tooltip("Mermi menzili")]
    [SerializeField] private float projectileRange = 10f;

    [Header("Baglanti")]
    [SerializeField] private EnemyProjectile projectilePrefab;

    // ---- ORTAK mermi havuzu: static = tum EnemyRanged'ler paylasir ----
    private static readonly Dictionary<EnemyProjectile, List<EnemyProjectile>> sharedPools = new();

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform target;
    private float fireTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void OnEnable()
    {
        fireTimer = fireInterval * Random.Range(0.5f, 1f);   // hepsi ayni anda atmasin
    }

    private void FixedUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)target.position - rb.position;
        float dist = toPlayer.magnitude;

        // Cok yakinsa KAC, degilse DUR (guvenli mesafeden ates)
        if (dist < fleeDistance)
            rb.linearVelocity = -toPlayer.normalized * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;

        if (sr != null)
            sr.flipX = toPlayer.x < 0f;
    }

    private void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy) return;

        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        float dist = Vector2.Distance(transform.position, target.position);
        if (dist <= fireRange)
        {
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            GetFromSharedPool(projectilePrefab)
                .Launch(transform.position, dir, projectileSpeed, projectileRange, projectileDamage);
            fireTimer = fireInterval;
        }
        else
        {
            fireTimer = 0.3f;   // menzil disinda - kisa sure sonra tekrar bak
        }
    }

    private static EnemyProjectile GetFromSharedPool(EnemyProjectile prefab)
    {
        if (!sharedPools.TryGetValue(prefab, out List<EnemyProjectile> pool))
        {
            pool = new List<EnemyProjectile>();
            sharedPools[prefab] = pool;
        }

        foreach (EnemyProjectile p in pool)
        {
            if (!p.gameObject.activeInHierarchy)
                return p;
        }

        EnemyProjectile yeni = Instantiate(prefab);
        yeni.gameObject.SetActive(false);
        pool.Add(yeni);
        return yeni;
    }
}
