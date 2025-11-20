using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [Header("Remove Mode")]
    [SerializeField] private Button removeModeButton; // The "Remove Button" in inventory
    [SerializeField] private TextMeshProUGUI removeModeButtonText; // Text component of the button
    [SerializeField] private Transform itemsContentContainer; // Content container that holds all item UI elements

    private bool isRemoveModeActive = false;
    private List<ItemUI> currentItemUIs = new List<ItemUI>();

    public bool isInventoryOpen = false;

    void Start()
    {
        inventory.SetActive(false);
        isInventoryOpen = false;
        isRemoveModeActive = false;

        if (character == null)
            character = FindObjectOfType<Character>();
        if (playerInput == null && character != null)
            playerInput = character.GetComponent<PlayerInput>();

        // Setup remove mode button
        if (removeModeButton != null)
        {
            removeModeButton.onClick.AddListener(ToggleRemoveMode);
        }

        // Initialize remove mode button text
        UpdateRemoveModeButtonText();
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
        // Exit remove mode if active
        if (isRemoveModeActive)
        {
            SetRemoveMode(false);
        }

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

    #region Remove Mode

    /// <summary>
    /// Toggle remove mode on/off
    /// </summary>
    public void ToggleRemoveMode()
    {
        SetRemoveMode(!isRemoveModeActive);
    }

    /// <summary>
    /// Set remove mode state
    /// </summary>
    private void SetRemoveMode(bool active)
    {
        isRemoveModeActive = active;
        UpdateRemoveModeButtonText();
        UpdateItemRemoveButtons();
    }

    /// <summary>
    /// Update the text of the remove mode button
    /// </summary>
    private void UpdateRemoveModeButtonText()
    {
        if (removeModeButtonText != null)
        {
            removeModeButtonText.text = isRemoveModeActive ? "Back" : "Remove Button";
        }
    }

    /// <summary>
    /// Show or hide remove buttons on all items
    /// </summary>
    private void UpdateItemRemoveButtons()
    {
        // Refresh the list of item UIs
        RefreshItemUIList();

        // Update visibility of remove buttons
        foreach (ItemUI itemUI in currentItemUIs)
        {
            if (itemUI != null)
            {
                itemUI.SetRemoveButtonVisible(isRemoveModeActive);
            }
        }
    }

    /// <summary>
    /// Refresh the list of ItemUI components in the content container
    /// </summary>
    private void RefreshItemUIList()
    {
        currentItemUIs.Clear();

        if (itemsContentContainer != null)
        {
            // Get all ItemUI components from children
            ItemUI[] itemUIs = itemsContentContainer.GetComponentsInChildren<ItemUI>(true);
            currentItemUIs.AddRange(itemUIs);
        }
    }

    /// <summary>
    /// Remove an item from the inventory
    /// Called by ItemUI when the X button is clicked
    /// </summary>
    public void RemoveItem(ItemUI itemUI, Item itemData, int amount)
    {
        if (itemUI == null || itemData == null) return;

        // Remove the item UI from the list
        if (currentItemUIs.Contains(itemUI))
        {
            currentItemUIs.Remove(itemUI);
        }

        // Destroy the item UI GameObject
        if (itemUI.gameObject != null)
        {
            Destroy(itemUI.gameObject);
        }

        // TODO: Here you can add logic to actually remove the item from your inventory data structure
        // For example: inventoryData.RemoveItem(itemData, amount);

        Debug.Log($"[InventoryController] Removed item: {itemData.itemName} (Amount: {amount})");
    }

    /// <summary>
    /// Call this method when items are added to the inventory to refresh the UI
    /// </summary>
    public void OnItemsUpdated()
    {
        if (isRemoveModeActive)
        {
            UpdateItemRemoveButtons();
        }
    }

    #endregion
}