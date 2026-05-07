using UnityEngine;
using System.Collections;
using System.Linq;

using Fusion;

public class EnemyDetection : NetworkBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float autoTargetRadius = 5f; // Bán kính tự động target enemy gần nhất
    [SerializeField] private float detectionUpdateInterval = 0.1f; // Update detection every 0.1 seconds instead of every frame

    // Camera distance is owned by PlayerCameraDistanceManager.

    [Header("Combat Movement")]
    [SerializeField] private float combatMoveSpeed = 2f; // Tốc độ di chuyển về phía enemy khi đánh
    // [SerializeField] private float rotationSpeed = 8f; // Unused - commented out
    [SerializeField] private float smoothRotationDuration = 0.3f; // Thời gian xoay mượt

    [Header("Root Motion Control")]
    [SerializeField] private bool useRootMotionWhenNoEnemy = true; // Dùng root motion khi không có enemy
    // [SerializeField] private bool moveTowardEnemyWhenAttacking = true; // Unused - commented out

    [Header("Weapon's Attack Range")]
    [SerializeField] private float swordAttackRange = 3f;
    [SerializeField] private float axeAttackRange = 3f;
    [SerializeField] private float mageAttackRange = 7f;

    // Private variables
    private EquipmentSystem equipment;
    private Character character;
    private Animator animator;
    private CharacterController controller;
    private Transform nearestEnemy;
    private bool isInCombat = false;
    private bool isAttacking = false;
    private Coroutine smoothMovementCoroutine;
    private Coroutine smoothRotationCoroutine;
    private Vector3 lastEnemyPosition;

    private PlayerCameraDistanceManager cameraDistanceManager;

    // Optimization variables
    private float lastDetectionTime = 0f;
    private float detectionRadiusSquared; // Cache squared radius for faster distance checks
    private float autoTargetRadiusSquared;
    private Collider[] enemyCollidersCache = new Collider[50]; // Reusable array to avoid allocations

    // Events
    public System.Action<Transform> OnEnemyDetected;
    public System.Action OnNoEnemyDetected;
    public System.Action<Transform> OnNearestEnemyChanged;

    private void Awake()
    {
        character = GetComponentInParent<Character>();
        animator = GetComponentInParent<Animator>();
        controller = GetComponentInParent<CharacterController>();
        equipment = GetComponentInParent<EquipmentSystem>();

        // Cache squared radii for faster distance checks
        detectionRadiusSquared = detectionRadius * detectionRadius;
        autoTargetRadiusSquared = autoTargetRadius * autoTargetRadius;
    }

    private void Start()
    {
        // Subscribe to character state changes
        if (character != null)
        {
            // Listen for combat state changes
            StartCoroutine(MonitorCombatState());
        }
    }

    private void Update()
    {
        // Update enemy detection at intervals instead of every frame
        if (Time.time - lastDetectionTime >= detectionUpdateInterval)
        {
            UpdateEnemyDetection();
            lastDetectionTime = Time.time;
        }
        ResolveLocalCameraDistanceManager();
        UpdateCameraSystem();
    }

    #region Enemy Detection
    private void UpdateEnemyDetection()
    {
        // Use cached array to avoid allocations
        int enemyCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, enemyCollidersCache, enemyLayer);
        Transform newNearestEnemy = GetNearestEnemyOptimized(enemyCount);

        // Check if nearest enemy changed
        if (newNearestEnemy != nearestEnemy)
        {
            if (newNearestEnemy != null)
            {
                OnNearestEnemyChanged?.Invoke(newNearestEnemy);
                if (nearestEnemy == null)
                {
                    OnEnemyDetected?.Invoke(newNearestEnemy);
                    // REMOVED: Auto EnterCombat - now only when enemy detects player
                }
            }
            else
            {
                OnNoEnemyDetected?.Invoke();
                ExitCombat();
            }

            nearestEnemy = newNearestEnemy;
        }

        // Update combat state - prioritize enemy proximity over enemy awareness state to prevent root motion bugs
        isInCombat = nearestEnemy != null;
    }

    // Check if enemy is aware of player (enemy has detected player)
    private bool IsEnemyAwareOfPlayer()
    {
        if (nearestEnemy == null) return false;

        // Check if enemy has a detection component or is in combat state
        var enemyDetection = nearestEnemy.GetComponent<EnemyDetection>();
        if (enemyDetection != null)
        {
            return enemyDetection.IsInCombat();
        }

        // Fallback: Check if enemy is looking at player or in combat animation
        var enemyAnimator = nearestEnemy.GetComponent<Animator>();
        if (enemyAnimator != null)
        {
            // Check if enemy is in combat state - safely check if parameter exists first
            bool isInCombatParam = false;
            try {
                var param = enemyAnimator.parameters.FirstOrDefault(p => p.name == "isInCombat");
                if (param != null) {
                    isInCombatParam = enemyAnimator.GetBool("isInCombat");
                }
            } catch {
                // Parameter doesn't exist in this animator
            }
            
            return isInCombatParam ||
                   enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Combat") ||
                   enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Alert");
        }

        // Default: assume enemy is aware if within detection radius
        return true;
    }

    private Transform GetNearestEnemyOptimized(int enemyCount)
    {
        if (enemyCount == 0) return null;

        Transform nearest = null;
        float nearestDistanceSquared = float.MaxValue;
        Vector3 playerPos = transform.position;

        for (int i = 0; i < enemyCount; i++)
        {
            Collider enemy = enemyCollidersCache[i];
            if (enemy == null) continue;

            // Use squared distance to avoid expensive square root calculations
            Vector3 enemyPos = enemy.transform.position;
            float distanceSquared = (enemyPos - playerPos).sqrMagnitude;

            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearest = enemy.transform;
            }
        }

        return nearest;
    }

    private void EnterCombat()
    {
        isInCombat = true;
    }

    private void ExitCombat()
    {
        isInCombat = false;
    }

    // Public method for enemies to call when they detect player
    public void OnEnemyDetectedPlayer(Transform enemy)
    {
        if (nearestEnemy == enemy)
        {
            EnterCombat();
        }
    }

    // Public method for enemies to call when they lose player
    public void OnEnemyLostPlayer(Transform enemy)
    {
        if (nearestEnemy == enemy)
        {
            ExitCombat();
        }
    }
    #endregion

    #region Camera System
    private void UpdateCameraSystem()
    {
        if (cameraDistanceManager == null) return;

        bool clamp = isInCombat && nearestEnemy != null;
        cameraDistanceManager.SetCombatClamp(clamp);
    }
    #endregion

    private void ResolveLocalCameraDistanceManager()
    {
        // Only local player should ever drive the camera
        if (cameraDistanceManager != null) return;
        if (Character.LocalCharacter == null) return;
        if (character == null || character != Character.LocalCharacter) return;

        cameraDistanceManager = PlayerNetworkSetup.LocalCameraDistanceManager;
    }

    // 2. SỬA 3 BIẾN NÀY THÀNH BIẾN MẠNG
    [Networked] private float _moveTowardTimer { get; set; }
    [Networked] private float _rotateTimer { get; set; }
    [Networked] private Vector3 _targetRotation { get; set; }

    // HÀM MỚI: Xử lý di chuyển chuẩn mạng
    private Vector3 _rootMotionDeltaPosition;

    // Lấy Root Motion từ Animator để đồng bộ với Fusion
    private void OnAnimatorMove()
    {
        if (animator == null || !animator.applyRootMotion) return;
        
        // Cộng dồn Root Motion sinh ra trong Update
        _rootMotionDeltaPosition += animator.deltaPosition;
    }

    // HÀM MỚI: Xử lý di chuyển chuẩn mạng
    public override void FixedUpdateNetwork()
    {
        // Chỉ cho phép di chuyển nếu bạn là người đang bấm nút (Input) hoặc là Server (State)
        if (!HasStateAuthority && !HasInputAuthority) return;

        // 1. Xử lý Root Motion (Khi không có quái)
        if (animator != null && animator.applyRootMotion && _rootMotionDeltaPosition.sqrMagnitude > 0)
        {
            controller.Move(_rootMotionDeltaPosition);
            _rootMotionDeltaPosition = Vector3.zero; // Xóa sau khi dùng
        }
        else
        {
            _rootMotionDeltaPosition = Vector3.zero; // Xóa rác nếu không dùng
        }

        // 2. Xử lý bước tới và xoay (Chỉ chạy khi tiến về phía trước, bỏ qua Resimulation để không bị trừ lố giờ)
        if (Runner.IsForward)
        {
            // Xử lý xoay mượt theo Tick mạng (Runner.DeltaTime)
            if (_rotateTimer > 0)
            {
                _rotateTimer -= Runner.DeltaTime;
                Quaternion targetRot = Quaternion.LookRotation(_targetRotation);
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRot, 15f * Runner.DeltaTime);
            }

            // Xử lý bước tới chém theo Tick mạng
            if (_moveTowardTimer > 0 && nearestEnemy != null)
            {
                _moveTowardTimer -= Runner.DeltaTime;
                
                // 1. Lấy tầm đánh của vũ khí hiện tại
                float attackRange = GetCurrentWeaponAttackRange();

                // 2. Tính khoảng cách thực tế đến quái (bỏ qua trục Y để tính trên mặt phẳng ngang)
                Vector3 vectorToEnemy = nearestEnemy.position - transform.position;
                vectorToEnemy.y = 0;
                float currentDistance = vectorToEnemy.magnitude;

                // 3. Nếu vẫn còn ở xa hơn tầm đánh thì mới được phép lướt tới
                if (currentDistance > attackRange)
                {
                    Vector3 direction = vectorToEnemy.normalized;
                    float moveStep = combatMoveSpeed * Runner.DeltaTime;

                    // TÍNH NĂNG CHỐNG LỐ (Anti-Overshoot): 
                    // Nếu bước nhảy tiếp theo làm nhân vật đâm xuyên qua ranh giới tầm đánh,
                    // thì bóp ngắn khoảng cách lướt lại cho vừa chạm mép ranh giới.
                    if (currentDistance - moveStep < attackRange)
                    {
                        moveStep = currentDistance - attackRange;
                    }

                    controller.Move(direction * moveStep);
                }
                else
                {
                    // Đã vào đúng tầm chém! Dừng ngay công tắc lướt lại để đứng yên múa kiếm
                    _moveTowardTimer = 0f;
                }
            }
        }
    }

    #region Combat Movement (Animation Event Controlled)

    // CÔNG TẮC 1: Xoay thông minh (Có quái thì hút, không quái thì xoay theo phím)
    public void AE_SmartRotate()
    {
        if (nearestEnemy != null)
        {
            if (animator != null) animator.applyRootMotion = false; 

            // Tính hướng chuẩn xác và an toàn
            Vector3 dir = nearestEnemy.position - transform.position;
            dir.y = 0; // Khóa trục Y

            if (dir.sqrMagnitude > 0.01f)
            {
                _targetRotation = dir.normalized;
                _rotateTimer = smoothRotationDuration; // Bật công tắc xoay về quái
            }
        }
        else
        {
            if (animator != null && useRootMotionWhenNoEnemy) animator.applyRootMotion = true;
            AE_RotateToMovementInput(); 
        }
    }

    // CÔNG TẮC 2: Bước tới chém
    public void AE_MoveTowardEnemy()
    {
        if (nearestEnemy == null) return;
        
        float attackRange = GetCurrentWeaponAttackRange();
        float dist = Vector3.Distance(transform.position, nearestEnemy.position);
        
        if (dist > attackRange)
        {
            _moveTowardTimer = 0.2f; // BẬT CÔNG TẮC BƯỚC TỚI (0.2 giây)
        }
    }

    // Hàm xoay theo phím (Chỉ gọi khi không có quái) - ĐÃ CẬP NHẬT CHUẨN MẠNG
    public void AE_RotateToMovementInput()
    {
        if (character == null) return;

        Vector2 movementInput = character.playerInput.actions["Move"].ReadValue<Vector2>();

        if (movementInput.sqrMagnitude > 0.01f)
        {
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);
            moveDirection = character.cameraTransform.TransformDirection(moveDirection);
            moveDirection.y = 0; // Khóa Y để không bị xoay ngửa lên trời

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                _targetRotation = moveDirection.normalized;
                _rotateTimer = smoothRotationDuration;
            }
        }
    }

    // Get attack range from current weapon
    private float GetCurrentWeaponAttackRange()
    {
        if (character == null || equipment == null) return swordAttackRange;

        var weapon = equipment.GetCurrentWeapon();
        if (weapon == null) return swordAttackRange;

        return weapon.weaponType switch
        {
            WeaponType.Sword => swordAttackRange,
            WeaponType.Axe => axeAttackRange,
            WeaponType.Mage => mageAttackRange,
            _ => swordAttackRange
        };
    }
    #endregion

    #region Combat State Monitoring
    private IEnumerator MonitorCombatState()
    {
        while (true)
        {
            // Check if character is attacking
            bool wasAttacking = isAttacking;
            isAttacking = IsCharacterAttacking();

            // Update root motion based on enemy presence
            UpdateRootMotion();

            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }

    private bool IsCharacterAttacking()
    {
        if (animator == null || character == null) return false;

        // Check if character is in attack state (with null checks)
        bool isInAttackState = (character.attacking != null && character.movementSM.currentState == character.attacking);

        // Check animator state with try-catch to handle missing parameters
        bool isInAttackAnimation = false;
        try {
            isInAttackAnimation = animator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
        } catch {
            // Ignore errors
        }

        // Check for isAttacking parameter - may not exist in all animators
        bool isAttackingParam = false;
        try {
            // First check if parameter exists to avoid errors
            if (animator != null) {
                var param = animator.parameters.FirstOrDefault(p => p.name == "isAttacking");
                if (param != null) {
            isAttackingParam = animator.GetBool("isAttacking");
                }
            }
        } catch {
            // Parameter doesn't exist in this animator
        }

        return isInAttackState || isInAttackAnimation || isAttackingParam;
    }

    private void UpdateRootMotion()
    {
        if (animator == null) return;

        // Use root motion when no enemy, disable when enemy present
        bool shouldUseRootMotion = useRootMotionWhenNoEnemy && !isInCombat;
        animator.applyRootMotion = shouldUseRootMotion;
    }
    #endregion

    #region Public API
    public Transform GetNearestEnemy()
    {
        return nearestEnemy;
    }

    public bool IsInCombat()
    {
        return isInCombat;
    }

    public void ForceLookAtEnemy()
    {
        if (nearestEnemy != null)
        {
            Vector3 directionToEnemy = (nearestEnemy.position - transform.position).normalized;
            directionToEnemy.y = 0;
            if (directionToEnemy.magnitude > 0.1f)
            {
                character.transform.rotation = Quaternion.LookRotation(directionToEnemy);
            }
        }
    }

    public void SetCombatState(bool inCombat)
    {
        isInCombat = inCombat;
    }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected()
    {
        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw auto-target radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, autoTargetRadius);

        // Draw line to nearest enemy
        if (nearestEnemy != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nearestEnemy.position);
        }
    }
    #endregion
}


