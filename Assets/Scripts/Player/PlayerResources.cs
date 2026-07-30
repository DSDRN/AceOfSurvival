using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("XP egrisi (balance 6_XP_Egrisi)")]
    [SerializeField] private float baseXP = 10f;
    [SerializeField] private float xpGrowth = 1.25f;
    [SerializeField] private int maxLevel = 25;

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

    /// Magaza satis iadesi (chips ekler - kazanc sayilmaz, altina donmez)
    public void AddChipsRefund(int amount)
    {
        Chips += Mathf.Max(0, amount);
    }

    /// Sandik altini (DUZELTILDI: artik chips'e degil, ayri altin sayacina)
    public void AddBonusGold(int amount)
    {
        BonusGold += Mathf.Max(0, amount);
    }

    // ---- Gecici debug HUD ----
    private void OnGUI()
    {
        GUI.Label(new Rect(Screen.width - 260, 10, 250, 25),
            $"Chips: {Chips}   Level: {Level}");
        GUI.Label(new Rect(Screen.width - 260, 35, 250, 25),
            $"XP: {CurrentXP:F0}/{XPToNext:F0}   Bekleyen: +{PendingLevelUps}");
    }
}