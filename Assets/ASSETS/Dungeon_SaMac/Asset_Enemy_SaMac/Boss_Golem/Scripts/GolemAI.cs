using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
// Force recompilation
//script su dung GolemAnimator.controller
// Updated to use GolemDamageHandler instead of Enemy.GolemDamageHandler
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class GolemAI : MonoBehaviour
{
    public enum State
    {
        Sleeping,
        IdleAction,
        Idle,
        Patrol,
        Chase,
        Attack,
        Rage,
        Dead
    }

    public enum AttackType
    {
        LightAttack,    // Hit nhẹ
        HeavyAttack,    // Hit mạnh
        ComboAttack,    // Combo 3 hit
        SpinAttack,     // Đòn xoay
        LeapAttack,     // Đòn nhảy
        GroundSlam,     // Đòn đấm đất
        RageSmash       // Đòn đấm đặc biệt khi Rage
    }

    [Header("References")]
    public Animator animator;
    public NavMeshAgent agent;
    [Tooltip("TakeDamageTest component - auto-created if missing")]
    public TakeDamageTest healthBarSystem;
    [Tooltip("Custom Golem damage handler - optional for boss-specific logic")]
    public GolemDamageHandler golemDamageHandler;

    [Header("Detection / Movement")]
    public LayerMask playerLayer;
    public float detectionRadius = 15f;
    public float attackRange = 3f;
    public float patrolRadius = 6f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Combat")]
    public string attackTrigger = "Hit";
    public string walkParameter = "Walk";
    public string dieTrigger = "Die";
    public string rageTrigger = "Rage";
    public string idleActionTrigger = "IdleAction";
    public string sleepStartTrigger = "SleepStart";
    public string sleepEndTrigger = "SleepEnd";
    public string wakeUpTrigger = "WakeUp";

    // Boss Attack System
    [Tooltip("Cooldown cho từng loại tấn công - tăng thêm 2s nữa để boss không attack liên tục")]
    public float lightAttackCooldown = 3.5f; // Tăng thêm 2s nữa (từ 1.5f lên 3.5f)
    public float heavyAttackCooldown = 5.0f; // Tăng thêm 2s nữa (từ 3.0f lên 5.0f)
    public float comboAttackCooldown = 7.0f; // Tăng thêm 2s nữa (từ 5.0f lên 7.0f)
    public float spinAttackCooldown = 6.0f; // Tăng thêm 2s nữa (từ 4.0f lên 6.0f)
    public float groundSlamCooldown = 9.0f; // Tăng thêm 2s nữa (từ 7.0f lên 9.0f)
    public float rageSmashCooldown = 5.0f; // Tăng thêm 2s nữa (từ 3.0f lên 5.0f)

    [Tooltip("Damage multiplier cho từng loại tấn công")]
    public float lightAttackDamage = 1.0f;
    public float heavyAttackDamage = 1.8f;
    public float comboAttackDamage = 2.5f;
    public float spinAttackDamage = 2.2f;
    public float groundSlamDamage = 3.0f;
    public float rageSmashDamage = 4.0f;

    [Tooltip("Range cho từng loại tấn công")]
    public float lightAttackRange = 3f;
    public float heavyAttackRange = 3.5f;
    public float comboAttackRange = 4f;
    public float spinAttackRange = 5f;
    public float groundSlamRange = 6f;

    // Boss Behavior
    [Tooltip("Số hit trong combo tối đa")]
    public int maxComboHits = 3;
    [Tooltip("Thời gian delay giữa các hit trong combo - tăng để player có cơ hội sống sót")]
    public float comboHitDelay = 3.5f; // Tăng thêm 2s nữa (từ 1.5f lên 3.5f)
    [Tooltip("Thời gian chuẩn bị cho đòn mạnh - tăng để player có thời gian phản ứng")]
    public float heavyAttackPrepTime = 3.0f; // Tăng thêm 2s nữa (từ 1.0f lên 3.0f)
    [Tooltip("Thời gian chuẩn bị cho Ground Slam - tăng để player có thời gian chạy thoát")]
    public float groundSlamPrepTime = 4.0f; // Tăng thêm 2s nữa (từ 2.0f lên 4.0f)


    // simple Rage uses animator trigger only
    [Tooltip("Chance (0..1) to play an IdleAction while idle/patrolling")]
    public float idleActionChance = 0.1f;
    public float idleActionInterval = 10f; // Tăng thêm 2s nữa (từ 8f lên 10f)
    public float sleepDelay = 22f; // Tăng thêm 2s nữa (từ 20f lên 22f)
    public float sleepDuration = 8f; // Tăng thêm 2s nữa (từ 6f lên 8f)
    public float leapMinDistance = 4f;
    public float leapMaxDistance = 10f;
    public float maxChaseDistance = 50f;
    public float leapCooldown = 8f; // Tăng thêm 2s nữa (từ 6f lên 8f)
    [Header("Leap / Landing")]
    [Tooltip("Prefab spawned at landing position to telegraph where Golem will land")]
    public GameObject landingIndicatorPrefab;
    [Tooltip("How long the landing indicator remains before landing")]
    public float landingIndicatorDuration = 1f;
    [Tooltip("Max sample radius when finding valid NavMesh landing position")]
    public float leapLandingSampleRadius = 2f;
    [Tooltip("Duration of the leap animation/movement (seconds)")]
    public float leapDuration = 0.9f;
    [Tooltip("Multiplier applied to distance to compute arc height")]
    public float leapArcHeightMultiplier = 0.5f;
    [Tooltip("Minimum arc height for leap")]
    public float leapArcMinHeight = 2f;
    [Tooltip("Maximum arc height for leap")]
    public float leapArcMaxHeight = 8f;

    [Header("Phase Leap Settings (50% HP)")]
    [Tooltip("Duration of phase leap animation/movement (seconds) - slower for dramatic effect")]
    public float phaseLeapDuration = 1.5f;
    [Tooltip("Simple height multiplier for phase leap (1.0 = same as regular, 2.0 = twice as high)")]
    public float phaseLeapHeightMultiplier = 2.0f;
    [Tooltip("Prefab for ice spike effect spawned at landing (assign IceSpikes2.fbx or prefab)")]
    public GameObject iceSpikePrefab;
    [Tooltip("How long spawned ice spikes should exist (seconds)")]
    public float iceSpikeDuration = 3f;
    [Tooltip("Multiplier applied to ice spike model height")]
    public float iceSpikeHeightMultiplier = 1f;
    [Tooltip("Multiplier applied to ice spike model radius (X/Z)")]
    public float iceSpikeRadiusMultiplier = 1f;
    [Tooltip("Optional material to apply to spawned ice spikes")]
    public Material iceSpikeMaterial;
    [Tooltip("How deep below ground spikes spawn before rising")]
    public float iceSpikeSpawnDepth = 0.6f;
    [Tooltip("How long spikes take to rise into place (seconds)")]
    public float iceSpikeRiseDuration = 0.3f;
    [Header("Phase Sequence (50% HP)")]
    public bool enablePhaseAtHalfHealth = true;
    private bool phaseTriggered = false;
    public GameObject orbitingOrbPrefab;
    public int orbitingOrbCount = 5;
    public float orbitRadius = 4f;
    public float orbitDuration = 2.5f;
    public GameObject modelVFXPrefab;
    public float modelVFXDuration = 4f;
    [Tooltip("Duration to play Rage animation during phase (seconds)")]
    public float rageAnimationDuration = 2.0f;
    public GameObject lineProjectilePrefab;
    public float lineProjectileSpeed = 12f;
    public float lineProjectileLifetime = 3f;
    [Tooltip("Show a Run Phase button in inspector via context menu")]
    public bool showPhaseTestButton = true;
    [Tooltip("Material applied to orbiting orbs (optional)")]
    public Material orbitingOrbMaterial;

    [Header("Phase Ice Spikes Settings")]
    [Tooltip("Prefab for ice spikes spawned during phase attack")]
    public GameObject phaseIceSpikePrefab;
    [Tooltip("Material for phase ice spikes")]
    public Material phaseIceSpikeMaterial;
    [Tooltip("How far below ground phase ice spikes spawn")]
    public float phaseIceSpikeSpawnDepth = 1f;
    [Tooltip("Height multiplier for phase ice spikes")]
    public float phaseIceSpikeHeightMultiplier = 2f;
    [Tooltip("Radius multiplier for phase ice spikes")]
    public float phaseIceSpikeRadiusMultiplier = 1.5f;
    [Tooltip("How long phase ice spikes take to rise")]
    public float phaseIceSpikeRiseDuration = 0.8f;
    [Tooltip("How long phase ice spikes last before disappearing")]
    public float phaseIceSpikeDuration = 3f;
    [Tooltip("Material applied to line projectiles (optional)")]
    public Material lineProjectileMaterial;
    [Header("Ground Line Indicators")]
    [Tooltip("Material for ground line indicator (transparent red)")]
    public Material groundLineMaterial;
    [Tooltip("Width of ground line indicator")]
    public float groundLineWidth = 0.25f;
    [Tooltip("Length of ground line indicator (meters)")]
    public float groundLineLength = 12f;
    [Tooltip("Duration ground lines show before projectiles fire (seconds)")]
    public float groundLineDuration = 0.8f;

    [Header("Phase Attack Ice Spikes")]
    [Tooltip("Height multiplier for phase attack ice spikes (taller than regular)")]
    public float phaseAttackIceSpikeHeightMultiplier = 3f;
    [Tooltip("Radius multiplier for phase attack ice spikes (wider than regular)")]
    public float phaseAttackIceSpikeRadiusMultiplier = 2f;
    [Tooltip("How long phase attack ice spikes take to rise (slower than regular)")]
    public float phaseAttackIceSpikeRiseDuration = 1f;
    [Tooltip("How long phase attack ice spikes last (longer than regular)")]
    public float phaseAttackIceSpikeDuration = 4f;
    [Tooltip("Total angle spread for phase attack fan shape (degrees)")]
    public float phaseAttackFanAngle = 120f;
    [Tooltip("Number of ice spikes in phase attack")]
    public int phaseAttackRayCount = 6;
    [Tooltip("Distance from center to ice spikes in phase attack")]
    public float phaseAttackSpikeDistance = 3f;
    [Tooltip("Minimum distance from center to start spawning spikes (should match ground line start)")]
    public float phaseAttackSpikeMinDistance = 0.5f;
    [Tooltip("Number of spikes per ray line")]
    public int phaseAttackSpikesPerRay = 1;
    [Tooltip("Spacing between spikes on the same ray")]
    public float phaseAttackRaySpikeSpacing = 2f;
    [Tooltip("Width of ground line indicators in phase attack")]
    public float phaseAttackGroundLineWidth = 0.25f;
    [Tooltip("Length of ground line indicators in phase attack")]
    public float phaseAttackGroundLineLength = 12f;
    [Tooltip("Duration ground lines show in phase attack")]
    public float phaseAttackGroundLineDuration = 0.8f;

    [Header("Ice Spike Damage Settings")]
    [Tooltip("Damage multiplier for landing ice spikes (relative to ground slam damage)")]
    public float landingSpikeDamageMultiplier = 0.5f;
    [Tooltip("Fixed damage for landing ice spikes (if > 0, overrides multiplier)")]
    public float landingSpikeFixedDamage = 0f;
    [Tooltip("Hit radius for landing ice spikes collision detection")]
    public float landingSpikeHitRadius = 0.8f;
    [Tooltip("Weapon length for landing ice spikes damage range")]
    public float landingSpikeWeaponLength = 1f;
    [Tooltip("Damage multiplier for phase ice spikes (relative to rage smash damage)")]
    public float phaseSpikeDamageMultiplier = 0.6f;
    [Tooltip("Fixed damage for phase ice spikes (if > 0, overrides multiplier)")]
    public float phaseSpikeFixedDamage = 0f;
    [Tooltip("Hit radius for phase ice spikes collision detection")]
    public float phaseSpikeHitRadius = 1f;
    [Tooltip("Weapon length for phase ice spikes damage range")]
    public float phaseSpikeWeaponLength = 1.2f;
    [Tooltip("If true, the first ground-line in the phase will point directly at the player")]
    public bool phaseFirstLineToPlayer = true;
    [Header("Phase Test")]
    [Tooltip("Manually trigger the phase sequence at runtime (will auto-reset when used)")]
    public bool testPhaseNow = false;
    [Header("Orb Phase Settings")]
    [Tooltip("How far below ground orbiting orbs spawn")]
    public float orbSpawnDepth = 1f;
    [Tooltip("Height orbs rise up to before orbiting")]
    public float orbOrbitHeight = 6f;
    [Tooltip("How long orbs take to rise up (seconds)")]
    public float orbRiseDuration = 0.6f;
    [Tooltip("How long orbs orbit at height (seconds)")]
    public float orbOrbitDuration = 2.5f;
    [Tooltip("How long orbs take to collapse into golem (seconds)")]
    public float orbCollapseTime = 0.6f;
    [Tooltip("Speed of orb rotation in degrees per second")]
    public float orbRotationSpeed = 180f;
    [Tooltip("Final scale of orbs when fully risen")]
    public float orbFinalScale = 0.6f;

    [Header("🔥 Fire Phase Settings (After Orbs Absorbed)")]
    [Space(10)]
    [Tooltip("Material applied to golem model during fire phase - supports dual material blending")]
    public Material firePhaseMaterial;
    [Tooltip("Use dual material system (original + fire) instead of replacing material")]
    public bool useDualMaterialSystem = true;
    [Tooltip("Duration of fire phase effect on model (seconds)")]
    [Range(5f, 30f)]
    public float firePhaseDuration = 10f;
    [Tooltip("Damage multiplier during fire phase (1.0 = no change, 2.0 = double damage)")]
    [Range(1f, 3f)]
    public float firePhaseDamageMultiplier = 1.5f;
    [Tooltip("Movement speed multiplier during fire phase (1.0 = no change, 2.0 = double speed)")]
    [Range(1f, 2f)]
    public float firePhaseSpeedMultiplier = 1.2f;
    [Tooltip("Number of orbs required to trigger fire phase")]
    [Range(1, 10)]
    public int orbsRequiredForFire = 5;
    [Space(10)]
    [Header("🔍 Debug Info (Runtime Only)")]
    [Tooltip("Current number of absorbed orbs (resets on death)")]
    public int absorbedOrbsCount = 0;
    [Tooltip("Whether fire phase is currently active")]
    public bool isFirePhaseActive = false;
    private Material[] originalMaterials; // Store original materials array for each renderer
    private Renderer[] golemRenderers;

    [Header("Stats")]
    public float maxHealth = 1000f;
    [HideInInspector] public float currentHealth;

    [Header("Behavior")]
    [Tooltip("Always start in sleeping state - Golem will wake up when player approaches")]
    public bool alwaysStartSleeping = true;
    public float rageThresholdPercent = 0.33f;

    [Header("Debug")]
    public bool showDebug = false;

    private State currentState = State.Idle;
    private Transform playerTarget;
    private float lastAttackTime = -999f;
    private Vector3 patrolCenter;
    private Vector3 currentPatrolTarget;
    private float lastIdleActionTime = -999f;
    private float lastSleepCheck = -999f;
    private float lastLeapTime = -999f;
    private float idleActionStartTime = -999f;
    private float rageStartTime = -999f;
    private bool rageTransitionScheduled = false; // Track if Rage transition is already scheduled
    // keep sleeping flag
    private bool isSleeping = false;
    private bool isRagePlaying = false;
    private bool isIdleActionPlaying = false;
    private bool hasLoggedThisSecond = false; // For rate limiting logs
    private bool hasLoggedIdleAction = false; // For IdleAction logging
    private bool hasLoggedRage = false; // For Rage logging
    private bool hasLoggedDetect = false; // For DetectPlayer logging
    private bool hasLoggedDamage = false; // For damage timing logging
    // Damage sync helpers
    private int damageStateHash = 0;
    private bool damageDealtInState = false;
    private bool idleActionTransitionScheduled = false; // Ensure IdleAction only transitions to Rage

    // Boss Attack System Variables
    private float lastLightAttackTime = -999f;
    private float lastHeavyAttackTime = -999f;
    private float lastComboAttackTime = -999f;
    private float lastSpinAttackTime = -999f;
    private float lastGroundSlamTime = -999f;
    private float lastRageSmashTime = -999f;
    private int currentComboCount = 0;
    private float lastComboHitTime = -999f;
    private bool isPreparingAttack = false;
    private AttackType lastAttackType = AttackType.LightAttack;
    private int attackPatternIndex = 0;
    private float[] attackPatternWeights = { 0.4f, 0.25f, 0.15f, 0.1f, 0.07f, 0.03f }; // Light, Heavy, Combo, Spin, GroundSlam, RageSmash
    // Rotation lock while performing an attack to keep facing the initial target direction
    private bool rotationLocked = false;
    private Quaternion lockedRotation = Quaternion.identity;
    private float rotationLockEndTime = 0f;
    // Phase isolation flag - when true, normal AI updates/attacks are paused
    private bool inPhase = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent for smooth movement
        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.acceleration = 8f;  // Smooth acceleration
            agent.angularSpeed = 120f;  // Smooth rotation
            agent.stoppingDistance = 0.1f;  // Stop close to target
        }

        currentHealth = maxHealth;
        patrolCenter = transform.position;

        // Initialize boss attack system
        lastLightAttackTime = -999f;
        lastHeavyAttackTime = -999f;
        lastComboAttackTime = -999f;
        lastSpinAttackTime = -999f;
        lastGroundSlamTime = -999f;
        lastRageSmashTime = -999f;
        currentComboCount = 0;

        // Ưu tiên GolemDamageHandler, chỉ dùng TakeDamageTest làm fallback
        if (golemDamageHandler == null)
        {
            golemDamageHandler = GetComponent<GolemDamageHandler>();
        }

        // Chỉ tạo TakeDamageTest nếu không có GolemDamageHandler
        if (healthBarSystem == null && golemDamageHandler == null)
        {
            healthBarSystem = GetComponent<TakeDamageTest>();
            if (healthBarSystem == null)
            {
                healthBarSystem = gameObject.AddComponent<TakeDamageTest>();
            }
        }

        // Sync health values from GolemDamageHandler first
        if (golemDamageHandler != null)
        {
            maxHealth = golemDamageHandler.MaxHealth;
            currentHealth = golemDamageHandler.CurrentHealth;
        }

        // Cấu hình health bar system
        if (healthBarSystem != null)
        {
            healthBarSystem.UseHealthBar = true;
            healthBarSystem.BossName = "GOLEM BOSS";
            healthBarSystem.MaxHealth = maxHealth;
            healthBarSystem.CurrentHealth = currentHealth;
            healthBarSystem.HealthBarColor = Color.green; // Màu xanh cho Golem

        // DISABLE raycast damage vì GolemAI có logic tấn công riêng
        // Nếu không disable, TakeDamageTest sẽ tự động damage player mỗi 2 giây
        healthBarSystem.DisableRaycastDamage();

        if (showDebug) Debug.Log($"[GolemAI] Disabled raycast damage in Awake - GolemAI handles attacks manually. EnableRaycastDamage: {healthBarSystem.EnableRaycastDamage}");
        }

        // Force Animator to start in correct state by disabling and re-enabling
        if (animator != null)
        {
            animator.enabled = false;
            // Will be re-enabled in Start()
        }
    }

    private void Start()
    {
        // Delay initialization to ensure all components are ready
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait for Animator and other components to be fully initialized
        yield return new WaitForSeconds(0.2f);

        // DOUBLE-CHECK: Disable raycast damage on TakeDamageTest if it exists
        if (healthBarSystem != null)
        {
            healthBarSystem.DisableRaycastDamage();
            if (showDebug) Debug.Log($"[GolemAI] Re-disabled raycast damage in DelayedInit - EnableRaycastDamage: {healthBarSystem.EnableRaycastDamage}");
        }

        // Note: GolemDamageHandler không có raycast damage, chỉ nhận damage từ weapons

        // CHECK FOR MULTIPLE TakeDamageTest COMPONENTS
        TakeDamageTest[] allTakeDamageTests = GetComponents<TakeDamageTest>();
        if (showDebug && allTakeDamageTests.Length > 1)
        {
            Debug.LogWarning($"[GolemAI] Found {allTakeDamageTests.Length} TakeDamageTest components on Golem! This may cause issues.");
            for (int i = 0; i < allTakeDamageTests.Length; i++)
            {
                Debug.Log($"[GolemAI] TakeDamageTest[{i}]: EnableRaycastDamage = {allTakeDamageTests[i].EnableRaycastDamage}");
            }
        }

        // Re-enable Animator now that we're ready to set the correct state
        if (animator != null && !animator.enabled)
        {
            animator.enabled = true;
        }

        // Start in sleeping state by default
        if (alwaysStartSleeping)
        {
            SetState(State.Sleeping);
            if (showDebug) Debug.Log("[GolemAI] Initialized - Starting in SLEEPING state");
            // Ensure sleep animation is triggered after components are ready
            StartCoroutine(DelayedSleepTrigger());
        }
        else
        {
            SetState(State.Idle);
            if (showDebug) Debug.Log("[GolemAI] Initialized - Starting in IDLE state (sleeping disabled)");
        }
    }

    /// <summary>
    /// Force the golem back to sleeping state
    /// </summary>
    public void ForceSleep()
    {
        playerTarget = null; // Clear target when going to sleep
        SetState(State.Sleeping);
        if (showDebug) Debug.Log("[GolemAI] Forced to sleep");
    }

    private IEnumerator DelayedWakeUp(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null && currentState != State.Sleeping)
        {
            // CrossFade to Idle state
            animator.CrossFade("Idle", 0.1f, 0);
            if (showDebug) Debug.Log("[GolemAI] Crossfading to Idle animation after wake up");
        }
    }

    private IEnumerator DelayedRageEntry(float delay)
    {
        if (showDebug) Debug.Log($"[GolemAI] STARTING DelayedRageEntry with {delay}s delay - currentState: {currentState}");
        yield return new WaitForSeconds(delay);

        if (showDebug) Debug.Log($"[GolemAI] DelayedRageEntry delay completed - currentState: {currentState}, isRagePlaying: {isRagePlaying}");

        // Only enter Rage if still in IdleAction and not already transitioning
        if (currentState == State.IdleAction && !isRagePlaying)
        {
            if (showDebug) Debug.Log("[GolemAI] ✅ Delayed Rage entry completed - entering Rage state");
            SetState(State.Rage);
        }
        else
        {
            if (showDebug && currentState != State.IdleAction) Debug.Log($"[GolemAI] ❌ Delayed Rage entry cancelled - no longer in IdleAction (current: {currentState})");
            if (showDebug && isRagePlaying) Debug.Log("[GolemAI] ❌ Delayed Rage entry cancelled - Rage already playing");
        }
    }


    private IEnumerator DelayedSleepTrigger()
    {
        // Wait for Animator to be fully initialized
        yield return new WaitForSeconds(0.1f);
        if (animator != null && currentState == State.Sleeping)
        {
            // CrossFade to Sleep state
            animator.CrossFade("Sleep", 0.1f, 0);
            if (showDebug) Debug.Log("[GolemAI] Crossfading to Sleep animation - ensuring proper state");
        }
    }

    private void Update()
    {
        // Allow manual test trigger in Play mode
        if (Application.isPlaying && testPhaseNow && !inPhase)
        {
            testPhaseNow = false;
            StartCoroutine(PhaseSequence());
        }

        // Pause normal AI updates during special phase
        if (inPhase) return;

        if (currentState == State.Dead) return;

        DetectPlayer();

        switch (currentState)
        {
            case State.Sleeping:
                UpdateSleeping();
                break;
            case State.IdleAction:
                UpdateIdleAction();
                break;
            case State.Idle:
                UpdateIdle();
                break;
            case State.Patrol:
                UpdatePatrol();
                break;
            case State.Chase:
                UpdateChase();
                break;
            case State.Attack:
                UpdateAttack();
                break;
            case State.Rage:
                UpdateRage();
                break;
        }
    }

    private void DetectPlayer()
    {
        // Log every 2 seconds or when detecting player
        if (showDebug && (Mathf.FloorToInt(Time.time) % 2 == 0 && !hasLoggedDetect))
        {
            Debug.Log($"[GolemAI] DetectPlayer called - playerTarget: {playerTarget}, detectionRadius: {detectionRadius}");
            hasLoggedDetect = true;
        }
        if (Mathf.FloorToInt(Time.time) % 2 != 0) hasLoggedDetect = false;

        if (playerTarget != null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);
        if (showDebug && hits.Length > 0) Debug.Log($"[GolemAI] OverlapSphere found {hits.Length} colliders");

        if (hits.Length == 0) return;

        float best = float.MaxValue;
        Transform bestT = null;
        foreach (var c in hits)
        {
            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < best)
            {
                best = d;
                bestT = c.transform;
            }
        }

        if (bestT != null)
        {
            playerTarget = bestT;
            if (showDebug) Debug.Log($"[GolemAI] Detected player: {playerTarget.name} at {best:F2}m (currentState={currentState})");

            // Wake up from sleeping state
            if (showDebug) Debug.Log($"[GolemAI] Player detected, checking if Sleeping: currentState={currentState}");
            if (currentState == State.Sleeping)
            {
                if (showDebug) Debug.Log("[GolemAI] Detected player while Sleeping - starting wake up sequence");
                // Stop sleeping and go to IdleAction immediately
                if (isSleeping)
                {
                    isSleeping = false;
                    // Don't use CrossFade to avoid Animator transition conflicts
                    if (showDebug) Debug.Log("[GolemAI] Waking up from sleep, transitioning to IdleAction");
                    // Transition to IdleAction immediately
                    SetState(State.IdleAction);
                }
                else
                {
                    // If not sleeping (shouldn't happen), just go to IdleAction
                    if (showDebug) Debug.Log("[GolemAI] Not sleeping flag, directly setting to IdleAction");
                    SetState(State.IdleAction);
                }
            }
            else if (currentState == State.Idle || currentState == State.Patrol)
            {
                SetState(State.Chase);
            }
        }
    }

    private void UpdateSleeping()
    {
        // Golem is sleeping - wait for player detection
        // Log every 2 seconds
        if (showDebug && Mathf.FloorToInt(Time.time) % 2 == 0 && !hasLoggedThisSecond)
        {
            Debug.Log("[GolemAI] UpdateSleeping - Waiting for player detection");
            hasLoggedThisSecond = true;
        }
        if (Mathf.FloorToInt(Time.time) % 2 != 0) hasLoggedThisSecond = false; // Reset for next second

        agent.isStopped = true;

        // Ensure Walk parameter stays at 0 to prevent transitions
        if (animator != null)
        {
            animator.SetFloat("Walk", 0f);
        }

        // Only wake up when player is detected through normal detection
        // No automatic wake up - let DetectPlayer handle it
    }

    private void UpdateIdleAction()
    {
        agent.isStopped = true;
        SetAnimatorWalk(0f);

        // Trigger IdleAction animation once when entering this state
        if (Time.time - idleActionStartTime < 0.1f && animator != null)
        {
            isIdleActionPlaying = true;
            rageTransitionScheduled = false;
            idleActionTransitionScheduled = false; // Ensure clean start
            animator.SetTrigger(idleActionTrigger);
            if (showDebug) Debug.Log("[GolemAI] Triggered IdleAction animation - MANDATORY transition to Rage only");
        }

        // Check if IdleAction animation has completed
        if (animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName("IdleAction"))
        {
            float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

            // Debug: Show progress every 0.5 seconds
            if (showDebug && (int)(Time.time * 2) % 2 == 0 && !hasLoggedIdleAction)
            {
                Debug.Log($"[GolemAI] IdleAction progress: {normalizedTime:F3} (need >= 0.80)");
                hasLoggedIdleAction = true;
            }
            if ((int)(Time.time * 2) % 2 != 0) hasLoggedIdleAction = false;

            // FORCE transition to Rage when animation is nearly complete
            // But skip during phase sequence - let phase control timing
            if (normalizedTime >= 0.80f && !idleActionTransitionScheduled && !inPhase)
            {
                idleActionTransitionScheduled = true; // Lock: ONLY Rage transition
                isIdleActionPlaying = false;

                // Turn to face player immediately
                if (playerTarget != null)
                {
                    Vector3 dir = playerTarget.position - transform.position;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(dir);
                        transform.rotation = targetRotation;
                        if (showDebug) Debug.Log($"[GolemAI] TURNED to face player: dir={dir.normalized:F2}, rotation={transform.rotation.eulerAngles:F0}");
                    }
                    else
                    {
                        if (showDebug) Debug.Log("[GolemAI] Cannot turn - too close to player");
                    }
                }
                else
                {
                    if (showDebug) Debug.Log("[GolemAI] Cannot turn - playerTarget is null");
                }

                if (showDebug) Debug.Log($"[GolemAI] FORCE IdleAction → Rage at {normalizedTime:F2} (>= 0.80)");
                SetState(State.Rage);
                return;
            }
        }

        // Force transition if animation takes too long (2 seconds max)
        if (Time.time - idleActionStartTime >= 2f && !idleActionTransitionScheduled)
        {
            idleActionTransitionScheduled = true;
            isIdleActionPlaying = false;

            // Still try to turn to face player even in timeout
            if (playerTarget != null)
            {
                Vector3 dir = playerTarget.position - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                    if (showDebug) Debug.Log($"[GolemAI] TURNED to face player (timeout): dir={dir.normalized:F2}");
                }
            }

            if (showDebug) Debug.Log("[GolemAI] IdleAction FORCE transition (2s timeout) - going to Rage");
            SetState(State.Rage);
            return;
        }

        // Ultimate fallback
        if (Time.time - idleActionStartTime >= 8f && !idleActionTransitionScheduled)
        {
            idleActionTransitionScheduled = true;
            isIdleActionPlaying = false;
            if (showDebug) Debug.Log("[GolemAI] IdleAction ultimate fallback - Rage transition");
            SetState(State.Rage);
        }
    }

    private void UpdateIdle()
    {
        agent.isStopped = true;
        SetAnimatorWalk(0f);

        // Special case: If we just woke up from sleeping, transition to IdleAction
        if (playerTarget != null && currentState == State.Idle)
        {
            if (showDebug) Debug.Log("[GolemAI] Just woke up from sleeping - transitioning to IdleAction");
            SetState(State.IdleAction);
            return;
        }

        // Don't play IdleAction if Rage or IdleAction is currently playing
        if (!isRagePlaying && !isIdleActionPlaying)
        {
            if (Time.time - lastIdleActionTime >= idleActionInterval)
            {
                lastIdleActionTime = Time.time;
                if (Random.value <= idleActionChance)
                {
                    if (animator != null) animator.SetTrigger("IdleAction");
                    if (showDebug) Debug.Log("[GolemAI] Played IdleAction animation in Idle state");
                }
            }
        }
        // Removed automatic sleep logic - Golem now starts in Sleeping state
    }

    private void UpdatePatrol()
    {
        agent.speed = patrolSpeed;
        SetAnimatorWalk(patrolSpeed);
        agent.isStopped = false;

        if (!agent.hasPath || Vector3.Distance(transform.position, currentPatrolTarget) < 0.5f)
        {
            currentPatrolTarget = GetRandomPatrolPoint();
            agent.SetDestination(currentPatrolTarget);
            if (showDebug) Debug.Log($"[GolemAI] New patrol point: {currentPatrolTarget}");
        }
    }

    private void UpdateChase()
    {
        if (playerTarget == null)
        {
            SetState(State.Patrol);
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // If player runs too far, stop chasing and return to patrol
        if (distance > maxChaseDistance)
        {
            if (showDebug) Debug.Log($"[GolemAI] Player too far ({distance:F1}m > {maxChaseDistance}m) - returning to patrol");
            SetState(State.Patrol);
            return;
        }

        // Boss Leap Attack - thông minh hơn
        if (distance >= leapMinDistance && distance <= leapMaxDistance &&
            Time.time - lastLeapTime >= leapCooldown &&
            currentState != State.Rage) // Không leap khi Rage
        {
            // Boss có 70% cơ hội leap khi ở khoảng cách phù hợp
            if (Random.value < 0.7f)
            {
                lastLeapTime = Time.time;
                if (!inPhase)
                {
                StartCoroutine(PerformLeap());
                }
                return;
            }
        }

        if (distance <= attackRange)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
            }
            SetAnimatorWalk(0f);
            SetState(State.Attack);
            return;
        }

        // Only update destination if player moved significantly (reduce jitter)
        Vector3 currentAgentDestination = transform.position;
        bool agentAvailable = agent != null && agent.enabled && agent.isOnNavMesh;
        if (agentAvailable)
            currentAgentDestination = agent.destination;

        if (Vector3.Distance(currentAgentDestination, playerTarget.position) > 0.5f)
        {
            if (agentAvailable)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(playerTarget.position);
            SetAnimatorWalk(chaseSpeed);
            }
            else
            {
                // Fallback movement when agent not available/on-navmesh
                // Move transform directly toward player to avoid errors
                transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, chaseSpeed * Time.deltaTime);
                SetAnimatorWalk(chaseSpeed);
            }
        }
    }

    private void UpdateAttack()
    {
        if (playerTarget == null)
        {
            SetState(State.Patrol);
            return;
        }

        // Boss luôn xoay về phía player nhanh hơn, trừ khi đang khóa rotation trong lúc tấn công
        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            // unlock if timeout passed
            if (rotationLocked && Time.time > rotationLockEndTime)
            {
                rotationLocked = false;
            }

            if (!rotationLocked)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 15f);
            }
            else
            {
                // giữ rotation cố định trong lúc tấn công
                transform.rotation = lockedRotation;
            }
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // GÂY DAMAGE DỰA TRÊN ANIMATION STATE - Fix delay issue
        CheckAndDealDamageDuringAttack(distance);

        // Nếu player quá xa, chuyển về Chase
        if (distance > Mathf.Max(groundSlamRange, spinAttackRange))
        {
            if (Time.time - GetLastAttackTime() < 2f) // Đợi cooldown ngắn trước khi chase
            {
                SetAnimatorWalk(0f);
                return;
            }
            SetState(State.Chase);
            return;
        }

        // Nếu đang chuẩn bị đòn tấn công, không làm gì khác
        if (isPreparingAttack)
        {
            SetAnimatorWalk(0f);
            return;
        }

        // COMBO CONTINUATION LOGIC - Tiếp tục combo nếu đang trong chuỗi
        if (currentComboCount > 0 && currentComboCount < maxComboHits &&
            Time.time - lastComboHitTime >= comboHitDelay)
        {
            // Tiếp tục combo với hit tiếp theo
            currentComboCount++;
            string triggerName;

            // Map combo count to available animator triggers
            if (currentComboCount == 2)
                triggerName = "Hit2"; // Second hit uses Hit2 trigger
            else if (currentComboCount == 3)
                triggerName = "Hit"; // Third hit uses Hit trigger (reuse)
            else
                triggerName = "Hit"; // Fallback

            animator.SetTrigger(triggerName);
            lastComboHitTime = Time.time;

            if (showDebug) Debug.Log($"[GolemAI] Combo continued - Hit {currentComboCount} triggered ({triggerName})");

            // Reset combo nếu đạt max
            if (currentComboCount >= maxComboHits)
            {
                currentComboCount = 0;
                if (showDebug) Debug.Log("[GolemAI] Combo finished - Max hits reached");
            }

            SetAnimatorWalk(0f);
            return; // Đừng thực hiện attack mới khi đang combo
        }

        // ĐỪNG ATTACK NỮA nếu vừa attack xong và đang trong cooldown
        float timeSinceLastAttack = Time.time - GetLastAttackTime();
        float currentCooldown = GetAttackCooldown(lastAttackType);

        if (showDebug && timeSinceLastAttack < currentCooldown)
        {
            Debug.Log($"[GolemAI] IN COOLDOWN - lastAttack: {lastAttackType}, timeSinceLast: {timeSinceLastAttack:F1}s, cooldown: {currentCooldown:F1}s");
        }

        if (timeSinceLastAttack < currentCooldown)
        {
            SetAnimatorWalk(0f);
            return; // Đang trong cooldown, không attack
        }

        if (showDebug) Debug.Log($"[GolemAI] COOLDOWN PASSED - Ready to attack!");

        // Thực hiện tấn công theo pattern boss
        PerformBossAttack(distance);
    }

    private void UpdateRage()
    {
        // Debug current animator state every second
        if (showDebug && Mathf.FloorToInt(Time.time) % 1 == 0 && !hasLoggedRage)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[GolemAI] UpdateRage - time since start: {Time.time - rageStartTime:F1}s, Current State: {stateInfo.fullPathHash}, IsName('Rage'): {stateInfo.IsName("Rage")}, normalizedTime: {stateInfo.normalizedTime:F3}");
            hasLoggedRage = true;
        }
        if (Mathf.FloorToInt(Time.time) % 1 != 0) hasLoggedRage = false;
        if (playerTarget == null)
        {
            isRagePlaying = false;
            SetState(State.Patrol);
            return;
        }

        // Trigger Rage animation once when entering this state
        if (Time.time - rageStartTime < 0.2f && animator != null && !isRagePlaying)
        {
            isRagePlaying = true;

            // Try Play() instead of CrossFade for more reliable state transition
            animator.Play("Rage", 0, 0f);
            if (showDebug) Debug.Log($"[GolemAI] PLAYED Rage animation at time {Time.time:F1}");

            // Debug immediately after Play
            AnimatorStateInfo debugState = animator.GetCurrentAnimatorStateInfo(0);
            if (showDebug) Debug.Log($"[GolemAI] State after Play: {debugState.fullPathHash}, IsName('Rage'): {debugState.IsName("Rage")}, Speed: {debugState.speed}");

            // Reset triggers
            animator.ResetTrigger(idleActionTrigger);
            animator.ResetTrigger("Hit");
            animator.ResetTrigger("Hit2");
            animator.ResetTrigger("HeavyAttack");
            animator.ResetTrigger("SpinAttack");
            animator.ResetTrigger("GroundSlam");
            animator.ResetTrigger("RageSmash");
        }

        // Check if Rage animation has completed
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("Rage"))
            {
                float normalizedTime = stateInfo.normalizedTime;
                // Log progress every 0.5 seconds
                if (showDebug && (int)(Time.time * 2) % 2 == 0 && !hasLoggedRage)
                {
                    Debug.Log($"[GolemAI] Rage progress: {normalizedTime:F2} ({normalizedTime * 100:F0}%) - wait until 1.0f");
                    if (normalizedTime >= 0.8f)
                    {
                        Debug.Log($"[GolemAI] Rage NEARING completion: {normalizedTime:F3}");
                    }
                    hasLoggedRage = true;
                }
                if ((int)(Time.time * 2) % 2 != 0) hasLoggedRage = false;

                // Transition to Attack mode when animation is 100% complete - Boss ready to fight!
                if (normalizedTime >= 1.0f)
                {
                    isRagePlaying = false;
                    if (showDebug) Debug.Log($"[GolemAI] Rage animation 100% complete ({normalizedTime:F2}) - Boss ready to attack!");
                    SetState(State.Attack); // Boss enters attack mode instead of just chase
                    return;
                }
            }
            else
            {
                // Debug: Not in Rage state
                if (showDebug && Mathf.FloorToInt(Time.time) % 2 == 0 && !hasLoggedRage)
                {
                    Debug.Log($"[GolemAI] Not in Rage state - current state: {stateInfo.fullPathHash}, IsName('Rage'): {stateInfo.IsName("Rage")}");
                    hasLoggedRage = true;
                }
                if (Mathf.FloorToInt(Time.time) % 2 != 0) hasLoggedRage = false;
            }
        }
        // Wait longer for Rage animation (3 seconds minimum)
        if (Time.time - rageStartTime >= 3f && isRagePlaying)
        {
            isRagePlaying = false;
            if (showDebug) Debug.Log("[GolemAI] Rage timeout (3s) - force transitioning to Chase");
            SetState(State.Chase);
            return;
        }

        // Emergency fallback if Rage gets stuck for too long (5 seconds)
        if (Time.time - rageStartTime >= 5f && isRagePlaying)
        {
            isRagePlaying = false;
            if (showDebug) Debug.Log("[GolemAI] Rage stuck - emergency transition to Chase");
            SetState(State.Chase);
            return;
        }

        // During Rage, stay in place - don't move or set walk speed
        agent.isStopped = true;  // Stop agent movement during Rage animation
        SetAnimatorWalk(0f);     // Set walk to 0 to prevent speed affecting Rage
        if (showDebug && (int)(Time.time * 2) % 2 == 0 && !hasLoggedRage)
        {
            Debug.Log($"[GolemAI] During Rage: agent stopped, walk speed set to 0");
        }
    }

    private void PerformBossAttack(float distance)
    {
        if (animator == null) return;

        // Don't attack if Rage or IdleAction is currently playing
        if (isRagePlaying || isIdleActionPlaying)
        {
            if (showDebug) Debug.Log("[GolemAI] Attack blocked - Rage/IdleAction playing");
            return;
        }

        // Chọn loại tấn công dựa trên khoảng cách, HP và pattern
        AttackType selectedAttack = SelectBossAttack(distance);

        // Kiểm tra cooldown
        if (!CanPerformAttack(selectedAttack))
        {
            SetAnimatorWalk(0f);
            return;
        }

        // Thực hiện tấn công
        ExecuteBossAttack(selectedAttack);
    }

    private AttackType SelectBossAttack(float distance)
    {
        // Ưu tiên tấn công trong Rage mode
        if (currentState == State.Rage && Time.time - lastRageSmashTime >= rageSmashCooldown)
        {
            return AttackType.RageSmash;
        }

        // Chọn tấn công dựa trên khoảng cách
        System.Collections.Generic.List<AttackType> availableAttacks = new System.Collections.Generic.List<AttackType>();

        if (distance <= lightAttackRange) availableAttacks.Add(AttackType.LightAttack);
        if (distance <= heavyAttackRange) availableAttacks.Add(AttackType.HeavyAttack);
        if (distance <= comboAttackRange) availableAttacks.Add(AttackType.ComboAttack);
        if (distance <= spinAttackRange) availableAttacks.Add(AttackType.SpinAttack);
        if (distance <= groundSlamRange) availableAttacks.Add(AttackType.GroundSlam);

        if (availableAttacks.Count == 0) return AttackType.LightAttack;

        // Logic chọn tấn công thông minh dựa trên pattern và tình huống
        // NOTE: Combo continuation được xử lý trong UpdateAttack(), không phải ở đây
        // Logic này chỉ để chọn attack type ban đầu, không phải để tiếp tục combo

        // Chọn ngẫu nhiên dựa trên trọng số
        float random = Random.value;
        float cumulativeWeight = 0f;

        foreach (AttackType attack in availableAttacks)
        {
            int index = (int)attack;
            if (index < attackPatternWeights.Length)
            {
                cumulativeWeight += attackPatternWeights[index];
                if (random <= cumulativeWeight)
                {
                    return attack;
                }
            }
        }

        return availableAttacks[0]; // Fallback
    }

    private bool CanPerformAttack(AttackType attackType)
    {
        float lastAttackTime = GetLastAttackTime(attackType);
        float cooldown = GetAttackCooldown(attackType);

        return Time.time - lastAttackTime >= cooldown;
    }

    private float GetLastAttackTime(AttackType attackType = AttackType.LightAttack)
    {
        switch (attackType)
        {
            case AttackType.LightAttack: return lastLightAttackTime;
            case AttackType.HeavyAttack: return lastHeavyAttackTime;
            case AttackType.ComboAttack: return lastComboAttackTime;
            case AttackType.SpinAttack: return lastSpinAttackTime;
            case AttackType.GroundSlam: return lastGroundSlamTime;
            case AttackType.RageSmash: return lastRageSmashTime;
            default: return lastLightAttackTime;
        }
    }

    private float GetAttackCooldown(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.LightAttack: return lightAttackCooldown;
            case AttackType.HeavyAttack: return heavyAttackCooldown;
            case AttackType.ComboAttack: return comboAttackCooldown;
            case AttackType.SpinAttack: return spinAttackCooldown;
            case AttackType.GroundSlam: return groundSlamCooldown;
            case AttackType.RageSmash: return rageSmashCooldown;
            default: return lightAttackCooldown;
        }
    }

    private void ExecuteBossAttack(AttackType attackType)
    {
        lastAttackType = attackType;
        UpdateLastAttackTime(attackType);

        // Lock rotation toward player's current position at the start of the attack.
        if (playerTarget != null)
        {
            Vector3 dir = playerTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                lockedRotation = Quaternion.LookRotation(dir);
                rotationLocked = true;
                // safety timeout in seconds in case damage never fires
                rotationLockEndTime = Time.time + 5f;
            }
        }

        switch (attackType)
        {
            case AttackType.LightAttack:
                ExecuteLightAttack();
                break;
            case AttackType.HeavyAttack:
                StartCoroutine(ExecuteHeavyAttack());
                break;
            case AttackType.ComboAttack:
                ExecuteComboAttack();
                break;
            case AttackType.SpinAttack:
                ExecuteSpinAttack();
                break;
            case AttackType.GroundSlam:
                StartCoroutine(ExecuteGroundSlam());
                break;
            case AttackType.RageSmash:
                ExecuteRageSmash();
                break;
        }
    }

    private void UpdateLastAttackTime(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.LightAttack: lastLightAttackTime = Time.time; break;
            case AttackType.HeavyAttack: lastHeavyAttackTime = Time.time; break;
            case AttackType.ComboAttack: lastComboAttackTime = Time.time; break;
            case AttackType.SpinAttack: lastSpinAttackTime = Time.time; break;
            case AttackType.GroundSlam: lastGroundSlamTime = Time.time; break;
            case AttackType.RageSmash: lastRageSmashTime = Time.time; break;
        }
    }

    public void TakeDamage(float amount)
    {
        // This method is now for backward compatibility only
        // Actual damage handling is done by GolemDamageHandler
        // GolemAI only syncs health values for AI decision making

        if (golemDamageHandler != null)
        {
            // Sync health from GolemDamageHandler
            currentHealth = golemDamageHandler.CurrentHealth;
            maxHealth = golemDamageHandler.MaxHealth;
        }
        else if (healthBarSystem != null)
        {
            // Fallback sync from TakeDamageTest
            currentHealth = healthBarSystem.GetCurrentHealth();
            maxHealth = healthBarSystem.GetMaxHealth();
        }

        if (showDebug) Debug.Log($"[GolemAI] Health synced - Current: {currentHealth}, Max: {maxHealth}");
        float healthPercent = 0f;
        if (golemDamageHandler != null)
        {
            healthPercent = golemDamageHandler.HealthPercentage;
        }
        else if (healthBarSystem != null)
        {
            healthPercent = healthBarSystem.GetHealthPercentage();
        }
        else
        {
            healthPercent = currentHealth / maxHealth;
        }
        if (healthPercent <= rageThresholdPercent && currentState != State.Rage)
        {
            // Boss enters Rage mode - mạnh mẽ hơn
            if (showDebug) Debug.Log($"[GolemAI] Boss entering RAGE mode! HP: {healthPercent:P1}");

            // Tăng tốc độ di chuyển khi Rage
            chaseSpeed *= 1.3f;
            patrolSpeed *= 1.2f;

            // Giảm cooldown cho các đòn mạnh
            heavyAttackCooldown *= 0.8f;
            groundSlamCooldown *= 0.7f;
            spinAttackCooldown *= 0.8f;

            // Tăng damage
            heavyAttackDamage *= 1.2f;
            groundSlamDamage *= 1.3f;
            rageSmashDamage *= 1.4f;

            SetState(State.Rage);
            if (animator != null && !string.IsNullOrEmpty(rageTrigger))
            {
                animator.SetTrigger(rageTrigger);
            }
        }
        else if (currentState == State.Rage && healthPercent > rageThresholdPercent * 1.1f)
        {
            // Boss exits Rage mode khi hồi máu (nếu có)
            if (showDebug) Debug.Log("[GolemAI] Boss exiting RAGE mode");

            // Reset stats về bình thường
            chaseSpeed /= 1.3f;
            patrolSpeed /= 1.2f;
            heavyAttackCooldown /= 0.8f;
            groundSlamCooldown /= 0.7f;
            spinAttackCooldown /= 0.8f;
            heavyAttackDamage /= 1.2f;
            groundSlamDamage /= 1.3f;
            rageSmashDamage /= 1.4f;

            SetState(State.Chase);
        }

        // Boss có cơ hội counter attack khi bị đánh
        if (currentState != State.Rage && Random.value < 0.3f && CanPerformAttack(AttackType.LightAttack))
        {
            if (showDebug) Debug.Log("[GolemAI] Boss counter attacking!");
            ExecuteBossAttack(AttackType.LightAttack);
        }

        // Phase trigger moved to CheckHealthBasedTriggers()

        if (animator != null)
        {
            animator.SetTrigger("Damage");
            if (showDebug) Debug.Log($"[GolemAI] Triggered Damage animation - HP: {currentHealth:F0}/{maxHealth:F0}");
        }
    }

    private void Die()
    {
        SetState(State.Dead);
        agent.isStopped = true;
        if (animator != null && !string.IsNullOrEmpty(dieTrigger)) animator.SetTrigger(dieTrigger);
        if (agent != null) agent.enabled = false;
        if (showDebug) Debug.Log("[GolemAI] Died");
    }

    /// <summary>
    /// Public method to trigger Golem death from external damage handlers
    /// </summary>
    public void TriggerDeath()
    {
        if (currentState != State.Dead && currentHealth <= 0)
        {
            // Reset fire phase variables on death
            absorbedOrbsCount = 0;
            isFirePhaseActive = false;
            RemoveFireMaterial();

            Die();
        }
    }

    /// <summary>
    /// Public method to trigger Rage mode from damage handler
    /// </summary>
    public void TriggerRageMode()
    {
        if (currentState == State.Rage) return;

        // Boss enters Rage mode - mạnh mẽ hơn
        if (showDebug) Debug.Log($"[GolemAI] Boss entering RAGE mode from damage handler!");

        // Tăng tốc độ di chuyển khi Rage
        chaseSpeed *= 1.3f;
        patrolSpeed *= 1.2f;

        // Giảm cooldown cho các đòn mạnh
        heavyAttackCooldown *= 0.8f;
        groundSlamCooldown *= 0.7f;
        spinAttackCooldown *= 0.8f;

        // Tăng damage
        heavyAttackDamage *= 1.2f;
        groundSlamDamage *= 1.3f;
        rageSmashDamage *= 1.4f;

        SetState(State.Rage);
        if (animator != null && !string.IsNullOrEmpty(rageTrigger))
        {
            animator.SetTrigger(rageTrigger);
        }
    }

    /// <summary>
    /// Public method to trigger phase sequence from damage handler
    /// </summary>
    public void TriggerPhaseSequence()
    {
        if (phaseTriggered) return;

        phaseTriggered = true;
        StartCoroutine(PhaseSequence());
        if (showDebug) Debug.Log("[GolemAI] Phase sequence triggered from damage handler!");
    }

    /// <summary>
    /// Public property to access current state from damage handler
    /// </summary>
    public State CurrentState => currentState;

    // EnterRage removed — simple trigger used inline where needed

    // Override coroutine removed — override is applied inside WaitForAnimatorStateToFinish when state is entered

    private void SetState(State s)
    {
        if (currentState == s) return;
        if (showDebug) Debug.Log($"[GolemAI] State {currentState} -> {s} at Time: {Time.time:F2}");
        currentState = s;
        switch (s)
        {
            case State.Sleeping:
                agent.isStopped = true;
                isSleeping = true;
                isRagePlaying = false;
                isIdleActionPlaying = false;
                if (animator != null)
                {
                    // Ensure Walk parameter is 0 to prevent unwanted transitions
                    animator.SetFloat("Walk", 0f);
                    // CrossFade to Sleep state
                    animator.CrossFade("Sleep", 0.1f, 0);
                    if (showDebug) Debug.Log("[GolemAI] Entering sleep state - crossfading to Sleep");
                }
                break;
            case State.IdleAction:
                agent.isStopped = true;
                SetAnimatorWalk(0f);
                idleActionStartTime = Time.time;
                isRagePlaying = false;
                break;
            case State.Idle:
                agent.isStopped = true;
                break;
            case State.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                currentPatrolTarget = GetRandomPatrolPoint();
                agent.SetDestination(currentPatrolTarget);
                break;
            case State.Chase:
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = chaseSpeed;
                }
                break;
            case State.Attack:
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                // Lock rotation immediately when entering Attack state so boss doesn't track player mid-attack
                if (playerTarget != null)
                {
                    Vector3 dir = playerTarget.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                    {
                        lockedRotation = Quaternion.LookRotation(dir);
                        rotationLocked = true;
                        rotationLockEndTime = Time.time + 5f;
                    }
                }
                break;
            case State.Rage:
                // Don't set speed/isStopped here - let UpdateRage handle it to prevent jerking
                // agent.isStopped = false;  // ← Commented out
                // agent.speed = chaseSpeed * 1.5f;  // ← Commented out
                SetAnimatorWalk(0f);  // ensure walk parameter is zero so Walk animation doesn't blend with Rage
                rageStartTime = Time.time;
                isIdleActionPlaying = false;
                rageTransitionScheduled = false; // Reset transition flag
                idleActionTransitionScheduled = false; // Reset IdleAction flag
                break;
            case State.Dead:
                agent.isStopped = true;
                break;
        }
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector3 rnd = Random.insideUnitSphere * patrolRadius;
        rnd += patrolCenter;
        rnd.y = patrolCenter.y;
        return rnd;
    }

    private IEnumerator PerformLeap()
    {
        if (playerTarget == null) yield break;
        // Ensure walk param is zero and force jump animation to start cleanly
        SetAnimatorWalk(0f);
        if (animator != null)
        {
            animator.ResetTrigger(wakeUpTrigger);
            animator.Play("Jump", 0, 0f);
        }
        if (showDebug) Debug.Log("[GolemAI] Performing leap (Jump)");

        // Prepare landing position (sample NavMesh near player)
        Vector3 desiredLanding = playerTarget.position;
        Vector3 landingPos = desiredLanding;
        UnityEngine.AI.NavMeshHit hit;
        if (agent != null && UnityEngine.AI.NavMesh.SamplePosition(desiredLanding, out hit, leapLandingSampleRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            landingPos = hit.position;
        }

        // Spawn landing indicator if provided
        if (landingIndicatorPrefab != null)
        {
            var indicator = Instantiate(landingIndicatorPrefab, landingPos, Quaternion.identity);
            Destroy(indicator, landingIndicatorDuration);
        }
        else
        {
            // TODO: Implement runtime landing indicator or assign landingIndicatorPrefab
            Debug.LogWarning("[GolemAI] No landingIndicatorPrefab assigned - landing indicator will not be shown");
        }

        // Smoothly move in an arc from start to landingPos over the animation duration
        // Use phase-specific parameters if in phase, otherwise use regular leap parameters
        float duration = Mathf.Max(0.01f, inPhase ? phaseLeapDuration : leapDuration);
        Vector3 startPos = transform.position;
        Vector3 endPos = landingPos;
        float distance = Vector3.Distance(startPos, endPos);

        // Calculate height: phase leap uses simple multiplier, regular leap uses full logic
        float height;
        if (inPhase)
        {
            // Phase leap: simple multiplier applied to regular leap height
            float regularHeight = Mathf.Clamp(distance * leapArcHeightMultiplier, leapArcMinHeight, leapArcMaxHeight);
            height = regularHeight * phaseLeapHeightMultiplier;
        }
        else
        {
            // Regular leap: use full min/max clamping
            height = Mathf.Clamp(distance * leapArcHeightMultiplier, leapArcMinHeight, leapArcMaxHeight);
        }

        if (showDebug)
        {
            Debug.Log($"[GolemAI] Leap - Phase: {inPhase}, Duration: {duration}, Height: {height}, Distance: {distance}");
        }

        // Disable agent movement during manual interpolation only if it was enabled
        bool agentWasEnabled = agent != null && agent.enabled;
        if (agent != null && agentWasEnabled) agent.enabled = false;

        float totalDistance = Vector3.Distance(startPos, endPos);
        if (totalDistance < 0.5f)
        {
            // Too close — do a small jump animation instead of full interpolation
            if (showDebug) Debug.Log("[GolemAI] PerformLeap: start and end very close, doing small jump");
            float smallDur = Mathf.Min(duration, 0.4f);
            float t = 0f;
            while (t < smallDur)
        {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / smallDur);
                Vector3 basePos = Vector3.Lerp(startPos, endPos, p);
                float arc = Mathf.Sin(p * Mathf.PI) * Mathf.Min(height, 1.5f);
                transform.position = basePos + Vector3.up * arc;
                yield return null;
            }
            transform.position = endPos;
        }
        else
        {
            // Interpolate along an arc
            float elapsed = 0f;
            float debugInterval = 0.25f;
            float nextDebug = debugInterval;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);
                Vector3 basePos = Vector3.Lerp(startPos, endPos, p);
                float arc = Mathf.Sin(p * Mathf.PI) * height;
                transform.position = basePos + Vector3.up * arc;
                if (showDebug && elapsed >= nextDebug)
                {
                    Debug.Log($"[GolemAI] PerformLeap progress {p:F2} pos={transform.position}");
                    nextDebug += debugInterval;
                }
                yield return null;
            }
            // Ensure final position
            transform.position = endPos;
        }

        // Re-enable agent if it was enabled before and restore navigation
        if (agent != null && agentWasEnabled)
        {
            agent.enabled = true;
            // Ensure agent is on NavMesh; if not, try to sample nearest
            if (!agent.isOnNavMesh)
            {
                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(endPos, out navHit, leapLandingSampleRadius * 2f, UnityEngine.AI.NavMesh.AllAreas))
                    agent.Warp(navHit.position);
            }
            if (agent.isOnNavMesh)
            {
                try
                {
            agent.isStopped = false;
                    agent.speed = chaseSpeed * 1.2f;
                    agent.SetDestination(endPos);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[GolemAI] SetDestination failed after leap: {ex.Message}");
                }
            }
        }

        // Ensure animator land transition and reset walk parameter
        if (animator != null)
        {
            animator.SetTrigger("Land");
            // small delay to let Land state start, then force Idle to avoid Walk blending/stuck
            yield return new WaitForSeconds(0.05f);
            animator.Play("Idle", 0, 0f);
            // reset common animator params & triggers to avoid stuck blending
            SetAnimatorWalk(0f);
            animator.speed = 1f;
            animator.Update(0f);
            animator.Rebind();
        }

        // Spawn ice spikes on landing if assigned; otherwise log hint
        if (iceSpikePrefab != null)
        {
            var spikes = Instantiate(iceSpikePrefab, endPos - Vector3.up * iceSpikeSpawnDepth, Quaternion.identity);
            // Ensure spikes are upright and sit initially below ground
            spikes.transform.position = new Vector3(endPos.x, endPos.y - iceSpikeSpawnDepth, endPos.z);
            // Apply scale multipliers (radius for X/Z, height for Y)
            Vector3 baseScale = spikes.transform.localScale;
            Vector3 applied = new Vector3(baseScale.x * Mathf.Max(0.01f, iceSpikeRadiusMultiplier),
                                          baseScale.y * Mathf.Max(0.01f, iceSpikeHeightMultiplier),
                                          baseScale.z * Mathf.Max(0.01f, iceSpikeRadiusMultiplier));
            spikes.transform.localScale = applied;
            // Apply material if provided
            if (iceSpikeMaterial != null)
            {
                var rends = spikes.GetComponentsInChildren<Renderer>();
                foreach (var r in rends) r.material = iceSpikeMaterial;
            }

            // Add DamageDealer component to make ice spikes damage player
            var damageDealer = spikes.AddComponent<DamageDealer>();

            // Use reflection to set private fields
            var targetLayerField = typeof(DamageDealer).GetField("targetLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var weaponDamageField = typeof(DamageDealer).GetField("weaponDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hitRadiusField = typeof(DamageDealer).GetField("hitRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var weaponLengthField = typeof(DamageDealer).GetField("weaponLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (targetLayerField != null) targetLayerField.SetValue(damageDealer, (LayerMask)LayerMask.GetMask("Player"));
            float landingDamage = landingSpikeFixedDamage > 0 ? landingSpikeFixedDamage : groundSlamDamage * landingSpikeDamageMultiplier;
            if (weaponDamageField != null) weaponDamageField.SetValue(damageDealer, landingDamage);
            if (hitRadiusField != null) hitRadiusField.SetValue(damageDealer, landingSpikeHitRadius);
            if (weaponLengthField != null) weaponLengthField.SetValue(damageDealer, landingSpikeWeaponLength);

            // Animate rise then destroy
            StartCoroutine(RiseAndDestroy(spikes, endPos, iceSpikeRiseDuration, iceSpikeDuration));
        }
        else
        {
            if (showDebug) Debug.Log("[GolemAI] No iceSpikePrefab assigned - skipping spikes spawn (assign IceSpikes2.fbx prefab)");
        }

        // Ensure Idle animation after landing for clarity
        if (animator != null)
        {
            animator.CrossFade("Idle", 0.1f, 0);
        }

        if (showDebug) Debug.Log("[GolemAI] Leap finished - landed and resumed");
    }

    // Removed PerformSleep method - Golem now starts in Sleeping state directly
    
    private IEnumerator WaitForAnimatorStateToFinish(int stateHash, float timeoutSeconds)
    {
        if (animator == null) yield break;
        isRagePlaying = true;
        float start = Time.time;

        // No longer waiting for animator state — simplified behaviour
        yield break;
    }
    private void SetAnimatorWalk(float speed)
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(walkParameter))
        {
            animator.SetFloat(walkParameter, speed);
        }
    }

    // Boss Attack Execution Methods
    private void ExecuteLightAttack()
    {
        if (currentComboCount > 0)
        {
            // Tiếp tục combo
            currentComboCount++;
            string triggerName;

            // Map combo count to available animator triggers
            if (currentComboCount == 2)
                triggerName = "Hit2"; // Second hit uses Hit2 trigger
            else if (currentComboCount == 3)
                triggerName = "Hit"; // Third hit uses Hit trigger (reuse)
            else
                triggerName = "Hit"; // Fallback

            animator.SetTrigger(triggerName);
            lastComboHitTime = Time.time;

            if (showDebug) Debug.Log($"[GolemAI] Combo hit {currentComboCount} triggered ({triggerName})");

            // Reset combo nếu đạt max
            if (currentComboCount >= maxComboHits)
            {
                currentComboCount = 0;
            }
        }
        else
        {
            // Đòn đánh nhẹ thông thường
            string trig = "Hit"; // Simplified for now
            animator.SetTrigger(trig);
            if (showDebug) Debug.Log($"[GolemAI] Light Attack triggered ({trig})");
        }
    }

    private System.Collections.IEnumerator ExecuteHeavyAttack()
    {
        isPreparingAttack = true;

        // Thời gian chuẩn bị - có thể thêm animation chuẩn bị
        yield return new WaitForSeconds(heavyAttackPrepTime);

        // SỬ DỤNG Hit animation cho Heavy Attack (vì Animator không có HeavyAttack state)
        animator.SetTrigger("Hit");
        isPreparingAttack = false;

        if (showDebug) Debug.Log("[GolemAI] Heavy Attack executed after preparation (using Hit animation)");
    }

    private void ExecuteComboAttack()
    {
        currentComboCount = 1;
        lastComboHitTime = Time.time;
        animator.SetTrigger("Hit"); // Start combo with Hit (Hit1)

        if (showDebug) Debug.Log("[GolemAI] Combo Attack started - Hit 1 (using Hit trigger)");
    }

    private void ExecuteSpinAttack()
    {
        // SỬ DỤNG Hit2 animation cho Spin Attack (vì Animator không có SpinAttack state)
        animator.SetTrigger("Hit2");
        if (showDebug) Debug.Log("[GolemAI] Spin Attack executed (using Hit2 animation)");
    }

    private System.Collections.IEnumerator ExecuteGroundSlam()
    {
        isPreparingAttack = true;

        // Thời gian chuẩn bị - Golem nâng tay lên
        yield return new WaitForSeconds(groundSlamPrepTime);

        // SỬ DỤNG Hit animation cho Ground Slam (vì Animator không có GroundSlam state)
        animator.SetTrigger("Hit");
        isPreparingAttack = false;

        if (showDebug) Debug.Log("[GolemAI] Ground Slam executed after preparation (using Hit animation)");
    }

    private void ExecuteRageSmash()
    {
        animator.SetTrigger("RageSmash");
        if (showDebug) Debug.Log("[GolemAI] Rage Smash executed - Special attack!");
    }

    // DAMAGE DEALING LOGIC - Gây damage dựa trên animation state
    private void CheckAndDealDamageDuringAttack(float distance)
    {
        if (animator == null || playerTarget == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        // normalizedTime may grow beyond 1 for looping clips; use progressInState for 0..1 range
        float progressInState = stateInfo.normalizedTime % 1f;
        float normalizedTime = stateInfo.normalizedTime;

        // Reset per-state damage flag when animation state changes
        int currentStateHash = stateInfo.fullPathHash;
        if (currentStateHash != damageStateHash)
        {
            damageStateHash = currentStateHash;
            damageDealtInState = false;
            // reset logging rate limiter for damage to allow logs for new state
            hasLoggedDamage = false;
        }

        // DEBUG: Log animation state mỗi giây
        if (showDebug && Mathf.FloorToInt(Time.time) % 1 == 0 && !hasLoggedDamage)
        {
            string stateName = stateInfo.IsName("Hit") ? "Hit" : stateInfo.IsName("Hit2") ? "Hit2" : stateInfo.IsName("Rage") ? "Rage" : "Other";
            Debug.Log($"[GolemAI] Animation State: '{stateName}', normalizedTime: {normalizedTime:F3}");
            hasLoggedDamage = true;
        }
        if (Mathf.FloorToInt(Time.time) % 1 != 0) hasLoggedDamage = false;

        // Chỉ gây damage khi đang trong attack animation THỰC SỰ TỒN TẠI trong Animator
        if (stateInfo.IsName("Hit") || stateInfo.IsName("Hit2") || stateInfo.IsName("Rage"))
        {
            // DEBUG: Log khi animation đạt 90%
            if (showDebug && progressInState >= 0.9f && progressInState <= 0.95f && !hasLoggedDamage)
            {
                Debug.Log($"[GolemAI] Animation reached 90% - About to deal damage!");
                hasLoggedDamage = true; // Prevent spam
            }

            // ĐẢM BẢO animation đã bắt đầu (normalizedTime > 0.1f) để tránh damage trước khi animation chạy
            // Gây damage tại thời điểm CUỐI animation (90-100%) để đồng bộ hoàn hảo
            if (normalizedTime > 0.1f && progressInState >= 0.9f && progressInState <= 1.0f && !damageDealtInState)
            {
                // Kiểm tra player có trong range không
                if (distance <= GetAttackRange(lastAttackType))
                {
                    // Gây damage theo loại attack
                    float damage = GetAttackDamage(lastAttackType);

                    // Golem gây damage cho player trực tiếp (không phụ thuộc vào healthBarSystem)
                    PlayerHealth playerHealth = playerTarget.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        // Tính toán hit position (tâm của Golem)
                        Vector3 hitPosition = transform.position + Vector3.up * 1.5f; // Chest level
                        playerHealth.TakeDamage(damage, hitPosition);
                        damageDealtInState = true;
                        // Unlock rotation immediately after dealing damage so boss can react/move again
                        rotationLocked = false;
                        if (showDebug) Debug.Log($"[GolemAI] Dealt {damage} damage to player with {lastAttackType} at animation progress {progressInState:F2} (rotation unlocked)");
                    }
                }
            }
        }
    }

    // Helper methods cho damage system
    private float GetAttackDamage(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.LightAttack: return lightAttackDamage;
            case AttackType.HeavyAttack: return heavyAttackDamage;
            case AttackType.ComboAttack: return comboAttackDamage;
            case AttackType.SpinAttack: return spinAttackDamage;
            case AttackType.GroundSlam: return groundSlamDamage;
            case AttackType.RageSmash: return rageSmashDamage;
            default: return lightAttackDamage;
        }
    }

    private float GetAttackRange(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.LightAttack: return lightAttackRange;
            case AttackType.HeavyAttack: return heavyAttackRange;
            case AttackType.ComboAttack: return comboAttackRange;
            case AttackType.SpinAttack: return spinAttackRange;
            case AttackType.GroundSlam: return groundSlamRange;
            case AttackType.RageSmash: return groundSlamRange; // Rage smash uses same range as ground slam
            default: return lightAttackRange;
        }
    }


    // Phase sequence coroutine
    [ContextMenu("Run Phase Sequence")]
    public void TriggerPhaseSequenceContext() { StartCoroutine(PhaseSequence()); }

    [ContextMenu("Test Fire Phase")]
    public void TestFirePhase() { StartCoroutine(ActivateFirePhase()); }

    /// <summary>
    /// Activates fire phase with burning material effect
    /// </summary>
    private IEnumerator ActivateFirePhase()
    {
        if (isFirePhaseActive) yield break;

        isFirePhaseActive = true;
        if (showDebug) Debug.Log($"[GolemAI] 🔥 FIRE PHASE ACTIVATED! 🔥");

        // Apply fire material to golem model
        ApplyFireMaterial();

        // Start animating fire intensity on shader (if shader supports _FireIntensity)
        if (golemRenderers != null && golemRenderers.Length > 0)
        {
            StartCoroutine(AnimateFireIntensity(1f, firePhaseDuration));
        }

        // Boost stats during fire phase
        float originalChaseSpeed = chaseSpeed;
        float originalPatrolSpeed = patrolSpeed;
        float originalHeavyDamage = heavyAttackDamage;
        float originalGroundDamage = groundSlamDamage;
        float originalRageDamage = rageSmashDamage;

        chaseSpeed *= firePhaseSpeedMultiplier;
        patrolSpeed *= firePhaseSpeedMultiplier;
        heavyAttackDamage *= firePhaseDamageMultiplier;
        groundSlamDamage *= firePhaseDamageMultiplier;
        rageSmashDamage *= firePhaseDamageMultiplier;

        // Fire phase duration
        yield return new WaitForSeconds(firePhaseDuration);

        // Restore original stats
        chaseSpeed = originalChaseSpeed;
        patrolSpeed = originalPatrolSpeed;
        heavyAttackDamage = originalHeavyDamage;
        groundSlamDamage = originalGroundDamage;
        rageSmashDamage = originalRageDamage;

        // Remove fire material
        RemoveFireMaterial();

        isFirePhaseActive = false;
        if (showDebug) Debug.Log($"[GolemAI] Fire phase ended");
    }

    /// <summary>
    /// Applies fire material to all golem renderers
    /// </summary>
    private void ApplyFireMaterial()
    {
        if (firePhaseMaterial == null)
        {
            if (showDebug) Debug.LogWarning("[GolemAI] Fire phase material not assigned!");
            return;
        }

        // Find all renderers in golem model (skip weapon renderers if needed)
        golemRenderers = GetComponentsInChildren<Renderer>()
            .Where(r => !r.gameObject.CompareTag("Weapon") && !r.gameObject.CompareTag("Effect"))
            .ToArray();

        // Store original materials and apply material system
        originalMaterials = new Material[golemRenderers.Length];
        for (int i = 0; i < golemRenderers.Length; i++)
        {
            var renderer = golemRenderers[i];
            if (renderer != null)
            {
                // Store original materials
                originalMaterials[i] = renderer.sharedMaterial;

                if (useDualMaterialSystem)
                {
                    // Apply dual materials: original + fire material
                    renderer.materials = new Material[] { renderer.sharedMaterial, firePhaseMaterial };
                }
                else
                {
                    // Replace material completely
                    renderer.material = firePhaseMaterial;
                }
            }
        }

        if (showDebug) Debug.Log($"[GolemAI] Applied {(useDualMaterialSystem ? "dual materials (original + fire)" : "single fire material")} to {golemRenderers.Length} renderers");
    }

    /// <summary>
    /// Removes fire material and restores original materials
    /// </summary>
    private void RemoveFireMaterial()
    {
        if (golemRenderers == null || originalMaterials == null) return;

        for (int i = 0; i < golemRenderers.Length; i++)
        {
            var renderer = golemRenderers[i];
            if (renderer != null && i < originalMaterials.Length)
            {
                // Restore original material
                renderer.material = originalMaterials[i];
            }
        }

        if (showDebug) Debug.Log($"[GolemAI] Removed fire material from {golemRenderers.Length} renderers");
    }

    /// <summary>
    /// Animate shader _FireIntensity on all golem renderers using MaterialPropertyBlock.
    /// This avoids creating material instances and is efficient at runtime.
    /// </summary>
    private IEnumerator AnimateFireIntensity(float peakIntensity, float duration)
    {
        float elapsed = 0f;
        var mpb = new MaterialPropertyBlock();

        // set initial intensity
        foreach (var r in golemRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetFloat("_FireIntensity", peakIntensity);
            r.SetPropertyBlock(mpb);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float intensity = Mathf.Lerp(peakIntensity, 0f, t);

            foreach (var r in golemRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetFloat("_FireIntensity", intensity);
                r.SetPropertyBlock(mpb);
            }

            yield return null;
        }

        // ensure final = 0
        foreach (var r in golemRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetFloat("_FireIntensity", 0f);
            r.SetPropertyBlock(mpb);
        }
    }

    private IEnumerator PhaseSequence()
    {
        inPhase = true;
        // 1. Jump -> perform leap movement then Idle
        // Use PerformLeap coroutine so Golem actually travels to the landing position (towards player)
        if (playerTarget != null)
        {
            yield return StartCoroutine(PerformLeap());
        }
        else
        {
            // fallback: just play jump then idle (use phase duration if in phase)
            if (animator != null) animator.SetTrigger("Jump");
            float jumpDuration = inPhase ? phaseLeapDuration : leapDuration;
            yield return new WaitForSeconds(Mathf.Max(0.1f, jumpDuration));
            if (animator != null) animator.CrossFade("Idle", 0.1f, 0);
        }

        // Wait 2 seconds after landing
        yield return new WaitForSeconds(2f);

        // 2. Rage -> face player
        SetState(State.Rage);
        if (playerTarget != null)
        {
            Vector3 dir = playerTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        }
        if (animator != null)
        {
            // Force normal playback speed and play Rage from start to avoid speedups
            animator.speed = 1f;
            animator.Play("Rage", 0, 0f);
        }
        // wait for the configured rage animation duration
        yield return new WaitForSeconds(Mathf.Max(0.1f, rageAnimationDuration));

        // 3. Sleep -> spawn orbiting orbs under ground, rise up to orbit height, orbit, then collapse into golem
        if (animator != null)
        {
            animator.speed = 1f; // Reset speed to normal to prevent fast playback
            animator.SetTrigger("SleepStart");
        }

        // Wait for Sleep animation to start playing
        yield return new WaitForSeconds(0.5f);

        // create orbit parent at golem position (we'll move children to orbit height)
        GameObject orbitParent = new GameObject("OrbitParent");
        orbitParent.transform.position = transform.position;
        GameObject[] orbs = new GameObject[orbitingOrbCount];

        // spawn orbs below ground around golem
        for (int i = 0; i < orbitingOrbCount; i++)
        {
            float ang = (360f / orbitingOrbCount) * i * Mathf.Deg2Rad;
            Vector3 basePos = transform.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * orbitRadius;
            Vector3 spawnPos = basePos - Vector3.up * orbSpawnDepth;
            GameObject orb = orbitingOrbPrefab != null ? Instantiate(orbitingOrbPrefab, spawnPos, Quaternion.identity) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.localScale = Vector3.one * 0.1f; // start small
            var c = orb.GetComponent<Collider>(); if (c) Destroy(c);
            orb.transform.parent = orbitParent.transform;
            orbs[i] = orb;
            // apply orbiting orb material if provided
            if (orbitingOrbMaterial != null)
            {
                var rends = orb.GetComponentsInChildren<Renderer>();
                foreach (var r in rends) r.material = orbitingOrbMaterial;
            }
        }

        // rise all orbs to orbit height (simultaneous)
        float riseElapsed = 0f;
        while (riseElapsed < orbRiseDuration)
        {
            riseElapsed += Time.deltaTime;
            float p = Mathf.Clamp01(riseElapsed / orbRiseDuration);
            for (int i = 0; i < orbitingOrbCount; i++)
            {
                if (orbs[i] == null) continue;
                float ang = (360f / orbitingOrbCount) * i * Mathf.Deg2Rad;
                Vector3 orbitPos = transform.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * orbitRadius + Vector3.up * orbOrbitHeight;
                Vector3 startPos = transform.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * orbitRadius - Vector3.up * orbSpawnDepth;
                orbs[i].transform.position = Vector3.Lerp(startPos, orbitPos, p);
                orbs[i].transform.localScale = Vector3.Lerp(Vector3.one * 0.1f, Vector3.one * orbFinalScale, p);
            }
            yield return null;
        }

        // parent position should be at orbit height for rotation
        orbitParent.transform.position = transform.position + Vector3.up * orbOrbitHeight;

        // orbit for orbOrbitDuration
        float orbitElapsed = 0f;
        while (orbitElapsed < orbOrbitDuration)
        {
            orbitElapsed += Time.deltaTime;
            orbitParent.transform.Rotate(Vector3.up, orbRotationSpeed * Time.deltaTime, Space.World);
            yield return null;
        }

        // collapse inward into golem center
        float collapseElapsed = 0f;
        while (collapseElapsed < orbCollapseTime)
        {
            collapseElapsed += Time.deltaTime;
            float p = Mathf.Clamp01(collapseElapsed / orbCollapseTime);
            for (int i = 0; i < orbitingOrbCount; i++)
            {
                if (orbs[i] == null) continue;
                Vector3 current = orbs[i].transform.position;
                Vector3 target = transform.position;
                orbs[i].transform.position = Vector3.Lerp(current, target, p);
                orbs[i].transform.localScale = Vector3.Lerp(orbs[i].transform.localScale, Vector3.zero, p);
            }
            yield return null;
        }

        // Count absorbed orbs and check for fire phase trigger
        absorbedOrbsCount += orbitingOrbCount; // All orbs absorbed at once
        if (showDebug) Debug.Log($"[GolemAI] All {orbitingOrbCount} orbs absorbed! Total absorbed: {absorbedOrbsCount}");

        // Trigger fire phase when required orbs are absorbed
        if (absorbedOrbsCount >= orbsRequiredForFire && !isFirePhaseActive)
        {
            StartCoroutine(ActivateFirePhase());
        }

        // attach model VFX to indicate increased damage/effect
        GameObject modelVFX = null;
        if (modelVFXPrefab != null)
        {
            modelVFX = Instantiate(modelVFXPrefab, transform.position, Quaternion.identity);
            modelVFX.transform.parent = transform;
            Destroy(modelVFX, modelVFXDuration);
        }

        // cleanup orbs and parent
        foreach (var o in orbs) if (o != null) Destroy(o);
        Destroy(orbitParent);

        // only after collapse attach, end sleep
        yield return new WaitForSeconds(1.0f); // Give more time for Sleep animation to complete
        if (animator != null) animator.SetTrigger("SleepEnd");
        // small pause before next sequence step
        yield return new WaitForSeconds(0.2f);

        // 4a. Idle state after SleepEnd
        SetState(State.Idle);
        if (animator != null)
        {
            animator.speed = 1f;
            animator.CrossFade("Idle", 0.1f, 0);
        }
        yield return new WaitForSeconds(1f); // Brief Idle state

        // 4b. Then IdleAction after orbs phase
        SetState(State.IdleAction);
        idleActionStartTime = Time.time;
        isRagePlaying = false;

        // Force animator to IdleAction state in phase sequence ONLY
        // This ensures IdleAction plays correctly during phase without interfering with normal behavior
        if (inPhase && animator != null)
        {
            animator.speed = 1f;
            // Force play IdleAction from start
            animator.Play(idleActionTrigger, 0, 0f);
            if (showDebug) Debug.Log($"[GolemAI] Phase: Forced animator to play '{idleActionTrigger}' animation");
        }

        if (showDebug) Debug.Log("[GolemAI] Phase: Starting IdleAction after orbs collapse");

        // Wait for IdleAction animation (don't let UpdateIdleAction auto-transition during phase)
        // We manually control the transition in phase sequence
        yield return new WaitForSeconds(2f); // Wait for idleAction duration

        if (showDebug) Debug.Log("[GolemAI] Phase: IdleAction completed, starting second Rage");

        // 5. Rage again after IdleAction
        SetState(State.Rage);
        if (playerTarget != null)
        {
            Vector3 dir = playerTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        }
        if (animator != null)
        {
            // Force normal playback speed and play Rage from start to avoid speedups
            animator.speed = 1f;
            animator.Play("Rage", 0, 0f);
        }
        // wait for the configured rage animation duration
        yield return new WaitForSeconds(Mathf.Max(0.1f, rageAnimationDuration));

        // 6. Sleep facing player then fire 6-line projectiles
        if (playerTarget != null)
        {
            Vector3 dir = (playerTarget.position - transform.position).normalized;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        }
        if (animator != null)
        {
            animator.speed = 1f; // Reset speed to normal to prevent fast playback
            animator.SetTrigger("SleepStart");
        }
        yield return new WaitForSeconds(0.8f); // Give more time for Sleep animation
        int rays = phaseAttackRayCount;
        // Compute base angle so the first ray can point to the player if requested
        float angleStepDeg = (rays > 0) ? (360f / rays) : 360f;
        float baseAngleDeg = 0f;
        if (phaseFirstLineToPlayer && playerTarget != null)
        {
            Vector3 toPlayer = (playerTarget.position - transform.position);
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                // Atan2(z,x) gives angle relative to +X axis which matches our cos/sin usage below
                baseAngleDeg = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg;
            }
        }

        // Spawn ground line indicators before firing projectiles
        GameObject[] groundLines = new GameObject[rays];
        for (int i = 0; i < rays; i++)
        {
            // Calculate angle for each ray (evenly distributed in 360 degrees) offset by baseAngleDeg
            float angleDeg = baseAngleDeg + i * angleStepDeg;
            float angle = angleDeg * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            // create flat cube as ground line extending from golem position outward
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GroundLineIndicator";
            var c = line.GetComponent<Collider>(); if (c) Destroy(c);

            // Position: start at golem position and extend outward
            Vector3 lineStart = transform.position;
            lineStart.y = transform.position.y + 0.02f;
            line.transform.position = lineStart + direction * (phaseAttackGroundLineLength * 0.5f);
            line.transform.rotation = Quaternion.LookRotation(direction);
            line.transform.localScale = new Vector3(phaseAttackGroundLineWidth, 0.02f, phaseAttackGroundLineLength);
            if (groundLineMaterial != null)
            {
                var rends = line.GetComponentsInChildren<Renderer>();
                foreach (var r in rends) r.material = groundLineMaterial;
            }
            groundLines[i] = line;
        }

        // wait for indicators to show
        yield return new WaitForSeconds(phaseAttackGroundLineDuration);

        // cleanup ground lines
        for (int i = 0; i < rays; i++)
        {
            if (groundLines[i] != null) Destroy(groundLines[i]);
        }

        for (int i = 0; i < rays; i++)
        {
            // Calculate angle for each ray (evenly distributed in 360 degrees) offset by baseAngleDeg
            float angleDeg = baseAngleDeg + i * angleStepDeg;
            float angle = angleDeg * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            // Spawn multiple spikes along each ray, starting from near the golem
            for (int j = 0; j < phaseAttackSpikesPerRay; j++)
            {
                float distanceAlongRay = phaseAttackSpikeMinDistance + j * phaseAttackRaySpikeSpacing;
                Vector3 spikePos = transform.position + direction * distanceAlongRay;

                // spawn spike prefab under ground and rise
                if (phaseIceSpikePrefab != null)
                {
                    GameObject spike = Instantiate(phaseIceSpikePrefab, spikePos - Vector3.up * phaseIceSpikeSpawnDepth, Quaternion.identity);
                    spike.transform.position = spikePos - Vector3.up * phaseIceSpikeSpawnDepth;
                    // apply scale and material for phase attack spikes (more dramatic than regular phase spikes)
                    Vector3 baseScale = spike.transform.localScale;
                    Vector3 applied = new Vector3(baseScale.x * Mathf.Max(0.01f, phaseAttackIceSpikeRadiusMultiplier),
                                                  baseScale.y * Mathf.Max(0.01f, phaseAttackIceSpikeHeightMultiplier),
                                                  baseScale.z * Mathf.Max(0.01f, phaseAttackIceSpikeRadiusMultiplier));
                    spike.transform.localScale = applied;
                    if (phaseIceSpikeMaterial != null)
                    {
                        var rends = spike.GetComponentsInChildren<Renderer>();
                        foreach (var r in rends) r.material = phaseIceSpikeMaterial;
                    }

                    // Add DamageDealer component to phase ice spikes (higher damage)
                    var phaseDamageDealer = spike.AddComponent<DamageDealer>();

                    // Use reflection to set private fields for phase damage dealer
                    var phaseTargetLayerField = typeof(DamageDealer).GetField("targetLayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var phaseWeaponDamageField = typeof(DamageDealer).GetField("weaponDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var phaseHitRadiusField = typeof(DamageDealer).GetField("hitRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var phaseWeaponLengthField = typeof(DamageDealer).GetField("weaponLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (phaseTargetLayerField != null) phaseTargetLayerField.SetValue(phaseDamageDealer, (LayerMask)LayerMask.GetMask("Player"));
                    float phaseDamage = phaseSpikeFixedDamage > 0 ? phaseSpikeFixedDamage : rageSmashDamage * phaseSpikeDamageMultiplier;
                    if (phaseWeaponDamageField != null) phaseWeaponDamageField.SetValue(phaseDamageDealer, phaseDamage);
                    if (phaseHitRadiusField != null) phaseHitRadiusField.SetValue(phaseDamageDealer, phaseSpikeHitRadius);
                    if (phaseWeaponLengthField != null) phaseWeaponLengthField.SetValue(phaseDamageDealer, phaseSpikeWeaponLength);

                    StartCoroutine(RiseAndDestroy(spike, spikePos, phaseAttackIceSpikeRiseDuration, phaseAttackIceSpikeDuration));
                }
                else
                {
                    // fallback: create a thin cylinder spike
                    GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    var col = spike.GetComponent<Collider>(); if (col) Destroy(col);
                    spike.transform.localScale = new Vector3(0.5f * phaseAttackIceSpikeRadiusMultiplier, 1f * phaseAttackIceSpikeHeightMultiplier, 0.5f * phaseAttackIceSpikeRadiusMultiplier);
                    spike.transform.position = spikePos - Vector3.up * phaseIceSpikeSpawnDepth;
                    spike.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    if (phaseIceSpikeMaterial != null)
                    {
                        var rends = spike.GetComponentsInChildren<Renderer>();
                        foreach (var r in rends) r.material = phaseIceSpikeMaterial;
                    }
                    StartCoroutine(RiseAndDestroy(spike, spikePos, phaseAttackIceSpikeRiseDuration, phaseAttackIceSpikeDuration));
                }
            }
        }

        if (animator != null) animator.SetTrigger("SleepEnd");
        inPhase = false;
        yield return null;
    }
    

    // Hiển thị Gizmos khi SELECT object (chi tiết hơn)
    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        // Vẽ text labels cho ranges (chỉ trong Editor)
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "Golem Attack Ranges");
        UnityEditor.Handles.Label(transform.position + Vector3.right * detectionRadius, $"Detection: {detectionRadius}m");
        UnityEditor.Handles.Label(transform.position + Vector3.right * lightAttackRange, $"Light: {lightAttackRange}m");
        UnityEditor.Handles.Label(transform.position + Vector3.right * heavyAttackRange, $"Heavy: {heavyAttackRange}m");
        UnityEditor.Handles.Label(transform.position + Vector3.right * spinAttackRange, $"Spin: {spinAttackRange}m");
        UnityEditor.Handles.Label(transform.position + Vector3.right * groundSlamRange, $"Ground Slam: {groundSlamRange}m");
#endif
    }

    // Hiển thị Gizmos LUÔN LƯƠN (không cần select)
    private void OnDrawGizmos()
    {
        // Vẽ detection range (luôn hiển thị)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Vẽ attack ranges
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lightAttackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange for heavy
        Gizmos.DrawWireSphere(transform.position, heavyAttackRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, spinAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, groundSlamRange);
    }

    private System.Collections.IEnumerator RiseAndDestroy(GameObject go, Vector3 targetPos, float riseDuration, float totalDuration)
    {
        if (go == null) yield break;
        Transform t = go.transform;
        Vector3 start = t.position;
        float elapsed = 0f;
        float rise = Mathf.Max(0.01f, riseDuration);
        while (elapsed < rise)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / rise);
            t.position = Vector3.Lerp(start, targetPos, p);
            yield return null;
        }
        t.position = targetPos;
        // wait remaining lifetime
        float remaining = Mathf.Max(0f, totalDuration - rise);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);
        if (go != null) Destroy(go);
    }
}




