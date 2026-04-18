using UnityEngine;

/// <summary>
/// Tự động Destroy VFX instance khi hiệu ứng kết thúc.
///
/// Điều kiện Destroy (whichever comes first):
///   1. Tất cả ParticleSystem con đã dừng (IsAlive = false)
///   2. Đã tồn tại quá maxLifetime giây kể từ khi được kích hoạt
/// </summary>
[DisallowMultipleComponent]
public class PooledVFXReturn : MonoBehaviour
{
    private float            _maxLifetime = 6f;
    private ParticleSystem[] _particles;
    private float            _timer;

    /// <summary>
    /// Khởi tạo. Gọi ngay sau khi Instantiate bởi WolfBossVFXEvents nếu muốn tuỳ chỉnh lifetime.
    /// Nếu không gọi Init(), lifetime mặc định 6s sẽ được dùng.
    /// </summary>
    public void Init(GameObject prefabKey, WolfBossVFXEvents poolManager, float maxLifetime = 6f)
    {
        // prefabKey và poolManager không còn dùng (đã bỏ pooling)
        // Giữ signature để tránh compile error nếu có nơi khác gọi Init()
        _maxLifetime = maxLifetime;
        _particles   = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
    }

    private void OnEnable()
    {
        _particles = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Safety timeout
        if (_timer >= _maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Chờ tất cả ParticleSystem dừng hoàn toàn
        if (_particles == null || _particles.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        foreach (var ps in _particles)
        {
            if (ps != null && ps.IsAlive(withChildren: true))
                return; // Còn particle đang sống → chờ tiếp
        }

        // Tất cả đã dừng → Destroy
        Destroy(gameObject);
    }
}
