using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Simple patrol/chase/attack AI for the spider enemy.
/// Requirements:
/// - NavMeshAgent on the same GameObject.
/// - Animator with parameters: float Speed, Trigger Attack, Trigger Die, Trigger Hit (optional), Trigger Taunt (optional).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class SpiderEnemyAI : MonoBehaviour
{
    public Transform player;

    [Header("Ranges")]
    public float patrolRadius = 10f;        // Bán kính đi tuần quanh spawn
    public float detectionRadius = 8f;      // Khi player bước vào, bắt đầu đuổi
    public float attackRange = 1.8f;        // Khoảng cách tấn công (tính theo mặt phẳng XZ)
    public float returnThreshold = 12f;     // Khi player ra xa hơn ngưỡng này, quay về spawn

    [Header("Movement Speeds")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 4.5f;

    [Header("Timing")]
    public float waypointPause = 1.5f;      // Thời gian dừng tại điểm tuần tra
    public float attackCooldown = 1.0f;     // Khoảng thời gian giữa các đòn tấn công

    [Header("Debug")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
    private Animator anim;
    private Vector3 spawnPos;
    private float nextAttackTime;
    private float waitTimer;

    private enum State { Idle, Patrol, Chase, Return, Attack, Dead }
    private State state = State.Idle;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        spawnPos = transform.position;

        // Stopping distance nhỏ để agent thực sự tới đích patrol/return
        agent.stoppingDistance = 0.2f;

        // Thử tự tìm player nếu chưa gán
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        PickNewPatrolPoint();
    }

    void Update()
    {
        if (state == State.Dead) return;
        if (!agent.isOnNavMesh) return;

        // Nếu chưa có player (chưa gán đúng tag / transform) thì chỉ tuần tra quanh spawn
        if (player == null)
        {
            state = State.Patrol;
            Patrol();
            anim.SetFloat("Speed", agent.velocity.magnitude);
            return;
        }

        // Tính khoảng cách trên mặt phẳng XZ để tránh lệch cao độ
        float distToPlayer = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(player.position.x, player.position.z));
        float distFromSpawn = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(spawnPos.x, spawnPos.z));

        // State decision (đơn giản)
        bool playerInDetect = distToPlayer <= detectionRadius;
        bool playerInAttack = distToPlayer <= attackRange;
        bool tooFarFromSpawn = distFromSpawn > returnThreshold;

        // Ưu tiên cao nhất: nếu đã ra ngoài vùng cho phép, quay về
        if (tooFarFromSpawn && !playerInAttack)
        {
            state = State.Return;
        }
        else if (playerInAttack && Time.time >= nextAttackTime)
        {
            state = State.Attack;
        }
        else if (playerInDetect && !tooFarFromSpawn)
        {
            state = State.Chase;
        }
        else
        {
            state = State.Patrol;
        }

        // State handling
        switch (state)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                Chase();
                break;
            case State.Attack:
                Attack();
                break;
            case State.Return:
                ReturnToSpawn();
                break;
        }

        // Update animator Speed parameter using agent velocity
        anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void Patrol()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = patrolSpeed;
        // Xem như đã tới điểm patrol khi còn cách trong khoảng stoppingDistance + 0.1
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

    private void Chase()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = chaseSpeed;
        // Nếu player vượt khỏi returnThreshold, sẽ bị logic ở Update chuyển sang Return ở frame sau
        agent.SetDestination(player.position);
    }

    private void Attack()
    {
        agent.ResetPath();
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        anim.SetTrigger("Attack");
        nextAttackTime = Time.time + attackCooldown;
        // Sau khi ra đòn, quay về Chase ngay trong khung Update tiếp theo
        state = State.Chase;
    }

    private void ReturnToSpawn()
    {
        if (!agent.isOnNavMesh) return;
        agent.speed = patrolSpeed;
        agent.SetDestination(spawnPos);
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            PickNewPatrolPoint();
            state = State.Patrol;
        }
    }

    private void PickNewPatrolPoint()
    {
        if (!agent.isOnNavMesh) return;
        // Chọn điểm ngẫu nhiên trong vòng tròn quanh spawn, giữ nguyên độ cao hiện tại
        Vector2 random = Random.insideUnitCircle * patrolRadius;
        Vector3 patrolPoint = new Vector3(spawnPos.x + random.x, spawnPos.y, spawnPos.z + random.y);
        agent.SetDestination(patrolPoint);
    }

    // Các sự kiện nhận sát thương/ chết có thể được gọi từ hệ thống combat
    public void TakeHit()
    {
        if (state == State.Dead) return;
        anim.SetTrigger("Hit");
    }

    public void Die()
    {
        if (state == State.Dead) return;
        state = State.Dead;
        agent.ResetPath();
        agent.enabled = false;
        anim.SetTrigger("Die");
        // Tuỳ chọn: Destroy(gameObject, 3f);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        var pos = Application.isPlaying ? transform.position : transform.position;
        // Patrol/detect vẽ quanh spawn để bạn thấy vùng tuần
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPos : transform.position, patrolRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPos : transform.position, detectionRadius);
        // Attack vẽ quanh vị trí hiện tại để thấy vùng đánh
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, attackRange);
    }
}

