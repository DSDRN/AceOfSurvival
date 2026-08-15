using System.Collections.Generic;
using UnityEngine;

public class WeaponTrapCard : WeaponBase
{
    [Header("Seviye tablosu (balance v1.7 - KILIT)")]
    [SerializeField] private float[] damageByLevel = { 4f, 5f, 6f, 6.5f, 7.25f };
    [SerializeField] private float[] cooldownByLevel = { 6f, 5.6f, 5.1f, 4.7f, 4.1f };

    [Header("Sabitler (v1.3 - seviyeyle DEGISMEZ)")]
    [SerializeField] private float explosionRadius = 1.6f;
    [SerializeField] private float cardLifeTime = 15f;
    [SerializeField] private int cardsPerDrop = 1;

    [Header("Baglanti")]
    [SerializeField] private TrapCardInstance trapPrefab;

    private float curDamage, curCooldown;
    private float timer;
    private readonly List<TrapCardInstance> pool = new List<TrapCardInstance>();

    protected override void OnLevelChanged()
    {
        int i = Level - 1;
        curDamage = damageByLevel[i];
        curCooldown = cooldownByLevel[i];
    }

    private void Update()
    {
        if (stats == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        DropCards();
        timer = curCooldown;
    }

    private void DropCards()
    {
        float toplamHasar = curDamage + stats.CardCounting;

        for (int i = 0; i < cardsPerDrop; i++)
        {
            Vector2 pos = (Vector2)owner.position;
            if (i > 0) pos += Random.insideUnitCircle * 0.6f;

            GetFromPool().Arm(pos, toplamHasar, explosionRadius, cardLifeTime, stats, owner);
        }
    }

    private TrapCardInstance GetFromPool()
    {
        foreach (TrapCardInstance t in pool)
            if (!t.gameObject.activeInHierarchy)
                return t;
        TrapCardInstance yeni = Instantiate(trapPrefab);
        yeni.gameObject.SetActive(false);
        pool.Add(yeni);
        return yeni;
    }

    // YENI: Tooltip Metotlari
    public override float GetCurrentDamage() => damageByLevel[Mathf.Clamp(Level - 1, 0, damageByLevel.Length - 1)];
    public override float GetCurrentCooldown() => cooldownByLevel[Mathf.Clamp(Level - 1, 0, cooldownByLevel.Length - 1)];
}