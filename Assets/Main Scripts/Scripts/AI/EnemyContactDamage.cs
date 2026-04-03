using UnityEngine;

/// <summary>
/// Apply damage to the player when this enemy attacks them.
/// Attach to enemy prefab (ensure enemy has Collider and is not a trigger).
///
/// SETUP OPTIONS:
/// 1. Basic collision damage (recommended for most enemies):
///    - useAnimationEvents = false
///    - useDamageRange = false
///    - requireAttackState = true
///    Result: Damage only when enemy touches player during Attack/Attacking state
///
/// 2. Range damage (like TakeDamageTest raycast):
///    - useAnimationEvents = false
///    - useDamageRange = true
///    - damageRange = 2.0f (adjust as needed)
///    - requireAttackState = true
///    Result: Damage player within range during Attack/Attacking state
///
/// 3. Animation-controlled damage (for precise timing):
///    - useAnimationEvents = true
///    - useDamageRange = false (or true for range)
///    - requireAttackState = false
///    - Add animation events calling BeginContactDamage() and EndContactDamage()
///    Result: Damage only during animation event windows
///
/// 4. Always damage on contact (not recommended):
///    - useAnimationEvents = false
///    - useDamageRange = false
///    - requireAttackState = false
///    Result: Damage whenever player touches enemy
/// </summary>
public class EnemyContactDamage : MonoBehaviour
{
    [Tooltip("Damage applied on contact")]
    public float contactDamage = 10f;
    [Tooltip("Seconds between contact damage applications")]
    public float damageCooldown = 1f;
    [Tooltip("Tag used for player objects")]
    public string playerTag = "Player";
    [Tooltip("Reference to player transform (optional, auto-found if not set)")]
    public Transform player;
    [Tooltip("Reference to enemy AI (optional, auto-found if not set)")]
    public BaseEnemyAI enemyAI;

    float lastDamageTime;
    [Header("Damage Control Options")]
    [Tooltip("If true, contact damage is only applied while animation events enable it.\nUse this when you have animation events calling BeginContactDamage() and EndContactDamage().")]
    public bool useAnimationEvents = false;
    [Tooltip("If true, enemy can damage player from a distance during attacks.\nSimilar to TakeDamageTest raycast damage but simpler.")]
    public bool useDamageRange = false;
    [Tooltip("If true, contact damage requires enemy to be in attack state (Attack or Attacking).\nRecommended: true when not using animation events, false when using animation events.")]
    public bool requireAttackState = true;
    // When using animation events, these get toggled by the animation clips' events.
    bool canDealContactDamage = true;

    void Awake()
    {
        // If configured to use animation events, start with damage disabled until an event enables it.
        canDealContactDamage = !useAnimationEvents;

        // Try to find player if not assigned
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null) player = found.transform;
        }

        // Try to find enemy AI if not assigned
        if (enemyAI == null)
        {
            enemyAI = GetComponent<BaseEnemyAI>();
        }

        // Set default values based on options
        if (useAnimationEvents)
        {
            // When using animation events, we typically don't need attack state check
            // because animation events control the damage timing
            requireAttackState = false;
        }
        else if (useDamageRange)
        {
            // When using damage range, we should check attack state to prevent spam damage
            requireAttackState = true;
        }
        else
        {
            // Default collision damage should check attack state
            requireAttackState = true;
        }
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

    void Update()
    {
        // Check range damage if enabled - throttled for performance
        if (useDamageRange && Time.time - lastRangeCheckTime > rangeCheckInterval)
        {
            CheckRangeDamage();
            lastRangeCheckTime = Time.time;
        }
    }

    private float lastRangeCheckTime;
    private const float rangeCheckInterval = 0.1f; // Check every 0.1s for performance
    [Header("Damage Range Settings")]
    [Tooltip("Maximum distance to damage player when useDamageRange is enabled.")]
    public float damageRange = 2.0f;
    [Tooltip("Layer mask used for player detection when using damage range.")]
    public LayerMask damageLayerMask = ~0;
    [Tooltip("Disable range damage when enemy is far from player for performance.")]
    public bool disableRangeOnDistance = true;
    [Tooltip("Distance threshold to disable range damage.")]
    public float maxRangeDistance = 10f;

    [Header("Animation-hit settings")]
    [Tooltip("If set, AttemptDealContactDamage will check this point for the player when called by Animation Event.")]
    public Transform damagePoint;
    [Tooltip("Radius around damagePoint to search for player when using Animation Events.")]
    public float damageRadius = 1.2f;
    [Tooltip("Layer mask used for player detection when AttemptDealContactDamage is called.")]
    public LayerMask playerLayerMask = ~0;

    // Animation Event friendly method: called from attack animation at hit frame.
    // It will immediately apply contactDamage to any Player found within damageRadius or damageRange.
    public void AttemptDealContactDamage()
    {
        if (!useAnimationEvents && !useDamageRange) return;
        if (useAnimationEvents && !canDealContactDamage) return;

        if (player == null) return;

        // Check attack state if required
        if (requireAttackState && enemyAI != null)
        {
            var currentState = enemyAI.GetCurrentState();
            if (currentState != BaseEnemyAI.EnemyState.Attack && currentState != BaseEnemyAI.EnemyState.Attacking)
                return; // Not attacking, no damage
        }

        // Try range-based damage first (if enabled)
        if (useDamageRange)
        {
            CheckRangeDamage();
            return;
        }

        // Fallback to sphere overlap (legacy behavior)
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

        // Accept either direct player collider or a child of player (common setups)
        if (!col.CompareTag(playerTag) && !col.transform.root.CompareTag(playerTag)) return;

        var ph = col.GetComponentInParent<PlayerHealth>();
        if (ph == null) return;

        // Check animation events first (if enabled)
        if (useAnimationEvents && !canDealContactDamage)
        {
            Debug.Log($"[EnemyContactDamage] {gameObject.name} blocked damage - animation events disabled (canDealContactDamage: {canDealContactDamage})");
            return;
        }

        // Check attack state if required
        if (requireAttackState && enemyAI != null)
        {
            var currentState = enemyAI.GetCurrentState();
            if (currentState != BaseEnemyAI.EnemyState.Attack && currentState != BaseEnemyAI.EnemyState.Attacking)
            {
                Debug.Log($"[EnemyContactDamage] {gameObject.name} blocked damage - not in attack state (current: {currentState})");
                return; // Not attacking, no damage
            }
        }

        // Deal damage
        Debug.Log($"[EnemyContactDamage] {gameObject.name} dealing {contactDamage} damage to player (useAnimationEvents: {useAnimationEvents}, requireAttackState: {requireAttackState}, canDealContactDamage: {canDealContactDamage})");
        ph.TakeDamage(contactDamage, transform.position);
        lastDamageTime = Time.time;
    }

    // New method for range-based damage (called from animation or update)
    public void CheckRangeDamage()
    {
        if (!useDamageRange || player == null) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        // Performance optimization: skip range damage for distant enemies
        if (disableRangeOnDistance && enemyAI != null)
        {
            float distanceToPlayer = enemyAI.GetDistanceToPlayer();
            if (distanceToPlayer > maxRangeDistance) return;
        }

        // Check animation events first (if enabled)
        if (useAnimationEvents && !canDealContactDamage) return;

        // Check attack state if required
        if (requireAttackState && enemyAI != null)
        {
            var currentState = enemyAI.GetCurrentState();
            if (currentState != BaseEnemyAI.EnemyState.Attack && currentState != BaseEnemyAI.EnemyState.Attacking)
                return; // Not attacking, no damage
        }

        // Check distance to player
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > damageRange) return;

        // Check line of sight (simple raycast)
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, damageRange, damageLayerMask))
        {
            // Check if we hit the player
            Transform hitTransform = hit.transform;
            if (hitTransform == player || hitTransform.IsChildOf(player))
            {
                var ph = hit.transform.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    Debug.Log($"[EnemyContactDamage] {gameObject.name} dealing {contactDamage} range damage to player (distance: {distance:F2}m)");
                    ph.TakeDamage(contactDamage, transform.position);
                    lastDamageTime = Time.time;
                }
            }
        }
    }
}






