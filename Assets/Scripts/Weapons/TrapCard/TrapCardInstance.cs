using UnityEngine;

public class TrapCardInstance : MonoBehaviour
{
    [Header("Tetikleme")]
    [Tooltip("Merkez bolge yaricapi - 'ortasina basmak' bu daire demek")]
    [SerializeField] private float triggerRadius = 0.35f;

    private float damage;
    private float explosionRadius;
    private float lifeTimer;
    private PlayerStats stats;
    private Transform player;

    private readonly Collider2D[] hitBuffer = new Collider2D[64];
    private ContactFilter2D noFilter;

    private void Awake()
    {
        noFilter = ContactFilter2D.noFilter;
    }

    /// <summary>WeaponTrapCard kart birakirken cagirir.</summary>
    public void Arm(Vector2 position, float dmg, float radius, float lifeTime,
                    PlayerStats playerStats, Transform playerTransform)
    {
        transform.position = position;
        damage = dmg;
        explosionRadius = radius;
        lifeTimer = lifeTime;
        stats = playerStats;
        player = playerTransform;

        // Gorsel boyut = patlama yaricapiyla oranli (oyuncu alani okuyabilsin)
        transform.localScale = new Vector3(radius, radius, 1f) * 0.8f;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        // 1) Omur doldu mu? (GDD: 15 sn sonra kendiliginden patlar)
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Explode();
            return;
        }

        // 2) OYUNCU merkeze basti mi? (patlar ama oyuncu hasar YEMEZ)
        if (player != null && player.gameObject.activeInHierarchy &&
            Vector2.Distance(player.position, transform.position) <= triggerRadius)
        {
            Explode();
            return;
        }

        // 3) Bir DUSMAN merkeze basti mi?
        foreach (EnemyHealth e in EnemyHealth.ActiveEnemies)
        {
            if (Vector2.Distance(e.transform.position, transform.position) <= triggerRadius)
            {
                Explode();
                return;
            }
        }
    }

    private void Explode()
    {
        // Patlama alanindaki TUM dusmanlara hasar (oyuncuya ASLA degil - GDD)
        int count = Physics2D.OverlapCircle(transform.position, explosionRadius, noFilter, hitBuffer);
        for (int i = 0; i < count; i++)
        {
            EnemyHealth enemy = hitBuffer[i].GetComponent<EnemyHealth>();
            if (enemy == null) continue;

            float dmg = damage;
            bool isCrit = false;
            if (stats != null)
                dmg = stats.ApplyCritRoll(damage, out isCrit);

            enemy.TakeDamage(dmg, isCrit);
        }

        // Ileride: patlama efekti + ses + kucuk screen shake
        gameObject.SetActive(false);   // havuza don
    }
}
