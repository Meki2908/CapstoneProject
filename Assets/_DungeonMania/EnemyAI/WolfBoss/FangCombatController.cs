using UnityEngine;

/// <summary>
/// Quản lý combat tự động cho Fang (Wolf fang flame / Wolf fang Ice).
/// Vì Fang không có animation, script này tự động đếm timer và kích hoạt VFX/Damage.
///
/// ═══ Logic ═══
///   1. Tự động kiểm tra Player trong tầm mỗi frame.
///   2. Cứ sau khoảng [attackInterval] giây, nếu có Player trong tầm -> kích hoạt đòn đánh.
///   3. Ưu tiên đòn kết hợp: Nếu cả 2 Fang còn sống, đòn tấn công sẽ là Combined Attack
///      (do Fire Fang điều phối để tránh trùng lặp VFX).
///
/// ═══ Setup ═══
///   • Gắn script này lên ROOT của mỗi Fang prefab.
/// </summary>
public class FangCombatController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("═══ References ═══")]
    [Tooltip("BossFang component trên cùng GO hoặc parent. Tự tìm nếu để trống.")]
    [SerializeField] private BossFang bossFang;

    [Tooltip("WolfBossVFXEvents trên Boss Root. Tự tìm nếu để trống.")]
    [SerializeField] private WolfBossVFXEvents vfxEvents;

    [Header("═══ Combat Settings ═══")]
    [Tooltip("Khoảng thời gian giữa các đòn đánh (seconds).")]
    [SerializeField] private float attackInterval = 5f;

    [Tooltip("Độ lệch ngẫu nhiên của timer (seconds) để tránh các đòn đánh quá đều nhau.")]
    [SerializeField] private float intervalRandomness = 0.5f;

    [Tooltip("Tầm kiểm tra Player để kích hoạt đòn đánh.")]
    [SerializeField] private float detectionRadius = 6f;

    [Tooltip("Layer mask để detect player.")]
    [SerializeField] private LayerMask playerLayer = ~0;

    // ── Runtime ───────────────────────────────────────────────────────────

    private float _nextAttackTime;
    private bool  _isInitialized = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Start()
    {
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
            Debug.LogWarning($"[FangCombatController] Thiếu references trên {gameObject.name}! Script sẽ không chạy.");
            return;
        }

        // ── Đăng ký với hệ thống manager ──────────────────────────────────
        vfxEvents.RegisterFang(bossFang);
        
        // Setup timer lần đầu (tặng thêm một chút delay ngẫu nhiên lúc vừa xuất hiện)
        _nextAttackTime = Time.time + Random.Range(1f, 3f);
        _isInitialized = true;
    }

    private void Update()
    {
        if (!_isInitialized) return;

        // Chỉ xử lý khi đến thời điểm tấn công
        if (Time.time < _nextAttackTime) return;

        // Phải có Player trong tầm mới thực hiện "vfx 1 lần"
        if (IsPlayerInRange())
        {
            ExecuteAttack();
            ResetAttackTimer();
        }
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
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  COMBAT LOGIC
    // ═══════════════════════════════════════════════════════════════════════

    private bool IsPlayerInRange()
    {
        // Kiểm tra xem có bất kỳ object nào thuộc layer Player trong bán kính detectionRadius
        return Physics.CheckSphere(transform.position, detectionRadius, playerLayer);
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

        // ── KIỂM TRA ĐÒN KẾT HỢP (COMBINED ATTACK) ───────────────────────
        // Nếu cả 2 nanh còn sống, ta gộp lại thành 1 đòn mạnh (Combined).
        // Để tránh việc cả 2 cùng gọi Combined VFX trùng nhau, ta quy tắc:
        // CHỈ Fire Fang mới được gọi lệnh Combined.
        if (vfxEvents.BothFangsAlive())
        {
            if (bossFang.Type == BossFang.FangType.FireFang)
            {
                // Solo Master: Fire Fang gọi đòn kết hợp
                vfxEvents.SpawnCombinedFangVFX();
                Debug.Log("[FangCombatController] Cả 2 nanh còn sống → Execute COMBINED Attack (Fire Fang master).");
                return;
            }
            else
            {
                // Ice Fang: Nếu Fire Fang còn sống, Ice Fang sẽ không tự bắn solo (để chừa chỗ cho Combined)
                // Nó chỉ đơn giản là Reset timer để chờ đợt sau.
                Debug.Log("[FangCombatController] Ice Fang nhường Fire Fang gọi đòn kết hợp.");
                return;
            }
        }

        // ── ĐÒN ĐÁNH ĐƠN LẺ (AUTO ATTACK) ────────────────────────────────
        // Chạy khi chỉ còn 1 nanh duy nhất trên sân.
        if (bossFang.Type == BossFang.FangType.FireFang)
            vfxEvents.SpawnFireFangAutoVFX();
        else
            vfxEvents.SpawnIceFangAutoVFX();
            
        Debug.Log($"[FangCombatController] Tấn công đơn lẻ: {bossFang.Type}.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ═══════════════════════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        // Hiển thị tầm detection trong Scene View (màu vàng)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
