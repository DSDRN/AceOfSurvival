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
    [SerializeField] private int healCost = 50;
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
        public bool locked;
        public bool IsWeapon => weaponPrefab != null;
        public bool IsHouseRule => hrPrefab != null;
    }

    private readonly Slot[] slots = new Slot[5];
    private int refreshesUsed;
    private int currentWave;
    private bool closeClicked;
    private bool sellMode;
    private WeaponBase pendingWeapon;
    private readonly List<WeaponBase> pendingCopies = new();
    private readonly Dictionary<TrinketDefinition, int> ownedTrinkets = new();

    public IEnumerator RunShop(int waveNumber)
    {
        currentWave = waveNumber;
        refreshesUsed = 0;
        sellMode = false;
        pendingWeapon = null;

        RollSlots(respectLocks: true);

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
                continue;
            slots[i] = RollOne();
        }
    }

    private Slot RollOne()
    {
        List<WeaponBase> silahlar = new();
        foreach (WeaponBase w in allWeapons)
            if (w != null && !weaponManager.IsCategoryLocked(w.Category))
                silahlar.Add(w);

        List<HouseRuleBase> kurallar = new();
        foreach (HouseRuleBase hr in allHouseRules)
        {
            if (hr == null) continue;
            HouseRuleBase owned = houseRuleManager.FindBySource(hr);
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
        return new Slot { trinket = t, price = CalculatePrice(t != null ? t.basePrice : 0) };
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
        if (s == null || s.sold || resources.Chips < s.price || pendingWeapon != null) return;

        if (s.IsWeapon)
        {
            if (!weaponManager.CanPurchase(s.weaponPrefab)) return;

            resources.SpendChips(s.price);
            s.sold = true;
            s.locked = false;

            weaponManager.GetUpgradableCopies(s.weaponPrefab, pendingCopies);
            bool bosVar = weaponManager.HasEmptySlot;
            int hedefSayisi = pendingCopies.Count + (bosVar ? 1 : 0);

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
                pendingWeapon = s.weaponPrefab;
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
            if (s.trinket == null) return;
            resources.SpendChips(s.price);
            ApplyTrinket(s.trinket, +1f);
            ownedTrinkets[s.trinket] = ownedTrinkets.TryGetValue(s.trinket, out int n) ? n + 1 : 1;
            s.sold = true; s.locked = false;
        }

        bool allSold = true;
        foreach (Slot slot in slots)
            if (slot != null && !slot.sold) { allSold = false; break; }
        if (allSold)
        {
            refreshesUsed++;
            RollSlots(respectLocks: true);
        }
    }

    private void PlacePendingOnCopy(WeaponBase copy)
    {
        if (pendingWeapon == null || copy == null) return;
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
        if (resources.Chips >= healCost && health.CurrentHealth < health.MaxHealth)
        {
            resources.SpendChips(healCost);
            float healAmount = health.MaxHealth * 0.3f;
            health.Heal(healAmount);
        }
    }

    // ---------------- SATIS ----------------
    private void TrySellWeapon(WeaponBase instance)
    {
        if (instance == null || !weaponManager.SellWeapon(instance)) return;
        resources.AddChipsRefund(RefundFor(weaponBasePrice));
    }

    private void TrySellHouseRule(HouseRuleBase instance)
    {
        if (instance == null || !houseRuleManager.Sell(instance)) return;
        resources.AddChipsRefund(RefundFor(houseRuleBasePrice));
    }

    private void TrySellTrinket(TrinketDefinition t)
    {
        if (t == null || !ownedTrinkets.TryGetValue(t, out int n) || n <= 0) return;
        ApplyTrinket(t, -1f);
        if (n == 1) ownedTrinkets.Remove(t);
        else ownedTrinkets[t] = n - 1;
        resources.AddChipsRefund(RefundFor(t.basePrice));
    }

    // ---------------- TRINKET ----------------
    private void ApplyTrinket(TrinketDefinition t, float sign)
    {
        if (t == null || t.modifiers == null) return;
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

    // ================= 3 SUTUNLU EKRAN (1080p Ölçekli Kalıcı Çözüm) =================
    private void OnGUI()
    {
        if (!IsOpen) return;
        if (health == null || stats == null || resources == null || movement == null || weaponManager == null || houseRuleManager == null) return;

        // 1920x1080 ZORUNLU EKRAN ÖLÇEKLEMESİ (Tüm arayüzü cam gibi büyütür)
        Vector3 scale = new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

        float leftW = 350f, midW = 600f, rightW = 400f, gap = 20f;
        float totalW = leftW + midW + rightW + gap * 2;
        float h = 800f;

        // Matrix sayesinde ekranı 1920x1080 farz ederek koordinat veriyoruz
        float x0 = (1920f - totalW) / 2f;
        float y0 = (1080f - h) / 2f;

        bool askida = pendingWeapon != null;

        // Askida bandi
        if (askida)
        {
            GUI.color = Color.yellow;
            GUI.Box(new Rect(x0 + leftW + gap, y0 - 40, midW, 35),
                $"YENI {pendingWeapon.WeaponName}: SAG PANELDE PARLAYAN HEDEFE TIKLA!");
            GUI.color = Color.white;
        }

        // ===== SOL: STATLAR + TRINKETLER =====
        GUI.Box(new Rect(x0, y0, leftW, h), "STATLARIN");
        float ly = y0 + 35;
        void S(string ad, string deger) { GUI.Label(new Rect(x0 + 15, ly, leftW - 30, 25), $"{ad}: {deger}"); ly += 25; }
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

        ly += 15;
        GUI.Label(new Rect(x0 + 15, ly, leftW - 30, 25), "-- TRINKETLERIN --"); ly += 30;
        List<TrinketDefinition> trKopya = new List<TrinketDefinition>(ownedTrinkets.Keys);
        foreach (TrinketDefinition t in trKopya)
        {
            if (t == null) continue;
            GUI.Label(new Rect(x0 + 15, ly, leftW - 120, 25), $"{t.displayName} x{ownedTrinkets[t]}");
            GUI.enabled = !askida;
            if (GUI.Button(new Rect(x0 + leftW - 100, ly, 85, 25), $"Sat {RefundFor(t.basePrice)}"))
                TrySellTrinket(t);
            GUI.enabled = true;
            ly += 30;
        }

        // ===== ORTA: SATIS SLOTLARI + REROLL =====
        float mx = x0 + leftW + gap;
        GUI.Box(new Rect(mx, y0, midW, h), $"MAGAZA (Wave {currentWave})    Chips: {resources.Chips}");

        GUI.enabled = !askida;
        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            float rowY = y0 + 40 + i * 100;
            string label; bool buyable;

            if (s == null) continue;
            if (s.sold) { label = "--- SATILDI ---"; buyable = false; }
            else if (s.IsWeapon)
            {
                int kopya = 0;
                foreach (WeaponBase w in weaponManager.Equipped)
                    if (w != null && w.SourcePrefab == s.weaponPrefab.gameObject) kopya++;
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
                if (s.trinket == null) { label = "Hata: Trinket Bos!"; buyable = false; }
                else
                {
                    int sahip = ownedTrinkets.TryGetValue(s.trinket, out int n) ? n : 0;
                    label = $"{s.trinket.displayName}   [{s.price}]" + (sahip > 0 ? $"  (x{sahip})" : "")
                          + $"\n{s.trinket.description}";
                    buyable = true;
                }
            }

            bool eskiEnabled = GUI.enabled;
            GUI.enabled = !askida && !s.sold;
            GUI.backgroundColor = s.locked ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(mx + midW - 70, rowY, 55, 85), s.locked ? "KILIT\nACIK" : "K"))
                s.locked = !s.locked;
            GUI.backgroundColor = Color.white;
            GUI.enabled = eskiEnabled;

            GUI.enabled = !askida && buyable && resources.Chips >= s.price && !s.sold;
            if (GUI.Button(new Rect(mx + 15, rowY, midW - 95, 85), label + (s.locked ? "   [KILITLI]" : "")))
                TryBuy(i);
            GUI.enabled = !askida;
        }

        int rCost = CurrentRefreshCost();
        GUI.enabled = !askida && resources.Chips >= rCost;
        if (GUI.Button(new Rect(mx + midW - 280, y0 + h - 60, 260, 45), $"REROLL ({rCost})"))
            TryRefresh();
        GUI.enabled = true;

        bool canHeal = health.CurrentHealth < health.MaxHealth && resources.Chips >= healCost;
        GUI.enabled = !askida && canHeal;
        if (GUI.Button(new Rect(mx + 15, y0 + h - 60, 260, 45), $"%30 CAN YENILE ({healCost})"))
            BuyHealthRestore();
        GUI.enabled = true;

        // ===== SAG: HOUSE RULES + SILAH SLOTLARI + SAT MODU + NEXT =====
        float rx = mx + midW + gap;
        GUI.Box(new Rect(rx, y0, rightW, h), "ENVANTERIN");

        GUI.enabled = !askida;
        GUI.backgroundColor = sellMode ? new Color(1f, 0.5f, 0.4f) : Color.white;
        if (GUI.Button(new Rect(rx + 15, y0 + 35, rightW - 30, 40),
            sellMode ? "SAT MODU ACIK - iptal icin tikla" : "SAT MODU"))
            sellMode = !sellMode;
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        float ry = y0 + 90;
        GUI.Label(new Rect(rx + 15, ry, rightW - 30, 25), $"-- HOUSE RULES ({houseRuleManager.Equipped.Count}/4) --"); ry += 30;
        List<HouseRuleBase> hrKopya = new List<HouseRuleBase>(houseRuleManager.Equipped);
        foreach (HouseRuleBase r in hrKopya)
        {
            if (r == null) continue;
            string lbl = $"{r.RuleName}" + (r.Levelable ? $" Lv{r.Level}" : "") + (r.CanSell ? "" : "  [KALICI]");
            if (sellMode && r.CanSell && !askida)
            {
                if (GUI.Button(new Rect(rx + 15, ry, rightW - 30, 30), $"SAT: {lbl}  (+{RefundFor(houseRuleBasePrice)})"))
                    TrySellHouseRule(r);
            }
            else
                GUI.Label(new Rect(rx + 15, ry, rightW - 30, 30), lbl);
            ry += 35;
        }

        ry += 15;
        GUI.Label(new Rect(rx + 15, ry, rightW - 30, 25), $"-- SILAHLAR ({weaponManager.Equipped.Count}/{weaponManager.MaxSlots}) --"); ry += 30;

        List<WeaponBase> silahlar = new List<WeaponBase>(weaponManager.Equipped);
        bool bosSlotButonuCizildi = false;
        for (int i = 0; i < weaponManager.MaxSlots; i++)
        {
            Rect slotRect = new Rect(rx + 15, ry, rightW - 30, 35);

            if (i < silahlar.Count)
            {
                WeaponBase w = silahlar[i];
                if (w == null) continue;
                string lbl = $"{w.WeaponName}  Lv{w.Level}";

                if (askida && weaponManager.IsUpgradableCopyOf(w, pendingWeapon))
                {
                    GUI.backgroundColor = Color.yellow;
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
                    GUI.backgroundColor = Color.yellow;
                    if (GUI.Button(slotRect, $"[BOS SLOT]  YENI {pendingWeapon.WeaponName} Lv1 TAK"))
                        PlacePendingOnEmptySlot();
                    GUI.backgroundColor = Color.white;
                }
                else
                    GUI.Label(slotRect, "(bos slot)");
            }
            ry += 40;
        }

        GUI.enabled = !askida;
        if (GUI.Button(new Rect(rx + 15, y0 + h - 60, rightW - 30, 45), "SONRAKI WAVE ->"))
            closeClicked = true;
        GUI.enabled = true;
    }
}