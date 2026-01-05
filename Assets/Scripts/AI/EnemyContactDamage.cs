using UnityEngine;

/// <summary>
/// Apply damage to the player when this enemy collides with them.
/// Attach to enemy prefab (ensure enemy has Collider and is not a trigger).
/// </summary>
public class EnemyContactDamage : MonoBehaviour
{
    [Tooltip("Damage applied on contact")]
    public float contactDamage = 10f;
    [Tooltip("Seconds between contact damage applications")]
    public float damageCooldown = 1f;
    [Tooltip("Tag used for player objects")]
    public string playerTag = "Player";

    float lastDamageTime;
    [Tooltip("If true, contact damage is only applied while animation events enable it.")]
    public bool useAnimationEvents = false;
    // When using animation events, these get toggled by the animation clips' events.
    bool canDealContactDamage = true;

    void Awake()
    {
        // If configured to use animation events, start with damage disabled until an event enables it.
        canDealContactDamage = !useAnimationEvents;
    }

    // Called from an Attack animation (at the frame where the hit should occur)
    public void BeginContactDamage()
    {
        canDealContactDamage = true;
    }

    // Called from the animation at the end of the attack (or when hit window ends)
    public void EndContactDamage()
    {
        canDealContactDamage = false;
    }
    [Header("Animation-hit settings")]
    [Tooltip("If set, AttemptDealContactDamage will check this point for the player when called by Animation Event.")]
    public Transform damagePoint;
    [Tooltip("Radius around damagePoint to search for player when using Animation Events.")]
    public float damageRadius = 1.2f;
    [Tooltip("Layer mask used for player detection when AttemptDealContactDamage is called.")]
    public LayerMask playerLayerMask = ~0;

    // Animation Event friendly method: called from attack animation at hit frame.
    // It will immediately apply contactDamage to any Player found within damageRadius.
    public void AttemptDealContactDamage()
    {
        if (!useAnimationEvents) return;
        if (!canDealContactDamage) return;

        Vector3 center = damagePoint != null ? damagePoint.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(center, damageRadius, playerLayerMask);
        foreach (var hit in hits)
        {
            if (hit.CompareTag(playerTag) || hit.transform.root.CompareTag(playerTag))
            {
                var ph = hit.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(contactDamage, transform.position);
                    lastDamageTime = Time.time;
                    // Only hit first valid player
                    return;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (Time.time - lastDamageTime >= damageCooldown)
            TryDamage(collision.collider);
    }

    // Support trigger colliders (useful if enemy or player uses trigger-based hurtboxes)
    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time - lastDamageTime >= damageCooldown)
            TryDamage(other);
    }

    private void TryDamage(Collider col)
    {
        if (Time.time - lastDamageTime < damageCooldown) return;

        // If configured to use animation events, only allow damage during the enabled window.
        if (useAnimationEvents && !canDealContactDamage) return;

        // Accept either direct player collider or a child of player (common setups)
        if (col.CompareTag(playerTag) || col.transform.root.CompareTag(playerTag))
        {
            var ph = col.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(contactDamage, transform.position);
                lastDamageTime = Time.time;
            }
        }
    }
}






