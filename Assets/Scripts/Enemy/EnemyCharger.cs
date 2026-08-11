using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class EnemyCharger : MonoBehaviour
{
    [Header("Hareket (balance 4_Dusmanlar - Tepsili Garson)")]
    [Tooltip("Normal kovalama hizi")]
    [SerializeField] private float chaseSpeed = 2.8f;

    [Tooltip("Atilis hizi (dash gibi)")]
    [SerializeField] private float chargeSpeed = 9f;

    [Header("Saldiri")]
    [Tooltip("Temas hasari = 2 (ham)")]
    [SerializeField] private float contactDamage = 2f;

    [Tooltip("Atilis cooldown araligi (balance: 2.5-3.5 sn)")]
    [SerializeField] private float chargeCooldownMin = 2.5f;
    [SerializeField] private float chargeCooldownMax = 3.5f;

    [Tooltip("Bu mesafedeyken atilis baslatabilir")]
    [SerializeField] private float chargeTriggerRange = 5f;

    [Tooltip("Telegraph suresi (kirmizi uyari - kacma penceresi)")]
    [SerializeField] private float telegraphTime = 0.5f;

    [Tooltip("Atilis suresi")]
    [SerializeField] private float chargeDuration = 0.45f;

    [Tooltip("Atilis sonrasi toparlanma (ceza penceresi)")]
    [SerializeField] private float recoverTime = 0.6f;

    private enum State { Chase, Telegraph, Charge, Recover }
    private State state = State.Chase;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform target;
    private float stateTimer;      // icinde bulunulan durumun kalan suresi
    private float cooldownTimer;   // yeni atilisa kalan sure
    private Vector2 chargeDir;     // telegraph'ta KILITLENEN yon
    private Color originalColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        originalColor = sr.color;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void OnEnable()
    {
        // Havuzdan yeniden dogunca temiz baslangic
        state = State.Chase;
        cooldownTimer = Random.Range(chargeCooldownMin, chargeCooldownMax);
        if (sr != null) sr.color = originalColor;
    }

    private void FixedUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        cooldownTimer -= Time.fixedDeltaTime;
        stateTimer -= Time.fixedDeltaTime;

        switch (state)
        {
            case State.Chase:
                Vector2 toPlayer = (Vector2)target.position - rb.position;
                rb.linearVelocity = toPlayer.normalized * chaseSpeed;
                sr.flipX = toPlayer.x < 0f;

                // Menzildeyiz + cooldown hazir -> TELEGRAPH baslat
                if (cooldownTimer <= 0f && toPlayer.magnitude <= chargeTriggerRange)
                {
                    state = State.Telegraph;
                    stateTimer = telegraphTime;
                    rb.linearVelocity = Vector2.zero;   // dur, nisan al
                    sr.color = Color.red;               // UYARI! (gorsel telegraph)
                }
                break;

            case State.Telegraph:
                rb.linearVelocity = Vector2.zero;
                if (stateTimer <= 0f)
                {
                    // Yonu SIMDI kilitle: bundan sonra oyuncu kacarsa iskalarsin
                    chargeDir = ((Vector2)target.position - rb.position).normalized;
                    state = State.Charge;
                    stateTimer = chargeDuration;
                }
                break;

            case State.Charge:
                rb.linearVelocity = chargeDir * chargeSpeed;   // kilitli yonde tam gaz
                if (stateTimer <= 0f)
                {
                    state = State.Recover;
                    stateTimer = recoverTime;
                    sr.color = originalColor;
                }
                break;

            case State.Recover:
                rb.linearVelocity = Vector2.zero;    // savunmasiz - ceza penceresi
                if (stateTimer <= 0f)
                {
                    state = State.Chase;
                    cooldownTimer = Random.Range(chargeCooldownMin, chargeCooldownMax);
                }
                break;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player != null)
            player.TakeDamage(contactDamage);
    }
}
