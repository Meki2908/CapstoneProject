using UnityEngine;

/// <summary>
/// Điều khiển sương mù đơn giản bằng Unity RenderSettings fog.
/// Gắn script này vào bất kỳ GameObject nào trong scene (ví dụ: GameManager).
/// Tất cả settings đều chỉnh được trực tiếp trong Inspector, kể cả khi đang Play.
/// </summary>
public class SimpleFogController : MonoBehaviour
{
    [Header("Enable / Disable")]
    [Tooltip("Bật/tắt sương mù")]
    public bool enableFog = true;

    [Header("Fog Mode")]
    [Tooltip("Linear: tầm nhìn giảm đều theo khoảng cách StartDistance→EndDistance\n" +
             "Exponential: mờ dần nhanh theo Density\n" +
             "ExponentialSquared: mờ nhanh hơn nữa")]
    public FogMode fogMode = FogMode.Linear;

    [Header("Color")]
    [Tooltip("Màu sương mù — thường đặt gần màu skybox để trông tự nhiên")]
    public Color fogColor = new Color(0.5f, 0.5f, 0.55f, 1f);

    [Header("Linear Mode Settings")]
    [Tooltip("Khoảng cách bắt đầu có sương (camera → mét)")]
    public float startDistance = 30f;
    [Tooltip("Khoảng cách hoàn toàn bị che bởi sương")]
    public float endDistance   = 120f;

    [Header("Exponential Mode Settings")]
    [Tooltip("Mật độ sương — giá trị càng cao càng mờ (0.001 – 0.05 thường dùng)")]
    [Range(0f, 0.1f)]
    public float density = 0.015f;

    // ── Runtime apply mỗi frame để Inspector live-edit hoạt động ──
    void OnEnable()  => ApplyFog();
    void OnDisable() => RenderSettings.fog = false;

#if UNITY_EDITOR
    void OnValidate() => ApplyFog(); // cập nhật ngay trong Editor khi kéo slider
#endif

    void Update()
    {
        // Cập nhật liên tục để thay đổi Inspector có hiệu lực ngay lúc Play
        ApplyFog();
    }

    void ApplyFog()
    {
        RenderSettings.fog          = enableFog;
        if (!enableFog) return;

        RenderSettings.fogMode      = fogMode;
        RenderSettings.fogColor     = fogColor;
        RenderSettings.fogStartDistance = startDistance;
        RenderSettings.fogEndDistance   = endDistance;
        RenderSettings.fogDensity       = density;
    }

    // ── API để gọi từ code khác (event, quest, cutscene, …) ──

    /// <summary>Bật sương mù với fade-in tuỳ chọn.</summary>
    public void SetFogEnabled(bool value) { enableFog = value; ApplyFog(); }

    /// <summary>Đặt tầm nhìn tối đa (Linear mode).</summary>
    public void SetVisibilityRange(float start, float end)
    {
        startDistance = start;
        endDistance   = end;
    }

    /// <summary>Đặt mật độ (Exponential mode).</summary>
    public void SetDensity(float d) { density = Mathf.Clamp(d, 0f, 0.1f); }

    /// <summary>Đặt màu sương.</summary>
    public void SetFogColor(Color c) { fogColor = c; }
}
