using UnityEngine;

public class KnockupEffect : BaseEffectScript
{
    [Header("Knockup Settings")]
    [SerializeField] private float knockupForce = 3f;

    protected override void ApplyEffect(TakeDamageTest enemy)
    {
        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Launch enemy upward
            rb.AddForce(Vector3.up * knockupForce, ForceMode.Impulse);

            if (debugMode) Debug.Log($"[KnockupEffect] Applied with force {knockupForce}");
        }
    }
}
