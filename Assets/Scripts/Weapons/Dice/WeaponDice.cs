using System.Collections.Generic;
using UnityEngine;

public class WeaponDice : WeaponBase
{
    [Header("Seviye tablosu (balance: Zar Lv1-5)")]
    [SerializeField] private int[] diceCountByLevel = { 1, 2, 2, 3, 4 };
    [SerializeField] private int[] maxFaceByLevel = { 3, 3, 4, 5, 6 };

    [Header("Sabitler")]
    [Tooltip("Cooldown = 5 sn (zar kendi ritminde - attack speed'den etkilenmez)")]
    [SerializeField] private float cooldown = 5f;
    [Tooltip("Oyuncudan bu mesafedeki dusmanlar hedef olabilir")]
    [SerializeField] private float targetSearchRange = 8f;
    [Tooltip("Zarin hedefin etrafina sacilma yaricapi")]
    [SerializeField] private float scatterRadius = 0.75f;

    [Header("D20 Özel Zar (GDD 4.3)")]
    [Tooltip("D20 cikma ihtimali (Ornek: 1.5 = %1.5 ihtimal)")]
    [SerializeField] private float nat20Chance = 1.5f;

    [Header("Baglanti")]
    [SerializeField] private DiceProjectile dicePrefab;

    private int curDiceCount, curMaxFace;
    private float timer;
    private readonly List<DiceProjectile> pool = new List<DiceProjectile>();

    protected override void OnLevelChanged()
    {
        int i = Level - 1;
        curDiceCount = diceCountByLevel[i];
        curMaxFace = maxFaceByLevel[i];
    }

    private void Update()
    {
        if (stats == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = ThrowDice() ? cooldown : 0.25f;
    }

    private bool ThrowDice()
    {
        List<EnemyHealth> candidates = new List<EnemyHealth>();
        float rangeSqr = targetSearchRange * targetSearchRange;

        foreach (EnemyHealth e in EnemyHealth.ActiveEnemies)
        {
            if (((Vector2)e.transform.position - (Vector2)owner.position).sqrMagnitude <= rangeSqr)
                candidates.Add(e);
        }
        if (candidates.Count == 0) return false;

        // --- D20 SANS KONTROLU (GDD 4.3) ---
        if (Random.Range(0f, 100f) <= nat20Chance)
        {
            // Jackpot! Sadece 1 adet D20 atilir.
            EnemyHealth target = candidates[Random.Range(0, candidates.Count)];
            Vector2 landPoint = (Vector2)target.transform.position;

            // "true" parametresi bu zarin D20 oldugunu soyler
            GetFromPool().Begin(landPoint, 20, stats, true);
            return true;
        }

        // --- NORMAL ZAR DONGUSU ---
        for (int i = 0; i < curDiceCount; i++)
        {
            EnemyHealth target = candidates[Random.Range(0, candidates.Count)];
            int face = Random.Range(1, curMaxFace + 1);

            Vector2 landPoint = (Vector2)target.transform.position
                                + Random.insideUnitCircle * scatterRadius;

            GetFromPool().Begin(landPoint, face, stats, false);
        }
        return true;
    }

    private DiceProjectile GetFromPool()
    {
        foreach (DiceProjectile d in pool)
        {
            if (!d.gameObject.activeInHierarchy)
                return d;
        }
        DiceProjectile yeni = Instantiate(dicePrefab);
        yeni.gameObject.SetActive(false);
        pool.Add(yeni);
        return yeni;
    }
}