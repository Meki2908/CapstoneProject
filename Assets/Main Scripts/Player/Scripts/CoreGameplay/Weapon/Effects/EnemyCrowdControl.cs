using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Runtime crowd-control helper for enemy movement displacement.
/// Works with NavMeshAgent-based enemies without requiring Rigidbody forces.
///
/// FIX: Enemy bị bay lên trời sau CC → thêm Fallback Gravity system:
///   • Sau mỗi CC routine (hoặc khi routine bị ngắt giữa chừng), nếu enemy
///     chưa chạm layer Ground, giữ NavMeshAgent dừng và tự rơi xuống theo
///     gravity. Chỉ resume agent khi enemy đã chạm đất.
///   • TornadoRoutine cũng được fix để snap về baseY khi kết thúc.
/// </summary>
public class EnemyCrowdControl : MonoBehaviour
{
    // ── References ──────────────────────────────────────────────────────────
    private NavMeshAgent navMeshAgent;
    private EnemyScript  enemyScript;
    private Rigidbody    enemyRigidbody;
    private EnemyState   enemyState;
    private BossMultiSkill bossMultiSkill;

    // ── CC state ─────────────────────────────────────────────────────────────
    private Coroutine activeControlRoutine;
    private bool      agentWasStopped;
    private bool      rbWasKinematic;
    private int       controlToken;

    // ── Fallback Gravity ─────────────────────────────────────────────────────
    [Header("Fallback Gravity (rơi xuống sau CC)")]
    [Tooltip("Bật/tắt gravity fallback. Để bật khi enemy không có Rigidbody thật.")]
    [SerializeField] private bool enableFallbackGravity = true;

    [Tooltip("Gia tốc rơi (units/s²). Nên >= 9.81 để tự nhiên.")]
    [SerializeField] private float gravityAcceleration = 20f;

    [Tooltip("LayerMask của layer 'Ground'. Dùng để detect enemy có đang đứng trên đất không.")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Khoảng cách raycast xuống phía dưới từ pivot (thêm buffer). Tăng nếu pivot nằm cao hơn chân.")]
    [SerializeField] private float groundCheckOffset = 0.3f;

    [Tooltip("Bán kính SphereCast khi check ground — rộng hơn để tránh miss.")]
    [SerializeField] private float groundCheckRadius = 0.35f;

    // Runtime gravity tracking
    private bool  _waitingForLanding = false; // đang rơi sau CC, chưa resume agent
    private float _fallVelocity      = 0f;    // vận tốc rơi tích lũy (units/s)
    private bool  _ccIsActive        = false;  // có CC routine đang chạy

    // ── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        navMeshAgent   = GetComponent<NavMeshAgent>();
        enemyScript    = GetComponent<EnemyScript>();
        enemyRigidbody = GetComponent<Rigidbody>();
        enemyState     = GetComponent<EnemyState>();
        if (enemyState  == null) enemyState  = GetComponentInParent<EnemyState>();
        bossMultiSkill  = GetComponent<BossMultiSkill>();
        if (bossMultiSkill == null) bossMultiSkill = GetComponentInParent<BossMultiSkill>();
    }

    private void Update()
    {
        // CC routine đang chạy → nó tự handle position, không cần can thiệp
        if (_ccIsActive) return;

        // Đang chờ rơi xuống đất (sau CC kết thúc hoặc bị ngắt)
        if (_waitingForLanding)
            HandleLandingGravity();
        // Ngoài CC: kiểm tra liên tục — đảm bảo nếu enemy bị stuck trên không
        // (do bất kỳ lý do gì) thì luôn rơi xuống
        else if (enableFallbackGravity && !IsGrounded())
            BeginLanding();
    }

    // ── Public CC API ────────────────────────────────────────────────────────

    public void PlayKnockback(Vector3 sourcePosition, float horizontalDistance,
                              float duration, float peakHeight = 0f)
    {
        Vector3 start = transform.position;
        Vector3 planarDir = (transform.position - sourcePosition);
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 0.0001f)
        {
            planarDir = transform.forward;
            planarDir.y = 0f;
        }

        Vector3 end = start + planarDir.normalized * Mathf.Max(0f, horizontalDistance);
        end.y = start.y;
        if (IsBossSuperArmor()) return;
        StartControlledMove(start, end, duration, peakHeight);
    }

    public void PlayKnockup(Vector3 sourcePosition, float horizontalDistance,
                            float peakHeight, float riseDuration, float fallDuration)
    {
        Vector3 start = transform.position;
        Vector3 planarDir = (transform.position - sourcePosition);
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 0.0001f)
        {
            planarDir = transform.forward;
            planarDir.y = 0f;
        }

        Vector3 end = start + planarDir.normalized * Mathf.Max(0f, horizontalDistance);
        end.y = start.y;

        int token = BeginControl();
        if (token < 0) return; // Super Armor
        activeControlRoutine = StartCoroutine(KnockupRoutine(
            token, start, end,
            Mathf.Max(0f, peakHeight),
            Mathf.Max(0.01f, riseDuration),
            Mathf.Max(0.01f, fallDuration)
        ));
    }

    public void PlayPull(Vector3 targetPosition, float pullDistance,
                         float duration, float peakHeight = 0f)
    {
        Vector3 start = transform.position;
        Vector3 planarDir = (targetPosition - start);
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 0.0001f) return;

        float distanceToTarget = planarDir.magnitude;
        float moveDistance = Mathf.Clamp(pullDistance, 0f, distanceToTarget);
        Vector3 end = start + planarDir.normalized * moveDistance;
        end.y = start.y;
        if (IsBossSuperArmor()) return;
        StartControlledMove(start, end, duration, peakHeight);
    }

    public void PlayTornado(Vector3 center, float radius, float totalRotationDegrees,
                            float duration, float maxHeight)
    {
        int token = BeginControl();
        if (token < 0) return; // Super Armor
        activeControlRoutine = StartCoroutine(TornadoRoutine(
            token, center,
            Mathf.Max(0.1f, radius),
            totalRotationDegrees, duration,
            Mathf.Max(0f, maxHeight)
        ));
    }

    // ── Internal helpers ─────────────────────────────────────────────────────

    private void StartControlledMove(Vector3 start, Vector3 end, float duration, float peakHeight)
    {
        int token = BeginControl();
        activeControlRoutine = StartCoroutine(MoveRoutine(token, start, end, duration, peakHeight));
    }

    /// <summary>Boss Super Armor: chỉ khi cast skill hoặc shield active.</summary>
    private bool IsBossSuperArmor()
    {
        if (enemyScript == null || !enemyScript.isBoss) return false;
        if (enemyState   != null && enemyState.isCastingSkill) return true;
        if (bossMultiSkill != null && bossMultiSkill.isShielded) return true;
        return false;
    }

    private int BeginControl()
    {
        if (IsBossSuperArmor())
        {
            Debug.Log($"[EnemyCrowdControl] {gameObject.name} SUPER ARMOR — CC refused!");
            return -1;
        }

        // Huỷ landing mode nếu đang rơi — CC mới sẽ handle position
        _waitingForLanding = false;
        _fallVelocity      = 0f;
        _ccIsActive        = true;

        controlToken++;
        if (activeControlRoutine != null)
        {
            StopCoroutine(activeControlRoutine);
            activeControlRoutine = null;
        }

        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            agentWasStopped = navMeshAgent.isStopped;
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }

        if (enemyRigidbody != null)
        {
            rbWasKinematic = enemyRigidbody.isKinematic;
            enemyRigidbody.isKinematic = true;
            enemyRigidbody.linearVelocity  = Vector3.zero;
            enemyRigidbody.angularVelocity = Vector3.zero;
        }

        return controlToken;
    }

    /// <summary>
    /// Gọi khi routine CC kết thúc BÌNH THƯỜNG (không bị interrupt).
    /// Nếu enemy chưa chạm đất → chuyển sang landing gravity, KHÔNG resume agent sớm.
    /// </summary>
    private void EndControl(int token)
    {
        if (token != controlToken) return;

        activeControlRoutine = null;
        _ccIsActive = false;

        if (enemyRigidbody != null)
            enemyRigidbody.isKinematic = rbWasKinematic;

        // Quan trọng: chỉ resume NavMeshAgent khi enemy đã chạm đất.
        // Nếu còn trên không → BeginLanding() sẽ apply gravity và resume sau.
        if (!IsGrounded())
        {
            BeginLanding();
        }
        else
        {
            ResumeAgentAfterCC();
        }
    }

    // ── Gravity / Landing ────────────────────────────────────────────────────

    /// <summary>Khởi động chế độ rơi tự do sau CC.</summary>
    private void BeginLanding()
    {
        if (!enableFallbackGravity) { ResumeAgentAfterCC(); return; }
        _waitingForLanding = true;
        _fallVelocity = 0f;
        // Đảm bảo NavMeshAgent vẫn dừng trong khi rơi
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
    }

    /// <summary>Gọi mỗi Update khi _waitingForLanding == true.</summary>
    private void HandleLandingGravity()
    {
        if (IsGrounded())
        {
            SnapToGround();
            _waitingForLanding = false;
            _fallVelocity = 0f;
            ResumeAgentAfterCC();
            return;
        }

        // Tích lũy vận tốc rơi và dịch chuyển xuống
        _fallVelocity += gravityAcceleration * Time.deltaTime;
        Vector3 pos = transform.position;
        pos.y -= _fallVelocity * Time.deltaTime;
        transform.position = pos;

        // Kiểm tra lại sau khi di chuyển để không xuyên qua đất
        if (IsGrounded())
        {
            SnapToGround();
            _waitingForLanding = false;
            _fallVelocity = 0f;
            ResumeAgentAfterCC();
        }
    }

    /// <summary>Resume NavMeshAgent sau khi đã chắc chắn chạm đất.</summary>
    private void ResumeAgentAfterCC()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            // Warp agent về đúng vị trí hiện tại của transform để tránh tele
            navMeshAgent.Warp(transform.position);

            bool canResume = (enemyScript == null || enemyScript.alive);
            navMeshAgent.isStopped = canResume ? agentWasStopped : true;
        }
    }

    // ── Ground Check ─────────────────────────────────────────────────────────

    /// <summary>
    /// Kiểm tra enemy có đang chạm layer Ground không.
    /// Dùng SphereCast xuống từ pivot + offset.
    /// </summary>
    private bool IsGrounded()
    {
        if (groundLayer.value == 0)
        {
            // Nếu chưa assign layer → dùng Raycast xuống thô sơ
            return Physics.Raycast(transform.position + Vector3.up * 0.1f,
                                   Vector3.down, 0.1f + groundCheckOffset);
        }

        Vector3 origin = transform.position + Vector3.up * (groundCheckRadius + 0.05f);
        float dist = groundCheckRadius + groundCheckOffset + 0.05f;
        return Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out _, dist, groundLayer);
    }

    /// <summary>Snap transform về mặt đất gần nhất theo raycast.</summary>
    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;
        LayerMask mask = groundLayer.value != 0 ? groundLayer : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 6f, mask))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;
        }
    }

    // ── CC Routines ──────────────────────────────────────────────────────────

    private IEnumerator MoveRoutine(int token, Vector3 start, Vector3 end,
                                    float duration, float peakHeight)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            if (token != controlToken) yield break; // interrupted → Update/gravity sẽ lo

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float yOffset = 4f * peakHeight * t * (1f - t);

            Vector3 nextPos = Vector3.Lerp(start, end, t);
            nextPos.y = start.y + yOffset;
            transform.position = nextPos;

            yield return null;
        }

        // Snap về đất xác nhận
        Vector3 finalPos = end;
        finalPos.y = start.y;
        transform.position = finalPos;
        EndControl(token);
    }

    private IEnumerator TornadoRoutine(int token, Vector3 center, float radius,
                                       float totalRotationDegrees, float duration, float maxHeight)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        Vector3 start = transform.position;
        Vector3 offset = start - center;
        offset.y = 0f;
        if (offset.sqrMagnitude < 0.0001f) offset = transform.forward * radius;

        float startAngle = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
        float baseY      = start.y; // Y gốc cần trả về khi kết thúc
        float elapsed    = 0f;

        while (elapsed < safeDuration)
        {
            if (token != controlToken) yield break;

            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / safeDuration);
            float angle = startAngle + totalRotationDegrees * t;
            float rad   = angle * Mathf.Deg2Rad;
            // Parabolic height: lên đỉnh giữa chu kỳ rồi hạ xuống
            float yOffset = 4f * maxHeight * t * (1f - t);

            Vector3 horizontal = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            transform.position  = new Vector3(horizontal.x, baseY + yOffset, horizontal.z);

            yield return null;
        }

        // FIX: snap về baseY trước khi EndControl (tránh floating sau tornado)
        Vector3 finalPos = transform.position;
        finalPos.y = baseY;
        transform.position = finalPos;

        EndControl(token);
    }

    private IEnumerator KnockupRoutine(int token, Vector3 start, Vector3 end,
                                       float peakHeight, float riseDuration, float fallDuration)
    {
        float totalDuration = riseDuration + fallDuration;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (token != controlToken) yield break;

            elapsed += Time.deltaTime;
            float clamped     = Mathf.Min(elapsed, totalDuration);
            float horizontalT = clamped / totalDuration;

            float yOffset;
            if (clamped <= riseDuration)
            {
                float riseT = clamped / riseDuration;
                yOffset = Mathf.Lerp(0f, peakHeight, Mathf.SmoothStep(0f, 1f, riseT));
            }
            else
            {
                float fallT = (clamped - riseDuration) / fallDuration;
                yOffset = Mathf.Lerp(peakHeight, 0f, Mathf.SmoothStep(0f, 1f, fallT));
            }

            Vector3 nextPos = Vector3.Lerp(start, end, horizontalT);
            nextPos.y = start.y + yOffset;
            transform.position = nextPos;

            yield return null;
        }

        // Snap về Y gốc đảm bảo
        Vector3 finalPos = end;
        finalPos.y = start.y;
        transform.position = finalPos;
        EndControl(token);
    }
}
