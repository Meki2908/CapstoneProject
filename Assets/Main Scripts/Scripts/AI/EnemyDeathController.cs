using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Helper component to handle enemy death transition:
/// - set Animator die parameter
/// - disable NavMeshAgent, make Rigidbody kinematic, set colliders to trigger
/// - destroy gameObject after a delay
/// Call Die() from your existing health script when HP <= 0.
/// </summary>
public class EnemyDeathController : MonoBehaviour
{
    [Tooltip("Name of the Animator bool parameter that triggers the die animation.")]
    public string dieParameter = "Die";
    [Tooltip("Seconds to wait before destroying the enemy after die animation starts.")]
    public float destroyDelay = 3f;
    [Tooltip("Max distance to search downward for ground snap when dying (meters).")]
    public float groundSnapMaxDistance = 5f;

    Animator animator;
    Rigidbody body;
    NavMeshAgent agent;
    Collider[] colliders;

    void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        colliders = GetComponentsInChildren<Collider>();
    }

    /// <summary>
    /// Call this when the enemy should die (from your health script).
    /// </summary>
    public void Die()
    {
        if (animator != null)
        {
            // Set parameter according to its configured type if present; fallback to trigger "Die"
            bool paramFound = false;
            foreach (var p in animator.parameters)
            {
                if (p.name == dieParameter)
                {
                    paramFound = true;
                    if (p.type == AnimatorControllerParameterType.Bool)
                        animator.SetBool(dieParameter, true);
                    else if (p.type == AnimatorControllerParameterType.Trigger)
                        animator.SetTrigger(dieParameter);
                    else
                        animator.SetBool(dieParameter, true);
                    break;
                }
            }
            if (!paramFound)
            {
                // Try common fallback trigger
                try { animator.SetTrigger("Die"); } catch { }
            }
        }

        // Turn off navigation and physics influence so the body doesn't fly away.
        if (agent != null)
            agent.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;

            // Snap to ground to avoid floating death poses
            Vector3 rayOrigin = transform.position + Vector3.up * 1f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundSnapMaxDistance))
            {
                Vector3 p = transform.position;
                p.y = hit.point.y;
                transform.position = p;
            }

            body.isKinematic = true;
        }

        // Make colliders triggers so they no longer block or apply physical responses.
        if (colliders != null)
        {
            foreach (var c in colliders)
                c.isTrigger = true;
        }

        // Finally destroy after a short delay so the die animation can be seen.
        StartCoroutine(DestroyAfterDelay());
    }

    System.Collections.IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}


