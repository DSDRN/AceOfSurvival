using System.Collections.Generic;
using UnityEngine;

public class OffscreenIndicatorManager : MonoBehaviour
{
    [Header("Baglantilar")]
    [Tooltip("Player'i surukle")]
    [SerializeField] private Transform player;

    [Tooltip("Ok gorseli prefab'i (kucuk dikdortgen - sagi 'ileri'si)")]
    [SerializeField] private Transform arrowPrefab;

    [Header("Ayarlar")]
    [Tooltip("Oklarin oyuncu etrafindaki cember yaricapi")]
    [SerializeField] private float circleRadius = 2.2f;

    [Tooltip("Ekran kenarindan bu kadar iceridesi 'gorunuyor' sayilir (0-0.5)")]
    [SerializeField] private float screenMargin = 0.03f;

    private Camera cam;
    private readonly List<Transform> arrowPool = new List<Transform>();

    private void Awake()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (player == null || !player.gameObject.activeInHierarchy || cam == null)
        {
            HideAll(0);
            return;
        }

        int used = 0;

        foreach (DangerMarker marker in DangerMarker.Active)
        {
            // Ekranda mi? Viewport: (0,0)-(1,1) arasi = gorunur alan
            Vector3 vp = cam.WorldToViewportPoint(marker.transform.position);
            bool onScreen = vp.z > 0f &&
                            vp.x > screenMargin && vp.x < 1f - screenMargin &&
                            vp.y > screenMargin && vp.y < 1f - screenMargin;
            if (onScreen) continue;   // goruyorsun zaten - ok yok

            // Oyuncudan tehlikeye yon -> cember uzerinde ok
            Vector2 dir = ((Vector2)marker.transform.position - (Vector2)player.position).normalized;
            Transform arrow = GetArrow(used++);
            arrow.position = (Vector2)player.position + dir * circleRadius;
            arrow.right = dir;   // okun "sagi" tehlikeye baksin

            // Nefes alan saydamlik (dikkat ceker, bagirmaz)
            SpriteRenderer sr = arrow.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0.45f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                sr.color = c;
            }
        }

        HideAll(used);   // bu kare kullanilmayan oklari kapat
    }

    private Transform GetArrow(int index)
    {
        while (arrowPool.Count <= index)
        {
            Transform yeni = Instantiate(arrowPrefab, transform);
            arrowPool.Add(yeni);
        }
        arrowPool[index].gameObject.SetActive(true);
        return arrowPool[index];
    }

    private void HideAll(int fromIndex)
    {
        for (int i = fromIndex; i < arrowPool.Count; i++)
            arrowPool[i].gameObject.SetActive(false);
    }
}
