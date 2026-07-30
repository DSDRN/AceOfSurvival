using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunEndManager : MonoBehaviour
{
    [Header("Baglantilar (Player'i surukle)")]
    [SerializeField] private PlayerResources resources;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Altin formulu (balance karari)")]
    [SerializeField] private int goldPerWave = 2;
    [SerializeField] private float chipsToGoldPercent = 5f;
    [SerializeField] private int victoryBonus = 25;

    [Header("Tematik metinler (Localization'a SEN baglayacaksin)")]
    [TextArea]
    [SerializeField]
    private string[] defeatLines = {
        "Kasa her zaman kazanir...",
        "Zarlar bu gece senin icin donmedi.",
        "Masadan kalkmasini bilemedin.",
        "Krupiye elini toplarken gulumsuyordu."
    };
    [TextArea]
    [SerializeField]
    private string[] victoryLines = {
        "Bu gece... kasa kaybetti.",
        "Dealer sapkasini cikardi. Masa senin.",
        "Cipleri say, efsaneyi yaz."
    };

    public bool IsShowing { get; private set; }

    private bool victory;
    private int waveReached, kills, chipsEarned;
    private float runTime;
    private string chosenLine;
    private int goldFromWaves, goldFromChips, goldBonus, goldChests, goldTotal, newBalance;
    private List<KeyValuePair<string, float>> topDealers;
    private string weaponsUsed;

    private float runStartUnscaled;   // sure olcumu (magaza dahil)

    private void Awake()
    {
        // unscaledTime: timeScale 0 olsa da akar -> "toplam sure magaza dahil"
        runStartUnscaled = Time.unscaledTime;
    }

    /// <summary>WaveManager cagirir: zafer / olum / sure doldu - hepsi buradan.</summary>
    public void ShowEnd(bool isVictory, int reachedWave)
    {
        if (IsShowing) return;   // cift cagri korumasi

        victory = isVictory;
        waveReached = reachedWave;
        kills = EnemyHealth.RunKills;
        runTime = Time.unscaledTime - runStartUnscaled;
        chipsEarned = resources != null ? resources.TotalChipsEarned : 0;
        topDealers = RunTracker.TopDamageDealers(5);

        // Kullanilan silahlar: "Kart Destesi Lv3, Zar Lv2" gibi
        weaponsUsed = "";
        if (weaponManager != null)
            foreach (WeaponBase w in weaponManager.Equipped)
                weaponsUsed += $"{w.WeaponName} Lv{w.Level}   ";
        if (weaponsUsed == "") weaponsUsed = "(silah kalmamis!)";

        // Tematik cumle
        string[] havuz = victory ? victoryLines : defeatLines;
        chosenLine = havuz.Length > 0 ? havuz[Random.Range(0, havuz.Length)] : "";

        // ---- ALTIN DOKUMU ----
        goldFromWaves = waveReached * goldPerWave;
        goldFromChips = Mathf.FloorToInt(chipsEarned * chipsToGoldPercent / 100f);
        goldBonus = victory ? victoryBonus : 0;
        goldChests = resources != null ? resources.BonusGold : 0;   // sandiklar!
        goldTotal = goldFromWaves + goldFromChips + goldBonus + goldChests;

        // TEK KAYIT ANI (KILIT kural)
        newBalance = SaveSystem.AddGold(goldTotal);

        IsShowing = true;
        Time.timeScale = 0f;
    }

    private void OnGUI()
    {
        if (!IsShowing) return;

        float w = 460f, h = 560f;
        float x = (Screen.width - w) / 2f, y = (Screen.height - h) / 2f;

        GUI.Box(new Rect(x, y, w, h),
            (victory ? "*** MASADAN ZAFERLE KALKTIN! ***" : "--- RUN BITTI ---")
            + "\n\"" + chosenLine + "\"");

        float ly = y + 55;
        void Satir(string s) { GUI.Label(new Rect(x + 25, ly, w - 50, 22), s); ly += 22; }

        Satir($"Ulasilan wave    : {waveReached}");
        Satir($"Oldurulen dusman : {kills}");
        Satir($"Toplam sure      : {Mathf.FloorToInt(runTime / 60f)}:{Mathf.FloorToInt(runTime % 60f):00}  (magaza dahil)");
        Satir($"Silahlarin       : {weaponsUsed}");
        ly += 6;

        Satir("--- SANA EN COK VURANLAR ---");
        if (topDealers != null && topDealers.Count > 0)
            foreach (var kv in topDealers)
                Satir($"  {kv.Key}: {kv.Value:F0} hasar");
        else
            Satir("  (hic hasar yemedin - efsanesin)");
        ly += 6;

        Satir("--- ALTIN HESABI ---");
        Satir($"Wave odulu    : {waveReached} x {goldPerWave} = {goldFromWaves}");
        Satir($"Chips cevrimi : %{chipsToGoldPercent:F0} x {chipsEarned} = {goldFromChips}");
        if (goldChests > 0) Satir($"Sandik altini : +{goldChests}");
        if (victory) Satir($"Zafer bonusu  : +{goldBonus}");
        Satir($"KAZANILAN     : {goldTotal} altin");
        Satir($"TOPLAM ALTIN  : {newBalance}  (kaydedildi)");

        if (GUI.Button(new Rect(x + 25, y + h - 55, 180, 40), "TEKRAR DENE"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (GUI.Button(new Rect(x + w - 205, y + h - 55, 180, 40), "OYUNDAN CIK"))
        {
            Application.Quit();
        }
    }
}
