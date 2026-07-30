using System.Collections;
using UnityEngine;


public class HouseRuleAceOfSpades : HouseRuleBase
{
    [Header("Burst ayarlari (KILIT degerler)")]
    [SerializeField] private float interval = 12f;
    [SerializeField] private float burstDuration = 2f;
    [SerializeField] private float burstMultiplier = 2.5f;

    private SpriteRenderer playerSr;   // burst'te oyuncuyu hafif sarart (feedback)

    protected override void OnLevelChanged()
    {
        // Seviyesiz kural: ilk kusanimda donguyu baslat
        playerSr = owner != null ? owner.GetComponentInChildren<SpriteRenderer>() : null;
        StopAllCoroutines();
        StartCoroutine(BurstLoop());
    }

    private IEnumerator BurstLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);

            stats.SetAttackSpeedBurst(burstMultiplier);
            Debug.Log("ACE OF SPADES: saldiri hizi patlamasi!");
            // Gecici gorsel feedback (gercek efekt cila turunda)
            if (playerSr != null) playerSr.color = new Color(1f, 1f, 0.6f);

            yield return new WaitForSeconds(burstDuration);

            stats.SetAttackSpeedBurst(1f);
            if (playerSr != null) playerSr.color = Color.white;
        }
    }

    public override void OnRemoved()
    {
        StopAllCoroutines();
        stats.SetAttackSpeedBurst(1f);   // burst ortasinda satilirsa carpan kalmasin
        if (playerSr != null) playerSr.color = Color.white;
    }
}
