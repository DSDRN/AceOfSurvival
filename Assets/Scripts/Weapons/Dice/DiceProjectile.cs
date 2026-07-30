using System.Collections;
using UnityEngine;

public class DiceProjectile : MonoBehaviour
{
    [Header("Ziplama ayarlari (his)")]
    [SerializeField] private float hopDuration = 0.35f;
    [SerializeField] private float hopDistanceMin = 0.8f;
    [SerializeField] private float hopDistanceMax = 1.4f;

    private SpriteRenderer sr;
    private PlayerStats stats;   // crit zari icin (WeaponDice teslim eder)

    // GC-dostu buffer: bir kez yaratilir, her AoE'de yeniden doldurulur
    private readonly Collider2D[] hitBuffer = new Collider2D[128];
    private ContactFilter2D noFilter;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        noFilter = ContactFilter2D.noFilter;
    }

    /// <summary>WeaponDice cagirir. playerStats: crit zari icin.</summary>
    public void Begin(Vector2 landPoint, int face, PlayerStats playerStats)
    {
        stats = playerStats;
        transform.position = landPoint;

        float size = 0.35f + face * 0.1f;
        transform.localScale = new Vector3(size, size, 1f);

        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(BounceRoutine(face));
    }

    private IEnumerator BounceRoutine(int face)
    {
        // KILIT formuller (GDD 4.3): taban degerler sayidan gelir
        float aoeRadius = face * 0.75f + 1.25f;
        float baseDamage = face * 0.75f + 0.75f;

        Vector2 pos = transform.position;

        for (int bounce = 0; bounce < face; bounce++)
        {
            Vector2 next = pos + Random.insideUnitCircle.normalized
                                 * Random.Range(hopDistanceMin, hopDistanceMax);

            float t = 0f;
            Vector3 baseScale = transform.localScale;
            while (t < 1f)
            {
                t += Time.deltaTime / hopDuration;
                float clamped = Mathf.Clamp01(t);

                transform.position = Vector2.Lerp(pos, next, clamped);
                float air = Mathf.Sin(clamped * Mathf.PI);
                transform.localScale = baseScale * (1f + 0.35f * air);
                transform.Rotate(0f, 0f, 360f * Time.deltaTime);

                yield return null;
            }
            transform.localScale = baseScale;
            pos = next;

            // --- YERE CARPTI: AoE hasar (GC-dostu) ---
            int count = Physics2D.OverlapCircle(pos, aoeRadius, noFilter, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                EnemyHealth enemy = hitBuffer[i].GetComponent<EnemyHealth>();
                if (enemy == null) continue;

                // Her dusman icin AYRI crit zari (sansli sekmeler!)
                float dmg = baseDamage;
                bool isCrit = false;
                if (stats != null)
                    dmg = stats.ApplyCritRoll(baseDamage, out isCrit);

                enemy.TakeDamage(dmg, isCrit);
            }
            // Ileride: carpma sesi + toz + mini screen shake
        }

        gameObject.SetActive(false);
    }
}
