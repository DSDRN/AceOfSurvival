using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerMovement))]
public class WeaponManager : MonoBehaviour
{
    [Header("Slot siniri (KILIT: demo 4, tam surum 6)")]
    [SerializeField] private int maxSlots = 4;

    [Header("Baslangic silah havuzu (rastgele 1'i kusanilir)")]
    [SerializeField] private WeaponBase[] startingWeapons;

    private readonly List<WeaponBase> equipped = new List<WeaponBase>();
    private PlayerStats stats;
    private PlayerMovement movement;

    public IReadOnlyList<WeaponBase> Equipped => equipped;
    public int MaxSlots => maxSlots;
    public bool HasEmptySlot => equipped.Count < maxSlots;

    /// LMSH kilidi (null = kilit yok). Tactical silahlar ASLA kilitlenmez.
    public WeaponCategory? LockedCategory { get; private set; }

    public bool IsCategoryLocked(WeaponCategory cat)
        => cat != WeaponCategory.Tactical
           && LockedCategory.HasValue && LockedCategory.Value == cat;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        if (startingWeapons != null && startingWeapons.Length > 0)
        {
            WeaponBase secilen = startingWeapons[Random.Range(0, startingWeapons.Length)];
            EquipNewCopy(secilen);
            Debug.Log($"Baslangic silahi: {secilen.WeaponName}");
        }
    }

    /// <summary>Bos slota YENI kopya takar (basarisizsa null).</summary>
    public WeaponBase EquipNewCopy(WeaponBase prefab)
    {
        if (prefab == null || !HasEmptySlot) return null;
        if (IsCategoryLocked(prefab.Category)) return null;

        WeaponBase instance = Instantiate(prefab, transform);
        instance.transform.localPosition = Vector3.zero;
        instance.SourcePrefab = prefab.gameObject;
        instance.Init(transform, stats, movement);
        equipped.Add(instance);
        Debug.Log($"Silah kusanildi: {instance.WeaponName} ({equipped.Count}/{maxSlots})");
        return instance;
    }

    /// Bu prefab'in MAX OLMAMIS kopyalarini doldurur (askida hedef listesi)
    public void GetUpgradableCopies(WeaponBase prefab, List<WeaponBase> result)
    {
        result.Clear();
        if (prefab == null) return;
        foreach (WeaponBase w in equipped)
            if (w.SourcePrefab == prefab.gameObject && w.Level < WeaponBase.MaxLevel)
                result.Add(w);
    }

    /// Bu kopya, pending silahin gecerli yukseltme hedefi mi? (UI parlatmasi)
    public bool IsUpgradableCopyOf(WeaponBase instance, WeaponBase prefab)
        => prefab != null && instance != null
           && instance.SourcePrefab == prefab.gameObject
           && instance.Level < WeaponBase.MaxLevel;

    /// <summary>
    /// Magaza butonu icin: satin alma MUMKUN mu?
    /// Kural (v1.3 Durum C): bos slot yoksa VE yukseltilebilir kopya yoksa = HAYIR.
    /// </summary>
    public bool CanPurchase(WeaponBase prefab)
    {
        if (prefab == null) return false;
        if (IsCategoryLocked(prefab.Category)) return false;
        if (HasEmptySlot) return true;

        foreach (WeaponBase w in equipped)
            if (w.SourcePrefab == prefab.gameObject && w.Level < WeaponBase.MaxLevel)
                return true;
        return false;
    }

    /// SATIS: min 1 silah kurali korunur
    public bool SellWeapon(WeaponBase instance)
    {
        if (instance == null || !equipped.Contains(instance)) return false;
        if (equipped.Count <= 1)
        {
            Debug.Log("Son silahini satamazsin!");
            return false;
        }
        equipped.Remove(instance);
        Debug.Log($"Silah satildi: {instance.WeaponName} (Lv{instance.Level})");
        Destroy(instance.gameObject);
        return true;
    }

    /// LMSH: kategorinin TUM kopyalarini soker, toplam seviyeyi doner.
    /// (Tactical parametre olarak gelemez - LMSH sadece Ranged/Melee kilitler)
    public int LockCategory(WeaponCategory cat)
    {
        LockedCategory = cat;
        int totalLevels = 0;
        for (int i = equipped.Count - 1; i >= 0; i--)
        {
            if (equipped[i].Category != cat) continue;
            totalLevels += equipped[i].Level;
            Debug.Log($"KILITLENDI: {equipped[i].WeaponName} Lv{equipped[i].Level}");
            Destroy(equipped[i].gameObject);
            equipped.RemoveAt(i);
        }
        return totalLevels;
    }

    // ---- Gecici debug listesi (sol alt) ----
    private void OnGUI()
    {
        float y = Screen.height - 25 * equipped.Count - 10;
        for (int i = 0; i < equipped.Count; i++)
            GUI.Label(new Rect(10, y + i * 25, 300, 22),
                $"[{equipped[i].WeaponName}]  Lv{equipped[i].Level}");
    }
}
