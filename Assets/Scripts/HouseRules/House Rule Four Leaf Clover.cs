using UnityEngine;

public class HouseRuleFourLeafClover : HouseRuleBase
{
    [Tooltip("Kural seviyesi basina crit sansi (puan)")]
    [SerializeField] private float critPerLevel = 4f;

    [Tooltip("Kural seviyesi basina armor kaybi")]
    [SerializeField] private float armorPerLevel = 1f;

    private int appliedLevels;   // kac seviyelik etki uygulandi

    protected override void OnLevelChanged()
    {
        int delta = Level - appliedLevels;   // yeni gelen seviye sayisi
        stats.AddCritChance(critPerLevel * delta);
        health.AddArmor(-armorPerLevel * delta);
        appliedLevels = Level;
    }

    public override void OnRemoved()
    {
        stats.AddCritChance(-critPerLevel * appliedLevels);
        health.AddArmor(armorPerLevel * appliedLevels);
        appliedLevels = 0;
    }
}
