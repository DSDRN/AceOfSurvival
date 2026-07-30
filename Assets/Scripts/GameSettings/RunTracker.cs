using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class RunTracker
{
    // Kaynak adi -> toplam verdigi hasar
    private static readonly Dictionary<string, float> damageTaken = new();

    /// Yeni run = temiz defter (WaveManager Awake'te cagirir)
    public static void Reset()
    {
        damageTaken.Clear();
    }

    /// PlayerHealth gercek hasar islendiginde cagirir
    public static void RecordDamageTaken(string source, float damage)
    {
        if (string.IsNullOrEmpty(source))
            source = "Bilinmeyen";

        // "Enemy_Fedai(Clone)" -> "Enemy_Fedai" (havuz kopyalarinin adi temizlensin)
        source = source.Replace("(Clone)", "").Trim();

        damageTaken.TryGetValue(source, out float mevcut);
        damageTaken[source] = mevcut + damage;
    }

    /// En cok vuran N kaynak (buyukten kucuge) - run sonu tablosu icin
    public static List<KeyValuePair<string, float>> TopDamageDealers(int n)
    {
        return damageTaken.OrderByDescending(kv => kv.Value).Take(n).ToList();
    }
}
