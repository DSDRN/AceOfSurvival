using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyFlee : MonoBehaviour
{
    [SerializeField] private float fleeSpeed = 4.4f;
    [SerializeField] private float escapeDistance = 16f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private EnemyHealth healthComp;
    private Transform target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        healthComp = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.transform;
    }

    private void FixedUpdate()
    {
        // Stagger yemis bir hirsiz kacamaz, oldugu yerde kilitlenir!
        if (healthComp != null && (healthComp.IsKnockedBack || healthComp.IsStaggered)) return;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 away = rb.position - (Vector2)target.position;

        if (away.magnitude > escapeDistance)
        {
            Debug.Log("Chip Hirsizi kacmayi basardi - loot gitti!");
            gameObject.SetActive(false);
            return;
        }

        rb.linearVelocity = away.normalized * fleeSpeed;
        if (sr != null) sr.flipX = away.x < 0f;
    }
}