using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))] // Stagger okumak icin zorunlu kildik
public class EnemyChaser : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Saldiri")]
    [SerializeField] private float contactDamage = 1f;
    [SerializeField] private float dmgPerWave = 0.6f;

    private float baseContactDamage;
    private Rigidbody2D rb;
    private EnemyHealth eh; // YENI: Stagger kontrolcusu
    private Transform target;
    private SpriteRenderer sr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        eh = GetComponent<EnemyHealth>();
        baseContactDamage = contactDamage;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    public void InitScaling(int waveNumber)
    {
        int scaleCount = Mathf.Max(0, waveNumber - 1);
        contactDamage = baseContactDamage + (dmgPerWave * scaleCount);
    }

    private void FixedUpdate()
    {
        // STAGGER KONTROLU: Eger donduysa yurumeyi kes
        if (eh != null && eh.IsStaggered) return;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        if (sr != null)
            sr.flipX = direction.x < 0f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player != null)
            // YENI: Kendi EnemyHealth bilesenini ve ismini (gameObject.name) bildiriyor!
            player.TakeDamage(contactDamage, eh, gameObject.name);
    }
}