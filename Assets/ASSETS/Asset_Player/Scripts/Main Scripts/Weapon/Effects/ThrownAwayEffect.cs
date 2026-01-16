using UnityEngine;

public class ThrownAwayEffect : BaseEffectScript
{
    [Header("ThrownAway Settings")]
    [SerializeField] private float thrownAwayForce = 5f;

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Push enemy away from source
            Vector3 direction = (enemy.transform.position - transform.position).normalized;
            rb.AddForce(direction * thrownAwayForce, ForceMode.Impulse);

            if (debugMode) Debug.Log($"[ThrownAwayEffect] Applied with force {thrownAwayForce}");
        }
    }
}
