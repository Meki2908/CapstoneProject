using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Singleton DontDestroyOnLoad — khóa/hiện chuột xuyên scene (Menu → Map → Dungeon).
/// Tôn trọng <see cref="CursorUIPriority"/> (inventory, pause, reward...) — không ép lock khi UI đang giữ overlay.
/// Phím Alt: nếu đã có <see cref="MovementSystem.CameraCursor"/> thì để CameraCursor xử lý, tránh double-toggle.
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class MouseLockManager : MonoBehaviour
{
    public static MouseLockManager Instance { get; private set; }

    [Header("Scenes — cursor tự do (menu / lobby)")]
    [Tooltip("Scene có menu: chuột hiện, không lock. Các scene khác = gameplay (lock).")]
    [SerializeField] private string[] menuSceneNames = new[]
    {
        "UI_Game",
        "Menu_Game",
        "DemoSceneSettings"
    };

    [Header("Behaviour")]
    [SerializeField] private bool applyLockOnSceneLoad = true;
    [Tooltip("Nếu true và trong scene có CameraCursor, không bắt Alt ở đây (tránh xung đột).")]
    [SerializeField] private bool delegateAltToCameraCursorWhenPresent = true;
    [Tooltip("Sau LoadScene gameplay, thử khóa lại chuột tối đa N frame (race với GameCursorManager). 0 = tắt. Không chạy mãi để Alt/CameraCursor có thể mở chuột.")]
    [SerializeField] private int gameplayLockRetryFrames = 8;

    [Header("Ẩn chuột FPS (không ép Cursor.visible = false)")]
    [Tooltip("Bật: Lock + Cursor.visible true + texture trong suốt (override hoặc runtime 32×32). Tắt: ẩn cứng Cursor.visible = false như cũ.")]
    [SerializeField] private bool useTransparentDefaultCursorGameplay = true;
    [Tooltip("Tuỳ chọn: gán texture toàn trong suốt; nếu null thì dùng runtime 32×32 trong suốt (TransparentCursorUtility).")]
    [SerializeField] private Texture2D transparentCursorOverride;
    [SerializeField] private Vector2 transparentCursorHotspot;

    /// <summary>True khi đang chế độ gameplay (chuột ẩn + lock).</summary>
    public bool IsGameplayCursorLocked { get; private set; } = true;

    /// <summary>Đang dùng chế độ “chuột ẩn” bằng texture trong suốt + Default Cursor (Alt/CameraCursor cần logic riêng).</summary>
    public bool UseTransparentDefaultCursorGameplay => useTransparentDefaultCursorGameplay;

    int _gameplayLockRetriesRemaining;

    /// <summary>Vào scene gameplay: chuột trong suốt (soft). Một cú click trái (không qua UI) → ẩn hẳn (Cursor.visible = false).</summary>
    bool _gameplayCursorSoftPhase;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Instance = null;
    }
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (!IsMenuScene(SceneManager.GetActiveScene().name))
            _gameplayCursorSoftPhase = useTransparentDefaultCursorGameplay;
        ApplyCursorForScene(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!applyLockOnSceneLoad)
            return;
        CursorUIPriority.ClearStaleUiOverlayDepthIfSingle(mode);
        if (IsMenuScene(scene.name))
            _gameplayCursorSoftPhase = false;
        else
            _gameplayCursorSoftPhase = useTransparentDefaultCursorGameplay;
        ApplyCursorForScene(scene.name);
        if (gameplayLockRetryFrames > 0 && !IsMenuScene(scene.name))
            _gameplayLockRetriesRemaining = gameplayLockRetryFrames;
    }

    /// <summary>Gọi từ CameraCursor khi người chơi bật chuột tự do (Alt) — dừng retry ép lock.</summary>
    public void ClearGameplayLockRetries()
    {
        _gameplayLockRetriesRemaining = 0;
    }

    void Update()
    {
        if (_gameplayLockRetriesRemaining > 0 && IsGameplayCursorLocked && !CursorUIPriority.IsUiOverlayActive)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                _gameplayLockRetriesRemaining = 0;
            else
                SetMouseState(true);
            if (_gameplayLockRetriesRemaining > 0)
                _gameplayLockRetriesRemaining--;
        }

        if (!ShouldHandleAltToggle())
            return;

        if (WasAltPressedThisFrame())
        {
            // Toggle: gameplay lock <-> free cursor — từ tự do → khóa lại: không dùng soft (đã tương tác)
            bool wasFree = !IsGameplayCursorLocked;
            IsGameplayCursorLocked = !IsGameplayCursorLocked;
            if (IsGameplayCursorLocked && wasFree)
                _gameplayCursorSoftPhase = false;
            ApplyFromGameplayFlag();
        }
    }

    bool ShouldHandleAltToggle()
    {
        if (CursorUIPriority.IsUiOverlayActive)
            return false;

        if (delegateAltToCameraCursorWhenPresent &&
            FindFirstObjectByType<MovementSystem.CameraCursor>() != null)
            return false;

        return true;
    }

    static bool WasAltPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.leftAltKey.wasPressedThisFrame)
            return true;
#endif
        try
        {
            return Input.GetKeyDown(KeyCode.LeftAlt);
        }
        catch
        {
            return false;
        }
    }

    void ApplyCursorForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        if (CursorUIPriority.IsUiOverlayActive)
            return;

        bool isMenu = IsMenuScene(sceneName);
        IsGameplayCursorLocked = !isMenu;
        ApplyFromGameplayFlag();
    }

    public bool IsActiveSceneMenuScene()
    {
        return IsMenuScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Đang chờ click trái để ẩn hẳn chuột (chỉ khi bật transparent gameplay + gameplay lock).</summary>
    public bool IsGameplaySoftCursorAwaitingFirstClick =>
        useTransparentDefaultCursorGameplay && _gameplayCursorSoftPhase && IsGameplayCursorLocked;

    /// <summary>Gọi từ GameCursorManager: click trái lần đầu → ẩn thật (visible = false).</summary>
    public void CommitGameplayCursorFullHideAfterFirstClick()
    {
        if (!_gameplayCursorSoftPhase || !IsGameplayCursorLocked)
            return;
        _gameplayCursorSoftPhase = false;
        SetMouseState(true);
    }

    bool IsMenuScene(string sceneName)
    {
        if (menuSceneNames == null || menuSceneNames.Length == 0)
            return false;

        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            if (string.Equals(menuSceneNames[i], sceneName, System.StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    void ApplyFromGameplayFlag()
    {
        SetMouseState(IsGameplayCursorLocked);
    }

    /// <summary>Gọi từ CameraCursor / script khác — đồng bộ trạng thái FPS vs chuột tự do.</summary>
    /// <param name="fromUserToggleFromFreeCursor">True khi Alt: từ chuột tự do → khóa FPS — bỏ soft phase.</param>
    public void SetGameplayCursorLocked(bool locked, bool fromUserToggleFromFreeCursor = false)
    {
        if (locked && fromUserToggleFromFreeCursor)
            _gameplayCursorSoftPhase = false;
        IsGameplayCursorLocked = locked;
        SetMouseState(locked);
    }

    /// <summary>
    /// isLocked = true: FPS (ẩn chuột, CursorLockMode.Locked). false: menu (chuột tự do).
    /// </summary>
    public void SetMouseState(bool isLocked)
    {
        if (isLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (useTransparentDefaultCursorGameplay && _gameplayCursorSoftPhase)
            {
                Cursor.visible = true;
                ApplyTransparentGameplayCursorTexture();
            }
            else
            {
                Cursor.visible = false;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GameCursorManager.TryApplyNormalCursorTextureFromScene();
        }
    }

    void ApplyTransparentGameplayCursorTexture()
    {
        if (transparentCursorOverride != null)
            Cursor.SetCursor(transparentCursorOverride, transparentCursorHotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(TransparentCursorUtility.GetRuntimeTransparent32(), transparentCursorHotspot, CursorMode.Auto);
    }

    /// <summary>Gọi sau khi đóng UI overlay cuối cùng — menu = chuột tự do, gameplay = lock (ghi đè sau CameraCursor).</summary>
    public void RefreshAfterUiOverlayClosed()
    {
        if (CursorUIPriority.IsUiOverlayActive)
            return;

        string name = SceneManager.GetActiveScene().name;
        if (IsMenuScene(name))
        {
            IsGameplayCursorLocked = false;
            SetMouseState(false);
        }
        else
        {
            IsGameplayCursorLocked = true;
            SetMouseState(true);
        }
    }
}
