using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
public enum CharacterStateSync { Standing, Jumping, Crouching, Sprinting, Dash, HardStop, DrawWeapon, SheathWeapon, CombatMove, Attack, GetHit, Die }
public class Character : NetworkBehaviour, IBeforeAllTicks
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
    [Tooltip("Body turn cap (degrees/sec) in simulation; used with RotateTowards for rollback-stable facing.")]
    [SerializeField] float locomotionBodyTurnDegreesPerSecond = 480f;
    public float LocomotionBodyTurnDegreesPerSecond => locomotionBodyTurnDegreesPerSecond;
    public float crouchColliderHeight = 1.35f;

    [Header("Fusion / CharacterController rollback safety")]
    [Tooltip("Helps prevent CharacterController internal cache from drifting during Fusion rollback/resimulation. Runs at IBeforeAllTicks (like Fusion's NetworkCharacterController).")]
    [SerializeField] bool resyncCharacterControllerCache = false;

    [Tooltip("When enabled, only resync during resimulation ticks (lower overhead, minimal impact). Disable to resync every tick.")]
    [SerializeField] bool resyncCcOnlyOnResimulation = true;

    void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
    {
        if (!resyncCharacterControllerCache)
            return;
        if (resyncCcOnlyOnResimulation && !resimulation)
            return;
        if (!HasInputAuthority && !HasStateAuthority)
            return;
        if (controller == null || !controller.enabled)
            return;

        // Disable CC before engine state is used for simulation (mirrors Fusion's NetworkCharacterController.CopyToEngine pattern).
        controller.enabled = false;
        transform.SetPositionAndRotation(transform.position, transform.rotation);
        controller.enabled = true;
    }

    [Header("Debug — FSM / Animator (Crit test)")]
    [Tooltip("Bật log [CritFSM] (landing, draw/sheath, trigger). Mặc định tắt; bật trên prefab khi cần debug.")]
    [SerializeField] bool debugCritFsmLogs = false;

    /// <summary>Log một dòng có tag cố định để lọc Console. Chỉ chạy khi <see cref="debugCritFsmLogs"/> bật.</summary>
    public void LogCritFsm(string tag, string message)
    {
        if (!debugCritFsmLogs) return;
        string cur = movementSM != null && movementSM.currentState != null
            ? movementSM.currentState.GetType().Name
            : "?";
        float t = Runner != null ? (float)Runner.SimulationTime : Time.time;
        Debug.Log(
            $"[CritFSM][{tag}][{name}] t={t:F3} SA={HasStateAuthority} IA={HasInputAuthority} NetDrawn={NetIsWeaponDrawn} st={cur} | {message}",
            this);
    }

    /// <summary>Forensic: logs every FSM transition (including same-tick resim replays) with per-tick counter.</summary>
    public void LogCritStateTransition(State fromState, State toState)
    {
        if (!debugCritFsmLogs || Runner == null || !Runner.IsRunning) return;
        int tick = (int)Runner.Tick;
        if (tick != _lastStateTransitionTick)
        {
            _lastStateTransitionTick = tick;
            _stateTransitionsThisTick = 0;
        }
        _stateTransitionsThisTick++;
        string fromName = fromState != null ? fromState.GetType().Name : "null";
        string toName = toState != null ? toState.GetType().Name : "null";
        LogCritFsm("State", $"tick={tick} n={_stateTransitionsThisTick} {fromName} -> {toName} | {CritFsmAnimatorLayerDump()} | {CritFsmAnimatorBoolDump()}");
    }

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
    public DrawWeaponState drawingWeapon;
    public SheathWeaponState sheathingWeapon;

    [HideInInspector]
    public float gravityValue = -9.81f;

    [Header("Landing & Fall Settings")]
    [Tooltip("Khoảng cách rơi tối thiểu (mét) để bắt buộc chạy animation Land")]
    public float minFallDistanceForLanding = 1.2f;

    [Tooltip("Với nhảy (requireLanding): chỉ knee land sau khi đã rơi khỏi đỉnh nhảy ít nhất (mét) SO VỚI jumpStartY, tránh resim/chạm đất sớm).")]
    [SerializeField] float jumpKneeLandMinDropFromApex = 0.12f;

    [Tooltip("Với nhảy: thêm điều kiện thời gian trên không tối thiểu (s) trước khi cho knee land (chỉ khi chưa đủ drop từ đỉnh).")]
    [SerializeField] float jumpKneeLandMinAirTime = 0.10f;

    public float JumpKneeLandMinDropFromApex => jumpKneeLandMinDropFromApex;
    public float JumpKneeLandMinAirTime => jumpKneeLandMinAirTime;

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
    [HideInInspector] public float jumpStartY; // Lưu lại tọa độ Y lúc bắt đầu nhảy

    /// <summary>Parkour roll landing (replicated so every peer agrees for Dash/Roll branch + Animator).</summary>
    [Networked] public NetworkBool NetIsRollLanding { get; set; }
    /// <summary>Convenience read for FSM / combat (mirrors <see cref="NetIsRollLanding"/>).</summary>
    public bool isRollLanding => NetIsRollLanding;

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
    public NetworkMecanimAnimator networkAnimator;

    [Header("Animation / Fusion")]
    [Tooltip("Ignored: NetworkMecanimAnimator.SetTrigger always uses passThrough=true (Fusion ring buffer + client responsiveness).")]
    [SerializeField] bool animatorTriggerPassThroughOnInputAuthority = true;
    /// <summary>Simulation velocity (incl. vertical); networked so Fusion rollback/resim does not stack gravity/move.</summary>
    [Networked] public Vector3 PlayerVelocity { get; set; }

    [Networked] public float NetLocomotionSpeed { get; set; }
    [Networked] public float NetAnimSpeedTarget { get; set; }
    [Networked] public float NetFallTimer { get; set; }
    [Networked] public float NetJumpBufferRemaining { get; set; }
    [Networked] public float NetToggleBuffer { get; set; }
    [Networked] public float NetLastToggleSimTime { get; set; }
    [HideInInspector]
    public Vector3 cachedPlanarForward;
    [HideInInspector]
    public Vector3 cachedPlanarRight;
    [HideInInspector]
    public SkillLock skillLock;

    public State currentLocomotionState;
    /// <summary>Input-owned player on this machine (Fusion). Cleared on <see cref="Despawned"/>.</summary>
    public static Character LocalCharacter;
    /// <summary>Alias for readability in UI code.</summary>
    public static Character Local => LocalCharacter;
    [Networked] public CharacterStateSync NetworkedState { get; set; }
    [Networked] public bool NetworkedIsLanding { get; set; }

    /// <summary>Rollback-safe landing lockout; do not use Animator layer state in FUN to decide exit.</summary>
    [Networked] public TickTimer NetLandingRecoveryTimer { get; set; }

    /// <summary>Monotonic counter for landing moments (Pattern 3: drive VFX/SFX from <see cref="Render"/> change detection).</summary>
    [Networked] public int LandEventCount { get; set; }

    /// <summary>Animator bool name synced from <see cref="NetworkedIsLanding"/> (add to controller; optional).</summary>
    public const string AnimatorKneeLandingParam = "kneeLanding";
    [Networked] public NetworkString<_32> DisplayName { get; set; }

    /// <summary>Packed <c>(a&lt;&lt;24)|(r&lt;&lt;16)|(g&lt;&lt;8)|b</c>; 0 = nameplate derives hue from name string.</summary>
    [Networked] public int DisplayNameColorArgb { get; set; }

    /// <summary>Fired from <see cref="Render"/> when <see cref="DisplayName"/> or <see cref="DisplayNameColorArgb"/> changes (after state sync).</summary>
    public event Action DisplayInfoChanged;

    /// <summary>Fired from <see cref="Render"/> when <see cref="LandEventCount"/> increments (safe for VFX/SFX; not resimulated).</summary>
    public event Action LandingEventForVfx;

    private ChangeDetector _changeDetector;
    WeaponController _weaponControllerCache;
    bool _weaponControllerCacheResolved;

    /// <summary>
    /// <see cref="WeaponController"/> is on the child rig (e.g. <c>player</c>) on Player_3.0 — not on the same GameObject as <see cref="Character"/>.
    /// </summary>
    WeaponController GetWeaponControllerInHierarchy()
    {
        if (_weaponControllerCacheResolved)
            return _weaponControllerCache;
        _weaponControllerCacheResolved = true;
        _weaponControllerCache = GetComponentInChildren<WeaponController>(true);
        if (_weaponControllerCache == null && Application.isPlaying)
            Debug.LogWarning("[Character] No WeaponController found under this Character hierarchy.", this);
        return _weaponControllerCache;
    }

    public NetworkInputData previousInput;
    public NetworkInputData currentInput;
    public Vector3 CalculatedVelocity;
    const int TriggerDedupeSlots = 24;
    readonly int[] _triggerDedupeHashes = new int[TriggerDedupeSlots];
    readonly int[] _triggerDedupeTicks = new int[TriggerDedupeSlots];
    int _triggerDedupeCursor;
    int _lastNoInputWarnTick = int.MinValue;
    int _lastInputEdgeTick = int.MinValue;
    int _lastInputEdgeMask;
    int _inputEdgeReplayCount;
    int _lastStateTransitionTick = int.MinValue;
    int _stateTransitionsThisTick;

    static bool s_warnedMissingNetworkMecanimAnimator;
    
    public State lastStateBeforeHit; // Track state before getting hit
    public float lastAttackInputTime; // Track when attack was last pressed

    /// <summary>Weapon considered drawn for gameplay + UI (replicated).</summary>
    [Networked] public NetworkBool NetIsWeaponDrawn { get; set; }
    /// <summary>Single source of truth for equipped weapon type across peers.</summary>
    [Networked] public int NetEquippedWeaponType { get; set; }
    [Header("Debug — networked weapon")]
    [Tooltip("Log [NetWeapon] when NetEquippedWeaponType syncs (RPC host + Render peers + delayed read after swap). Tắt khi không test.")]
    [SerializeField] bool debugNetEquippedWeaponLog = false;
    /// <summary>Convenience read (mirrors <see cref="NetIsWeaponDrawn"/>).</summary>
    public bool isWeaponDrawn => NetIsWeaponDrawn;
    public enum QueuedWeaponAction { None, Draw, Sheath }

    // Queue requested weapon action when animation/state would skip it (e.g. GetHit/skill lock).
    public QueuedWeaponAction queuedWeaponAction { get; private set; } = QueuedWeaponAction.None;

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

    [Header("Landing recovery (TickTimer — simulation, not Animator)")]
    [Tooltip("Thời khóa recovery knee khi rơi xa (> minFallDistanceForLanding). Tune theo độ dài clip.")]
    [SerializeField] float netLandingRecoveryFallKneeSeconds = 0.95f;
    [Tooltip("Thời khóa recovery knee khi chỉ từ nhảy (requireLanding + apex / min air). Ngắn hơn fall.")]
    [SerializeField] float netLandingRecoveryJumpKneeSeconds = 0.42f;

    public float NetLandingRecoveryFallKneeSeconds => netLandingRecoveryFallKneeSeconds;
    public float NetLandingRecoveryJumpKneeSeconds => netLandingRecoveryJumpKneeSeconds;

    [Header("Animator locomotion speed")]
    [Tooltip("Input magnitude >= giá trị này được coi là đang di chuyển (cập nhật thời điểm \"có move\").")]
    [SerializeField] float locomotionIdleMoveThreshold = 0.05f;
    [Tooltip("Sau khi input < threshold liên tục đủ lâu mới snap speed=0 (tránh jitter, vẫn blend mượt lúc vừa buông phím).")]
    [SerializeField] float locomotionIdleSnapAfterSeconds = 0.2f;
    float lastLocomotionMoveTime;
    float lastLocomotionMoveRenderTime = -999f;

    /// <summary>Hai ray dưới chân đều chạm ground layer (cập nhật mỗi Update, trước input).</summary>
    public bool CachedGroundedFeet { get; private set; }

    private InputAction jumpActionCache;
    float lastGroundedFeetTime = -999f;
    float lastGroundedStableTime = -999f;
    float jumpAllowedAfterTime = -999f;

    private int originalLayer; // Store original layer before dash

    private Vector3 initialModelLocalPosition;

    int upperBodyAnimatorLayerIndex = -1;
    int lowerBodyAnimatorLayerIndex = -1;

    /// <summary>Cached Animator layer index for <c>Upper body</c> (draw/sheath timing). Falls back to 4.</summary>
    public int UpperBodyAnimatorLayerIndex => upperBodyAnimatorLayerIndex >= 0 ? upperBodyAnimatorLayerIndex : 4;

    /// <summary>Cached Animator layer index for <c>Lower body</c> (landing legs). Falls back to 5.</summary>
    public int LowerBodyAnimatorLayerIndex => lowerBodyAnimatorLayerIndex >= 0 ? lowerBodyAnimatorLayerIndex : 5;

    void RefreshAnimatorLayerCaches()
    {
        upperBodyAnimatorLayerIndex = -1;
        lowerBodyAnimatorLayerIndex = -1;
        if (animator == null) return;
        upperBodyAnimatorLayerIndex = animator.GetLayerIndex("Upper body");
        if (upperBodyAnimatorLayerIndex < 0)
            upperBodyAnimatorLayerIndex = animator.GetLayerIndex("UpperBody_Hit");
        lowerBodyAnimatorLayerIndex = animator.GetLayerIndex("Lower body");
    }

    /// <summary>Sets an Animator bool only if a matching parameter exists (avoids errors before controller is updated).</summary>
    public void TrySetAnimatorBool(string paramName, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
            {
                animator.SetBool(paramName, value);
                return;
            }
        }
    }

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
        networkAnimator = GetComponentInChildren<NetworkMecanimAnimator>();

        if (animator != null)
        {
            // Lưu lại tọa độ gốc của Model (Thường là 0, -1, 0)
            initialModelLocalPosition = animator.transform.localPosition;
            
            // Ng định tắt Root Motion một lần và mãi mãi
            animator.applyRootMotion = false;
        }

        RefreshAnimatorLayerCaches();
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
                var wc = GetWeaponControllerInHierarchy();
                AbilityIconManager.Instance.BindToLocalPlayer(wc);
            }

            string saved = PlayerDisplayNamePrefs.GetSavedOrDefault();
            RPC_SetDisplayName(saved);
        }
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        RefreshAnimatorLayerCaches();
        
        if (!s_warnedMissingNetworkMecanimAnimator && networkAnimator == null && animator != null)
        {
            s_warnedMissingNetworkMecanimAnimator = true;
            Debug.LogWarning("[Character] No NetworkMecanimAnimator in children — SetTriggerSafe falls back to Animator.SetTrigger (Fusion rollback can drop triggers). Add NetworkMecanimAnimator on the player rig.", this);
        }

        CalculatedVelocity = Vector3.zero; // Reset v?n t?c ngay khi sinh ra
        if (Runner != null && Object != null && Object.HasStateAuthority)
        {
            NetLastToggleSimTime = -999f; // So first weapon-toggle buffer does not instantly satisfy cooldown vs SimTime 0.
            NetIsRollLanding = false;
            NetIsWeaponDrawn = false; // default sheathed at spawn (Draw/Sheath states update when needed)
            NetEquippedWeaponType = (int)WeaponType.None;
            NetworkedIsLanding = false;
            NetLandingRecoveryTimer = TickTimer.None;
            NetJumpBufferRemaining = 0f;
            NetAnimSpeedTarget = 0f;
        }

        if (animator != null)
        {
            animator.SetBool("isWeaponDrawn", NetIsWeaponDrawn);
            animator.SetBool("isRollLanding", NetIsRollLanding);
            TrySetAnimatorBool(AnimatorKneeLandingParam, NetworkedIsLanding);
        }

        var weaponCtrl = GetWeaponControllerInHierarchy();
        if (HasInputAuthority && weaponCtrl != null && weaponCtrl.GetCurrentWeapon() != null)
            SyncEquippedWeaponType(weaponCtrl.GetCurrentWeapon().weaponType);
        if (weaponCtrl != null && NetEquippedWeaponType != (int)WeaponType.None)
            weaponCtrl.ApplyNetworkWeaponType((WeaponType)NetEquippedWeaponType);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        CancelInvoke(nameof(DebugLogNetEquippedWeaponDelayedTick));
        if (HasInputAuthority && LocalCharacter == this)
            LocalCharacter = null;
        base.Despawned(runner, hasState);
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
        drawingWeapon = new DrawWeaponState(this, movementSM);
        sheathingWeapon = new SheathWeaponState(this, movementSM);

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
        var weaponController = GetWeaponControllerInHierarchy();
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
            // Critical for client resimulation: keep last sampled input when GetInput misses.
            // Resetting buttons to default here fabricates false edges (0->1) on next successful sample.
            if (debugCritFsmLogs && HasInputAuthority && Runner != null && Runner.IsRunning)
            {
                int tick = (int)Runner.Tick;
                if (tick != _lastNoInputWarnTick)
                {
                    _lastNoInputWarnTick = tick;
                    LogCritFsm("Input", $"MISS GetInput at tick={tick} -> reuse previous input");
                }
            }
        }

        if (debugCritFsmLogs && HasInputAuthority && Runner != null && Runner.IsRunning)
        {
            bool jumpEdge = currentInput.buttons.IsSet(NetworkInputButtons.Jump) && !previousInput.buttons.IsSet(NetworkInputButtons.Jump);
            bool dashEdge = currentInput.buttons.IsSet(NetworkInputButtons.Dash) && !previousInput.buttons.IsSet(NetworkInputButtons.Dash);
            bool attackEdge = currentInput.buttons.IsSet(NetworkInputButtons.Attack) && !previousInput.buttons.IsSet(NetworkInputButtons.Attack);
            bool toggleEdge = currentInput.buttons.IsSet(NetworkInputButtons.ToggleWeapon) && !previousInput.buttons.IsSet(NetworkInputButtons.ToggleWeapon);
            int edgeMask = (jumpEdge ? 1 : 0) | (dashEdge ? 2 : 0) | (attackEdge ? 4 : 0) | (toggleEdge ? 8 : 0);
            int tick = (int)Runner.Tick;
            if (edgeMask != 0)
            {
                if (tick == _lastInputEdgeTick && edgeMask == _lastInputEdgeMask)
                {
                    _inputEdgeReplayCount++;
                    if (_inputEdgeReplayCount == 2 || _inputEdgeReplayCount == 5 || _inputEdgeReplayCount == 10)
                    {
                        LogCritFsm("Input",
                            $"EDGE replay tick={tick} x{_inputEdgeReplayCount} jump={jumpEdge} dash={dashEdge} attack={attackEdge} tab={toggleEdge}");
                    }
                }
                else
                {
                    _lastInputEdgeTick = tick;
                    _lastInputEdgeMask = edgeMask;
                    _inputEdgeReplayCount = 1;
                    LogCritFsm("Input",
                        $"EDGE tick={tick} jump={jumpEdge} dash={dashEdge} attack={attackEdge} tab={toggleEdge} prev={previousInput.buttons} cur={currentInput.buttons}");
                }
            }
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
            Vector3 pVel = PlayerVelocity;

            // 1. Giữ nhân vật bấm sn nếu đang đứng trên đất
            if (IsGroundedStable() && pVel.y < 0)
            {
                pVel.y = -8f; 
            }

            // 2. Tích luỹ trọng lực (Nếu không phải đang Dash)
            if (!IsDashing)
            {
                pVel.y += gravityValue * Runner.DeltaTime;
            }

            PlayerVelocity = pVel;

            // 3. Gộp vận tốc dọc (Trọng lực/Nhảy) vào vận tốc ngang (FSM)
            CalculatedVelocity.y = PlayerVelocity.y;

            // 4. Lệnh di chuyển vật lý cuối cùng
            controller.Move(CalculatedVelocity * Runner.DeltaTime);
        }

        // Animator bool (optional): Running Jump → Unarmed locomotion; add param "isGrounded" on controller.
        TrySetAnimatorBool("isGrounded", IsGroundedStable());
    }
    
    public override void Render()
    {
        // Render-only animator speed smoothing (client-friendly; avoids FUN resim jitter).
        ApplyAnimatorLocomotionSpeedVisual(NetAnimSpeedTarget, Time.deltaTime, useRenderClock: true);

        if (_changeDetector == null)
            return;
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetworkedState))
            {
                // Proxy animation handling can go here
            }
            else if (change == nameof(DisplayName) || change == nameof(DisplayNameColorArgb))
            {
                DisplayInfoChanged?.Invoke();
            }
            else if (change == nameof(NetIsRollLanding))
            {
                if (animator != null)
                    animator.SetBool("isRollLanding", NetIsRollLanding);
            }
            else if (change == nameof(NetIsWeaponDrawn))
            {
                if (animator != null)
                    animator.SetBool("isWeaponDrawn", NetIsWeaponDrawn);
                LogCritFsm("Render", $"NetIsWeaponDrawn → {NetIsWeaponDrawn} (anim bool synced)");

                var weaponCtrl = GetWeaponControllerInHierarchy();
                if (weaponCtrl != null)
                {
                    if (NetIsWeaponDrawn)
                        weaponCtrl.EnsureDrawn(requestAnimation: false);
                    else
                        weaponCtrl.EnsureSheathed(requestAnimation: false);
                }
            }
            else if (change == nameof(NetEquippedWeaponType))
            {
                if (debugNetEquippedWeaponLog)
                {
                    Debug.Log(
                        $"[NetWeapon] Render NetEquippedWeaponType={(WeaponType)NetEquippedWeaponType} SA={HasStateAuthority} IA={HasInputAuthority} tick={(Runner != null && Runner.IsRunning ? Runner.Tick.ToString() : "?")} obj={name}",
                        this);
                }
                var weaponCtrl = GetWeaponControllerInHierarchy();
                if (weaponCtrl != null)
                    weaponCtrl.ApplyNetworkWeaponType((WeaponType)NetEquippedWeaponType);
            }
            else if (change == nameof(NetworkedIsLanding))
            {
                TrySetAnimatorBool(AnimatorKneeLandingParam, NetworkedIsLanding);
            }
            else if (change == nameof(LandEventCount))
            {
                LandingEventForVfx?.Invoke();
            }
        }
    }

    public enum PlayerUltimateVisualKind : byte
    {
        Mage = 0,
        Sword = 1,
        Axe = 2
    }

    /// <summary>Play ultimate timeline / VFX on every peer (camera bind stays local-only inside skill).</summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_PlayPlayerUltimate(PlayerUltimateVisualKind kind)
    {
        switch (kind)
        {
            case PlayerUltimateVisualKind.Mage:
                GetComponentInChildren<MageSkills>(true)?.PlayUltimateFromNetworkRpc();
                break;
            case PlayerUltimateVisualKind.Sword:
                GetComponentInChildren<SwordSkills>(true)?.PlayUltimateFromNetworkRpc();
                break;
            case PlayerUltimateVisualKind.Axe:
                GetComponentInChildren<AxeSkill>(true)?.PlayUltimateFromNetworkRpc();
                break;
        }
    }

    /// <summary>
    /// Broadcast ultimate visual timeline from the local input owner.
    /// Returns true when an RPC was sent; false when caller should play local fallback.
    /// </summary>
    public bool TryBroadcastUltimateTimeline(PlayerUltimateVisualKind kind)
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
            return false;
        if (!HasInputAuthority)
            return false;

        RPC_PlayPlayerUltimate(kind);
        return true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SyncWeaponDrawn(bool drawn)
    {
        NetIsWeaponDrawn = drawn;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SyncEquippedWeaponType(int weaponTypeValue)
    {
        int before = NetEquippedWeaponType;
        NetEquippedWeaponType = weaponTypeValue;
        if (debugNetEquippedWeaponLog)
        {
            Debug.Log(
                $"[NetWeapon] RPC_SyncEquippedWeaponType APPLIED on StateAuthority {before} → {weaponTypeValue} ({(WeaponType)weaponTypeValue}) obj={name} SA={HasStateAuthority} IA={HasInputAuthority}",
                this);
        }
    }

    /// <summary>Replicate weapon drawn flag (InputAuthority → StateAuthority RPC if needed).</summary>
    public void SyncWeaponDrawnState(bool drawn)
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
            return;
        if ((bool)NetIsWeaponDrawn == drawn)
            return;
        LogCritFsm("WeaponNet", $"SyncWeaponDrawnState → {drawn} (was {(bool)NetIsWeaponDrawn})");
        if (HasStateAuthority)
            NetIsWeaponDrawn = drawn;
        else if (HasInputAuthority)
            RPC_SyncWeaponDrawn(drawn);

        if (animator != null && (HasStateAuthority || HasInputAuthority))
            animator.SetBool("isWeaponDrawn", drawn);
    }

    /// <summary>Replicate equipped weapon type so every peer can mirror visuals/scripts deterministically.</summary>
    public void SyncEquippedWeaponType(WeaponType type)
    {
        if (!Enum.IsDefined(typeof(WeaponType), type))
        {
            Debug.LogWarning($"[NetWeapon] SyncEquippedWeaponType INVALID type={type} ({(int)type}) obj={name}", this);
            return;
        }

        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
        {
            if (debugNetEquippedWeaponLog)
                Debug.LogWarning($"[NetWeapon] SyncEquippedWeaponType SKIP network not ready type={type} obj={name}", this);
            return;
        }

        int weaponTypeValue = (int)type;
        if (NetEquippedWeaponType == weaponTypeValue)
        {
            if (debugNetEquippedWeaponLog)
                Debug.Log($"[NetWeapon] SyncEquippedWeaponType SKIP (already {weaponTypeValue}) obj={name}", this);
            return;
        }

        if (HasStateAuthority)
        {
            if (debugNetEquippedWeaponLog)
                Debug.Log($"[NetWeapon] SyncEquippedWeaponType LOCAL SET SA {NetEquippedWeaponType} → {weaponTypeValue} ({(WeaponType)weaponTypeValue}) obj={name}", this);
            NetEquippedWeaponType = weaponTypeValue;
        }
        else if (HasInputAuthority)
        {
            if (debugNetEquippedWeaponLog)
                Debug.Log($"[NetWeapon] SyncEquippedWeaponType RPC SEND {NetEquippedWeaponType} → {weaponTypeValue} ({(WeaponType)weaponTypeValue}) obj={name}", this);
            RPC_SyncEquippedWeaponType(weaponTypeValue);
        }
        else if (debugNetEquippedWeaponLog)
        {
            Debug.LogWarning(
                $"[NetWeapon] SyncEquippedWeaponType NO PATH (not SA, not IA) wanted={(WeaponType)weaponTypeValue} obj={name} — networked weapon will NOT update.",
                this);
        }
    }

    /// <summary>Call from UI after EquipWeapon to log replicated value a few frames later (same-frame read is often stale on client).</summary>
    public void DebugLogNetEquippedWeaponDelayed()
    {
        if (!debugNetEquippedWeaponLog) return;
        CancelInvoke(nameof(DebugLogNetEquippedWeaponDelayedTick));
        _netWeaponDebugTicksRemaining = 6;
        InvokeRepeating(nameof(DebugLogNetEquippedWeaponDelayedTick), 0.05f, 0.05f);
    }

    int _netWeaponDebugTicksRemaining;

    void DebugLogNetEquippedWeaponDelayedTick()
    {
        if (this == null || !isActiveAndEnabled)
        {
            CancelInvoke(nameof(DebugLogNetEquippedWeaponDelayedTick));
            return;
        }
        if (!debugNetEquippedWeaponLog)
        {
            CancelInvoke(nameof(DebugLogNetEquippedWeaponDelayedTick));
            return;
        }
        _netWeaponDebugTicksRemaining--;
        Debug.Log(
            $"[NetWeapon] delayed tick NetEquippedWeaponType={(WeaponType)NetEquippedWeaponType} SA={HasStateAuthority} IA={HasInputAuthority} remaining={_netWeaponDebugTicksRemaining} obj={name}",
            this);
        if (_netWeaponDebugTicksRemaining <= 0)
            CancelInvoke(nameof(DebugLogNetEquippedWeaponDelayedTick));
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SyncNetRollLanding(bool roll)
    {
        NetIsRollLanding = roll;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SyncNetworkedIsLanding(bool value)
    {
        NetworkedIsLanding = value;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_BumpLandEventCount()
    {
        LandEventCount++;
    }

    /// <summary>Replicated knee-landing flag (InputAuthority → StateAuthority RPC). Animator mirrors in <see cref="Render"/>.</summary>
    public void SetNetworkedKneeLanding(bool value)
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
            return;
        if (NetworkedIsLanding == value)
            return;
        if (HasStateAuthority)
            NetworkedIsLanding = value;
        else if (HasInputAuthority)
            RPC_SyncNetworkedIsLanding(value);
    }

    /// <summary>Pattern 3: monotonic counter; bump on StateAuthority or RPC from InputAuthority.</summary>
    public void IncrementLandEventCount()
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
            return;
        if (HasStateAuthority)
            LandEventCount++;
        else if (HasInputAuthority)
            RPC_BumpLandEventCount();
    }

    /// <summary>Replicate roll-landing flag for high fall → dash roll branch.</summary>
    public void SyncRollLandingState(bool roll)
    {
        if (Object == null || !Object.IsValid || Runner == null || !Runner.IsRunning)
            return;
        if ((bool)NetIsRollLanding == roll)
            return;
        if (HasStateAuthority)
            NetIsRollLanding = roll;
        else if (HasInputAuthority)
            RPC_SyncNetRollLanding(roll);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetDisplayName(string name)
    {
        string s = PlayerDisplayNamePrefs.Sanitize(name ?? "");
        DisplayName = s;
        DisplayNameColorArgb = PlayerNameplate.StableColorArgbFromString(s);
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
            NetJumpBufferRemaining = jumpBufferDuration;
        else
            NetJumpBufferRemaining = Mathf.Max(0f, NetJumpBufferRemaining - Runner.DeltaTime);

        // Không giữ buffer trong phase bay lên (tránh spam Space tích buffer rồi chạm đất là nhảy lại ngay).
        if (controller != null && controller.velocity.y > 0.25f)
            NetJumpBufferRemaining = 0f;
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
        NetJumpBufferRemaining = 0f;
        jumpAllowedAfterTime = Runner.SimulationTime + Mathf.Max(0f, jumpCooldownSeconds);
    }

    /// <summary>Consume một lần nhảy đã buffer khi được phép và đang grounded/coyote.</summary>
    public bool TryConsumeJumpBuffered(bool canJump)
    {
        if (!canJump || NetJumpBufferRemaining <= 0f) return false;
        if (Runner.SimulationTime < jumpAllowedAfterTime) return false;
        if (!IsGroundedForJump()) return false;
        NetJumpBufferRemaining = 0f;
        return true;
    }

    /// <summary>
    /// Blend tree locomotion (param "speed"): có move thì damp; khi input nhỏ thì vẫn damp tới khi đủ lâu không có move mới snap 0
    /// (cách 2 — tránh jitter sau dash nhưng blend lúc dừng vẫn mượt).
    /// </summary>
    public void SetAnimatorLocomotionSpeed(float targetMagnitude)
    {
        if (animator == null) return;
        targetMagnitude = Mathf.Max(0f, targetMagnitude);

        // When Fusion is running, simulation states should only write desired speed.
        // Actual damp/snap is applied in Render once per visual frame.
        if (Runner != null && Runner.IsRunning)
        {
            NetAnimSpeedTarget = targetMagnitude;
            return;
        }

        ApplyAnimatorLocomotionSpeedVisual(targetMagnitude, Time.deltaTime, useRenderClock: false);
    }

    void ApplyAnimatorLocomotionSpeedVisual(float targetMagnitude, float deltaTime, bool useRenderClock)
    {
        if (animator == null) return;
        float threshold = Mathf.Max(1e-4f, locomotionIdleMoveThreshold);
        float dt = Mathf.Max(1e-5f, deltaTime);

        if (useRenderClock)
        {
            if (targetMagnitude >= threshold)
                lastLocomotionMoveRenderTime = Time.time;

            if (targetMagnitude < threshold)
            {
                if (Time.time - lastLocomotionMoveRenderTime >= locomotionIdleSnapAfterSeconds)
                    animator.SetFloat("speed", 0f);
                else
                    animator.SetFloat("speed", targetMagnitude, speedDampTime, dt);
            }
            else
            {
                animator.SetFloat("speed", targetMagnitude, speedDampTime, dt);
            }
            return;
        }

        float simNow = Runner != null ? Runner.SimulationTime : Time.time;
        if (targetMagnitude >= threshold)
            lastLocomotionMoveTime = simNow;

        if (targetMagnitude < threshold)
        {
            if (simNow - lastLocomotionMoveTime >= locomotionIdleSnapAfterSeconds)
                animator.SetFloat("speed", 0f);
            else
                animator.SetFloat("speed", targetMagnitude, speedDampTime, dt);
        }
        else
        {
            animator.SetFloat("speed", targetMagnitude, speedDampTime, dt);
        }
    }

    /// <summary>
    /// Prefer this over <see cref="Animator.SetTrigger(string)"/> from FSM / <see cref="FixedUpdateNetwork"/> so triggers survive Fusion rollback and replicate via <see cref="NetworkMecanimAnimator"/>.
    /// </summary>
    public void SetTriggerSafe(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
            return;
        SetTriggerSafeInternal(Animator.StringToHash(triggerName), triggerName);
    }

    /// <summary>Hash overload for callers using <see cref="Animator.StringToHash"/> (avoids string alloc in hot paths).</summary>
    public void SetTriggerSafe(int triggerHash)
    {
        if (triggerHash == 0)
            return;
        string resolved = null;
        if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.type != AnimatorControllerParameterType.Trigger) continue;
                if (Animator.StringToHash(p.name) != triggerHash) continue;
                resolved = p.name;
                break;
            }
        }
        SetTriggerSafeInternal(triggerHash, resolved);
    }

    void SetTriggerSafeInternal(int triggerHash, string resolvedName)
    {
        if (triggerHash == 0)
            return;
        bool shouldLog = !string.IsNullOrEmpty(resolvedName) && IsCritFsmLoggedTrigger(resolvedName);
        bool suppress = ShouldSuppressDuplicateTriggerThisTick(triggerHash, out int tick);
        if (suppress)
        {
            if (debugCritFsmLogs && shouldLog)
                LogCritFsm("Trigger", $"SUPPRESS duplicate \"{resolvedName}\" tick={tick}");
            return;
        }
        if (debugCritFsmLogs)
        {
            if (shouldLog)
                LogCritFsm("Trigger", $"SetTriggerSafe(\"{resolvedName}\") NMA={(networkAnimator != null)} tick={(Runner != null && Runner.IsRunning ? ((int)Runner.Tick).ToString() : "?")} | {CritFsmAnimatorLayerDump()}");
            else if (resolvedName == null)
                LogCritFsm("Trigger", $"SetTriggerSafe(UNKNOWN hash={triggerHash})");
        }
        if (networkAnimator != null)
            networkAnimator.SetTrigger(triggerHash, true);
        else if (animator != null)
            animator.SetTrigger(triggerHash);
    }

    bool ShouldSuppressDuplicateTriggerThisTick(int triggerHash, out int tick)
    {
        tick = int.MinValue;
        if (Runner == null || !Runner.IsRunning)
            return false;
        tick = (int)Runner.Tick;
        for (int i = 0; i < TriggerDedupeSlots; i++)
        {
            if (_triggerDedupeHashes[i] != triggerHash) continue;
            if (_triggerDedupeTicks[i] != tick) continue;
            return true;
        }
        _triggerDedupeHashes[_triggerDedupeCursor] = triggerHash;
        _triggerDedupeTicks[_triggerDedupeCursor] = tick;
        _triggerDedupeCursor = (_triggerDedupeCursor + 1) % TriggerDedupeSlots;
        return false;
    }

    public string GetCritFsmAnimatorSnapshot() => CritFsmAnimatorLayerDump();

    static bool IsCritFsmLoggedTrigger(string triggerName)
    {
        return triggerName == "isLanding" || triggerName == "drawWeapon" || triggerName == "sheathWeapon"
               || triggerName == "jump" || triggerName == "sprintJump" || triggerName == "dash" || triggerName == "attack";
    }

    string CritFsmAnimatorLayerDump()
    {
        if (animator == null) return "anim=null";
        int ub = UpperBodyAnimatorLayerIndex;
        int lb = LowerBodyAnimatorLayerIndex;
        var s = $"L0={DescribeAnimState(0)}";
        if (ub >= 0 && ub < animator.layerCount)
            s += $" UB[{ub}]={DescribeAnimState(ub)}";
        if (lb >= 0 && lb < animator.layerCount)
            s += $" LB[{lb}]={DescribeAnimState(lb)}";
        s += $" combatMove={(animator.GetBool("combatMove") ? 1 : 0)}";
        return s;
    }

    string CritFsmAnimatorBoolDump()
    {
        if (animator == null) return "animBool=null";
        string g = TryReadAnimatorBool("isGrounded");
        string roll = TryReadAnimatorBool("isRollLanding");
        string weapon = TryReadAnimatorBool("isWeaponDrawn");
        string knee = TryReadAnimatorBool(AnimatorKneeLandingParam);
        return $"bools(gnd={g}, roll={roll}, weapon={weapon}, knee={knee})";
    }

    string TryReadAnimatorBool(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return "na";
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
                return animator.GetBool(paramName) ? "1" : "0";
        }
        return "na";
    }

    string DescribeAnimState(int layer)
    {
        if (animator == null || layer < 0 || layer >= animator.layerCount) return "?";
        var i = animator.GetCurrentAnimatorStateInfo(layer);
        bool tr = animator.IsInTransition(layer);
        string clip = "—";
        var infos = animator.GetCurrentAnimatorClipInfo(layer);
        if (infos.Length > 0 && infos[0].clip != null)
            clip = infos[0].clip.name;
        return tr ? $"{clip}→trans nt={i.normalizedTime:F2}" : $"{clip} nt={i.normalizedTime:F2}";
    }

    /// <summary>CritFSM: deep probe for landing mismatch (Current vs Next during transitions, hashes, net vs anim knee).</summary>
    public string GetCritFsmLandingProbe()
    {
        if (animator == null) return "anim=null";
        bool kneeParam = false;
        foreach (var p in animator.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Bool || p.name != AnimatorKneeLandingParam)
                continue;
            kneeParam = true;
            break;
        }

        string animKnee = kneeParam ? (animator.GetBool(AnimatorKneeLandingParam) ? "1" : "0") : "na";
        string tick = Runner != null && Runner.IsRunning ? $"{Runner.Tick}" : "?";
        return
            $"tick={tick} netKnee={(NetworkedIsLanding ? 1 : 0)} animKnee={animKnee} | {CritFsmLandingLayerProbe(0, "LightLanding")} || {CritFsmLandingLayerProbe(LowerBodyAnimatorLayerIndex, "Landing")}";
    }

    string CritFsmLandingLayerProbe(int layer, string expectedLeafName)
    {
        if (animator == null || layer < 0 || layer >= animator.layerCount)
            return $"L{layer}=bad";
        var cur = animator.GetCurrentAnimatorStateInfo(layer);
        bool tr = animator.IsInTransition(layer);
        bool curHit = cur.IsName(expectedLeafName);
        string clip = "—";
        var infos = animator.GetCurrentAnimatorClipInfo(layer);
        if (infos.Length > 0 && infos[0].clip != null)
            clip = infos[0].clip.name;
        string nextHitStr = "";
        float trNorm = 0f;
        if (tr)
        {
            var nx = animator.GetNextAnimatorStateInfo(layer);
            bool nxHit = nx.IsName(expectedLeafName);
            var tinf = animator.GetAnimatorTransitionInfo(layer);
            trNorm = tinf.normalizedTime;
            string nxClip = "—";
            var nxInfos = animator.GetNextAnimatorClipInfo(layer);
            if (nxInfos.Length > 0 && nxInfos[0].clip != null)
                nxClip = nxInfos[0].clip.name;
            nextHitStr = $" nxHit={(nxHit ? 1 : 0)} nxClip={nxClip} nxNT={nx.normalizedTime:F2} nxHash=0x{nx.fullPathHash:X8} trNorm={trNorm:F2}";
        }

        return $"L{layer} exp={expectedLeafName} curHit={(curHit ? 1 : 0)} clip={clip} curNT={cur.normalizedTime:F2} curHash=0x{cur.fullPathHash:X8} tr={(tr ? 1 : 0)}{nextHitStr}";
    }

    /// <summary>
    /// Clears a trigger on the Unity <see cref="Animator"/> only when <see cref="NetworkMecanimAnimator"/> is absent.
    /// With NMA, Fusion owns trigger timing; calling <see cref="Animator.ResetTrigger(string)"/> directly can fight queued triggers.
    /// </summary>
    public void ResetTriggerSafe(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
            return;
        if (networkAnimator != null)
            return;
        if (animator != null)
            animator.ResetTrigger(triggerName);
    }

    /// <inheritdoc cref="ResetTriggerSafe(string)"/>
    public void ResetTriggerSafe(int triggerHash)
    {
        if (triggerHash == 0)
            return;
        if (networkAnimator != null)
            return;
        if (animator != null)
            animator.ResetTrigger(triggerHash);
    }

    public void QueueWeaponAction(QueuedWeaponAction action)
    {
        if (action == QueuedWeaponAction.None) return;
        if (queuedWeaponAction != QueuedWeaponAction.None)
        {
            LogCritFsm("Queue", $"QueueWeaponAction({action}) IGNORED — already queued {queuedWeaponAction}");
            return; // buffer exactly once
        }

        LogCritFsm("Queue", $"QueueWeaponAction({action}) OK");
        queuedWeaponAction = action;
    }

    public void QueueWeaponActionFromCurrentContext()
    {
        // Lấy sự thật tuyệt đối từ FSM C#, cấm hỏi ý kiến Animator!
        QueueWeaponAction(isWeaponDrawn ? QueuedWeaponAction.Sheath : QueuedWeaponAction.Draw);
    }

    public bool TryConsumeQueuedWeaponAction(out QueuedWeaponAction action)
    {
        action = queuedWeaponAction;
        if (action == QueuedWeaponAction.None) return false;

        queuedWeaponAction = QueuedWeaponAction.None;
        return true;
    }

    

    /// <summary>
    /// Update speed values based on equipped gems and equipment: speed = baseSpeed + (baseSpeed × gem%) + (baseSpeed × equipment%)
    /// </summary>
    private void UpdateSpeedWithGems()
    {
        float gemSpeedPercent = 0f;
        float equipmentSpeedPercent = 0f;

        // Get gem speed multiplier
        var weaponController = GetWeaponControllerInHierarchy();
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
        var weaponController = GetWeaponControllerInHierarchy();
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














