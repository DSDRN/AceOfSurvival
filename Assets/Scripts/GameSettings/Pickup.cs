using System.Collections.Generic;
using UnityEngine;

public enum PickupType { XP, Chips, Potion, Magnet, Chest }

public class Pickup : MonoBehaviour
{
    public static readonly List<Pickup> ActivePickups = new List<Pickup>();

    [SerializeField] private PickupType type;
    [SerializeField] private float flySpeed = 9f;

    public PickupType Type => type;

    /// Wave sonu %75 kurali SADECE bunlara uygulanir
    public bool IsAutoCollectible => type == PickupType.XP || type == PickupType.Chips;

    private float value;
    private bool flying;

    private static Transform player;
    private static PlayerStats playerStats;
    private static PlayerResources playerResources;
    private static PlayerHealth playerHealth;
    private static WeaponManager weaponManager;
    private static HouseRuleManager hrManager;

    private void OnEnable() { ActivePickups.Add(this); }
    private void OnDisable() { ActivePickups.Remove(this); }

    public void Init(float pickupValue)
    {
        value = pickupValue;
        flying = false;
    }

    private void Update()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            player = p.transform;
            playerStats = p.GetComponent<PlayerStats>();
            playerResources = p.GetComponent<PlayerResources>();
            playerHealth = p.GetComponent<PlayerHealth>();
            weaponManager = p.GetComponent<WeaponManager>();
            hrManager = p.GetComponent<HouseRuleManager>();
        }
        if (!player.gameObject.activeInHierarchy) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (!flying && dist <= playerStats.PickupRadius)
            flying = true;

        if (flying)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, player.position, flySpeed * Time.deltaTime);
            if (dist < 0.3f)
                Collect(1f);
        }
    }

    public void Collect(float oran)
    {
        switch (type)
        {
            case PickupType.XP:
            case PickupType.Chips:
                playerResources?.AddPickup(type, value * oran);
                break;

            case PickupType.Potion:
                if (playerHealth != null)
                {
                    // İSTEDİĞİN GİBİ DEĞİŞTİRİLDİ: Maksimum canın %20'sini verir.
                    // PlayerHealth içindeki Mathf.Min komutu sayesinde can %100'ü asla aşamaz.
                    float healAmount = playerHealth.MaxHealth * 0.20f;
                    playerHealth.Heal(healAmount);
                }
                break;

            case PickupType.Magnet:
                MagnetEffect();
                break;

            case PickupType.Chest:
                OpenChest();
                break;
        }
        gameObject.SetActive(false);
    }

    /// MAGNET: haritadaki tum XP+chips ANINDA TAM DEGERLE toplanir (GDD 7.1)
    private void MagnetEffect()
    {
        Debug.Log("MAGNET! Her sey toplandi.");
        List<Pickup> copy = new List<Pickup>(ActivePickups);
        foreach (Pickup p in copy)
        {
            if (p != this && p.IsAutoCollectible)
                p.Collect(1f);   // %100 deger - magnet cezasiz
        }
    }

    /// KART KUTUSU: altin + sahip olunan rastgele esyaya BEDAVA seviye
    private void OpenChest()
    {
        // 1) Altin (value = spawner'in belirledigi miktar)
        int altin = Mathf.RoundToInt(value);
        playerResources?.AddBonusGold(altin);

        // 2) Bedava seviye adaylari: max olmamis silahlar + levellenebilir kurallar
        List<System.Func<bool>> adaylar = new List<System.Func<bool>>();
        if (weaponManager != null)
            foreach (WeaponBase w in weaponManager.Equipped)
                if (w.Level < WeaponBase.MaxLevel)
                    adaylar.Add(w.LevelUp);
        if (hrManager != null)
            foreach (HouseRuleBase r in hrManager.Equipped)
                if (r.Levelable && r.Level < HouseRuleBase.MaxLevel)
                    adaylar.Add(r.LevelUp);

        if (adaylar.Count > 0)
            adaylar[Random.Range(0, adaylar.Count)].Invoke();

        Debug.Log($"KART KUTUSU: +{altin} altin (run sonunda)" +
                  (adaylar.Count > 0 ? " + bedava seviye!" : ""));
    }
}