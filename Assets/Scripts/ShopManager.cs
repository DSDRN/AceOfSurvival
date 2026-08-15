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
        {
            if (closeClicked && pendingWeapon != null)
            {
                Debug.LogWarning("Once aldigin silahi yerlestir!");
                closeClicked = false;
            }
            yield return null;
        }

        Time.timeScale = 1f;
        IsOpen = false;
    }

    private void RollSlots(bool respectLocks)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (respectLocks && slots[i] != null && slots[i].locked && !slots[i].sold)
                continue;
            slots[i] = RollOne() ?? new Slot { sold = true };
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

        if (toplam == 0) return null;

        int roll = Random.Range(0, toplam);

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

    // YENI FONKSIYON: Silahtan verileri alip oyuncu statlariyla carparak GERCEK DPS'i hesaplar.
    private string GetWeaponTooltip(WeaponBase w)
    {
        if (w == null) return "";
        float baseDmg = w.GetCurrentDamage();
        float baseCd = w.GetCurrentCooldown();
        float speedMod = 1f;
        float extraDmg = 0f;

        if (stats != null)
        {
            if (w.Category == WeaponCategory.Ranged)
            {
                extraDmg = stats.BaseProjectileDamage;
                speedMod = stats.ProjectileAttackSpeed;
            }
            else if (w.Category == WeaponCategory.Melee)
            {
                extraDmg = stats.BaseMeleeDamage;
                speedMod = stats.MeleeAttackSpeed;
            }
            else if (w.Category == WeaponCategory.Tactical)
            {
                extraDmg = stats.CardCounting;
                speedMod = 1f; // Taktiksel silahlar hizdan etkilenmez
            }
        }

        float totalDmg = baseDmg + extraDmg;
        float realCd = Mathf.Max(0.01f, baseCd / speedMod);
        float dps = totalDmg / realCd;

        return $"{w.WeaponName}\nTur: {w.Category}\nSeviye: {w.Level} / {WeaponBase.MaxLevel}\nHasar: {totalDmg:F1}\nAtis Araligi: {realCd:F2} sn\nDPS: ~{dps:F1}";
    }

    private void OnGUI()
    {
        if (!IsOpen) return;
        if (health == null || stats == null || resources == null || movement == null || weaponManager == null || houseRuleManager == null) return;

        Vector3 scale = new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

        float leftW = 350f, midW = 600f, rightW = 400f, gap = 20f;
        float totalW = leftW + midW + rightW + gap * 2;
        float h = 800f;

        float x0 = (1920f - totalW) / 2f;
        float y0 = (1080f - h) / 2f;

        bool askida = pendingWeapon != null;

        if (askida)
        {
            GUI.color = Color.yellow;
            GUI.Box(new Rect(x0 + leftW + gap, y0 - 40, midW, 35), $"YENI {pendingWeapon.WeaponName}: SAG PANELDE PARLAYAN HEDEFE TIKLA!");
            GUI.color = Color.white;
        }

        GUI.Box(new Rect(x0, y0, leftW, h), "STATLARIN");
        float ly = y0 + 35;

        void S(string ad, string deger, string aciklama)
        {
            GUI.Label(new Rect(x0 + 15, ly, leftW - 30, 25), new GUIContent($"{ad}: {deger}", aciklama));
            ly += 25;
        }

        S("Can", $"{health.CurrentHealth:F0}/{health.MaxHealth:F0}", "Maksimum canin ve su anki canin.");
        S("Zirh", $"{health.Armor:F1}", "Gelen hasari belirli bir yuzdede emerek azaltir.");
        S("Hareket Hizi", $"{movement.MoveSpeed:F1}", "Karakterin arenadaki yurume hizi.");
        S("Menzilli Hasar", $"{stats.BaseProjectileDamage:F1}", "Kart ve benzeri firlatilan mermilerin taban hasari.");
        S("Yakin Dovus Hasari", $"{stats.BaseMeleeDamage:F1}", "Zincir ve diger yakin dovus silahlarinin taban hasari.");
        S("Card Counting", $"{stats.CardCounting:F1}", "Trap Card (Tuzak Karti) gibi taktiksel silahlarin hasarini artirir.");
        S("Mermi Hizi", $"{stats.BaseProjectileSpeed:F1}", "Menzilli silahlarin gidis hizi. Ne kadar hizliysa o kadar uzaga ulasir.");
        S("Menzil", $"{stats.BaseProjectileRange:F1}", "Menzilli mermilerin ulasabilecegi maksimum uzaklik.");
        S("Menzilli Saldiri Hizi", $"x{stats.ProjectileAttackSpeed:F2}", "Menzilli silahlarin atis sikligini artirir (bekleme suresini dusurur).");
        S("Yakin Saldiri Hizi", $"x{stats.MeleeAttackSpeed:F2}", "Yakin dovus silahlarinin vurus sikligini artirir.");
        S("Crit", $"%{stats.CritChance:F1} / x{stats.CritMultiplier:F2}", "Kritik vurma sansin ve vurdugunda hasari kacla carpacagi.");
        S("Pickup Yaricapi", $"{stats.PickupRadius:F1}", "Etraftaki tecrube puani (XP) ve chipleri toplama mesafen.");
        S("Level", $"{resources.Level}", "Karakterin mevcut seviyesi. Her 5 seviyede bir otomatik %2 Kritik Sans kazanirsin!");

        ly += 15;
        GUI.Label(new Rect(x0 + 15, ly, leftW - 30, 25), "-- TRINKETLERIN --"); ly += 30;
        List<TrinketDefinition> trKopya = new List<TrinketDefinition>(ownedTrinkets.Keys);
        foreach (TrinketDefinition t in trKopya)
        {
            if (t == null) continue;
            GUI.Label(new Rect(x0 + 15, ly, leftW - 120, 25), new GUIContent($"{t.displayName} x{ownedTrinkets[t]}", t.description));
            GUI.enabled = !askida;
            if (GUI.Button(new Rect(x0 + leftW - 100, ly, 85, 25), new GUIContent($"Sat {RefundFor(t.basePrice)}", "Bu trinket'i satarak altin iadesi alirsin.")))
                TrySellTrinket(t);
            GUI.enabled = true;
            ly += 30;
        }

        float mx = x0 + leftW + gap;
        GUI.Box(new Rect(mx, y0, midW, h), $"MAGAZA (Wave {currentWave})   Chips: {resources.Chips}");

        GUI.enabled = !askida;
        for (int i = 0; i < slots.Length; i++)
        {
            Slot s = slots[i];
            float rowY = y0 + 40 + i * 100;
            string label; bool buyable;
            string tooltip = "";

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

                // YENI: Magaza silahlarina (sol panel) detayli tooltip eklendi
                tooltip = GetWeaponTooltip(s.weaponPrefab) + "\n\n(Bu silahi satin alir veya mevcut bir kopyasini seviye atlatir.)";
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
                tooltip = "Oyunun kurallarini degistiren kalici guclendirme.";
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
                    tooltip = s.trinket.description;
                }
            }

            bool eskiEnabled = GUI.enabled;
            GUI.enabled = !askida && !s.sold;
            GUI.backgroundColor = s.locked ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(mx + midW - 70, rowY, 55, 85), new GUIContent(s.locked ? "KILIT\nACIK" : "K", "Bu esyayi bir sonraki wave icin dondurur ve kaybolmasini engeller.")))
                s.locked = !s.locked;
            GUI.backgroundColor = Color.white;
            GUI.enabled = eskiEnabled;

            GUI.enabled = !askida && buyable && resources.Chips >= s.price && !s.sold;
            if (GUI.Button(new Rect(mx + 15, rowY, midW - 95, 85), new GUIContent(label + (s.locked ? "   [KILITLI]" : ""), tooltip)))
                TryBuy(i);
            GUI.enabled = !askida;
        }

        int rCost = CurrentRefreshCost();
        GUI.enabled = !askida && resources.Chips >= rCost;
        if (GUI.Button(new Rect(mx + midW - 280, y0 + h - 60, 260, 45), new GUIContent($"REROLL ({rCost})", "Magazadaki esyalari yeni rastgele esyalarla degistirir.")))
            TryRefresh();
        GUI.enabled = true;

        bool canHeal = health.CurrentHealth < health.MaxHealth && resources.Chips >= healCost;
        GUI.enabled = !askida && canHeal;
        if (GUI.Button(new Rect(mx + 15, y0 + h - 60, 260, 45), new GUIContent($"%30 CAN YENILE ({healCost})", "Maksimum caninin %30'unu aninda doldurur.")))
            BuyHealthRestore();
        GUI.enabled = true;

        float rx = mx + midW + gap;
        GUI.Box(new Rect(rx, y0, rightW, h), "ENVANTERIN");

        GUI.enabled = !askida;
        GUI.backgroundColor = sellMode ? new Color(1f, 0.5f, 0.4f) : Color.white;
        if (GUI.Button(new Rect(rx + 15, y0 + 35, rightW - 30, 40), new GUIContent(sellMode ? "SAT MODU ACIK - iptal icin tikla" : "SAT MODU", "Sahip oldugun silahlari veya kurallari %50 fiyata satmani saglar.")))
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
            string hrTooltip = $"Mevcut Seviye: {r.Level}\nOyunun genel kurallarini degistirir.";

            if (sellMode && r.CanSell && !askida)
            {
                if (GUI.Button(new Rect(rx + 15, ry, rightW - 30, 30), new GUIContent($"SAT: {lbl}  (+{RefundFor(houseRuleBasePrice)})", "Bu kurali yari fiyatina satar.")))
                    TrySellHouseRule(r);
            }
            else
                GUI.Label(new Rect(rx + 15, ry, rightW - 30, 30), new GUIContent(lbl, hrTooltip));
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

                // YENI (TOOLTIP): Envanterdeki silahlara da ayni gercekci istatistik eklendi
                string wTooltip = GetWeaponTooltip(w);

                if (askida && pendingWeapon != null && weaponManager.IsUpgradableCopyOf(w, pendingWeapon))
                {
                    GUI.backgroundColor = Color.yellow;
                    if (GUI.Button(slotRect, new GUIContent($"{lbl}  ->  Lv{w.Level + 1}  [BURAYI YUKSELT]", wTooltip)))
                        PlacePendingOnCopy(w);
                    GUI.backgroundColor = Color.white;
                }
                else if (sellMode && !askida)
                {
                    if (GUI.Button(slotRect, new GUIContent($"SAT: {lbl}  (+{RefundFor(weaponBasePrice)})", "Bu silahi yari fiyatina satar ve slotu bosaltir.")))
                        TrySellWeapon(w);
                }
                else
                    GUI.Label(slotRect, new GUIContent(lbl, wTooltip));
            }
            else
            {
                if (askida && pendingWeapon != null && !bosSlotButonuCizildi)
                {
                    bosSlotButonuCizildi = true;
                    GUI.backgroundColor = Color.yellow;
                    if (GUI.Button(slotRect, new GUIContent($"[BOS SLOT]  YENI {pendingWeapon.WeaponName} Lv1 TAK", "Satin aldigin silahi bu bos slota yerlestir.")))
                        PlacePendingOnEmptySlot();
                    GUI.backgroundColor = Color.white;
                }
                else
                    GUI.Label(slotRect, new GUIContent("(bos slot)", "Satin alinan yeni bir silah buraya yerlesir."));
            }
            ry += 40;
        }

        GUI.enabled = !askida;
        if (GUI.Button(new Rect(rx + 15, y0 + h - 60, rightW - 30, 45), new GUIContent("SONRAKI WAVE ->", "Hazirliklarini bitirip savasa devam et.")))
            closeClicked = true;
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(GUI.tooltip))
        {
            Vector2 mousePos = Event.current.mousePosition;
            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            GUIContent tooltipContent = new GUIContent(GUI.tooltip);
            Vector2 size = GUI.skin.box.CalcSize(tooltipContent);
            GUI.Box(new Rect(mousePos.x + 15, mousePos.y + 15, size.x + 20, size.y + 10), GUI.tooltip);
            GUI.backgroundColor = Color.white;
        }
    }
}