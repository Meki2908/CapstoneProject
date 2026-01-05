using UnityEngine;
using AI.BehaviorTree;
using System.Collections.Generic;

/// <summary>
/// Enemy Behavior Tree - AI tự động cho enemy
/// </summary>
public class EnemyBT : BehaviorTree
{
    [Header("Enemy Settings")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float moveSpeed = 3f;           // Walk speed (patrol)
    public float chaseSpeed = 5f;          // Run speed (đuổi theo - phải > 3 để trigger run animation)
    public float attackCooldown = 1.5f;
    public float attackAnimationDuration = 2.5f;  // Thời gian chờ animation attack hoàn thành trước khi chase
    public float patrolRadius = 5f;
    public bool shouldPatrol = true;       // LUÔN TUẦN TRA (default = true)

    [Header("References")]
    public Transform[] patrolPoints;
    public LayerMask targetLayer = -1;  // Default: All layers (user should set to "Player")

    [Header("Debug")]
    public bool showGizmos = true;
    public bool showDebugLogs = true;      // Hiển thị debug logs trong Console

    private Animator animator;
    public CharacterController characterController;

    // Attack position lock - PUBLIC để TaskAttack có thể set
    [HideInInspector] public bool isPositionLocked = false;
    [HideInInspector] public Vector3 lockedPosition;


    protected override Node SetupTree()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        // Auto-set target layer to Player if not configured
        // Kiểm tra cả value == 0 và value == -1 (default "Everything")
        if (targetLayer.value == 0 || targetLayer.value == -1)
        {
            int playerLayerMask = LayerMask.GetMask("Player");
            if (playerLayerMask == 0)
            {
                Debug.LogError($"[{gameObject.name}] ❌ 'Player' layer không tồn tại! Vui lòng tạo layer 'Player' trong Project Settings → Tags and Layers.");
            }
            else
            {
                targetLayer = playerLayerMask;
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[{gameObject.name}] ⚠️ Target Layer was not set! Auto-set to 'Player' layer (mask: {targetLayer.value}).");
                }
            }
        }
        
        // Debug: Log target layer info
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 🎯 Target Layer: {targetLayer.value} (Player layer mask: {LayerMask.GetMask("Player")})");
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] ✅ Behavior Tree SETUP COMPLETE");
            Debug.Log($"   - Mode: PATROL (Always Active)");
            Debug.Log($"   - Patrol Radius: {patrolRadius}m");
            Debug.Log($"   - Patrol Points: {(patrolPoints != null && patrolPoints.Length > 0 ? patrolPoints.Length.ToString() : "Random")}");
            Debug.Log($"   - Detection Range: {detectionRange}m");
            Debug.Log($"   - Attack Range: {attackRange}m");
            Debug.Log($"   - Move Speed: {moveSpeed} | Chase Speed: {chaseSpeed}");
        }

        // Tạo cấu trúc Behavior Tree
        Node root = new Selector(new List<Node>
        {
            // Priority 1: Check if dead
            new TaskCheckIsDead(this),
            
            // Priority 2: Combat behavior
            new Sequence(new List<Node>
            {
                new TaskDetectTarget(transform, detectionRange, targetLayer),
                new Selector(new List<Node>
                {
                    // TaskAttack tự check range VÀ xử lý timer 1.5s
                    // Nếu player trong range HOẶC đang trong 1.5s delay → Success (không chase)
                    // Nếu player ngoài range > 1.5s → Failure (chase)
                    new TaskAttack(this, animator, attackCooldown),
                    
                    // Chase if TaskAttack fails
                    new TaskChaseTarget(this, transform, chaseSpeed, animator)
                })
            }),
            
            // Priority 3: LUÔN TẠO CẢ PATROL VÀ IDLE, TaskPatrol sẽ tự check shouldPatrol
            new TaskPatrol(this, transform, patrolPoints, patrolRadius, moveSpeed, animator)
        });

        return root;
    }

    /// <summary>
    /// LateUpdate - Chạy SAU physics/animation update
    /// Force position nếu đang locked (attack mode)
    /// </summary>
    private void LateUpdate()
    {
        if (isPositionLocked)
        {
            // FORCE về locked position MỖI FRAME sau khi Unity xử lý xong physics
            transform.position = lockedPosition;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 position = transform.position;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(position, detectionRange);

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(position, attackRange);

        // Patrol radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(position, patrolRadius);
    }
}

#region Task Nodes

/// <summary>
/// Task: Kiểm tra enemy đã chết chưa
/// </summary>
public class TaskCheckIsDead : Node
{
    private EnemyBT enemy;

    public TaskCheckIsDead(EnemyBT enemy)
    {
        this.enemy = enemy;
    }

    public override NodeState Evaluate()
    {
        // TODO: Implement death check logic
        // Ví dụ: if (enemy.health <= 0)
        state = NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Phát hiện target trong range
/// </summary>
public class TaskDetectTarget : Node
{
    private Transform transform;
    private float detectionRange;
    private LayerMask targetLayer;

    public TaskDetectTarget(Transform transform, float range, LayerMask layer)
    {
        this.transform = transform;
        this.detectionRange = range;
        this.targetLayer = layer;
    }

    public override NodeState Evaluate()
    {
        object target = GetData("target");

        if (target == null)
        {
            // DEBUG: Log detection attempt
            EnemyBT enemyBT = transform.GetComponent<EnemyBT>();
            if (enemyBT != null && enemyBT.showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[{transform.gameObject.name}] 🔍 Detecting... Range: {detectionRange}m, Layer: {targetLayer.value}");
            }
            
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);

            // DEBUG: Log detection results
            if (enemyBT != null && enemyBT.showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[{transform.gameObject.name}] 🔍 Found {colliders.Length} colliders in detection range");
                if (colliders.Length == 0 && targetLayer.value == 0)
                {
                    Debug.LogWarning($"[{transform.gameObject.name}] ⚠️ Target Layer = 0! Spider không thể detect player! Vui lòng set targetLayer = 'Player' layer.");
                }
            }

            if (colliders.Length > 0)
            {
                // Tìm target gần nhất
                Transform closestTarget = null;
                float closestDistance = float.MaxValue;

                foreach (Collider col in colliders)
                {
                    float distance = Vector3.Distance(transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = col.transform;
                    }
                }

                parent.parent.SetData("target", closestTarget);

                // DEBUG LOG (reuse enemyBT from line 190)
                if (enemyBT != null && enemyBT.showDebugLogs)
                {
                    Debug.Log($"[{transform.gameObject.name}] 🎯 DETECTED TARGET: {closestTarget.name} at {closestDistance:F2}m");
                }

                state = NodeState.Success;
                return state;
            }

            state = NodeState.Failure;
            return state;
        }

        state = NodeState.Success;
        return state;
    }
}

/// <summary>
/// Task: Kiểm tra target trong attack range
/// </summary>
public class TaskCheckAttackRange : Node
{
    private Transform transform;
    private float attackRange;

    public TaskCheckAttackRange(Transform transform, float range)
    {
        this.transform = transform;
        this.attackRange = range;
    }

    public override NodeState Evaluate()
    {
        object target = GetData("target");

        if (target == null)
        {
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;
        float distance = Vector3.Distance(transform.position, targetTransform.position);

        if (distance <= attackRange)
        {
            state = NodeState.Success;
            return state;
        }

        state = NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Tấn công target
/// </summary>
public class TaskAttack : Node
{
    private EnemyBT enemy;
    private Animator animator;
    private float attackCooldown;
    private float lastAttackTime;
    private bool isAttacking = false;

    // Delay trước khi chase khi player rời khỏi attack range
    private float outOfRangeTimer = 0f;
    private bool isWaitingToChase = false; // Đang đợi để chase

    // Position lock giờ dùng biến public trong EnemyBT để LateUpdate có thể access

    public TaskAttack(EnemyBT enemy, Animator animator, float cooldown)
    {
        this.enemy = enemy;
        this.animator = animator;
        this.attackCooldown = cooldown;
    }

    public override NodeState Evaluate()
    {
        // Sử dụng attackAnimationDuration từ enemy (có thể config trong Inspector)
        float outOfRangeDelay = enemy.attackAnimationDuration;
        object target = GetData("target");
        if (target == null)
        {
            // Hủy attack nếu mất target
            CancelAttack();
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;
        float distance = Vector3.Distance(enemy.transform.position, targetTransform.position);

        // DEBUG: Show distance every frame when out of range
        if (distance > enemy.attackRange && enemy.showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log($"📏 Distance: {distance:F2}m (attack range: {enemy.attackRange}m)");
        }

        // ===== CHECK: NẾU CHƯA BAO GIỜ VÀO ATTACK RANGE → RETURN FAILURE (CHASE) =====
        if (distance > enemy.attackRange && !enemy.isPositionLocked && !isWaitingToChase)
        {
            // Player CHƯA BAO GIỜ vào attack range (chưa locked)
            // → Return Failure để TaskChaseTarget chạy
            if (enemy.showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"🏃 Player far away ({distance:F2}m), not attacking yet - let Chase handle it");
            }

            state = NodeState.Failure;
            return state;
        }

        // ===== LOGIC: ĐỢI 1.5s TRƯỚC KHI CHASE KHI PLAYER RA KHỎI RANGE =====
        // (Chỉ chạy khi ĐÃ TỪNG vào attack range = isPositionLocked hoặc isWaitingToChase)
        if (distance > enemy.attackRange)
        {
            // Player RA NGOÀI attack range (nhưng ĐÃ TỪNG vào)
            if (!isWaitingToChase)
            {
                // Lần đầu ra ngoài range (sau khi đã attack) - bắt đầu đếm timer
                isWaitingToChase = true;
                outOfRangeTimer = 0f;

                if (enemy.showDebugLogs)
                {
                    Debug.Log($"⏱️ Player left attack range! Waiting {outOfRangeDelay}s before chase...");
                }

                // QUAN TRỌNG: Return Success để giữ position lock và không chase ngay
                // Set Speed = 0 để giữ battleidle
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                }

                state = NodeState.Success;
                return state;
            }

            // ===== ĐANG ĐẾM TIMER (isWaitingToChase = true) =====
            outOfRangeTimer += Time.deltaTime;

            if (outOfRangeTimer >= outOfRangeDelay)
            {
                // ĐỦ 1.5s RỒI - BẮT ĐẦU CHASE!
                if (enemy.showDebugLogs)
                {
                    Debug.Log($"🏃 {outOfRangeDelay}s elapsed! Starting chase!");
                    Debug.Log($"   - Unlocking position...");
                    Debug.Log($"   - Re-enabling CharacterController...");
                }

                CancelAttack(); // Unlock position và re-enable CharacterController

                // DEBUG: Verify unlock
                if (enemy.showDebugLogs)
                {
                    Debug.Log($"   - isPositionLocked: {enemy.isPositionLocked}");
                    Debug.Log($"   - CharacterController enabled: {enemy.characterController?.enabled}");
                }

                isWaitingToChase = false;
                outOfRangeTimer = 0f;
                state = NodeState.Failure; // Failure để chuyển sang Chase
                return state;
            }

            // VẪN ĐANG ĐỢI - GIỮ VỊ TRÍ LOCK, TIẾP TỤC XOAY THEO TARGET
            // Không cancel attack, vẫn giữ battleidle/attack animation
            if (enemy.showDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"⏱️ Waiting... {outOfRangeTimer:F1}s / {outOfRangeDelay}s");
            }

            // Vẫn xoay theo target
            Vector3 waitDirection = (targetTransform.position - enemy.transform.position).normalized;
            waitDirection.y = 0;
            if (waitDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(waitDirection);
                enemy.transform.rotation = Quaternion.Slerp(
                    enemy.transform.rotation,
                    targetRotation,
                    Time.deltaTime * 10f
                );
            }

            // Set Speed = 0 để giữ battleidle
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }

            state = NodeState.Success; // Success để không trigger chase
            return state;
        } // Kết thúc: if (distance > enemy.attackRange)

        // ===== PLAYER TRONG ATTACK RANGE =====
        // Nếu code chạy tới đây = player TRONG attack range
        // Reset timer
        isWaitingToChase = false;
        outOfRangeTimer = 0f;        // ===== CHỈ TIẾP TỤC NẾU PLAYER TRONG ATTACK RANGE =====
                                     // Nếu đang ở ngoài và đang wait thì đã return Success ở trên
                                     // Đoạn code dưới CHỈ chạy khi player TRONG attack range

        // ===== LOCK POSITION NGAY KHI VÀO ATTACK RANGE =====
        if (!enemy.isPositionLocked)
        {
            enemy.lockedPosition = enemy.transform.position;
            enemy.isPositionLocked = true;

            // DISABLE CharacterController để không bị physics/collision đẩy
            if (enemy.characterController != null)
            {
                enemy.characterController.enabled = false;
            }

            if (enemy.showDebugLogs)
            {
                Debug.Log($"🔒 Position LOCKED at {enemy.lockedPosition} | CharacterController DISABLED");
            }
        }

        // ===== KHÔNG CẦN FORCE POSITION Ở ĐÂY - LateUpdate sẽ lo =====
        // LateUpdate() trong EnemyBT sẽ force position SAU physics update

        // ===== QUAN TRỌNG: CHỈ XOAY, KHÔNG DI CHUYỂN KHI ATTACK =====
        // Luôn luôn quay về phía target (mỗi frame)
        Vector3 direction = (targetTransform.position - enemy.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            // Smooth rotation để tự nhiên hơn
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            enemy.transform.rotation = Quaternion.Slerp(
                enemy.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f // Rotation speed
            );
        }

        // ===== ĐẢM BẢO ĐỨNG YÊN: Set Speed = 0 MỖI FRAME =====
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f); // FORCE idle/attack animation
        }

        // ===== CRITICAL: FORCE STOP CharacterController =====
        // CharacterController có gravity và inertia, phải force stop mỗi frame!
        // NHƯNG: Chỉ gọi Move() nếu controller ENABLED (không bị disabled từ attack lock)
        if (enemy.characterController != null && enemy.characterController.enabled)
        {
            // Apply zero movement + small gravity để giữ grounded
            enemy.characterController.Move(Vector3.down * 0.01f);
        }
        // Nếu controller đã disabled (đang attack lock), LateUpdate sẽ force position

        // Check cooldown
        if (Time.time - lastAttackTime < attackCooldown)
        {
            // Vẫn trong cooldown - GIỮ NGUYÊN VỊ TRÍ, CHỈ XOAY
            // QUAN TRỌNG: Return SUCCESS để Selector DỪNG, không chạy Chase!
            state = NodeState.Success;
            return state;
        }

        // ===== COOLDOWN ĐÃ HẾT - ATTACK TIẾP! =====
        // Reset isAttacking để có thể attack lại
        isAttacking = false;

        // Trigger attack animation
        if (animator != null)
        {
            // Reset IsHurt để đảm bảo attack animation có thể trigger (hit state có priority cao)
            animator.SetBool("IsHurt", false);
            animator.SetTrigger("Attack");
            animator.SetFloat("Speed", 0f);
            isAttacking = true;

            // DEBUG LOG
            if (enemy.showDebugLogs)
            {
                Debug.Log($"[{enemy.gameObject.name}] ⚔️ ATTACKING! Distance={distance:F2}m (LOCKED IN PLACE)");
            }
        }

        lastAttackTime = Time.time;

        // QUAN TRỌNG: Return SUCCESS để Selector DỪNG!
        state = NodeState.Success;
        return state;
    }

    /// <summary>
    /// Hủy attack và reset animator về idle/chase state
    /// </summary>
    private void CancelAttack()
    {
        if (isAttacking && animator != null)
        {
            // Reset trigger để không trigger attack nữa
            animator.ResetTrigger("Attack");

            // CRITICAL: FORCE chuyển về idle state ngay lập tức
            // Animator đang trong attack combo (attack1→attack2→battleidle vòng lặp)
            // PHẢI force Play() để thoát khỏi vòng lặp!
            animator.Play("idle", 0, 0f); // Layer 0, normalized time 0 (bắt đầu)

            // Reset IsHurt để đảm bảo có thể chuyển sang chase state
            animator.SetBool("IsHurt", false);
            
            // KHÔNG set Speed ở đây - để TaskChaseTarget xử lý
            // animator.SetFloat("Speed", enemy.chaseSpeed); // ❌ REMOVED - redundant

            isAttacking = false;

            if (enemy.showDebugLogs)
            {
                Debug.Log("🚫 CANCEL ATTACK: Force animator → idle, Speed = chaseSpeed");
            }
        }

        // QUAN TRỌNG: RE-ENABLE CharacterController và Unlock position khi cancel
        if (enemy.characterController != null)
        {
            enemy.characterController.enabled = true;
        }
        enemy.isPositionLocked = false;
    }
}

/// <summary>
/// Task: Đuổi theo target
/// </summary>
public class TaskChaseTarget : Node
{
    private EnemyBT enemy;
    private Transform transform;
    private float moveSpeed;
    private Animator animator;

    public TaskChaseTarget(EnemyBT enemy, Transform transform, float speed, Animator animator)
    {
        this.enemy = enemy;
        this.transform = transform;
        this.moveSpeed = speed;
        this.animator = animator;
    }

    public override NodeState Evaluate()
    {
        object target = GetData("target");

        if (target == null)
        {
            // Reset animation về idle
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }

            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;

        // Check if target still in detection range
        float distance = Vector3.Distance(transform.position, targetTransform.position);
        if (distance > enemy.detectionRange)
        {
            ClearData("target");

            // Reset animation về idle
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }

            state = NodeState.Failure;
            return state;
        }

        // ===== KIỂM TRA: Nếu đang trong attack range → KHÔNG CHASE! =====
        if (distance <= enemy.attackRange)
        {
            // Trong attack range - để TaskAttack xử lý
            // KHÔNG DI CHUYỂN! (TaskAttack sẽ set Speed = 0)
            // ❌ REMOVED: animator.SetFloat("Speed", 0f); // Redundant - TaskAttack sẽ xử lý

            state = NodeState.Success; // Success để Sequence tiếp tục sang TaskAttack
            return state;
        }

        // Move towards target (CHỈ KHI NGOÀI ATTACK RANGE)
        Vector3 direction = (targetTransform.position - transform.position).normalized;
        direction.y = 0;

        // ===== QUAN TRỌNG: RE-ENABLE CharacterController nếu bị disabled từ attack =====
        if (enemy.characterController != null && !enemy.characterController.enabled)
        {
            enemy.characterController.enabled = true;

            if (enemy.showDebugLogs)
            {
                Debug.Log($"✅ CharacterController RE-ENABLED for chase");
            }
        }

        // Dùng CharacterController nếu có, nếu không dùng transform.position
        if (enemy.characterController != null && enemy.characterController.enabled)
        {
            enemy.characterController.Move(direction * moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }

        transform.rotation = Quaternion.LookRotation(direction);

        // Update animation - ĐẢM BẢO reset combat triggers
        if (animator != null)
        {
            animator.ResetTrigger("Attack"); // Reset attack trigger nếu còn
            animator.SetBool("IsHurt", false); // Reset IsHurt để đảm bảo có thể chuyển sang run
            animator.SetFloat("Speed", moveSpeed);
        }

        // DEBUG LOG (every 60 frames = ~1 second)
        if (enemy.showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[{enemy.gameObject.name}] 🏃 CHASING: Speed={moveSpeed:F1}, Distance={distance:F2}m");
        }

        state = NodeState.Running;
        return state;
    }
}

/// <summary>
/// Task: Tuần tra khu vực
/// </summary>
public class TaskPatrol : Node
{
    private EnemyBT enemy;
    private Transform transform;
    private Transform[] patrolPoints;
    private float patrolRadius;
    private float moveSpeed;
    private Animator animator;

    private int currentPatrolIndex = 0;
    private Vector3 currentPatrolPoint;
    private float waitTime = 2f;
    private float waitCounter = 0f;
    private bool isWaiting = false;

    public TaskPatrol(EnemyBT enemy, Transform transform, Transform[] points, float radius, float speed, Animator animator)
    {
        this.enemy = enemy;
        this.transform = transform;
        this.patrolPoints = points;
        this.patrolRadius = radius;
        this.moveSpeed = speed;
        this.animator = animator;

        // Set initial patrol point CHỈ KHI có patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentPatrolPoint = patrolPoints[0].position;
        }
        else if (radius > 0)
        {
            // Nếu có radius nhưng không có points → random patrol
            currentPatrolPoint = GetRandomPatrolPoint();
        }
        else
        {
            // Không có points và radius = 0 → Idle mode (không di chuyển)
            currentPatrolPoint = transform.position;
        }
    }

    public override NodeState Evaluate()
    {
        // LUÔN PATROL - Waiting at patrol point
        if (isWaiting)
        {
            waitCounter += Time.deltaTime;

            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }

            if (waitCounter >= waitTime)
            {
                isWaiting = false;
                waitCounter = 0f;
            }

            state = NodeState.Running;
            return state;
        }

        // Move to patrol point
        float distance = Vector3.Distance(transform.position, currentPatrolPoint);

        if (distance < 0.5f)
        {
            // Reached patrol point, start waiting
            isWaiting = true;

            // DEBUG LOG
            if (enemy.showDebugLogs)
            {
                Debug.Log($"[{enemy.gameObject.name}] 🎯 REACHED patrol point! Waiting {waitTime}s...");
            }

            // Get next patrol point
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                currentPatrolPoint = patrolPoints[currentPatrolIndex].position;

                if (enemy.showDebugLogs)
                {
                    Debug.Log($"[{enemy.gameObject.name}] → Next point: {currentPatrolPoint}");
                }
            }
            else if (patrolRadius > 0)
            {
                // Random patrol nếu có radius
                currentPatrolPoint = GetRandomPatrolPoint();

                if (enemy.showDebugLogs)
                {
                    Debug.Log($"[{enemy.gameObject.name}] → Random patrol point: {currentPatrolPoint}");
                }
            }
            else
            {
                // Không có points và radius = 0 → Đứng yên idle
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                }

                if (enemy.showDebugLogs)
                {
                    Debug.Log($"[{enemy.gameObject.name}] ⚠️ No patrol points and radius=0, staying idle");
                }

                state = NodeState.Running;
                return state;
            }
        }
        else
        {
            // Move towards patrol point
            Vector3 direction = (currentPatrolPoint - transform.position).normalized;
            direction.y = 0;

            // ===== QUAN TRỌNG: RE-ENABLE CharacterController nếu bị disabled từ attack =====
            if (enemy.characterController != null && !enemy.characterController.enabled)
            {
                enemy.characterController.enabled = true;

                if (enemy.showDebugLogs)
                {
                    Debug.Log($"✅ CharacterController RE-ENABLED for patrol");
                }
            }

            // Dùng CharacterController nếu có
            if (enemy.characterController != null && enemy.characterController.enabled)
            {
                enemy.characterController.Move(direction * (moveSpeed * 0.5f) * Time.deltaTime);
            }
            else
            {
                transform.position += direction * (moveSpeed * 0.5f) * Time.deltaTime; // Patrol chậm hơn chase
            }

            transform.rotation = Quaternion.LookRotation(direction);

            if (animator != null)
            {
                // Reset combat triggers để đảm bảo có thể chuyển sang walk từ combat state
                animator.ResetTrigger("Attack");
                animator.SetBool("IsHurt", false);
                animator.SetFloat("Speed", moveSpeed * 0.5f);
            }

            // DEBUG LOG (every 60 frames)
            if (Time.frameCount % 60 == 0 && enemy.showDebugLogs)
            {
                Debug.Log($"[{enemy.gameObject.name}] 🚶 PATROL: Moving to {currentPatrolPoint}, Distance={distance:F2}m, Speed={moveSpeed * 0.5f:F1}");
            }
        }

        state = NodeState.Running;
        return state;
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        randomDirection.y = transform.position.y;

        return randomDirection;
    }
}

#endregion


