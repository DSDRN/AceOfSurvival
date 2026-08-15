using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceProjectile : MonoBehaviour
{
    [Header("Ziplama ayarlari (his)")]
    [SerializeField] private float hopDuration = 0.35f;
    [SerializeField] private float hopDistanceMin = 0.8f;
    [SerializeField] private float hopDistanceMax = 1.4f;

    private SpriteRenderer sr;
    private PlayerStats stats;
    private readonly Collider2D[] hitBuffer = new Collider2D[128];
    private ContactFilter2D noFilter;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        noFilter = ContactFilter2D.noFilter;
    }

    public void Begin(Vector2 landPoint, int face, PlayerStats playerStats, bool isNat20 = false)
    {
        stats = playerStats;
        transform.position = landPoint;

        float size = isNat20 ? 1.5f : (0.35f + face * 0.1f);
        transform.localScale = new Vector3(size, size, 1f);

        gameObject.SetActive(true);
        StopAllCoroutines();

        if (isNat20)
            StartCoroutine(Nat20Routine());
        else
            StartCoroutine(BounceRoutine(face));
    }

    private IEnumerator Nat20Routine()
    {
        yield return new WaitForSeconds(0.5f);

        DamageNumberManager.Instance?.Show(transform.position, 20, true, Color.yellow);
        Debug.Log("NATURAL 20!!! HARITA TEMIZLENIYOR!");

        // HATA COZUMU: Orijinal listeyi bozmamak icin anlik bir kopya olusturuyoruz!
        List<EnemyHealth> kopyalananListe = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);

        foreach (EnemyHealth activeEnemy in kopyalananListe)
        {
            if (activeEnemy == null || !activeEnemy.gameObject.activeInHierarchy) continue;

            if (activeEnemy.CompareTag("Boss"))
            {
                activeEnemy.TakeDamage(activeEnemy.MaxHealth * 0.5f, true);
            }
            else
            {
                // Tek atma islemi
                activeEnemy.TakeDamage(activeEnemy.MaxHealth * 2f, true);
            }
        }

        gameObject.SetActive(false);
    }

    private IEnumerator BounceRoutine(int face)
    {
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

            int count = Physics2D.OverlapCircle(pos, aoeRadius, noFilter, hitBuffer);
            for (int i = 0; i < count; i++)
            {
                EnemyHealth enemy = hitBuffer[i].GetComponent<EnemyHealth>();
                if (enemy == null) continue;

                float dmg = baseDamage;
                bool isCrit = false;
                if (stats != null)
                    dmg = stats.ApplyCritRoll(baseDamage, out isCrit);

                enemy.TakeDamage(dmg, isCrit);

                // YENI: Zar patlamasi agir hasardir, 0.25 sn dondurur!
                enemy.ApplyStagger(0.25f);
            }
        }

        gameObject.SetActive(false);
    }
}