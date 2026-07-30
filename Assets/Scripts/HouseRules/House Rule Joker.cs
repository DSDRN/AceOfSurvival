using UnityEngine;

public class HouseRuleJoker : HouseRuleBase
{
    [Tooltip("Seviye basina yok sayma sansi (%) - KILIT degerler")]
    [SerializeField] private float[] chanceByLevel = { 10f, 12f, 14f, 15.5f, 17.5f };

    [Tooltip("L5'te yansitma carpani = 1.25 (oncesinde 1.0)")]
    [SerializeField] private float level5ReflectMult = 1.25f;

    private float curChance;

    protected override void OnLevelChanged()
    {
        curChance = chanceByLevel[Level - 1];

        // Kancaya tak (ilk kusanımda; seviye atlayinca sadece sans guncellenir)
        if (health.DamageInterceptor == null)
            health.DamageInterceptor = Intercept;
    }

    /// PlayerHealth her hasarda cagirir. true = hasari yok say.
    private bool Intercept(float rawDamage, EnemyHealth attacker)
    {
        if (Random.Range(0f, 100f) >= curChance)
            return false;   // sans tutmadi, hasar normal islensin

        // SANS TUTTU: hasar yok + saldirana yansitma
        float reflect = rawDamage * (Level >= 5 ? level5ReflectMult : 1f);
        if (attacker != null)
            attacker.TakeDamage(reflect, false);

        Debug.Log($"JOKER! Hasar yok sayildi" + (attacker != null ? $", {reflect:F1} yansitildi" : ""));
        // Gecici feedback: beyaz "JOKER" hissi olarak 0 hasar sayisi yerine
        // ileride ozel yazi/ses eklenecek (cila)
        return true;
    }

    public override void OnRemoved()
    {
        // Kancayi sok (baska seyin kancasini yanlislikla sokme)
        if (health.DamageInterceptor == Intercept)
            health.DamageInterceptor = null;
    }
}
