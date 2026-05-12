using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý Priority Stack cho UI overlay (Inventory, Pause, Dialogue...).
/// Dùng counter (depth) để hỗ trợ nhiều UI chồng nhau.
/// Thông báo <see cref="MouseLockManager"/> thay vì tự set Cursor trực tiếp.
/// </summary>
public static class CursorUIPriority
{
    private static int _depth;

    /// <summary>
    /// LoadScene(Single) có thể hủy menu trước khi EndUiOverlay chạy → _depth kẹt.
    /// Gọi trước MouseLockManager.ApplyCursorForScene.
    /// </summary>
    public static void ClearStaleUiOverlayDepthIfSingle(LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        _depth = 0;
        // MouseLockManager được reset trong OnSceneLoaded của chính nó
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearStaleUiOverlayDepthIfSingle(mode);
    }

    /// <summary>True khi có ít nhất một UI đang giữ quyền ưu tiên.</summary>
    public static bool IsUiOverlayActive => _depth > 0;

    /// <summary>Gọi khi mở một UI panel/menu.</summary>
    public static void BeginUiOverlay()
    {
        _depth++;
        MouseLockManager.Instance?.NotifyUiOverlay(true);
    }

    /// <summary>Gọi khi đóng một UI panel/menu; khi depth về 0 → FPS mode.</summary>
    public static void EndUiOverlay()
    {
        if (_depth <= 0) return;

        _depth--;
        MouseLockManager.Instance?.NotifyUiOverlay(false);

        if (_depth == 0)
        {
            // Sync Cinemachine sau khi đóng UI cuối
            MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
        }
    }

    /// <summary>Đóng hết overlay (ví dụ trước khi chuyển scene) — reset depth.</summary>
    public static void EndAllUiOverlays()
    {
        _depth = 0;
        MouseLockManager.Instance?.NotifyUiOverlayReset();
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _depth = 0;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}
