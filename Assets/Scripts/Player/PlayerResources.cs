using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("XP egrisi (balance 6_XP_Egrisi)")]
    [SerializeField] private float baseXP = 10f;
    [SerializeField] private float xpGrowth = 1.25f;
    [SerializeField] private int maxLevel = 25;

    [Header("Stat Baglantisi (Crit icin)")]
    [SerializeField] private PlayerStats stats; // Inspector'dan suruklemeyi unutma!

    public int Chips { get; private set; }
    public int Level { get; private set; } = 1;
    public float CurrentXP { get; private set; }
    public int PendingLevelUps { get; private set; }

    /// Run boyunca KAZANILAN toplam chips (harcananlar dusulmez) - altin cevrimi icin
    public int TotalChipsEarned { get; private set; }

    /// Sandiklardan biriken bonus altin (altinin run icindeki TEK kaynagi)
    public int BonusGold { get; private set; }

    public float XPToNext => Mathf.Round(baseXP * Mathf.Pow(xpGrowth, Level - 1));

    public void AddPickup(PickupType type, float value)
    {
        if (type == PickupType.Chips)
        {
            int miktar = Mathf.RoundToInt(value);
            Chips += miktar;
            TotalChipsEarned += miktar;
            RunTracker.TotalChips += miktar; // RunTracker'a baglandi
        }
        else
            AddXP(value);
    }

    private void AddXP(float amount)
    {
        if (Level >= maxLevel) return;
        CurrentXP += amount;
        while (CurrentXP >= XPToNext && Level < maxLevel)
        {
            CurrentXP -= XPToNext;
            Level++;
            PendingLevelUps++;
            Debug.Log($"LEVEL UP! Yeni level: {Level} (bekleyen secim: {PendingLevelUps})");

            // EPIC 2: Her 5 seviyede bir otomatik Crit artisi!
            // EPIC 2: Her 5 seviyede bir otomatik Crit artisi!
            if (Level % 5 == 0 && stats != null)
            {
                stats.AddCritChance(2f);
                stats.AddCritDamage(0.08f); // Hatalı satırları silip doğrusunu yazdık
                Debug.Log($"5. Seviye Bonusu: Crit Sansi +%2, Crit Carpani +0.08x");
            }
        }
    }

    public void ConsumeLevelUp()
    {
        if (PendingLevelUps > 0) PendingLevelUps--;
    }

    public bool SpendChips(int amount)
    {
        if (Chips < amount) return false;
        Chips -= amount;
        return true;
    }

    public void AddChipsRefund(int amount)
    {
        Chips += Mathf.Max(0, amount);
    }

    public void AddBonusGold(int amount)
    {
        int kazanilan = Mathf.Max(0, amount);
        BonusGold += kazanilan;
        RunTracker.TotalGold += kazanilan; // RunTracker'a baglandi
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(Screen.width - 260, 10, 250, 25),
            $"Chips: {Chips}   Level: {Level}");
        GUI.Label(new Rect(Screen.width - 260, 35, 250, 25),
            $"XP: {CurrentXP:F0}/{XPToNext:F0}   Bekleyen: +{PendingLevelUps}");
    }
}