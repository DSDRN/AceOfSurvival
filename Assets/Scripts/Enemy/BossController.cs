using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
public class BossController : MonoBehaviour
{
    [Header("Hareket (balance 9_Boss)")]
    [SerializeField] private float moveSpeed = 3.6f;

    [Header("Temas")]
    [SerializeField] private float contactDamage = 12f;

    [Header("Saldiri dongusu")]
    [SerializeField] private float attackInterval = 4f;

    [Header("Saldiri 1: Kart Yelpazesi")]
    [SerializeField] private int fanCardCount = 7;
    [SerializeField] private float fanArc = 60f;
    [SerializeField] private float fanCardDamage = 3f;
    [SerializeField] private float fanCardSpeed = 5.5f;
    [SerializeField] private float fanCardRange = 12f;
    [SerializeField] private EnemyProjectile cardPrefab;

    [Header("Saldiri 2: Chip Yagmuru")]
    [SerializeField] private float slamTelegraph = 1f;
    [SerializeField] private float slamRadius = 1.6f;
    [SerializeField] private float slamDamage = 8f;
    [SerializeField] private Transform[] slamMarkers;

    [Header("Enrage (%50 can alti)")]
    [SerializeField] private float enrageIntervalMultiplier = 0.6f;
    [SerializeField] private float enrageSpeedMultiplier = 1.25f;
    [SerializeField] private int enrageFanCardCount = 9;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private EnemyHealth healthComp;
    private Transform target;
    private bool enraged;
    private readonly List<EnemyProjectile> cardPool = new List<EnemyProjectile>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        healthComp = GetComponent<EnemyHealth>();
        foreach (Transform m in slamMarkers)
            if (m != null) m.gameObject.SetActive(false);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) target = p.transform;
        StartCoroutine(AttackLoop());
    }

    private void FixedUpdate()
    {
        // Eger Boss bir sekilde Staggerlanirsa (Direnc 1 degilse) hareketini durdur.
        if (healthComp != null && healthComp.IsStaggered) return;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!enraged && healthComp.HealthPercent <= 0.5f)
        {
            enraged = true;
            moveSpeed *= enrageSpeedMultiplier;
            fanCardCount = enrageFanCardCount;
            if (sr != null) sr.color = new Color(1f, 0.6f, 0.6f);
            Debug.Log("THE DEALER OFKELENDI!");
        }

        Vector2 toPlayer = (Vector2)target.position - rb.position;
        rb.linearVelocity = toPlayer.normalized * moveSpeed;
        if (sr != null) sr.flipX = toPlayer.x < 0f;
    }

    private IEnumerator AttackLoop()
    {
        bool fanNext = true;
        while (true)
        {
            float bekle = enraged ? attackInterval * enrageIntervalMultiplier : attackInterval;
            yield return new WaitForSeconds(bekle);

            // Stagger sirasinda saldiri gerceklestirme, bekle
            if (healthComp != null && healthComp.IsStaggered) continue;
            if (target == null || !target.gameObject.activeInHierarchy) continue;

            if (fanNext) CardFan();
            else yield return ChipRain();

            fanNext = !fanNext;
        }
    }

    private void CardFan()
    {
        Vector2 baseDir = ((Vector2)target.position - (Vector2)transform.position).normalized;

        for (int i = 0; i < fanCardCount; i++)
        {
            float offset = fanCardCount > 1
                ? Mathf.Lerp(-fanArc / 2f, fanArc / 2f, (float)i / (fanCardCount - 1))
                : 0f;
            Vector2 dir = Quaternion.Euler(0f, 0f, offset) * baseDir;

            GetCard().Launch(transform.position, dir, fanCardSpeed, fanCardRange, fanCardDamage);
        }
    }

    private IEnumerator ChipRain()
    {
        if (target == null) yield break;

        Vector2[] noktalar = new Vector2[slamMarkers.Length];
        for (int i = 0; i < slamMarkers.Length; i++)
        {
            if (slamMarkers[i] == null) continue;
            noktalar[i] = (Vector2)target.position +
                          (i == 0 ? Vector2.zero : Random.insideUnitCircle * 2.5f);
            slamMarkers[i].position = noktalar[i];
            slamMarkers[i].localScale = Vector3.one * (slamRadius * 2f);
            slamMarkers[i].gameObject.SetActive(true);

            foreach (Vector2 nokta in noktalar)
            {
                if (target != null && target.gameObject.activeInHierarchy &&
                    Vector2.Distance(target.position, nokta) <= slamRadius)
                {
                    target.GetComponent<PlayerHealth>()?.TakeDamage(slamDamage, healthComp, "The Dealer (Chip Yagmuru)");
                }
            }
        }

        yield return new WaitForSeconds(slamTelegraph);

        foreach (Vector2 nokta in noktalar)
        {
            if (target != null && target.gameObject.activeInHierarchy &&
                Vector2.Distance(target.position, nokta) <= slamRadius)
            {
                target.GetComponent<PlayerHealth>()?.TakeDamage(slamDamage);
            }
        }

        foreach (Transform m in slamMarkers)
            if (m != null) m.gameObject.SetActive(false);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player != null)
            player.TakeDamage(contactDamage, healthComp, "The Dealer (Temas)");
    }

    private EnemyProjectile GetCard()
    {
        foreach (EnemyProjectile c in cardPool)
            if (!c.gameObject.activeInHierarchy) return c;
        EnemyProjectile yeni = Instantiate(cardPrefab);
        yeni.gameObject.SetActive(false);
        cardPool.Add(yeni);
        return yeni;
    }

    private void OnGUI()
    {
        if (!gameObject.activeInHierarchy) return;
        float w = 400f, h = 18f;
        float x = (Screen.width - w) / 2f, y = 60f;
        GUI.Box(new Rect(x - 2, y - 2, w + 4, h + 4), "");
        GUI.color = Color.red;
        GUI.DrawTexture(new Rect(x, y, w * healthComp.HealthPercent, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y - 22, w, 20), "THE DEALER");
    }
}