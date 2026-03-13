using UnityEngine;

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
        if (promptCanvas) promptCanvas.SetActive(false);
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
