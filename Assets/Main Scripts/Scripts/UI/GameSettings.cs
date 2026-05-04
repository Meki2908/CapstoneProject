using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Singleton DontDestroyOnLoad — lưu trữ và đồng bộ tất cả settings game qua mọi scene.
/// Các script khác đọc: GameSettings.Instance.MusicVolume, GameSettings.Instance.CameraMouseSpeed, v.v.
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    // ==================== EVENTS ====================
    /// <summary>
    /// Gọi khi bất kỳ setting nào thay đổi — các script khác listen để cập nhật
    /// </summary>
    public static event System.Action OnSettingsChanged;

    /// <summary>Gọi từ bên ngoài class (event không được Invoke trực tiếp ngoài type khai báo).</summary>
    public static void RaiseSettingsChanged() => OnSettingsChanged?.Invoke();

    // ==================== AUDIO ====================
    [Header("Audio")]
    public float masterVolume = 0.75f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    public int voiceLanguageIndex = 0;
    public bool backgroundSoundEnabled = true;

    // ==================== GRAPHICS ====================
    [Header("Graphics")]
    public float brightness = 0.5f;
    public bool saturationEnabled = true;
    public float contrast = 0.5f;
    public int screenResolutionIndex = 0;
    public int displayModeIndex = 0; // 0=Fullscreen, 1=Windowed, 2=Borderless
    public int frameRate = 60;
    public bool chromaticAberrationEnabled = false;
    public bool sharpeningEnabled = false;
    public int renderDistanceIndex = 3; // default 16x (index 3 in renderDistanceOptions)
    public int shadowQualityIndex = 2;   // 0=Off, 1=Low, 2=Medium, 3=High
    public int graphicsQualityIndex = 2; // 0=Low, 1=Medium, 2=High, 3=Ultra

    // Shadow Quality options
    public static readonly string[] shadowQualityOptions = { "Off", "Low", "Medium", "High" };
    // Graphics Quality options
    public static readonly string[] graphicsQualityOptions = { "Low", "Medium", "High", "Ultra" };

    // Render Distance options: 4x, 8x, 12x, 16x, 20x, 24x
    public static readonly int[] renderDistanceOptions = { 4, 8, 12, 16, 20, 24 };

    /// <summary>
    /// Multiplier áp dụng lên Camera.farClipPlane & ShadowDistance.
    /// 16x = 1.0 (default), 4x = 0.25, 24x = 1.5
    /// </summary>
    public float RenderDistanceMultiplier => renderDistanceOptions[renderDistanceIndex] / 16f;

    // ==================== GAMEPLAY ====================
    [Header("Gameplay")]
    public float cameraMouseSpeed = 0.5f;
    public float cameraZoomSpeed = 0.5f;
    public bool miniMapEnabled = true;

    // ==================== CONTROLS ====================
    [Header("Controls")]
    [HideInInspector] public string keyBindingsJson = ""; // Serialized key bindings

    // ==================== PlayerPrefs Keys ====================
    private const string KEY_MASTER_VOLUME = "Settings_MasterVolume";
    private const string KEY_MUSIC_VOLUME = "Settings_MusicVolume";
    private const string KEY_SFX_VOLUME = "Settings_SFXVolume";
    private const string KEY_VOICE_LANGUAGE = "Settings_VoiceLanguage";
    private const string KEY_BACKGROUND_SOUND = "Settings_BackgroundSound";

    private const string KEY_BRIGHTNESS = "Settings_Brightness";
    private const string KEY_SATURATION = "Settings_Saturation";
    private const string KEY_CONTRAST = "Settings_Contrast";
    private const string KEY_SCREEN_RESOLUTION = "Settings_ScreenResolution";
    private const string KEY_DISPLAY_MODE = "Settings_DisplayMode";
    private const string KEY_FRAME_RATE = "Settings_FrameRate";
    private const string KEY_CHROMATIC_ABERRATION = "Settings_ChromaticAberration";
    private const string KEY_SHARPENING = "Settings_Sharpening";
    private const string KEY_RENDER_DISTANCE = "Settings_RenderDistance";
    private const string KEY_SHADOW_QUALITY = "Settings_ShadowQuality";
    private const string KEY_GRAPHICS_QUALITY = "Settings_GraphicsQuality";

    private const string KEY_CAMERA_MOUSE_SPEED = "Settings_CameraMouseSpeed";
    private const string KEY_CAMERA_ZOOM_SPEED = "Settings_CameraZoomSpeedGameplay";
    private const string KEY_MINI_MAP = "Settings_MiniMap";

    // ==================== SINGLETON ====================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            LoadFromPlayerPrefs();
            ApplyAll();

            // Mỗi khi chuyển Scene → ép toàn bộ Canvas cập nhật lại
            SceneManager.sceneLoaded += OnSceneLoaded;

            Debug.Log("[GameSettings] Initialized — DontDestroyOnLoad");
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Khi Scene mới load → ép tất cả Canvas tính toán lại layout ngay lập tức
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Canvas.ForceUpdateCanvases();
        Debug.Log($"[GameSettings] Scene '{scene.name}' loaded → Canvas layout updated");
    }

    /// <summary>
    /// Đảm bảo Instance tồn tại — tự tạo nếu chưa có
    /// </summary>
    public static GameSettings EnsureInstance()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("[GameSettings]");
        Instance = go.AddComponent<GameSettings>();
        // Awake sẽ tự chạy DontDestroyOnLoad + LoadFromPlayerPrefs
        return Instance;
    }

    // ==================== LOAD / SAVE ====================

    /// <summary>
    /// Load tất cả settings từ PlayerPrefs
    /// </summary>
    public void LoadFromPlayerPrefs()
    {
        // Audio
        masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOLUME, 0.75f);
        musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 0.7f);
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 0.8f);
        voiceLanguageIndex = PlayerPrefs.GetInt(KEY_VOICE_LANGUAGE, 0);
        backgroundSoundEnabled = PlayerPrefs.GetInt(KEY_BACKGROUND_SOUND, 1) == 1;

        // Graphics
        brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 0.5f);
        saturationEnabled = PlayerPrefs.GetInt(KEY_SATURATION, 1) == 1;
        contrast = PlayerPrefs.GetFloat(KEY_CONTRAST, 0.5f);
        screenResolutionIndex = PlayerPrefs.GetInt(KEY_SCREEN_RESOLUTION, -1);
        // Mặc định 1920x1080 nếu chưa có setting
        if (screenResolutionIndex < 0)
            screenResolutionIndex = FindResolutionIndex(1920, 1080);
        displayModeIndex = PlayerPrefs.GetInt(KEY_DISPLAY_MODE, 0);
        frameRate = PlayerPrefs.GetInt(KEY_FRAME_RATE, 60);
        chromaticAberrationEnabled = PlayerPrefs.GetInt(KEY_CHROMATIC_ABERRATION, 0) == 1;
        sharpeningEnabled = PlayerPrefs.GetInt(KEY_SHARPENING, 0) == 1;
        renderDistanceIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(KEY_RENDER_DISTANCE, 3),
            0, renderDistanceOptions.Length - 1
        );
        shadowQualityIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(KEY_SHADOW_QUALITY, 2),
            0, shadowQualityOptions.Length - 1
        );
        graphicsQualityIndex = Mathf.Clamp(
            PlayerPrefs.GetInt(KEY_GRAPHICS_QUALITY, 2),
            0, graphicsQualityOptions.Length - 1
        );

        // Gameplay
        cameraMouseSpeed = PlayerPrefs.GetFloat(KEY_CAMERA_MOUSE_SPEED, 0.5f);
        cameraZoomSpeed = PlayerPrefs.GetFloat(KEY_CAMERA_ZOOM_SPEED, 0.5f);
        miniMapEnabled = PlayerPrefs.GetInt(KEY_MINI_MAP, 1) == 1;

        // === VALIDATE VALUES — tránh giá trị bị hỏng từ PlayerPrefs ===
        // Contrast phải trong range 0-1 (default 0.5)
        if (contrast < 0f || contrast > 1f)
        {
            Debug.LogWarning($"[GameSettings] Contrast PlayerPrefs bị sai ({contrast}), reset về 0.5");
            contrast = 0.5f;
        }
        // Brightness phải trong range 0-1 (default 0.5)
        if (brightness < 0f || brightness > 1f)
        {
            Debug.LogWarning($"[GameSettings] Brightness PlayerPrefs bị sai ({brightness}), reset về 0.5");
            brightness = 0.5f;
        }

        // Clamp resolution index
        var extRes = GetExtendedResolutions();
        if (extRes.Count > 0)
            screenResolutionIndex = Mathf.Clamp(screenResolutionIndex, 0, extRes.Count - 1);

        Debug.Log("[GameSettings] Loaded from PlayerPrefs");
    }

    /// <summary>
    /// Lưu tất cả settings vào PlayerPrefs
    /// </summary>
    public void SaveToPlayerPrefs()
    {
        // Audio
        PlayerPrefs.SetFloat(KEY_MASTER_VOLUME, masterVolume);
        PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, musicVolume);
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume);
        PlayerPrefs.SetInt(KEY_VOICE_LANGUAGE, voiceLanguageIndex);
        PlayerPrefs.SetInt(KEY_BACKGROUND_SOUND, backgroundSoundEnabled ? 1 : 0);

        // Graphics
        PlayerPrefs.SetFloat(KEY_BRIGHTNESS, brightness);
        PlayerPrefs.SetInt(KEY_SATURATION, saturationEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(KEY_CONTRAST, contrast);
        PlayerPrefs.SetInt(KEY_SCREEN_RESOLUTION, screenResolutionIndex);
        PlayerPrefs.SetInt(KEY_DISPLAY_MODE, displayModeIndex);
        PlayerPrefs.SetInt(KEY_FRAME_RATE, frameRate);
        PlayerPrefs.SetInt(KEY_CHROMATIC_ABERRATION, chromaticAberrationEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_SHARPENING, sharpeningEnabled ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RENDER_DISTANCE, renderDistanceIndex);
        PlayerPrefs.SetInt(KEY_SHADOW_QUALITY, shadowQualityIndex);
        PlayerPrefs.SetInt(KEY_GRAPHICS_QUALITY, graphicsQualityIndex);

        // Gameplay
        PlayerPrefs.SetFloat(KEY_CAMERA_MOUSE_SPEED, cameraMouseSpeed);
        PlayerPrefs.SetFloat(KEY_CAMERA_ZOOM_SPEED, cameraZoomSpeed);
        PlayerPrefs.SetInt(KEY_MINI_MAP, miniMapEnabled ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("[GameSettings] Saved to PlayerPrefs");
    }

    // ==================== APPLY ====================

    /// <summary>
    /// Apply tất cả settings vào game (gọi sau khi thay đổi)
    /// </summary>
    public void ApplyAll()
    {
        ApplyAudio();
        ApplyGraphics();
        ApplyGameplay();

        // Notify listeners
        OnSettingsChanged?.Invoke();
    }

    /// <summary>
    /// Apply + Save (gọi khi user nhấn Confirm)
    /// </summary>
    public void ApplyAndSave()
    {
        ApplyAll();
        SaveToPlayerPrefs();
    }

    private void ApplyAudio()
    {
        // Master Volume → AudioListener (ảnh hưởng tất cả audio)
        AudioListener.volume = masterVolume;

        // Music/SFX/Background Sound → AudioManager quản lý
        AudioManager.EnsureInstance();

        Debug.Log($"[GameSettings] Audio: Master={masterVolume:F2}, Music={musicVolume:F2}, SFX={sfxVolume:F2}");
    }

    private void ApplyGraphics()
    {
        // Frame Rate
        Application.targetFrameRate = frameRate;

        // Screen Resolution + Display Mode
        var resolutions = GetExtendedResolutions();
        if (resolutions.Count > 0 && screenResolutionIndex >= 0 && screenResolutionIndex < resolutions.Count)
        {
            var res = resolutions[screenResolutionIndex];
            FullScreenMode mode = displayModeIndex switch
            {
                0 => FullScreenMode.FullScreenWindow,
                1 => FullScreenMode.Windowed,
                2 => FullScreenMode.MaximizedWindow,
                _ => FullScreenMode.FullScreenWindow
            };
            Screen.SetResolution(res.x, res.y, mode);

            // Ép toàn bộ Canvas cập nhật lại layout ngay lập tức
            // Tránh UI bị lệch/bể trong 1-2 frame sau khi đổi Resolution
            Canvas.ForceUpdateCanvases();

            Debug.Log($"[GameSettings] Resolution: {res.x}x{res.y}, Mode={mode}");
        }

        // === GRAPHICS QUALITY ===
        // 0=Low, 1=Medium, 2=High, 3=Ultra
        ApplyGraphicsQuality();

        // === SHADOW QUALITY ===
        // 0=Off, 1=Low, 2=Medium, 3=High
        ApplyShadowQuality();

        // Post-processing: Brightness, Contrast, Saturation, ChromaticAberration, Sharpening
        // → PostProcessingSettings tự đọc qua OnSettingsChanged event
        PostProcessingSettings.EnsureInstance();

        // Render Distance → RenderDistanceController apply vào Camera.farClipPlane + ShadowDistance
        RenderDistanceController.EnsureInstance();

        Debug.Log($"[GameSettings] Graphics: Brightness={brightness:F2}, FPS={frameRate}, RenderDist={renderDistanceOptions[renderDistanceIndex]}x, Shadow={shadowQualityOptions[shadowQualityIndex]}, Quality={graphicsQualityOptions[graphicsQualityIndex]}");
    }

    /// <summary>
    /// Áp dụng Shadow Quality trực tiếp vào URP Pipeline Asset
    /// supportsMainLightShadows là read-only trong Unity 6 →
    /// dùng shadowDistance=0 để tắt shadow hiệu quả thay vì toggle property.
    /// </summary>
    private void ApplyShadowQuality()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        switch (shadowQualityIndex)
        {
            case 0: // Off — shadowDistance=0 khiến shadow không render
                if (urpAsset != null)
                {
                    urpAsset.shadowDistance = 0f;
                    urpAsset.mainLightShadowmapResolution = 256;
                    urpAsset.shadowCascadeCount = 1;
                }
                QualitySettings.shadowDistance = 0f;
                break;
            case 1: // Low — shadow thô, gần
                if (urpAsset != null)
                {
                    urpAsset.mainLightShadowmapResolution = 512;
                    urpAsset.shadowCascadeCount = 1;
                    urpAsset.shadowDistance = 30f;
                }
                QualitySettings.shadowDistance = 30f;
                break;
            case 2: // Medium — shadow mềm, trung bình
                if (urpAsset != null)
                {
                    urpAsset.mainLightShadowmapResolution = 1024;
                    urpAsset.shadowCascadeCount = 2;
                    urpAsset.shadowDistance = 80f;
                }
                QualitySettings.shadowDistance = 80f;
                break;
            case 3: // High — shadow mềm, xa, chi tiết
                if (urpAsset != null)
                {
                    urpAsset.mainLightShadowmapResolution = 2048;
                    urpAsset.shadowCascadeCount = 4;
                    urpAsset.shadowDistance = 150f;
                }
                QualitySettings.shadowDistance = 150f;
                break;
        }
        Debug.Log($"[GameSettings] Shadow Quality: {shadowQualityOptions[shadowQualityIndex]}, " +
                  $"Distance={urpAsset?.shadowDistance ?? 0}, Resolution={urpAsset?.mainLightShadowmapResolution ?? 0}");
    }

    /// <summary>
    /// Áp dụng Graphics Quality tổng thể — dùng cả QualitySettings (LOD, texture)
    /// và URP Pipeline Asset (MSAA, light count) để thật sự có tác dụng.
    /// KHÔNG gọi SetQualityLevel() — sẽ đổi URP Asset gốc → gây lỗi render.
    /// </summary>
    private void ApplyGraphicsQuality()
    {
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        switch (graphicsQualityIndex)
        {
            case 0: // Low — tối ưu cho máy yếu
                QualitySettings.lodBias = 0.5f;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.globalTextureMipmapLimit = 2;
                if (urpAsset != null)
                {
                    urpAsset.msaaSampleCount = 1; // MSAA off
                    urpAsset.maxAdditionalLightsCount = 1;
                }
                break;
            case 1: // Medium — cân bằng
                QualitySettings.lodBias = 1.0f;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.globalTextureMipmapLimit = 1;
                if (urpAsset != null)
                {
                    urpAsset.msaaSampleCount = 2; // MSAA 2x
                    urpAsset.maxAdditionalLightsCount = 2;
                }
                break;
            case 2: // High — đẹp, đòi hỏi máy khá
                QualitySettings.lodBias = 1.5f;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.globalTextureMipmapLimit = 0;
                if (urpAsset != null)
                {
                    urpAsset.msaaSampleCount = 4; // MSAA 4x
                    urpAsset.maxAdditionalLightsCount = 4;
                }
                break;
            case 3: // Ultra — max đồ họa
                QualitySettings.lodBias = 2.0f;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.globalTextureMipmapLimit = 0;
                if (urpAsset != null)
                {
                    urpAsset.msaaSampleCount = 8; // MSAA 8x
                    urpAsset.maxAdditionalLightsCount = 8;
                }
                break;
        }
        Debug.Log($"[GameSettings] Graphics Quality: {graphicsQualityOptions[graphicsQualityIndex]}, " +
                  $"LOD={QualitySettings.lodBias}, MSAA={urpAsset?.msaaSampleCount ?? 0}, " +
                  $"Lights={urpAsset?.maxAdditionalLightsCount ?? 0}");
    }

    private void ApplyGameplay()
    {
        // Camera speeds → CameraCursor + CameraZoom đọc qua OnSettingsChanged
        // MiniMap → MinimapCameraFollow đọc qua OnSettingsChanged
        Debug.Log($"[GameSettings] Gameplay: CamMouse={cameraMouseSpeed:F2}, MiniMap={miniMapEnabled}");
    }

    // ==================== CONVENIENCE METHODS ====================

    /// <summary>
    /// Reset tất cả về default
    /// </summary>
    public void ResetToDefaults()
    {
        masterVolume = 0.75f;
        musicVolume = 0.7f;
        sfxVolume = 0.8f;
        voiceLanguageIndex = 0;
        backgroundSoundEnabled = true;

        brightness = 0.5f;
        saturationEnabled = true;
        contrast = 0.5f;
        screenResolutionIndex = 0;
        displayModeIndex = 0;
        frameRate = 60;
        chromaticAberrationEnabled = false;
        sharpeningEnabled = false;
        renderDistanceIndex = 3; // 16x default
        shadowQualityIndex = 2;   // Medium
        graphicsQualityIndex = 2; // High

        cameraMouseSpeed = 0.5f;
        cameraZoomSpeed = 0.5f;
        miniMapEnabled = true;

        ApplyAndSave();
    }

    /// <summary>
    /// Tạo danh sách custom resolutions kết hợp với hệ thống để đảm bảo luôn có FullHD, 2K, 4K
    /// </summary>
    public static System.Collections.Generic.List<Vector2Int> GetExtendedResolutions()
    {
        var list = new System.Collections.Generic.List<Vector2Int>();
        foreach (var res in Screen.resolutions)
        {
            var v = new Vector2Int(res.width, res.height);
            if (!list.Contains(v)) list.Add(v);
        }
        var targets = new Vector2Int[] {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };
        foreach(var t in targets) {
            if (!list.Contains(t)) list.Add(t);
        }
        // Fallback safety
        if (list.Count == 0) list.Add(new Vector2Int(1920, 1080));
        // Sắp xếp độ phân giải tăng dần
        list.Sort((a, b) => (a.x * a.y).CompareTo(b.x * b.y));
        return list;
    }

    /// <summary>
    /// Tìm index của resolution gần nhất với target (mặc định 1920x1080)
    /// </summary>
    int FindResolutionIndex(int targetW, int targetH)
    {
        var resolutions = GetExtendedResolutions();
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].x == targetW && resolutions[i].y == targetH)
                return i;
        }
        // Fallback: lấy resolution cao nhất
        return Mathf.Max(0, resolutions.Count - 1);
    }
}
