using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    private const float FrozenScale = 0.05f;   // menulerin 0'indan ayirt edilebilir
    private const float Cooldown = 0.25f;

    private static HitStop instance;
    private static float nextAllowedTime;

    private void Awake()
    {
        instance = this;
    }

    public static void Do(float duration = 0.05f)
    {
        if (instance == null) return;
        if (Time.timeScale != 1f) return;                 // menu acik ya da zaten donuk
        if (Time.unscaledTime < nextAllowedTime) return;  // cok sik vuruluyorsa yut (performans)

        nextAllowedTime = Time.unscaledTime + Cooldown;
        instance.StartCoroutine(instance.FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        Time.timeScale = FrozenScale;
        // Realtime bekleme sart: scaled bekleseydik donma hic bitmezdi!
        yield return new WaitForSecondsRealtime(duration);

        // SADECE kendi donmamizi geri al (bu arada oyuncu ESC ile menu actiysa timescale 0'dir, ona dokunma)
        if (Mathf.Approximately(Time.timeScale, FrozenScale))
            Time.timeScale = 1f;
    }
}