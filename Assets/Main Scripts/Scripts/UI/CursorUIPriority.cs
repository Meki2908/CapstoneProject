using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ưu tiên UI: khi có bất kỳ panel/menu nào mở, toàn quyền hiện/ẩn cursor thuộc về UI.
/// Đếm lớp (stack): đóng UI cuối cùng thì luôn về chế độ chơi — chuột ẩn, camera xoay (kể cả trước đó đang bật Alt).
/// </summary>
public static class CursorUIPriority
{
    private static int _depth;

    /// <summary>
    /// LoadScene(Single) có thể hủy menu trước khi EndUiOverlay chạy → _depth kẹt.
    /// Gọi trước MouseLockManager.ApplyCursorForScene (thứ tự sceneLoaded không đảm bảo).
    /// </summary>
    public static void ClearStaleUiOverlayDepthIfSingle(LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;
        _depth = 0;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearStaleUiOverlayDepthIfSingle(mode);
    }

    /// <summary>True khi có ít nhất một UI đang giữ quyền ưu tiên.</summary>
    public static bool IsUiOverlayActive => _depth > 0;

    /// <summary>Gọi khi mở một UI (Inventory, Pause, Dialogue, ...).</summary>
    public static void BeginUiOverlay()
    {
        _depth++;
    }

    /// <summary>Gọi khi đóng một UI; khi không còn UI nào — về FPS: ẩn chuột + bật xoay camera.</summary>
    public static void EndUiOverlay()
    {
        if (_depth <= 0)
            return;

        _depth--;

        if (_depth != 0)
            return;

        // Cinemachine / camera look — luôn gọi trước
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();

        // Chuột: menu scene = tự do, gameplay = lock (MouseLockManager nếu có)
        if (MouseLockManager.Instance != null)
            MouseLockManager.Instance.RefreshAfterUiOverlayClosed();
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    /// <summary>
    /// Đóng hết overlay (ví dụ trước khi chuyển scene) — reset depth và ép về chế độ FPS.
    /// </summary>
    public static void EndAllUiOverlays()
    {
        _depth = 0;
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
        if (MouseLockManager.Instance != null)
            MouseLockManager.Instance.RefreshAfterUiOverlayClosed();
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _depth = 0;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
}
