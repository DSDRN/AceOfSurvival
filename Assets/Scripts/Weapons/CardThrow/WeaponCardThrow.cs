using System.Collections.Generic;
using UnityEngine;

public class WeaponCardThrow : WeaponBase
{
    [Header("Seviye tablosu (balance: Kart Destesi Lv1-5)")]
    [SerializeField] private float[] damageByLevel = { 2f, 2.5f, 3.25f, 5f, 6.5f };
    [SerializeField] private float[] cooldownByLevel = { 2f, 2f, 2f, 2f, 1.9f };
    [SerializeField] private int[] cardsByLevel = { 1, 2, 2, 3, 3 };
    [SerializeField] private float[] rangeByLevel = { 5f, 5f, 6.5f, 7f, 7.3f };

    [Header("Geri Tepme (Knockback)")]
    [Tooltip("Kartin dusmana carptiginda uygulayacagi itme kuvveti")]
    [SerializeField] private float knockbackForce = 2.5f;

    [Header("Diger")]
    [Tooltip("Coklu kartta yelpaze acisi (derece)")]
    [SerializeField] private float spreadAngle = 10f;

    [Tooltip("Kart prefab'i")]
    [SerializeField] private ProjectileCard cardPrefab;

    [Tooltip("Range statinin referans tabani (balance: Base Projectile Range = 11)")]
    [SerializeField] private float rangeStatBaseline = 11f;

    // Gecerli seviyenin degerleri (OnLevelChanged doldurur)
    private float curDamage, curCooldown, curRange;
    private int curCards;

    private float cooldownTimer;
    private readonly List<ProjectileCard> pool = new List<ProjectileCard>();

    protected override void OnLevelChanged()
    {
        int i = Level - 1;   // dizi 0'dan baslar, seviye 1'den
        curDamage = damageByLevel[i];
        curCooldown = cooldownByLevel[i];
        curCards = cardsByLevel[i];
        curRange = rangeByLevel[i];
    }

    private void Update()
    {
        if (stats == null) return;   // Init edilmeden calisma (guvenlik)

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (TryFire())
            cooldownTimer = curCooldown / stats.ProjectileAttackSpeed;
        else
            cooldownTimer = 0.1f;
    }

    /// Silahin etkin menzili: seviye menzili + oyuncu statindaki fark.
    /// (Oyuncu range statini 11 -> 12 yaptiysa tum kart seviyeleri +1 kazanir)
    private float EffectiveRange()
    {
        return Mathf.Max(1f, curRange + (stats.BaseProjectileRange - rangeStatBaseline));
    }

    private bool TryFire()
    {
        float range = EffectiveRange();
        EnemyHealth target = FindNearestEnemy(range);
        if (target == null) return false;

        Vector2 baseDir = ((Vector2)target.transform.position - (Vector2)owner.position).normalized;

        for (int i = 0; i < curCards; i++)
        {
            float offset = (i - (curCards - 1) / 2f) * spreadAngle;
            Vector2 dir = Quaternion.Euler(0f, 0f, offset) * baseDir;

            float dmg = stats.RollProjectileDamage(curDamage, out bool isCrit);

            // YENI EKLENDI: knockbackForce degerini de mermiye gonderiyoruz
            GetCardFromPool().Launch(owner.position, dir,
                                     stats.BaseProjectileSpeed, range, dmg, isCrit, knockbackForce);
        }
        return true;
    }

    private EnemyHealth FindNearestEnemy(float maxRange)
    {
        EnemyHealth nearest = null;
        float nearestSqr = maxRange * maxRange;

        foreach (EnemyHealth e in EnemyHealth.ActiveEnemies)
        {
            float sqr = ((Vector2)e.transform.position - (Vector2)owner.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = e;
            }
        }
        return nearest;
    }

    private ProjectileCard GetCardFromPool()
    {
        foreach (ProjectileCard c in pool)
        {
            if (!c.gameObject.activeInHierarchy)
                return c;
        }
        ProjectileCard yeni = Instantiate(cardPrefab);
        yeni.gameObject.SetActive(false);
        pool.Add(yeni);
        return yeni;
    }
}