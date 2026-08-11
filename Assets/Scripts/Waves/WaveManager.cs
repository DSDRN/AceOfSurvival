using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    // ELDIVENIN VE DIGER SISTEMLERIN ZAMANA BAKABILMESI ICIN DISARI ACILAN KAPI
    public static WaveManager Instance { get; private set; }

    [Header("Wave listesi (sirayla oynatilir)")]
    [SerializeField] private WaveDefinition[] waves;

    [Header("Baglantilar")]
    [SerializeField] private Transform player;

    [Tooltip("GiantGlove objesini surukle")]
    [SerializeField] private GiantGloveHazard glove;

    [Header("Spawn ayarlari")]
    [Tooltip("Dusmanlarin dogdugu cember yaricapi (ekran disi)")]
    [SerializeField] private float spawnRadius = 12f;

    [Tooltip("Wave'ler arasi bekleme (sn) - ileride level-up + magaza ekranlari")]
    [SerializeField] private float pauseBetweenWaves = 3f;

    [Header("Wave sonu kurallari (GDD 2.2 - KILIT)")]
    [Tooltip("Kalan XP/chips bu oranla otomatik toplanir = 0.75")]
    [SerializeField] private float autoCollectRatio = 0.75f;

    [Tooltip("ShopManager'i surukle")]
    [SerializeField] private ShopManager shop;

    [Tooltip("LevelUpManager'i surukle (Race condition cozumu icin)")]
    [SerializeField] private LevelUpManager levelUpManager;

    private int currentWaveIndex = -1;
    private float waveTimer;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public float WaveTimeLeft => waveTimer;

    private readonly Dictionary<EnemyHealth, List<EnemyHealth>> pools = new();

    private void Awake()
    {
        Instance = this; // HATA BURADA COZULDU: Instance tanimlandi
        RunTracker.Reset();
        EnemyHealth.RunKills = 0;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);

        for (currentWaveIndex = 0; currentWaveIndex < waves.Length; currentWaveIndex++)
        {
            WaveDefinition wave = waves[currentWaveIndex];
            player.position = Vector3.zero;
            Debug.Log($"--- WAVE {currentWaveIndex + 1} basladi ({wave.duration} sn) ---");

            if (glove != null) glove.OnWaveStart();

            waveTimer = wave.duration;
            float spawnTimer = 0f;

            while (waveTimer > 0f)
            {
                if (player == null || !player.gameObject.activeInHierarchy) yield break;

                waveTimer -= Time.deltaTime;
                spawnTimer -= Time.deltaTime;

                if (spawnTimer <= 0f && EnemyHealth.ActiveEnemies.Count < wave.maxActiveEnemies)
                {
                    SpawnEnemy(wave);
                    spawnTimer = wave.spawnInterval;
                }
                yield return null;
            }

            if (glove != null) glove.StopCycle();

            ClearAllEnemies();
            AutoCollectPickups();
            Debug.Log($"--- WAVE {currentWaveIndex + 1} bitti! ---");

            if (levelUpManager != null && player.GetComponent<PlayerResources>().PendingLevelUps > 0)
                yield return StartCoroutine(levelUpManager.RunSelection());

            if (levelUpManager != null) yield return new WaitUntil(() => !levelUpManager.IsOpen);

            if (shop != null && currentWaveIndex < waves.Length - 1)
                yield return StartCoroutine(shop.RunShop(currentWaveIndex + 1));

            yield return new WaitForSeconds(pauseBetweenWaves);
        }
        Debug.Log("=== STAGE TAMAMLANDI! ===");
    }

    private void SpawnEnemy(WaveDefinition wave)
    {
        EnemySpawnEntry entry = PickWeighted(wave.enemies);
        if (entry == null || entry.prefab == null) return;

        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector2 targetSpawnPos = (Vector2)player.position + dir * spawnRadius;

        float clampedX = Mathf.Clamp(targetSpawnPos.x, -28f, 28f);
        float clampedY = Mathf.Clamp(targetSpawnPos.y, -13f, 13f);
        Vector2 safeSpawnPos = new Vector2(clampedX, clampedY);

        int adet = Mathf.Max(1, entry.groupSize);
        for (int i = 0; i < adet; i++)
        {
            EnemyHealth enemy = GetFromPool(entry.prefab);
            enemy.transform.position = safeSpawnPos + Random.insideUnitCircle * 1.2f;
            enemy.gameObject.SetActive(true);
        }
    }

    private EnemySpawnEntry PickWeighted(EnemySpawnEntry[] entries)
    {
        if (entries == null || entries.Length == 0) return null;
        float total = 0f;
        foreach (var e in entries) total += e.weight;
        float roll = Random.Range(0f, total);
        foreach (var e in entries)
        {
            if (roll < e.weight) return e;
            roll -= e.weight;
        }
        return entries[entries.Length - 1];
    }

    private EnemyHealth GetFromPool(EnemyHealth prefab)
    {
        if (!pools.TryGetValue(prefab, out List<EnemyHealth> list))
        {
            list = new List<EnemyHealth>();
            pools[prefab] = list;
        }
        foreach (EnemyHealth e in list)
        {
            if (!e.gameObject.activeInHierarchy) return e;
        }
        EnemyHealth yeni = Instantiate(prefab);
        yeni.gameObject.SetActive(false);
        list.Add(yeni);
        return yeni;
    }

    private void ClearAllEnemies()
    {
        List<EnemyHealth> copy = new List<EnemyHealth>(EnemyHealth.ActiveEnemies);
        foreach (EnemyHealth e in copy) e.gameObject.SetActive(false);
    }

    private void AutoCollectPickups()
    {
        List<Pickup> copy = new List<Pickup>(Pickup.ActivePickups);
        foreach (Pickup p in copy) p.Collect(autoCollectRatio);
    }

    private void OnGUI()
    {
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Length) return;
        GUI.Label(new Rect(10, 10, 400, 25), $"WAVE {currentWaveIndex + 1}/{waves.Length}   Kalan: {Mathf.CeilToInt(Mathf.Max(0, waveTimer))} sn");
        GUI.Label(new Rect(10, 35, 400, 25), $"Aktif dusman: {EnemyHealth.ActiveEnemies.Count}");
    }
}