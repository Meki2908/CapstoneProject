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
    public float hysteresisBuffer = 1.5f; // Buffer to prevent rapid state switching
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

    // Optimization variables
    protected float detectionRadiusSquared;
    protected float attackRangeSquared;
    protected float returnThresholdSquared;
    protected float returnThresholdWithHysteresis;
    protected Vector3 lastPlayerPosition;
    protected float lastDistanceToPlayerSquared;
    protected float lastAnimatorUpdateTime;
    protected const float ANIMATOR_UPDATE_INTERVAL = 0.1f;

    public enum EnemyState { Idle, Patrol, Chase, Return, Attack, Dead }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        spawnPos = transform.position;

        agent.stoppingDistance = 0.2f;

        // Cache squared distances for faster calculations
        detectionRadiusSquared = detectionRadius * detectionRadius;
        attackRangeSquared = attackRange * attackRange;
        returnThresholdSquared = returnThreshold * returnThreshold;
        returnThresholdWithHysteresis = returnThreshold + hysteresisBuffer;

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
        float distFromSpawnSquared = (new Vector2(transform.position.x, transform.position.z) -
                                     new Vector2(spawnPos.x, spawnPos.z)).sqrMagnitude;

        bool playerInDetect = lastDistanceToPlayerSquared <= detectionRadiusSquared;
        bool playerInAttack = lastDistanceToPlayerSquared <= attackRangeSquared;
        bool tooFarFromSpawn = distFromSpawnSquared > returnThresholdSquared;

        // Hysteresis logic
        float effectiveReturnThreshold = (currentState == EnemyState.Return) ?
            returnThresholdSquared * 0.8f : returnThresholdSquared;

        // Priority: Return if too far from spawn
        if (tooFarFromSpawn && !playerInAttack)
        {
            currentState = EnemyState.Return;
        }
        else if (playerInAttack && Time.time >= nextAttackTime)
        {
            currentState = EnemyState.Attack;
        }
        else if (playerInDetect && distFromSpawnSquared <= effectiveReturnThreshold)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            currentState = EnemyState.Patrol;
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
        agent.SetDestination(player.position);
    }

    protected virtual void Attack()
    {
        agent.ResetPath();
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        anim.SetTrigger("Attack");
        nextAttackTime = Time.time + attackCooldown;
        currentState = EnemyState.Chase;
    }

    protected virtual void ReturnToSpawn()
    {
        agent.speed = patrolSpeed;
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
