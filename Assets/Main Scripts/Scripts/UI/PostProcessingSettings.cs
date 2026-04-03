using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Kết nối GameSettings (UI) → URP Volume Profile runtime.
/// Tạo Volume riêng (priority cao nhất) để không ảnh hưởng đến Fog/Zoom volumes.
/// DontDestroyOnLoad — tự apply lại khi chuyển scene.
/// 
/// Settings được áp dụng:
/// - Brightness   → ColorAdjustments.postExposure
/// - Contrast     → ColorAdjustments.contrast
/// - Saturation   → ColorAdjustments.saturation (ON=0, OFF=-100)
/// - Chromatic Aberration → ChromaticAberration.intensity
/// - Sharpening   → Thay đổi FSR upscaling trên URP Asset
/// </summary>
public class PostProcessingSettings : MonoBehaviour
{
    public static PostProcessingSettings Instance { get; private set; }

    // Volume riêng cho settings (không dùng chung với fog)
    private Volume _settingsVolume;
    private VolumeProfile _settingsProfile;
    private ColorAdjustments _colorAdjustments;
    private ChromaticAberration _chromaticAberration;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    private void Start()
    {
        CreateSettingsVolume();
        ApplySettings();
        GameSettings.OnSettingsChanged += ApplySettings;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        GameSettings.OnSettingsChanged -= ApplySettings;
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Cleanup runtime profile
        if (_settingsProfile != null)
            DestroyImmediate(_settingsProfile);

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-apply settings khi chuyển scene (đảm bảo Volume vẫn hoạt động)
        ApplySettings();
    }

    // ==================== SETUP ====================

    /// <summary>
    /// Tạo Volume riêng với priority 999 — đè lên tất cả volumes khác trong scene.
    /// Dùng profile runtime (không sửa asset gốc).
    /// </summary>
    private void CreateSettingsVolume()
    {
        // Tạo Volume component trên chính GameObject này
        _settingsVolume = gameObject.AddComponent<Volume>();
        _settingsVolume.isGlobal = true;
        _settingsVolume.priority = 999f; // Cao nhất — đè lên Fog volumes

        // Tạo profile runtime (không ảnh hưởng asset trên disk)
        _settingsProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        _settingsProfile.name = "Settings_Runtime";
        _settingsVolume.profile = _settingsProfile;

        // Thêm overrides
        _colorAdjustments = _settingsProfile.Add<ColorAdjustments>(false);
        _chromaticAberration = _settingsProfile.Add<ChromaticAberration>(false);

        Debug.Log("[PostProcessing] Settings Volume created (priority=999)");
    }

    // ==================== APPLY ====================

    /// <summary>
    /// Đọc GameSettings và apply vào URP Volume overrides.
    /// Gọi khi: Start, OnSettingsChanged, OnSceneLoaded.
    /// </summary>
    private void ApplySettings()
    {
        if (GameSettings.Instance == null) return;
        var gs = GameSettings.Instance;

        ApplyColorAdjustments(gs);
        ApplyChromaticAberration(gs);
        ApplySharpening(gs);

        Debug.Log($"[PostProcessing] Applied: Brightness={gs.brightness:F2}, Contrast={gs.contrast:F0}, " +
                  $"Saturation={gs.saturationEnabled}, ChromAb={gs.chromaticAberrationEnabled}, Sharpen={gs.sharpeningEnabled}");
    }

    /// <summary>
    /// Brightness + Contrast + Saturation → ColorAdjustments
    /// </summary>
    private void ApplyColorAdjustments(GameSettings gs)
    {
        if (_colorAdjustments == null) return;

        // === BRIGHTNESS ===
        // Slider range: 0.0 → 1.0 (default 0.5)
        // postExposure range safe limit: -1.5 → +1.5 (vừa phải, không làm màn hình cháy sáng/tối thui)
        // Mapping: slider 0→-1.5, 0.5→0, 1.0→+1.5
        _colorAdjustments.postExposure.overrideState = true;
        _colorAdjustments.postExposure.value = (gs.brightness - 0.5f) * 3f;

        // === CONTRAST ===
        // Slider range: 0.0 → 1.0 (default 0.5)
        // URP contrast safe limit: -30 → +30 (để -100 màn hình sẽ thành sương xám xịt)
        // Mapping: slider 0→-30, 0.5→0, 1.0→+30
        _colorAdjustments.contrast.overrideState = true;
        _colorAdjustments.contrast.value = (gs.contrast - 0.5f) * 60f;

        // === SATURATION ===
        // Toggle ON/OFF
        // ON = saturation 0 (bình thường)
        // OFF = saturation -100 (đen trắng)
        _colorAdjustments.saturation.overrideState = true;
        _colorAdjustments.saturation.value = gs.saturationEnabled ? 0f : -100f;
    }

    /// <summary>
    /// Chromatic Aberration ON/OFF
    /// </summary>
    private void ApplyChromaticAberration(GameSettings gs)
    {
        if (_chromaticAberration == null) return;

        _chromaticAberration.intensity.overrideState = true;
        _chromaticAberration.intensity.value = gs.chromaticAberrationEnabled ? 0.5f : 0f;
    }

    /// <summary>
    /// Sharpening — dùng URP Render Scale + FSR upscaling.
    /// ON: renderScale=0.95 + FSR (tạo hiệu ứng sắc nét)
    /// OFF: renderScale=1.0 (bình thường)
    /// </summary>
    private void ApplySharpening(GameSettings gs)
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null) return;

        if (gs.sharpeningEnabled)
        {
            urpAsset.renderScale = 0.95f;
            urpAsset.upscalingFilter = UpscalingFilterSelection.FSR;
        }
        else
        {
            urpAsset.renderScale = 1.0f;
            urpAsset.upscalingFilter = UpscalingFilterSelection.Auto;
        }
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Đảm bảo Instance tồn tại — gọi từ scene bất kỳ
    /// </summary>
    public static PostProcessingSettings EnsureInstance()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("[PostProcessingSettings]");
        Instance = go.AddComponent<PostProcessingSettings>();
        return Instance;
    }
}
