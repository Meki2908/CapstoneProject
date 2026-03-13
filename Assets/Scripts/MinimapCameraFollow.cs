using UnityEngine;
using UnityEngine.UI;

public class MinimapCameraFollow : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform player;               // Nhân vật
    public Vector3 offset = new Vector3(0, 50, 0); // Camera cao nhìn thẳng

    [Header("UI Settings")]
    public RectTransform playerIcon;       // Icon Player trên RawImage

    [Header("Minimap Canvas")]
    [Tooltip("Canvas hoặc GameObject chứa minimap UI — sẽ ẩn/hiện theo settings")]
    public GameObject minimapCanvas;       // Gán Canvas minimap để toggle

    private Camera minimapCamera;

    void Start()
    {
        minimapCamera = GetComponent<Camera>();

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

        // Listen settings changed
        GameSettings.OnSettingsChanged += UpdateMinimapVisibility;
        UpdateMinimapVisibility();
    }

    void OnDestroy()
    {
        GameSettings.OnSettingsChanged -= UpdateMinimapVisibility;
    }

    /// <summary>
    /// Ẩn/hiện minimap theo GameSettings.miniMapEnabled
    /// </summary>
    private void UpdateMinimapVisibility()
    {
        bool enabled = GameSettings.Instance == null || GameSettings.Instance.miniMapEnabled;

        if (minimapCamera != null)
            minimapCamera.enabled = enabled;

        if (minimapCanvas != null)
            minimapCanvas.SetActive(enabled);
    }

    void LateUpdate()
    {
        if (player == null)
            return;

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
