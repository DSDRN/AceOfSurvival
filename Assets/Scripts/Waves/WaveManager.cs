using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave listesi (sirayla oynatilir)")]
    [SerializeField] private WaveDefinition[] waves;

    [Header("Baglantilar")]
    [SerializeField] private Transform player;
    [SerializeField] private GiantGloveHazard glove;
    [SerializeField] private ShopManager shop;
    [SerializeField] private LevelUpManager levelUpManager;

    [Tooltip("Wave 7'de dogacak The Dealer prefabini buraya surukle")]
    [SerializeField] private BossController bossPrefab;

    [Header("Spawn ayarlari")]
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float pauseBetweenWaves = 3f;
    [SerializeField] private float autoCollectRatio = 0.75f;

    private int currentWaveIndex = -1;
    private float waveTimer;
    private bool tankSpawnedThisWave = false;

    public int CurrentWaveNumber => currentWaveIndex + 1;
    public float WaveTimeLeft => waveTimer;

    private readonly Dictionary<EnemyHealth, List<EnemyHealth>> pools = new();

    private void Awake()
    {
        Instance = this;
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
            tankSpawnedThisWave = false;

            // HATA COZULDU: Harita Buyumesi Baglantisi
            if (currentWaveIndex == 4 && MapController.Instance != null)
                MapController.Instance.ExpandMap();

            Debug.Log($"--- WAVE {currentWaveIndex + 1} basladi ({wave.duration} sn) ---");

            if (glove != null) glove.OnWaveStart();

            waveTimer = wave.duration;
            float spawnTimer = 0f;
            bool isBossWave = (currentWaveIndex == 6);
            BossController aktifBoss = null;

            if (isBossWave)
            {
                Debug.Log("DIKKAT: BOSS WAVE BASLADI! 50 SANIYE KURALI AKTIF!");
                waveTimer = 50f;

                // HATA COZULDU: Boss artik gercekten sahneye doguyor
                if (bossPrefab != null)
                {
                    aktifBoss = Instantiate(bossPrefab, new Vector3(0f, 6f, 0f), Quaternion.identity);
                    aktifBoss.gameObject.SetActive(true);
                }
            }

            while (waveTimer > 0f)
            {
                if (player == null || !player.gameObject.activeInHierarchy) yield break;

                waveTimer -= Time.deltaTime;
                spawnTimer -= Time.deltaTime;

                int activeEnemiesAllowed = isBossWave ? Mathf.RoundToInt(wave.maxActiveEnemies * 0.75f) : wave.maxActiveEnemies;

                if (spawnTimer <= 0f && EnemyHealth.ActiveEnemies.Count < activeEnemiesAllowed)
                {
                    SpawnEnemy(wave);
                    spawnTimer = wave.spawnInterval;
                }
                yield return null;
            }

            if (glove != null) glove.StopCycle();

            if (isBossWave)
            {
                // HATA COZULDU: Gercek boss kontrolu
                bool bossYasiyorMu = aktifBoss != null && aktifBoss.gameObject.activeInHierarchy && aktifBoss.GetComponent<EnemyHealth>().CurrentHealth > 0;

                if (bossYasiyorMu)
                {
                    Debug.LogError("SURE BITTI, BOSS YASIYOR! RUN KAYBEDILDI!");
                    player.GetComponent<PlayerHealth>()?.TakeDamage(9999f);
                    yield break;
                }
            }

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
        // TODO: Zafer akisi
    }

    private void SpawnEnemy(WaveDefinition wave)
    {
        EnemySpawnEntry entry = null;

        if (!tankSpawnedThisWave && currentWaveIndex >= 2)
        {
            foreach (var e in wave.enemies)
            {
                // HATA COZULDU: Prefab ismine gore aramak yerine, gercek isTank kutucuguna bakar
                EnemyHealth eh = e.prefab.GetComponent<EnemyHealth>();
                if (eh != null && eh.isTank)
                {
                    entry = e;
                    tankSpawnedThisWave = true;
                    Debug.Log("Tank Garantisi Çalişti!");
                    break;
                }
            }
        }

        if (entry == null) entry = PickWeighted(wave.enemies);
        if (entry == null || entry.prefab == null) return;

        Vector2 dir = Random.insideUnitCircle.normalized;
        Vector2 targetSpawnPos = (Vector2)player.position + dir * spawnRadius;
        Vector2 safeSpawnPos;

        // HATA COZULDU: Harita disi dogmayi engeller
        if (MapController.Instance != null)
            safeSpawnPos = MapController.Instance.ClampInside(targetSpawnPos, 1.5f);
        else
            safeSpawnPos = new Vector2(Mathf.Clamp(targetSpawnPos.x, -28f, 28f), Mathf.Clamp(targetSpawnPos.y, -13f, 13f));

        int adet = Mathf.Max(1, entry.groupSize);
        for (int i = 0; i < adet; i++)
        {
            EnemyHealth enemy = GetFromPool(entry.prefab);
            enemy.transform.position = safeSpawnPos + Random.insideUnitCircle * 1.2f;

            enemy.InitScaling(CurrentWaveNumber);
            var chaser = enemy.GetComponent<EnemyChaser>();
            if (chaser != null) chaser.InitScaling(CurrentWaveNumber);

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
        foreach (Pickup p in copy)
        {
            // HATA COZULDU: İksirleri ve sandiklari yutmaz
            if (p.IsAutoCollectible)
                p.Collect(autoCollectRatio);
        }
    }
}