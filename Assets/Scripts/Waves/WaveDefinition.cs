using UnityEngine;

[CreateAssetMenu(menuName = "Ace of Survival/Wave Definition", fileName = "Wave_")]
public class WaveDefinition : ScriptableObject
{
    [Header("Zamanlama (balance 5_Waveler)")]
    [Tooltip("Wave suresi (sn). Demo: 20/25/30/35/40/45/50 - KILIT")]
    public float duration = 20f;

    [Tooltip("Kac saniyede bir dusman dogsun? Kucuk sayi = daha yogun")]
    public float spawnInterval = 1f;

    [Tooltip("Ayni anda sahnede olabilecek max dusman")]
    public int maxActiveEnemies = 30;

    [Header("Dusman havuzu")]
    public EnemySpawnEntry[] enemies;
}

[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Dusman prefab'i (ustunde EnemyHealth olmali)")]
    public EnemyHealth prefab;

    [Tooltip("Secilme agirligi (digerlerine gore oran)")]
    public float weight = 1f;

    [Tooltip("YENI: tek seferde kac tane dogsun? (Kart Usagi = 4, digerleri = 1)")]
    public int groupSize = 1;
}
