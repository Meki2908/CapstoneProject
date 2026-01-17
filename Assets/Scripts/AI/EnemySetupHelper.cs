using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Helper script to ensure enemy is properly set up with all required components
/// Attach this to enemy prefabs to auto-configure missing components
/// </summary>
[ExecuteInEditMode]
public class EnemySetupHelper : MonoBehaviour
{
    [Header("Auto-Setup Components")]
    [SerializeField] private bool autoSetupOnAwake = true;
    [SerializeField] private bool requireNavMeshAgent = true;
    [SerializeField] private bool requireAnimator = true;
    [SerializeField] private bool requireRigidbody = true;
    [SerializeField] private bool requireCollider = true;
    [SerializeField] private bool requireEnemyContactDamage = true;
    [SerializeField] private bool requireTakeDamageTest = true;

    [Header("Component Settings")]
    [SerializeField] private float agentRadius = 0.5f;
    [SerializeField] private float agentHeight = 2.0f;
    [SerializeField] private float agentStoppingDistance = 2.1f;
    [SerializeField] private RigidbodyConstraints rigidbodyConstraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    void Awake()
    {
        if (!autoSetupOnAwake || !Application.isPlaying) return;
        AutoSetupComponents();
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            AutoSetupComponents();
        }
    }

    [ContextMenu("Auto Setup Components")]
    public void AutoSetupComponents()
    {
        bool hasChanges = false;

        // NavMeshAgent
        if (requireNavMeshAgent)
        {
            var agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
                hasChanges = true;
                Debug.Log($"[EnemySetupHelper] Added NavMeshAgent to {gameObject.name}");
            }

            // Configure agent
            agent.radius = agentRadius;
            agent.height = agentHeight;
            agent.stoppingDistance = agentStoppingDistance;
            agent.avoidancePriority = 50;
        }

        // Animator
        if (requireAnimator && GetComponent<Animator>() == null)
        {
            gameObject.AddComponent<Animator>();
            hasChanges = true;
            Debug.Log($"[EnemySetupHelper] Added Animator to {gameObject.name}");
        }

        // Rigidbody
        if (requireRigidbody)
        {
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                hasChanges = true;
                Debug.Log($"[EnemySetupHelper] Added Rigidbody to {gameObject.name}");
            }

            // Configure rigidbody
            rb.constraints = rigidbodyConstraints;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        // Collider
        if (requireCollider && GetComponent<Collider>() == null)
        {
            var capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = agentHeight;
            capsule.radius = agentRadius;
            capsule.center = new Vector3(0, agentHeight * 0.5f, 0);
            hasChanges = true;
            Debug.Log($"[EnemySetupHelper] Added CapsuleCollider to {gameObject.name}");
        }

        // EnemyContactDamage
        if (requireEnemyContactDamage && GetComponent<EnemyContactDamage>() == null)
        {
            gameObject.AddComponent<EnemyContactDamage>();
            hasChanges = true;
            Debug.Log($"[EnemySetupHelper] Added EnemyContactDamage to {gameObject.name}");
        }

        // TakeDamageTest
        if (requireTakeDamageTest && GetComponent<TakeDamageTest>() == null)
        {
            gameObject.AddComponent<TakeDamageTest>();
            hasChanges = true;
            Debug.Log($"[EnemySetupHelper] Added TakeDamageTest to {gameObject.name}");
        }

        if (hasChanges)
        {
            Debug.Log($"[EnemySetupHelper] Setup completed for {gameObject.name}. Remember to assign Animator Controller and configure components!");
        }
    }

    [ContextMenu("Remove This Helper")]
    public void RemoveHelper()
    {
        if (Application.isPlaying)
        {
            Destroy(this);
        }
        else
        {
            DestroyImmediate(this);
        }
    }
}

