using UnityEngine;
using UnityEngine.InputSystem;
[DefaultExecutionOrder(-140)]
public class Character : MonoBehaviour
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

    [Header("Animation Smoothing")]
    [Range(0, 1)]
    public float speedDampTime = 0.1f;
    [Range(0, 1)]
    public float velocityDampTime = 0.9f;
    [Range(0, 1)]
    public float rotationDampTime = 0.2f;
    [Range(0, 1)]
    public float airControl = 0.5f;

    public StateMachine movementSM;
    public BaseMoveState standing;
    public JumpingState jumping;
    public CrouchingState crouching;
    public SprintState sprinting;
    public SprintJumpState sprintjumping;
    public DashState dashing;
    public HardStopState hardStop;

    public DrawWeaponState drawWeapon;
    public SheathWeaponState sheathWeapon;
    public CombatMoveState combatMove;
    public AttackState attacking;
    public GetHitState getHit;
    public DieState dieState;

    [HideInInspector]
    public float gravityValue = -9.81f;
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

    public State currentLocomotionState;
    public State lastStateBeforeHit; // Track state before getting hit
    public float lastAttackInputTime; // Track when attack was last pressed

    //public bool isInCombatState { get; set; }
    public bool isWeaponDrawn { get; set; }
    public bool IsDashing { get; set; } // For invincibility frame during dash
    public float dashLockUntil = 0f; // Thời điểm trước đó dash bị khóa (để tránh auto-dash sau khi bị hit)

    /// <summary> Khi false, bỏ qua input nhảy cho đến khi vào lại locomotion. Nhảy thật sự còn cần <see cref="TryConsumeJumpBuffered"/> (ray chân + buffer + coyote nhẹ). </summary>
    public bool canStartJump = true;

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
    float jumpAllowedAfterTime = -999f;

    private int originalLayer; // Store original layer before dash
    private const int NOTHING_LAYER = 0; // Unity's "Nothing" layer index

    // Start is called before the first frame update
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        // ─── MULTIPLAYER: chờ NetworkPlayerLocalOwnership xử lý trước ───
        // NetworkPlayerLocalOwnership.Spawned() chạy khi NetworkObject spawns (trước frame đầu tiên).
        // → Nó set Character.enabled = true cho local player, = false cho remote player.
        // Kiểm tra enabled thay vì HasInputAuthority (tránh race condition khi Fusion chưa ready).
        var ownershipComponent = GetComponent<NetworkPlayerLocalOwnership>();
        if (ownershipComponent != null)
        {
            // NetworkPlayerLocalOwnership đã enable Character nếu là local → check ngay.
            if (!enabled)
            {
                Debug.Log($"[Character] Remote player '{gameObject.name}' — input disabled by ownership.");
                return;
            }
            // Local player — tiếp tục khởi tạo
        }
        // else: không có NetworkObject → chế độ offline, khởi tạo bình thường.

        InitCharacter();
        
        // Cú chốt: Ép map vĩnh viễn về Player để tránh UI cướp mất input khi load xong
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
            var pm = playerInput.actions.FindActionMap("Player");
            if (pm != null)
            {
                pm.Enable();
                Debug.Log($"[Character] ActionMap 'Player' FORCED to enable. IsEnabled={pm.enabled}");
            }
        }
    }

    /// <summary>
    /// Khởi tạo đầy đủ Character (gọi sau khi xác nhận là local player).
    /// Tách riêng để có thể gọi sau coroutine.
    /// </summary>
    private void InitCharacter()
    {
        // Load saved key binding overrides từ Settings
        InputRebindHelper.LoadBindingOverrides(playerInput);
        jumpActionCache = playerInput.actions["Jump"];
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            if (playerInput != null) playerInput.camera = Camera.main; // Cấp quyền Camera cho InputSystem
        }
        else
        {
            Debug.LogWarning($"[Character] Camera.main is NULL during InitCharacter! Attempting fallback search.");
            var fallbackCam = FindFirstObjectByType<Camera>();
            if (fallbackCam != null)
            {
                cameraTransform = fallbackCam.transform;
                if (playerInput != null) playerInput.camera = fallbackCam;
            }
            else
                cameraTransform = this.transform; // Tránh Crash NullReferenceException
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
        crouching = new CrouchingState(this, movementSM);
        sprinting = new SprintState(this, movementSM);
        sprintjumping = new SprintJumpState(this, movementSM);
        dashing = new DashState(this, movementSM);
        hardStop = new HardStopState(this, movementSM);
        drawWeapon = new DrawWeaponState(this, movementSM);
        sheathWeapon = new SheathWeaponState(this, movementSM);
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
        DashState.ResetDashCooldown();

        // Add stuck detection if not present
        if (GetComponent<StuckDetection>() == null)
        {
            gameObject.AddComponent<StuckDetection>();
            Debug.Log("[Character] Added StuckDetection component");
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

        lastLocomotionMoveTime = Time.time;

        Debug.Log($"[Character] Local player '{gameObject.name}' initialized — input ENABLED.");
    }

    private void OnEquipmentChanged()
    {
        // Update speeds when equipment changes
        UpdateSpeedWithGems();
    }

    private void Update()
    {
        // Safety net: nếu bị disabled giữa chừng (do NetworkPlayerLocalOwnership), bỏ qua
        if (movementSM == null || movementSM.currentState == null) return;

        // Auto-Recovery: Unity's EventSystem map bug sometimes abruptly drops map enablement when Canvases die.
        if (playerInput != null && playerInput.currentActionMap != null)
        {
            if (playerInput.currentActionMap.name == "Player" && !playerInput.currentActionMap.enabled && !CursorUIPriority.IsUiOverlayActive)
            {
                Debug.LogWarning($"[Character] ActionMap '{playerInput.currentActionMap.name}' was found DISABLED during gameplay! Aggressively reviving it.");
                playerInput.currentActionMap.Enable();
            }
        }

        if (Time.frameCount % 60 == 0 && playerInput != null && Object.FindFirstObjectByType<Fusion.NetworkRunner>() != null)
        {
            var moveAct = playerInput.actions["Move"];
            Debug.Log($"[InputDebug] {gameObject.name} | Map: {playerInput.currentActionMap?.name} | UI_Active: {CursorUIPriority.IsUiOverlayActive} | Move enabled: {moveAct?.enabled} | Value: {moveAct?.ReadValue<Vector2>()}");
        }

        // Nếu lúc Start() chưa tìm thấy Camera HOẶC Camera đang reference bị đem đi vứt/tắt (Offline Player bị disable)
        if (cameraTransform == null || cameraTransform == this.transform || !cameraTransform.gameObject.activeInHierarchy)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                if (playerInput != null) playerInput.camera = Camera.main;
                Debug.Log($"[Character] Camera.main dynamic RECOVERY in Update for '{gameObject.name}'");
            }
        }

        UpdateGroundedAndJumpBuffer();
        movementSM.currentState.HandleInput();

        movementSM.currentState.LogicUpdate();
    }

    void UpdateGroundedAndJumpBuffer()
    {
        CachedGroundedFeet = ComputeGroundedFeetRays();
        if (CachedGroundedFeet)
            lastGroundedFeetTime = Time.time;

        if (jumpActionCache != null && jumpActionCache.triggered)
            jumpBufferRemaining = jumpBufferDuration;
        else
            jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - Time.deltaTime);

        // Không giữ buffer trong phase bay lên (tránh spam Space tích buffer rồi chạm đất là nhảy lại ngay).
        if (controller != null && controller.velocity.y > 0.25f)
            jumpBufferRemaining = 0f;
    }

    private void HandleCameraAndMovementCalculations()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                if (playerInput != null) playerInput.camera = Camera.main;
            }
            if (cameraTransform == null) return;
        }
    }

    bool ComputeGroundedFeetRays()
    {
        if (controller == null) return false;
        LayerMask mask = groundLayers.value == 0 ? Physics.DefaultRaycastLayers : groundLayers;
        float bottomY = transform.position.y + controller.center.y - controller.height * 0.5f + controller.skinWidth;
        Vector3 basePos = new Vector3(transform.position.x, bottomY, transform.position.z);
        Vector3 left = basePos + transform.TransformDirection(new Vector3(-footRayHalfWidth, 0f, 0f));
        Vector3 right = basePos + transform.TransformDirection(new Vector3(footRayHalfWidth, 0f, 0f));
        float dist = groundRayDistance + controller.skinWidth;
        bool hitL = Physics.Raycast(left, Vector3.down, out _, dist, mask, QueryTriggerInteraction.Ignore);
        bool hitR = Physics.Raycast(right, Vector3.down, out _, dist, mask, QueryTriggerInteraction.Ignore);
        return hitL && hitR;
    }

    /// <summary>Đứng trên đất (2 ray) hoặc coyote ngắn sau khi rời mép; không dùng CC.isGrounded để tránh nhấp nháy.</summary>
    public bool IsGroundedForJump()
    {
        if (CachedGroundedFeet) return true;
        if (coyoteTime <= 0f) return false;
        if (Time.time - lastGroundedFeetTime > coyoteTime) return false;
        return controller != null && controller.velocity.y <= coyoteMaxUpSpeed;
    }

    /// <summary>Gọi khi impulse nhảy đã áp (JumpingState / SprintJump): xóa buffer và bật cooldown.</summary>
    public void NotifyJumpImpulseStarted()
    {
        jumpBufferRemaining = 0f;
        jumpAllowedAfterTime = Time.time + Mathf.Max(0f, jumpCooldownSeconds);
    }

    /// <summary>Consume một lần nhảy đã buffer khi được phép và đang grounded/coyote.</summary>
    public bool TryConsumeJumpBuffered(bool canJump)
    {
        if (!canJump || jumpBufferRemaining <= 0f) return false;
        if (Time.time < jumpAllowedAfterTime) return false;
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
            lastLocomotionMoveTime = Time.time;

        if (targetMagnitude < threshold)
        {
            if (Time.time - lastLocomotionMoveTime >= locomotionIdleSnapAfterSeconds)
                animator.SetFloat("speed", 0f);
            else
                animator.SetFloat("speed", targetMagnitude, speedDampTime, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("speed", targetMagnitude, speedDampTime, Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (!enabled) return;
        if (movementSM == null || movementSM.currentState == null) return;
        movementSM.currentState.PhysicsUpdate();
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
    /// Animation Event: Start dash movement
    /// Call this from dash animation at the frame where dash movement should begin
    /// </summary>
    public void AE_StartDashMovement()
    {
        if (dashing != null)
        {
            dashing.AE_StartDashMovement();
        }
    }

    /// <summary>
    /// Animation Event: Stop dash movement
    /// Call this from dash animation at the frame where dash movement should end
    /// </summary>
    public void AE_StopDashMovement()
    {
        if (dashing != null)
        {
            dashing.AE_StopDashMovement();
        }
    }

    /// <summary>
    /// Animation Event: Enable dash invincibility frame
    /// Sets player layer to "Nothing" to prevent raycast detection and damage
    /// Call this from dash animation at the exact frame where invincibility should start
    /// </summary>
    public void AE_EnableDashInvincibility()
    {
        IsDashing = true;

        // Store original layer before changing (only if not already Nothing layer)
        if (gameObject.layer != NOTHING_LAYER)
        {
            originalLayer = gameObject.layer;
        }

        // Set player and all children to "Nothing" layer to prevent damage detection
        SetLayerRecursively(gameObject, NOTHING_LAYER);

        Debug.Log($"[Character] AE_EnableDashInvincibility - Dash iframe enabled (layer set to Nothing, original: {originalLayer})");
    }

    /// <summary>
    /// Animation Event: Disable dash invincibility frame
    /// Restores player layer to original layer
    /// Call this from dash animation at the exact frame where invincibility should end
    /// </summary>
    public void AE_DisableDashInvincibility()
    {
        IsDashing = false;

        // Restore original layer for player and all children
        SetLayerRecursively(gameObject, originalLayer);

        Debug.Log($"[Character] AE_DisableDashInvincibility - Dash iframe disabled (layer restored to {originalLayer})");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Animation Event Receivers — fallback
    // Fixes Unity's "Has no receiver" issue by receiving events here and 
    // strictly delegating them to the PlayerAudioEmitter on the same object.
    // ═══════════════════════════════════════════════════════════════════

    private PlayerAudioEmitter _cachedEmitter;
    
    private PlayerAudioEmitter Emitter
    {
        get
        {
            if (_cachedEmitter == null) _cachedEmitter = GetComponent<PlayerAudioEmitter>();
            return _cachedEmitter;
        }
    }

    public void AE_PlayFootstepSound() { if (Emitter != null) Emitter.AE_PlayFootstepSound(); else SoundManager.PlayFootstep(null, 1f); }
    public void AE_PlayFootstepSoundFromWeaponLayer() { if (Emitter != null) Emitter.AE_PlayFootstepSoundFromWeaponLayer(); else if (isWeaponDrawn) SoundManager.PlayFootstep(null, 1f); }
    public void AE_PlayFootstepVFXLeft() { if (Emitter != null) Emitter.AE_PlayFootstepVFXLeft(); }
    public void AE_PlayFootstepVFXRight() { if (Emitter != null) Emitter.AE_PlayFootstepVFXRight(); }
    public void AE_PlayJumpSound() { if (Emitter != null) Emitter.AE_PlayJumpSound(); else SoundManager.PlayJump(null, 1f); }
    public void AE_PlayLandSound() { if (Emitter != null) Emitter.AE_PlayLandSound(); else SoundManager.PlayLand(null, 1f); }
    public void AE_PlayDashSound() { if (Emitter != null) Emitter.AE_PlayDashSound(); else SoundManager.PlayDash(null, 1f); }
    public void AE_PlayCrouchMoveSound() { if (Emitter != null) Emitter.AE_PlayCrouchMoveSound(); else SoundManager.PlayCrouchMove(null, 1f); }
    public void AE_PlayGetHitSound() { if (Emitter != null) Emitter.AE_PlayGetHitSound(); else SoundManager.PlayGetHit(null, 1f); }
    public void AE_PlayDieSound() { if (Emitter != null) Emitter.AE_PlayDieSound(); else SoundManager.PlayDie(null, 1f); }
    public void AE_PlayDrawWeaponSound() { if (Emitter != null) Emitter.AE_PlayDrawWeaponSound(); else { var w = GetComponent<WeaponController>()?.GetCurrentWeapon()?.weaponType ?? WeaponType.Sword; SoundManager.PlayDrawWeapon(w, null, 1f); } }
    public void AE_PlayDrawWeaponSoundSecond() { if (Emitter != null) Emitter.AE_PlayDrawWeaponSoundSecond(); else { var w = GetComponent<WeaponController>()?.GetCurrentWeapon()?.weaponType ?? WeaponType.Sword; SoundManager.PlayDrawWeapon(w, 1, null, 1f); } }
    public void AE_PlaySheathWeaponSound() { if (Emitter != null) Emitter.AE_PlaySheathWeaponSound(); else { var w = GetComponent<WeaponController>()?.GetCurrentWeapon()?.weaponType ?? WeaponType.Sword; SoundManager.PlaySheathWeapon(w, null, 1f); } }
    public void AE_PlaySheathWeaponSoundSecond() { if (Emitter != null) Emitter.AE_PlaySheathWeaponSoundSecond(); else { var w = GetComponent<WeaponController>()?.GetCurrentWeapon()?.weaponType ?? WeaponType.Sword; SoundManager.PlaySheathWeapon(w, 1, null, 1f); } }
    public void AE_PlayBasicAttackSound(int comboIndex) { if (Emitter != null) Emitter.AE_PlayBasicAttackSound(comboIndex); else { var w = GetComponent<WeaponController>()?.GetCurrentWeapon()?.weaponType ?? WeaponType.Sword; SoundManager.PlayBasicAttack(w, comboIndex, null, 1f); } }
    public void AE_PlaySkillSound(int abilityInputIndex) { if (Emitter != null) Emitter.AE_PlaySkillSound(abilityInputIndex); else { var w = GetComponent<WeaponController>()?.GetCurrentWeapon()?.weaponType ?? WeaponType.Sword; SoundManager.PlaySkill(w, (AbilityInput)abilityInputIndex, null, 1f); } }
    public void AE_PlayMageProjectileHitSound() { if (Emitter != null) Emitter.AE_PlayMageProjectileHitSound(); else SoundManager.PlayMageProjectileHit(null, 1f); }

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