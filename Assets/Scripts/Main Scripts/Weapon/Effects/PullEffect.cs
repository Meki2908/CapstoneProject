using UnityEngine;

public class PullEffect : BaseEffectScript
{
    [Header("Pull Settings")]
    [SerializeField] private float pullForce = 2f;

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Pull enemy towards source
            Vector3 direction = (transform.position - enemy.transform.position).normalized;
            rb.AddForce(direction * pullForce, ForceMode.Impulse);

            if (debugMode) Debug.Log($"[PullEffect] Applied with force {pullForce}");
        }
    }
}
