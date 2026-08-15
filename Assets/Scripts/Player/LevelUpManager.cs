using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum StatType
{
    ProjectileDamage, MeleeDamage, Health, Armor, MoveSpeed,
    ProjectileSpeed, ProjectileRange, ProjectileAttackSpeed, MeleeAttackSpeed,
    CardCounting   // YENI (v1.3): Tactical silah hasari
}

public class LevelUpManager : MonoBehaviour
{
    [Header("Baglantilar (Player'dan surukle)")]
    [SerializeField] private PlayerResources resources;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerMovement movement;

    [Tooltip("GDD: Stat havuzunu eldeki silahlara gore filtrelemek icin eklendi")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Kademe agirliklari (KILIT: %80/%15/%5)")]
    [SerializeField] private float tier1Weight = 80f;
    [SerializeField] private float tier2Weight = 15f;
    [SerializeField] private float tier3Weight = 5f;

    [Header("Stat basina birim artis (balance 1_Karakter)")]
    [SerializeField] private float incProjectileDamage = 1f;
    [SerializeField] private float incMeleeDamage = 0.75f;
    [SerializeField] private float incHealth = 3f;
    [SerializeField] private float incArmor = 2f;
    [SerializeField] private float incMoveSpeed = 0.2f;
    [SerializeField] private float incProjectileSpeed = 0.25f;
    [SerializeField] private float incProjectileRange = 0.25f;
    [SerializeField] private float incProjectileAttackSpeed = 0.15f;
    [SerializeField] private float incMeleeAttackSpeed = 0.15f;
    [Tooltip("YENI: Card Counting = 0.5 (KILIT)")]
    [SerializeField] private float incCardCounting = 0.5f;

    public bool IsOpen { get; private set; }

    private struct Option { public StatType stat; public int tier; }
    private readonly Option[] options = new Option[3];
    private int clickedIndex = -1;

    public IEnumerator RunSelection()
    {
        if (resources == null || resources.PendingLevelUps <= 0)
            yield break;

        IsOpen = true;
        Time.timeScale = 0f;

        while (resources.PendingLevelUps > 0)
        {
            RollOptions();
            clickedIndex = -1;
            while (clickedIndex < 0)
                yield return null;

            Apply(options[clickedIndex]);
            resources.ConsumeLevelUp();
        }

        Time.timeScale = 1f;
        IsOpen = false;
    }

    private void RollOptions()
    {
        // 1. ADIM: Karakterin elindeki silah kategorilerini bul (Akilli Filtreleme Icin)
        bool hasRanged = false;
        bool hasMelee = false;

        if (weaponManager != null)
        {
            foreach (WeaponBase w in weaponManager.Equipped)
            {
                if (w.Category == WeaponCategory.Ranged) hasRanged = true;
                if (w.Category == WeaponCategory.Melee) hasMelee = true;
            }
        }
        else
        {
            // Guvenlik agi: Eger WeaponManager baglanmamissa her seyi acik farzet
            hasRanged = true; hasMelee = true;
        }

        // 2. ADIM: Let Me Solo Her kontrolu (Kilitli kategori varsa havuzu daralt)
        if (weaponManager != null && weaponManager.LockedCategory.HasValue)
        {
            if (weaponManager.LockedCategory.Value == WeaponCategory.Ranged) hasRanged = false;
            if (weaponManager.LockedCategory.Value == WeaponCategory.Melee) hasMelee = false;
        }

        // 3. ADIM: Tüm statlari havuza ekle, kullanilmayanlari sil (Olu Stat Filtresi)
        List<StatType> pool = new List<StatType>((StatType[])System.Enum.GetValues(typeof(StatType)));

        // Ranged silah yoksa ranged statlarini cikar
        if (!hasRanged)
        {
            pool.Remove(StatType.ProjectileDamage);
            pool.Remove(StatType.ProjectileSpeed);
            pool.Remove(StatType.ProjectileRange);
            pool.Remove(StatType.ProjectileAttackSpeed);
        }

        // Melee silah yoksa melee statlarini cikar
        if (!hasMelee)
        {
            pool.Remove(StatType.MeleeDamage);
            pool.Remove(StatType.MeleeAttackSpeed);
        }

        // Notr Statlar ve Card Counting her zaman kalir.

        // 4. ADIM: Secenekleri Belirle
        // GDD Kurali: En az 1 stat kesinlikle build ile alakali olmali!
        List<StatType> relatedStats = new List<StatType>();
        if (hasRanged) relatedStats.AddRange(new[] { StatType.ProjectileDamage, StatType.ProjectileSpeed, StatType.ProjectileRange, StatType.ProjectileAttackSpeed });
        if (hasMelee) relatedStats.AddRange(new[] { StatType.MeleeDamage, StatType.MeleeAttackSpeed });
        relatedStats.Add(StatType.CardCounting); // Taktiksel her zaman eklenebilir

        // Secenek 1: Kesinlikle build ile alakali bir stat
        if (relatedStats.Count > 0)
        {
            StatType guaranteedStat = relatedStats[Random.Range(0, relatedStats.Count)];
            options[0] = new Option { stat = guaranteedStat, tier = RollTier() };
            pool.Remove(guaranteedStat); // Ayni stat bir daha cikmasin
        }
        else
        {
            // Eger hic silahi yoksa rastgele ver
            int r = Random.Range(0, pool.Count);
            options[0] = new Option { stat = pool[r], tier = RollTier() };
            pool.RemoveAt(r);
        }

        // Secenek 2 ve 3: Kalan gecerli havuzdan tamamen rastgele
        for (int i = 1; i < 3; i++)
        {
            if (pool.Count == 0) break; // Guvenlik

            int r = Random.Range(0, pool.Count);
            options[i] = new Option { stat = pool[r], tier = RollTier() };
            pool.RemoveAt(r);
        }

        // Oyunculari sasirtmamak icin seceneklerin yerlerini karistir (Shuffle)
        System.Random rnd = new System.Random();
        options.OrderBy(x => rnd.Next()).ToArray().CopyTo(options, 0);
    }

    private int RollTier()
    {
        float total = tier1Weight + tier2Weight + tier3Weight;
        float roll = Random.Range(0f, total);
        if (roll < tier1Weight) return 1;
        if (roll < tier1Weight + tier2Weight) return 2;
        return 3;
    }

    private void Apply(Option o)
    {
        float amount = GetIncrement(o.stat) * o.tier;
        switch (o.stat)
        {
            case StatType.Health: health.AddMaxHealth(amount); break;
            case StatType.Armor: health.AddArmor(amount); break;
            case StatType.MoveSpeed: movement.AddMoveSpeed(amount); break;
            default: stats.AddStat(o.stat, amount); break;
        }
        Debug.Log($"Secildi: {DisplayName(o.stat)} +{amount}");
    }

    private float GetIncrement(StatType s) => s switch
    {
        StatType.ProjectileDamage => incProjectileDamage,
        StatType.MeleeDamage => incMeleeDamage,
        StatType.Health => incHealth,
        StatType.Armor => incArmor,
        StatType.MoveSpeed => incMoveSpeed,
        StatType.ProjectileSpeed => incProjectileSpeed,
        StatType.ProjectileRange => incProjectileRange,
        StatType.ProjectileAttackSpeed => incProjectileAttackSpeed,
        StatType.MeleeAttackSpeed => incMeleeAttackSpeed,
        StatType.CardCounting => incCardCounting,
        _ => 1f
    };

    private string DisplayName(StatType s) => s switch
    {
        StatType.ProjectileDamage => "Menzilli Hasar",
        StatType.MeleeDamage => "Yakin Dovus Hasari",
        StatType.Health => "Max Can",
        StatType.Armor => "Zirh",
        StatType.MoveSpeed => "Hareket Hizi",
        StatType.ProjectileSpeed => "Mermi Hizi",
        StatType.ProjectileRange => "Menzil",
        StatType.ProjectileAttackSpeed => "Menzilli Saldiri Hizi",
        StatType.MeleeAttackSpeed => "Yakin Dovus Saldiri Hizi",
        StatType.CardCounting => "Card Counting (taktik hasar)",
        _ => s.ToString()
    };

    private void OnGUI()
    {
        if (!IsOpen) return;

        float w = 340f, h = 260f;
        float x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;

        GUI.Box(new Rect(x, y, w, h),
            $"LEVEL UP!  (kalan secim: {resources.PendingLevelUps})");

        for (int i = 0; i < 3; i++)
        {
            Option o = options[i];
            float amount = GetIncrement(o.stat) * o.tier;
            string label = $"{DisplayName(o.stat)}  +{amount}" + (o.tier > 1 ? $"   (x{o.tier} kademe!)" : "");

            if (GUI.Button(new Rect(x + 20, y + 45 + i * 65, w - 40, 50), label))
                clickedIndex = i;
        }
    }
}