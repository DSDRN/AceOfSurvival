using System.Collections;
using UnityEngine;

public class WeaponChain : WeaponBase
{
    [Header("Seviye tablosu (balance: Zincir Lv1-5)")]
    [SerializeField] private float[] damageByLevel = { 3f, 4f, 5f, 6f, 7.25f };
    [SerializeField] private float[] cooldownByLevel = { 1.7f, 1.7f, 1.7f, 1.7f, 1.65f };

    // MENZIL ARTTIRILDI (Eski: 2.0 -> 3.5 | Yeni: 2.5 -> 4.2)
    [SerializeField] private float[] reachByLevel = { 2.5f, 3.0f, 3.5f, 3.8f, 4.2f };

    [Header("Geri Tepme (Knockback)")]
    [Tooltip("Vurus aninda dusmani geriye itme kuvveti")]
    [SerializeField] private float knockbackForce = 4.5f;

    [Tooltip("Bu seviyeden itibaren ayni anda ust uste 2 vurus yapar")]
    [SerializeField] private int doubleHitFromLevel = 5;
    [Tooltip("Bu seviyeden itibaren arkaya artci vurus acar")]
    [SerializeField] private int backStrikeFromLevel = 3;

    [Header("Vurus kutusu")]
    [SerializeField] private float hitHeight = 1.6f;

    [Header("Gorsel")]
    [Tooltip("Prefab'in child'i olan yariseffaf kutu (SpriteRenderer)")]
    [SerializeField] private SpriteRenderer slashVisual;

    // Gecerli seviye degerleri
    private float curDamage, curCooldown, curReach;
    private int curHits;
    private bool curBackStrike;

    private float cooldownTimer;

    // ---- GC-dostu fizik: buffer BIR KEZ yaratilir ----
    private readonly Collider2D[] hitBuffer = new Collider2D[64];
    private ContactFilter2D noFilter;

    private void Awake()
    {
        noFilter = ContactFilter2D.noFilter;
        if (slashVisual != null)
            slashVisual.enabled = false;
    }

    protected override void OnLevelChanged()
    {
        int i = Mathf.Clamp(Level - 1, 0, 4);

        curDamage = damageByLevel[i];
        curCooldown = cooldownByLevel[i];
        curReach = reachByLevel[i];

        curHits = Level >= doubleHitFromLevel ? 2 : 1;
        curBackStrike = Level >= backStrikeFromLevel;
    }

    private void Update()
    {
        if (stats == null || movement == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        StartCoroutine(PerformChainCombo());

        cooldownTimer = curCooldown / stats.MeleeAttackSpeed;
    }

    private IEnumerator PerformChainCombo()
    {
        // 1. ILK VURUS (Lv 1-2 Arka | Lv 3+ Ön)
        Vector2 firstDir = (Level < 3) ? -movement.FacingDir : movement.FacingDir;
        Strike(firstDir, curHits);

        // 2. IKINCI VURUS (Lv 3+ Arka)
        if (curBackStrike)
        {
            yield return new WaitForSeconds(0.3f);
            Vector2 backDir = -movement.FacingDir;
            Strike(backDir, 1);
        }
    }

    private void Strike(Vector2 dir, int hitCount)
    {
        Vector2 center = (Vector2)owner.position + dir * (curReach / 2f);
        Vector2 boxSize = new Vector2(curReach, hitHeight);

        int count = Physics2D.OverlapBox(center, boxSize, 0f, noFilter, hitBuffer);

        for (int i = 0; i < count; i++)
        {
            EnemyHealth enemy = hitBuffer[i].GetComponent<EnemyHealth>();
            if (enemy == null) continue;

            for (int h = 0; h < hitCount; h++)
            {
                float dmg = stats.RollMeleeDamage(curDamage, out bool isCrit);
                if (dmg > 0f)
                {
                    enemy.TakeDamage(dmg, isCrit);

                    // --- KNOCKBACK (GERI TEPME) ---
                    // Dusmanda Rigidbody2D varsa vurulan yone dogru itilir
                    if (hitBuffer[i].TryGetComponent<Rigidbody2D>(out Rigidbody2D enemyRb))
                    {
                        enemyRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                    }
                }
            }
        }

        if (slashVisual != null)
            StartCoroutine(FlashSlash(dir));
    }

    private IEnumerator FlashSlash(Vector2 dir)
    {
        slashVisual.transform.localPosition = dir * (curReach / 2f);
        slashVisual.transform.localScale = new Vector3(curReach, hitHeight, 1f);

        slashVisual.flipX = dir.x < 0;

        slashVisual.enabled = true;
        yield return new WaitForSeconds(0.08f);
        slashVisual.enabled = false;
    }
}