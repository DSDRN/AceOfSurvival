using System.IO;
using UnityEngine;

public static class SaveSystem
{
    // [System.Serializable] = JsonUtility bu sinifi JSON'a cevirebilsin
    [System.Serializable]
    private class SaveData
    {
        public int gold;
        // Ileride: ayarlar (ses, dil, fullscreen), meta yukseltmeler (tam surum)
    }

    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "aos_save.json");

    /// <summary>Kayitli toplam altini oku (dosya yoksa 0).</summary>
    public static int LoadGold()
    {
        return Load().gold;
    }

    /// <summary>Run sonunda kazanilan altini toplama ekle ve DISKE YAZ.</summary>
    public static int AddGold(int amount)
    {
        SaveData data = Load();
        data.gold += Mathf.Max(0, amount);
        Write(data);
        return data.gold;   // yeni toplam (ekranda gostermek icin)
    }

    // ---------------- ic isler ----------------
    private static SaveData Load()
    {
        try
        {
            if (File.Exists(SavePath))
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        }
        catch (System.Exception e)
        {
            // Bozuk dosya oyunu cokertmesin: logla, sifirdan basla
            Debug.LogWarning($"Save okunamadi, sifirdan baslaniyor: {e.Message}");
        }
        return new SaveData();
    }

    private static void Write(SaveData data)
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save YAZILAMADI: {e.Message}");
        }
    }
}
