using UnityEngine;

public class HouseRuleLetMeSoloHer : HouseRuleBase
{
    [Header("Kilitli seviye basina stat puani = 2 (KILIT)")]
    [SerializeField] private int pointsPerLevel = 2;

    [Header("Puan basina stat degerleri")]
    [SerializeField] private float pProjDmg = 1f, pMeleeDmg = 1f, pHealth = 3f, pArmor = 1f;
    [SerializeField] private float pMove = 0.2f, pProjSpeed = 0.25f, pRange = 0.25f;
    [SerializeField] private float pProjAtkSpd = 0.15f, pMeleeAtkSpd = 0.15f;
    [SerializeField] private float pCritChance = 1f, pCritDamage = 0.05f;

    public override bool CanSell => false;   // KALICI SECIM

    private bool choiceMade = false;
    private bool isWaitingForWaveStart = true;
    private bool showUI = false;

    // Magaza referansi BIR KEZ bulunur. Eskiden her frame FindFirstObjectByType
    // cagriliyordu = sahnedeki tum objeleri taramak, kare basina bosa maliyet.
    private ShopManager shopCache;

    // Base class kurali geregi bos durmali
    protected override void OnLevelChanged() { }

    private void Awake()
    {
        shopCache = FindFirstObjectByType<ShopManager>();
    }

    private void Update()
    {
        // Eger secim zaten yapildiysa hicbir sey yapma
        if (choiceMade) return;

        // Eger magaza aciksa, sessizce bekle (UI cizme, zamani durdurma)
        if (AnyOtherScreenOpen()) return;

        // Magaza kapandiysa (Next Wave basildiysa) ve hala bekliyorsak:
        // Harekete gec ve pusuya yat!
        if (isWaitingForWaveStart)
        {
            isWaitingForWaveStart = false; // Pusu tetiklendi
            showUI = true;                 // Arayuzu goster
            Time.timeScale = 0f;           // Dalga baslamadan oyunu ANINDA dondur
        }
    }

    private void OnGUI()
    {
        // UI kapaliysa (magazadaysak veya secim yapildiysa) cizme
        if (!showUI) return;

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
        showUI = false; // Ekrani kapat

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

            // DIKKAT: eskiden "AddStat(..., stats.ProjectileAttackSpeed * 0.5f)" yaziyordu.
            // stats.ProjectileAttackSpeed bir GETTER: taban degeri Ace of Spades burst'u ve
            // diger carpanlarla CARPILMIS halde dondurur. Yani Ace of Spades tam o anda
            // aktifse (2.5x) tabana 2.5 kat fazla eklenirdi - kalici sismis stat.
            // Dogrusu: carpan olarak uygula. %50 = 1.5x
            stats.MultiplyPermanentAtkSpeed(1.5f);
        }
        else
        {
            // Melee yolu: proj hasar 0, melee erisim +%50
            stats.AddStat(StatType.ProjectileDamage, -stats.BaseProjectileDamage);
            stats.MultiplyMeleeReach(1.5f);
        }

        // Oyuncu secimini yapti, dalgayi (oyunu) baslat!
        Time.timeScale = 1f;
    }

    private bool AnyOtherScreenOpen()
    {
        return shopCache != null && shopCache.IsOpen;
    }

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