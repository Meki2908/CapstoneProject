using UnityEngine;

public class FangCombatController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("═══ References ═══")]
    [Tooltip("BossFang component trên cùng GO hoặc parent. Tự tìm nếu để trống.")]
    [SerializeField] private BossFang bossFang;

    [Tooltip("WolfBossVFXEvents trên Boss Root. Tự tìm nếu để trống.")]
    [SerializeField] private WolfBossVFXEvents vfxEvents;

    [Header("═══ Combat Settings ═══")]
    [Tooltip("Khoảng thời gian giữa các đòn đánh cá nhân (seconds).")]
    [SerializeField] private float attackInterval = 5f;

    [Tooltip("Độ lệch ngẫu nhiên của timer (seconds) để tránh các đòn đánh quá đều nhau.")]
    [SerializeField] private float intervalRandomness = 0.5f;

    [Tooltip("Cooldown dùng chung cho đòn kết hợp (seconds). Reset khi game bắt đầu.")]
    [SerializeField] private float combinedAttackCooldown = 30f;

    [Tooltip("Tầm kiểm tra Player để kích hoạt đòn đánh.")]
    [SerializeField] private float detectionRadius = 6f;

    [Tooltip("Layer mask để detect player.")]
    [SerializeField] private LayerMask playerLayer = ~0;

    // ── Runtime ───────────────────────────────────────────────────────────

    private float _nextAttackTime;
    private bool  _isInitialized = false;

    // Cooldown dùng chung (static) giữa 2 Fang — đảm bảo chỉ 1 Combined mỗi 30s.
    // Reset về 0 khi scene/play mode bắt đầu để lần Combined đầu tiên xảy ra sớm.
    private static float _nextCombinedTime = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Start()
    {
        TryInitialize();
    }

    private void OnEnable()
    {
        // Quan trọng cho pooling/Fusion reuse:
        // object có thể bị disable/enable nhiều lần, nên cần đăng ký lại mỗi lần bật.
        TryInitialize();
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            // Retry nhẹ trong trường hợp Start/OnEnable chạy trước khi VFX manager xuất hiện.
            TryInitialize();
            return;
        }

        // Chỉ xử lý khi đến thời điểm tấn công
        if (Time.time < _nextAttackTime) return;

        // Phải có Player trong tầm mới thực hiện tấn công
        // ExecuteAttack() tự gọi ResetAttackTimer() bên trong
        if (IsPlayerInRange())
            ExecuteAttack();
    }

    private void OnDestroy()
    {
        if (bossFang != null && vfxEvents != null)
            vfxEvents.UnregisterFang(bossFang);
    }

    private void OnDisable()
    {
        if (bossFang != null && vfxEvents != null)
            vfxEvents.UnregisterFang(bossFang);
        _isInitialized = false;
    }

    private void TryInitialize()
    {
        if (_isInitialized) return;

        // ── Tìm references ────────────────────────────────────────────────
        if (bossFang == null)
        {
            bossFang = GetComponent<BossFang>();
            if (bossFang == null) bossFang = GetComponentInParent<BossFang>();
        }

        if (vfxEvents == null)
            vfxEvents = FindFirstObjectByType<WolfBossVFXEvents>();

        if (vfxEvents == null || bossFang == null)
        {
            return;
        }

        // Đăng ký lại mỗi lần được bật (an toàn vì RegisterFang chỉ set transform theo type).
        vfxEvents.RegisterFang(bossFang);

        // Setup timer lần đầu cho vòng sống hiện tại.
        // Stagger nhẹ để Fire và Ice không bắn cùng lúc.
        float stagger = bossFang.Type == BossFang.FangType.FireFang ? 0f : attackInterval * 0.5f;
        _nextAttackTime = Time.time + Random.Range(1f, 3f) + stagger;
        _isInitialized = true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  COMBAT LOGIC
    // ═══════════════════════════════════════════════════════════════════════

    private bool IsPlayerInRange()
    {
        // Tầng 1: LayerMask check (nhanh, ưu tiên)
        if (playerLayer.value != 0 &&
            Physics.CheckSphere(transform.position, detectionRadius, playerLayer))
            return true;

        // Tầng 2: Fallback tìm bằng tag "Player" (đảm bảo không miss do layer set sai)
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) return false;
        return Vector3.Distance(transform.position, playerGO.transform.position) <= detectionRadius;
    }

    private void ResetAttackTimer()
    {
        // Tính toán thời điểm tiếp theo
        float randomOffset = Random.Range(-intervalRandomness, intervalRandomness);
        _nextAttackTime = Time.time + attackInterval + randomOffset;
    }

    private void ExecuteAttack()
    {
        if (vfxEvents == null || bossFang == null) return;

        bool combinedReady = Time.time >= _nextCombinedTime;
        bool bothAlive     = vfxEvents.BothFangsAlive();

        // ── ƯU TIÊN 1: ĐÒN KẾT HỢP (COMBINED ATTACK) — mỗi 30s ───────────
        // Chỉ kích hoạt khi CD đã hồi VÀ cả 2 nanh còn sống.
        // Nếu CD hồi nhưng thiếu 1 nanh → KHÔNG consume CD, chờ cơ hội sau.
        if (combinedReady && bothAlive)
        {
            if (bossFang.Type == BossFang.FangType.FireFang)
            {
                // FireFang là master — trigger combined & consume CD dùng chung.
                _nextCombinedTime = Time.time + combinedAttackCooldown;
                vfxEvents.SpawnCombinedFangVFX();
                Debug.Log($"[FangCombatController] CD hồi → COMBINED Attack! CD tiếp theo sau {combinedAttackCooldown}s.");
            }
            else
            {
                // IceFang nhường — FireFang sẽ tự trigger khi đến turn của nó.
                Debug.Log("[FangCombatController] IceFang nhường — chờ FireFang trigger Combined.");
            }
            ResetAttackTimer();
            return;
        }

        // ── ƯU TIÊN 2: ĐÒN ĐƠN LẺ (SOLO ATTACK) ─────────────────────────
        // Mỗi Fang dùng skill của riêng mình khi CD kết hợp chưa hồi,
        // hoặc khi chỉ còn 1 nanh sống.
        if (bossFang.Type == BossFang.FangType.FireFang)
        {
            vfxEvents.SpawnFireFangAutoVFX();
            Debug.Log("[FangCombatController] FireFang solo attack.");
        }
        else
        {
            vfxEvents.SpawnIceFangAutoVFX();
            Debug.Log("[FangCombatController] IceFang solo attack.");
        }

        ResetAttackTimer();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ═══════════════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        bool playerDetected = false;

#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            // Tầng 1: CheckSphere theo layer
            if (playerLayer.value != 0 &&
                Physics.CheckSphere(transform.position, detectionRadius, playerLayer))
                playerDetected = true;

            // Tầng 2: Tag fallback
            if (!playerDetected)
            {
                var playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null &&
                    Vector3.Distance(transform.position, playerGO.transform.position) <= detectionRadius)
                    playerDetected = true;
            }
        }
#endif

        // Màu đỏ = Player trong tầm (sẽ tấn công)
        // Màu vàng = ngoài tầm
        Gizmos.color = playerDetected
            ? new Color(1f, 0.1f, 0.1f, 0.6f)
            : new Color(1f, 1f, 0f, 0.3f);

        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Vẽ thêm vòng solid nhỏ ở tâm để dễ thấy vị trí Fang
        Gizmos.color = playerDetected
            ? new Color(1f, 0.1f, 0.1f, 0.8f)
            : new Color(1f, 1f, 0f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.15f);
    }
}
