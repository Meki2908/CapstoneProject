using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// SCRIPT DUY NHẤT cho In-Game Options Menu
/// Sử dụng Screen Space - Overlay để đảm bảo hiển thị
/// Tích hợp với PlayerRoot để ẩn/hiện UI khi mở menu
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("=== MENU REFERENCE ===")]
    [Tooltip("Kéo Canv_Options hoặc bất kỳ Canvas menu nào vào đây")]
    public GameObject menuCanvas;
    
    [Header("=== CLOSE BUTTON ===")]
    [Tooltip("Kéo Button đóng menu vào đây (Btn_Return hoặc tương tự)")]
    public Button closeButton;
    
    [Header("=== PLAYER ROOT ===")]
    [Tooltip("Kéo PlayerRoot vào đây hoặc để trống để tự động tìm")]
    public GameObject playerRoot;
    
    [Header("=== PLAYER UI TO HIDE ===")]
    [Tooltip("UI_Inventory sẽ bị ẩn khi mở menu")]
    public GameObject uiInventory;
    
    [Tooltip("UI_HP sẽ bị ẩn khi mở menu")]
    public GameObject uiHP;
    
    [Header("=== SETTINGS ===")]
    [Tooltip("Pause game khi mở menu")]
    public bool pauseOnOpen = true;
    
    [Tooltip("Hiện cursor khi mở menu")]
    public bool showCursor = true;
    
    [Tooltip("Ẩn Player UI khi mở menu")]
    public bool hidePlayerUIOnOpen = true;
    
    private bool isOpen = false;
    private float savedTimeScale = 1f;
    
    // Lưu trạng thái UI trước khi ẩn
    private bool wasInventoryActive = true;
    private bool wasHPActive = true;
    
    void Awake()
    {
        Debug.Log($"[PauseMenu] Awake() on {gameObject.name}");
    }
    
    void OnEnable()
    {
        Debug.Log($"[PauseMenu] OnEnable() on {gameObject.name}");
    }
    
    void Start()
    {
        Debug.Log("[PauseMenu] Start() called");
        
        // === TÌM PLAYERROOT ===
        if (playerRoot == null)
        {
            playerRoot = GameObject.Find("PlayerRoot");
            if (playerRoot != null)
            {
                Debug.Log($"[PauseMenu] Found PlayerRoot: {playerRoot.name}");
            }
            else
            {
                Debug.LogWarning("[PauseMenu] PlayerRoot not found!");
            }
        }
        
        // === TÌM UI_INVENTORY VÀ UI_HP TỪ PLAYERROOT ===
        if (playerRoot != null)
        {
            if (uiInventory == null)
            {
                Transform t = playerRoot.transform.Find("UI_Invetory"); // Tên gốc có thể sai chính tả
                if (t == null) t = playerRoot.transform.Find("UI_Inventory");
                if (t != null)
                {
                    uiInventory = t.gameObject;
                    Debug.Log($"[PauseMenu] Found UI_Inventory: {uiInventory.name}");
                }
            }
            
            if (uiHP == null)
            {
                Transform t = playerRoot.transform.Find("UI_HP");
                if (t != null)
                {
                    uiHP = t.gameObject;
                    Debug.Log($"[PauseMenu] Found UI_HP: {uiHP.name}");
                }
            }
        }
        
        // === TÌM MENU CANVAS (kể cả inactive) ===
        if (menuCanvas == null)
        {
            // Cách 1: Tìm active object
            menuCanvas = GameObject.Find("Canv_Options");
            
            // Cách 2: Tìm trong Main_Menu (nếu là child)
            if (menuCanvas == null)
            {
                var mainMenu = GameObject.Find("Main_Menu");
                if (mainMenu != null)
                {
                    var t = mainMenu.transform.Find("Canv_Options");
                    if (t != null) menuCanvas = t.gameObject;
                }
            }
            
            // Cách 3: Tìm tất cả Canvas kể cả inactive
            if (menuCanvas == null)
            {
                Canvas[] allCanvases = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (Canvas c in allCanvases)
                {
                    if (c.gameObject.name == "Canv_Options")
                    {
                        menuCanvas = c.gameObject;
                        Debug.Log($"[PauseMenu] Found inactive Canv_Options via Resources");
                        break;
                    }
                }
            }
        }
        
        if (menuCanvas != null)
        {
            Debug.Log($"[PauseMenu] Found menuCanvas: {menuCanvas.name}, Active: {menuCanvas.activeSelf}");
            
            // KHÔNG tách parent - giữ nguyên hierarchy
            // Chỉ cấu hình Canvas
            
            Canvas canvas = menuCanvas.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = menuCanvas.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            
            // Thêm CanvasScaler
            CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = menuCanvas.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Thêm GraphicRaycaster
            if (menuCanvas.GetComponent<GraphicRaycaster>() == null)
            {
                menuCanvas.AddComponent<GraphicRaycaster>();
            }
            
            // === TÌM VÀ GÁN CLOSE BUTTON ===
            if (closeButton == null)
            {
                // Tìm các button có thể là nút đóng
                string[] buttonNames = { "Btn_Return", "Btn_GameReturn", "Btn_Close", "Btn_Back", "CloseButton", "BackButton" };
                
                foreach (string btnName in buttonNames)
                {
                    Transform btnTransform = menuCanvas.transform.Find(btnName);
                    
                    // Tìm sâu hơn nếu không thấy ở root
                    if (btnTransform == null)
                    {
                        btnTransform = FindDeepChild(menuCanvas.transform, btnName);
                    }
                    
                    if (btnTransform != null)
                    {
                        closeButton = btnTransform.GetComponent<Button>();
                        if (closeButton != null)
                        {
                            Debug.Log($"[PauseMenu] Found close button: {btnName}");
                            break;
                        }
                    }
                }
            }
            
            // Wire close button
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
                Debug.Log("[PauseMenu] Close button wired to Close()");
            }
            else
            {
                Debug.LogWarning("[PauseMenu] Close button not found - please assign in Inspector or add button named Btn_Return");
            }
            
            // Ẩn menu lúc đầu
            menuCanvas.SetActive(false);
            
            Debug.Log("[PauseMenu] Setup complete - Canvas: Overlay, SortOrder: 1000");
        }
        else
        {
            Debug.LogError("[PauseMenu] ❌ KHÔNG TÌM THẤY Canv_Options! Hãy làm theo hướng dẫn:");
            Debug.LogError("[PauseMenu] 1. Kéo Canv_Options vào field 'Menu Canvas' trong Inspector");
            Debug.LogError("[PauseMenu] 2. Hoặc đảm bảo Canv_Options tồn tại trong scene");
        }
        
        // Đảm bảo có EventSystem
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[PauseMenu] Created EventSystem");
        }
    }
    
    void Update()
    {
        bool escPressed = false;

        // 1. Check New Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }
        }
        
        // 2. Check Legacy Input (Fallback)
        if (!escPressed)
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    escPressed = true;
                }
            }
            catch {} // Ignore error if legacy input is disabled
        }

        if (escPressed)
        {
            Debug.Log("[PauseMenu] ESC pressed!");
            Toggle();
        }
    }
    
    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }
    
    public void Open()
    {
        if (menuCanvas == null)
        {
            Debug.LogError("[PauseMenu] menuCanvas is null!");
            return;
        }
        
        isOpen = true;
        
        // === ẨN PLAYER UI ===
        if (hidePlayerUIOnOpen)
        {
            HidePlayerUI();
        }
        
        menuCanvas.SetActive(true);
        
        // Hiện cursor
        if (showCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        // Pause
        if (pauseOnOpen)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        
        Debug.Log("[PauseMenu] OPENED - Player UI hidden");
    }
    
    public void Close()
    {
        if (menuCanvas == null) return;
        
        isOpen = false;
        menuCanvas.SetActive(false);
        
        // === HIỆN LẠI PLAYER UI ===
        if (hidePlayerUIOnOpen)
        {
            ShowPlayerUI();
        }
        
        // Resume
        if (pauseOnOpen)
        {
            Time.timeScale = savedTimeScale;
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("[PauseMenu] CLOSED - Player UI restored");
    }
    
    /// <summary>
    /// Ẩn UI_Inventory và UI_HP
    /// </summary>
    private void HidePlayerUI()
    {
        if (uiInventory != null)
        {
            wasInventoryActive = uiInventory.activeSelf;
            uiInventory.SetActive(false);
            Debug.Log("[PauseMenu] Hidden UI_Inventory");
        }
        
        if (uiHP != null)
        {
            wasHPActive = uiHP.activeSelf;
            uiHP.SetActive(false);
            Debug.Log("[PauseMenu] Hidden UI_HP");
        }
    }
    
    /// <summary>
    /// Hiện lại UI_Inventory và UI_HP (về trạng thái trước khi ẩn)
    /// </summary>
    private void ShowPlayerUI()
    {
        if (uiInventory != null)
        {
            uiInventory.SetActive(wasInventoryActive);
            Debug.Log($"[PauseMenu] Restored UI_Inventory: {wasInventoryActive}");
        }
        
        if (uiHP != null)
        {
            uiHP.SetActive(wasHPActive);
            Debug.Log($"[PauseMenu] Restored UI_HP: {wasHPActive}");
        }
    }
    
    /// <summary>
    /// Tìm child theo tên trong toàn bộ hierarchy (recursive)
    /// </summary>
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    
    // Gọi từ button Resume
    public void OnResume() => Close();
    
    // Gọi từ button Quit
    public void OnQuit()
    {
        Time.timeScale = 1f;
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    
    /// <summary>
    /// Kiểm tra menu đang mở hay không
    /// </summary>
    public bool IsMenuOpen => isOpen;
}
