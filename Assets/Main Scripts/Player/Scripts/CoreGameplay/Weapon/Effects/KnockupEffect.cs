using UnityEngine;

public class KnockupEffect : BaseEffectScript
{
    [Header("Knockup Settings")]
    [SerializeField] private float knockupHeight = 3f;
    [SerializeField] private float horizontalPush = 0.35f;
    [SerializeField] private float riseDuration = 0.18f;
    [SerializeField] private float fallDuration = 0.4f;

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        if (enemy == null) return;

        EnemyCrowdControl cc = enemy.GetComponent<EnemyCrowdControl>();
        if (cc == null) cc = enemy.gameObject.AddComponent<EnemyCrowdControl>();
        cc.PlayKnockup(transform.position, horizontalPush, knockupHeight, riseDuration, fallDuration);

        if (debugMode) Debug.Log($"[KnockupEffect] Applied height={knockupHeight}, rise={riseDuration}, fall={fallDuration}");
    }
}
