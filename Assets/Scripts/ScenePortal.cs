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
    [Tooltip("Kéo Canvas chọn Scene vào đây")]
    public GameObject portalCanvas;

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
        // Ẩn Canvas lúc đầu
        if (portalCanvas != null)
        {
            portalCanvas.SetActive(false);
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
        if (portalCanvas != null)
        {
            portalCanvas.SetActive(true);
            
            // Hiển thị chuột để chọn
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Debug.LogWarning("[ScenePortal] Chưa gán portalCanvas trong Inspector!");
        }
    }

    public void ClosePortalUI()
    {
        if (portalCanvas != null)
        {
            portalCanvas.SetActive(false);
            
            // Khóa lại chuột để chơi tiếp
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
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
            Time.timeScale = 1f; // Đảm bảo game không bị pause khi chuyển
            StartCoroutine(TeleportRoutine(sceneName));
        }
        else
        {
            Debug.LogError($"[ScenePortal] Scene '{sceneName}' chưa được thêm vào Build Settings!");
        }
    }

    private System.Collections.IEnumerator TeleportRoutine(string sceneName)
    {
        if (SceneTransitionManager.Instance != null)
        {
            // SceneTransitionManager tự có fade → không cần delay thêm
            SceneTransitionManager.Instance.GoToScene(sceneName, "Đang chuyển vùng...");
        }
        else
        {
            yield return new WaitForSeconds(teleportDelay);
            SceneManager.LoadScene(sceneName);
        }
    }
}
