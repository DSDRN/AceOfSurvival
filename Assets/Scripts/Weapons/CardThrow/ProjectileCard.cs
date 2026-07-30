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
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null) return;    // dusman degilse (duvar vb.) simdilik delip gec

        enemy.TakeDamage(damage, isCrit);

        // Eger dusmanda Rigidbody2D varsa, merminin gittigi yone dogru it:
        if (other.TryGetComponent<Rigidbody2D>(out Rigidbody2D enemyRb))
        {
            // Kartlar Rigidbody kullanmadigi icin dogrudan ucus yonumuzu (direction) kullaniyoruz
            enemyRb.AddForce(direction * currentKnockback, ForceMode2D.Impulse);
        }

        gameObject.SetActive(false);  // kart isini yapti, havuza don
    }
}