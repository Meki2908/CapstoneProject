using UnityEngine;

/// <summary>
/// Ưu tiên UI: khi có bất kỳ panel/menu nào mở, toàn quyền hiện/ẩn cursor thuộc về UI.
/// Đếm lớp (stack): đóng UI cuối cùng thì luôn về chế độ chơi — chuột ẩn, camera xoay (kể cả trước đó đang bật Alt).
/// </summary>
public static class CursorUIPriority
{
    private static int _depth;

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

        // Luôn về gameplay: không giữ trạng thái Alt (chuột tự do) sau khi đóng UI
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
    }

    /// <summary>
    /// Đóng hết overlay (ví dụ trước khi chuyển scene) — reset depth và ép về chế độ FPS.
    /// </summary>
    public static void EndAllUiOverlays()
    {
        _depth = 0;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _depth = 0;
    }
#endif
}
