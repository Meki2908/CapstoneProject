using UnityEngine;

/// <summary>
/// Gắn lên panel/canvas được bật/tắt bằng SetActive — đồng bộ stack CursorUIPriority.
/// Dùng khi prefab cần đảm bảo đóng/mở UI luôn khớp cursor priority dù có nhiều đường gọi SetActive.
/// Không tự set Cursor.visible / Cursor.lockState — toàn bộ do <see cref="MouseLockManager"/> xử lý.
/// </summary>
[DisallowMultipleComponent]
public class CursorUiOverlayWhenActive : MonoBehaviour
{
    void OnEnable()
    {
        CursorUIPriority.BeginUiOverlay();
    }

    void OnDisable()
    {
        CursorUIPriority.EndUiOverlay();
    }
}
