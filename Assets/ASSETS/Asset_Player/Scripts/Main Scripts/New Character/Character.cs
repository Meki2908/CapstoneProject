using UnityEngine;
using UnityEngine.InputSystem;
public class Character : MonoBehaviour
{
    [Header("Controls")]
    public float playerSpeed = 5.0f;
    public float crouchSpeed = 2.0f;
    public float sprintSpeed = 7.0f;
    public float dashSpeed = 10.0f;
    public float jumpHeight = 0.8f;
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
    public LandingState landing;
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

    public State currentLocomotionState;
    public State lastStateBeforeHit; // Track state before getting hit
    public float lastAttackInputTime; // Track when attack was last pressed

    //public bool isInCombatState { get; set; }
    public bool isWeaponDrawn { get; set; }
    public bool IsDashing { get; set; } // For invincibility frame during dash

    private int originalLayer; // Store original layer before dash
    private const int NOTHING_LAYER = 0; // Unity's "Nothing" layer index

    // Start is called before the first frame update
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        cameraTransform = Camera.main.transform;

        movementSM = new StateMachine();
        standing = new StandingState(this, movementSM);
        jumping = new JumpingState(this, movementSM);
        crouching = new CrouchingState(this, movementSM);
        landing = new LandingState(this, movementSM);
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
    }

    private void OnEquipmentChanged()
    {
        // Update speeds when equipment changes
        UpdateSpeedWithGems();
    }

    private void Update()
    {
        movementSM.currentState.HandleInput();

        movementSM.currentState.LogicUpdate();
    }

    private void FixedUpdate()
    {
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