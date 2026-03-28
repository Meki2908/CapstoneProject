using UnityEngine;

/// <summary>
/// Đã được CHUYỂN LOGIC vào FogController.cs và RTSCameraController.cs.
/// Lý do: Game sử dụng Cinemachine VirtualCamera nên nếu sửa Main Camera ở đây sẽ bị Cinemachine đè lại mỗi frame gây lỗi sương mù toàn màn hình.
/// (Script này có thể xóa bỏ, hoặc cứ để nguyên không sao vì đã được làm rỗng an toàn)
/// </summary>
public class RenderDistanceController : MonoBehaviour
{
    // Logic moved to FogController.cs 
}
