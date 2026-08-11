using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RunTracker : MonoBehaviour
{
    // --- HASAR ALINAN (Eski kodundan gelenler) ---
    private static readonly Dictionary<string, float> damageTaken = new();

    // --- YENI ISTATISTIKLER (Zaman, Ekonomi, Hasar Verme) ---
    public static float TotalActiveTime = 0f;
    public static float TotalShopTime = 0f;
    public static int TotalChips = 0;
    public static int TotalGold = 0;
    public static Dictionary<string, float> WeaponDamages = new Dictionary<string, float>();

    public static void Reset()
    {
        damageTaken.Clear();
        WeaponDamages.Clear();
        TotalActiveTime = 0f;
        TotalShopTime = 0f;
        TotalChips = 0;
        TotalGold = 0;
    }

    /// PlayerHealth gercek hasar islendiginde cagirir (Senin kodun)
    public static void RecordDamageTaken(string source, float damage)
    {
        if (string.IsNullOrEmpty(source))
            source = "Bilinmeyen";

        source = source.Replace("(Clone)", "").Trim();

        damageTaken.TryGetValue(source, out float mevcut);
        damageTaken[source] = mevcut + damage;
    }

    /// Silahlarin verdigi hasari kaydetmek icin cagirilir (YENI)
    public static void AddDamageDealt(string weaponName, float damage)
    {
        if (WeaponDamages.ContainsKey(weaponName))
            WeaponDamages[weaponName] += damage;
        else
            WeaponDamages[weaponName] = damage;
    }

    /// En cok vuran N kaynak (Senin kodun)
    public static List<KeyValuePair<string, float>> TopDamageDealers(int n)
    {
        return damageTaken.OrderByDescending(kv => kv.Value).Take(n).ToList();
    }

    private void Update()
    {
        // Zaman durmussa (timeScale == 0) magaza/pause suresidir. Oynuyorsa gercek suredir.
        if (Time.timeScale == 0f)
            TotalShopTime += Time.unscaledDeltaTime;
        else
            TotalActiveTime += Time.deltaTime;
    }

    // Oyun bittiginde bunu cagirirsan her seyi konsola liste gibi yazdirir!
    public static void PrintStats()
    {
        Debug.Log("=== RUN OZETI ===");
        Debug.Log($"Oyun Suresi: {TotalActiveTime / 60f:F2} Dakika");
        Debug.Log($"Market/Duraklama Suresi: {TotalShopTime / 60f:F2} Dakika");
        Debug.Log($"Kazanilan Chip: {TotalChips}");
        Debug.Log($"Kazanilan Altin: {TotalGold}");
        Debug.Log($"Öldürülen Düşman: {EnemyHealth.RunKills}");

        Debug.Log("--- EN COK HASAR VURDUGUN SİLAHLAR ---");
        foreach (var kv in WeaponDamages.OrderByDescending(x => x.Value).Take(5))
            Debug.Log($"> {kv.Key}: {kv.Value:F1} Hasar");

        Debug.Log("--- SANA EN COK VURANLAR ---");
        foreach (var kv in TopDamageDealers(5))
            Debug.Log($"> {kv.Key}: {kv.Value:F1} Hasar");

        Debug.Log("=================");
    }
}