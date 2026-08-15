using System.Collections.Generic;
using UnityEngine;

public class WeaponDice : WeaponBase
{
    [Header("Seviye tablosu (balance: Zar Lv1-5)")]
    [SerializeField] private int[] diceCountByLevel = { 1, 2, 2, 3, 4 };
    [SerializeField] private int[] maxFaceByLevel = { 3, 3, 4, 5, 6 };

    [Header("Sabitler")]
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private float targetSearchRange = 8f;
    [SerializeField] private float scatterRadius = 0.75f;
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

        foreach (EnemyHealth e in EnemyHealth.AllHittables)
        {
            if (((Vector2)e.transform.position - (Vector2)owner.position).sqrMagnitude <= rangeSqr)
                candidates.Add(e);
        }
        if (candidates.Count == 0) return false;

        if (Random.Range(0f, 100f) <= nat20Chance)
        {
            EnemyHealth target = candidates[Random.Range(0, candidates.Count)];
            Vector2 landPoint = (Vector2)target.transform.position;
            GetFromPool().Begin(landPoint, 20, stats, true);
            return true;
        }

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

    // YENI: Tooltip Metotlari (Zar hasari formulu: Yuz x 0.75 + 0.75)
    public override float GetCurrentDamage()
    {
        int maxF = maxFaceByLevel[Mathf.Clamp(Level - 1, 0, maxFaceByLevel.Length - 1)];
        return (maxF * 0.75f) + 0.75f; // Maksimum sekme hasarini temsil eder
    }
    public override float GetCurrentCooldown() => cooldown;
}