using UnityEngine;

[CreateAssetMenu(menuName = "Ace of Survival/Trinket", fileName = "Trinket_")]
public class TrinketDefinition : ScriptableObject
{
    [Tooltip("Magazada gorunecek isim (or. New Tuxedo)")]
    public string displayName;

    [TextArea]
    [Tooltip("Magazada gorunecek aciklama")]
    public string description;

    [Tooltip("Taban fiyat (chips) - balance tablosundaki fiyat kolonu")]
    public int basePrice = 20;

    [Tooltip("Bu trinket hangi statlari ne kadar degistirir? (eksi degerler serbest)")]
    public TrinketModifier[] modifiers;
}

// Trinketlerin dokunabildigi statlar - level-up havuzundan (StatType) FARKLI liste:
// crit burada VAR (GDD: crit yalniz trinket + her 5 seviyede), melee range henuz yok
// (melee silah sistemi gelince eklenecek).
public enum TrinketStat
{
    MaxHealth, Armor, MoveSpeed,
    ProjectileDamage, MeleeDamage,
    ProjectileSpeed, ProjectileRange,
    ProjectileAttackSpeed, MeleeAttackSpeed,
    CritChance, CritDamage
}

[System.Serializable]
public class TrinketModifier
{
    public TrinketStat stat;
    [Tooltip("Degisim miktari. Eksi yazilabilir (or. Greed: -3)")]
    public float amount;
}
