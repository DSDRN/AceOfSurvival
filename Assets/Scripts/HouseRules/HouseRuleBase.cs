using UnityEngine;

public abstract class HouseRuleBase : MonoBehaviour
{
    [Header("Kimlik")]
    [SerializeField] protected string ruleName = "House Rule";

    [Tooltip("Seviye atlayabilir mi? (Ace of Spades / HAW / LMSH: HAYIR)")]
    [SerializeField] protected bool levelable = true;

    public string RuleName => ruleName;
    public int Level { get; private set; } = 1;
    public const int MaxLevel = 5;
    public bool Levelable => levelable;
    public GameObject SourcePrefab { get; set; }

    /// Satilabilir mi? (kalici secim kurallari false doner)
    public virtual bool CanSell => true;

    protected Transform owner;
    protected PlayerStats stats;
    protected PlayerHealth health;
    protected PlayerMovement movement;
    protected WeaponManager weaponManager;

    public virtual void Init(Transform ownerT, PlayerStats s, PlayerHealth h,
                             PlayerMovement m, WeaponManager wm)
    {
        owner = ownerT; stats = s; health = h; movement = m; weaponManager = wm;
        OnLevelChanged();
    }

    public bool LevelUp()
    {
        if (!levelable || Level >= MaxLevel) return false;
        Level++;
        OnLevelChanged();
        Debug.Log($"{ruleName} seviye atladi -> Lv{Level}");
        return true;
    }

    /// Seviye degisince (ve ilk kusanimda) etkilerini uygula/tazele
    protected abstract void OnLevelChanged();

    /// Satista cagrilir: verdigin HER SEYI geri al
    public virtual void OnRemoved() { }
}
