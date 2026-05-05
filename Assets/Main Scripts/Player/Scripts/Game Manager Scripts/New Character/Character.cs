using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
public enum CharacterStateSync { Standing, Jumping, Crouching, Sprinting, Dash, HardStop, DrawWeapon, SheathWeapon, CombatMove, Attack, GetHit, Die }
public class Character : NetworkBehaviour
{
    [Header("Controls")]
    public float playerSpeed = 5.0f;
    public float crouchSpeed = 2.0f;
    public float sprintSpeed = 7.0f;
    public float dashSpeed = 10.0f;
    public float jumpHeight = 0.8f;
    [SerializeField] public float sprintJumpHeight = 1.0f;
    public float gravityMultiplier = 2;
    public float rotationSpeed = 5f;
    public float crouchColliderHeight = 1.35f;

    // Base speeds (stored to apply gem multipliers)
    private float basePlayerSpeed;
    private float baseCrouchSpeed;
    private float baseSprintSpeed;
    private float baseDashSpeed;

    [Header("Dash Settings")]
    [SerializeField] public float dashDuration = 0.2f; // Duration of the dash
    [SerializeField] public int maxConsecutiveDashes = 2; // Maximum consecutive dashes allowed
    [SerializeField] public float dashCooldown = 1f; // Cooldown between consecutive dashes (seconds)
    [SerializeField] public float dashChainCooldown = 2.5f; // Cooldown after max consecutive dashes (seconds)
    [Tooltip("Nhân dashSpeed theo tiến độ dash (0 = đầu, 1 = cuối).")]
    [SerializeField] public AnimationCurve dashSpeedMultiplierOverTime = DefaultDashSpeedCurve();

    static AnimationCurve DefaultDashSpeedCurve()
    {
        var c = new AnimationCurve(
            new Keyframe(0f, 1.12f, 0f, -0.35f),
            new Keyframe(0.22f, 1.02f, -0.15f, -0.12f),
            new Keyframe(1f, 0.88f, -0.2f, 0f));
        c.preWrapMode = WrapMode.ClampForever;
        c.postWrapMode = WrapMode.ClampForever;
        return c;
    }

    [Header("Animation Smoothing")]
    [Range(0, 1)]
    public float speedDampTime = 0.1f;
    [Range(0, 1)]
    public float velocityDampTime = 0.9f;
    [Range(0, 1)]
    public float rotationDampTime = 0.2f;
    
    [Range(1f, 50f)]
    [Tooltip("Tốc độ xoay người khi tung đòn đánh (Càng cao xoay càng nhanh). Khuyến nghị: 15-25")]
    public float attackRotationSpeed = 20f;

    [Range(0, 1)]
    public float airControl = 0.5f;

    public StateMachine movementSM;
    public BaseMoveState standing;
    public JumpingState jumping;
    public FallingState falling;
    public CrouchingState crouching;
    public SprintState sprinting;
    public SprintJumpState sprintjumping;
    public DashState dashing;
    public HardStopState hardStop;


    public CombatMoveState combatMove;
    public AttackState attacking;
    public GetHitState getHit;
    public DieState dieState;

    [HideInInspector]
    public float gravityValue = -9.81f;

    [Header("Landing & Fall Settings")]
    [Tooltip("Khoảng cách rơi tối thiểu (mét) để bắt buộc chạy animation Land")]
    public float minFallDistanceForLanding = 1.2f;

    [Tooltip("Khoảng cách mặt đất tối thiểu để ngắt Dash và bắt đầu rơi (mét)")]
    public float enoughDistanceToFall = 0.8f;

    [Tooltip("Độ cao tối thiểu để kích hoạt Parkour Roll khi tiếp đất (mét)")]
    public float farFallDistanceForRoll = 5.0f;

    [Tooltip("Tốc độ triệt tiêu quán tính phương ngang khi rơi.")]
    public float fallInertiaDecayRate = 3f;

    // === THÊM BIẾN NÀY ĐỂ ÉP RƠI NHANH DẦN ===
    [Tooltip("Gia tốc cộng dồn ép nhân vật rơi nhanh dần theo thời gian (Tránh cảm giác trôi nổi)")]
    public float extraFallAcceleration = 25f;
    // ==========================================

    [HideInInspector] public Vector3 momentumToInherit;
    [HideInInspector] public bool isRollLanding;
<<<<<<< HEAD
    [HideInInspector] public float jumpStartY; // Lưu lại tọa độ Y lúc bắt đầu nhảy
=======
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6

    [HideInInspector]
    public bool requireLanding;

    [HideInInspector]
    public float normalColliderHeight;
    [HideInInspector]
    public CharacterController controller;
    [HideInInspector]
    public PlayerInput playerInput;
    [HideInInspector]
    public Transform cameraTransform;
    [HideInInspector]
    public Animator animator;
    [HideInInspector]
    public Vector3 playerVelocity;
    [HideInInspector]
    public Vector3 cachedPlanarForward;
    [HideInInspector]
    public Vector3 cachedPlanarRight;
    [HideInInspector]
    public SkillLock skillLock;

    public State currentLocomotionState;
    public static Character LocalCharacter;
    [Networked] public CharacterStateSync NetworkedState { get; set; }
    [Networked] public bool NetworkedIsLanding { get; set; }
    private ChangeDetector _changeDetector;
    public NetworkInputData previousInput;
    public NetworkInputData currentInput;
    public Vector3 CalculatedVelocity;
    
    public State lastStateBeforeHit; // Track state before getting hit
    public float lastAttackInputTime; // Track when attack was last pressed

    //public bool isInCombatState { get; set; }
    public bool isWeaponDrawn { get; set; }
    public bool IsDashing { get; set; } // For invincibility frame during dash
    public float dashLockUntil = 0f; // Thời điểm trước đó dash bị khóa (để tránh auto-dash sau khi bị hit)

    public float lastJumpTime; 

    /// <summary> Khi false, bỏ qua input nhảy cho đến khi vào lại locomotion. Nhảy thật sự còn cần <see cref="TryConsumeJumpBuffered"/> (ray chân + buffer + coyote nhẹ). </summary>
    public bool canStartJump => IsGroundedStable() && (Runner.SimulationTime >= lastJumpTime + jumpCooldownSeconds);

    [Header("Jump — ground & buffer")]
    [Tooltip("Layer được coi là mặt đất cho 2 ray dưới chân. Để trống = mọi layer (DefaultRaycastLayers).")]
    [SerializeField] LayerMask groundLayers;
    [SerializeField] float groundRayDistance = 0.38f;
    [SerializeField] float footRayHalfWidth = 0.12f;
    [SerializeField] float jumpBufferDuration = 0.12f;
    [SerializeField] float coyoteTime = 0.08f;
    [Tooltip("Coyote không áp dụng khi vận tốc Y lớn hơn (đang nhảy lên, tránh double jump).")]
    [SerializeField] float coyoteMaxUpSpeed = 0.35f;
    [Tooltip("Khoảng thời gian tối thiểu giữa hai lần nhảy (chống spam Space + buffer tích trên không).")]
    [SerializeField] float jumpCooldownSeconds = 0.32f;

    [Header("Animator locomotion speed")]
    [Tooltip("Input magnitude >= giá trị này được coi là đang di chuyển (cập nhật thời điểm \"có move\").")]
    [SerializeField] float locomotionIdleMoveThreshold = 0.05f;
    [Tooltip("Sau khi input < threshold liên tục đủ lâu mới snap speed=0 (tránh jitter, vẫn blend mượt lúc vừa buông phím).")]
    [SerializeField] float locomotionIdleSnapAfterSeconds = 0.2f;
    float lastLocomotionMoveTime;

    /// <summary>Hai ray dưới chân đều chạm ground layer (cập nhật mỗi Update, trước input).</summary>
    public bool CachedGroundedFeet { get; private set; }

    private InputAction jumpActionCache;
    float jumpBufferRemaining;
    float lastGroundedFeetTime = -999f;
    float lastGroundedStableTime = -999f;
    float jumpAllowedAfterTime = -999f;

    private int originalLayer; // Store original layer before dash

    private Vector3 initialModelLocalPosition;

    private void Awake()
    {
        // Lấy bộ điều khiển vật lý ở Root
        controller = GetComponent<CharacterController>(); 
        // (Nếu ở các bước trước bạn đổi tên biến thành _cc thì dùng _cc nhé)

        // Lấy bộ thu tín hiệu ở Root
        playerInput = GetComponent<PlayerInput>();
        skillLock = GetComponentInChildren<SkillLock>();

        // Lấy animator ở các Con
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            // Lưu lại tọa độ gốc của Model (Thường là 0, -1, 0)
            initialModelLocalPosition = animator.transform.localPosition;
            
            // Ng định tắt Root Motion một lần và mãi mãi
            animator.applyRootMotion = false;
        }
    }

    private void LateUpdate()
    {
        // Hàm này chạy sau cùng mọi frame.
        // Bất kỳ Animation nào có tính kéo Model dịch chuyển, ta đều "giết" nó về lại vị trí trung tâm của Root.
        if (animator != null)
        {
            animator.transform.localPosition = initialModelLocalPosition;
            animator.transform.localRotation = Quaternion.identity;
        }
    }
    // Start is called before the first frame update
    public override void Spawned()
    {
        if (HasInputAuthority) 
        {
            LocalCharacter = this;
            
            // GI?I THI?U V?I UI:
            if (AbilityIconManager.Instance != null)
            {
                var wc = GetComponent<WeaponController>();
                AbilityIconManager.Instance.BindToLocalPlayer(wc);
            }
        }
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        
        CalculatedVelocity = Vector3.zero; // Reset v?n t?c ngay khi sinh ra
    }
    
    private void Start()
    {
        // Kiểm tra an toàn: Nếu chưa có PlayerInput, báo lỗi ngay lập tức thay vì để Crash
        if (playerInput == null)
        {
            Debug.LogError("<color=red>[Character]</color> LỖI CỰC NẶNG: Không tìm thấy component PlayerInput trên Prefab! Hãy mở Prefab Player và thêm nó vào!");
            return; // Thoát ngang hàm Start để tránh Crash các dòng bên dưới
        }

        // Load saved key binding overrides từ Settings
        InputRebindHelper.LoadBindingOverrides(playerInput);
        
        // Kiểm tra an toàn trước khi gán Jump
        if (playerInput.actions != null)
        {
            jumpActionCache = playerInput.actions["Jump"];
        }
        // Thay vì gán thẳng, chúng ta check null và tìm đường vòng
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            // Fallback: Tìm bừa 1 cái Camera bất kỳ trong scene nếu lỡ quên tag
            Camera fallbackCam = FindFirstObjectByType<Camera>();
            if (fallbackCam != null)
            {
                cameraTransform = fallbackCam.transform;
                Debug.LogWarning("<color=yellow>[Character]</color> Camera.main bị null (Chưa gắn tag MainCamera). Đã tự động lấy Camera khác thay thế!");
            }
            else
            {
                Debug.LogError("<color=red>[Character]</color> BÁO ĐỘNG: Không có bất kỳ Camera nào trong Scene!");
            }
        }
        cachedPlanarForward = transform.forward;
        cachedPlanarForward.y = 0f;
        if (cachedPlanarForward.sqrMagnitude < 0.0001f) cachedPlanarForward = Vector3.forward;
        cachedPlanarForward.Normalize();

        cachedPlanarRight = transform.right;
        cachedPlanarRight.y = 0f;
        if (cachedPlanarRight.sqrMagnitude < 0.0001f) cachedPlanarRight = Vector3.right;
        cachedPlanarRight.Normalize();

        movementSM = new StateMachine();
        standing = new StandingState(this, movementSM);
        jumping = new JumpingState(this, movementSM);
        falling = new FallingState(this, movementSM);
        crouching = new CrouchingState(this, movementSM);
        sprinting = new SprintState(this, movementSM);
        sprintjumping = new SprintJumpState(this, movementSM);
        dashing = new DashState(this, movementSM);
        hardStop = new HardStopState(this, movementSM);

        combatMove = new CombatMoveState(this, movementSM);
        attacking = new AttackState(this, movementSM);
        getHit = new GetHitState(this, movementSM);
        dieState = new DieState(this, movementSM);        

        currentLocomotionState = standing;
        movementSM.Initialize(currentLocomotionState);

        normalColliderHeight = controller.height;
        gravityValue *= gravityMultiplier;

        // Store base speeds for gem multiplier calculation
        basePlayerSpeed = playerSpeed;
        baseCrouchSpeed = crouchSpeed;
        baseSprintSpeed = sprintSpeed;
        baseDashSpeed = dashSpeed;

        // Initialize dash state
        IsDashing = false;

        // Store original layer for dash invincibility
        originalLayer = gameObject.layer;

        // Reset dash cooldown when game starts (important for Editor play/stop/play)
        dashing.ResetDashCooldown();

        // Add stuck detection if not present
        if (GetComponent<StuckDetection>() == null)
        {
            gameObject.AddComponent<StuckDetection>();
        }

        // Subscribe to weapon change events to update speed multipliers
        var weaponController = GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged += OnWeaponChanged;
        }

        // Subscribe to equipment changes
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
        }

        // Apply initial speed multipliers
        UpdateSpeedWithGems();

        lastLocomotionMoveTime = Runner.SimulationTime;
    }

    private void OnEquipmentChanged()
    {
        // Update speeds when equipment changes
        UpdateSpeedWithGems();
    }

    public override void FixedUpdateNetwork()
    {
        if (movementSM == null || movementSM.currentState == null) return;
        
        if (GetInput<NetworkInputData>(out var input))
        {
            previousInput = currentInput;
            currentInput = input;
        }
        else
        {
            previousInput = currentInput;
            currentInput = default;
        }
        
        UpdateGroundedAndJumpBuffer();
        movementSM.currentState.HandleInput();
        movementSM.currentState.LogicUpdate();

        CalculatedVelocity = Vector3.zero;
        movementSM.currentState.PhysicsUpdate(); // Các State CHỈ tính vận tốc X, Z
        
        // === BẮT ĐẦU CÁI PHANH ===
        // Nếu đang xài skill -> Ép vận tốc đi ngang (X, Z) về số 0 tròn trĩnh!
        if (skillLock != null && skillLock.isPerformingSkill)
        {
            CalculatedVelocity.x = 0f;
            CalculatedVelocity.z = 0f;
        }
        // === KẾT THÚC CÁI PHANH ===

        if (controller != null && controller.enabled) 
        {
            // 1. Giữ nhân vật bấm sn nếu đang đứng trên đất
            if (IsGroundedStable() && playerVelocity.y < 0)
            {
                playerVelocity.y = -8f; 
            }

            // 2. Tích luỹ trọng lực (Nếu không phải đang Dash)
            if (!IsDashing)
            {
                playerVelocity.y += gravityValue * Runner.DeltaTime;
            }

            // 3. Gộp vận tốc dọc (Trọng lực/Nhảy) vào vận tốc ngang (FSM)
            CalculatedVelocity.y = playerVelocity.y;

            // 4. Lệnh di chuyển vật lý cuối cùng
            controller.Move(CalculatedVelocity * Runner.DeltaTime);
        }
    }
    
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedState))
            {
                // Proxy animation handling can go here
            }
        }
    }
    
    private void Update()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;
        
        // Giữ nguyên code xử lý UI, cooldown timers, hoặc Animation cũ của bạn ở đây...
    }

    void UpdateGroundedAndJumpBuffer()
    {
        CachedGroundedFeet = ComputeGroundedFeetRays();
        if (CachedGroundedFeet)
            lastGroundedFeetTime = Runner.SimulationTime;
        if (CachedGroundedFeet || (controller != null && controller.isGrounded))
            lastGroundedStableTime = Runner.SimulationTime;

        if ((currentInput.buttons.IsSet(NetworkInputButtons.Jump) && !previousInput.buttons.IsSet(NetworkInputButtons.Jump))
            && (!TutorialInputGate.IsActive || TutorialInputGate.Allows(TutorialInputMask.Jump)))
            jumpBufferRemaining = jumpBufferDuration;
        else
            jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - Runner.DeltaTime);

        // Không giữ buffer trong phase bay lên (tránh spam Space tích buffer rồi chạm đất là nhảy lại ngay).
        if (controller != null && controller.velocity.y > 0.25f)
            jumpBufferRemaining = 0f;
    }

    bool ComputeGroundedFeetRays()
    {
        if (controller == null) return false;
        LayerMask mask = groundLayers.value == 0 ? Physics.DefaultRaycastLayers : groundLayers;

        // CapsuleCast ổn định hơn Raycast trên MeshCollider gồ ghề.
        Vector3 up = Vector3.up;
        Vector3 worldCenter = transform.TransformPoint(controller.center);
        float radius = Mathf.Max(0.01f, controller.radius * 0.85f);
        float halfHeight = Mathf.Max(controller.height * 0.5f, controller.radius);
        float cylinderHalf = Mathf.Max(0f, halfHeight - controller.radius);

        // Hai đầu capsule ở trạng thái hiện tại (world space)
        Vector3 p1 = worldCenter + up * cylinderHalf;
        Vector3 p2 = worldCenter - up * cylinderHalf;

        // Đẩy điểm dưới lên một chút để tránh cast bắt đầu trong mặt đất
        float eps = 0.02f;
        Vector3 castP1 = p1;
        Vector3 castP2 = p2 + up * (controller.skinWidth + eps);
        float dist = groundRayDistance + controller.skinWidth + eps;

        if (!Physics.CapsuleCast(castP1, castP2, radius, Vector3.down, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            return false;

        // Loại tường/độ dốc quá lớn để tránh dính side mesh
        float maxSlope = controller.slopeLimit + 5f;
        float angle = Vector3.Angle(hit.normal, up);
        return angle <= maxSlope;
    }

    /// <summary>Mask giống ray chân — dùng cho FallingState đo khoảng cách xuống đất.</summary>
    public LayerMask GetGroundRaycastMask()
    {
        return groundLayers.value == 0 ? Physics.DefaultRaycastLayers : groundLayers;
    }

    /// <summary>Đứng trên đất (2 ray) hoặc coyote ngắn sau khi rời mép; không dùng CC.isGrounded để tránh nhấp nháy.</summary>
    public bool IsGroundedForJump()
    {
        if (CachedGroundedFeet) return true;
        if (coyoteTime <= 0f) return false;
        if (Runner.SimulationTime - lastGroundedFeetTime > coyoteTime) return false;
        return controller != null && controller.velocity.y <= coyoteMaxUpSpeed;
    }

    /// <summary>Grounded ổn định: ưu tiên ray/cast chân + grace window chống nhấp nháy 1-2 frame.</summary>
    public bool IsGroundedStable(float graceSeconds = 0.08f)
    {
        if (CachedGroundedFeet) return true;
        if (controller != null && controller.isGrounded) return true;
        if (graceSeconds <= 0f) return false;
        return Runner != null && (Runner.SimulationTime - lastGroundedStableTime) <= graceSeconds;
    }

    /// <summary>Gọi khi impulse nhảy đã áp (JumpingState / SprintJump): xóa buffer và bật cooldown.</summary>
    public void NotifyJumpImpulseStarted()
    {
        jumpBufferRemaining = 0f;
        jumpAllowedAfterTime = Runner.SimulationTime + Mathf.Max(0f, jumpCooldownSeconds);
    }

    /// <summary>Consume một lần nhảy đã buffer khi được phép và đang grounded/coyote.</summary>
    public bool TryConsumeJumpBuffered(bool canJump)
    {
        if (!canJump || jumpBufferRemaining <= 0f) return false;
        if (Runner.SimulationTime < jumpAllowedAfterTime) return false;
        if (!IsGroundedForJump()) return false;
        jumpBufferRemaining = 0f;
        return true;
    }

    /// <summary>
    /// Blend tree locomotion (param "speed"): có move thì damp; khi input nhỏ thì vẫn damp tới khi đủ lâu không có move mới snap 0
    /// (cách 2 — tránh jitter sau dash nhưng blend lúc dừng vẫn mượt).
    /// </summary>
    public void SetAnimatorLocomotionSpeed(float targetMagnitude)
    {
        if (animator == null) return;
        float threshold = Mathf.Max(1e-4f, locomotionIdleMoveThreshold);

        if (targetMagnitude >= threshold)
            lastLocomotionMoveTime = Runner.SimulationTime;

        if (targetMagnitude < threshold)
        {
            if (Runner.SimulationTime - lastLocomotionMoveTime >= locomotionIdleSnapAfterSeconds)
                animator.SetFloat("speed", 0f);
            else
                animator.SetFloat("speed", targetMagnitude, speedDampTime, Runner.DeltaTime);
        }
        else
        {
            animator.SetFloat("speed", targetMagnitude, speedDampTime, Runner.DeltaTime);
        }
    }

    

    /// <summary>
    /// Update speed values based on equipped gems and equipment: speed = baseSpeed + (baseSpeed × gem%) + (baseSpeed × equipment%)
    /// </summary>
    private void UpdateSpeedWithGems()
    {
        float gemSpeedPercent = 0f;
        float equipmentSpeedPercent = 0f;

        // Get gem speed multiplier
        var weaponController = GetComponent<WeaponController>();
        if (WeaponGemManager.Instance != null && weaponController != null)
        {
            WeaponSO currentWeapon = weaponController.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                float speedMultiplier = WeaponGemManager.Instance.GetMovementSpeedMultiplier(currentWeapon.weaponType);
                gemSpeedPercent = speedMultiplier - 1f; // Extract the % part
            }
        }

        // Get equipment speed bonus
        if (EquipmentManager.Instance != null)
        {
            equipmentSpeedPercent = EquipmentManager.Instance.GetTotalMovementSpeedBonus();
        }

        // Calculate: baseSpeed + (baseSpeed × gem%) + (baseSpeed × equipment%)
        float totalSpeedPercent = gemSpeedPercent + equipmentSpeedPercent;

        playerSpeed = basePlayerSpeed + (basePlayerSpeed * totalSpeedPercent);
        crouchSpeed = baseCrouchSpeed + (baseCrouchSpeed * totalSpeedPercent);
        sprintSpeed = baseSprintSpeed + (baseSprintSpeed * totalSpeedPercent);
        dashSpeed = baseDashSpeed + (baseDashSpeed * totalSpeedPercent);
    }

    private void OnWeaponChanged(WeaponSO weapon)
    {
        // Update speeds when weapon changes
        UpdateSpeedWithGems();
    }

    private void OnDestroy()
    {
        // Unsubscribe from weapon change events
        var weaponController = GetComponent<WeaponController>();
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChanged;
        }

        // Unsubscribe from equipment changes
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }

    #region Animation Events - Dash

    /// <summary>
    /// Animation Event: Enable dash invincibility frame
    /// Sets player layer to "Nothing" to prevent raycast detection and damage
    /// Call this from dash animation at the exact frame where invincibility should start
    /// </summary>
    public void AE_EnableDashInvincibility()
    {
        IsDashing = true;

        // Lưu layer mặc định của Player (chỉ lưu 1 lần nếu chưa lưu)
        if (originalLayer == 0 && gameObject.layer != LayerMask.NameToLayer("Player_Dashing"))
        {
            originalLayer = gameObject.layer;
        }

        // Lấy layer Player_Dashing
        int dashLayer = LayerMask.NameToLayer("Player_Dashing");
        if (dashLayer == -1)
        {
            Debug.LogWarning("[Character] CẢNH BÁO: Bạn chưa tạo Layer 'Player_Dashing' trong Unity! Hãy vào Tags and Layers để tạo.");
            dashLayer = originalLayer; // Fallback an toàn
        }

        // NGAY LẬP TỨC: Đổi toàn bộ model sang layer Dashing để xài Physics Matrix xuyên thấu
        SetLayerRecursively(gameObject, dashLayer);
        
        // Vẫn giữ lại excludeLayers như một lớp bảo mật bổ sung (phòng hờ matrix config sót)
        if (controller != null)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int hurtboxLayer = LayerMask.NameToLayer("EnemyHurtbox");
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            controller.excludeLayers |= (1 << enemyLayer) | (1 << hurtboxLayer) | (1 << ignoreRaycastLayer);
        }
    }

    /// <summary>
    /// Animation Event: Disable dash invincibility frame
    /// Restores player layer to original layer
    /// Call this from dash animation at the exact frame where invincibility should end
    /// </summary>
    public void AE_DisableDashInvincibility()
    {
        if (!IsDashing) return; // Prevent double-triggering
        
        IsDashing = false;

        // Restore original layer for player and all children
        SetLayerRecursively(gameObject, originalLayer);
        
        // Khôi phục va chạm
        if (controller != null)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int hurtboxLayer = LayerMask.NameToLayer("EnemyHurtbox");
            int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
            controller.excludeLayers &= ~((1 << enemyLayer) | (1 << hurtboxLayer) | (1 << ignoreRaycastLayer));
        }
    }

    /// <summary>
    /// Recursively set layer for GameObject and all its children
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    #endregion
}














