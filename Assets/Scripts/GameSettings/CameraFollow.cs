using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    private static CameraFollow instance;
    private Camera cam;
    private float shakeAmplitude;
    private float shakeTimer;
    private float shakeDuration;

    private void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
    }

    public static void Shake(float amplitude, float duration)
    {
        if (instance == null) return;
        if (amplitude >= instance.shakeAmplitude || instance.shakeTimer <= 0f)
        {
            instance.shakeAmplitude = amplitude;
            instance.shakeDuration = duration;
            instance.shakeTimer = duration;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = target.position + offset;

        // ---- HARITA KELEPCESI ----
        if (MapController.Instance != null)
        {
            // Kameranin gordugu alanin yarisi: yukseklik = orthographicSize,
            // genislik = yukseklik x en-boy orani
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector2 map = MapController.Instance.CurrentHalfSize;

            // Kamera merkezi en fazla (harita - gorunum) kadar kayabilir.
            // Mathf.Max(0,...): harita gorunumden kucukse merkeze kilitle
            float maxX = Mathf.Max(0f, map.x - halfW);
            float maxY = Mathf.Max(0f, map.y - halfH);
            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
        }

        // ---- SHAKE (kelepceden sonra) ----
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float guc = shakeAmplitude * (shakeTimer / shakeDuration);
            pos += (Vector3)(Random.insideUnitCircle * guc);
        }

        transform.position = pos;
    }
}
