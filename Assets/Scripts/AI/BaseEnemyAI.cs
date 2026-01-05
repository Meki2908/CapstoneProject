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
    protected const float ANIMATOR_UPDATE_INTERVAL = 0.1f;
    protected float lastStateChangeTime;
    protected const float STATE_CHANGE_COOLDOWN = 0.5f; // Minimum time between state changes

    public enum EnemyState { Idle, Patrol, Chase, Return, Attack, Dead }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        spawnPos = transform.position;

        // Ensure agent stops slightly before the attack range to avoid collider penetration
        agent.stoppingDistance = attackRange + 0.3f;

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

        // Optimized distance calculations
        UpdateDistances();

        // State decision with hysteresis
        UpdateState();

        // DEBUG: Log state changes for debugging
        if (currentState != lastDebugState)
        {
            Debug.Log($"[BaseEnemyAI] {gameObject.name} state changed: {lastDebugState} -> {currentState} (dist: {GetDistanceToPlayer():F2}m)");
            lastDebugState = currentState;
        }

        // Execute current state
        ExecuteState();

        // Update animator at intervals
        UpdateAnimatorSpeed();
    }

    protected virtual void UpdateDistances()
    {
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

        // Priority-based state logic with improved hysteresis
        if (playerInAttack && Time.time >= nextAttackTime)
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
        currentState = EnemyState.Chase;
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
