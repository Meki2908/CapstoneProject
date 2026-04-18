using UnityEngine;
using Fusion;

/// <summary>
/// Cầu nối giữa TakeDamageTest (hệ thống nhận damage hiện tại) và WolfBossAI (Fusion).
/// Gắn script này lên cùng GameObject với TakeDamageTest trên Wolf Boss.
///
/// Nhiệm vụ:
///   - Forward sự kiện OnEnemyDied → WolfBossAI.OnBossDied()
///   - Forward các lần nhận damage → WolfBossAI.OnDamageTaken(amount)
///     (để Boss update NetworkedHp đồng bộ qua mạng)
///
/// Lưu ý: TakeDamageTest.currentHealth là private — đọc qua GetCurrentHealth().
/// </summary>
[RequireComponent(typeof(TakeDamageTest))]
public class WolfBossHealthBridge : MonoBehaviour
{
    // ── Inspector ───────────────────────────────────────────────────────────────────

    [Header("Links")]
    [Tooltip("WolfBossAI nằm trên Root object. Tự tìm nếu để trống.")]
    [SerializeField] private WolfBossAI bossAI;

    // ── Cached ──────────────────────────────────────────────────────────────────────────

    private TakeDamageTest _health;
    private float _lastKnownHp = -1f;

    // ── Lifecycle ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _health = GetComponent<TakeDamageTest>();

        if (bossAI == null)
        {
            bossAI = GetComponent<WolfBossAI>();
            if (bossAI == null) bossAI = GetComponentInParent<WolfBossAI>();
            if (bossAI == null) bossAI = GetComponentInChildren<WolfBossAI>();
        }

        if (bossAI == null)
            Debug.LogError("[WolfBossHealthBridge] Không tìm thấy WolfBossAI! Hãy gán vào Inspector.");

        if (_health == null)
            Debug.LogError("[WolfBossHealthBridge] Không tìm thấy TakeDamageTest!");
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnEnemyDied += HandleBossDied;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnEnemyDied -= HandleBossDied;
    }

    // ── Update — Poll HP changes ───────────────────────────────────────────────────────

    private void Update()
    {
        if (_health == null || bossAI == null) return;

        // Đọc HP qua public property (xem TakeDamageTest)
        float currentHp = _health.CurrentHealth;
        float maxHp     = _health.MaxHealth;

        if (Mathf.Approximately(currentHp, _lastKnownHp)) return;

        _lastKnownHp = currentHp;
        bossAI.OnDamageTaken(currentHp, maxHp);
    }

    // ── Handlers ────────────────────────────────────────────────────────────────────

    private void HandleBossDied()
    {
        if (bossAI != null)
            bossAI.OnBossDied();
    }
}
