using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gắn lên NPC Thợ Rèn trong scene.
/// Player đến gần → hiện "Press F" → nhấn F → mở BlacksmithUI.
/// Khi mở: pause game, unlock cursor, disable player input/camera.
/// </summary>
public class BlacksmithNPC : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Canvas hiện 'Press F to interact'")]
    public GameObject promptCanvas;

    [Tooltip("BlacksmithUI panel (sẽ SetActive khi nhấn F)")]
    public BlacksmithUI blacksmithUI;

    [Header("Settings")]
    public KeyCode interactKey = KeyCode.F;
    public string playerTag = "Player";

    private bool _canInteract = false;
    private bool _isOpen = false;

    void Awake()
    {
        // ─── Auto-create "Press F" prompt if not assigned ────────
        if (promptCanvas == null)
        {
            promptCanvas = CreatePromptCanvas();
            Debug.Log("[BlacksmithNPC] Auto-created 'Press F' prompt");
        }
        promptCanvas.SetActive(false);

        // ─── Auto-add BoxCollider trigger if missing ─────────────
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4f, 3f, 4f);
            box.center = new Vector3(0, 1.5f, 0);
            Debug.Log("[BlacksmithNPC] Auto-added BoxCollider trigger (4x3x4)");
        }
        else if (!col.isTrigger)
        {
            // Ensure existing collider is trigger
            col.isTrigger = true;
        }

        // ─── Find BlacksmithUI if not assigned ─────────────────────
        if (blacksmithUI == null)
        {
            blacksmithUI = FindFirstObjectByType<BlacksmithUI>(FindObjectsInactive.Include);
        }
        if (blacksmithUI == null)
        {
            Debug.LogError("[BlacksmithNPC] BlacksmithUI not found! " +
                          "Please use Tools → Create Blacksmith Canvas to create it, " +
                          "then assign it in the Inspector.");
        }
    }

    /// <summary>
    /// Tạo Canvas world-space hiện "Nhấn F" phía trên đầu NPC
    /// </summary>
    GameObject CreatePromptCanvas()
    {
        // Canvas (World Space, Billboard)
        GameObject canvasGO = new GameObject("PromptCanvas_PressF");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0, 2.5f, 0); // Trên đầu NPC
        canvasGO.transform.localScale = Vector3.one * 0.01f; // Scale down for world space

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        canvasGO.AddComponent<GraphicRaycaster>();

        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 60);

        // Background panel
        GameObject bgGO = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        var outline = bgGO.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.84f, 0f, 0.7f);
        outline.effectDistance = new Vector2(2, 2);

        // "Nhấn F" text
        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textGO.transform.SetParent(bgGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Nhấn <color=#FFD700><b>F</b></color> để mở Thợ Rèn";
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;

        // Try to find a font
        var anyText = FindFirstObjectByType<TextMeshProUGUI>();
        if (anyText != null && anyText != tmp) tmp.font = anyText.font;

        // Billboard behavior (always face camera) — add simple component
        canvasGO.AddComponent<BillboardPrompt>();

        return canvasGO;
    }

    void Update()
    {
        if (!_canInteract && !_isOpen) return;

        if (IsInteractPressedThisFrame())
        {
            if (_isOpen)
            {
                CloseBlacksmith();
            }
            else if (_canInteract)
            {
                OpenBlacksmith();
            }
        }

        // ESC to close
        if (_isOpen && IsEscPressedThisFrame())
        {
            CloseBlacksmith();
        }
    }

    // ─── Open / Close ────────────────────────────────────────────

    void OpenBlacksmith()
    {
        if (blacksmithUI == null) return;

        _isOpen = true;

        // Ẩn prompt
        if (promptCanvas) promptCanvas.SetActive(false);

        // Mở UI
        blacksmithUI.Open();

        // Pause game
        Time.timeScale = 0f;

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player input
        DisablePlayerInput();
        DisableCameraControls();
    }

    public void CloseBlacksmith()
    {
        if (!_isOpen) return;
        _isOpen = false;

        // Đóng UI
        if (blacksmithUI != null) blacksmithUI.Close();

        // Resume game
        Time.timeScale = 1f;

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Re-enable player input
        EnablePlayerInput();
        EnableCameraControls();

        // Hiện lại prompt nếu player vẫn trong range
        if (_canInteract && promptCanvas) promptCanvas.SetActive(true);
    }

    // ─── Input Helpers ───────────────────────────────────────────

    bool IsInteractPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(interactKey);
    }

    bool IsEscPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(KeyCode.Escape);
    }

    // ─── Trigger Zone ────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _canInteract = true;
        if (!_isOpen && promptCanvas) promptCanvas.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _canInteract = false;
        if (promptCanvas) promptCanvas.SetActive(false);

        // Auto-close if player walks away
        if (_isOpen) CloseBlacksmith();
    }

    // ─── Player / Camera Controls ────────────────────────────────
    // (Same pattern as InventoryController)

    void DisablePlayerInput()
    {
#if ENABLE_INPUT_SYSTEM
        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            var playerMap = playerInput.actions.FindActionMap("Player", false);
            if (playerMap != null) playerMap.Disable();

            var skillMap = playerInput.actions.FindActionMap("Skill", false);
            if (skillMap != null) skillMap.Disable();
        }
#endif
    }

    void EnablePlayerInput()
    {
#if ENABLE_INPUT_SYSTEM
        var playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            var playerMap = playerInput.actions.FindActionMap("Player", false);
            if (playerMap != null) playerMap.Enable();

            var skillMap = playerInput.actions.FindActionMap("Skill", false);
            if (skillMap != null) skillMap.Enable();
        }
#endif
    }

    void DisableCameraControls()
    {
        try
        {
            var cameraInputProviders = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>(
                FindObjectsSortMode.None);
            foreach (var provider in cameraInputProviders)
            {
                provider.enabled = false;
            }
        }
        catch { }
    }

    void EnableCameraControls()
    {
        try
        {
            var cameraInputProviders = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>(
                FindObjectsSortMode.None);
            foreach (var provider in cameraInputProviders)
            {
                provider.enabled = true;
            }
        }
        catch { }
    }
}
