using UnityEngine;

public enum WeaponCategory { Ranged, Melee, Tactical }

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Kimlik")]
    [SerializeField] protected string weaponName = "Silah";

    [Tooltip("Ranged / Melee / Tactical (Trap Card = Tactical!)")]
    [SerializeField] protected WeaponCategory category = WeaponCategory.Ranged;

    public string WeaponName => weaponName;
    public WeaponCategory Category => category;
    public int Level { get; private set; } = 1;
    public const int MaxLevel = 5;

    public GameObject SourcePrefab { get; set; }

    protected Transform owner;
    protected PlayerStats stats;
    protected PlayerMovement movement;

    public virtual void Init(Transform ownerTransform, PlayerStats playerStats, PlayerMovement playerMovement)
    {
        owner = ownerTransform;
        stats = playerStats;
        movement = playerMovement;
        OnLevelChanged();
    }

    public bool LevelUp()
    {
        if (Level >= MaxLevel) return false;
        Level++;
        OnLevelChanged();
        Debug.Log($"{weaponName} seviye atladi -> Lv{Level}");
        return true;
    }

    protected abstract void OnLevelChanged();

    // YENI: Tooltip ve DPS hesabi icin hasar ve cooldown degerlerini disari aktaran metotlar
    public abstract float GetCurrentDamage();
    public abstract float GetCurrentCooldown();
}