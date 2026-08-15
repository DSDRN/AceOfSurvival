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
        if (other.CompareTag("Wall"))
        {
            gameObject.SetActive(false);
            return;
        }

        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player == null) return;

        // YENI: Merminin adi kaynak (sourceName) olarak gider (Orn: "Krupiye Karti")
        player.TakeDamage(damage, null, gameObject.name);
        gameObject.SetActive(false);
    }
}
