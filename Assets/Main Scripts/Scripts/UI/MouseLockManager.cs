using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Singleton DontDestroyOnLoad — nguồn duy nhất quyết định ẩn/hiện cursor xuyên scene.
///
/// ĐỘ ƯU TIÊN (priority stack):
///   P1 (cao nhất) : UI overlay đang mở (Inventory, Pause, Dialogue...)  → cursor HIỆN
///   P2 (trung bình): Alt đang được giữ                                   → cursor HIỆN
///   P3 (thấp)     : Bất kỳ object tag "HUD" nào đang active             → cursor HIỆN
///   Default       : Tất cả trên đều false                               → cursor ẨN, camera tự do
///
/// ANTI-SPAM: Kết quả cuối là bool "shouldCursorBeVisible" tính mỗi frame.
///   Chỉ apply khi khác trạng thái frame trước — không quan trọng ai gọi bao nhiêu lần/frame.
///   HUD dùng counter (không dùng toggle) → spam SetActive không gây lệch counter.
///
/// Cho phép scripts khác báo cáo ý định qua:
///   NotifyUiOverlay(bool)           — CursorUIPriority gọi
///   NotifyAltHeld(bool)             — CameraCursor gọi
///   NotifyHudVisibilityChanged(bool)— HudTaggedCanvasCursorOnEnable gọi (mỗi object báo cáo riêng)
///
/// Camera Lock: khi cursor hiện → disable Look + Zoom input; ẩn → enable lại.
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class MouseLockManager : MonoBehaviour
{
    public static MouseLockManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Scenes — cursor tự do (menu / lobby)")]
    [Tooltip("Scene menu: cursor hiện, không lock. Các scene khác = gameplay.")]
    [SerializeField] private string[] menuSceneNames = new[]
    {
        "UI_Game",
        "Menu_Game",
        "DemoSceneSettings"
    };

    [Header("Behaviour")]
    [SerializeField] private bool applyLockOnSceneLoad = true;
    [Tooltip("Sau LoadScene gameplay, thử lock lại tối đa N frame. 0 = tắt.")]
    [SerializeField] private int gameplayLockRetryFrames = 8;

    [Header("Camera Input Actions (disable khi cursor hiện)")]
    [Tooltip("Look / Camera rotate action — sẽ bị disable khi cursor visible.")]
    [SerializeField] private UnityEngine.InputSystem.InputActionReference lookInputAction;
    [Tooltip("CameraZoom action — sẽ bị disable khi cursor visible.")]
    [SerializeField] private UnityEngine.InputSystem.InputActionReference zoomInputAction;
    [Tooltip("Nếu true: disable Look+Zoom khi cursor hiện. False = luôn bật.")]
    [SerializeField] private bool lockCameraInputWhenCursorVisible = true;

    [Header("Ẩn chuột FPS")]
    [Tooltip("Bật: dùng transparent cursor (soft hide). Tắt: Cursor.visible=false cứng.")]
    [SerializeField] private bool useTransparentDefaultCursorGameplay = true;
    [Tooltip("Tuỳ chọn: texture trong suốt; null = runtime 32×32.")]
    [SerializeField] private Texture2D transparentCursorOverride;
    [SerializeField] private Vector2 transparentCursorHotspot;

    // ── Public State ──────────────────────────────────────────────────────

    /// <summary>True khi đang ở chế độ gameplay (cursor ẩn + locked).</summary>
    public bool IsGameplayCursorLocked { get; private set; } = true;

    /// <summary>Đang ở phase "soft cursor" chờ click lần đầu.</summary>
    public bool UseTransparentDefaultCursorGameplay => useTransparentDefaultCursorGameplay;

    // ── Priority Flags ────────────────────────────────────────────────────

    // P1: UI overlay (Inventory, Pause, Dialogue, Portal, Reward...)
    private int _uiOverlayDepth = 0;

    // P2: Người chơi đang giữ Alt
    private bool _isAltHeld = false;

    // P3: Số lượng HUD object đang active báo cáo
    private int _hudVisibleCount = 0;

    // ── Internal ──────────────────────────────────────────────────────────

    private bool _lastAppliedVisible = false;
    private bool _gameplayCursorSoftPhase = false;
    private int  _gameplayLockRetriesRemaining = 0;

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
#endif

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void Start()
    {
        bool isMenu = IsMenuScene(SceneManager.GetActiveScene().name);
        _gameplayCursorSoftPhase = !isMenu && useTransparentDefaultCursorGameplay;
        ApplyCursorForScene(SceneManager.GetActiveScene().name);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!applyLockOnSceneLoad) return;
        CursorUIPriority.ClearStaleUiOverlayDepthIfSingle(mode);

        // Reset priority flags khi chuyển scene (Alt không còn giữ, HUD về 0)
        _isAltHeld       = false;
        _hudVisibleCount = 0;
        _uiOverlayDepth  = 0;

        bool isMenu = IsMenuScene(scene.name);
        _gameplayCursorSoftPhase = !isMenu && useTransparentDefaultCursorGameplay;
        ApplyCursorForScene(scene.name);

        if (gameplayLockRetryFrames > 0 && !isMenu)
            _gameplayLockRetriesRemaining = gameplayLockRetryFrames;
    }

    // ── Update ────────────────────────────────────────────────────────────

    void Update()
    {
        // Retry lock sau scene load (race với các hệ thống khác)
        if (_gameplayLockRetriesRemaining > 0)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _gameplayLockRetriesRemaining = 0;
            }
            else if (!ComputeShouldCursorBeVisible())
            {
                ApplyMouseState(false);
            }
            if (_gameplayLockRetriesRemaining > 0)
                _gameplayLockRetriesRemaining--;
        }

        // Tính toán ưu tiên và apply nếu khác trạng thái cũ
        bool shouldShow = ComputeShouldCursorBeVisible();
        if (shouldShow != _lastAppliedVisible)
        {
            ApplyMouseState(shouldShow);
        }
    }

    // ── Priority Compute ──────────────────────────────────────────────────

    /// <summary>
    /// Tính result ưu tiên trong gameplay.
    /// Chỉ P1 (UI overlay) hoặc P2 (giữ Alt) được phép hiện cursor.
    /// P3 (HUD active) chỉ giữ để chẩn đoán, KHÔNG ép cursor hiện trong gameplay.
    /// </summary>
    private bool ComputeShouldCursorBeVisible()
    {
        // Menu scene luôn hiện cursor
        if (IsMenuScene(SceneManager.GetActiveScene().name)) return true;

        bool p1 = _uiOverlayDepth  > 0; // UI overlay mở
        bool p2 = _isAltHeld;            // Giữ Alt
        bool p3 = _hudVisibleCount > 0;  // HUD canvas active (diagnostic only)

        if (p1 || p2)
        {
            // DIANGNOSTIC: In ra nguyên nhân hiển thị cursor (giới hạn tần suất để đỡ spam log)
            if (Time.frameCount % 90 == 0)
            {
                Debug.Log($"[MouseLockManager] Cursors is visible because: UI_Overlay={_uiOverlayDepth}, Alt={_isAltHeld}, HUD={_hudVisibleCount}");
            }
            return true;
        }

        // Extra diagnostic: HUD vẫn active nhưng không còn quyền ép cursor hiện trong gameplay.
        if (p3 && Time.frameCount % 180 == 0)
        {
            Debug.Log($"[MouseLockManager] HUD active without overlay (HUD={_hudVisibleCount}) -> keep gameplay cursor locked.");
        }

        return false;
    }

    /// <summary>
    /// Bạo lực reset tất cả các state đếm overlay/hud về 0.
    /// Dùng cho các trường hợp Timeline/Transition bị kẹt state (HUD bật nhưng không tắt).
    /// </summary>
    public void ClearAllLocksAndForceGameplay()
    {
        _uiOverlayDepth = 0;
        _isAltHeld = false;
        _hudVisibleCount = 0;
        IsGameplayCursorLocked = true;
        ForceApplyCurrentState();
        Debug.Log("[MouseLockManager] Đã dùng bạo lực CLEAR ALL LOCKS -> Ép về Gameplay!");
    }

    // ── Public Notification APIs ──────────────────────────────────────────

    /// <summary>
    /// Gọi từ CursorUIPriority khi UI overlay mở/đóng.
    /// Dùng counter để hỗ trợ nhiều UI chồng nhau.
    /// </summary>
    public void NotifyUiOverlay(bool opening)
    {
        if (opening)
        {
            _uiOverlayDepth++;
            _gameplayLockRetriesRemaining = 0; // Dừng retry khi UI mở
        }
        else
        {
            _uiOverlayDepth = Mathf.Max(0, _uiOverlayDepth - 1);
        }
        // Force apply ngay lập tức (không chờ frame sau)
        ForceApplyCurrentState();
    }

    /// <summary>
    /// Gọi từ CursorUIPriority.EndAllUiOverlays (reset sạch overlay depth).
    /// </summary>
    public void NotifyUiOverlayReset()
    {
        _uiOverlayDepth = 0;
        ForceApplyCurrentState();
    }

    /// <summary>
    /// Gọi từ CameraCursor khi người chơi bắt đầu/kết thúc giữ Alt.
    /// </summary>
    public void NotifyAltHeld(bool held)
    {
        _isAltHeld = held;
        if (held) _gameplayLockRetriesRemaining = 0;
        ForceApplyCurrentState();
    }

    /// <summary>
    /// Gọi từ HudTaggedCanvasCursorOnEnable khi một HUD object bật/tắt.
    /// Mỗi object báo cáo riêng → dùng counter, không dùng bool toggle.
    /// </summary>
    public void NotifyHudVisibilityChanged(bool hudBecameActive)
    {
        if (hudBecameActive)
            _hudVisibleCount++;
        else
            _hudVisibleCount = Mathf.Max(0, _hudVisibleCount - 1);

        ForceApplyCurrentState();
    }

    /// <summary>
    /// Backward compat: gọi từ CameraCursor/CursorUiOverlayWhenActive cũ.
    /// </summary>
    public void SetGameplayCursorLocked(bool locked, bool fromUserToggleFromFreeCursor = false)
    {
        if (locked)
        {
            // Muốn ẩn cursor → chỉ apply nếu không có P1/P2/P3 cản
            if (ComputeShouldCursorBeVisible()) return;
            IsGameplayCursorLocked = true;
            ApplyMouseState(false);
        }
        else
        {
            IsGameplayCursorLocked = false;
            ApplyMouseState(true);
        }
    }

    /// <summary>Dừng retry lock (CameraCursor gọi khi Alt mở cursor).</summary>
    public void ClearGameplayLockRetries()
    {
        _gameplayLockRetriesRemaining = 0;
    }

    /// <summary>Đang chờ click trái để ẩn hẳn cursor (soft phase).</summary>
    public bool IsGameplaySoftCursorAwaitingFirstClick =>
        useTransparentDefaultCursorGameplay && _gameplayCursorSoftPhase && IsGameplayCursorLocked;

    /// <summary>Gọi từ GameCursorManager: click trái lần đầu → ẩn thật.</summary>
    public void CommitGameplayCursorFullHideAfterFirstClick()
    {
        if (!_gameplayCursorSoftPhase || !IsGameplayCursorLocked) return;
        _gameplayCursorSoftPhase = false;
        ApplyMouseState(false);
    }

    public bool IsActiveSceneMenuScene() => IsMenuScene(SceneManager.GetActiveScene().name);

    /// <summary>Gọi sau khi đóng UI overlay — refresh về đúng trạng thái scene.</summary>
    public void RefreshAfterUiOverlayClosed()
    {
        ForceApplyCurrentState();
    }

    // ── Apply Logic ───────────────────────────────────────────────────────

    private void ForceApplyCurrentState()
    {
        bool shouldShow = ComputeShouldCursorBeVisible();
        ApplyMouseState(shouldShow);
        _lastAppliedVisible = shouldShow;
    }

    private void ApplyMouseState(bool showCursor)
    {
        _lastAppliedVisible = showCursor;
        IsGameplayCursorLocked = !showCursor;

        if (showCursor)
        {
            // Hiện cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
            GameCursorManager.TryApplyNormalCursorTextureFromScene();

            // Lock camera input
            if (lockCameraInputWhenCursorVisible)
                SetCameraInputEnabled(false);
        }
        else
        {
            // Ẩn cursor (gameplay)
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

            // Unlock camera input
            if (lockCameraInputWhenCursorVisible)
                SetCameraInputEnabled(true);
        }
    }

    private void SetCameraInputEnabled(bool enable)
    {
        SetActionEnabled(lookInputAction, enable);
        SetActionEnabled(zoomInputAction, enable);
    }

    private static void SetActionEnabled(UnityEngine.InputSystem.InputActionReference actionRef, bool enable)
    {
        if (actionRef == null || actionRef.action == null) return;
        if (enable) actionRef.action.Enable();
        else        actionRef.action.Disable();
    }

    private void ApplyTransparentGameplayCursorTexture()
    {
        if (transparentCursorOverride != null)
            Cursor.SetCursor(transparentCursorOverride, transparentCursorHotspot, CursorMode.Auto);
        else
            Cursor.SetCursor(TransparentCursorUtility.GetRuntimeTransparent32(), transparentCursorHotspot, CursorMode.Auto);
    }

    // ── Scene Helpers ─────────────────────────────────────────────────────

    private void ApplyCursorForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        bool shouldShow = IsMenuScene(sceneName);
        ApplyMouseState(shouldShow);
    }

    private bool IsMenuScene(string sceneName)
    {
        if (menuSceneNames == null) return false;
        for (int i = 0; i < menuSceneNames.Length; i++)
            if (string.Equals(menuSceneNames[i], sceneName, System.StringComparison.Ordinal))
                return true;
        return false;
    }

    // ── Legacy SetMouseState (compat) ─────────────────────────────────────

    /// <summary>Backward compat — dùng ApplyMouseState thay thế.</summary>
    public void SetMouseState(bool isLocked) => ApplyMouseState(!isLocked);
}
