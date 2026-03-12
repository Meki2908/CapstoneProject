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
    [Tooltip("UI mặc định sẽ hiển thị. Nếu bạn chỉ có 1 UI, hãy kéo vào đây.")]
    public GameObject portalCanvas;

    [Tooltip("Danh sách các UI khác (dùng khi bạn muốn mỗi cổng hiện 1 UI khác nhau)")]
    public GameObject[] availableUIs;
    
    [Tooltip("Chỉ số của UI trong mảng availableUIs sẽ được chọn (nếu dùng mảng)")]
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
        // Ẩn tất cả UI lúc đầu để tránh bị hiện đè
        HideAllUIs();

        if (portalEffect != null)
        {
            portalEffect.Play();
        }

        // Tự động thiết lập Trigger nếu chưa có
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void HideAllUIs()
    {
        if (portalCanvas != null) portalCanvas.SetActive(false);
        
        if (availableUIs != null)
        {
            foreach (var ui in availableUIs)
            {
                if (ui != null) ui.SetActive(false);
            }
        }
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
        GameObject uiToShow = GetCurrentUI();

        if (uiToShow != null)
        {
            uiToShow.SetActive(true);
            
            // Hiển thị chuột để chọn
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Debug.LogWarning("[ScenePortal] Không tìm thấy UI nào để hiển thị! Hãy gán portalCanvas hoặc availableUIs.");
        }
    }

    public void ClosePortalUI()
    {
        GameObject uiToHide = GetCurrentUI();

        if (uiToHide != null)
        {
            uiToHide.SetActive(false);
            
            // Khóa lại chuột để chơi tiếp
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private GameObject GetCurrentUI()
    {
        // Ưu tiên dùng mảng nếu có phần tử
        if (availableUIs != null && availableUIs.Length > 0)
        {
            if (selectedUIIndex >= 0 && selectedUIIndex < availableUIs.Length)
            {
                return availableUIs[selectedUIIndex];
            }
        }
        
        // Nếu mảng trống hoặc index sai, dùng portalCanvas đơn lẻ
        return portalCanvas;
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
