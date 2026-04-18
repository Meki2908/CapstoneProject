using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base enemy AI script that can be inherited by different enemy types.
/// Provides common AI behaviors: patrol, chase, attack, return to spawn.
/// Optimized for performance with caching and hysteresis.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public abstract class BaseEnemyAI : MonoBehaviour
{
    [Header("Common AI Settings")]
    public Transform player;
    public float patrolRadius = 10f;
    public float detectionRadius = 8f;
    public float attackRange = 1.8f;
    public float returnThreshold = 12f;
    public float hysteresisBuffer = 3.0f; // Buffer to prevent rapid state switching - increased
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 4.5f;
    public float waypointPause = 1.5f;
    public float attackCooldown = 1.0f;
    public bool drawGizmos = true;
    public bool enableDebugLogging = false; // Disable for production performance

    // Cached components
    protected NavMeshAgent agent;
    protected Animator anim;
    protected Vector3 spawnPos;

    // State variables
    protected float nextAttackTime;
    protected float waitTimer;
    protected EnemyState currentState = EnemyState.Idle;
    private EnemyState lastDebugState = EnemyState.Idle; // For debugging state changes

    // Optimization variables
    protected float detectionRadiusSquared;
    protected float attackRangeSquared;
    protected float returnThresholdSquared;
    protected float returnThresholdWithHysteresis;
    protected Vector3 lastPlayerPosition;
    protected float lastDistanceToPlayerSquared;
    protected float lastAnimatorUpdateTime;
    protected const float ANIMATOR_UPDATE_INTERVAL = 0.2f; // Further reduced for many enemies
    protected float lastStateChangeTime;
    protected const float STATE_CHANGE_COOLDOWN = 1.0f; // Further increased for many enemies
    protected const float DISTANCE_UPDATE_INTERVAL = 0.1f; // How often to update player distance
    protected float lastDistanceUpdateTime;

    public enum EnemyState { Idle, Patrol, Chase, Return, Attack, Attacking, Dead }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        spawnPos = transform.position;

        // Ensure agent stops slightly before the attack range to avoid collider penetration
        agent.stoppingDistance = attackRange + 0.3f;

        // Configure NavMeshAgent for better collision behavior
        agent.avoidancePriority = 50; // Lower priority means less avoidance
        agent.radius = 0.5f; // Adjust based on enemy size
        agent.height = 2.0f; // Adjust based on enemy height

        // Cache squared distances for faster calculations
        detectionRadiusSquared = detectionRadius * detectionRadius;
        attackRangeSquared = attackRange * attackRange;
        returnThresholdSquared = returnThreshold * returnThreshold;
        // Precompute squared threshold including hysteresis buffer for comparisons on XZ plane
        returnThresholdWithHysteresis = (returnThreshold + hysteresisBuffer) * (returnThreshold + hysteresisBuffer);

        // Initialize optimization variables
        lastDistanceToPlayerSquared = float.MaxValue;
        lastAnimatorUpdateTime = 0f;

        // Try to find player if not assigned
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        // Initialize first patrol point
        PickNewPatrolPoint();

        // Call subclass initialization
        OnInitialize();
    }

    protected virtual void Start()
    {
        // Subclass can override for additional start logic
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;
        if (!agent.isOnNavMesh) return;

        if (player == null)
        {
            currentState = EnemyState.Patrol;
            Patrol();
            UpdateAnimatorSpeed();
            return;
        }

        // Distance-based performance optimization
        // Enemies far from player update less frequently
        float distanceToPlayer = GetDistanceToPlayer();
        float updateInterval = GetUpdateIntervalBasedOnDistance(distanceToPlayer);

        if (Time.time - lastFullUpdateTime < updateInterval) return;
        lastFullUpdateTime = Time.time;

        // Optimized distance calculations
        UpdateDistances();

        // State decision with hysteresis
        UpdateState();

        // DEBUG: Log state changes for debugging (only if enabled)
        if (enableDebugLogging && currentState != lastDebugState)
        {
            Debug.Log($"[BaseEnemyAI] {gameObject.name} state changed: {lastDebugState} -> {currentState} (dist: {distanceToPlayer:F2}m)");
            lastDebugState = currentState;
        }

        // Execute current state
        ExecuteState();

        // Update animator at intervals
        UpdateAnimatorSpeed();
    }

    protected float lastFullUpdateTime;
    protected virtual float GetUpdateIntervalBasedOnDistance(float distance)
    {
        // Performance optimization: distant enemies update less frequently
        if (distance > 20f) return 0.5f;      // Very far: update every 0.5s
        if (distance > 10f) return 0.2f;      // Far: update every 0.2s
        if (distance > 5f) return 0.1f;       // Medium: update every 0.1s
        return 0.05f;                         // Close: update every 0.05s
    }

    protected virtual void UpdateDistances()
    {
        // Throttle distance updates for performance with many enemies
        if (Time.time - lastDistanceUpdateTime < DISTANCE_UPDATE_INTERVAL) return;
        lastDistanceUpdateTime = Time.time;

        Vector3 currentPos = transform.position;
        Vector3 playerPos = player.position;

        // Calculate distances on XZ plane
        Vector2 pos2D = new Vector2(currentPos.x, currentPos.z);
        Vector2 playerPos2D = new Vector2(playerPos.x, playerPos.z);
        Vector2 spawnPos2D = new Vector2(spawnPos.x, spawnPos.z);

        lastDistanceToPlayerSquared = (playerPos2D - pos2D).sqrMagnitude;
        lastPlayerPosition = playerPos;
    }

    protected virtual void UpdateState()
    {
        // Prevent state changes too frequently
        if (Time.time - lastStateChangeTime < STATE_CHANGE_COOLDOWN) return;

        float distFromSpawnSquared = (new Vector2(transform.position.x, transform.position.z) -
                                     new Vector2(spawnPos.x, spawnPos.z)).sqrMagnitude;

        bool playerInDetect = lastDistanceToPlayerSquared <= detectionRadiusSquared;
        bool playerInAttack = lastDistanceToPlayerSquared <= attackRangeSquared;
        bool tooFarFromSpawn = distFromSpawnSquared > returnThresholdSquared;
        bool playerWithinReturnAreaWithHysteresis = distFromSpawnSquared <= returnThresholdWithHysteresis;

        EnemyState newState = currentState; // Default to current state

        // Check if attacking animation is finished (simple time-based check)
        if (currentState == EnemyState.Attacking && Time.time >= nextAttackTime)
        {
            newState = EnemyState.Chase;
        }
        // Priority-based state logic with improved hysteresis
        else if (playerInAttack && Time.time >= nextAttackTime)
        {
            newState = EnemyState.Attack;
        }
        else if (playerInDetect && playerWithinReturnAreaWithHysteresis)
        {
            // Player detected and within acceptable distance from spawn (with hysteresis)
            newState = EnemyState.Chase;
        }
        else if (tooFarFromSpawn && currentState != EnemyState.Attack)
        {
            // Only return if we're not in attack state and player is not detected
            if (!playerInDetect)
            {
                newState = EnemyState.Return;
            }
        }
        else if (!playerInDetect && !tooFarFromSpawn)
        {
            newState = EnemyState.Patrol;
        }

        // Only change state if it's actually different
        if (newState != currentState)
        {
            currentState = newState;
            lastStateChangeTime = Time.time;
        }
    }

    protected virtual void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Chase:
                Chase();
                break;
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Attacking:
                // Stay stopped and facing player during attack animation
                agent.isStopped = true;
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
                break;
            case EnemyState.Return:
                ReturnToSpawn();
                break;
        }
    }

    protected virtual void Patrol()
    {
        agent.speed = patrolSpeed;

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waypointPause)
            {
                PickNewPatrolPoint();
                waitTimer = 0f;
            }
        }
    }

    protected virtual void Chase()
    {
        agent.speed = chaseSpeed;
        if (agent.isStopped) agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    protected virtual void Attack()
    {
        // Stop the agent immediately to avoid physics pushing into the player
        agent.ResetPath();
        agent.isStopped = true;
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        anim.SetTrigger("Attack");
        nextAttackTime = Time.time + attackCooldown;
        currentState = EnemyState.Attacking; // Stay in attacking state until animation finishes
    }

    protected virtual void ReturnToSpawn()
    {
        agent.speed = patrolSpeed;
        if (agent.isStopped) agent.isStopped = false;
        agent.SetDestination(spawnPos);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            PickNewPatrolPoint();
            currentState = EnemyState.Patrol;
        }
    }

    protected virtual void PickNewPatrolPoint()
    {
        if (!agent.isOnNavMesh) return;
        Vector2 random = Random.insideUnitCircle * patrolRadius;
        Vector3 patrolPoint = new Vector3(spawnPos.x + random.x, spawnPos.y, spawnPos.z + random.y);
        agent.SetDestination(patrolPoint);
    }

    protected virtual void UpdateAnimatorSpeed()
    {
        if (Time.time - lastAnimatorUpdateTime >= ANIMATOR_UPDATE_INTERVAL)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
            lastAnimatorUpdateTime = Time.time;
        }
    }

    // Abstract methods for subclasses to implement
    protected abstract void OnInitialize();

    // Virtual methods that can be overridden
    public virtual void TakeHit()
    {
        if (currentState == EnemyState.Dead) return;
        anim.SetTrigger("Hit");
    }

    public virtual void Die()
    {
        if (currentState == EnemyState.Dead) return;
        currentState = EnemyState.Dead;
        agent.ResetPath();
        agent.enabled = false;
        anim.SetTrigger("Die");
    }

    // Public API
    public EnemyState GetCurrentState() => currentState;
    public bool IsDead() => currentState == EnemyState.Dead;
    public float GetDistanceToPlayer() => Mathf.Sqrt(lastDistanceToPlayerSquared);
    public bool IsPlayerInDetectionRange() => lastDistanceToPlayerSquared <= detectionRadiusSquared;
    public bool IsPlayerInAttackRange() => lastDistanceToPlayerSquared <= attackRangeSquared;

    // Debug/Test API
    public void ForceAttackForTesting()
    {
        if (currentState == EnemyState.Dead) return;
        Debug.Log($"[BaseEnemyAI] Force attack triggered for {gameObject.name}");
        Attack();
    }

    // Debug
    protected virtual void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        var pos = Application.isPlaying ? transform.position : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPos : transform.position, patrolRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPos : transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, attackRange);
    }
}
