using UnityEngine;

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
    public float brightness = 1.0f;
    public bool saturationEnabled = true;
    public float contrast = 50f;
    public int screenResolutionIndex = 0;
    public int displayModeIndex = 0; // 0=Fullscreen, 1=Windowed, 2=Borderless
    public int frameRate = 60;
    public bool chromaticAberrationEnabled = false;
    public bool sharpeningEnabled = false;

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
            Debug.Log("[GameSettings] Initialized — DontDestroyOnLoad");
        }
        else if (Instance != this)
        {
            // CHỈ xóa component — gameObject có thể là child của Canvas player
            Destroy(this);
        }
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
        brightness = PlayerPrefs.GetFloat(KEY_BRIGHTNESS, 1.0f);
        saturationEnabled = PlayerPrefs.GetInt(KEY_SATURATION, 1) == 1;
        contrast = PlayerPrefs.GetFloat(KEY_CONTRAST, 50f);
        screenResolutionIndex = PlayerPrefs.GetInt(KEY_SCREEN_RESOLUTION, 0);
        displayModeIndex = PlayerPrefs.GetInt(KEY_DISPLAY_MODE, 0);
        frameRate = PlayerPrefs.GetInt(KEY_FRAME_RATE, 60);
        chromaticAberrationEnabled = PlayerPrefs.GetInt(KEY_CHROMATIC_ABERRATION, 0) == 1;
        sharpeningEnabled = PlayerPrefs.GetInt(KEY_SHARPENING, 0) == 1;

        // Gameplay
        cameraMouseSpeed = PlayerPrefs.GetFloat(KEY_CAMERA_MOUSE_SPEED, 0.5f);
        cameraZoomSpeed = PlayerPrefs.GetFloat(KEY_CAMERA_ZOOM_SPEED, 0.5f);
        miniMapEnabled = PlayerPrefs.GetInt(KEY_MINI_MAP, 1) == 1;

        // === VALIDATE VALUES — tránh giá trị bị hỏng từ PlayerPrefs ===
        // Contrast phải trong range 0-100 (default 50 → URP contrast = 0)
        if (contrast < 0f || contrast > 100f)
        {
            Debug.LogWarning($"[GameSettings] Contrast PlayerPrefs bị sai ({contrast}), reset về 50");
            contrast = 50f;
        }
        // Brightness phải trong range 0-2 (default 1.0)
        if (brightness < 0f || brightness > 2f)
        {
            Debug.LogWarning($"[GameSettings] Brightness PlayerPrefs bị sai ({brightness}), reset về 1.0");
            brightness = 1.0f;
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
            Debug.Log($"[GameSettings] Resolution: {res.width}x{res.height}, Mode={mode}");
        }

        // Post-processing: Brightness, Contrast, Saturation, ChromaticAberration, Sharpening
        // → PostProcessingSettings tự đọc qua OnSettingsChanged event
        PostProcessingSettings.EnsureInstance();
        Debug.Log($"[GameSettings] Graphics: Brightness={brightness:F2}, FPS={frameRate}");
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

        brightness = 1.0f;
        saturationEnabled = true;
        contrast = 50f;
        screenResolutionIndex = 0;
        displayModeIndex = 0;
        frameRate = 60;
        chromaticAberrationEnabled = false;
        sharpeningEnabled = false;

        cameraMouseSpeed = 0.5f;
        cameraZoomSpeed = 0.5f;
        miniMapEnabled = true;

        ApplyAndSave();
    }
}
