using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(WeaponManager))]
public class HouseRuleManager : MonoBehaviour
{
    [Header("Slot siniri (GDD - KILIT: demo 4, tam surum 6)")]
    [SerializeField] private int maxSlots = 4;

    private readonly List<HouseRuleBase> equipped = new List<HouseRuleBase>();
    private PlayerStats stats;
    private PlayerHealth health;
    private PlayerMovement movement;
    private WeaponManager weaponManager;

    public IReadOnlyList<HouseRuleBase> Equipped => equipped;
    public bool HasFreeSlot => equipped.Count < maxSlots;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        weaponManager = GetComponent<WeaponManager>();
    }

    public HouseRuleBase FindBySource(HouseRuleBase prefab)
    {
        foreach (HouseRuleBase r in equipped)
            if (r.SourcePrefab == prefab.gameObject)
                return r;
        return null;
    }

    public bool TryEquipOrLevelUp(HouseRuleBase rulePrefab)
    {
        if (rulePrefab == null) return false;

        HouseRuleBase owned = FindBySource(rulePrefab);
        if (owned != null)
            return owned.LevelUp();   // seviyesizse false = para cekilmez

        if (equipped.Count >= maxSlots)
        {
            Debug.Log($"House rule slotlari dolu ({maxSlots})!");
            return false;
        }

        HouseRuleBase instance = Instantiate(rulePrefab, transform);
        instance.transform.localPosition = Vector3.zero;
        instance.SourcePrefab = rulePrefab.gameObject;
        instance.Init(transform, stats, health, movement, weaponManager);
        equipped.Add(instance);

        Debug.Log($"House rule kusanildi: {instance.RuleName} ({equipped.Count}/{maxSlots})");
        return true;
    }

    public bool Sell(HouseRuleBase instance)
    {
        if (instance == null || !equipped.Contains(instance)) return false;
        if (!instance.CanSell)
        {
            Debug.Log($"{instance.RuleName} satilamaz (kalici secim)!");
            return false;
        }

        instance.OnRemoved();          // etkileri geri al
        equipped.Remove(instance);
        Debug.Log($"House rule satildi: {instance.RuleName}");
        Destroy(instance.gameObject);
        return true;
    }
}
