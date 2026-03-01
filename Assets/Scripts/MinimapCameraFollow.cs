using UnityEngine;
using UnityEngine.UI;

public class MinimapCameraFollow : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;               // Nhân vật
    public Vector3 offset = new Vector3(0, 50, 0); // Camera cao nhìn thẳng

    [Header("UI Settings")]
    public RectTransform playerIcon;       // Icon Player trên RawImage

    void Start()
    {
        // Tự động tìm player nếu chưa gán
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
    }

    void LateUpdate()
    {
        if (player == null)
        {
            Debug.LogWarning("Player chưa gán vào MinimapCameraFollow!");
            return;
        }

        // 1. Camera xoay theo player
        transform.position = player.position + offset;
        transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f); // xoay map theo player

        // 2. Icon player luôn hướng lên trên UI
        if (playerIcon != null)
        {
            playerIcon.localRotation = Quaternion.identity; // icon không xoay, luôn hướng lên trên
        }
    }
}
