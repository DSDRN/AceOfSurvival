using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))] // Stagger okumak icin zorunlu kildik
public class EnemyCharger : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float chaseSpeed = 2.8f;
    [SerializeField] private float chargeSpeed = 9f;

    [Header("Saldiri")]
    [SerializeField] private float contactDamage = 2f;
    [SerializeField] private float chargeCooldownMin = 2.5f;
    [SerializeField] private float chargeCooldownMax = 3.5f;
    [SerializeField] private float chargeTriggerRange = 5f;
    [SerializeField] private float telegraphTime = 0.5f;
    [SerializeField] private float chargeDuration = 0.45f;
    [SerializeField] private float recoverTime = 0.6f;

    private enum State { Chase, Telegraph, Charge, Recover }
    private State state = State.Chase;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private EnemyHealth eh; // YENI: Stagger okumak icin
    private Transform target;
    private float stateTimer;
    private float cooldownTimer;
    private Vector2 chargeDir;
    private Color originalColor;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        eh = GetComponent<EnemyHealth>();
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
        state = State.Chase;
        cooldownTimer = Random.Range(chargeCooldownMin, chargeCooldownMax);
        if (sr != null) sr.color = originalColor;
    }

    private void FixedUpdate()
    {
        // STAGGER KONTROLU: Dusman dondurulduysa saldiriyi iptal et veya bekle.
        if (eh != null && eh.IsStaggered)
        {
            // Eger charge halindeyken stagger yerse saldiri bozulur
            if (state == State.Charge)
            {
                state = State.Recover;
                stateTimer = recoverTime;
                sr.color = originalColor;
            }
            return;
        }

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

                if (cooldownTimer <= 0f && toPlayer.magnitude <= chargeTriggerRange)
                {
                    state = State.Telegraph;
                    stateTimer = telegraphTime;
                    rb.linearVelocity = Vector2.zero;
                    sr.color = Color.red;
                }
                break;

            case State.Telegraph:
                rb.linearVelocity = Vector2.zero;
                if (stateTimer <= 0f)
                {
                    chargeDir = ((Vector2)target.position - rb.position).normalized;
                    state = State.Charge;
                    stateTimer = chargeDuration;
                }
                break;

            case State.Charge:
                rb.linearVelocity = chargeDir * chargeSpeed;
                if (stateTimer <= 0f)
                {
                    state = State.Recover;
                    stateTimer = recoverTime;
                    sr.color = originalColor;
                }
                break;

            case State.Recover:
                rb.linearVelocity = Vector2.zero;
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
            player.TakeDamage(contactDamage, eh, gameObject.name);
    }
}