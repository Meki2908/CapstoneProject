using UnityEngine;
using UnityEngine.UI;

public class MinimapCameraFollow : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;               // Nhân vật
    public Vector3 offset = new Vector3(0, 50, 0);

    [Header("Rotation Settings")]
    [Tooltip("Kéo Main Camera vào đây để minimap xoay theo hướng nhìn của camera. Để trống sẽ xoay theo player body.")]
    public Transform cameraTransform;      // Main Camera (optional)

    [Header("UI Settings")]
    public RectTransform playerIcon;       // Icon Player trên RawImage

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("[MinimapCameraFollow] Đã tự động tìm thấy Player!");
            }
            else
            {
                Debug.LogWarning("[MinimapCameraFollow] Không tìm thấy GameObject với tag 'Player'. Hãy gán thủ công trong Inspector!");
            }
        }

        // Tự động tìm Main Camera nếu chưa gán
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Debug.Log("[MinimapCameraFollow] Tự động dùng Main Camera để xoay minimap.");
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Camera minimap theo sát player
        transform.position = player.position + offset;

        // Xoay theo camera (hoặc player nếu không có camera)
        float yAngle = (cameraTransform != null) ? cameraTransform.eulerAngles.y : player.eulerAngles.y;
        transform.rotation = Quaternion.Euler(90f, yAngle, 0f);

        // Icon player luôn hướng lên trên UI (không xoay theo map)
        if (playerIcon != null)
            playerIcon.localRotation = Quaternion.identity;
    }
}
