using TMPro;
using UnityEngine;


[RequireComponent(typeof(TMP_Text))]
public class DamageNumber : MonoBehaviour
{
    [Tooltip("Yukari suzulme hizi")]
    [SerializeField] private float floatSpeed = 1.6f;

    [Tooltip("Ekranda kalma suresi")]
    [SerializeField] private float lifeTime = 0.7f;

    private TMP_Text text;
    private float timer;
    private Color baseColor;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    /// <summary>DamageNumberManager cagirir.</summary>
    public void Show(Vector2 pos, float amount, bool isCrit, Color color)
    {
        // Hafif rastgele yatay kaydirma: ust uste vuruslarda sayilar ayrissin
        transform.position = pos + new Vector2(Random.Range(-0.25f, 0.25f), 0.35f);

        text.text = Mathf.RoundToInt(amount).ToString();
        baseColor = color;
        text.color = color;

        // Crit: daha buyuk + unlemli his
        transform.localScale = Vector3.one * (isCrit ? 1.5f : 1f);

        timer = lifeTime;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

        // Son %40'ta yavasca seffaflastir (alpha 1 -> 0)
        float a = Mathf.Clamp01(timer / (lifeTime * 0.4f));
        text.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);

        if (timer <= 0f)
            gameObject.SetActive(false);   // havuza don
    }
}
