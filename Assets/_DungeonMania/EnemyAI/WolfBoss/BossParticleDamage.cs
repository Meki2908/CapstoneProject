using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn lên GameObject có ParticleSystem để gây damage cho các object thuộc layer được chọn.
///
/// ═══ Setup bắt buộc ═══
/// Trên ParticleSystem phải bật:
///   Collision module → bật ON
///   Type            → World
///   Send Collision Messages → ✓ (BẮT BUỘC — script dùng OnParticleCollision)
///
/// ═══ Cách dùng ═══
///   • Gắn script lên ROOT object của VFX prefab (hoặc child có ParticleSystem).
///   • Trong Inspector chọn targetLayer = "Player" để VFX của Boss gây damage lên Player.
///   • Tuỳ chỉnh damagePerHit, damageCooldown theo từng loại VFX.
///
/// ═══ Damage flow ═══
///   OnParticleCollision(other) → kiểm tra layer → tìm PlayerHealth → TakeDamage()
///   Có cooldown per-target để tránh spam damage trong 1 frame nhiều particle cùng hit.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BossParticleDamage : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════

    [Header("═══ Target Layer ═══")]
    [Tooltip("Layer của object sẽ nhận damage khi particle va chạm.\n" +
             "Chọn 'Player' để VFX Boss gây damage lên player.\n" +
             "Có thể chọn nhiều layer bằng cách giữ Shift khi click.")]
    [SerializeField] private LayerMask targetLayer = 0;

    [Header("═══ Damage Settings ═══")]
    [Tooltip("Damage mỗi lần collision particle trúng target.")]
    [SerializeField] private float damagePerHit = 10f;

    [Tooltip("Thời gian chờ (giây) giữa 2 lần damage lên CÙNG 1 target.\n" +
             "Tránh trường hợp nhiều particle hit cùng lúc gây spam damage.")]
    [SerializeField] private float damageCooldown = 0.4f;

    [Tooltip("Nếu bật: chỉ damage 1 lần duy nhất mỗi lần particle system active.\n" +
             "Hữu ích cho VFX flash/explosion chỉ damage 1 lần.")]
    [SerializeField] private bool damageOncePerActivation = false;

    [Header("═══ Multiplier ═══")]
    [Tooltip("Nhân thêm damage, dùng để tăng damage theo phase (set từ code nếu cần).")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("═══ Feedback ═══")]
    [Tooltip("Không bắt buộc: Spawn thêm hit effect tại điểm va chạm khi damage player.")]
    [SerializeField] private GameObject hitEffectPrefab;

    [Tooltip("Thời gian tự destroy hit effect (giây).")]
    [SerializeField] private float hitEffectLifetime = 0.5f;

    [Header("═══ Debug ═══")]
    [SerializeField] private bool showDebugLog = false;

    // ═══════════════════════════════════════════════════════════════════════
    //  RUNTIME
    // ═══════════════════════════════════════════════════════════════════════

    // Cooldown map: key = target InstanceID, value = thời điểm lần cuối bị damage
    private readonly Dictionary<int, float> _cooldownMap = new Dictionary<int, float>(4);

    // Reusable list cho GetCollisionEvents (tránh GC alloc mỗi frame)
    private readonly List<ParticleCollisionEvent> _collisionEvents =
        new List<ParticleCollisionEvent>(16);

    private ParticleSystem _ps;
    private bool _hasDealtDamageThisActivation = false;

    // ═══════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        // Cảnh báo nếu Collision module chưa được cấu hình đúng
        if (_ps != null)
        {
            var col = _ps.collision;
            if (!col.enabled)
                Debug.LogWarning($"[ParticleDamage] '{gameObject.name}': " +
                                 "Collision module chưa được bật! " +
                                 "Bật Collision → ON trong ParticleSystem.");
            else if (!col.sendCollisionMessages)
                Debug.LogWarning($"[ParticleDamage] '{gameObject.name}': " +
                                 "'Send Collision Messages' chưa được bật! " +
                                 "Bật trong Collision module của ParticleSystem.");
        }

        // Cảnh báo nếu chưa chọn layer nào
        if (targetLayer.value == 0)
            Debug.LogWarning($"[ParticleDamage] '{gameObject.name}': " +
                             "targetLayer = Nothing — particle sẽ không gây damage cho ai. " +
                             "Chọn layer trong Inspector.");
    }

    private void OnEnable()
    {
        // Reset per-activation state
        _hasDealtDamageThisActivation = false;
        _cooldownMap.Clear();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PARTICLE COLLISION CALLBACK
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Unity gọi callback này khi một hoặc nhiều particle va chạm với collider của 'other'.
    /// Yêu cầu: Collision module ON + Send Collision Messages = true.
    /// </summary>
    private void OnParticleCollision(GameObject other)
    {
        // ── Kiểm tra layer ────────────────────────────────────────────────
        // Nếu layer của object bị hit KHÔNG có trong targetLayer mask → bỏ qua
        if ((targetLayer.value & (1 << other.layer)) == 0) return;

        // ── Kiểm tra chế độ one-shot ──────────────────────────────────────
        if (damageOncePerActivation && _hasDealtDamageThisActivation) return;

        // ── Kiểm tra cooldown per-target ──────────────────────────────────
        int targetID = other.GetInstanceID();
        if (_cooldownMap.TryGetValue(targetID, out float lastHitTime))
        {
            if (Time.time - lastHitTime < damageCooldown) return;
        }

        // ── Lấy collision events để biết điểm va chạm ─────────────────────
        int numEvents = ParticlePhysicsExtensions.GetCollisionEvents(_ps, other, _collisionEvents);
        Vector3 hitPoint = numEvents > 0 ? _collisionEvents[0].intersection : other.transform.position;

        // ── Tìm PlayerHealth trên object hoặc parent hierarchy ────────────
        var playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            // Tính damage cuối cùng
            float finalDamage = damagePerHit * damageMultiplier;

            // Gây damage
            playerHealth.TakeDamage(finalDamage, hitPoint);

            // Cập nhật trạng thái
            _cooldownMap[targetID] = Time.time;
            _hasDealtDamageThisActivation = true;

            // Spawn hit effect (tại điểm va chạm)
            if (hitEffectPrefab != null)
            {
                var fx = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
                Destroy(fx, hitEffectLifetime);
            }

            if (showDebugLog)
                Debug.Log($"[ParticleDamage] '{gameObject.name}' → '{other.name}' " +
                          $"| damage={finalDamage:F1} | hitPoint={hitPoint}");
            return; // Damage 1 target mỗi collision event batch
        }

        // ── Ghi cooldown ngay cả khi không tìm thấy PlayerHealth ─────────
        // Tránh tìm kiếm GetComponent liên tục mỗi frame cho target không có HP
        _cooldownMap[targetID] = Time.time;

        if (showDebugLog)
            Debug.Log($"[ParticleDamage] '{gameObject.name}' hit '{other.name}' " +
                      $"(layer={other.layer}) nhưng không có PlayerHealth.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  UPDATE — Dọn dẹp cooldown map
    // ═══════════════════════════════════════════════════════════════════════

    private void LateUpdate()
    {
        // Dọn cooldown cũ định kỳ để tránh dictionary phình vô hạn
        if (_cooldownMap.Count <= 8) return;

        var toRemove = new List<int>(4);
        float expireTime = damageCooldown * 4f;
        foreach (var kv in _cooldownMap)
        {
            if (Time.time - kv.Value > expireTime)
                toRemove.Add(kv.Key);
        }
        foreach (var key in toRemove)
            _cooldownMap.Remove(key);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Đặt damage multiplier từ code (VD: WolfBossAI tăng theo phase).</summary>
    public void SetDamageMultiplier(float multiplier) => damageMultiplier = multiplier;

    /// <summary>Override damage base từ code.</summary>
    public void SetDamagePerHit(float damage) => damagePerHit = damage;

    /// <summary>Override target layer từ code (VD: đổi layer khi phase thay đổi).</summary>
    public void SetTargetLayer(LayerMask layer) => targetLayer = layer;

    // ═══════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ═══════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Hiển thị thông tin damage trong label Scene View
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.3f,
            $"ParticleDamage\n" +
            $"dmg={damagePerHit:F0}×{damageMultiplier:F1}\n" +
            $"layer={LayerMaskToString(targetLayer)}\n" +
            $"cd={damageCooldown:F2}s");
    }

    private static string LayerMaskToString(LayerMask mask)
    {
        if (mask.value == 0) return "Nothing";
        if (mask.value == ~0) return "Everything";

        var layers = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            if ((mask.value & (1 << i)) != 0)
            {
                string name = UnityEngine.LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(name))
                {
                    if (layers.Length > 0) layers.Append(", ");
                    layers.Append(name);
                }
            }
        }
        return layers.Length > 0 ? layers.ToString() : $"mask={mask.value}";
    }
#endif
}
