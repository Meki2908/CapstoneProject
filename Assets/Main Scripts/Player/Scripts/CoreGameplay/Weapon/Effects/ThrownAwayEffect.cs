using UnityEngine;

public class ThrownAwayEffect : BaseEffectScript
{
    [Header("ThrownAway Settings")]
    [SerializeField] private float thrownAwayForce = 5f;
    [SerializeField] private float throwDuration = 0.25f;
    [SerializeField] private float throwLiftHeight = 0.2f;

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        if (enemy == null) return;

        EnemyCrowdControl cc = enemy.GetComponent<EnemyCrowdControl>();
        if (cc == null) cc = enemy.gameObject.AddComponent<EnemyCrowdControl>();
        cc.PlayKnockback(transform.position, thrownAwayForce, throwDuration, throwLiftHeight);

        if (debugMode) Debug.Log($"[ThrownAwayEffect] Applied distance={thrownAwayForce}, duration={throwDuration}");
    }
}
