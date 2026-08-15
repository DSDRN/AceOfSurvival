using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProjectileCard : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private float damage;
    private bool isCrit;
    private float traveled;

    private float currentKnockback;

    public void Launch(Vector2 startPos, Vector2 dir, float spd, float range, float dmg, bool crit, float knockback)
    {
        transform.position = startPos;
        direction = dir.normalized;
        speed = spd;
        maxRange = range;
        damage = dmg;
        isCrit = crit;
        traveled = 0f;

        currentKnockback = knockback;

        transform.right = direction;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        traveled += step;

        if (traveled >= maxRange)
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
            return;
        }

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null) return;

        // 1. Hasari ver
        enemy.TakeDamage(damage, isCrit);

        // 2. Geri Tepmeyi ve Stagger'i Uygula
        enemy.ApplyKnockback(direction, currentKnockback);

        // YENI: Kartlar dusmani cok kisa sureligine (0.15 sn) sersemletir.
        enemy.ApplyStagger(0.15f);

        // Eger dusmanda Rigidbody2D varsa (Eski Itme formulu yedek olarak kalabilir)
        if (other.TryGetComponent<Rigidbody2D>(out Rigidbody2D enemyRb))
        {
            enemyRb.AddForce(direction * currentKnockback, ForceMode2D.Impulse);
        }

        // 3. Mermiyi yokedip islemi bitir
        gameObject.SetActive(false);
    }
}