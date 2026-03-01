using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Linq;

public class EnemyDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float autoTargetRadius = 5f; // Bán kính tự động target enemy gần nhất
    [SerializeField] private float detectionUpdateInterval = 0.1f; // Update detection every 0.1 seconds instead of every frame

    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera cnCamera;
    [SerializeField] private float combatCameraDistance = 9f;
    [SerializeField] private float normalCameraDistance = 5f;
    [SerializeField] private float cameraTransitionSpeed = 2f;
    // [SerializeField] private float cameraLookOffset = 0.5f; // Unused - commented out

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
    private Character character;
    private Animator animator;
    private CharacterController controller;
    private Transform nearestEnemy;
    private bool isInCombat = false;
    private bool isAttacking = false;
    private Coroutine cameraTransitionCoroutine;
    private Coroutine smoothMovementCoroutine;
    private Coroutine smoothRotationCoroutine;
    private Vector3 lastEnemyPosition;

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
        character = GetComponent<Character>();
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();

        if (!cnCamera)
            cnCamera = GetComponent<CinemachineCamera>();

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

        // Update combat state - only when enemy is detected AND enemy is aware of player
        isInCombat = nearestEnemy != null && IsEnemyAwareOfPlayer();
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
        Debug.Log("[EnemyDetection] Entered Combat - Enemy detected player");
    }

    private void ExitCombat()
    {
        isInCombat = false;
        Debug.Log("[EnemyDetection] Exited Combat - No enemy or enemy lost player");
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
        if (cnCamera == null) return;

        if (isInCombat && nearestEnemy != null)
        {
            // Smooth transition to combat distance
            if (cameraTransitionCoroutine != null)
                StopCoroutine(cameraTransitionCoroutine);
            cameraTransitionCoroutine = StartCoroutine(SmoothCameraDistance(combatCameraDistance));
        }
        else
        {
            // Smooth transition to normal distance
            if (cameraTransitionCoroutine != null)
                StopCoroutine(cameraTransitionCoroutine);
            cameraTransitionCoroutine = StartCoroutine(SmoothCameraDistance(normalCameraDistance));
        }
    }

    private IEnumerator SmoothCameraDistance(float targetDistance)
    {
        // Get current distance from camera's body component
        var body = cnCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachineThirdPersonFollow;
        if (body == null) yield break;

        float startDistance = body.CameraDistance;
        float elapsed = 0f;

        while (elapsed < 1f / cameraTransitionSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed * cameraTransitionSpeed;
            body.CameraDistance = Mathf.Lerp(startDistance, targetDistance, t);
            yield return null;
        }

        body.CameraDistance = targetDistance;
        cameraTransitionCoroutine = null;
    }
    #endregion

    #region Combat Movement (Animation Event Controlled)
    // REMOVED: Auto-rotation logic - now controlled by Animation Events

    // Animation Event: Smart rotation - enemy priority over movement input
    public void AE_SmartRotate()
    {
        Debug.Log("[EnemyDetection] AE_SmartRotate called");

        // Priority 1: Rotate to enemy if available
        if (nearestEnemy != null)
        {
            Debug.Log("[EnemyDetection] Rotating to enemy");
            // Disable root motion when enemy is present (manual rotation)
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }
            AE_RotateToEnemy();
        }
        // Priority 2: Rotate to movement input if no enemy
        else
        {
            Debug.Log("[EnemyDetection] Rotating to movement input - enabling root motion");
            // Enable root motion when no enemy (for normal attack movement)
            if (animator != null && useRootMotionWhenNoEnemy)
            {
                animator.applyRootMotion = true;
            }
            AE_RotateToMovementInput();
        }
    }

    // Animation Event: Rotate to face nearest enemy
    public void AE_RotateToEnemy()
    {
        if (nearestEnemy == null) return;

        Vector3 directionToEnemy = (nearestEnemy.position - transform.position).normalized;
        directionToEnemy.y = 0; // Keep rotation on horizontal plane

        if (directionToEnemy.magnitude > 0.1f)
        {
            // Cancel previous rotation if any
            if (smoothRotationCoroutine != null)
            {
                StopCoroutine(smoothRotationCoroutine);
            }

            // Start smooth rotation coroutine
            smoothRotationCoroutine = StartCoroutine(SmoothRotateToEnemy(directionToEnemy));
        }
    }

    // Smooth rotation coroutine for better visual effect
    private System.Collections.IEnumerator SmoothRotateToEnemy(Vector3 directionToEnemy)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(directionToEnemy);

        float elapsed = 0f;

        while (elapsed < smoothRotationDuration)
        {
            float progress = elapsed / smoothRotationDuration;
            // Use smooth step for more natural rotation
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, smoothProgress);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final rotation is exact
        transform.rotation = targetRotation;

        // Clear coroutine reference
        smoothRotationCoroutine = null;
    }

    // Animation Event: Move toward enemy during attack
    public void AE_MoveTowardEnemy()
    {
        Debug.Log("[EnemyDetection] AE_MoveTowardEnemy called");

        if (nearestEnemy == null || controller == null)
        {
            Debug.Log("[EnemyDetection] No enemy or controller");
            return;
        }

        // Get current weapon's attack range
        float attackRange = GetCurrentWeaponAttackRange();
        if (attackRange <= 0f) attackRange = autoTargetRadius; // Fallback
        if (attackRange <= 0f) attackRange = swordAttackRange; // Fallback
        if (attackRange <= 0f) attackRange = axeAttackRange; // Fallback
        if (attackRange <= 0f) attackRange = mageAttackRange; // Fallback

        float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.position);
        Debug.Log($"[EnemyDetection] Distance to enemy: {distanceToEnemy}, Attack range: {attackRange}");

        // Only move if enemy is beyond attack range
        if (distanceToEnemy > attackRange)
        {
            // Stop any existing movement coroutine
            if (smoothMovementCoroutine != null)
            {
                StopCoroutine(smoothMovementCoroutine);
            }

            // Start smooth movement coroutine
            smoothMovementCoroutine = StartCoroutine(SmoothMoveTowardEnemy(0.2f));
            Debug.Log("[EnemyDetection] Starting smooth movement toward enemy");
        }
        else
        {
            Debug.Log("[EnemyDetection] Enemy within attack range, no movement needed");
        }
    }

    // Smooth movement toward enemy over duration
    private IEnumerator SmoothMoveTowardEnemy(float duration)
    {
        if (nearestEnemy == null || controller == null) yield break;

        float elapsed = 0f;
        Vector3 startPosition = transform.position;

        // Calculate target position (stop at attack range)
        float attackRange = GetCurrentWeaponAttackRange();
        Vector3 directionToEnemy = (nearestEnemy.position - transform.position).normalized;
        directionToEnemy.y = 0;
        Vector3 targetPosition = nearestEnemy.position - (directionToEnemy * attackRange);

        while (elapsed < duration && nearestEnemy != null)
        {
            elapsed += Time.deltaTime;

            // Check if we've reached attack range
            float currentDistance = Vector3.Distance(transform.position, nearestEnemy.position);
            if (currentDistance <= attackRange)
            {
                Debug.Log("[EnemyDetection] Reached attack range, stopping movement");
                break;
            }

            // Move toward enemy with controlled speed (reuse directionToEnemy variable)
            directionToEnemy = (nearestEnemy.position - transform.position).normalized;
            directionToEnemy.y = 0; // Keep on horizontal plane

            Vector3 movement = directionToEnemy * combatMoveSpeed * Time.deltaTime;
            controller.Move(movement);

            yield return null;
        }

        smoothMovementCoroutine = null;
        Debug.Log("[EnemyDetection] Smooth movement completed");
    }

    // Get attack range from current weapon
    private float GetCurrentWeaponAttackRange()
    {
        if (character == null) return 0f;

        var equipment = character.GetComponent<EquipmentSystem>();
        if (equipment == null) return 0f;

        var weapon = equipment.GetCurrentWeapon();
        if (weapon == null) return 0f;

        // Check if weapon has attack range data
        // You may need to add attackRange field to WeaponSO
        // For now, return a default value based on weapon type
        return weapon.weaponType switch
        {
            WeaponType.Sword => swordAttackRange,
            WeaponType.Axe => axeAttackRange,
            WeaponType.Mage => mageAttackRange,
            _ => swordAttackRange
        };
    }

    // Animation Event: Rotate to movement input direction
    public void AE_RotateToMovementInput()
    {
        if (character == null) return;

        // Get current movement input
        Vector2 movementInput = character.playerInput.actions["Move"].ReadValue<Vector2>();

        if (movementInput.magnitude > 0.1f)
        {
            Vector3 moveDirection = new Vector3(movementInput.x, 0, movementInput.y);
            moveDirection = character.cameraTransform.TransformDirection(moveDirection);
            moveDirection.y = 0;
            moveDirection.Normalize();

            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = targetRotation; // Instant rotation for AE
            }
        }
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
                transform.rotation = Quaternion.LookRotation(directionToEnemy);
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
