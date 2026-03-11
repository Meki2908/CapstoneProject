using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// Portal chuyển scene — gắn vào object Portal trong Map_Chinh
/// Khi player lại gần → hiện hướng dẫn "Nhấn Z để dịch chuyển"
/// Nhấn Z → chuyển sang scene dungeon (MapSaMac)
/// </summary>
public class ScenePortal : MonoBehaviour
{
    [Header("=== CÀI ĐẶT PORTAL ===")]
    [Tooltip("Tên scene sẽ chuyển đến (phải thêm vào Build Settings)")]
    public string targetSceneName = "MapSaMac";
    
    [Tooltip("Phím bấm để dịch chuyển")]
    public KeyCode teleportKey = KeyCode.Z;
    
    [Header("=== UI HƯỚNG DẪN ===")]
    [Tooltip("Kéo Canvas hướng dẫn vào đây (tạo sẵn trong Portal)")]
    public GameObject promptUI;
    
    [Tooltip("Text hiển thị hướng dẫn (tự tìm nếu để trống)")]
    public TextMeshProUGUI promptText;
    
    [Tooltip("Nội dung hướng dẫn")]
    public string promptMessage = "Press <color=#FFD700>Z</color> to Teleport";
    
    [Header("=== HIỆU ỨNG ===")]
    [Tooltip("Hiệu ứng particle khi portal hoạt động")]
    public ParticleSystem portalEffect;
    
    [Tooltip("Thời gian chờ trước khi chuyển scene (giây)")]
    public float teleportDelay = 0.5f;
    
    // Trạng thái
    private bool playerInRange = false;
    private bool isTeleporting = false;
    
    void Start()
    {
        // Nếu chưa gán UI → tự tạo UI Screen Space Overlay (luôn hiện trên màn hình)
        if (promptUI == null)
        {
            CreateScreenSpaceUI();
        }
        
        // Ẩn UI hướng dẫn khi bắt đầu
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
        
        // Tự tìm text nếu chưa gán
        if (promptText == null && promptUI != null)
        {
            promptText = promptUI.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        // Đặt nội dung text
        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
        
        // Bật hiệu ứng portal
        if (portalEffect != null)
        {
            portalEffect.Play();
        }
        
        // Kiểm tra Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[ScenePortal] Không có Collider! Thêm Box Collider hoặc Sphere Collider và bật Is Trigger.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[ScenePortal] Collider chưa bật Is Trigger! Tự động bật.");
            col.isTrigger = true;
        }
    }
    
    void Update()
    {
        // Chỉ xử lý khi player trong phạm vi và chưa đang dịch chuyển
        if (playerInRange && !isTeleporting)
        {
            if (Input.GetKeyDown(teleportKey))
            {
                StartTeleport();
            }
        }
    }
    
    /// <summary>
    /// Khi player bước vào vùng portal
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = true;
            
            // Hiện UI hướng dẫn
            if (promptUI != null)
            {
                promptUI.SetActive(true);
            }
            
            Debug.Log("[ScenePortal] Player vào vùng portal");
        }
    }
    
    /// <summary>
    /// Khi player rời khỏi vùng portal
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInRange = false;
            
            // Ẩn UI hướng dẫn
            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }
            
            Debug.Log("[ScenePortal] Player rời vùng portal");
        }
    }
    
    /// <summary>
    /// Kiểm tra có phải player không
    /// </summary>
    private bool IsPlayer(Collider other)
    {
        // Kiểm tra bằng tag "Player" hoặc tên "player"
        return other.CompareTag("Player") || 
               other.gameObject.name.ToLower().Contains("player");
    }
    
    /// <summary>
    /// Bắt đầu dịch chuyển
    /// </summary>
    private void StartTeleport()
    {
        isTeleporting = true;
        
        // Đổi text thành "Đang dịch chuyển..."
        if (promptText != null)
        {
            promptText.text = "<color=#00FF00>Teleporting...</color>";
        }
        
        Debug.Log($"[ScenePortal] Dịch chuyển đến scene: {targetSceneName}");
        
        // Chuyển scene sau delay
        if (teleportDelay > 0)
        {
            StartCoroutine(TeleportAfterDelay());
        }
        else
        {
            LoadTargetScene();
        }
    }
    
    private System.Collections.IEnumerator TeleportAfterDelay()
    {
        yield return new WaitForSeconds(teleportDelay);
        LoadTargetScene();
    }
    
    private void LoadTargetScene()
    {
        // Kiểm tra scene có trong Build Settings không
        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            // Đảm bảo game không bị pause
            Time.timeScale = 1f;
            
            // === FIX: Reset URP shadow settings trước khi chuyển scene ===
            // FogController ở map chính set các giá trị shadow theo camera zoom
            // Các giá trị persist trên URP asset global → dungeon kế thừa sai
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.shadowDistance = 50f;          // Max Distance gốc
                urpAsset.shadowCascadeCount = 4;        // Cascade Count (nhiều cascade = bóng nét hơn)
                urpAsset.cascadeBorder = 0.2f;          // Last Border
                Debug.Log($"[ScenePortal] Reset URP shadow settings before scene transition");
            }
            
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[ScenePortal] Scene '{targetSceneName}' không có trong Build Settings! " +
                           "Vào File → Build Settings → kéo scene vào danh sách.");
            
            if (promptText != null)
            {
                promptText.text = $"<color=#FF0000>Error: Scene '{targetSceneName}' not in Build Settings!</color>";
            }
            
            isTeleporting = false;
        }
    }
    
    /// <summary>
    /// Tự tạo UI Screen Space Overlay — luôn hiện trên màn hình
    /// </summary>
    private void CreateScreenSpaceUI()
    {
        // Tạo Canvas (Screen Space Overlay)
        GameObject canvasObj = new GameObject("PortalPromptCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Hiện trên các UI khác
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = 
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Tạo panel nền (bán trong suốt)
        GameObject panelObj = new GameObject("PromptPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image panelImg = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.05f); // Dưới giữa màn hình
        panelRect.anchorMax = new Vector2(0.7f, 0.12f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Tạo Text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(panelObj.transform, false);
        promptText = textObj.AddComponent<TextMeshProUGUI>();
        promptText.text = promptMessage;
        promptText.fontSize = 28;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);
        
        promptUI = canvasObj;
        
        Debug.Log("[ScenePortal] Tự động tạo UI Screen Space Overlay");
    }
}
