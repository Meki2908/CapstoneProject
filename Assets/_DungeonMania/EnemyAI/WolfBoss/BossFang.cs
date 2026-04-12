using UnityEngine;
using Fusion;

/// <summary>
/// Gắn lên Wolf Fang prefab (Fire Fang và Ice Fang).
/// Khi Fang bị phá hủy hoặc SetActive(false) (Object Pooling),
/// script sẽ báo về WolfBossAI để kiểm tra điều kiện Stun.
///
/// Setup: Gắn script này lên "Wolf fang flame.prefab" và "Wolf fang Ice.prefab".
/// Gán tham chiếu WolfBossAI qua code (được set khi Boss spawn Fang).
/// </summary>
public class BossFang : NetworkBehaviour
{
    // ── Loại Nanh ────────────────────────────────────────────────────────────

    public enum FangType { FireFang, IceFang }

    [Header("Fang Settings")]
    [SerializeField] private FangType fangType = FangType.FireFang;

    // ── Networked State ───────────────────────────────────────────────────────

    [Networked] public NetworkBool IsFangAlive { get; set; }

    // ── Runtime Refs ──────────────────────────────────────────────────────────

    // Được set bởi WolfBossAI khi spawn
    [HideInInspector] public WolfBossAI bossRef;

    public FangType Type => fangType;

    // ── TakeDamageTest integration ────────────────────────────────────────────

    private TakeDamageTest _health;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Spawned()
    {
        IsFangAlive = true;

        // Subscribe vào sự kiện chết từ TakeDamageTest
        _health = GetComponent<TakeDamageTest>();
        if (_health == null) _health = GetComponentInChildren<TakeDamageTest>();

        if (_health != null)
            _health.OnEnemyDied += OnFangKilled;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_health != null)
            _health.OnEnemyDied -= OnFangKilled;
    }

    /// <summary>
    /// Dành cho hệ thống Object Pooling — gọi khi Fang bị ẩn đi thay vì Despawn.
    /// </summary>
    private void OnDisable()
    {
        if (!IsFangAlive) return; // Đã thông báo rồi

        // Nếu Fang bị tắt giữa chừng (pooling) → coi như chết
        if (Runner != null && Object != null && Object.HasStateAuthority)
            OnFangKilled();
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    private void OnFangKilled()
    {
        if (!IsFangAlive) return; // Tránh gọi 2 lần

        IsFangAlive = false;
        Debug.Log($"[BossFang] {fangType} destroyed!");

        // Thông báo cho Boss
        if (bossRef != null)
            bossRef.OnFangDestroyed(this);
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
