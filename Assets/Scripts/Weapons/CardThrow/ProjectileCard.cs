using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProjectileCard : MonoBehaviour
{
    // Bu degerler her atista silah tarafindan Launch() ile set edilir -
    // Inspector'da ayar yok, cunku kaynak PlayerStats + silah seviyesi.
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private float damage;
    private bool isCrit;
    private float traveled;   // simdiye kadar uculan mesafe

    private float currentKnockback; // Silahin verdigi itme gucunu burada saklariz

    /// <summary>Silah bunu cagirarak karti firlatir.</summary>
    public void Launch(Vector2 startPos, Vector2 dir, float spd, float range, float dmg, bool crit, float knockback)
    {
        transform.position = startPos;
        direction = dir.normalized;
        speed = spd;
        maxRange = range;
        damage = dmg;
        isCrit = crit;
        traveled = 0f;

        currentKnockback = knockback; // Gelen itme degerini kaydet

        // Kartin "sagi" ucus yonune baksin (gorsel donme - kart yan ucmasin)
        transform.right = direction;

        gameObject.SetActive(true);   // havuzdan uyanis
    }

    private void Update()
    {
        // Bu kare ne kadar yol alacagiz?
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        traveled += step;

        // GDD: menzil dolunca mermi yok olur (havuza doner)
        if (traveled >= maxRange)
            gameObject.SetActive(false);
    }

    // OnTriggerEnter2D: collider'imiz "Is Trigger" isaretli oldugu icin
    // fiziksel carpma yerine "icinden gecerken haber ver" calisir.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Duvara carpan mermi yok olur (dev eldiven karti vb.)
        if (other.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
            return;
        }

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null) return;    // dusman degilse (duvar vb.) simdilik delip gec

        // 1. Hasari ver
        enemy.TakeDamage(damage, isCrit);

        // 2. Geri Tepmeyi (Knockback) Uygula (ARTIK KENDI FONKSIYONUMUZU KULLANIYORUZ!)
        enemy.ApplyKnockback(direction, currentKnockback);

        // 3. Mermiyi yokedip (havuza dondurup) islemi bitir
        gameObject.SetActive(false);
    }
}