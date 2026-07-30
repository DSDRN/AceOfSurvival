using System.Collections.Generic;
using UnityEngine;

public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    // Hazir renkler (cagiranlar bunlari kullanir)
    public static readonly Color NormalRenk = Color.white;
    public static readonly Color CritRenk = new Color(1f, 0.85f, 0.2f);   // sari
    public static readonly Color OyuncuRenk = new Color(1f, 0.35f, 0.35f);  // kirmizi

    [Tooltip("DamageNumber prefab'ini surukle")]
    [SerializeField] private DamageNumber numberPrefab;

    private readonly List<DamageNumber> pool = new List<DamageNumber>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Show(Vector2 pos, float amount, bool isCrit, Color color)
    {
        GetFromPool().Show(pos, amount, isCrit, color);
    }

    private DamageNumber GetFromPool()
    {
        foreach (DamageNumber d in pool)
        {
            if (!d.gameObject.activeInHierarchy)
                return d;
        }
        DamageNumber yeni = Instantiate(numberPrefab, transform);
        yeni.gameObject.SetActive(false);
        pool.Add(yeni);
        return yeni;
    }
}
