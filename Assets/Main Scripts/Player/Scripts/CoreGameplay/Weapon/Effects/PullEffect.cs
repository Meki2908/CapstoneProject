using UnityEngine;

public class PullEffect : BaseEffectScript
{
    [Header("Pull Settings")]
    [SerializeField] private float pullForce = 2f;
    [SerializeField] private float pullDuration = 0.22f;
    [SerializeField] private float pullLiftHeight = 0f;

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        if (enemy == null) return;

        EnemyCrowdControl cc = enemy.GetComponent<EnemyCrowdControl>();
        if (cc == null) cc = enemy.gameObject.AddComponent<EnemyCrowdControl>();
        cc.PlayPull(transform.position, pullForce, pullDuration, pullLiftHeight);

        if (debugMode) Debug.Log($"[PullEffect] Applied distance={pullForce}, duration={pullDuration}");
    }
}
