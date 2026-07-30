using System.Collections;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public static MapController Instance { get; private set; }

    [Header("Boyutlar (genislik x yukseklik, birim)")]
    [Tooltip("Wave 1-4 boyutu")]
    [SerializeField] private Vector2 normalSize = new Vector2(24f, 14f);

    [Tooltip("Wave 5+ boyutu (KILIT: wave 5'te buyur)")]
    [SerializeField] private Vector2 expandedSize = new Vector2(34f, 20f);

    [Tooltip("Buyume animasyonu suresi = 2-3 sn (KILIT)")]
    [SerializeField] private float growDuration = 2.5f;

    [SerializeField] private float wallThickness = 1f;

    [Header("Baglantilar (child objeler)")]
    [Tooltip("Gorsel zemin (koyu buyuk kare) - bos birakilabilir")]
    [SerializeField] private Transform floor;
    [SerializeField] private BoxCollider2D wallTop;
    [SerializeField] private BoxCollider2D wallBottom;
    [SerializeField] private BoxCollider2D wallLeft;
    [SerializeField] private BoxCollider2D wallRight;

    /// Haritanin yarim boyutu (merkezden kenara) - kamera/eldiven/spawn okur
    public Vector2 CurrentHalfSize { get; private set; }

    private void Awake()
    {
        Instance = this;
        ApplySize(normalSize);
    }

    /// WaveManager wave 5 basinda cagirir
    public void ExpandMap()
    {
        StopAllCoroutines();
        StartCoroutine(GrowRoutine());
    }

    private IEnumerator GrowRoutine()
    {
        Debug.Log("MASA BUYUYOR!");
        CameraFollow.Shake(0.25f, 0.4f);   // buyume hissi

        Vector2 from = CurrentHalfSize * 2f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / growDuration;
            // SmoothStep: basta/sonda yavas, ortada hizli - dogal buyume
            ApplySize(Vector2.Lerp(from, expandedSize, Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }
        ApplySize(expandedSize);
    }

    /// Duvarlari ve zemini verilen boyuta yerlestirir
    private void ApplySize(Vector2 size)
    {
        CurrentHalfSize = size / 2f;
        float hw = CurrentHalfSize.x, hh = CurrentHalfSize.y, th = wallThickness;

        // Ust/alt duvar: harita genisliginde + koselerde tasma payi
        SetWall(wallTop, new Vector2(0f, hh + th / 2f), new Vector2(size.x + th * 2f, th));
        SetWall(wallBottom, new Vector2(0f, -hh - th / 2f), new Vector2(size.x + th * 2f, th));
        SetWall(wallLeft, new Vector2(-hw - th / 2f, 0f), new Vector2(th, size.y));
        SetWall(wallRight, new Vector2(hw + th / 2f, 0f), new Vector2(th, size.y));

        if (floor != null)
            floor.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void SetWall(BoxCollider2D wall, Vector2 pos, Vector2 size)
    {
        if (wall == null) return;
        wall.transform.position = pos;
        wall.size = size;
    }

    /// Bir noktayi harita icinde tutar. margin = kenardan uzaklik
    /// (eldiven karti icin kartin yarisi kadar margin verilir ki tasmasin)
    public Vector2 ClampInside(Vector2 point, float margin)
    {
        return new Vector2(
            Mathf.Clamp(point.x, -CurrentHalfSize.x + margin, CurrentHalfSize.x - margin),
            Mathf.Clamp(point.y, -CurrentHalfSize.y + margin, CurrentHalfSize.y - margin));
    }
}
