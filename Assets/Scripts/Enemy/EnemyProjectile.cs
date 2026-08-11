using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float maxRange;
    private float damage;
    private float traveled;

    public void Launch(Vector2 startPos, Vector2 dir, float spd, float range, float dmg)
    {
        transform.position = startPos;
        direction = dir.normalized;
        speed = spd;
        maxRange = range;
        damage = dmg;
        traveled = 0f;

        transform.right = direction;   // gorsel yon
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
        // Duvara carpan mermi yok olur (dev eldiven karti vb.)
        if (other.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
            return;
        }

        // Sadece oyuncuyu ariyoruz - dusmanlar ve baska mermiler umursanmaz
        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player == null) return;

        player.TakeDamage(damage);     // i-frame'deyse zaten yok sayilir
        gameObject.SetActive(false);
    }
}
