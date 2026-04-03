using UnityEngine;

/// <summary>
/// Gắn lên panel/canvas được bật/tắt bằng SetActive: đồng bộ stack CursorUIPriority + chuột.
/// Dùng khi prefab cần đảm bảo đóng/mở UI luôn khớp CameraCursor dù có nhiều đường gọi SetActive.
/// </summary>
[DisallowMultipleComponent]
public class CursorUiOverlayWhenActive : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Bật thì OnEnable sẽ hiện chuột + None lock (giống PortalUIController).")]
    bool manageCursorVisibility = true;

    void OnEnable()
    {
        CursorUIPriority.BeginUiOverlay();
        if (manageCursorVisibility)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    void OnDisable()
    {
        CursorUIPriority.EndUiOverlay();
    }
}
