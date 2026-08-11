using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChaser : MonoBehaviour
{
    [Header("Hareket")]
    [Tooltip("Birim/sn. Oyuncu 4.5 - dusman ondan YAVAS olmali ki kacilabilsin. Sarhos Kumarbaz onerisi: 3.5")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Saldiri")]
    [Tooltip("Balance tablosu: Sarhos Kumarbaz hasari = 1 (ham hasar; zirh formulu PlayerHealth'te uygulanir)")]
    [SerializeField] private float contactDamage = 1f;

    private Rigidbody2D rb;
    private Transform target;          // kovalanacak hedef (oyuncu)
    private SpriteRenderer sr;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // Oyuncuyu "Player" TAG'i ile buluruz (kurulumda tag atamayi unutma!).
        // Neden tag? Sahnede yuzlerce obje olacak; isimle aramak yavas ve kirilgan.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
        else
            Debug.LogWarning("EnemyChaser: 'Player' tag'li obje bulunamadi! Player objesine tag atadin mi?");
    }
    // Excel tablosundaki +Dmg/wave degeri
    [SerializeField] private float dmgPerWave = 0.6f;

    public void InitScaling(int waveNumber)
    {
        int scaleCount = Mathf.Max(0, waveNumber - 1);
        contactDamage += (dmgPerWave * scaleCount);
    }
    private void FixedUpdate()
    {
        // Hedef yoksa (oyuncu oldu / sahnede yok) dur.
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Oyuncuya dogru yon vektoru:
        // (hedef pozisyonu - benim pozisyonum) bana "oraya giden ok"u verir,
        // .normalized ile okun boyunu 1'e indiririz (sadece YON kalir),
        // sonra hizla carpariz.
        Vector2 direction = ((Vector2)target.position - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Sprite yuzu: oyuncu solundaysa sola bak (kozmetik)
        if (sr != null)
            sr.flipX = direction.x < 0f;
    }

    // OnCollisionStay2D: iki collider TEMAS HALINDE OLDUGU SURECE her fizik
    // adiminda calisir. "Stay" kullaniyoruz cunku dusman oyuncuya yapisip
    // kalirsa vurmaya devam etmeli - hasar sikligini oyuncunun i-frame'i sinirlar.
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Carptigimiz seyin ustunde PlayerHealth var mi? (duvarsa null doner)
        PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
        if (player != null)
            player.TakeDamage(contactDamage);
    }
}
