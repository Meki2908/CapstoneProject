using UnityEngine;

/// <summary>
/// Helper tự động trả VFX instance về WolfBossVFXEvents pool khi hiệu ứng kết thúc.
/// Được gắn tự động bởi WolfBossVFXEvents.CreateInstance() — KHÔNG gắn tay.
///
/// Điều kiện trả pool (whichever comes first):
///   1. Tất cả ParticleSystem con đã dừng (IsAlive = false)
///   2. Đã tồn tại quá maxLifetime giây kể từ khi được kích hoạt
/// </summary>
[DisallowMultipleComponent]
public class PooledVFXReturn : MonoBehaviour
{
    // ── Được set bởi WolfBossVFXEvents.Init() ────────────────────────────
    private GameObject        _prefabKey;    // Key tra cứu trong pool dictionary
    private WolfBossVFXEvents _poolManager;  // Reference về pool manager
    private float             _maxLifetime;  // Thời gian sống tối đa (safety)

    // ── Runtime ──────────────────────────────────────────────────────────
    private ParticleSystem[] _particles;     // Tất cả PS trên object và children
    private float            _timer;         // Bộ đếm thời gian kể từ OnEnable

    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo các tham chiếu. Gọi ngay sau khi Instantiate bởi WolfBossVFXEvents.
    /// </summary>
    public void Init(GameObject prefabKey, WolfBossVFXEvents poolManager, float maxLifetime = 6f)
    {
        _prefabKey   = prefabKey;
        _poolManager = poolManager;
        _maxLifetime = maxLifetime;

        // Cache tất cả ParticleSystem (bao gồm children) một lần
        _particles = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
    }

    private void OnEnable()
    {
        _timer = 0f; // Reset bộ đếm mỗi lần object được kích hoạt từ pool
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Kiểm tra safety timeout trước
        if (_timer >= _maxLifetime)
        {
            ReturnNow();
            return;
        }

        // Chờ tất cả ParticleSystem dừng hoàn toàn
        if (_particles == null || _particles.Length == 0)
        {
            ReturnNow(); // Không có PS → trả ngay
            return;
        }

        foreach (var ps in _particles)
        {
            if (ps != null && ps.IsAlive(withChildren: true))
                return; // Còn particle đang sống → chờ tiếp
        }

        // Tất cả đã dừng → trả về pool
        ReturnNow();
    }

    private void ReturnNow()
    {
        _poolManager?.ReturnToPool(_prefabKey, gameObject);
    }
}
