using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        screenResolutionIndex = PlayerPrefs.GetInt(KEY_SCREEN_RESOLUTION, 0);
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
        int resCount = Screen.resolutions.Length;
        if (resCount > 0)
            screenResolutionIndex = Mathf.Clamp(screenResolutionIndex, 0, resCount - 1);

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
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions.Length > 0 && screenResolutionIndex >= 0 && screenResolutionIndex < resolutions.Length)
        {
            Resolution res = resolutions[screenResolutionIndex];
            FullScreenMode mode = displayModeIndex switch
            {
                0 => FullScreenMode.FullScreenWindow,
                1 => FullScreenMode.Windowed,
                2 => FullScreenMode.MaximizedWindow,
                _ => FullScreenMode.FullScreenWindow
            };
            Screen.SetResolution(res.width, res.height, mode);

            // Ép toàn bộ Canvas cập nhật lại layout ngay lập tức
            // Tránh UI bị lệch/bể trong 1-2 frame sau khi đổi Resolution
            Canvas.ForceUpdateCanvases();

            Debug.Log($"[GameSettings] Resolution: {res.width}x{res.height}, Mode={mode}");
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
        // Render Distance → FogController đọc qua OnSettingsChanged
        Debug.Log($"[GameSettings] Graphics: Brightness={brightness:F2}, FPS={frameRate}, RenderDist={renderDistanceOptions[renderDistanceIndex]}x, Shadow={shadowQualityOptions[shadowQualityIndex]}, Quality={graphicsQualityOptions[graphicsQualityIndex]}");
    }

    /// <summary>
    /// Áp dụng Shadow Quality vào URP pipeline
    /// </summary>
    private void ApplyShadowQuality()
    {
        switch (shadowQualityIndex)
        {
            case 0: // Off — tắt hoàn toàn bóng đổ
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowDistance = 0f;
                break;
            case 1: // Low — bóng đổ thô, gần
                QualitySettings.shadows = ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = 30f;
                QualitySettings.shadowCascades = 1;
                break;
            case 2: // Medium — bóng đổ mềm, trung bình
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowDistance = 80f;
                QualitySettings.shadowCascades = 2;
                break;
            case 3: // High — bóng đổ mềm, xa, chi tiết
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.High;
                QualitySettings.shadowDistance = 150f;
                QualitySettings.shadowCascades = 4;
                break;
        }
        Debug.Log($"[GameSettings] Shadow Quality: {shadowQualityOptions[shadowQualityIndex]}, Distance={QualitySettings.shadowDistance}");
    }

    /// <summary>
    /// Áp dụng Graphics Quality tổng thể
    /// </summary>
    private void ApplyGraphicsQuality()
    {
        // KHÔNG gọi SetQualityLevel() — nó sẽ đổi URP Render Pipeline Asset
        // → gây mất texture, vật thể tàng hình nếu project chưa cấu hình đủ Quality Level
        // Chỉ chỉnh từng thông số riêng lẻ, giữ nguyên URP Asset gốc

        switch (graphicsQualityIndex)
        {
            case 0: // Low — tối ưu cho máy yếu
                QualitySettings.lodBias = 0.5f;
                QualitySettings.pixelLightCount = 1;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.antiAliasing = 0;
                QualitySettings.globalTextureMipmapLimit = 2; // texture thấp
                break;
            case 1: // Medium — cân bằng
                QualitySettings.lodBias = 1.0f;
                QualitySettings.pixelLightCount = 2;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                QualitySettings.antiAliasing = 2;
                QualitySettings.globalTextureMipmapLimit = 1; // texture trung bình
                break;
            case 2: // High — đẹp, đòi hỏi máy khá
                QualitySettings.lodBias = 1.5f;
                QualitySettings.pixelLightCount = 4;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.antiAliasing = 4;
                QualitySettings.globalTextureMipmapLimit = 0; // texture full
                break;
            case 3: // Ultra — max đồ họa
                QualitySettings.lodBias = 2.0f;
                QualitySettings.pixelLightCount = 8;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                QualitySettings.antiAliasing = 8;
                QualitySettings.globalTextureMipmapLimit = 0; // texture full
                break;
        }
        Debug.Log($"[GameSettings] Graphics Quality: {graphicsQualityOptions[graphicsQualityIndex]}, LOD={QualitySettings.lodBias}, AA={QualitySettings.antiAliasing}");
    }

    private void ApplyGameplay()
    {
        // Camera speeds → RTSCameraController đọc qua OnSettingsChanged
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
}
