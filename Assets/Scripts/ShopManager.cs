using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("Baglantilar (Player'dan surukle)")]
    [SerializeField] private PlayerResources resources;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private HouseRuleManager houseRuleManager;

    [Header("Satis havuzu")]
    [SerializeField] private TrinketDefinition[] allTrinkets;
    [SerializeField] private WeaponBase[] allWeapons;
    [SerializeField] private HouseRuleBase[] allHouseRules;

    [Header("Fiyatlar (balance 7_Magaza)")]
    [SerializeField] private int weaponBasePrice = 70;
    [SerializeField] private int houseRuleBasePrice = 90;
    [SerializeField] private float wavePriceScalePercent = 30f;
    [SerializeField] private float itemPriceIncreasePercent = 35f;
    [SerializeField] private int healCost = 50; // Can yenileme fiyati
    [SerializeField] private int refreshBaseCost = 35;
    [SerializeField] private float refreshIncreasePercent = 50f;
    [SerializeField] private float sellRefundPercent = 50f;

    public bool IsOpen { get; private set; }

    private class Slot
    {
        public TrinketDefinition trinket;
        public WeaponBase weaponPrefab;
        public HouseRuleBase hrPrefab;
        public int price;
        public bool sold;
        public bool locked;                    // YENI: kilit
        public bool IsWeapon => weaponPrefab != null;
        public bool IsHouseRule => hrPrefab != null;
    }

    private readonly Slot[] slots = new Slot[5];   // KALICI (kilitler yasasin diye)
    private int refreshesUsed;
    private int currentWave;
    private bool closeClicked;
    private bool sellMode;                          // YENI: sat modu
    private WeaponBase pendingWeapon;               // YENI: askidaki silah (prefab)
    private readonly List<WeaponBase> pendingCopies = new();   // gecerli kopya hedefleri
    private readonly Dictionary<TrinketDefinition, int> ownedTrinkets = new();

    public IEnumerator RunShop(int waveNumber)
    {
        currentWave = waveNumber;
        refreshesUsed = 0;
        sellMode = false;
        pendingWeapon = null;

        RollSlots(respectLocks: true);   // KILIT kurali: her wave zorunlu bedava reroll (kilitliler haric)

        IsOpen = true;
        closeClicked = false;
        Time.timeScale = 0f;

        while (!closeClicked || pendingWeapon != null)
            yield return null;

        Time.timeScale = 1f;
        IsOpen = false;
    }

    // ---------------- HAVUZ + SLOT ÜRETİMİ ----------------
    private void RollSlots(bool respectLocks)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (respectLocks && slots[i] != null && slots[i].locked && !slots[i].sold)
                continue;   // kilitli ve satilmamis: aynen kalir (fiyati da sabit)
            slots[i] = RollOne();
        }
    }

    private Slot RollOne()
    {
        // FILTRELI havuzlar (v1.3):
        List<WeaponBase> silahlar = new();
        foreach (WeaponBase w in allWeapons)
            if (!weaponManager.IsCategoryLocked(w.Category))   // LMSH filtresi
                silahlar.Add(w);

        List<HouseRuleBase> kurallar = new();
        foreach (HouseRuleBase hr in allHouseRules)
        {
            HouseRuleBase owned = houseRuleManager.FindBySource(hr);
            // Sahipsek ve (seviyesiz VEYA max) -> vitrine girmez (tek kopya kurali)
            if (owned != null && (!owned.Levelable || owned.Level >= HouseRuleBase.MaxLevel))
                continue;
            kurallar.Add(hr);
        }

        int toplam = silahlar.Count + kurallar.Count + allTrinkets.Length;
        int roll = Random.Range(0, Mathf.Max(1, toplam));

        if (roll < silahlar.Count)
            return new Slot { weaponPrefab = silahlar[roll], price = CalculatePrice(weaponBasePrice) };
        roll -= silahlar.Count;
        if (roll < kurallar.Count)
            return new Slot { hrPrefab = kurallar[roll], price = CalculatePrice(houseRuleBasePrice) };
        TrinketDefinition t = allTrinkets[Mathf.Clamp(roll - kurallar.Count, 0, allTrinkets.Length - 1)];
        return new Slot { trinket = t, price = CalculatePrice(t.basePrice) };
    }

    private int CalculatePrice(int basePrice)
    {
        float wave = Mathf.Pow(1f + wavePriceScalePercent / 100f, currentWave - 1);
        float refresh = Mathf.Pow(1f + itemPriceIncreasePercent / 100f, refreshesUsed);
        return Mathf.RoundToInt(basePrice * wave * refresh);
    }

    private int CurrentRefreshCost()
        => Mathf.RoundToInt(refreshBaseCost * Mathf.Pow(1f + refreshIncreasePercent / 100f, refreshesUsed));

    private int RefundFor(int basePrice)
        => Mathf.RoundToInt(basePrice * sellRefundPercent / 100f);

    // ---------------- SATIN ALMA ----------------
    private void TryBuy(int index)
    {
        Slot s = slots[index];
        if (s.sold || resources.Chips < s.price || pendingWeapon != null) return;

        if (s.IsWeapon)
        {
            if (!weaponManager.CanPurchase(s.weaponPrefab)) return;   // Durum C korumasi

            resources.SpendChips(s.price);      // para HEMEN duser (belge kurali)
            s.sold = true;
            s.locked = false;                    // satilan slotta kilit anlamsiz

            // Gecerli hedefleri hesapla
            weaponManager.GetUpgradableCopies(s.weaponPrefab, pendingCopies);
            bool bosVar = weaponManager.HasEmptySlot;
            int hedefSayisi = pendingCopies.Count + (bosVar ? 1 : 0);

            // OTOMATIK kisayollar (belge: tek hedef varsa sorma)
            if (hedefSayisi <= 1)
            {
                if (pendingCopies.Count == 1 && !bosVar)
                    pendingCopies[0].LevelUp();
                else if (bosVar && pendingCopies.Count == 0)
                    weaponManager.EquipNewCopy(s.weaponPrefab);
                pendingWeapon = null;
            }
            else
            {
                pendingWeapon = s.weaponPrefab;  // ASKIDA: oyuncu hedef secene kadar
                sellMode = false;
            }
        }
        else if (s.IsHouseRule)
        {
            if (!houseRuleManager.TryEquipOrLevelUp(s.hrPrefab)) return;
            resources.SpendChips(s.price);
            s.sold = true; s.locked = false;
        }
        else
        {
            resources.SpendChips(s.price);
            ApplyTrinket(s.trinket, +1f);
            ownedTrinkets[s.trinket] = ownedTrinkets.TryGetValue(s.trinket, out int n) ? n + 1 : 1;
            s.sold = true; s.locked = false;
        }

        // 5/5 bedava yenileme (kilitler zaten satildiginda dusuyor)
        bool allSold = true;
        foreach (Slot slot in slots)
            if (!slot.sold) { allSold = false; break; }
        if (allSold)
        {
            refreshesUsed++;
            RollSlots(respectLocks: true);
        }
    }

    // Askidaki silahi hedefe uygula
    private void PlacePendingOnCopy(WeaponBase copy)
    {
        if (pendingWeapon == null) return;
        copy.LevelUp();
        pendingWeapon = null;
    }

    private void PlacePendingOnEmptySlot()
    {
        if (pendingWeapon == null) return;
        weaponManager.EquipNewCopy(pendingWeapon);
        pendingWeapon = null;
    }

    private void TryRefresh()
    {
        if (pendingWeapon != null) return;
        int cost = CurrentRefreshCost();
        if (!resources.SpendChips(cost)) return;
        refreshesUsed++;
        RollSlots(respectLocks: true);
    }

    private void BuyHealthRestore()
    {
        // Parasi yetiyorsa ve cani max degilse
        if (resources.Chips >= healCost && health.CurrentHealth < health.MaxHealth)
        {
            resources.SpendChips(healCost);

            // Maksimum canin %30'u kadar iyilestir (0.5f yerine 0.3f yazdik)
            float healAmount = health.MaxHealth * 0.3f;

            // PlayerHealth icindeki yeni Heal fonksiyonumuzu cagiriyoruz
            health.Heal(healAmount);
        }
    }

    // ---------------- SATIS ----------------
    private void TrySellWeapon(WeaponBase instance)
    {
        if (!weaponManager.SellWeapon(instance)) return;
        resources.AddChipsRefund(RefundFor(weaponBasePrice));
    }

    private void TrySellHouseRule(HouseRuleBase instance)
    {
        if (!houseRuleManager.Sell(instance)) return;
        resources.AddChipsRefund(RefundFor(houseRuleBasePrice));
    }

    private void TrySellTrinket(TrinketDefinition t)
    {
        if (!ownedTrinkets.TryGetValue(t, out int n) || n <= 0) return;
        ApplyTrinket(t, -1f);
        if (n == 1) ownedTrinkets.Remove(t);
        else ownedTrinkets[t] = n - 1;
        resources.AddChipsRefund(RefundFor(t.basePrice));
    }

    // ---------------- TRINKET (degismedi) ----------------
    private void ApplyTrinket(TrinketDefinition t, float sign)
    {
        foreach (TrinketModifier m in t.modifiers)
        {
            float a = m.amount * sign;
            switch (m.stat)
            {
                case TrinketStat.MaxHealth: health.AddMaxHealth(a); break;
                case TrinketStat.Armor: health.AddArmor(a); break;
                case TrinketStat.MoveSpeed: movement.AddMoveSpeed(a); break;
                case TrinketStat.CritChance: stats.AddCritChance(a); break;
                case TrinketStat.CritDamage: stats.AddCritDamage(a); break;
                case TrinketStat.ProjectileDamage: stats.AddStat(StatType.ProjectileDamage, a); break;
                case TrinketStat.MeleeDamage: stats.AddStat(StatType.MeleeDamage, a); break;
                case TrinketStat.ProjectileSpeed: stats.AddStat(StatType.ProjectileSpeed, a); break;
                case TrinketStat.ProjectileRange: stats.AddStat(StatType.ProjectileRange, a); break;
                case TrinketStat.ProjectileAttackSpeed: stats.AddStat(StatType.ProjectileAttackSpeed, a); break;
                case TrinketStat.MeleeAttackSpeed: stats.AddStat(StatType.MeleeAttackSpeed, a); break;
            }
        }
    }

    // ================= 3 SUTUNLU EKRAN (OnGUI - gecici) =================
    private void OnGUI()
    {
        if (!IsOpen) return;

        float leftW = 250f, midW = 430f, rightW = 300f, gap = 10f;
        float totalW = leftW + midW + rightW + gap * 2;
        float h = 600f;
        float x0 = (Screen.width - totalW) / 2f;
        float y0 = (Screen.height - h) / 2f;

        bool askida = pendingWeapon != null;

        // Askida bandi (ust orta)
        if (askida)
        {
            GUI.color = Color.yellow;
            GUI.Box(new Rect(x0 + leftW + gap, y0 - 34, midW, 28),
                $"YENI {pendingWeapon.WeaponName}: SAG PANELDE PARLAYAN HEDEFE TIKLA!");
            GUI.color = Color.white;
        }

        // ===== SOL: STATLAR + TRINKETLER =====
        GUI.Box(new Rect(x0, y0, leftW, h), "STATLARIN");
        float ly = y0 + 26;
        void S(string ad, string deger) { GUI.Label(new Rect(x0 + 10, ly, leftW - 20, 19), $"{ad}: {deger}"); ly += 19; }
        S("Can", $"{health.CurrentHealth:F0}/{health.MaxHealth:F0}");
        S("Zirh", $"{health.Armor:F1}");
        S("Hareket Hizi", $"{movement.MoveSpeed:F1}");
        S("Menzilli Hasar", $"{stats.BaseProjectileDamage:F1}");
        S("Yakin Dovus Hasari", $"{stats.BaseMeleeDamage:F1}");
        S("Card Counting", $"{stats.CardCounting:F1}");
        S("Mermi Hizi", $"{stats.BaseProjectileSpeed:F1}");
        S("Menzil", $"{stats.BaseProjectileRange:F1}");
        S("Menzilli Saldiri Hizi", $"x{stats.ProjectileAttackSpeed:F2}");
        S("Yakin Saldiri Hizi", $"x{stats.MeleeAttackSpeed:F2}");
        S("Crit", $"%{stats.CritChance:F1} / x{stats.CritMultiplier:F2}");
        S("Pickup Yaricapi", $"{stats.PickupRadius:F1}");
        S("Level", $"{resources.Level}");

        ly += 8;
        GUI.Label(new Rect(x0 + 10, ly, leftW - 20, 19), "-- TRINKETLERIN --"); ly += 21;
        List<TrinketDefinition> trKopya = new List<TrinketDefinition>(ownedTrinkets.Keys);
        foreach (TrinketDefinition t in trKopya)
        {
            GUI.Label(new Rect(x0 + 10, ly, leftW - 95, 19), $"{t.displayName} x{ownedTrinkets[t]}");
            GUI.enabled = !askida;
            if (GUI.Button(new Rect(x0 + leftW - 80, ly, 70, 18), $"Sat {RefundFor(t.basePrice)}"))
                TrySellTrinket(t);
            GUI.enabled = true;
            ly += 21;
        }

        // ===== ORTA: SATIS SLOTLARI + REROLL =====
        float mx = x0 + leftW + gap;
        GUI.Box(new Rect(mx, y0, midW, h), $"MAGAZA (Wave {currentWave})     Chips: {resources.Chips}");

        GUI.enabled = !askida;
        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            float rowY = y0 + 30 + i * 70;
            string label; bool buyable;

            if (s == null) continue;
            if (s.sold) { label = "--- SATILDI ---"; buyable = false; }
            else if (s.IsWeapon)
            {
                int kopya = 0;
                foreach (WeaponBase w in weaponManager.Equipped)
                    if (w.SourcePrefab == s.weaponPrefab.gameObject) kopya++;
                bool alinabilir = weaponManager.CanPurchase(s.weaponPrefab);
                label = $"[SILAH] {s.weaponPrefab.WeaponName}   [{s.price}]"
                      + (kopya > 0 ? $"   (sende: {kopya} kopya)" : "   YENI")
                      + (alinabilir ? "" : "\n(SLOT DOLU / KOPYALAR MAX)");
                buyable = alinabilir;
            }
            else if (s.IsHouseRule)
            {
                HouseRuleBase owned = houseRuleManager.FindBySource(s.hrPrefab);
                if (owned != null)
                { label = $"[KURAL] {s.hrPrefab.RuleName}  Lv{owned.Level} -> Lv{owned.Level + 1}   [{s.price}]"; buyable = true; }
                else
                {
                    bool var = houseRuleManager.HasFreeSlot;
                    label = $"[KURAL] {s.hrPrefab.RuleName} - YENI   [{s.price}]" + (var ? "" : "\n(KURAL SLOTLARIN DOLU)");
                    buyable = var;
                }
            }
            else
            {
                int sahip = ownedTrinkets.TryGetValue(s.trinket, out int n) ? n : 0;
                label = $"{s.trinket.displayName}   [{s.price}]" + (sahip > 0 ? $"  (x{sahip})" : "")
                      + $"\n{s.trinket.description}";
                buyable = true;
            }

            // Kilit dugmesi [K] - BEDAVA (v1.3)
            bool eskiEnabled = GUI.enabled;
            GUI.enabled = !askida && !s.sold;
            GUI.backgroundColor = s.locked ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(mx + midW - 52, rowY, 40, 62), s.locked ? "KILIT\nACIK" : "K"))
                s.locked = !s.locked;
            GUI.backgroundColor = Color.white;
            GUI.enabled = eskiEnabled;

            GUI.enabled = !askida && buyable && resources.Chips >= s.price && !s.sold;
            if (GUI.Button(new Rect(mx + 12, rowY, midW - 70, 62), label + (s.locked ? "   [KILITLI]" : "")))
                TryBuy(i);
            GUI.enabled = !askida;
        }

        int rCost = CurrentRefreshCost();
        GUI.enabled = !askida && resources.Chips >= rCost;
        if (GUI.Button(new Rect(mx + midW - 212, y0 + h - 50, 200, 38), $"REROLL ({rCost})"))
            TryRefresh();
        GUI.enabled = true;

        // YENI: %30 Can Yenileme Butonu (Reroll'un yanina yerlesir)
        bool canHeal = health.CurrentHealth < health.MaxHealth && resources.Chips >= healCost;
        GUI.enabled = !askida && canHeal;
        if (GUI.Button(new Rect(mx + 12, y0 + h - 50, 200, 38), $"%30 CAN YENILE ({healCost})"))
            BuyHealthRestore();
        GUI.enabled = true;

        // ===== SAG: HOUSE RULES + SILAH SLOTLARI + SAT MODU + NEXT =====
        float rx = mx + midW + gap;
        GUI.Box(new Rect(rx, y0, rightW, h), "ENVANTERIN");

        // Sat modu
        GUI.enabled = !askida;
        GUI.backgroundColor = sellMode ? new Color(1f, 0.5f, 0.4f) : Color.white;
        if (GUI.Button(new Rect(rx + 10, y0 + 26, rightW - 20, 30),
            sellMode ? "SAT MODU ACIK - iptal icin tikla" : "SAT MODU"))
            sellMode = !sellMode;
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        float ry = y0 + 64;
        GUI.Label(new Rect(rx + 10, ry, rightW - 20, 20), $"-- HOUSE RULES ({houseRuleManager.Equipped.Count}/4) --"); ry += 22;
        List<HouseRuleBase> hrKopya = new List<HouseRuleBase>(houseRuleManager.Equipped);
        foreach (HouseRuleBase r in hrKopya)
        {
            string lbl = $"{r.RuleName}" + (r.Levelable ? $" Lv{r.Level}" : "") + (r.CanSell ? "" : "  [KALICI]");
            if (sellMode && r.CanSell && !askida)
            {
                if (GUI.Button(new Rect(rx + 10, ry, rightW - 20, 24), $"SAT: {lbl}  (+{RefundFor(houseRuleBasePrice)})"))
                    TrySellHouseRule(r);
            }
            else
                GUI.Label(new Rect(rx + 10, ry, rightW - 20, 24), lbl);
            ry += 26;
        }

        ry += 8;
        GUI.Label(new Rect(rx + 10, ry, rightW - 20, 20), $"-- SILAHLAR ({weaponManager.Equipped.Count}/{weaponManager.MaxSlots}) --"); ry += 22;

        // Silah slotlari: dolu olanlar + bos olanlar (askida hedefleri BURADA parlar)
        List<WeaponBase> silahlar = new List<WeaponBase>(weaponManager.Equipped);
        bool bosSlotButonuCizildi = false;
        for (int i = 0; i < weaponManager.MaxSlots; i++)
        {
            Rect slotRect = new Rect(rx + 10, ry, rightW - 20, 26);

            if (i < silahlar.Count)
            {
                WeaponBase w = silahlar[i];
                string lbl = $"{w.WeaponName}  Lv{w.Level}";

                if (askida && weaponManager.IsUpgradableCopyOf(w, pendingWeapon))
                {
                    GUI.backgroundColor = Color.yellow;              // PARLAMA
                    if (GUI.Button(slotRect, $"{lbl}  ->  Lv{w.Level + 1}  [BURAYI YUKSELT]"))
                        PlacePendingOnCopy(w);
                    GUI.backgroundColor = Color.white;
                }
                else if (sellMode && !askida)
                {
                    if (GUI.Button(slotRect, $"SAT: {lbl}  (+{RefundFor(weaponBasePrice)})"))
                        TrySellWeapon(w);
                }
                else
                    GUI.Label(slotRect, lbl);
            }
            else
            {
                if (askida && !bosSlotButonuCizildi)
                {
                    bosSlotButonuCizildi = true;
                    GUI.backgroundColor = Color.yellow;              // PARLAMA
                    if (GUI.Button(slotRect, $"[BOS SLOT]  YENI {pendingWeapon.WeaponName} Lv1 TAK"))
                        PlacePendingOnEmptySlot();
                    GUI.backgroundColor = Color.white;
                }
                else
                    GUI.Label(slotRect, "(bos slot)");
            }
            ry += 28;
        }

        // Next wave (askidayken kilitli)
        GUI.enabled = !askida;
        if (GUI.Button(new Rect(rx + 10, y0 + h - 50, rightW - 20, 38), "SONRAKI WAVE ->"))
            closeClicked = true;
        GUI.enabled = true;
    }
}
