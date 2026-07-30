using System.Collections.Generic;
using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    public static PickupSpawner Instance { get; private set; }

    [Header("Prefablar")]
    [SerializeField] private Pickup xpPrefab;
    [SerializeField] private Pickup chipPrefab;
    [SerializeField] private Pickup potionPrefab;
    [SerializeField] private Pickup magnetPrefab;
    [SerializeField] private Pickup chestPrefab;

    [Header("Ayarlar")]
    [SerializeField] private float scatterRadius = 0.4f;

    [Tooltip("Consumable dustugunde magnet olma orani (gerisi iksir)")]
    [Range(0f, 1f)]
    [SerializeField] private float magnetShare = 0.15f;

    [Tooltip("Sandiktan cikan altin araligi")]
    [SerializeField] private int chestGoldMin = 3;
    [SerializeField] private int chestGoldMax = 10;

    private readonly Dictionary<Pickup, List<Pickup>> pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// EnemyHealth.Die() cagirir. consumablePct/chestPct: 0-100 arasi sans
    /// (balance 4_Dusmanlar kolonlari).
    /// </summary>
    public void SpawnDrops(Vector2 pos, float chips, float xp,
                           float consumablePct = 0f, float chestPct = 0f)
    {
        if (chips > 0f) Spawn(chipPrefab, pos, chips);
        if (xp > 0f) Spawn(xpPrefab, pos, xp);

        if (consumablePct > 0f && Random.Range(0f, 100f) < consumablePct)
        {
            bool magnet = Random.value < magnetShare;
            Spawn(magnet ? magnetPrefab : potionPrefab, pos, 0f);
        }

        if (chestPct > 0f && Random.Range(0f, 100f) < chestPct)
            Spawn(chestPrefab, pos, Random.Range(chestGoldMin, chestGoldMax + 1));
    }

    private void Spawn(Pickup prefab, Vector2 pos, float value)
    {
        if (prefab == null) return;
        Pickup p = GetFromPool(prefab);
        p.transform.position = pos + Random.insideUnitCircle * scatterRadius;
        p.Init(value);
        p.gameObject.SetActive(true);
    }

    private Pickup GetFromPool(Pickup prefab)
    {
        if (!pools.TryGetValue(prefab, out List<Pickup> pool))
        {
            pool = new List<Pickup>();
            pools[prefab] = pool;
        }
        foreach (Pickup p in pool)
            if (!p.gameObject.activeInHierarchy)
                return p;
        Pickup yeni = Instantiate(prefab);
        yeni.gameObject.SetActive(false);
        pool.Add(yeni);
        return yeni;
    }
}
