using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages system cursor textures and swaps cursor by hover target.
/// Use Player Settings cursor as default, then override with Cursor.SetCursor at runtime.
/// </summary>
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
        DontDestroyOnLoad(gameObject);
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

    private void LateUpdate()
    {
        if (!ShouldApplyCursorOverrides()) return;

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
