using System.Collections.Generic;
using UnityEngine;

public class WeaponBodyguards : WeaponBase
{
    [Header("Seviye tablosu (balance: Bodyguards Lv1-5)")]
    [SerializeField] private float[] damageByLevel = { 1f, 2f, 2.5f, 3f, 3.5f };
    [SerializeField] private int[] countByLevel = { 1, 2, 2, 3, 4 };

    [Header("Yorunge (balance 9. paket onerileri)")]
    [Tooltip("Cember yaricapi = 1.5")]
    [SerializeField] private float orbitRadius = 1.5f;

    [Tooltip("Donus hizi (tur/saniye) = 0.5")]
    [SerializeField] private float turnsPerSecond = 0.5f;

    [Header("Vurus")]
    [Tooltip("Ayni dusmana iki vurus arasi minimum sure = 0.5 sn (KRITIK)")]
    [SerializeField] private float hitCooldown = 0.5f;

    [Tooltip("Korumanin vurus mesafesi (koruma merkezinden)")]
    [SerializeField] private float hitRadius = 0.5f;

    [Header("Baglanti")]
    [Tooltip("Koruma gorseli prefab'i (kucuk kare - sonra bodyguard sprite'i)")]
    [SerializeField] private Transform guardPrefab;

    private float curDamage;
    private int curCount;
    private float angle;

    private readonly List<Transform> guards = new List<Transform>();
    private readonly Dictionary<EnemyHealth, float> nextHitTime = new();

    protected override void OnLevelChanged()
    {
        int i = Level - 1;
        curDamage = damageByLevel[i];
        curCount = countByLevel[i];
        RebuildGuards();
    }

    private void RebuildGuards()
    {
        while (guards.Count < curCount)
        {
            Transform g = Instantiate(guardPrefab, transform);
            guards.Add(g);
        }
        for (int i = 0; i < guards.Count; i++)
            guards[i].gameObject.SetActive(i < curCount);
    }

    private void Update()
    {
        if (stats == null) return;

        angle += 360f * turnsPerSecond * Time.deltaTime;
        if (angle >= 360f) angle -= 360f;

        for (int i = 0; i < curCount; i++)
        {
            float a = (angle + (360f / curCount) * i) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * orbitRadius;

            Transform g = guards[i];
            g.position = (Vector2)owner.position + offset;
            g.rotation = Quaternion.identity;

            CheckHits(g.position);
        }
    }

    private void CheckHits(Vector2 guardPos)
    {
        float hitSqr = hitRadius * hitRadius;

        // GUNCELLEME: Artik "AllHittables" listesine bakiyoruz ki Proplari da vurabilsin
        for (int i = EnemyHealth.AllHittables.Count - 1; i >= 0; i--)
        {
            EnemyHealth e = EnemyHealth.AllHittables[i];

            if (((Vector2)e.transform.position - guardPos).sqrMagnitude > hitSqr)
                continue;

            if (nextHitTime.TryGetValue(e, out float t) && Time.time < t)
                continue;

            float dmg = stats.RollMeleeDamage(curDamage, out bool isCrit);
            if (dmg > 0f)
                e.TakeDamage(dmg, isCrit);

            nextHitTime[e] = Time.time + hitCooldown;
        }
    }
}