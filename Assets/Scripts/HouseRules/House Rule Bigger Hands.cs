using UnityEngine;

public class HouseRuleBiggerHands : HouseRuleBase
{
    [Tooltip("Seviye basina TOPLAM yaricap bonusu (KILIT: GDD dipnotu)")]
    [SerializeField] private float[] totalBonusByLevel = { 0.5f, 1f, 1.5f, 2f, 2.5f };

    private float applied;   // su ana kadar uygulanan toplam

    protected override void OnLevelChanged()
    {
        float hedef = totalBonusByLevel[Level - 1];
        float delta = hedef - applied;      // sadece FARKI ekle (cift sayma olmasin)
        stats.AddPickupRadius(delta);
        applied = hedef;
    }

    public override void OnRemoved()
    {
        stats.AddPickupRadius(-applied);    // her seyi geri al
        applied = 0f;
    }
}
