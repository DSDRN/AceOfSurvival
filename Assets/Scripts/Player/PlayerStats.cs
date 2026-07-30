using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Menzilli (balance)")]
    [SerializeField] private float baseProjectileDamage = 4f;
    [SerializeField] private float baseProjectileSpeed = 13f;
    [SerializeField] private float baseProjectileRange = 11f;
    [SerializeField] private float projectileAttackSpeed = 1.25f;

    [Header("Yakin dovus (balance)")]
    [SerializeField] private float baseMeleeDamage = 2.5f;
    [SerializeField] private float meleeAttackSpeed = 1f;

    [Header("Taktiksel (balance v1.7)")]
    [Tooltip("Card Counting = 2 (Trap Card / turret hasar stati) - KILIT")]
    [SerializeField] private float cardCounting = 2f;

    [Header("Crit (balance)")]
    [SerializeField] private float critChance = 5f;
    [SerializeField] private float critMultiplier = 1.75f;

    [Header("Toplama (balance)")]
    [SerializeField] private float pickupRadius = 2.5f;

    // ---- House rule carpanlari ----
    private float attackSpeedBurst = 1f;
    private float permanentAtkSpeedMult = 1f;
    private float damageMultiplier = 1f;
    private float meleeReachMult = 1f;

    // ---- Okuma kapilari ----
    public float BaseProjectileDamage => baseProjectileDamage;
    public float BaseProjectileSpeed => baseProjectileSpeed;
    public float BaseProjectileRange => baseProjectileRange;
    public float ProjectileAttackSpeed => projectileAttackSpeed * attackSpeedBurst * permanentAtkSpeedMult;
    public float BaseMeleeDamage => baseMeleeDamage;
    public float MeleeAttackSpeed => meleeAttackSpeed * attackSpeedBurst * permanentAtkSpeedMult;
    public float CardCounting => cardCounting;      // YENI
    public float PickupRadius => pickupRadius;
    public float CritChance => critChance;
    public float CritMultiplier => critMultiplier;
    public float MeleeReachMult => meleeReachMult;

    // ---------------- HASAR PIPELINE ----------------
    public float RollProjectileDamage(float weaponDamage, out bool isCrit)
    {
        float total = (weaponDamage + baseProjectileDamage) * damageMultiplier;
        isCrit = Random.Range(0f, 100f) < critChance;
        if (isCrit) total *= critMultiplier;
        return Mathf.Max(1f, total);
    }

    public float RollMeleeDamage(float weaponDamage, out bool isCrit)
    {
        float total = (weaponDamage + baseMeleeDamage) * damageMultiplier;
        isCrit = false;
        if (total <= 0f) return 0f;
        isCrit = Random.Range(0f, 100f) < critChance;
        if (isCrit) total *= critMultiplier;
        return Mathf.Max(1f, total);
    }

    /// Zar + Trap: hazir taban hasari carpan + bireysel crit zarindan gecirir
    public float ApplyCritRoll(float baseDamage, out bool isCrit)
    {
        float total = baseDamage * damageMultiplier;
        isCrit = Random.Range(0f, 100f) < critChance;
        return isCrit ? total * critMultiplier : total;
    }

    // ---------------- DEGISTIRME KAPILARI ----------------
    public void AddStat(StatType type, float amount)
    {
        switch (type)
        {
            case StatType.ProjectileDamage: baseProjectileDamage += amount; break;
            case StatType.MeleeDamage: baseMeleeDamage += amount; break;
            case StatType.ProjectileSpeed: baseProjectileSpeed += amount; break;
            case StatType.ProjectileRange: baseProjectileRange += amount; break;
            case StatType.ProjectileAttackSpeed: projectileAttackSpeed += amount; break;
            case StatType.MeleeAttackSpeed: meleeAttackSpeed += amount; break;
            case StatType.CardCounting: cardCounting += amount; break;   // YENI
        }
    }

    public void AddCritChance(float amount) => critChance = Mathf.Max(0f, critChance + amount);
    public void AddCritDamage(float amount) => critMultiplier = Mathf.Max(1f, critMultiplier + amount);
    public void SetAttackSpeedBurst(float mult) => attackSpeedBurst = Mathf.Max(0.1f, mult);
    public void AddPickupRadius(float amount) => pickupRadius = Mathf.Max(0.1f, pickupRadius + amount);
    public void MultiplyPermanentAtkSpeed(float f) => permanentAtkSpeedMult = Mathf.Max(0.1f, permanentAtkSpeedMult * f);
    public void MultiplyDamage(float f) => damageMultiplier = Mathf.Max(0.1f, damageMultiplier * f);
    public void MultiplyMeleeReach(float f) => meleeReachMult = Mathf.Max(0.1f, meleeReachMult * f);
}
