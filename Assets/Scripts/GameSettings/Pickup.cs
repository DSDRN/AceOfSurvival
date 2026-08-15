using System.Collections.Generic;
using UnityEngine;

public enum PickupType { XP, Chips, Potion, Magnet, Chest }

public class Pickup : MonoBehaviour
{
    public static readonly List<Pickup> ActivePickups = new List<Pickup>();

    [SerializeField] private PickupType type;
    [SerializeField] private float flySpeed = 9f;

    public PickupType Type => type;
    public bool IsAutoCollectible => type == PickupType.XP || type == PickupType.Chips;

    private float value;
    private bool flying;
    private float currentCollectRatio = 1f; // Vakum durumunda bu oran %0.75 olur

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
        currentCollectRatio = 1f; // Normal toplamada oran hep 1
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

        // Vakum modunda degilse ve yakinsa, normal ucusu baslat
        if (!flying && dist <= playerStats.PickupRadius)
            flying = true;

        if (flying)
        {
            // Hizlanarak karaktere gitme (Tatmin edici vakum hissi icin flySpeed artabilir)
            flySpeed += Time.deltaTime * 15f;
            transform.position = Vector2.MoveTowards(transform.position, player.position, flySpeed * Time.deltaTime);

            if (dist < 0.3f)
                Collect(currentCollectRatio);
        }
    }

    // WAVE SONU VAKUM TETIKLEYICISI
    public void StartVacuum(float ratio)
    {
        if (!IsAutoCollectible || flying) return;
        currentCollectRatio = ratio;
        flying = true; // Ucusu zorla baslat
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

    private void MagnetEffect()
    {
        Debug.Log("MAGNET! Her sey toplandi.");
        List<Pickup> copy = new List<Pickup>(ActivePickups);
        foreach (Pickup p in copy)
        {
            // Magnet de vakum animasyonu baslatir (aninda toplamak yerine)
            if (p != this && p.IsAutoCollectible)
                p.StartVacuum(1f);
        }
    }

    private void OpenChest()
    {
        int altin = Mathf.RoundToInt(value);
        playerResources?.AddBonusGold(altin);

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