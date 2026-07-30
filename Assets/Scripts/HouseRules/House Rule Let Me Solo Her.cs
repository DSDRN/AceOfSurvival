using UnityEngine;

public class HouseRuleLetMeSoloHer : HouseRuleBase
{
    [Header("Kilitli seviye basina stat puani = 2 (KILIT)")]
    [SerializeField] private int pointsPerLevel = 2;

    [Header("Puan basina stat degerleri (level-up birimleriyle uyumlu oneri)")]
    [SerializeField] private float pProjDmg = 1f, pMeleeDmg = 1f, pHealth = 3f, pArmor = 1f;
    [SerializeField] private float pMove = 0.2f, pProjSpeed = 0.25f, pRange = 0.25f;
    [SerializeField] private float pProjAtkSpd = 0.15f, pMeleeAtkSpd = 0.15f;
    [SerializeField] private float pCritChance = 1f, pCritDamage = 0.05f;

    public override bool CanSell => false;   // KALICI SECIM

    private bool choiceMade;

    private void Start()
    {
        // Magazadan alindigi an (Obje uretildiginde) secim ekranini hazirla
        if (!choiceMade)
        {
            Time.timeScale = 0f;
        }
    }
    protected override void OnLevelChanged()
    {
        // Içi bos kalacak, sadece base class'in kuralina uymak icin burada.
    }

    private void OnGUI()
    {
        if (choiceMade) return;

        float w = 420f, h = 180f;
        float x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;

        GUI.Box(new Rect(x, y, w, h),
            "LET ME SOLO HER\nBir yol sec - GERI DONUS YOK:");

        if (GUI.Button(new Rect(x + 20, y + 60, w - 40, 45),
            "SADECE RANGED (melee kilitlenir, atis hizi +%50)"))
            Choose(WeaponCategory.Melee);   // melee KILITLENIR

        if (GUI.Button(new Rect(x + 20, y + 115, w - 40, 45),
            "SADECE MELEE (ranged kilitlenir, melee erisim +%50)"))
            Choose(WeaponCategory.Ranged);  // ranged KILITLENIR
    }

    private void Choose(WeaponCategory lockedCategory)
    {
        choiceMade = true;

        // 1) Kategoriyi kilitle: silahlar sokulur, toplam seviye doner
        int lockedLevels = weaponManager.LockCategory(lockedCategory);

        // 2) Seviye -> random stat cevrimi: her seviye = 2 puan
        int points = lockedLevels * pointsPerLevel;
        for (int i = 0; i < points; i++)
            ApplyRandomStatPoint();
        Debug.Log($"LMSH: {lockedLevels} kilitli seviye -> {points} random stat puani");

        // 3) Kategori bonus/cezalari
        if (lockedCategory == WeaponCategory.Melee)
        {
            // Ranged yolu: melee hasar 0, atis hizi +%50
            stats.AddStat(StatType.MeleeDamage, -stats.BaseMeleeDamage);
            stats.AddStat(StatType.ProjectileAttackSpeed, stats.ProjectileAttackSpeed * 0.5f);
        }
        else
        {
            // Melee yolu: proj hasar 0, melee erisim +%50
            stats.AddStat(StatType.ProjectileDamage, -stats.BaseProjectileDamage);
            stats.MultiplyMeleeReach(1.5f);
        }

        // Oyunu SADECE biz durdurmussak akit (magazadaysak magaza yonetiyor)
        // Basit kural: magaza acik degilse timeScale'i geri ver
        if (Time.timeScale == 0f && !AnyOtherScreenOpen())
            Time.timeScale = 1f;
    }

    private bool AnyOtherScreenOpen()
    {
        ShopManager shop = FindFirstObjectByType<ShopManager>();
        return shop != null && shop.IsOpen;
    }

    /// +1 puani 11 statin rastgele birine uygular (routing - ayni desen)
    private void ApplyRandomStatPoint()
    {
        int r = Random.Range(0, 11);
        switch (r)
        {
            case 0: stats.AddStat(StatType.ProjectileDamage, pProjDmg); break;
            case 1: stats.AddStat(StatType.MeleeDamage, pMeleeDmg); break;
            case 2: health.AddMaxHealth(pHealth); break;
            case 3: health.AddArmor(pArmor); break;
            case 4: movement.AddMoveSpeed(pMove); break;
            case 5: stats.AddStat(StatType.ProjectileSpeed, pProjSpeed); break;
            case 6: stats.AddStat(StatType.ProjectileRange, pRange); break;
            case 7: stats.AddStat(StatType.ProjectileAttackSpeed, pProjAtkSpd); break;
            case 8: stats.AddStat(StatType.MeleeAttackSpeed, pMeleeAtkSpd); break;
            case 9: stats.AddCritChance(pCritChance); break;
            case 10: stats.AddCritDamage(pCritDamage); break;
        }
    }
}
