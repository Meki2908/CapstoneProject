using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Portal chuyển scene (Phiên bản cải tiến)
/// Khi player lại gần -> Hiện Canvas chọn Scene.
/// Các nút trên Canvas sẽ gọi hàm LoadSelectedScene() để chuyển vùng.
/// </summary>
public class ScenePortal : MonoBehaviour
{
    [Header("=== UI REFERENCES ===")]
    [Tooltip("Danh sách các UI có thể chọn")]
    public GameObject[] availableUIs;
    
    [Tooltip("Chỉ số của UI được chọn hiện tại (0, 1, 2, ...)")]
    public int selectedUIIndex = 0;

    [Header("=== SETTINGS ===")]
    [Tooltip("Tag của người chơi")]
    public string playerTag = "Player";
    
    [Tooltip("Thời gian delay trước khi chuyển scene")]
    public float teleportDelay = 0.5f;

    [Header("=== EFFECTS ===")]
    public ParticleSystem portalEffect;

    private bool playerInRange = false;

    void Start()
    {
        // Ẩn tất cả UI lúc đầu
        if (availableUIs != null)
        {
            foreach (var ui in availableUIs)
            {
                if (ui != null) ui.SetActive(false);
            }
        }

        if (portalEffect != null)
        {
            portalEffect.Play();
        }

        // Tự động thiết lập Trigger nếu chưa có
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            OpenPortalUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            ClosePortalUI();
        }
    }

    public void OpenPortalUI()
    {
        if (availableUIs != null && selectedUIIndex >= 0 && selectedUIIndex < availableUIs.Length)
        {
            GameObject selectedUI = availableUIs[selectedUIIndex];
            if (selectedUI != null)
            {
                selectedUI.SetActive(true);
                
                // Hiển thị chuột để chọn
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Debug.LogWarning($"[ScenePortal] UI tại index {selectedUIIndex} đang bị trống!");
            }
        }
        else
        {
            Debug.LogWarning("[ScenePortal] Chưa gán availableUIs hoặc index không hợp lệ!");
        }
    }

    public void ClosePortalUI()
    {
        if (availableUIs != null && selectedUIIndex >= 0 && selectedUIIndex < availableUIs.Length)
        {
            GameObject selectedUI = availableUIs[selectedUIIndex];
            if (selectedUI != null)
            {
                selectedUI.SetActive(false);
                
                // Khóa lại chuột để chơi tiếp
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    /// <summary>
    /// Hàm này để gán vào OnClick() của các Button trên Canvas.
    /// Truyền tên Scene vào tham số của hàm trong Inspector.
    /// </summary>
    public void LoadSelectedScene(string sceneName)
    {
        Debug.Log($"[ScenePortal] Đang chuyển đến Scene: {sceneName}");
        
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            // Đảm bảo game không bị pause khi chuyển
            Time.timeScale = 1f; 
            
            // Ẩn chuột trước khi load scene mới
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            StartCoroutine(TeleportRoutine(sceneName));
        }
        else
        {
            Debug.LogError($"[ScenePortal] Scene '{sceneName}' chưa được thêm vào Build Settings!");
        }
    }

    private System.Collections.IEnumerator TeleportRoutine(string sceneName)
    {
        // Có thể thêm hiệu ứng Fade Out ở đây
        yield return new WaitForSeconds(teleportDelay);
        SceneManager.LoadScene(sceneName);
    }
}
