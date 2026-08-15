using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))] // Stagger icin eklendi
public class EnemyRanged : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float fleeDistance = 4f;

    [Header("Saldiri")]
    [SerializeField] private float projectileDamage = 3f;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private float fireRange = 9f;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileRange = 10f;

    [Header("Baglanti")]
    [SerializeField] private EnemyProjectile projectilePrefab;

    private static readonly Dictionary<EnemyProjectile, List<EnemyProjectile>> sharedPools = new();

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private EnemyHealth eh; // Stagger icin
    private Transform target;
    private float fireTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        eh = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void OnEnable()
    {
        fireTimer = fireInterval * Random.Range(0.5f, 1f);
    }

    private void FixedUpdate()
    {
        // STAGGER: Sersemlediyse kacmayi veya nisan almayi birak.
        if (eh != null && eh.IsStaggered) return;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)target.position - rb.position;
        float dist = toPlayer.magnitude;

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

        // STAGGER HALINDE ATESTE EDEMEZ
        if (eh != null && eh.IsStaggered)
        {
            fireTimer += Time.deltaTime; // Sureyi geri sar ki saldirisi iptal olsun
            return;
        }

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
            fireTimer = 0.3f;
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