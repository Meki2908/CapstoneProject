using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Fusion;

public class WolfBossAI : NetworkBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════════

    [Header("=== References ===")]
    [Tooltip("Animator trên child Wolfboss_A. Tự tìm nếu để trống.")]
    [SerializeField] private Animator animator;

    [Tooltip("NavMeshAgent — có thể là Root hoặc child. Tự tìm nếu để trống.")]
    [SerializeField] private NavMeshAgent agent;

    [Header("=== Fang Spawning ===")]
    [Tooltip("NetworkObject prefab Fire Fang (có NetworkObject + BossFang).")]
    [SerializeField] private NetworkObject fireFangPrefab;

    [Tooltip("NetworkObject prefab Ice Fang (có NetworkObject + BossFang).")]
    [SerializeField] private NetworkObject iceFangPrefab;

    [Tooltip("Spawn point Fire Fang (Transform trên prefab Root).")]
    [SerializeField] private Transform fireFangSpawn;

    [Tooltip("Spawn point Ice Fang (Transform trên prefab Root).")]
    [SerializeField] private Transform iceFangSpawn;

    [Header("=== Phase 2 Visuals ===")]
    [Tooltip("Danh sách GameObjects (Eye Trails, Aura Smoke) được bật khi vào Phase 2.")]
    [SerializeField] private GameObject[] phase2Visuals;

    [Header("=== Combat ===")]
    [SerializeField] private float attackRange       = 7f;
    [SerializeField] private float detectRange       = 20f;
    [SerializeField] private int   normalAttackCountForSpecial = 5;
    [SerializeField] private float specialAttackCooldown       = 10f;
    [SerializeField] private float ultimateCooldown            = 60f;
    [SerializeField] private float stunDuration                = 5f;
    [SerializeField] private float fangDamageTickInterval      = 20f;

    [Header("=== Root Motion per Attack ===")]
    [Tooltip("Bật Root Motion trong luc Normal Attack (bươc tới theo animation).")]
    [SerializeField] private bool rootMotionOnNormalAttack  = false;
    [Tooltip("Bật Root Motion trong luc Special Attack (quan trọng — cầp nhậy vào player).")]
    [SerializeField] private bool rootMotionOnSpecialAttack = true;
    [Tooltip("Bật Root Motion trong luc Ultimate / Roar.")]
    [SerializeField] private bool rootMotionOnUltimate      = false;
    [Tooltip("Nếu Special Attack dùng Root Motion, Boss sẽ lunge về phía player trước khi chạy animation.")]
    [SerializeField] private float specialAttackLungeDistance = 2.5f;
    [Tooltip("Tốc độ lunge trước Special Attack (units/s).")]
    [SerializeField] private float specialAttackLungeSpeed    = 8f;

    [Header("=== Movement ===")]
    [Tooltip("Tốc độ xoay mặt về phía player khi Chase (Slerp t). Giảm để mượt hơn.")]
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private float walkSpeed     = 3.5f;
    [SerializeField] private float runSpeed      = 6f;

    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugLog = true;

    // ═══════════════════════════════════════════════════════════════════════════
    //  NETWORKED STATE
    // ═══════════════════════════════════════════════════════════════════════════

    [Networked] public float       NetworkedHp        { get; set; }
    [Networked] public float       NetworkedMaxHp     { get; set; }
    [Networked] public int         BossPhase          { get; set; }
    [Networked] public NetworkBool IsStunning         { get; set; }
    [Networked] public int         DamageMultiplier   { get; set; }

    [Networked] private int         _lastAnimTriggerHash  { get; set; }
    [Networked] private NetworkBool _animBoolIsStunning   { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FSM
    // ═══════════════════════════════════════════════════════════════════════════

    private enum BossState { Idle, Chase, NormalAttack, SpecialAttack, Ultimate, Stun, Dead }
    private BossState _state = BossState.Idle;

    // ═══════════════════════════════════════════════════════════════════════════
    //  RUNTIME
    // ═══════════════════════════════════════════════════════════════════════════

    private Transform _target;
    private float _distToTarget;

    private float _specialAttackTimer = 0f;
    private int   _normalAttackCount  = 0;
    private float _ultimateTimer      = 0f;
    private bool  _ultimateReadyAfterStun       = false;
    private bool  _phase2Entered                = false;
    private bool  _ultimateActivatedForPhase2   = false;

    private BossFang _fireFang;
    private BossFang _iceFang;
    private bool  _fireFangAlive = false;
    private bool  _iceFangAlive  = false;
    private float _fangDamageTimer = 0f;

    private float _stunTimer   = 0f;
    private bool  _isAttacking = false;

    // Phase 2 entry flag — Đảm bảo Ultimate phase 2 luôn được dùng (ít nhất 1 lần mỗi phase)
    // _phase2UltiPending = true khi vừa enter phase 2, chờ dịp đưa Ultimate ra
    private bool _phase2UltiPending = false;

    // ─── Phase 2 Fang Buff System ────────────────────────────────────────────
    // Khi cả 2 Fang còn sống, boss được buff:
    //   • Damage × 1.2 (tích và _fangDmgMultiplier)
    //   • Lifesteal = 80% (BossPawDamage.CheckPaw đọc qua GetLifeStealPercent())
    //   • Armor += baseArmor × 2.5 (EnemyScript.armorValue)
    //   • MagicResist += baseMagic × 2.5 (EnemyScript.magicValue)
    //   • Animator Speed = 1 khi di chuyển (chạy thay vì đi)
    // Khi Fang bị phá huỷ hết → buff tắt, CD 60s bắt đầu đếm
    private bool  _fangBuffActive      = false;
    private float _lifeStealPercent    = 0f;           // 0 = không hút máu, 0.8 = 80%
    private float _fangDmgMultiplier   = 1f;           // 1.2 khi fang active, 1 khi không

    // Base stats của EnemyScript — lưu khi Start để reset buff sau
    private int   _baseArmorValue      = 0;
    private int   _baseMagicValue      = 0;

    // Cache EnemyScript để apply buff
    private EnemyScript _enemyScript;

    // === Animation Event movement lock ===
    // LockMovement() và UnlockMovement() được gọi bởi WolfBossAnimationEvents (trên Wolfboss_A).
    // Khi _movementLocked = true, MoveAgentToward() không làm gì.
    private bool _movementLocked = false;

    // Fusion ChangeDetector
    private ChangeDetector _changeDetector;

    // Standalone mode (không có Fusion session)
    private bool _standaloneMode = false;

    // === NavMeshAgent proxy pattern ===
    // Agent (có thể nằm trên child) chỉ dùng để TÍNH PATH (updatePosition = false).
    // Root tự di chuyển theo agent.nextPosition mỗi frame.
    // Đây là pattern chuẩn của Unity khi NavMeshAgent không nằm trên Root.
    private bool _agentOnChild; // true nếu agent nằm trên child

    // === Locomotion velocity tracking ===
    // Vì agent.updatePosition=false, agent.velocity luôn = 0.
    // Ta tự tính bằng cách so sánh vị trí Root giữa các frame.
    private Vector3 _lastFramePos;
    private float   _currentMoveSpeed = 0f; // tốc độ thực tế đang di chuyển (units/s)

    // ═══════════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE (fallback khi không có Fusion)
    // ═══════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        // Nếu Fusion không active, chạy standalone mode
        if (Runner == null || !Object.IsValid)
        {
            _standaloneMode = true;
            InitReferences();
            InitValues();
            Log("[WolfBossAI] Running in STANDALONE mode (no Fusion session).");
        }
    }

    private void Update()
    {
        if (!_standaloneMode) return;
        if (_state == BossState.Dead) return;

        RefreshTarget();
        UpdateDistToTarget();

        // Kiểm tra Phase 2
        StandaloneCheckPhaseTransition();

        float delta = Time.deltaTime;
        _specialAttackTimer += delta;
        // Ultimate CD chỉ đếm khi không còn Fang nào trên sân
        if (!_fireFangAlive && !_iceFangAlive && _ultimateActivatedForPhase2)
            _ultimateTimer += delta;
        // Fang damage timer: chỉ đếm khi fang còn sống
        if (_fireFangAlive || _iceFangAlive) _fangDamageTimer += delta;

        RunFSM(delta);
        TrackRootVelocity(delta);
        SyncLocomotionAnimation();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FUSION LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════════

    public override void Spawned()
    {
        _standaloneMode = false;
        InitReferences();

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
            InitValues();

        Log($"[WolfBossAI] Spawned (Fusion). IsStateAuthority={Object.HasStateAuthority}");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_state == BossState.Dead) return;

        RefreshTarget();
        UpdateDistToTarget();

        // Phase check
        FusionCheckPhaseTransition();

        float delta = Runner.DeltaTime;
        _specialAttackTimer += delta;
        // Ultimate CD chỉ đếm khi không còn Fang
        if (!_fireFangAlive && !_iceFangAlive && _ultimateActivatedForPhase2)
            _ultimateTimer += delta;
        if (_fireFangAlive || _iceFangAlive) _fangDamageTimer += delta;

        RunFSM(delta);
    }

    public override void Render()
    {
        if (animator == null || _changeDetector == null) return;

        // Tính velocity thực từ độ thay đổi vị trí Root
        TrackRootVelocity(Time.deltaTime);
        SyncLocomotionAnimation();

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(_lastAnimTriggerHash):
                    if (_lastAnimTriggerHash != 0)
                        animator.SetTrigger(_lastAnimTriggerHash);
                    break;
                case nameof(_animBoolIsStunning):
                    animator.SetBool("isStunning", _animBoolIsStunning);
                    break;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INIT
    // ═══════════════════════════════════════════════════════════════════════════

    private void InitReferences()
    {
        // Animator — tìm trong children (Wolfboss_A)
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // EnemyScript — đảm bảo isBoss = true để CC immunity và damage system
        _enemyScript = GetComponent<EnemyScript>();
        if (_enemyScript == null) _enemyScript = GetComponentInChildren<EnemyScript>();
        if (_enemyScript != null)
        {
            _enemyScript.isBoss = true;
            _baseArmorValue = _enemyScript.armorValue;
            _baseMagicValue = _enemyScript.magicValue;
            Log($"[WolfBossAI] EnemyScript found — isBoss=true, baseArmor={_baseArmorValue}, baseMagic={_baseMagicValue}");
        }
        else
        {
            Debug.LogWarning("[WolfBossAI] EnemyScript not found on Vargr! CC immunity relies on isBoss flag. " +
                             "Add EnemyScript to Vargr root GO and set isBoss=true in Inspector.");
        }


        // NavMeshAgent — Root ưu tiên, sau đó mới tìm trong children
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = GetComponentInChildren<NavMeshAgent>();
        }

        if (agent != null)
        {
            _agentOnChild = (agent.transform != transform);

            // === KEY: NavMeshAgent chỉ làm pathfinding, ROOT tự di chuyển ===
            agent.updatePosition = false;
            agent.updateRotation = false;

            // Warp agent về vị trí Root để đảm bảo sampling đúng trên NavMesh
            if (agent.isOnNavMesh)
                agent.Warp(transform.position);
            else
                UnityEngine.AI.NavMesh.SamplePosition(
                    transform.position, out var hit, 5f,
                    UnityEngine.AI.NavMesh.AllAreas);

            if (_agentOnChild)
                Log($"[WolfBossAI] NavMeshAgent trên child '{agent.transform.name}'. " +
                    $"isOnNavMesh={agent.isOnNavMesh}. Dùng proxy mode.");

            if (!agent.isOnNavMesh)
                Debug.LogWarning("[WolfBossAI] Agent KHÔNG trên NavMesh lúc khởi tạo! " +
                    "Kiểm tra: (1) NavMesh đã bake? (2) AgentTypeID trên NavMesh Surface " +
                    $"khớp với agent (AgentTypeID={agent.agentTypeID})? " +
                    "Boss sẽ dùng direct movement làm fallback.");
        }
        else
        {
            Debug.LogError("[WolfBossAI] Không tìm thấy NavMeshAgent! Boss sẽ không di chuyển được.");
        }

        FindTarget();
    }

    private void InitValues()
    {
        BossPhase        = 1;
        IsStunning       = false;
        DamageMultiplier = 1;
        _state = BossState.Idle;
        ResetPhase2State();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FSM
    // ═══════════════════════════════════════════════════════════════════════════

    private void RunFSM(float delta)
    {
        if (_state == BossState.Stun)
        {
            HandleStunState(delta);
            return;
        }

        if (_isAttacking) return;

        switch (_state)
        {
            case BossState.Idle:  HandleIdleState();  break;
            case BossState.Chase: HandleChaseState(); break;
        }
    }

    // ── Idle ─────────────────────────────────────────────────────────────────

    private void HandleIdleState()
    {
        if (_target == null) return;
        if (_distToTarget <= detectRange)
        {
            Log("[WolfBossAI] Player detected → Chase");
            ChangeState(BossState.Chase);
        }
    }

    // ── Chase ────────────────────────────────────────────────────────────────

    private void HandleChaseState()
    {
        if (_target == null) { ChangeState(BossState.Idle); return; }

        SmoothRotateToTarget();

        // Phase 2: Ultimate trước
        if (GetBossPhase() == 2 && CanUseUltimate())
        {
            StartAttack(BossState.Ultimate);
            return;
        }

        if (_distToTarget <= attackRange)
        {
            if (CanUseSpecialAttack())
            {
                StartAttack(BossState.SpecialAttack);
                return;
            }
            StartAttack(BossState.NormalAttack);
            return;
        }

        // Phase 1 → Walk, Phase 2 → Run
        MoveAgentToward(_target.position, GetChaseSpeed());
    }

    /// <summary>
    /// Phase 1: đi bộ (walkSpeed). Phase 2: chạy (runSpeed).
    /// </summary>
    private float GetChaseSpeed() => GetBossPhase() >= 2 ? runSpeed : walkSpeed;

    // ── Stun ─────────────────────────────────────────────────────────────────

    private void HandleStunState(float delta)
    {
        _stunTimer -= delta;
        if (_stunTimer <= 0f) EndStun();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MOVEMENT — NavMeshAgent proxy pattern
    //
    //  NavMeshAgent (trên Root hoặc child) dùng để tính TOÁN PATH.
    //  Root object tự di chuyển theo agent.nextPosition mỗi frame.
    //  Sau đó feed lại agent.nextPosition = Root.position.
    //  => NavMesh sampling luôn chính xác, Root di chuyển mượt.
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Di chuyển Root về phía destination với tốc độ chỉ định.
    /// Ưu tiên dùng NavMesh Agent (proxy mode). Fallback: direct movement.
    /// </summary>
    private void MoveAgentToward(Vector3 destination, float speed)
    {
        // Animation Event đã khoá di chuyển (boss đang perform attack)
        if (_movementLocked) return;

        float dt = GetDeltaTime();

        // === NavMesh path mode ===
        if (agent != null && agent.isOnNavMesh && !agent.isStopped)
        {
            // Sync Root position vào agent (proxy pattern)
            agent.nextPosition = transform.position;
            agent.speed = speed;
            agent.SetDestination(destination);

            // Di chuyển Root theo nextPosition agent tính toán
            Vector3 desired = agent.nextPosition;
            Vector3 targetFlat = new Vector3(desired.x, transform.position.y, desired.z);
            transform.position = Vector3.MoveTowards(transform.position, targetFlat, speed * dt);
            return;
        }

        // === Fallback: direct movement (không cần NavMesh) ===
        Vector3 dir = destination - transform.position;
        dir.y = 0f;
        if (dir.magnitude > 0.1f)
        {
            Vector3 step = dir.normalized * speed * dt;
            transform.position += new Vector3(step.x, 0f, step.z);
        }
    }

    private void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.nextPosition = transform.position;
        }
    }

    private void ResumeAgent()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.nextPosition = transform.position;

            // Warp lại về Root nếu agent bị lệch
            if (Vector3.Distance(agent.nextPosition, transform.position) > 2f)
                agent.Warp(transform.position);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ATTACK EXECUTION
    // ═══════════════════════════════════════════════════════════════════════════

    private void StartAttack(BossState attackType)
    {
        _isAttacking = true;
        _state = attackType;
        StopAgent();

        // Đặt root motion theo loại attack
        bool useRootMotion = attackType switch
        {
            BossState.NormalAttack  => rootMotionOnNormalAttack,
            BossState.SpecialAttack => rootMotionOnSpecialAttack,
            BossState.Ultimate      => rootMotionOnUltimate,
            _                       => false
        };
        SetRootMotion(useRootMotion);

        switch (attackType)
        {
            case BossState.NormalAttack:  StartCoroutine(ExecuteNormalAttack());  break;
            case BossState.SpecialAttack: StartCoroutine(ExecuteSpecialAttack()); break;
            case BossState.Ultimate:      StartCoroutine(ExecuteUltimate());      break;
        }
    }

    /// <summary>Bật/tắt Animator.applyRootMotion trên child Wolfboss_A.</summary>
    private void SetRootMotion(bool enable)
    {
        if (animator != null)
            animator.applyRootMotion = enable;
        Log($"[WolfBossAI] Root Motion = {enable}");
    }

    private IEnumerator ExecuteNormalAttack()
    {
        Log("[WolfBossAI] Normal Attack!");
        SnapFaceTarget();
        TriggerAnimationOnAll("na");

        // Animation Event sẽ gọi UnlockMovement() khi animation kết thúc.
        // Safety fallback: tự động unlock nếu AE không fire (animation bị ngắt,
        // hoặc chưa setup AE trong clip).
        yield return new WaitForSeconds(GetAnimationDuration("na", 1.2f) + 0.5f);
        if (_isAttacking && _state == BossState.NormalAttack)
        {
            Log("[WolfBossAI] NormalAttack timeout — force UnlockMovement");
            UnlockMovement();
        }
    }

    private IEnumerator ExecuteSpecialAttack()
    {
        Log("[WolfBossAI] Special Attack!");
        _normalAttackCount = 0;
        _specialAttackTimer = 0f;

        SnapFaceTarget();

        // Lunge về phía player trước khi chạy animation (nếu không dùng root motion)
        if (!rootMotionOnSpecialAttack && specialAttackLungeDistance > 0f)
            yield return StartCoroutine(LungeTowardTarget(
                specialAttackLungeDistance, specialAttackLungeSpeed));

        TriggerAnimationOnAll("sa");

        // Safety fallback
        yield return new WaitForSeconds(GetAnimationDuration("sa", 1.8f) + 0.5f);
        if (_isAttacking && _state == BossState.SpecialAttack)
        {
            Log("[WolfBossAI] SpecialAttack timeout — force UnlockMovement");
            UnlockMovement();
        }
    }

    private IEnumerator ExecuteUltimate()
    {
        Log("[WolfBossAI] Ultimate!");
        _ultimateTimer = 0f;
        _ultimateReadyAfterStun = false;
        _ultimateActivatedForPhase2 = true;
        _phase2UltiPending = false; // Đã xử lý phase 2 pending

        // War Roar — roar không phải attack nên không lock movement
        TriggerAnimationOnAll("roar");
        yield return new WaitForSeconds(GetAnimationDuration("roar", 2f));

        // Ultimate anim — AE sẽ gọi LockMovement/UnlockMovement
        TriggerAnimationOnAll("ulti");
        SpawnFangs();
        _fangDamageTimer = 0f;

        // Safety fallback cho ulti
        yield return new WaitForSeconds(GetAnimationDuration("ulti", 2.5f) + 0.5f);
        if (_isAttacking && _state == BossState.Ultimate)
        {
            Log("[WolfBossAI] Ultimate timeout — force UnlockMovement");
            UnlockMovement();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LUNGE HELPER
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dịch chuyển Boss về phía target một đoạn maxDist trong thời gian ngắn.
    /// Dùng trước animation khi không dùng Root Motion (special attack lunge).
    /// </summary>
    private IEnumerator LungeTowardTarget(float maxDist, float speed)
    {
        if (_target == null) yield break;

        Vector3 start = transform.position;
        Vector3 dir   = (_target.position - start);
        dir.y = 0f;
        float available = Mathf.Max(0f, dir.magnitude - 1.5f); // giữ khoảng cách tối thiểu
        float dist = Mathf.Min(maxDist, available);
        Vector3 end = start + dir.normalized * dist;

        float elapsed = 0f;
        float duration = dist / Mathf.Max(speed, 0.01f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 next = Vector3.Lerp(start, end, t);
            transform.position = new Vector3(next.x, transform.position.y, next.z);
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PHASE
    // ═══════════════════════════════════════════════════════════════════════════

    // Dùng cho FixedUpdateNetwork (Fusion mode)
    private void FusionCheckPhaseTransition()
    {
        if (_phase2Entered) return;
        if (NetworkedMaxHp <= 0f) return;
        if (NetworkedHp / NetworkedMaxHp <= 0.5f) EnterPhase2();
    }

    // Dùng cho Update (standalone mode) — đọc từ TakeDamageTest
    private void StandaloneCheckPhaseTransition()
    {
        if (_phase2Entered) return;
        var health = GetComponentInChildren<TakeDamageTest>();
        if (health == null) return;
        if (health.MaxHealth <= 0f) return;
        if (health.CurrentHealth / health.MaxHealth <= 0.5f) EnterPhase2();
    }

    private void EnterPhase2()
    {
        _phase2Entered = true;
        SetBossPhase(2);
        _phase2UltiPending = true; // Queue ultimate để dùng ngay khi có dịp
        Log("[WolfBossAI] *** PHASE 2 ENTERED ***");

        // Bật các hiệu ứng hình ảnh phase 2 (Eye Trails, Aura Smoke...)
        if (phase2Visuals != null)
        {
            foreach (var go in phase2Visuals)
            {
                if (go != null) go.SetActive(true);
            }
        }

        // Nếu boss đang rảnh (đang Chase hoặc Idle và không tấn công) thì dùng ngay
        if (!_isAttacking && _state != BossState.Stun && _state != BossState.Dead)
            StartAttack(BossState.Ultimate);
        // Nếu đang tấn công thì _phase2UltiPending sẽ được xử lý sau khi UnlockMovement()
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FANG SYSTEM
    // ═══════════════════════════════════════════════════════════════════════════

    private void SpawnFangs()
    {
        bool hasFusion = !_standaloneMode && Runner != null;

        // === Fusion mode ===
        if (hasFusion && Object.HasStateAuthority)
        {
            SpawnFangFusion(fireFangPrefab,fireFangSpawn, transform.right * 3f, BossFang.FangType.FireFang, ref _fireFang, ref _fireFangAlive);
            SpawnFangFusion(iceFangPrefab, iceFangSpawn, -transform.right * 3f,BossFang.FangType.IceFang,  ref _iceFang,  ref _iceFangAlive);
        }
        // === Standalone mode (test) ===
        else if (_standaloneMode)
        {
            SpawnFangStandalone(fireFangPrefab, fireFangSpawn, transform.right * 3f, BossFang.FangType.FireFang, ref _fireFang, ref _fireFangAlive);
            SpawnFangStandalone(iceFangPrefab,  iceFangSpawn, -transform.right * 3f, BossFang.FangType.IceFang,  ref _iceFang,  ref _iceFangAlive);
        }

        Log($"[WolfBossAI] Fangs spawned: Fire={_fireFangAlive}, Ice={_iceFangAlive}");
        // Bật buff ngay khi spawn Fang thành công
        if (_fireFangAlive && _iceFangAlive)
            EnableFangBuff();
    }

    private void SpawnFangFusion(NetworkObject prefab, Transform spawnPoint, Vector3 fallback,
                                  BossFang.FangType fangType, ref BossFang fangRef, ref bool aliveFlag)
    {
        if (prefab == null) { Debug.LogWarning($"[WolfBossAI] {fangType} prefab null!"); return; }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + fallback;
        var obj = Runner.Spawn(prefab, pos, Quaternion.identity, Object.InputAuthority);
        if (obj == null) return;

        fangRef = obj.GetComponent<BossFang>();
        if (fangRef != null) { fangRef.bossRef = this; aliveFlag = true; }
    }

    private void SpawnFangStandalone(NetworkObject prefab, Transform spawnPoint, Vector3 fallback,
                                      BossFang.FangType fangType, ref BossFang fangRef, ref bool aliveFlag)
    {
        if (prefab == null) { Debug.LogWarning($"[WolfBossAI] {fangType} prefab null!"); return; }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position + fallback;
        var go = Instantiate(prefab.gameObject, pos, Quaternion.identity);
        fangRef = go.GetComponent<BossFang>();
        if (fangRef != null) { fangRef.bossRef = this; aliveFlag = true; }
    }

    public void OnFangDestroyed(BossFang fang)
    {
        if (!_standaloneMode && !Object.HasStateAuthority) return;

        if (fang.Type == BossFang.FangType.FireFang) { _fireFangAlive = false; Log("[WolfBossAI] Fire Fang destroyed!"); }
        else                                          { _iceFangAlive  = false; Log("[WolfBossAI] Ice Fang destroyed!"); }

        if (!_fireFangAlive && !_iceFangAlive)
        {
            Log("[WolfBossAI] Both Fangs down → STUN + remove buff!");
            // Tắt buff khi cả 2 Fang bị phá huỷ
            EndFangBuff();
            TriggerStun();
            // Bắt đầu đếm CD Ultimate từ đây (timer đã reset trong EndStun)
            _ultimateTimer = 0f;
        }
    }

    // ── Fang Buff System ─────────────────────────────────────────────────────

    /// <summary>
    /// Bật buff của Fang khi cả 2 Fang vừa được spawn.
    /// • Damage × 1.2
    /// • Lifesteal = 80%
    /// • Armor += baseArmor × 2.5
    /// • MagicResist += baseMagic × 2.5
    /// </summary>
    private void EnableFangBuff()
    {
        if (_fangBuffActive) return;
        _fangBuffActive    = true;
        _fangDmgMultiplier = 1.2f;
        _lifeStealPercent  = 0.8f;

        if (_enemyScript != null)
        {
            _enemyScript.armorValue = _baseArmorValue + Mathf.RoundToInt(_baseArmorValue * 2.5f);
            _enemyScript.magicValue = _baseMagicValue + Mathf.RoundToInt(_baseMagicValue * 2.5f);
            // Kicking inspector values into EnemyClass immediately
            if (_enemyScript.enemy != null)
            {
                _enemyScript.enemy.armor.value = _enemyScript.armorValue;
                _enemyScript.enemy.magic.value = _enemyScript.magicValue;
            }
        }
        Log($"[WolfBossAI] FANG BUFF ON — dmg×1.2, lifesteal 80%, " +
            $"armor={( _enemyScript != null ? _enemyScript.armorValue : 0 )}, " +
            $"magic={( _enemyScript != null ? _enemyScript.magicValue : 0 )}");
    }

    /// <summary>Tắt buff Fang — reset stats về base.</summary>
    private void EndFangBuff()
    {
        if (!_fangBuffActive) return;
        _fangBuffActive    = false;
        _fangDmgMultiplier = 1f;
        _lifeStealPercent  = 0f;

        if (_enemyScript != null)
        {
            _enemyScript.armorValue = _baseArmorValue;
            _enemyScript.magicValue = _baseMagicValue;
            if (_enemyScript.enemy != null)
            {
                _enemyScript.enemy.armor.value = _baseArmorValue;
                _enemyScript.enemy.magic.value = _baseMagicValue;
            }
        }
        Log("[WolfBossAI] FANG BUFF OFF — stats reset to base.");
    }

    /// <summary>
    /// Public: BossPawDamage đọc vào để tính damage thực tế.
    /// Bao gồm cả DamageMultiplier từ Networked var và fang buff.
    /// </summary>
    public float GetCurrentDamageMultiplier() => _fangDmgMultiplier;

    /// <summary>Public: BossPawDamage đọc vào để hồi máu sau mỗi đòn đánh.</summary>
    public float GetLifeStealPercent() => _lifeStealPercent;

    // ═══════════════════════════════════════════════════════════════════════════
    //  STUN
    // ═══════════════════════════════════════════════════════════════════════════

    private void TriggerStun()
    {
        if (_state == BossState.Stun || _state == BossState.Dead) return;

        _state = BossState.Stun;
        _stunTimer = stunDuration;
        _isAttacking = false;
        StopAgent();

        SetIsStunning(true);
        SetAnimBoolIsStunning(true);
        Log($"[WolfBossAI] STUNNED for {stunDuration}s");
    }

    private void EndStun()
    {
        SetIsStunning(false);
        SetAnimBoolIsStunning(false);
        animator?.SetBool("isStunning", false);

        SetDamageMultiplier(1);
        _ultimateTimer = 0f;
        _ultimateReadyAfterStun = true;

        ResumeAgent();
        Log("[WolfBossAI] Stun ended.");
        ChangeState(BossState.Chase);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  CONDITIONS
    // ═══════════════════════════════════════════════════════════════════════════

    private bool CanUseSpecialAttack()
    {
        return _specialAttackTimer >= specialAttackCooldown
            || _normalAttackCount >= normalAttackCountForSpecial;
    }

    private bool CanUseUltimate()
    {
        if (GetBossPhase() < 2) return false;

        // Pending từ Phase 2 entry (chưa dùng lần đầu)
        // Cho phép immediate — không cần stun hay timer
        if (_phase2UltiPending) return true;

        // Lần sau: chỉ dùng khi KHÔNG còn Fang và CD đủ (60s)
        // • Nếu Fang còn sống → CD không đếm, không dùng được
        if (!_ultimateActivatedForPhase2) return false;
        if (_fireFangAlive || _iceFangAlive) return false;
        return _ultimateTimer >= ultimateCooldown;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  NETWORKED FIELD WRAPPERS (dual mode)
    //  Dùng [Networked] khi có Fusion, dùng biến local khi standalone
    // ═══════════════════════════════════════════════════════════════════════════

    // Local fallbacks cho standalone mode
    private int   _localBossPhase      = 1;
    private bool  _localIsStunning     = false;
    private int   _localDmgMultiplier  = 1;
    private bool  _localAnimStunning   = false;

    private int  GetBossPhase()        => _standaloneMode ? _localBossPhase     : BossPhase;
    private void SetBossPhase(int v)   { if (_standaloneMode) _localBossPhase = v; else BossPhase = v; }

    private bool GetIsStunning()       => _standaloneMode ? _localIsStunning    : (bool)IsStunning;
    private void SetIsStunning(bool v) { if (_standaloneMode) _localIsStunning = v; else IsStunning = v; }

    private int  GetDamageMultiplier()     => _standaloneMode ? _localDmgMultiplier   : DamageMultiplier;
    private void SetDamageMultiplier(int v){ if (_standaloneMode) _localDmgMultiplier = v; else DamageMultiplier = v; }

    private void SetAnimBoolIsStunning(bool v)
    {
        _localAnimStunning = v;
        if (!_standaloneMode) _animBoolIsStunning = v;
        animator?.SetBool("isStunning", v);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ANIMATION
    // ═══════════════════════════════════════════════════════════════════════════

    private void TriggerAnimationOnAll(string triggerName)
    {
        animator?.SetTrigger(triggerName);

        if (!_standaloneMode)
            _lastAnimTriggerHash = Animator.StringToHash(triggerName);
    }

    /// <summary>
    /// Tính velocity thực tế của Root bằng cách so delta vị trí giữa 2 frame.
    /// Cần gọi MỖI FRAME (Update hoặc Render) với đúng deltaTime.
    /// </summary>
    private void TrackRootVelocity(float dt)
    {
        if (dt <= 0f) return;
        _currentMoveSpeed = Vector3.Distance(transform.position, _lastFramePos) / dt;
        _lastFramePos = transform.position;
    }

    /// <summary>
    /// Đặt Animator Speed dựa trên tốc độ thực Root đang di chuyển.
    /// Phase 1: Walk blend (0→0.5), Phase 2: Run blend (0.5→1.0).
    /// </summary>
    private void SyncLocomotionAnimation()
    {
        if (animator == null) return;

        // Ngưỡng để coi là "đang di chuyển"
        const float movingThreshold = 0.05f;
        bool isMoving = _currentMoveSpeed > movingThreshold
                        && _state == BossState.Chase
                        && !_isAttacking;

        float targetSpeed;
        if (!isMoving)
        {
            // Idle
            targetSpeed = 0f;
        }
        else if (GetBossPhase() >= 2)
        {
            // Phase 2 → Run (Speed = 1.0)
            targetSpeed = 1.0f;
        }
        else
        {
            // Phase 1 → Walk (Speed = 0.5)
            targetSpeed = 0.5f;
        }

        // Lerp mượt để tránh animation giật
        float current = animator.GetFloat("Speed");
        float smoothed = Mathf.Lerp(current, targetSpeed, 10f * GetDeltaTime());
        animator.SetFloat("Speed", smoothed);
    }

    private float GetAnimationDuration(string triggerName, float fallback)
    {
        if (animator == null) return fallback;
        var clips = animator.runtimeAnimatorController?.animationClips;
        if (clips != null)
            foreach (var clip in clips)
                if (clip.name.ToLower().Contains(triggerName.ToLower()))
                    return clip.length;
        return fallback;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  MOVEMENT HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void SmoothRotateToTarget(float speed = -1f)
    {
        if (_target == null) return;
        Vector3 dir = _target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        float spd = speed < 0f ? rotationSpeed : speed;
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            spd * GetDeltaTime()
        );
    }

    /// <summary>Snap ngay về hướng player (không lerp) trước khi tấn công.</summary>
    private void SnapFaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = _target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    private void UpdateDistToTarget()
    {
        if (_target == null) { _distToTarget = float.MaxValue; return; }
        Vector3 flat = _target.position - transform.position;
        flat.y = 0f;
        _distToTarget = flat.magnitude;
    }

    private float GetDeltaTime() => _standaloneMode ? Time.deltaTime : Runner.DeltaTime;

    // ═══════════════════════════════════════════════════════════════════════════
    //  TARGET FINDING
    // ═══════════════════════════════════════════════════════════════════════════

    private void FindTarget()
    {
        var character = FindFirstObjectByType<Character>();
        if (character != null) { _target = character.transform; return; }

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _target = go.transform;
    }

    private void RefreshTarget()
    {
        if (_target == null) FindTarget();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PUBLIC API (từ WolfBossAnimationEvents & WolfBossHealthBridge)
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Animation Event callbacks ─────────────────────────────────────────────

    /// <summary>
    /// Gọi bởi Animation Event (qua WolfBossAnimationEvents) tại frame BẮT ĐẦU tấn công.
    /// Khoá di chuyển: Boss đứng yên trong suốt animation.
    /// </summary>
    public void LockMovement()
    {
        _movementLocked = true;
        Log("[WolfBossAI] Movement LOCKED (Animation Event)");
    }

    /// <summary>
    /// Gọi bởi Animation Event (qua WolfBossAnimationEvents) tại frame CUỐI animation attack.
    /// Mở lại di chuyển và chuyển Boss về trạng thái Chase.
    /// </summary>
    public void UnlockMovement()
    {
        if (!_isAttacking) return; // safety: đã unlock rồi

        _movementLocked = false;

        // Post-attack cleanup tuỳ loại attack
        if (_state == BossState.NormalAttack)
            _normalAttackCount++;

        bool wasDead = (_state == BossState.Dead);
        SetRootMotion(false);
        StopAllCoroutines(); // huỷ safety-timeout coroutine nếu AE fire trước
        _isAttacking = false;
        ResumeAgent();

        if (!wasDead)
            _state = BossState.Chase;

        Log("[WolfBossAI] Movement UNLOCKED (Animation Event) → Chase");

        // Nếu Phase 2 Ultimate bị pending (boss đang tấn công khi enter phase 2), dùng ngay bây giờ
        if (_phase2UltiPending && !wasDead)
        {
            Log("[WolfBossAI] Phase 2 Ultimate pending — triggering now!");
            StartAttack(BossState.Ultimate);
        }
    }

    /// <summary>Gọi khi HP thay đổi. Cập nhật networked HP + Fang buff.</summary>
    public void OnDamageTaken(float currentHp, float maxHp)
    {
        if (!_standaloneMode && !Object.HasStateAuthority) return;

        if (!_standaloneMode)
        {
            NetworkedHp    = currentHp;
            NetworkedMaxHp = maxHp;
        }

        // Gethit animation được điều khiển bởi Animator Controller (Stun state).
        // Không trigger gethit từ code nữa vì quái to không giật khi ăn đòn nhỏ.
    }

    /// <summary>Gọi khi Boss chết.</summary>
    public void OnBossDied()
    {
        if (!_standaloneMode && !Object.HasStateAuthority) return;
        if (_state == BossState.Dead) return;

        Log("[WolfBossAI] BOSS DIED!");
        _state = BossState.Dead;
        SetIsStunning(false);
        StopAgent();
        TriggerAnimationOnAll("die");

        _fireFang?.ForceKill();
        _iceFang?.ForceKill();

        // Reset toàn bộ Phase 2 state để Play Mode lần sau khởi đầu sạch
        ResetPhase2State();
    }

    private void OnDisable()
    {
        // Đảm bảo Phase 2 được reset hoàn toàn khi thoát Play Mode hoặc disable GO
        ResetPhase2State();
    }

    /// <summary>
    /// Reset toàn bộ Phase 2 runtime state về giá trị ban đầu.
    /// Gọi khi: boss chết, OnDisable, InitValues.
    /// Đảm bảo mỗi lần vào Play Mode đều sạch rủi mới bắt đầu Phase.
    /// </summary>
    private void ResetPhase2State()
    {
        _phase2Entered              = false;
        _phase2UltiPending          = false;
        _ultimateActivatedForPhase2 = false;
        _ultimateReadyAfterStun     = false;
        _ultimateTimer              = 0f;
        _fangDamageTimer            = 0f;
        _fireFangAlive              = false;
        _iceFangAlive               = false;
        EndFangBuff(); // reset stats + flags
        SetBossPhase(1);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FSM HELPER
    // ═══════════════════════════════════════════════════════════════════════════

    private void ChangeState(BossState newState)
    {
        if (_state == newState || _state == BossState.Dead) return;
        Log($"[WolfBossAI] {_state} → {newState}");
        _state = newState;
        if (newState == BossState.Chase) ResumeAgent();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  DEBUG
    // ═══════════════════════════════════════════════════════════════════════════

    private void Log(string msg) { if (showDebugLog) Debug.Log(msg); }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        if (fireFangSpawn != null) { Gizmos.color = Color.red;  Gizmos.DrawSphere(fireFangSpawn.position, 0.4f); }
        if (iceFangSpawn  != null) { Gizmos.color = Color.cyan; Gizmos.DrawSphere(iceFangSpawn.position,  0.4f); }
    }
}
