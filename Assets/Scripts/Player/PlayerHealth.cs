using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Can (balance)")]
    [SerializeField] private float maxHealth = 15f;

    [Header("Zirh (balance)")]
    [SerializeField] private float armor = 5f;
    [Tooltip("K = 25 (KILIT)")]
    [SerializeField] private float armorConstantK = 25f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public float Armor => armor;
    public bool IsDead { get; private set; }

    /// JOKER KANCASI: true donerse hasar ISLENMEZ (yansitmayi Joker yapar)
    public System.Func<float, EnemyHealth, bool> DamageInterceptor;

    private PlayerMovement movement;
    private SpriteRenderer sr;
    private bool glassCannon;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        sr = GetComponentInChildren<SpriteRenderer>();
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float rawDamage, EnemyHealth attacker = null, string sourceName = null)
    {
        if (IsDead) return;
        if (movement.IsInvincible) return;

        if (DamageInterceptor != null && DamageInterceptor(rawDamage, attacker))
            return;

        float finalDamage;
        if (armor >= 0f)
            finalDamage = rawDamage * (armorConstantK / (armorConstantK + armor));
        else
            finalDamage = rawDamage * (1f + Mathf.Abs(armor) * 0.05f);

        CurrentHealth -= finalDamage;
        movement.TriggerHurtIFrames();

        RunTracker.RecordDamageTaken(
            sourceName ?? (attacker != null ? attacker.gameObject.name : null),
            finalDamage);

        DamageNumberManager.Instance?.Show(transform.position, finalDamage, false,
                                           DamageNumberManager.OyuncuRenk);

        if (CurrentHealth <= 0f)
            Die();
    }

    // HATALI OLAN İLK FONKSİYONU SİLDİK, SADECE GÜVENLİ OLAN KALDI!
    public void Heal(float amount)
    {
        if (IsDead) return;

        // Mathf.Min KORUMASI: İksir veya mağaza ne kadar can verirse versin, MaxHealth'i geçemez!
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
    }

    /// Level-up VE trinketler icin: max can degisir.
    public void AddMaxHealth(float amount)
    {
        if (glassCannon) return;
        maxHealth = Mathf.Max(1f, maxHealth + amount);
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 1f, maxHealth);
    }

    public void AddArmor(float amount)
    {
        armor += amount;
    }

    public void ForceGlassCannon()
    {
        glassCannon = true;
        maxHealth = 1f;
        CurrentHealth = 1f;
        Debug.Log("GLASS CANNON: max can 1'e sabitlendi!");
    }

    private void Die()
    {
        IsDead = true;
        Debug.Log("OLDUN!");
        gameObject.SetActive(false);
        // Karakter öldüğünde Game Over ekranını ÇAĞIR:
        FindFirstObjectByType<GameOverManager>().TriggerGameOver();
    }
}