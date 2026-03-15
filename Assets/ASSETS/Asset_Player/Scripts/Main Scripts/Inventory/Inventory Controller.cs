using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private GameObject inventory;
#pragma warning disable CS0618 // Type is obsolete - still functional, will migrate to InputAxisController later
    [SerializeField] private CinemachineInputProvider inputProvider;
#pragma warning restore CS0618
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

    [Header("Inventory UI")]
    [SerializeField] private GameObject itemUIPrefab; // Prefab for item UI element

    // Public property to access itemUIPrefab (for WeaponForgeUI)
    public GameObject ItemUIPrefab => itemUIPrefab;

    private bool isRemoveModeActive = false;
    private List<ItemUI> currentItemUIs = new List<ItemUI>();
    private InputAction inventoryToggleAction; // New Input System action

    public bool isInventoryOpen = false;

    void Start()
    {
        inventory.SetActive(false);
        isInventoryOpen = false;
        isRemoveModeActive = false;

        if (character == null)
            character = FindFirstObjectByType<Character>();
        if (playerInput == null && character != null)
            playerInput = character.GetComponent<PlayerInput>();

        // Setup Input System action for inventory toggle
        if (playerInput != null && playerInput.actions != null)
        {
            inventoryToggleAction = playerInput.actions.FindAction("Inventory");
            if (inventoryToggleAction != null)
            {
                inventoryToggleAction.performed += _ => ToggleInventory();
            }
            else
            {
                Debug.LogWarning("[InventoryController] 'Inventory' action not found in PlayerInput. Please add it to your Input Actions asset.");
            }
        }

        // Setup remove mode button
        if (removeModeButton != null)
        {
            removeModeButton.onClick.AddListener(ToggleRemoveMode);
        }

        // Initialize remove mode button text
        UpdateRemoveModeButtonText();

        // Subscribe to InventoryManager events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
        }
        else
        {
            // Retry subscription nếu InventoryManager chưa Awake xong
            StartCoroutine(RetrySubscribeInventoryManager());
        }

        // Initial UI refresh
        RefreshInventoryUI();
    }

    private System.Collections.IEnumerator RetrySubscribeInventoryManager()
    {
        // Đợi tối đa 3s cho InventoryManager khởi tạo
        float timeout = 3f;
        while (InventoryManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
            RefreshInventoryUI();
            Debug.Log("[InventoryController] Late-subscribed to InventoryManager.OnInventoryChanged");
        }
        else
        {
            Debug.LogError("[InventoryController] InventoryManager.Instance still null after 3s!");
        }
    }

    void Update()
    {
        // Chỉ dùng legacy Input khi Input System action "Inventory" KHÔNG tồn tại
        // Nếu cả 2 cùng chạy → double toggle → mở rồi đóng ngay
        if (inventoryToggleAction == null && Input.GetKeyDown(KeyCode.I))
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
        // Pause game khi mở inventory (để kéo thả item thoải mái)
        Time.timeScale = 0f;

        // Disable camera controls TRƯỚC
        DisableCameraControls();

        // Disable player input
        DisablePlayerInput();

        // Refresh inventory UI before showing
        RefreshInventoryUI();

        // Show inventory
        if (inventory == null)
        {
            Debug.LogWarning("[InventoryController] inventory reference is null or destroyed!");
            return;
        }
        inventory.SetActive(true);
        isInventoryOpen = true;

        // Unlock cursor SAU CÙNG (đảm bảo không bị script khác override)
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log($"[InventoryController] Inventory opened - timeScale={Time.timeScale}, cursor locked={Cursor.lockState}");
    }

    private void CloseInventory()
    {
        // Exit remove mode if active
        if (isRemoveModeActive)
        {
            SetRemoveMode(false);
        }

        // NOTE: WeaponForgeUI and EquipmentPanelUI have been moved to BlacksmithUI (NPC Thợ Rèn)
        // Player no longer accesses forge/equipment panel from inventory

        // Kiểm tra PauseMenu có đang mở không
        bool isPauseMenuOpen = false;

        // Chỉ lock cursor nếu PauseMenu KHÔNG mở
        if (!isPauseMenuOpen)
        {
            // Resume game khi đóng inventory
            Time.timeScale = 1f;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            // Enable camera controls
            EnableCameraControls();

            // Enable player input
            EnablePlayerInput();
        }
        else
        {
            Debug.Log("[InventoryController] PauseMenu is open - keeping cursor unlocked");
        }

        // Hide inventory
        inventory.SetActive(false);
        isInventoryOpen = false;

        Debug.Log("[InventoryController] Inventory closed");
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
        // Tự tìm nếu chưa gắn trong Inspector
        if (inputProvider == null)
            inputProvider = FindFirstObjectByType<CinemachineInputProvider>();
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

            // Giữ Inventory action luôn enabled để nhấn I để đóng inventory
            if (inventoryToggleAction != null)
            {
                inventoryToggleAction.Enable();
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

        // Remove from InventoryManager
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(itemData.id, amount);
        }

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

    /// <summary>
    /// Called when inventory changes (from InventoryManager event)
    /// </summary>
    private void OnInventoryChanged()
    {
        RefreshInventoryUI();
    }

    /// <summary>
    /// Refresh the inventory UI by loading items from InventoryManager
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (itemsContentContainer == null || itemUIPrefab == null)
        {
            Debug.LogWarning("[InventoryController] Cannot refresh UI: itemsContentContainer or itemUIPrefab is null!");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryController] InventoryManager.Instance is null!");
            return;
        }

        // Clear existing UI items — dùng DestroyImmediate để xoá ngay, tránh lộn thứ tự
        for (int i = itemsContentContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(itemsContentContainer.GetChild(i).gameObject);
        }
        currentItemUIs.Clear();

        // Get all items with rarity from InventoryManager
        var allItems = InventoryManager.Instance.GetAllItemsWithRarity();

        // Create UI for each item+rarity combo
        foreach (var (item, amount, rarity) in allItems)
        {
            if (item == null) continue;

            GameObject itemUIObject = Instantiate(itemUIPrefab, itemsContentContainer);
            ItemUI itemUI = itemUIObject.GetComponent<ItemUI>();

            if (itemUI != null)
            {
                itemUI.Initialize(item, amount, this, rarity);
                currentItemUIs.Add(itemUI);
            }
            else
            {
                Destroy(itemUIObject);
            }
        }

        // Update remove buttons if in remove mode
        if (isRemoveModeActive)
        {
            UpdateItemRemoveButtons();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from Input System action
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.performed -= _ => ToggleInventory();
        }

        // Unsubscribe from InventoryManager events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    #endregion
}