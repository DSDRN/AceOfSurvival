using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // GameOver icin gerekli

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave listesi (sirayla oynatilir)")]
    [SerializeField] private WaveDefinition[] waves;

    [Header("Baglantilar")]
    [SerializeField] private Transform player;

    [Tooltip("GiantGlove objesini surukle")]
    [SerializeField] private GiantGloveHazard glove;

    [Header("Spawn ayarlari")]
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float pauseBetweenWaves = 3f;
    [SerializeField] private float autoCollectRatio = 0.75f;

    [SerializeField] private ShopManager shop;
    [SerializeField] private LevelUpManager levelUpManager;

    private int currentWaveIndex = -1;
    private float waveTimer;

    // YENI: Tank Garantisi icin takip
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
            tankSpawnedThisWave = false; // Yeni wave basladi, tank garantisini sifirla

            Debug.Log($"--- WAVE {currentWaveIndex + 1} basladi ({wave.duration} sn) ---");

            if (glove != null) glove.OnWaveStart();

            waveTimer = wave.duration;
            float spawnTimer = 0f;

            // BOSS WAVE KONTROLU (Wave 7, index 6)
            bool isBossWave = (currentWaveIndex == 6);
            if (isBossWave)
            {
                Debug.Log("DIKKAT: BOSS WAVE BASLADI! 50 SANIYE KURALI AKTIF!");
                waveTimer = 50f; // Boss icin sure kilitlenir
            }

            while (waveTimer > 0f)
            {
                if (player == null || !player.gameObject.activeInHierarchy) yield break;

                waveTimer -= Time.deltaTime;
                spawnTimer -= Time.deltaTime;

                // Spawner mantigi (Boss wave'inde normal dusmanlari %25 azaltip cikaririz)
                int activeEnemiesAllowed = isBossWave ? Mathf.RoundToInt(wave.maxActiveEnemies * 0.75f) : wave.maxActiveEnemies;

                if (spawnTimer <= 0f && EnemyHealth.ActiveEnemies.Count < activeEnemiesAllowed)
                {
                    SpawnEnemy(wave);
                    spawnTimer = wave.spawnInterval;
                }
                yield return null;
            }

            if (glove != null) glove.StopCycle();

            // BOSS 50 SANIYEDE OLMEDI MI?
            if (isBossWave)
            {
                // TODO: Boss objesini kontrol et, yasiyorsa Run Kaybedilir
                bool bossYasiyorMu = true; // Sahnede 'Boss' tag'li obje var mi diye aranacak
                if (bossYasiyorMu)
                {
                    Debug.LogError("SURE BITTI, BOSS YASIYOR! RUN KAYBEDILDI!");
                    player.GetComponent<PlayerHealth>()?.TakeDamage(9999f); // Oyuncuya tek at
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
        // Run Sonu (Win) ekrani tetiklenecek
    }

    private void SpawnEnemy(WaveDefinition wave)
    {
        EnemySpawnEntry entry = null;

        // AKILLI SPAWNER (Task 2.3): Eger bu wave'de tank cikmadiysa ve wave'in dusman havuzunda tank varsa ilk onu cikar
        if (!tankSpawnedThisWave && currentWaveIndex >= 2) // Wave 3'ten itibaren gecerli
        {
            foreach (var e in wave.enemies)
            {
                // Fedai'nin (Tankin) ismini veya ozel bir ayirici etiketini buraya yazmalisin
                if (e.prefab.name.Contains("Bouncer") || e.prefab.name.Contains("Fedai"))
                {
                    entry = e;
                    tankSpawnedThisWave = true;
                    Debug.Log("Tank Garantisi Çalişti: Ilk once Tank dogdu!");
                    break;
                }
            }
        }

        // Eger tank degilse normal agirilikli secim yap
        if (entry == null)
            entry = PickWeighted(wave.enemies);

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

            // TASK 2.2: Dusman sahnede yaratilirken gucunu Wave numarasina gore scale eder
            enemy.InitScaling(CurrentWaveNumber);

            // Dusmanin davranisini da (hiz/hasar vb) scale et
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
        foreach (Pickup p in copy) p.Collect(autoCollectRatio);
    }

    private void OnGUI()
    {
        if (currentWaveIndex < 0 || currentWaveIndex >= waves.Length) return;
        GUI.Label(new Rect(10, 10, 400, 25), $"WAVE {currentWaveIndex + 1}/{waves.Length}   Kalan: {Mathf.CeilToInt(Mathf.Max(0, waveTimer))} sn");
        GUI.Label(new Rect(10, 35, 400, 25), $"Aktif dusman: {EnemyHealth.ActiveEnemies.Count}");
    }
}