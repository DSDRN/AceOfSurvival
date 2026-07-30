using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        List<StatType> pool = new List<StatType>((StatType[])System.Enum.GetValues(typeof(StatType)));
        for (int i = 0; i < 3; i++)
        {
            int r = Random.Range(0, pool.Count);
            options[i] = new Option { stat = pool[r], tier = RollTier() };
            pool.RemoveAt(r);
        }
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
            default: stats.AddStat(o.stat, amount); break;   // CardCounting dahil
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
