using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion; // 1. BẮT BUỘC THÊM THƯ VIỆN NÀY

/// <summary>
/// Portal chuyển scene (Phiên bản chuẩn mạng Fusion)
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
        if (portalCanvas != null) portalCanvas.SetActive(false);
        if (portalEffect != null) portalEffect.Play();

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
            CursorUIPriority.BeginUiOverlay();
            portalCanvas.SetActive(true);
        }
    }

    public void ClosePortalUI()
    {
        if (portalCanvas != null)
        {
            portalCanvas.SetActive(false);
            CursorUIPriority.EndUiOverlay();
        }
    }

    public void LoadSelectedScene(string sceneName)
    {
        Debug.Log($"[ScenePortal] Đang chuyển đến Scene: {sceneName} bằng hệ thống Mạng!");
        
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Time.timeScale = 1f; 
            StartCoroutine(TeleportRoutine(sceneName));
        }
        else
        {
            Debug.LogError($"[ScenePortal] Scene '{sceneName}' chưa được thêm vào Build Settings!");
        }
    }

    private IEnumerator TeleportRoutine(string sceneName)
    {
        // 1. Ẩn UI Cổng
        ClosePortalUI();

        // 2. Tạm thời Delay để màn hình/UI có thời gian phản hồi
        // LƯU Ý: Tạm thời chúng ta KHÔNG gọi SceneTransitionManager.Instance.GoToScene nữa 
        // vì nó dùng SceneManager thuần gây đứt mạng. (Chúng ta sẽ sửa nó sau nếu cần thiết)
        yield return new WaitForSeconds(teleportDelay);

        // 3. TÌM TRÁI TIM MẠNG (NETWORK RUNNER)
        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null && runner.IsServer) // Singleplayer luôn là Server
        {
            // Lấy mã số của Scene (Build Index) dựa vào tên Scene
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (buildIndex >= 0)
            {
                // LỆNH CHUYỂN SCENE CHUẨN FUSION
                runner.LoadScene(SceneRef.FromIndex(buildIndex)); 
            }
            else
            {
                Debug.LogError($"[ScenePortal] Không tìm thấy Build Index cho Scene: {sceneName}");
            }
        }
        else
        {
            // FALLBACK: Đề phòng lúc bạn dev test Offline chay không bật Fusion
            SceneManager.LoadScene(sceneName);
        }
    }
}
