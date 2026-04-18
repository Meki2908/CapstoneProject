using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
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

    private void OnInventoryPerformed(InputAction.CallbackContext ctx) => ToggleInventory();

    // Snapshot camera Cinemachine (cursor do CursorUIPriority quản lý)
    private bool cameraLookWasEnabledBeforeInventory;
    private bool cameraZoomWasEnabledBeforeInventory;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Player prefab spawn sau scene load — gán lại Character/PlayerInput (HUD DontDestroyOnLoad).
        StopCoroutine(nameof(RebindPlayerInputAfterSceneLoad));
        StartCoroutine(RebindPlayerInputAfterSceneLoad());
    }

    private IEnumerator RebindPlayerInputAfterSceneLoad()
    {
        yield return null;
        yield return null;
        SetupPlayerInputBinding();
    }

    /// <summary>
    /// Gỡ subscription cũ rồi tìm player hiện tại và gắn lại action Inventory (I).
    /// </summary>
    private void SetupPlayerInputBinding()
    {
        if (inventoryToggleAction != null)
        {
            inventoryToggleAction.performed -= OnInventoryPerformed;
            inventoryToggleAction = null;
        }

        character = FindFirstObjectByType<Character>();
        playerInput = character != null && character.playerInput != null
            ? character.playerInput
            : FindFirstObjectByType<PlayerInput>();

        if (playerInput != null && playerInput.actions != null)
        {
            inventoryToggleAction = playerInput.actions.FindAction("Inventory");
            if (inventoryToggleAction != null)
                inventoryToggleAction.performed += OnInventoryPerformed;
            else
                Debug.LogWarning("[InventoryController] 'Inventory' action not found in PlayerInput. Please add it to your Input Actions asset.");
        }
        else
            Debug.LogWarning("[InventoryController] No PlayerInput after scene load — inventory hotkey disabled until player exists.");
    }

    void Start()
    {
        inventory.SetActive(false);
        isInventoryOpen = false;
        isRemoveModeActive = false;

        SetupPlayerInputBinding();

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

    void LateUpdate()
    {
        // Cursor được xử lý bời MouseLockManager — không cần force ở đây
    }

    private void ToggleInventory()
    {
        if (!isInventoryOpen && TutorialInputGate.IsActive && !TutorialInputGate.Allows(TutorialInputMask.Inventory))
            return;

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
        if (inventory == null)
        {
            Debug.LogWarning("[InventoryController] inventory reference is null or destroyed!");
            return;
        }

        if (isInventoryOpen)
            return;

        // UI ưu tiên: snapshot cursor (lần đầu trong stack) và chặn Alt/CameraCursor
        CursorUIPriority.BeginUiOverlay();

        // Pause game khi mở inventory (để kéo thả item thoải mái)
        Time.timeScale = 0f;

        // Snapshot trạng thái input camera (nếu có CinemachineInputProvider và dùng fixedCinemachineVersion)
        cameraLookWasEnabledBeforeInventory = true;
        cameraZoomWasEnabledBeforeInventory = true;
        if (inputProvider != null && fixedCinemachineVersion)
        {
            if (disableCameraLookOnInventoryOpen && inputProvider.XYAxis.action != null)
                cameraLookWasEnabledBeforeInventory = inputProvider.XYAxis.action.enabled;
            if (disableCameraZoomOnInventoryOpen && inputProvider.ZAxis.action != null)
                cameraZoomWasEnabledBeforeInventory = inputProvider.ZAxis.action.enabled;
        }

        // Disable camera controls TRƯỚC (nhưng đã snapshot trạng thái để sau này khôi phục đúng)
        DisableCameraControls();

        // Disable player input
        DisablePlayerInput();

        // Refresh inventory UI before showing
        RefreshInventoryUI();

        // Show inventory
        inventory.SetActive(true);
        isInventoryOpen = true;
        TutorialTextDisplay.NotifyInventoryOpenedFromGameplay();
        SoundManager.PlayUIOpenMenu();

        // Cursor được xử lý bời CursorUIPriority.BeginUiOverlay() ở trên
        MouseLockManager.Instance?.ClearGameplayLockRetries();
        GameCursorManager.TryApplyNormalCursorTextureFromScene();

        Debug.Log($"[InventoryController] Inventory opened - timeScale={Time.timeScale}, cursor locked={Cursor.lockState}");
    }

    private void CloseInventory()
    {
        if (!isInventoryOpen)
            return;

        // Exit remove mode if active
        if (isRemoveModeActive)
        {
            SetRemoveMode(false);
        }

        // NOTE: WeaponForgeUI and EquipmentPanelUI have been moved to BlacksmithUI (NPC Thợ Rèn)
        // Player no longer accesses forge/equipment panel from inventory

        // Resume game khi đóng inventory
        Time.timeScale = 1f;

        // Khôi phục lại đúng trạng thái camera look/zoom trước đó
        EnableCameraControls();

        // Enable player input
        EnablePlayerInput();

        // Hide inventory
        inventory.SetActive(false);
        isInventoryOpen = false;
        TutorialTextDisplay.NotifyInventoryClosedFromGameplay();
        SoundManager.PlayUICloseMenu();

        // Trả cursor + CameraCursor về trạng thái trước khi mở UI đầu tiên (stack UI)
        CursorUIPriority.EndUiOverlay();

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
        if (inputProvider == null)
            return;

        if (!fixedCinemachineVersion)
        {
            // For older Cinemachine versions, khôi phục trạng thái enabled như trước
            inputProvider.enabled = true;
            return;
        }

        // For newer Cinemachine versions, khôi phục đúng trạng thái từng action như trước khi mở inventory
        if (disableCameraLookOnInventoryOpen && inputProvider.XYAxis.action != null)
        {
            if (cameraLookWasEnabledBeforeInventory)
                inputProvider.XYAxis.action.Enable();
            else
                inputProvider.XYAxis.action.Disable();
        }

        if (disableCameraZoomOnInventoryOpen && inputProvider.ZAxis.action != null)
        {
            if (cameraZoomWasEnabledBeforeInventory)
                inputProvider.ZAxis.action.Enable();
            else
                inputProvider.ZAxis.action.Disable();
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
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (inventoryToggleAction != null)
            inventoryToggleAction.performed -= OnInventoryPerformed;

        // Unsubscribe from InventoryManager events
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    #endregion
}