using UnityEngine;
using Fusion;

public class BossFang : NetworkBehaviour
{
    // ── Loại Nanh ────────────────────────────────────────────────────────────

    public enum FangType { FireFang, IceFang }

    [Header("Fang Settings")]
    [SerializeField] private FangType fangType = FangType.FireFang;

    [Header("Death VFX")]
    [Tooltip("Prefab VFX hiện ra khi Fang bị tiêu diệt. Để trống nếu không cần.")]
    [SerializeField] private GameObject deathVFXPrefab;
    [Tooltip("Thời gian (giây) trước khi tự huỷ VFX. 0 = không tự huỷ (prefab tự xử lý).")]
    [SerializeField] private float deathVFXDuration = 3f;

    // ── Networked State ───────────────────────────────────────────────────────

    [Networked] public NetworkBool IsFangAlive { get; set; }

    // ── Runtime Refs ──────────────────────────────────────────────────────────

    // Được set bởi WolfBossAI khi spawn
    [HideInInspector] public WolfBossAI bossRef;

    public FangType Type => fangType;

    // ── TakeDamageTest integration ────────────────────────────────────────────

    private TakeDamageTest _health;

    // Local alive flag — dùng khi Fusion không active (standalone mode)
    private bool _localAlive = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Start(): xử lý standalone mode — khi không có Fusion session,
    /// Spawned() không bao giờ được gọi, nên ta subscribe health events ở đây.
    /// </summary>
    private void Start()
    {
        bool hasFusion = Runner != null && Object != null && Object.IsValid;
        if (!hasFusion)
        {
            // Standalone mode: Fusion không active
            _localAlive = true;
            SubscribeHealth();
            Debug.Log($"[BossFang] {fangType} — Standalone mode, subscribed health events via Start().");
        }
    }

    public override void Spawned()
    {
        IsFangAlive = true;
        _localAlive  = true;
        SubscribeHealth();
        Debug.Log($"[BossFang] {fangType} Spawned (Fusion).");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnsubscribeHealth();
    }

    private void OnDestroy()
    {
        // Đảm bảo unsubscribe trong mọi trường hợp (standalone Destroy)
        UnsubscribeHealth();
    }

    // ── Health subscription helpers ───────────────────────────────────────────

    private void SubscribeHealth()
    {
        _health = GetComponent<TakeDamageTest>();
        if (_health == null) _health = GetComponentInChildren<TakeDamageTest>();

        if (_health != null)
        {
            _health.OnEnemyDied -= OnFangKilled; // tránh double-subscribe
            _health.OnEnemyDied += OnFangKilled;
        }
        else
        {
            Debug.LogWarning($"[BossFang] {fangType}: TakeDamageTest không tìm thấy! " +
                             "Fang sẽ không báo death về Boss.");
        }
    }

    private void UnsubscribeHealth()
    {
        if (_health != null)
            _health.OnEnemyDied -= OnFangKilled;
    }

    /// <summary>
    /// Object Pooling: fang bị SetActive(false) thay vì Destroy/Despawn.
    /// </summary>
    private void OnDisable()
    {
        bool alreadyDead = (Runner != null && Object != null && Object.IsValid)
            ? !IsFangAlive
            : !_localAlive;

        if (alreadyDead) return; // Đã thông báo rồi, bỏ qua

        // Fusion mode: chỉ authority mới xử lý
        bool hasFusion = Runner != null && Object != null && Object.IsValid;
        if (hasFusion && !Object.HasStateAuthority) return;

        // Coi như chết khi bị pool
        OnFangKilled();
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    private void OnFangKilled()
    {
        // Kiểm tra đã chết chưa (tránh gọi 2 lần)
        bool hasFusion = Runner != null && Object != null && Object.IsValid;
        if (hasFusion)
        {
            if (!IsFangAlive) return;
            IsFangAlive  = false;
        }
        else
        {
            if (!_localAlive) return;
        }

        _localAlive = false;
        Debug.Log($"[BossFang] {fangType} destroyed!");

        // Spawn Death VFX tại vị trí Fang (trước khi GO bị pool/disable)
        SpawnDeathVFX();

        // Thông báo cho Boss
        if (bossRef != null)
            bossRef.OnFangDestroyed(this);

        // Unsubscribe để tránh double-call
        UnsubscribeHealth();
    }

    private void SpawnDeathVFX()
    {
        if (deathVFXPrefab == null) return;

        GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
        if (deathVFXDuration > 0f)
            Destroy(vfx, deathVFXDuration);
    }

    /// <summary>
    /// Gọi công khai từ bên ngoài nếu cần force-kill Fang (vd: despawn scene).
    /// </summary>
    public void ForceKill()
    {
        OnFangKilled();
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = fangType == FangType.FireFang ? Color.red : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
