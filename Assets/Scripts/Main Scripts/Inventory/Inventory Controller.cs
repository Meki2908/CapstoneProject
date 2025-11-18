using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private GameObject inventory;
    [SerializeField] private CinemachineInputProvider inputProvider;
    [SerializeField] private bool disableCameraLookOnInventoryOpen = true;
    [SerializeField] private bool disableCameraZoomOnInventoryOpen = true;
    [Tooltip("If you're using Cinemachine 2.8.4 or earlier, untick this option.\\nIf unticked, both Look and Zoom will be disabled.")]
    [SerializeField] private bool fixedCinemachineVersion = true;

    [Header("Player Input Blocking")]
    [SerializeField] private Character character;
    [SerializeField] private PlayerInput playerInput;

    public bool isInventoryOpen = false;

    void Start()
    {
        inventory.SetActive(false);
        isInventoryOpen = false;

        if (character == null)
            character = FindObjectOfType<Character>();
        if (playerInput == null && character != null)
            playerInput = character.GetComponent<PlayerInput>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    private void OpenInventory()
    {
        // Show cursor and unlock
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Disable camera controls
        DisableCameraControls();

        // Disable player input
        DisablePlayerInput();

        // Show inventory
        inventory.SetActive(true);
        isInventoryOpen = true;

        Debug.Log("[InventoryController] Inventory opened - Cursor visible and unlocked");
    }

    private void CloseInventory()
    {
        // Hide cursor and lock
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Enable camera controls
        EnableCameraControls();

        // Enable player input
        EnablePlayerInput();

        // Hide inventory
        inventory.SetActive(false);
        isInventoryOpen = false;

        Debug.Log("[InventoryController] Inventory closed - Cursor hidden and locked");
    }

    /// <summary>
    /// Public method to close inventory from UI button (e.g., X button)
    /// This will restore player input, camera controls, and cursor state
    /// </summary>
    public void CloseInventoryFromUI()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
    }

    public void OpenInventoryFromUI()
    {
        if (!isInventoryOpen)
        {
            OpenInventory();
        }
    }

    private void DisableCameraControls()
    {
        if (inputProvider == null) return;

        if (!fixedCinemachineVersion)
        {
            // For older Cinemachine versions, disable the entire input provider
            inputProvider.enabled = false;
            return;
        }

        // For newer Cinemachine versions, disable specific actions
        if (disableCameraLookOnInventoryOpen)
        {
            inputProvider.XYAxis.action?.Disable();
        }

        if (disableCameraZoomOnInventoryOpen)
        {
            inputProvider.ZAxis.action?.Disable();
        }
    }

    private void EnableCameraControls()
    {
        if (inputProvider == null) return;

        if (!fixedCinemachineVersion)
        {
            // For older Cinemachine versions, enable the entire input provider
            inputProvider.enabled = true;
            return;
        }

        // For newer Cinemachine versions, enable specific actions
        if (disableCameraLookOnInventoryOpen)
        {
            inputProvider.XYAxis.action?.Enable();
        }

        if (disableCameraZoomOnInventoryOpen)
        {
            inputProvider.ZAxis.action?.Enable();
        }
    }

    private void DisablePlayerInput()
    {
        // Instead of disabling entire PlayerInput component, disable specific action maps
        // This allows UI to still work while blocking player movement/combat actions
        PlayerInput targetPlayerInput = playerInput;
        if (targetPlayerInput == null && character != null)
        {
            targetPlayerInput = character.playerInput;
        }

        if (targetPlayerInput != null && targetPlayerInput.actions != null)
        {
            // Disable Player action map (movement, combat, etc.)
            var playerMap = targetPlayerInput.actions.FindActionMap("Player");
            if (playerMap != null)
            {
                playerMap.Disable();
                Debug.Log("[InventoryController] Disabled Player action map");
            }

            // Disable Skill action map
            var skillMap = targetPlayerInput.actions.FindActionMap("Skill");
            if (skillMap != null)
            {
                skillMap.Disable();
                Debug.Log("[InventoryController] Disabled Skill action map");
            }

            // Note: UI action map (if exists) will remain enabled for button clicks
        }
    }

    private void EnablePlayerInput()
    {
        // Re-enable the action maps that were disabled
        PlayerInput targetPlayerInput = playerInput;
        if (targetPlayerInput == null && character != null)
        {
            targetPlayerInput = character.playerInput;
        }

        if (targetPlayerInput != null && targetPlayerInput.actions != null)
        {
            // Enable Player action map
            var playerMap = targetPlayerInput.actions.FindActionMap("Player");
            if (playerMap != null)
            {
                playerMap.Enable();
                Debug.Log("[InventoryController] Enabled Player action map");
            }

            // Enable Skill action map
            var skillMap = targetPlayerInput.actions.FindActionMap("Skill");
            if (skillMap != null)
            {
                skillMap.Enable();
                Debug.Log("[InventoryController] Enabled Skill action map");
            }
        }
    }
}