using UnityEngine;

public class TornadoEffect : BaseEffectScript
{
    [Header("Tornado Settings")]
    [SerializeField] private float tornadoDuration = 2f;
    [SerializeField] private float tornadoHeight = 5f; // Height enemy flies up
    [SerializeField] private float tornadoRotationDegrees = 180f;

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        if (enemy == null) return;

        var particleCollider = GetComponent<Collider>();
        float radius = particleCollider != null ? particleCollider.bounds.size.x * 0.5f : 2f;

        EnemyCrowdControl cc = enemy.GetComponent<EnemyCrowdControl>();
        if (cc == null) cc = enemy.gameObject.AddComponent<EnemyCrowdControl>();
        cc.PlayTornado(transform.position, radius, tornadoRotationDegrees, tornadoDuration, tornadoHeight);

        if (debugMode) Debug.Log($"[TornadoEffect] Applied for {tornadoDuration}s");
    }
}
