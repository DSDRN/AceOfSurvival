using UnityEngine;

public class HouseRuleHouseAlwaysWins : HouseRuleBase
{
    [Tooltip("Hasar carpani = 3 (KILIT)")]
    [SerializeField] private float damageMult = 3f;

    [Tooltip("Saldiri hizi carpani = 1.5 (KILIT)")]
    [SerializeField] private float atkSpeedMult = 1.5f;

    public override bool CanSell => false;   // KALICI SECIM

    protected override void OnLevelChanged()
    {
        // Seviyesiz: sadece ilk kusanimda calisir
        stats.MultiplyDamage(damageMult);
        stats.MultiplyPermanentAtkSpeed(atkSpeedMult);
        health.ForceGlassCannon();
        Debug.Log("HOUSE ALWAYS WINS: x3 hasar, x1.5 hiz... ve 1 can. Iyi sanslar.");
    }

    // OnRemoved yok - satilamaz zaten (CanSell=false manager'da engeller)
}
