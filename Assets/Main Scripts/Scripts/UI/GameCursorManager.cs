using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Manages system cursor textures and swaps cursor by hover target.
/// Use Player Settings cursor as default, then override with Cursor.SetCursor at runtime.
/// Gameplay: một cú click trái (không qua UI) chuyển từ chuột trong suốt (soft) sang ẩn hẳn — xem MouseLockManager.
/// Một instance mỗi scene (không DontDestroyOnLoad) — cấu hình texture theo từng scene trong Inspector.
/// </summary>
[DefaultExecutionOrder(-45)]
public class GameCursorManager : MonoBehaviour
{
    public static GameCursorManager Instance { get; private set; }

    public enum CursorState
    {
        Normal,
        Button,
        Item,
        Settings
    }

    [Header("Cursor Textures")]
    [Tooltip("Optional. If empty, Unity uses the default cursor configured in Player Settings.")]
    [SerializeField] private Texture2D normalCursorTexture;
    [SerializeField] private Texture2D buttonHoverCursorTexture;
    [SerializeField] private Texture2D itemHoverCursorTexture;
    [SerializeField] private Texture2D settingsHoverCursorTexture;

    [Header("Hotspot (pixels)")]
    [SerializeField] private Vector2 normalHotspot;
    [SerializeField] private Vector2 buttonHotspot;
    [SerializeField] private Vector2 itemHotspot;
    [SerializeField] private Vector2 settingsHotspot;

    [Header("Options")]
    [Tooltip("Only apply cursor overrides while cursor should be interactable (Alt/menu/inventory).")]
    [SerializeField] private bool syncVisibilityWithCursorLock = true;
    [Tooltip("If true, manager keeps applying cursor every frame while active.")]
    [SerializeField] private bool keepRefreshingCursor = false;

    [Header("Gameplay — soft → ẩn hẳn")]
    [Tooltip("Nếu true: click trái khi chưa qua UI mới commit ẩn hẳn. False = mọi click trái đều commit (có thể ăn click vào nút).")]
    [SerializeField] private bool commitFullHideOnlyWhenNotOverUi = true;

    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(16);
    private CursorState _currentState = (CursorState)(-1);
    private CursorState? _forcedState;
    private readonly HashSet<int> _warnedUnreadableTextures = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ApplyCursor(CursorState.Normal, true);
    }

    private void OnDestroy()
    {
        RestoreDefaultCursor();
        if (Instance == this)
            Instance = null;
    }

    private void OnDisable()
    {
        RestoreDefaultCursor();
    }

    private void Update()
    {
        if (MouseLockManager.Instance == null)
            return;
        if (!MouseLockManager.Instance.IsGameplaySoftCursorAwaitingFirstClick)
            return;
        if (MouseLockManager.Instance.IsActiveSceneMenuScene())
            return;
        if (CursorUIPriority.IsUiOverlayActive)
            return;
        if (commitFullHideOnlyWhenNotOverUi && IsPointerOverUiBlockingFirstClick())
            return;
        if (!WasLeftMouseButtonPressedThisFrame())
            return;
        MouseLockManager.Instance.CommitGameplayCursorFullHideAfterFirstClick();
    }

    static bool WasLeftMouseButtonPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;
#endif
        try
        {
            return Input.GetMouseButtonDown(0);
        }
        catch
        {
            return false;
        }
    }

    bool IsPointerOverUiBlockingFirstClick()
    {
        if (EventSystem.current == null)
            return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void LateUpdate()
    {
        if (!ShouldApplyCursorOverrides()) return;

        // Ensure cursor remains visible while UI is interactive
        // (prevents timing conflicts with CameraCursor/other systems)
        if (!Cursor.visible)
            Cursor.visible = true;

        CursorState targetState = _forcedState ?? DetectHoverState();
        bool shouldApply = keepRefreshingCursor || targetState != _currentState;
        if (shouldApply)
            ApplyCursor(targetState);
    }

    private CursorState DetectHoverState()
    {
        if (EventSystem.current == null) return CursorState.Normal;

        var eventData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);

        foreach (var r in _raycastResults)
        {
            if (r.gameObject == null) continue;

            // Optional: CursorHoverTarget overrides
            var hoverTarget = r.gameObject.GetComponentInParent<CursorHoverTarget>();
            if (hoverTarget != null)
            {
                switch (hoverTarget.CurrentHoverType)
                {
                    case CursorHoverTarget.HoverType.Button: return CursorState.Button;
                    case CursorHoverTarget.HoverType.Item: return CursorState.Item;
                    case CursorHoverTarget.HoverType.Settings: return CursorState.Settings;
                    default: continue;
                }
            }

            if (r.gameObject.GetComponentInParent<Button>() != null)
                return CursorState.Button;
            if (r.gameObject.GetComponentInParent<ItemUI>() != null)
                return CursorState.Item;
            // Optional: other item-like UI
            if (r.gameObject.GetComponentInParent<GemItemUI>() != null || r.gameObject.GetComponentInParent<EquipmentItemUI>() != null)
                return CursorState.Item;
        }

        return CursorState.Normal;
    }

    private bool ShouldApplyCursorOverrides()
    {
        if (!syncVisibilityWithCursorLock) return true;
        // FPS + soft (trong suốt) hoặc đã ẩn hẳn: không ghi đè hover lên Cursor
        if (MouseLockManager.Instance != null &&
            MouseLockManager.Instance.UseTransparentDefaultCursorGameplay &&
            MouseLockManager.Instance.IsGameplayCursorLocked &&
            Cursor.lockState == CursorLockMode.Locked)
            return false;
        return Cursor.lockState != CursorLockMode.Locked || Cursor.visible;
    }

    private void ApplyCursor(CursorState state, bool force = false)
    {
        Texture2D primaryTexture = null;
        Texture2D fallbackTexture = normalCursorTexture;
        Vector2 hotspot = Vector2.zero;

        switch (state)
        {
            case CursorState.Button:
                primaryTexture = buttonHoverCursorTexture;
                hotspot = buttonHoverCursorTexture != null ? buttonHotspot : normalHotspot;
                break;
            case CursorState.Item:
                primaryTexture = itemHoverCursorTexture;
                hotspot = itemHoverCursorTexture != null ? itemHotspot : normalHotspot;
                break;
            case CursorState.Settings:
                primaryTexture = settingsHoverCursorTexture;
                fallbackTexture = buttonHoverCursorTexture != null ? buttonHoverCursorTexture : normalCursorTexture;
                hotspot = settingsHoverCursorTexture != null ? settingsHotspot : buttonHotspot;
                break;
            default:
                primaryTexture = normalCursorTexture;
                hotspot = normalHotspot;
                break;
        }

        if (!force && state == _currentState && !keepRefreshingCursor)
            return;

        Texture2D texture = GetUsableCursorTexture(primaryTexture, fallbackTexture);
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        _currentState = state;
    }

    private Texture2D GetUsableCursorTexture(Texture2D primary, Texture2D fallback)
    {
        if (IsTextureCpuReadable(primary))
            return primary;
        if (IsTextureCpuReadable(fallback))
            return fallback;
        return null; // Use default cursor from Player Settings
    }

    private bool IsTextureCpuReadable(Texture2D texture)
    {
        if (texture == null) return false;

        if (!texture.isReadable)
        {
            int id = texture.GetInstanceID();
            if (!_warnedUnreadableTextures.Contains(id))
            {
                _warnedUnreadableTextures.Add(id);
                Debug.LogWarning($"[GameCursorManager] Cursor texture '{texture.name}' is not CPU accessible. Enable Read/Write in Import Settings.");
            }
            return false;
        }

        return true;
    }

    private void RestoreDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        _currentState = (CursorState)(-1);
    }

    /// <summary>Chuột tự do (Alt, menu): dùng Normal Cursor texture trong Inspector — không dùng Player Settings transparent.</summary>
    public void ApplyNormalCursorTexture()
    {
        _forcedState = null;
        ApplyCursor(CursorState.Normal, true);
    }

    /// <summary>Khi không có instance (scene không gắn GameCursorManager), fallback SetCursor(null).</summary>
    public static void TryApplyNormalCursorTextureFromScene()
    {
        if (Instance != null)
            Instance.ApplyNormalCursorTexture();
        else
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    /// <summary>Call from other scripts to force a cursor state (e.g. drag).</summary>
    public void SetCursorState(CursorState state)
    {
        _forcedState = state;
        ApplyCursor(state, true);
    }

    /// <summary>Clear forced state and return to hover-based cursor switching.</summary>
    public void ClearForcedCursorState()
    {
        _forcedState = null;
    }
}
