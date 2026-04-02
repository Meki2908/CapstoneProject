using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Main controller for the NPC Blacksmith GUI.
/// 3 tabs: Weapon Socketing (3 slots), Equipment Socketing (4 slots), and Refinement (+0→+7).
/// Handles gem/crystal selection, success rate display, socketing execution, and equipment refinement.
/// </summary>
public class BlacksmithUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button weaponTabButton;
    [SerializeField] private Button equipmentTabButton;
    [SerializeField] private GameObject weaponTabPanel;
    [SerializeField] private GameObject equipmentTabPanel;
    [SerializeField] private Color activeTabColor = new Color(1f, 0.84f, 0.0f);
    [SerializeField] private Color inactiveTabColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("Weapon Tab")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Transform weaponGemSlotsParent; // Contains 3 SocketingSlotUI
    private Button[] weaponGemRemoveButtons;
    [SerializeField] private Image weaponCrystalSlotIcon;
    [SerializeField] private TextMeshProUGUI weaponCrystalSlotText;
    [SerializeField] private Button weaponCrystalClearButton;

    [Header("Equipment Tab")]
    [SerializeField] private Button[] equipmentSlotButtons = new Button[4]; // Head, Body, Legs, Accessory
    [SerializeField] private Image[] equipmentSlotIcons = new Image[4];
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private Transform equipmentGemSlotsParent; // Contains 4 SocketingSlotUI
    private Button[] equipmentGemRemoveButtons;
    [SerializeField] private Image equipmentCrystalSlotIcon;
    [SerializeField] private TextMeshProUGUI equipmentCrystalSlotText;
    [SerializeField] private Button equipmentCrystalClearButton;

    [Header("Gem Drop Slot (shared)")]
    [SerializeField] private Image gemDropIcon;
    [SerializeField] private TextMeshProUGUI gemDropText;

    [Header("Success Rate")]
    [SerializeField] private Image successRateBar;
    [SerializeField] private TextMeshProUGUI successRateText;

    [Header("Socket Button")]
    [SerializeField] private Button socketButton;
    [SerializeField] private TextMeshProUGUI socketButtonText;

    [Header("Result Display")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Image resultIcon;

    [Header("Inventory Panel (Hành Trang)")]
    [SerializeField] private Transform inventoryContent; // Grid content parent
    [SerializeField] private GameObject itemUIPrefab; // Same prefab as main inventory

    [Header("── Inventory Grid Config (tweak in Play Mode) ──")]
    [SerializeField] private Vector2 gridCellSize = new Vector2(180, 200);
    [SerializeField] private Vector2 gridSpacing = new Vector2(10, 10);
    [SerializeField] private int gridPaddingLeft = 10, gridPaddingRight = 10, gridPaddingTop = 10, gridPaddingBottom = 10;
    [SerializeField] private int gridColumns = 6;

    [SerializeField] private Vector2 itemIconSize = new Vector2(130, 130);
    [SerializeField] private float itemNameFontSize = 33f;
    [SerializeField] private float itemAmountFontSize = 26f;
    [SerializeField] private float itemBorderWidth = 6f;
    [SerializeField] private bool showScrollbar = true;


    [Header("Blacksmith Tooltip (self-contained)")]
    private GameObject bsTooltipPanel;
    private TextMeshProUGUI bsTooltipText;
    private RectTransform bsTooltipRect;

    [Header("Refinement Tab")]
    [SerializeField] private Button refinementTabButton;
    [SerializeField] private GameObject refinementTabPanel;
    [SerializeField] private Button[] refineEquipSlotButtons = new Button[4];
    [SerializeField] private Image[] refineEquipSlotIcons = new Image[4];
    [SerializeField] private TextMeshProUGUI refineEquipNameText;
    [SerializeField] private TextMeshProUGUI refineLevelText;
    [SerializeField] private Image[] refineStarImages = new Image[7];
    [SerializeField] private TextMeshProUGUI refineStatsText;
    [SerializeField] private Image refineMaterialIcon;
    [SerializeField] private TextMeshProUGUI refineMaterialText;
    [SerializeField] private Button refineMaterialClearButton;
    [SerializeField] private Image refineSuccessBar;
    [SerializeField] private TextMeshProUGUI refineSuccessText;
    [SerializeField] private Button refineButton;
    [SerializeField] private RectTransform refineGearImage; // small gear icon next to btn
    [SerializeField] private TextMeshProUGUI refineButtonText;
    [SerializeField] private Image fusionSourceIcon;
    [SerializeField] private Image fusionResultIcon;
    [SerializeField] private Button fusionButton;
    [SerializeField] private TextMeshProUGUI fusionInfoText;



    // ─── Runtime State ───────────────────────────────────────────
    private enum ActiveTab { Weapon, Equipment, Refinement }
    private ActiveTab currentTab = ActiveTab.Weapon;

    // Refinement state
    private int selectedRefineSlot = -1;
    private Item selectedRefineMaterial = null;
    private bool isRefining = false;

    private int selectedEquipmentSlot = -1; // 0-3 for equipment tab
    private Item selectedGem = null;
    private int selectedGemSlotIndex = -1;
    private Item selectedCrystal = null;
    private Item selectedEquipment = null;       // Equipment tab: selected equipment item
    private Rarity selectedEquipmentRarity;       // Equipment tab: rarity of selected equipment
    private bool _isClosing = false; // Guard against double Close() invocation

    // ─── Gem Removal Confirmation State ──────────────────────────
    private int pendingRemoveSlotIndex = -1;       // Which gem slot is pending removal
    private bool pendingRemoveIsWeapon = false;     // Is pending removal from weapon tab?
    private Coroutine pendingRemoveResetCoroutine;  // Auto-reset timer

    // Cached gem slot UIs
    private SocketingSlotUI[] weaponGemSlots;
    private SocketingSlotUI[] equipmentGemSlots;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Start()
    {
        // ── Ensure SocketingManager exists ──
        if (SocketingManager.Instance == null)
        {
            var smGO = new GameObject("SocketingManager");
            smGO.AddComponent<SocketingManager>();
            Debug.Log("[BlacksmithUI] Auto-created SocketingManager (was missing from scene)");
        }

        // Create self-contained tooltip (previously done in SetupReferences)
        CreateBlacksmithTooltip();

        // Button listeners
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (weaponTabButton) weaponTabButton.onClick.AddListener(() => SwitchTab(ActiveTab.Weapon));
        if (equipmentTabButton) equipmentTabButton.onClick.AddListener(() => SwitchTab(ActiveTab.Equipment));
        if (refinementTabButton) refinementTabButton.onClick.AddListener(() => SwitchTab(ActiveTab.Refinement));
        if (socketButton) socketButton.onClick.AddListener(OnSocketButtonClicked);

        // Crystal clear buttons
        if (weaponCrystalClearButton) weaponCrystalClearButton.onClick.AddListener(ClearSelectedCrystal);
        if (equipmentCrystalClearButton) equipmentCrystalClearButton.onClick.AddListener(ClearSelectedCrystal);
        if (refineMaterialClearButton) refineMaterialClearButton.onClick.AddListener(ClearSelectedRefineMaterial);

        // Refinement button
        if (refineButton) refineButton.onClick.AddListener(OnRefineButtonClicked);
        if (fusionButton) fusionButton.onClick.AddListener(OnFusionButtonClicked);

        // Refinement equip slot selection
        for (int i = 0; i < refineEquipSlotButtons.Length; i++)
        {
            int index = i;
            if (refineEquipSlotButtons[i] != null)
                refineEquipSlotButtons[i].onClick.AddListener(() => SelectRefineEquipSlot(index));
        }

        // Equipment slot selection
        for (int i = 0; i < equipmentSlotButtons.Length; i++)
        {
            int index = i;
            if (equipmentSlotButtons[i] != null)
                equipmentSlotButtons[i].onClick.AddListener(() => SelectEquipmentSlot(index));
        }

        // Get SocketingSlotUI components
        if (weaponGemSlotsParent)
            weaponGemSlots = weaponGemSlotsParent.GetComponentsInChildren<SocketingSlotUI>(true);
        if (equipmentGemSlotsParent)
            equipmentGemSlots = equipmentGemSlotsParent.GetComponentsInChildren<SocketingSlotUI>(true);

        // Initialize slot callbacks
        InitializeGemSlots(weaponGemSlots, 3);
        InitializeGemSlots(equipmentGemSlots, 1);

        // Auto-find & wire remove buttons for weapon gem slots
        // Structure: WeaponGemSlots → SlotGroup_X → [GemSlot_X, RemoveBtn_X]
        if (weaponGemRemoveButtons == null && weaponGemSlots != null)
        {
            weaponGemRemoveButtons = new Button[weaponGemSlots.Length];
            for (int i = 0; i < weaponGemSlots.Length; i++)
            {
                // RemoveBtn_X is a sibling of GemSlot_X inside SlotGroup_X
                var slotGroup = weaponGemSlots[i].transform.parent;
                var btn = slotGroup?.Find($"RemoveBtn_{i}")?.GetComponent<Button>();
                if (btn == null) // fallback: try any child Button that has "Remove" in name
                {
                    foreach (Transform child in slotGroup)
                    {
                        if (child.name.StartsWith("RemoveBtn"))
                        {
                            btn = child.GetComponent<Button>();
                            break;
                        }
                    }
                }
                weaponGemRemoveButtons[i] = btn;
            }
            Debug.Log($"[BlacksmithUI] Weapon remove buttons: [{string.Join(", ", System.Array.ConvertAll(weaponGemRemoveButtons, b => (b != null).ToString()))}]");
        }
        if (weaponGemRemoveButtons != null)
        {
            for (int i = 0; i < weaponGemRemoveButtons.Length; i++)
            {
                int slotIdx = i;
                if (weaponGemRemoveButtons[i] != null)
                    weaponGemRemoveButtons[i].onClick.AddListener(() => OnRemoveGemSlot(slotIdx, true));
            }
        }
        // Auto-find & wire remove buttons for equipment gem slots
        if (equipmentGemRemoveButtons == null && equipmentGemSlots != null)
        {
            equipmentGemRemoveButtons = new Button[equipmentGemSlots.Length];
            for (int i = 0; i < equipmentGemSlots.Length; i++)
            {
                var slotGroup = equipmentGemSlots[i].transform.parent;
                var btn = slotGroup?.Find($"RemoveBtn_{i}")?.GetComponent<Button>();
                if (btn == null)
                {
                    foreach (Transform child in slotGroup)
                    {
                        if (child.name.StartsWith("RemoveBtn"))
                        {
                            btn = child.GetComponent<Button>();
                            break;
                        }
                    }
                }
                equipmentGemRemoveButtons[i] = btn;
            }
        }
        if (equipmentGemRemoveButtons != null)
        {
            for (int i = 0; i < equipmentGemRemoveButtons.Length; i++)
            {
                int slotIdx = i;
                if (equipmentGemRemoveButtons[i] != null)
                    equipmentGemRemoveButtons[i].onClick.AddListener(() => OnRemoveGemSlot(slotIdx, false));
            }
        }

        if (resultPanel) resultPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(false);

        // ─── Subscribe events để tự đồng bộ với inventory ────
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentOrGemsChanged;
        if (WeaponGemManager.Instance != null)
            WeaponGemManager.Instance.OnGemsChanged += OnWeaponGemsChanged;
    }

    void OnDestroy()
    {
        // ─── Unsubscribe events ──────────────────────────────
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= OnInventoryChanged;
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentOrGemsChanged;
        if (WeaponGemManager.Instance != null)
            WeaponGemManager.Instance.OnGemsChanged -= OnWeaponGemsChanged;
    }

    // ─── Event Handlers (auto-sync với Inventory) ────────────────

    void OnInventoryChanged()
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshAll();
    }

    void OnEquipmentOrGemsChanged()
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshAll();
    }

    void OnWeaponGemsChanged(WeaponType wt)
    {
        if (mainPanel != null && mainPanel.activeSelf)
            RefreshAll();
    }

    void InitializeGemSlots(SocketingSlotUI[] slots, int maxSlots)
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length && i < maxSlots; i++)
        {
            int slotIndex = i;
            slots[i].OnSlotClicked += () => OnGemSlotClicked(slotIndex);
            slots[i].OnSlotDoubleClicked += () => OnGemSlotDoubleClicked(slotIndex);
        }
    }

    // ─── Open / Close ────────────────────────────────────────────

    public void Open()
    {
        if (mainPanel) mainPanel.SetActive(true);
        ApplyGridConfig(); // Apply grid config from Inspector values
        // Suppress global tooltip while Blacksmith is open
        if (ItemTooltipManager.Instance != null)
            ItemTooltipManager.Instance.SuppressTooltip = true;
        ClearSelection();
        SwitchTab(ActiveTab.Weapon);
        RefreshAll();
    }

    public void Close()
    {
        if (_isClosing) return; // Prevent double-invocation loop
        _isClosing = true;

        if (mainPanel) mainPanel.SetActive(false);
        ClearSelection();

        // Re-enable global tooltip
        if (ItemTooltipManager.Instance != null)
            ItemTooltipManager.Instance.SuppressTooltip = false;

        // Notify NPC to close
        var npc = FindFirstObjectByType<BlacksmithNPC>();
        if (npc != null) npc.CloseBlacksmith();

        _isClosing = false;
    }

    // ─── Tab Switching ───────────────────────────────────────────

    void SwitchTab(ActiveTab tab)
    {
        currentTab = tab;

        if (weaponTabPanel) weaponTabPanel.SetActive(tab == ActiveTab.Weapon);
        if (equipmentTabPanel) equipmentTabPanel.SetActive(tab == ActiveTab.Equipment);
        if (refinementTabPanel) refinementTabPanel.SetActive(tab == ActiveTab.Refinement);

        // Hide shared socketing UI when on Refinement tab
        bool isSocketingTab = (tab != ActiveTab.Refinement);

        // Find and toggle SuccessArea + RawImage by name from ContentArea
        Transform contentParent = refinementTabPanel != null ? refinementTabPanel.transform.parent : null;
        if (contentParent == null && weaponTabPanel != null) contentParent = weaponTabPanel.transform.parent;

        if (contentParent != null)
        {
            Transform successArea = contentParent.Find("SuccessArea");
            if (successArea != null) successArea.gameObject.SetActive(isSocketingTab);

            Transform rawImage = contentParent.Find("RawImage");
            if (rawImage != null) rawImage.gameObject.SetActive(isSocketingTab);
        }

        // Fallback: hide individual elements
        if (socketButton != null)
            socketButton.gameObject.SetActive(isSocketingTab);

        // Tab button colors
        UpdateTabButtonColor(weaponTabButton, tab == ActiveTab.Weapon);
        UpdateTabButtonColor(equipmentTabButton, tab == ActiveTab.Equipment);
        UpdateTabButtonColor(refinementTabButton, tab == ActiveTab.Refinement);

        ClearSelection();
        RefreshAll();
    }

    void UpdateTabButtonColor(Button btn, bool active)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = active ? activeTabColor : inactiveTabColor;
        btn.colors = colors;

        // Also update text color
        var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.color = active ? Color.black : Color.white;
    }

    // ─── Equipment Slot Selection ────────────────────────────────

    private int lastEquipClickIndex = -1;
    private float lastEquipClickTime = -1f;
    private int pendingUnequipSlot = -1; // -1 = no pending, >= 0 = waiting for confirm

    void SelectEquipmentSlot(int index)
    {
        float now = Time.unscaledTime;

        // ── If we have a pending unequip on this slot → confirm it ──
        if (pendingUnequipSlot == index)
        {
            var equipped = EquipmentManager.Instance?.GetEquippedItemByIndex(index);
            if (equipped != null)
            {
                pendingUnequipSlot = -1;
                StopCoroutine(nameof(ResetPendingUnequipCoroutine));
                EquipmentManager.Instance.RemoveItemByIndex(index);
                Debug.Log($"[BlacksmithUI] Unequipped {equipped.itemName} from slot {index}");

                if (resultPanel != null && resultText != null)
                {
                    resultPanel.SetActive(true);
                    ConfigureResultText();
                    resultText.text = $"Unequipped {equipped.itemName}.\nReturned to inventory.";
                    resultText.color = new Color(0.2f, 0.9f, 0.3f);
                    StopCoroutine(nameof(HideResultCoroutine));
                    StartCoroutine(HideResultCoroutine());
                }
            }
            ClearGemAndCrystalSelection();
            selectedEquipmentSlot = -1;
            lastEquipClickIndex = -1;
            RefreshAll();
            return;
        }

        // ── Double-click detection: show warning ──
        if (index == lastEquipClickIndex && (now - lastEquipClickTime) < 1.0f)
        {
            if (index >= 0 && index < 4 && EquipmentManager.Instance != null)
            {
                var equipped = EquipmentManager.Instance.GetEquippedItemByIndex(index);
                if (equipped != null)
                {
                    pendingUnequipSlot = index;
                    if (resultPanel != null && resultText != null)
                    {
                        resultPanel.SetActive(true);
                        ConfigureResultText();
                        resultText.text = $"Unequip {equipped.itemName}?\nClick again to confirm.";
                        resultText.color = new Color(1f, 0.7f, 0.2f);
                    }
                    StopCoroutine(nameof(ResetPendingUnequipCoroutine));
                    StartCoroutine(ResetPendingUnequipCoroutine());
                    return;
                }
            }
        }

        // ── Single click: select slot ──
        lastEquipClickIndex = index;
        lastEquipClickTime = now;
        pendingUnequipSlot = -1;

        selectedEquipmentSlot = index;
        ClearGemAndCrystalSelection();
        RefreshEquipmentDisplay();
        RefreshGemSlots();
        RefreshViewports();
        UpdateSuccessRate();
    }

    IEnumerator ResetPendingUnequipCoroutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        pendingUnequipSlot = -1;
        if (resultPanel != null) resultPanel.SetActive(false);
    }

    // ─── Gem Slot Interaction ────────────────────────────────────

    void OnGemSlotClicked(int slotIndex)
    {
        // If we have a selected gem → place it in this slot (tracked for socketing)
        selectedGemSlotIndex = slotIndex;
        Debug.Log($"[BlacksmithUI] Gem slot {slotIndex} clicked — selectedGemSlotIndex = {selectedGemSlotIndex}");
        RefreshGemSlots();      // Visual highlight on selected slot
        UpdateSuccessRate();
    }

    void OnGemSlotDoubleClicked(int slotIndex)
    {
        // Double-click on gem slot → select slot only (removal via X button)
        selectedGemSlotIndex = slotIndex;
        RefreshGemSlots();
        UpdateSuccessRate();
    }

    void OnRemoveGemSlot(int slotIndex, bool isWeapon)
    {
        // Check if this slot actually has a gem
        Item gemInSlot = null;
        if (isWeapon)
        {
            var wc = FindFirstObjectByType<WeaponController>();
            if (wc != null && WeaponGemManager.Instance != null)
                gemInSlot = WeaponGemManager.Instance.GetEquippedGem(wc.GetCurrentWeapon().weaponType, slotIndex);
        }
        else if (selectedEquipmentSlot >= 0 && EquipmentManager.Instance != null)
        {
            gemInSlot = EquipmentManager.Instance.GetEquippedGem(selectedEquipmentSlot, slotIndex);
        }

        if (gemInSlot == null) return; // Slot trống → không làm gì

        // ── Lần nhấn 1: Hiện cảnh báo ──
        if (pendingRemoveSlotIndex != slotIndex || pendingRemoveIsWeapon != isWeapon)
        {
            pendingRemoveSlotIndex = slotIndex;
            pendingRemoveIsWeapon = isWeapon;

            // Hiện cảnh báo
            ShowRemovalWarning(gemInSlot);

            // Auto-reset sau 3 giây nếu không nhấn lần 2
            if (pendingRemoveResetCoroutine != null)
                StopCoroutine(pendingRemoveResetCoroutine);
            pendingRemoveResetCoroutine = StartCoroutine(ResetPendingRemovalCoroutine());

            Debug.Log($"[BlacksmithUI] Removal warning shown for {gemInSlot.itemName} at slot {slotIndex}");
            return;
        }

        // ── Lần nhấn 2: Thực hiện gỡ gem ──
        Debug.Log($"[BlacksmithUI] Confirmed removal of {gemInSlot.itemName} at slot {slotIndex}");

        if (isWeapon)
        {
            var wc = FindFirstObjectByType<WeaponController>();
            if (wc != null && WeaponGemManager.Instance != null)
            {
                WeaponGemManager.Instance.RemoveGem(wc.GetCurrentWeapon().weaponType, slotIndex);
            }
        }
        else if (selectedEquipmentSlot >= 0)
        {
            EquipmentManager.Instance?.RemoveGemFromSlot(selectedEquipmentSlot, slotIndex);
        }

        // Reset trạng thái và hiện kết quả
        ClearPendingRemoval();
        ShowRemovalSuccess(gemInSlot);
        RefreshAll();
    }

    // ─── Removal Confirmation Helpers ────────────────────────────

    void ShowRemovalWarning(Item gem)
    {
        if (resultPanel == null || resultText == null) return;
        resultPanel.SetActive(true);
        ConfigureResultText();
        resultText.text = $"Removing {gem.itemName} will DESTROY it!\nClick X again to confirm.";
        resultText.color = new Color(1f, 0.7f, 0.2f); // Warning orange
    }

    void ShowRemovalSuccess(Item gem)
    {
        if (resultPanel == null || resultText == null) return;
        resultPanel.SetActive(true);
        ConfigureResultText();
        resultText.text = $"Removed {gem.itemName}.\nGem has been destroyed.";
        resultText.color = new Color(0.9f, 0.3f, 0.2f); // Red

        StopCoroutine(nameof(HideResultCoroutine));
        StartCoroutine(HideResultCoroutine());
    }

    void ClearPendingRemoval()
    {
        pendingRemoveSlotIndex = -1;
        pendingRemoveIsWeapon = false;
        if (pendingRemoveResetCoroutine != null)
        {
            StopCoroutine(pendingRemoveResetCoroutine);
            pendingRemoveResetCoroutine = null;
        }
    }

    IEnumerator ResetPendingRemovalCoroutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        pendingRemoveSlotIndex = -1;
        pendingRemoveIsWeapon = false;
        pendingRemoveResetCoroutine = null;

        // Ẩn cảnh báo
        if (resultPanel) resultPanel.SetActive(false);
        Debug.Log("[BlacksmithUI] Removal confirmation timed out — reset.");
    }

    // ─── Gem / Crystal Selection from Viewports ──────────────────

    public void SelectGem(Item gem)
    {
        // Only accept actual Gems — reject potions, materials, etc.
        if (gem == null || gem.itemType != ItemType.Gems)
        {
            Debug.LogWarning($"[BlacksmithUI] Rejected non-gem item: {gem?.itemName} (type={gem?.itemType})");
            return;
        }
        selectedGem = gem;
        UpdateGemDropDisplay();
        UpdateSuccessRate();
        RefreshViewports(); // Highlight selected in inventory
    }

    public void SelectCrystal(Item crystal)
    {
        selectedCrystal = crystal;
        UpdateCrystalSlotDisplay();
        UpdateSuccessRate();
    }

    void ClearSelectedCrystal()
    {
        selectedCrystal = null;
        UpdateCrystalSlotDisplay();
        UpdateSuccessRate();
    }

    void ClearGemAndCrystalSelection()
    {
        selectedGem = null;
        selectedCrystal = null;
        selectedGemSlotIndex = -1;
        selectedEquipment = null;
        ClearPendingRemoval();
        UpdateGemDropDisplay();
    }

    void ClearSelection()
    {
        ClearGemAndCrystalSelection();
        selectedEquipmentSlot = -1;
    }

    // ─── Equipment Item Selection (Equipment Tab) ────────────────

    /// <summary>
    /// Select an equipment item from inventory for socketing (Equipment tab).
    /// Auto-detects the target slot from the item's equipmentSlot.
    /// </summary>
    public void SelectEquipmentItem(Item equipItem, Rarity rarity)
    {
        if (equipItem == null || equipItem.itemType != ItemType.Equipment) return;
        selectedEquipment = equipItem;
        selectedEquipmentRarity = rarity;
        selectedEquipmentSlot = EquipmentSlotToIndex(equipItem.equipmentSlot);
        UpdateGemDropDisplay();
        UpdateSuccessRate();
        RefreshEquipmentDisplay();
        RefreshViewports();
    }

    // ─── Socket Button ───────────────────────────────────────────

    void OnSocketButtonClicked()
    {
        Debug.Log($"[BlacksmithUI] OnSocketButtonClicked called! tab={currentTab}, gem={selectedGem?.itemName}, crystal={selectedCrystal?.itemName}, slotIdx={selectedGemSlotIndex}, equipment={selectedEquipment?.itemName}");

        if (SocketingManager.Instance == null) { Debug.LogError("[BlacksmithUI] SocketingManager.Instance is NULL!"); return; }

        // Validate based on tab
        if (currentTab == ActiveTab.Weapon)
        {
            if (selectedGem == null || selectedCrystal == null || selectedGemSlotIndex < 0)
            {
                Debug.LogWarning($"[BlacksmithUI] Weapon validation FAILED: gem={selectedGem != null}, crystal={selectedCrystal != null}, slot={selectedGemSlotIndex}");
                return;
            }
        }
        else
        {
            if (selectedEquipment == null || selectedCrystal == null)
            {
                Debug.LogWarning($"[BlacksmithUI] Equipment validation FAILED: equip={selectedEquipment != null}, crystal={selectedCrystal != null}");
                return;
            }
        }

        Debug.Log("[BlacksmithUI] Validation PASSED — starting socketing animation!");

        // Disable button during animation
        if (socketButton) socketButton.interactable = false;

        StartCoroutine(SocketingAnimationCoroutine());
    }

    IEnumerator SocketingAnimationCoroutine()
    {
        // ── Play forge sound ──
        SoundManager.PlaySound(SoundType.Blacksmith_Forge);

        // ── Spin the gem drop icon during socketing (2 seconds) ──
        float duration = 0.3f;
        float elapsed = 0f;
        float spinSpeed = 720f; // degrees per second

        Transform spinTarget = null;
        if (socketButton != null) spinTarget = socketButton.transform;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (spinTarget != null)
                spinTarget.Rotate(0, 0, -spinSpeed * Time.unscaledDeltaTime);
            yield return null;
        }

        // Reset rotation
        if (spinTarget != null)
            spinTarget.localRotation = Quaternion.identity;

        // ── Perform actual socketing ──
        SocketResult result;

        if (currentTab == ActiveTab.Weapon)
        {
            var wc = FindFirstObjectByType<WeaponController>();
            if (wc == null) { if (socketButton) socketButton.interactable = true; yield break; }
            result = SocketingManager.Instance.TrySocketWeapon(
                wc.GetCurrentWeapon().weaponType, selectedGemSlotIndex, selectedGem, selectedCrystal);
        }
        else
        {
            if (selectedEquipment == null || selectedEquipmentSlot < 0)
            { if (socketButton) socketButton.interactable = true; yield break; }
            result = SocketingManager.Instance.TrySocketEquipmentItem(
                selectedEquipmentSlot, selectedEquipment, selectedEquipmentRarity, selectedCrystal);
        }

        ShowResult(result);

        // Clear consumed items
        if (result == SocketResult.Success || result == SocketResult.Fail)
        {
            selectedCrystal = null;
            if (currentTab == ActiveTab.Equipment)
                selectedEquipment = null; // Clear equipment selection after attempt
        }

        RefreshAll();
    }

    void ShowResult(SocketResult result)
    {
        if (resultPanel == null || resultText == null) return;
        resultPanel.SetActive(true);
        ConfigureResultText();

        switch (result)
        {
            case SocketResult.Success:
                resultText.text = "SOCKETING SUCCESS!";
                resultText.color = new Color(0.2f, 0.9f, 0.3f);
                SoundManager.PlaySound(SoundType.Blacksmith_Socket_Success);
                break;
            case SocketResult.Fail:
                if (currentTab == ActiveTab.Weapon)
                    resultText.text = "SOCKETING FAILED!\nCrystal Stone and Gem destroyed.";
                else
                    resultText.text = "SOCKETING FAILED!\nCrystal Stone consumed. Equipment returned.";
                resultText.color = new Color(0.9f, 0.3f, 0.2f);
                SoundManager.PlaySound(SoundType.Blacksmith_Socket_Fail);
                break;
            case SocketResult.NoGem:
                resultText.text = "No Gem selected!";
                resultText.color = Color.yellow;
                break;
            case SocketResult.NoCrystal:
                resultText.text = "No Crystal Stone selected!";
                resultText.color = Color.yellow;
                break;
            case SocketResult.NoTarget:
                resultText.text = "No equipment or weapon available!";
                resultText.color = Color.yellow;
                break;
            default:
                resultText.text = "Socketing error.";
                resultText.color = Color.gray;
                break;
        }

        resultText.ForceMeshUpdate();

        // Auto-hide after 2 seconds (unscaled time — works even when timeScale=0)
        StopCoroutine(nameof(HideResultCoroutine));
        StartCoroutine(HideResultCoroutine());
    }

    /// <summary>
    /// Shared text config for resultPanel — used by ShowResult, ShowRemovalWarning, ShowRemovalSuccess
    /// </summary>
    void ConfigureResultText()
    {
        // Text settings
        resultText.enableWordWrapping = true;
        resultText.overflowMode = TMPro.TextOverflowModes.Overflow;
        resultText.alignment = TextAlignmentOptions.TopLeft;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 14;
        resultText.fontSizeMax = 36;

        // Force text RectTransform to fit inside panel
        float pad = 20f;
        var textRT = resultText.rectTransform;
        var panelRT = resultPanel.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(
            panelRT.rect.width - pad * 2,
            panelRT.rect.height - pad * 2
        );
    }

    IEnumerator HideResultCoroutine()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (resultPanel) resultPanel.SetActive(false);
    }

    // ─── Refresh Methods ─────────────────────────────────────────

    void RefreshAll()
    {
        RefreshWeaponDisplay();
        RefreshEquipmentDisplay();
        RefreshGemSlots();
        RefreshViewports();
        UpdateCrystalSlotDisplay();
        UpdateGemDropDisplay();
        UpdateSuccessRate();
        RefreshRefinementDisplay();
    }

    void RefreshWeaponDisplay()
    {
        if (currentTab != ActiveTab.Weapon) return;

        var wc = FindFirstObjectByType<WeaponController>();
        if (wc != null && wc.GetCurrentWeapon() != null)
        {
            var weapon = wc.GetCurrentWeapon();
            if (weaponIcon) { weaponIcon.sprite = weapon.icon; weaponIcon.enabled = true; }
            if (weaponNameText) weaponNameText.text = weapon.weaponName;
        }
        else
        {
            if (weaponIcon) weaponIcon.enabled = false;
            if (weaponNameText) weaponNameText.text = "No weapon equipped";
        }
    }

    void RefreshEquipmentDisplay()
    {
        if (currentTab != ActiveTab.Equipment) return;
        if (EquipmentManager.Instance == null) return;

        // Update 4 equipment slot icons
        string[] slotNames = { "Head", "Body", "Legs", "Accessory" };
        for (int i = 0; i < 4; i++)
        {
            var item = EquipmentManager.Instance.GetEquippedItemByIndex(i);
            if (i < equipmentSlotIcons.Length && equipmentSlotIcons[i] != null)
            {
                if (item != null)
                {
                    equipmentSlotIcons[i].sprite = item.icon;
                    equipmentSlotIcons[i].enabled = true;
                    equipmentSlotIcons[i].color = Color.white;
                }
                else
                {
                    equipmentSlotIcons[i].enabled = true;
                    equipmentSlotIcons[i].color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
            }

            // Highlight selected slot
            if (i < equipmentSlotButtons.Length && equipmentSlotButtons[i] != null)
            {
                var colors = equipmentSlotButtons[i].colors;
                colors.normalColor = (i == selectedEquipmentSlot) ?
                    new Color(1f, 0.84f, 0f, 0.8f) : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                equipmentSlotButtons[i].colors = colors;
            }
        }

        // Update selected equipment name
        if (equipmentNameText)
        {
            if (selectedEquipmentSlot >= 0)
            {
                var item = EquipmentManager.Instance.GetEquippedItemByIndex(selectedEquipmentSlot);
                if (item != null)
                {
                    Rarity r = EquipmentManager.Instance.GetEquippedRarity(selectedEquipmentSlot);
                    string colorHex = Item.GetRarityColorHex(r);
                    equipmentNameText.text = $"<color={colorHex}>{item.itemName} [{r}]</color>";
                }
                else
                {
                    equipmentNameText.text = $"Slot {slotNames[selectedEquipmentSlot]} — Empty";
                }
            }
            else
            {
                equipmentNameText.text = "Select equipment slot";
            }
        }
    }

    void RefreshGemSlots()
    {
        if (currentTab == ActiveTab.Weapon)
        {
            if (weaponGemSlots == null) { Debug.LogWarning("[BlacksmithUI] weaponGemSlots is NULL!"); return; }
            var wc = FindFirstObjectByType<WeaponController>();
            WeaponType wt = (wc != null) ? wc.GetCurrentWeapon().weaponType : WeaponType.None;

            Debug.Log($"[BlacksmithUI] RefreshGemSlots: wc={wc != null}, wt={wt}, WeaponGemMgr={WeaponGemManager.Instance != null}, slots={weaponGemSlots.Length}");

            for (int i = 0; i < weaponGemSlots.Length && i < 3; i++)
            {
                Item gem = null;
                if (wt != WeaponType.None && WeaponGemManager.Instance != null)
                    gem = WeaponGemManager.Instance.GetEquippedGem(wt, i);

                Debug.Log($"[BlacksmithUI] Slot {i}: gem={gem?.itemName ?? "null"}, icon={gem?.icon != null}");
                weaponGemSlots[i].SetGem(gem, i == selectedGemSlotIndex);
            }
        }
        // Equipment tab: no gem slots (equipment uses equipment items, not gems)
    }

    void RefreshViewports()
    {
        RefreshInventoryPanel();
    }

    /// <summary>
    /// Apply grid config from serialized fields — call from RefreshInventoryPanel or live Update
    /// </summary>
    void ApplyGridConfig()
    {
        if (inventoryContent == null) return;

        // ── Grid Layout ──
        var grid = inventoryContent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid != null)
        {
            grid.cellSize = gridCellSize;
            grid.spacing = gridSpacing;
            grid.constraintCount = gridColumns;
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.padding.left = gridPaddingLeft;
            grid.padding.right = gridPaddingRight;
            grid.padding.top = gridPaddingTop;
            grid.padding.bottom = gridPaddingBottom;
            UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(inventoryContent as RectTransform);
        }

        // ── Scroll settings ──
        var scrollRect = inventoryContent.GetComponentInParent<UnityEngine.UI.ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;



            // Scrollbar visibility
            if (scrollRect.verticalScrollbar != null)
                scrollRect.verticalScrollbar.gameObject.SetActive(showScrollbar);
        }

        // ── Item-level sizing ──
        foreach (Transform child in inventoryContent)
        {
            // Icon size
            var iconTf = child.Find("Item Icon");
            if (iconTf != null)
            {
                var iconRT = iconTf.GetComponent<RectTransform>();
                if (iconRT != null) iconRT.sizeDelta = itemIconSize;
            }
            // Name font
            var nameTf = child.Find("Item name");
            if (nameTf != null)
            {
                var tmp = nameTf.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = itemNameFontSize;
            }
            // Amount font
            var amtTf = child.Find("Item amount");
            if (amtTf != null)
            {
                var tmp = amtTf.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.fontSize = itemAmountFontSize;
            }
            // Border width
            var outline = child.GetComponent<Outline>();
            if (outline != null)
                outline.effectDistance = new Vector2(itemBorderWidth, itemBorderWidth);
        }
    }

    void RefreshInventoryPanel()
    {
        if (inventoryContent == null || itemUIPrefab == null) return;
        if (InventoryManager.Instance == null) return;

        // Clear existing
        foreach (Transform child in inventoryContent)
            Destroy(child.gameObject);

        // Populate with items from inventory (filtered when equipment slot is selected)
        var allItems = InventoryManager.Instance.GetAllItemsWithRarity();

        // Determine filter: when in Equipment tab with a slot selected, only show matching items
        EquipmentSlotType? filterSlot = null;
        if (currentTab == ActiveTab.Equipment && selectedEquipmentSlot >= 0)
        {
            EquipmentSlotType[] slotMap = { EquipmentSlotType.Head, EquipmentSlotType.Body,
                                             EquipmentSlotType.Legs, EquipmentSlotType.Accessory };
            if (selectedEquipmentSlot < slotMap.Length)
                filterSlot = slotMap[selectedEquipmentSlot];
        }
        // Items already sorted by InventoryManager: Equipment → Crystal → Gems, rarity ascending

        foreach (var (item, amount, rarity) in allItems)
        {
            if (item == null || amount <= 0) continue;

            // Filter: Weapon tab → only Gems + CrystalStone
            if (currentTab == ActiveTab.Weapon)
            {
                if (item.itemType != ItemType.Gems && item.itemType != ItemType.CrystalStone)
                    continue;
            }

            // Filter: Equipment tab → Equipment + CrystalStone only
            if (currentTab == ActiveTab.Equipment)
            {
                if (item.itemType == ItemType.CrystalStone)
                { /* always show crystals */ }
                else if (item.itemType == ItemType.Equipment)
                {
                    // If a slot is selected, only show matching equipment
                    if (filterSlot.HasValue && item.equipmentSlot != filterSlot.Value)
                        continue;
                }
                else
                    continue; // not equipment or crystal
            }

            // Filter: Refinement tab → only Material items (refinement stones)
            if (currentTab == ActiveTab.Refinement)
            {
                if (item.itemType != ItemType.Material || item.refinementTier <= 0)
                    continue;
            }

            GameObject go = Instantiate(itemUIPrefab, inventoryContent);
            // Enable drag-scroll through item buttons
            if (go.GetComponent<ScrollDragPassthrough>() == null)
                go.AddComponent<ScrollDragPassthrough>();
            var itemUI = go.GetComponent<ItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(item, amount, null, rarity);
                itemUI.SetRemoveButtonVisible(false);
                itemUI.SetUseTooltip(false); // Disable global tooltip — Blacksmith has its own BS_Tooltip
            }

            // ── Rarity background color + border ──
            var bgImage = go.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = GetRarityBgColor(rarity);
            }
            var rarityOutline = go.AddComponent<Outline>();
            rarityOutline.effectColor = GetRarityBorderColor(rarity);
            rarityOutline.effectDistance = new Vector2(itemBorderWidth, itemBorderWidth);

            // Item sizing uses prefab defaults — no code override

            // ── Click behavior — use existing Button from prefab ──
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.navigation = new Navigation { mode = Navigation.Mode.None }; // Don't block drag
            // Clear any prefab listeners, add Blacksmith-specific ones
            btn.onClick.RemoveAllListeners();
            Item capturedItem = item;

            if (item.itemType == ItemType.Gems)
            {
                btn.onClick.AddListener(() => SelectGem(capturedItem));

                if (selectedGem != null && selectedGem.id == item.id)
                {
                    var outline = go.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.84f, 0f);
                    outline.effectDistance = new Vector2(6, 6);
                }
            }
            else if (item.itemType == ItemType.CrystalStone)
            {
                btn.onClick.AddListener(() => SelectCrystal(capturedItem));

                if (selectedCrystal != null && selectedCrystal.id == item.id)
                {
                    var outline = go.AddComponent<Outline>();
                    outline.effectColor = new Color(0.6f, 0.2f, 0.9f);
                    outline.effectDistance = new Vector2(6, 6);
                }
            }
            else if (item.itemType == ItemType.Equipment)
            {
                Rarity capturedRarity = rarity;
                btn.onClick.AddListener(() => SelectEquipmentItem(capturedItem, capturedRarity));

                // Highlight if currently selected
                if (selectedEquipment != null && selectedEquipment.id == item.id && selectedEquipmentRarity == rarity)
                {
                    var outline = go.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.84f, 0f);
                    outline.effectDistance = new Vector2(6, 6);
                }
            }
            else if (item.itemType == ItemType.Material && item.refinementTier > 0)
            {
                btn.onClick.AddListener(() => SelectRefineMaterial(capturedItem));

                // Highlight if currently selected
                if (selectedRefineMaterial != null && selectedRefineMaterial.id == item.id)
                {
                    var outline = go.AddComponent<Outline>();
                    outline.effectColor = new Color(0.9f, 0.5f, 0.1f);
                    outline.effectDistance = new Vector2(6, 6);
                }
            }
            // All other items — display normally, no dimming

            // ── Tooltip hover via EventTrigger ──
            // Pass rolled value for gems
            float rolledVal = -1f;
            if (item.HasRandomStats && InventoryManager.Instance != null)
            {
                float peek = InventoryManager.Instance.PeekNextRoll(item.id, rarity);
                if (peek >= 0f) rolledVal = peek;
            }
            AddTooltipTrigger(go, item, rarity, rolledVal);
        }
    }

    void UpdateCrystalSlotDisplay()
    {
        Image icon = (currentTab == ActiveTab.Weapon) ? weaponCrystalSlotIcon : equipmentCrystalSlotIcon;
        TextMeshProUGUI text = (currentTab == ActiveTab.Weapon) ? weaponCrystalSlotText : equipmentCrystalSlotText;

        if (icon != null)
        {
            if (selectedCrystal != null)
            {
                icon.sprite = selectedCrystal.icon;
                icon.enabled = true;
                icon.color = Color.white;
            }
            else
            {
                icon.enabled = true;
                icon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            }
        }

        if (text != null)
        {
            text.text = (selectedCrystal != null)
                ? $"<color={Item.GetRarityColorHex(selectedCrystal.rarity)}>{selectedCrystal.itemName}</color>"
                : "";
        }

        SetDropSlotTooltip(icon, selectedCrystal, "Select Crystal Stone from inventory\nto increase socketing success rate");
    }

    void UpdateGemDropDisplay()
    {
        // Determine what item to show in the drop zone based on current tab
        Item displayItem = null;
        string emptyHint = "";

        if (currentTab == ActiveTab.Weapon)
        {
            displayItem = selectedGem;
            emptyHint = "Select Gem from inventory\nto socket into weapon";
        }
        else
        {
            displayItem = selectedEquipment;
            emptyHint = "Select Equipment from inventory\nto equip into slot";
        }

        if (gemDropIcon != null)
        {
            if (displayItem != null)
            {
                gemDropIcon.sprite = displayItem.icon;
                gemDropIcon.enabled = true;
                gemDropIcon.color = Color.white;
            }
            else
            {
                gemDropIcon.enabled = true;
                gemDropIcon.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            }
        }

        if (gemDropText != null)
        {
            if (displayItem != null)
            {
                Rarity r = (currentTab == ActiveTab.Weapon && selectedGem != null) ? selectedGem.rarity
                         : (currentTab == ActiveTab.Equipment && selectedEquipment != null) ? selectedEquipmentRarity
                         : Rarity.Common;
                gemDropText.text = $"<color={Item.GetRarityColorHex(r)}>{displayItem.itemName}</color>";
            }
            else
            {
                gemDropText.text = "";
            }
        }

        SetDropSlotTooltip(gemDropIcon, displayItem, emptyHint);
    }

    void UpdateSuccessRate()
    {
        if (SocketingManager.Instance == null) return;

        float rate = 0f;
        bool canSocket = false;

        if (selectedCrystal != null)
        {
            Rarity targetRarity = Rarity.Common;

            if (currentTab == ActiveTab.Weapon)
            {
                targetRarity = (selectedGem != null) ? selectedGem.rarity : Rarity.Common;
                canSocket = selectedGem != null && selectedGemSlotIndex >= 0;
            }
            else
            {
                // Equipment tab: rate based on equipment rarity vs crystal rarity
                if (selectedEquipment != null)
                {
                    targetRarity = selectedEquipmentRarity;
                    canSocket = true;
                }
            }

            rate = SocketingManager.Instance.CalculateSuccessRate(targetRarity, selectedCrystal.rarity);
        }

        Debug.Log($"[BlacksmithUI] UpdateSuccessRate: tab={currentTab}, gem={selectedGem?.itemName}, crystal={selectedCrystal?.itemName}, slotIdx={selectedGemSlotIndex}, canSocket={canSocket}, rate={rate:P0}, btnInteractable={canSocket && selectedCrystal != null}");

        // Update bar
        if (successRateBar)
        {
            successRateBar.fillAmount = rate;
            successRateBar.color = SocketingManager.Instance.GetSuccessRateColor(rate);
        }

        if (successRateText)
        {
            successRateText.text = $"Success Rate: {rate * 100f:F0}%";
        }

        // Socket button state
        if (socketButton)
        {
            socketButton.interactable = canSocket && selectedCrystal != null;
        }

        if (socketButtonText)
        {
            if (currentTab == ActiveTab.Weapon)
                socketButtonText.text = canSocket ? "SOCKET GEM" : "Select Gem + Crystal + Slot";
            else
                socketButtonText.text = canSocket ? "EQUIP ITEM" : "Select Equipment + Crystal";
        }
    }

    // ─── Rarity Color Helpers ────────────────────────────────────

    /// <summary>
    /// Background color sáng cho ô item theo rarity
    /// </summary>
    static Color GetRarityBgColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return new Color(1f, 1f, 1f, 1f);       // Trắng
            case Rarity.Uncommon:  return new Color(0f, 1f, 0f, 1f);       // Xanh lá 100%
            case Rarity.Rare:      return new Color(0.2f, 0.6f, 1f, 1f);   // Xanh dương 100%
            case Rarity.Epic:      return new Color(0.6f, 0.2f, 0.9f, 1f); // Tím 100%
            case Rarity.Legendary: return new Color(1f, 0.84f, 0f, 1f);    // Vàng 100%
            case Rarity.Mythic:    return new Color(1f, 0.27f, 0.27f, 1f); // Đỏ 100%
            default:               return new Color(0.7f, 0.7f, 0.7f, 1f);
        }
    }

    /// <summary>
    /// Border color đậm theo rarity
    /// </summary>
    static Color GetRarityBorderColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return new Color(0.4f, 0.4f, 0.4f, 1f);     // Xám đậm
            case Rarity.Uncommon:  return new Color(0f, 0.5f, 0f, 1f);         // Xanh lá đậm
            case Rarity.Rare:      return new Color(0.1f, 0.3f, 0.6f, 1f);     // Xanh dương đậm
            case Rarity.Epic:      return new Color(0.35f, 0.1f, 0.55f, 1f);   // Tím đậm
            case Rarity.Legendary: return new Color(0.6f, 0.45f, 0f, 1f);      // Vàng đậm
            case Rarity.Mythic:    return new Color(0.6f, 0.1f, 0.1f, 1f);     // Đỏ đậm
            default:               return new Color(0.3f, 0.3f, 0.3f, 1f);
        }
    }

    void SetDropSlotTooltip(Image slotIcon, Item item, string emptyHint = "")
    {
        if (slotIcon == null) return;
        GameObject container = slotIcon.transform.parent != null ? slotIcon.transform.parent.gameObject : slotIcon.gameObject;

        // Remove old EventTrigger if any
        var oldTrigger = container.GetComponent<EventTrigger>();
        if (oldTrigger != null) Destroy(oldTrigger);

        var trigger = container.AddComponent<EventTrigger>();

        if (item != null)
        {
            // Has item → show item info tooltip
            var rarity = item.rarity;
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => ShowBsTooltip(item, rarity));
            trigger.triggers.Add(enterEntry);
        }
        else if (!string.IsNullOrEmpty(emptyHint))
        {
            // Empty slot → show hint tooltip
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener((data) => ShowHintTooltip(emptyHint));
            trigger.triggers.Add(enterEntry);
        }

        // PointerExit → always hide
        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((data) => HideBsTooltip());
        trigger.triggers.Add(exitEntry);
    }

    void ShowHintTooltip(string hint)
    {
        if (bsTooltipPanel == null || bsTooltipText == null) return;

        bsTooltipText.text = $"<color=#FFCC00>{hint}</color>";
        bsTooltipPanel.SetActive(true);

        float pad = 45f;
        float maxW = 1050f;
        bsTooltipText.rectTransform.sizeDelta = new Vector2(maxW, 0);
        bsTooltipText.ForceMeshUpdate();
        Vector2 pref = bsTooltipText.GetPreferredValues(bsTooltipText.text, maxW, 0);
        float textW = Mathf.Clamp(pref.x, 450f, maxW);
        float textH = pref.y;

        bsTooltipText.rectTransform.anchoredPosition = new Vector2(pad, -pad);
        bsTooltipText.rectTransform.sizeDelta = new Vector2(textW, textH);
        bsTooltipRect.sizeDelta = new Vector2(textW + pad * 2, textH + pad * 2);
    }

    // ─── Self-Contained Blacksmith Tooltip ────────────────────────

    void CreateBlacksmithTooltip()
    {
        if (mainPanel == null) return;

        // Find the ROOT Canvas — parent tooltip here so it renders ON TOP of everything
        Canvas rootCanvas = mainPanel.GetComponentInParent<Canvas>();
        // Walk up to the topmost root canvas
        while (rootCanvas != null && rootCanvas.transform.parent != null)
        {
            var parentCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
            if (parentCanvas == null) break;
            rootCanvas = parentCanvas;
        }
        Transform tooltipParent = (rootCanvas != null) ? rootCanvas.transform : mainPanel.transform;

        // Create panel on the ROOT Canvas (not mainPanel) so it's always on top
        bsTooltipPanel = new GameObject("BS_Tooltip", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        bsTooltipPanel.transform.SetParent(tooltipParent, false);

        // Override sorting → HIGHEST possible order
        var tooltipCanvas = bsTooltipPanel.GetComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 32767;

        bsTooltipRect = bsTooltipPanel.GetComponent<RectTransform>();
        bsTooltipRect.pivot = new Vector2(0f, 1f);
        bsTooltipRect.sizeDelta = new Vector2(1050, 600); // default, will be resized (4K ×3)

        // Background
        var bg = bsTooltipPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        bg.raycastTarget = false;

        // Gold border
        var outline = bsTooltipPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.8f, 0.65f, 0.2f, 1f);
        outline.effectDistance = new Vector2(6, 6);

        // Text child — use SAME anchor as panel (top-left), manual position
        GameObject textGO = new GameObject("TooltipText", typeof(RectTransform));
        textGO.transform.SetParent(bsTooltipPanel.transform, false);
        bsTooltipText = textGO.AddComponent<TextMeshProUGUI>();
        bsTooltipText.fontSize = 45;
        bsTooltipText.color = Color.white;
        bsTooltipText.alignment = TextAlignmentOptions.TopLeft;
        bsTooltipText.enableWordWrapping = true;
        bsTooltipText.overflowMode = TMPro.TextOverflowModes.Overflow;
        bsTooltipText.raycastTarget = false;
        bsTooltipText.richText = true;

        // Anchor top-left, pivot top-left — position via anchoredPosition
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 1);
        textRT.anchorMax = new Vector2(0, 1);
        textRT.pivot = new Vector2(0, 1);

        bsTooltipPanel.SetActive(false);

        // Match font from inventory UI → consistent look
        ApplyInventoryFont();
    }

    /// <summary>
    /// Find the font used in inventory/UI and apply it to Blacksmith tooltip
    /// </summary>
    void ApplyInventoryFont()
    {
        if (bsTooltipText == null) return;

        // Try to get font from itemUIPrefab first (same prefab as player inventory)
        if (itemUIPrefab != null)
        {
            var prefabTMP = itemUIPrefab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (prefabTMP != null && prefabTMP.font != null)
            {
                bsTooltipText.font = prefabTMP.font;
                Debug.Log($"[BlacksmithUI] Tooltip font set from itemUIPrefab: {prefabTMP.font.name}");
                return;
            }
        }

        // Fallback: find any existing TMP text in the main panel
        if (mainPanel != null)
        {
            var anyTMP = mainPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (anyTMP != null && anyTMP != bsTooltipText && anyTMP.font != null)
            {
                bsTooltipText.font = anyTMP.font;
                Debug.Log($"[BlacksmithUI] Tooltip font set from panel: {anyTMP.font.name}");
                return;
            }
        }

        Debug.LogWarning("[BlacksmithUI] Could not find inventory font — using TMP default");
    }

    void AddTooltipTrigger(GameObject go, Item item, Rarity rarity, float rolledValue = -1f)
    {
        var trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        // PointerEnter
        var enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        Item capturedItem = item;
        Rarity capturedRarity = rarity;
        float capturedRoll = rolledValue;
        enterEntry.callback.AddListener((data) => ShowBsTooltip(capturedItem, capturedRarity, capturedRoll));
        trigger.triggers.Add(enterEntry);

        // PointerExit
        var exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => HideBsTooltip());
        trigger.triggers.Add(exitEntry);
    }

    void ShowBsTooltip(Item item, Rarity rarity, float rolledValue = -1f)
    {
        if (bsTooltipPanel == null || bsTooltipText == null || item == null) return;

        bsTooltipText.text = BuildTooltipContent(item, rarity, rolledValue);
        bsTooltipPanel.SetActive(true);

        float pad = 45f;
        float minW = 840f, maxW = 1950f;

        // First pass: get unconstrained preferred size
        bsTooltipText.rectTransform.sizeDelta = new Vector2(maxW, 0);
        bsTooltipText.ForceMeshUpdate();
        Vector2 pref = bsTooltipText.GetPreferredValues(bsTooltipText.text, maxW, 0);
        float textW = Mathf.Clamp(pref.x, minW, maxW);
        float textH = pref.y;

        // If width clamped, recalculate height
        if (textW < pref.x)
        {
            pref = bsTooltipText.GetPreferredValues(bsTooltipText.text, textW, 0);
            textH = pref.y;
        }

        // Set text rect: top-left anchor, offset by padding
        bsTooltipText.rectTransform.anchoredPosition = new Vector2(pad, -pad);
        bsTooltipText.rectTransform.sizeDelta = new Vector2(textW, textH);

        // Panel = text + padding on all sides
        bsTooltipRect.sizeDelta = new Vector2(textW + pad * 2, textH + pad * 2);
    }

    void HideBsTooltip()
    {
        if (bsTooltipPanel != null)
            bsTooltipPanel.SetActive(false);
    }

    void Update()
    {
        // Always apply grid config when UI is open (live tweaking in play mode)
        if (mainPanel != null && mainPanel.activeSelf)
            ApplyGridConfig();
        // Follow mouse when tooltip is showing, flip upward near screen bottom
        if (bsTooltipPanel != null && bsTooltipPanel.activeSelf && bsTooltipRect != null)
        {
            Vector2 localPos;
            var parentCanvas = mainPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
                float tooltipH = bsTooltipRect.sizeDelta.y;
                float mouseY = Input.mousePosition.y;

                // If tooltip would go below screen bottom → flip upward
                Vector3 offset;
                if (mouseY - tooltipH - 45f < 0f)
                {
                    // Show above cursor
                    offset = new Vector3(45, tooltipH + 45f, 0);
                }
                else
                {
                    // Show below cursor (default)
                    offset = new Vector3(45, -45, 0);
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    Input.mousePosition + offset,
                    cam, out localPos);
                bsTooltipRect.localPosition = localPos;
            }
        }
    }

    string BuildTooltipContent(Item item, Rarity rarity, float rolledValue = -1f)
    {
        var sb = new System.Text.StringBuilder();
        string rarityColor = Item.GetRarityColorHex(rarity);
        sb.AppendLine($"<color={rarityColor}><b>{item.itemName}</b></color>");
        sb.AppendLine($"<color=#888888>{rarity}</color>");
        sb.AppendLine();

        switch (item.itemType)
        {
            case ItemType.Equipment:
                sb.AppendLine($"<color=#FFD700>Slot: {item.equipmentSlot}</color>");
                // Show rolled multiplier if available
                if (rolledValue >= 0f)
                {
                    sb.AppendLine($"<color=#FF8800>⚡ Stat Roll: {rolledValue*100f:F0}%</color>");
                }
                float mult = (rolledValue >= 0f) ? rolledValue : 1f;
                if (item.ScaledHPBonus(rarity) > 0f)
                    sb.AppendLine($"<color=#00FF00>HP: +{item.ScaledHPBonus(rarity)*mult:F0}</color> <color=#888888>(max {item.ScaledHPBonus(rarity):F0})</color>");
                if (item.ScaledDefenseBonus(rarity) > 0f)
                    sb.AppendLine($"<color=#00AAFF>Defense: +{item.ScaledDefenseBonus(rarity)*mult:F0}</color> <color=#888888>(max {item.ScaledDefenseBonus(rarity):F0})</color>");
                if (item.ScaledCritRateBonus(rarity) > 0f)
                    sb.AppendLine($"<color=#FF00FF>Crit Rate: +{item.ScaledCritRateBonus(rarity)*mult*100:F1}%</color> <color=#888888>(max {item.ScaledCritRateBonus(rarity)*100:F1}%)</color>");
                if (item.ScaledCritDamageMultiplier(rarity) > 1f)
                    sb.AppendLine($"<color=#FF00FF>Crit Dmg: +{(item.ScaledCritDamageMultiplier(rarity)-1)*mult*100:F1}%</color> <color=#888888>(max {(item.ScaledCritDamageMultiplier(rarity)-1)*100:F1}%)</color>");
                if (item.ScaledMovementSpeedBonus(rarity) > 0f)
                    sb.AppendLine($"<color=#00FFFF>Move Speed: +{item.ScaledMovementSpeedBonus(rarity)*mult*100:F1}%</color> <color=#888888>(max {item.ScaledMovementSpeedBonus(rarity)*100:F1}%)</color>");
                if (item.ScaledAttackSpeedBonus(rarity) > 0f)
                    sb.AppendLine($"<color=#FFAA00>Atk Speed: +{item.ScaledAttackSpeedBonus(rarity)*mult*100:F1}%</color> <color=#888888>(max {item.ScaledAttackSpeedBonus(rarity)*100:F1}%)</color>");
                if (!string.IsNullOrEmpty(item.passiveDescription))
                {
                    sb.AppendLine();
                    sb.AppendLine($"<color=#FFD700>Passive:</color>");
                    sb.AppendLine($"<color=#FFFF00>{item.passiveDescription}</color>");
                }
                break;
            case ItemType.Gems:
                sb.AppendLine($"<color=#FFD700>Type: {item.gemType}</color>");
                // Show ROLLED value if available, otherwise show max
                if (rolledValue >= 0f)
                {
                    string gemStat = item.GetGemStatTextWithRoll(rolledValue);
                    if (!string.IsNullOrEmpty(gemStat)) sb.AppendLine($"<color=#00FF00>{gemStat}</color>");
                }
                else
                {
                    string gemStat = item.GetGemStatText();
                    if (!string.IsNullOrEmpty(gemStat)) sb.AppendLine($"<color=#00FF00>{gemStat} <color=#888888>(max)</color></color>");
                }
                break;
            case ItemType.CrystalStone:
                sb.AppendLine("<color=#00FFFF>Crystal Stone</color>");
                sb.AppendLine("<color=#888888>Socketing material - increases success rate</color>");
                sb.AppendLine();
                sb.AppendLine("<color=#FFD700>Success Rate:</color>");
                if (SocketingManager.Instance != null)
                {
                    string[] rarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
                    Color[] rarityColors = {
                        Color.white, new Color(0, 1, 0), new Color(0.2f, 0.6f, 1f),
                        new Color(0.6f, 0.2f, 0.9f), new Color(1, 0.84f, 0), new Color(1, 0.27f, 0.27f)
                    };
                    for (int ri = 1; ri <= 6; ri++)
                    {
                        float rate = SocketingManager.Instance.CalculateSuccessRate((Rarity)ri, rarity);
                        string rName = (ri - 1 < rarityNames.Length) ? rarityNames[ri - 1] : "???";
                        string rHex = ColorUtility.ToHtmlStringRGB(ri - 1 < rarityColors.Length ? rarityColors[ri - 1] : Color.white);
                        sb.AppendLine($"  <color=#{rHex}>{rName}</color>: <color=#FFFFFF>{rate * 100f:F0}%</color>");
                    }
                }
                break;
            case ItemType.Consumable:
                if (item.itemName != null && item.itemName.ToLower().Contains("health potion"))
                    sb.AppendLine("<color=#00FF00>+ 50% HP</color>");
                else
                    sb.AppendLine("<color=#FFD700>Consumable</color>");
                break;
            default:
                sb.AppendLine("<color=#FFD700>Material</color>");
                break;
        }
        return sb.ToString().TrimEnd();
    }

    // ─── Helper: EquipmentSlotType → index ────────────────────────
    int EquipmentSlotToIndex(EquipmentSlotType slot)
    {
        switch (slot)
        {
            case EquipmentSlotType.Head: return 0;
            case EquipmentSlotType.Body: return 1;
            case EquipmentSlotType.Legs: return 2;
            case EquipmentSlotType.Accessory: return 3;
            default: return 0;
        }
    }

    // ================================================================
    // REFINEMENT TAB LOGIC
    // ================================================================

    void SelectRefineEquipSlot(int index)
    {
        selectedRefineSlot = index;
        ClearSelectedRefineMaterial();
        RefreshRefinementDisplay();
        RefreshInventoryPanel();
    }

    public void SelectRefineMaterial(Item stone)
    {
        if (stone == null || stone.refinementTier <= 0) return;
        selectedRefineMaterial = stone;
        RefreshRefinementDisplay();
        RefreshInventoryPanel();
    }

    void ClearSelectedRefineMaterial()
    {
        selectedRefineMaterial = null;
        RefreshRefinementDisplay();
    }

    void RefreshRefinementDisplay()
    {
        if (currentTab != ActiveTab.Refinement) return;
        if (EquipmentManager.Instance == null) return;

        // Ensure RefinementManager exists
        if (RefinementManager.Instance == null)
        {
            var rmGO = new GameObject("RefinementManager");
            rmGO.AddComponent<RefinementManager>();
            Debug.Log("[BlacksmithUI] Auto-created RefinementManager");
        }

        // Update 4 equipment slot icons
        string[] slotNames = { "Head", "Body", "Legs", "Acc" };
        for (int i = 0; i < 4; i++)
        {
            var item = EquipmentManager.Instance.GetEquippedItemByIndex(i);
            if (i < refineEquipSlotIcons.Length && refineEquipSlotIcons[i] != null)
            {
                if (item != null)
                {
                    refineEquipSlotIcons[i].sprite = item.icon;
                    refineEquipSlotIcons[i].color = Color.white;
                }
                else
                {
                    refineEquipSlotIcons[i].sprite = null;
                    refineEquipSlotIcons[i].color = new Color(0.18f, 0.18f, 0.25f, 0.8f);
                }
            }
            // Highlight selected slot
            if (i < refineEquipSlotButtons.Length && refineEquipSlotButtons[i] != null)
            {
                var outline = refineEquipSlotButtons[i].GetComponent<Outline>();
                if (i == selectedRefineSlot && item != null)
                {
                    if (outline == null) outline = refineEquipSlotButtons[i].gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.84f, 0f);
                    outline.effectDistance = new Vector2(4, 4);
                    outline.enabled = true;
                }
                else if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }

        // Equipment name + level
        if (selectedRefineSlot >= 0 && selectedRefineSlot < 4)
        {
            var equip = EquipmentManager.Instance.GetEquippedItemByIndex(selectedRefineSlot);
            if (equip != null)
            {
                Rarity r = EquipmentManager.Instance.GetEquippedRarity(selectedRefineSlot);
                int level = EquipmentManager.Instance.GetEnhancementLevel(selectedRefineSlot);
                string rarityHex = Item.GetRarityColorHex(r);
                string levelStr = level > 0 ? $" +{level}" : "";
                if (refineEquipNameText)
                    refineEquipNameText.text = $"<color={rarityHex}>{equip.itemName} [{r}]{levelStr}</color>";

                // Level display with star images
                UpdateStarImages(level);
                if (refineLevelText)
                {
                    if (level >= EquipmentManager.MAX_ENHANCEMENT_LEVEL)
                        refineLevelText.text = $"<color=#FFD700>+{level} MAX</color>";
                    else
                        refineLevelText.text = $"<color=#FFD700>+{level}</color> >> <color=#00FF00>+{level + 1}</color>";
                }

                // Stats preview (before → after)
                RefreshRefineStatsPreview(equip, r, selectedRefineSlot, level);

                // Update material slot display
                if (selectedRefineMaterial != null)
                {
                    if (refineMaterialIcon) { refineMaterialIcon.sprite = selectedRefineMaterial.icon; refineMaterialIcon.enabled = true; refineMaterialIcon.color = Color.white; }
                    string stoneHex = Item.GetRarityColorHex(selectedRefineMaterial.rarity);
                    if (refineMaterialText) refineMaterialText.text = $"<color={stoneHex}>{selectedRefineMaterial.itemName}</color>";

                    // Success rate
                    float rate = RefinementManager.Instance.CalculateRefineRate(level, selectedRefineMaterial.refinementTier);
                    if (refineSuccessBar) { refineSuccessBar.fillAmount = rate; refineSuccessBar.color = RefinementManager.Instance.GetRefineRateColor(rate); }
                    if (refineSuccessText) refineSuccessText.text = $"Success Rate: {rate * 100f:F0}%";
                }
                else
                {
                    if (refineMaterialIcon) { refineMaterialIcon.enabled = false; }
                    if (refineMaterialText) refineMaterialText.text = "Select Refinement Stone";
                    if (refineSuccessBar) refineSuccessBar.fillAmount = 0f;
                    if (refineSuccessText) refineSuccessText.text = "Success Rate: 0%";
                }

                // Enable/disable refine button
                bool canRefine = selectedRefineMaterial != null && level < EquipmentManager.MAX_ENHANCEMENT_LEVEL && !isRefining;
                if (refineButton) refineButton.interactable = canRefine;
            }
            else
            {
                ClearRefineDisplay();
            }
        }
        else
        {
            ClearRefineDisplay();
        }

        // Fusion display
        RefreshFusionDisplay();
    }

    void ClearRefineDisplay()
    {
        if (refineEquipNameText) refineEquipNameText.text = "Select equipment to refine";
        if (refineLevelText) refineLevelText.text = "";
        if (refineStatsText) refineStatsText.text = "";
        if (refineMaterialIcon) refineMaterialIcon.enabled = false;
        if (refineMaterialText) refineMaterialText.text = "Select Refinement Stone";
        if (refineSuccessBar) refineSuccessBar.fillAmount = 0f;
        if (refineSuccessText) refineSuccessText.text = "Success Rate: 0%";
        if (refineButton) refineButton.interactable = false;
        UpdateStarImages(-1); // all gray
    }

    void UpdateStarImages(int level)
    {
        if (refineStarImages == null) return;
        Color gold = new Color(1f, 0.84f, 0f);
        Color gray = new Color(0.3f, 0.3f, 0.35f);
        for (int i = 0; i < refineStarImages.Length; i++)
        {
            if (refineStarImages[i] != null)
                refineStarImages[i].color = (i < level) ? gold : gray;
        }
    }

    void RefreshRefineStatsPreview(Item equip, Rarity r, int slot, int level)
    {
        if (refineStatsText == null) return;

        float roll = EquipmentManager.Instance.GetEquipStatRoll(slot);
        float currentMul = 1f + level * 0.03f;
        float nextMul = 1f + (level + 1) * 0.03f;
        float rmul = Item.GetRarityMultiplier(r);

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("<mspace=0.55em><color=#AAAAAA>Stat        Current   After Refine</color>");

        void AddStatLine(string name, float baseVal)
        {
            if (baseVal <= 0) return;
            float current = baseVal * rmul * roll * currentMul;
            float next = baseVal * rmul * roll * nextMul;
            sb.AppendLine($"{name,-10} <color=#FFFFFF>{current,7:F1}</color>  >>  <color=#00FF00>{next,7:F1}</color>  <color=#FFD700>(+3%)</color>");
        }

        AddStatLine("HP", equip.hpBonus);
        AddStatLine("Defense", equip.defenseBonus);
        if (equip.critRateBonus > 0)
        {
            float curCR = equip.critRateBonus * rmul * roll * currentMul * 100f;
            float nextCR = equip.critRateBonus * rmul * roll * nextMul * 100f;
            sb.AppendLine($"{"Crit Rate",-10} <color=#FFFFFF>{curCR,6:F1}%</color>  >>  <color=#00FF00>{nextCR,6:F1}%</color>  <color=#FFD700>(+3%)</color>");
        }
        if (equip.critDamageMultiplier > 1f)
        {
            float cdBase = equip.critDamageMultiplier - 1f;
            float curCD = (1f + cdBase * rmul * roll * currentMul) * 100f;
            float nextCD = (1f + cdBase * rmul * roll * nextMul) * 100f;
            sb.AppendLine($"{"Crit Dmg",-10} <color=#FFFFFF>{curCD,6:F0}%</color>  >>  <color=#00FF00>{nextCD,6:F0}%</color>  <color=#FFD700>(+3%)</color>");
        }
        if (equip.movementSpeedBonus > 0)
        {
            float curMS = equip.movementSpeedBonus * rmul * roll * currentMul * 100f;
            float nextMS = equip.movementSpeedBonus * rmul * roll * nextMul * 100f;
            sb.AppendLine($"{"Move Spd",-10} <color=#FFFFFF>{curMS,6:F1}%</color>  >>  <color=#00FF00>{nextMS,6:F1}%</color>  <color=#FFD700>(+3%)</color>");
        }
        if (equip.attackSpeedBonus > 0)
        {
            float curAS = equip.attackSpeedBonus * rmul * roll * currentMul * 100f;
            float nextAS = equip.attackSpeedBonus * rmul * roll * nextMul * 100f;
            sb.AppendLine($"{"Atk Spd",-10} <color=#FFFFFF>{curAS,6:F1}%</color>  >>  <color=#00FF00>{nextAS,6:F1}%</color>  <color=#FFD700>(+3%)</color>");
        }

        if (sb.Length < 80)
            sb.AppendLine("<color=#888888>No stats to display</color>");

        sb.Append("</mspace>");
        refineStatsText.text = sb.ToString().TrimEnd();
    }

    // ─── Refinement Execution ─────────────────────────────────────

    void OnRefineButtonClicked()
    {
        if (isRefining) return;
        if (selectedRefineSlot < 0 || selectedRefineMaterial == null) return;
        if (EquipmentManager.Instance == null || RefinementManager.Instance == null) return;

        var equip = EquipmentManager.Instance.GetEquippedItemByIndex(selectedRefineSlot);
        if (equip == null) return;

        int level = EquipmentManager.Instance.GetEnhancementLevel(selectedRefineSlot);
        if (level >= EquipmentManager.MAX_ENHANCEMENT_LEVEL) return;

        StartCoroutine(RefineAnimationCoroutine());
    }

    IEnumerator RefineAnimationCoroutine()
    {
        isRefining = true;
        if (refineButton) refineButton.interactable = false;

        // Play forge sound
        SoundManager.PlaySound(SoundType.Blacksmith_Forge);

        // Cache transforms
        RectTransform btnRT = refineButton?.GetComponent<RectTransform>();
        RectTransform gearRT = refineGearImage;
        RectTransform stoneRT = refineMaterialIcon?.GetComponent<RectTransform>();
        RectTransform barRT = refineSuccessBar?.GetComponent<RectTransform>();
        Vector2 stoneOrigPos = stoneRT != null ? stoneRT.anchoredPosition : Vector2.zero;
        Quaternion btnOrigRot = btnRT != null ? btnRT.localRotation : Quaternion.identity;
        Quaternion gearOrigRot = gearRT != null ? gearRT.localRotation : Quaternion.identity;
        float barOrigFill = refineSuccessBar != null ? refineSuccessBar.fillAmount : 0f;
        Color barOrigColor = refineSuccessBar != null ? refineSuccessBar.color : Color.green;

        // ── PHASE 1: Dramatic refine animation (~2s) ──
        float animDuration = 2.0f;
        float timer = 0f;
        float spinDirection = 1f;
        float spinSpeed = 180f;       // degrees/sec
        float dirChangeTimer = 0f;
        float nextDirChange = 0.3f;   // first direction change at 0.3s

        while (timer < animDuration)
        {
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.005f);
            timer += dt;
            float progress = timer / animDuration; // 0→1

            // ── Button spin: rotate with increasing speed, alternating direction ──
            if (btnRT != null)
            {
                dirChangeTimer += dt;
                if (dirChangeTimer >= nextDirChange)
                {
                    spinDirection *= -1f;
                    dirChangeTimer = 0f;
                    nextDirChange = Mathf.Lerp(0.3f, 0.08f, progress); // faster changes
                }
                float currentSpeed = spinSpeed * (1f + progress * 2f); // accelerate
                btnRT.Rotate(0, 0, currentSpeed * dt * spinDirection);
                // Small gear: counter-rotate at 1.5x speed (meshing effect)
                if (gearRT != null)
                    gearRT.Rotate(0, 0, -currentSpeed * 1.5f * dt * spinDirection);
            }

            // ── Bar fill: bounce randomly with pulsing color ──
            if (refineSuccessBar != null)
            {
                float bounce = Mathf.Abs(Mathf.Sin(timer * 8f + Mathf.Sin(timer * 3f) * 2f));
                refineSuccessBar.fillAmount = bounce;
                // Color shifts: green → yellow → red → green
                float hue = Mathf.Repeat(timer * 0.8f, 1f);
                refineSuccessBar.color = Color.HSVToRGB(hue, 0.8f, 0.9f);
            }

            // ── Stone shake: intensifying ──
            if (stoneRT != null)
            {
                float intensity = Mathf.Lerp(3f, 15f, progress);
                float shakeX = Mathf.Sin(timer * 45f) * intensity;
                float shakeY = Mathf.Cos(timer * 37f) * intensity * 0.5f;
                stoneRT.anchoredPosition = stoneOrigPos + new Vector2(shakeX, shakeY);
            }

            // ── Star images pulse ──
            if (refineStarImages != null)
            {
                for (int i = 0; i < refineStarImages.Length; i++)
                {
                    if (refineStarImages[i] != null)
                    {
                        float pulse = Mathf.Sin(timer * 12f + i * 0.9f) * 0.5f + 0.5f;
                        float starHue = Mathf.Repeat(timer * 0.5f + i * 0.1f, 1f);
                        refineStarImages[i].color = Color.HSVToRGB(starHue, 0.6f, 0.5f + pulse * 0.5f);
                    }
                }
            }

            yield return null;
        }

        // ── PHASE 2: Quick freeze (dramatic pause) ──
        if (btnRT != null) btnRT.localRotation = btnOrigRot;
        if (gearRT != null) gearRT.localRotation = gearOrigRot;
        if (stoneRT != null) stoneRT.anchoredPosition = stoneOrigPos;
        if (refineSuccessBar != null)
        {
            refineSuccessBar.fillAmount = barOrigFill;
            refineSuccessBar.color = barOrigColor;
        }

        // Flash everything white for a moment
        yield return new WaitForSecondsRealtime(0.15f);

        // Execute refinement
        RefinementResult result = RefinementManager.Instance.TryRefine(selectedRefineSlot, selectedRefineMaterial);

        // Get equip icon for animation
        RectTransform equipRT = (selectedRefineSlot >= 0 && selectedRefineSlot < refineEquipSlotIcons.Length)
            ? refineEquipSlotIcons[selectedRefineSlot]?.GetComponent<RectTransform>() : null;
        Image equipImg = (selectedRefineSlot >= 0 && selectedRefineSlot < refineEquipSlotIcons.Length)
            ? refineEquipSlotIcons[selectedRefineSlot] : null;

        if (result == RefinementResult.Success)
        {
            // Success animation: flash white + scale bounce
            SoundManager.PlaySound(SoundType.Blacksmith_Refine_Success);

            // Flash white
            if (equipImg != null)
            {
                Color origColor = equipImg.color;
                equipImg.color = Color.white;
                yield return new WaitForSecondsRealtime(0.15f);
                equipImg.color = origColor;
            }

            // Scale bounce
            if (equipRT != null)
            {
                Vector3 origScale = equipRT.localScale;
                float bounceTimer2 = 0f;
                float bounceDuration = 0.3f;
                while (bounceTimer2 < bounceDuration)
                {
                    bounceTimer2 += Mathf.Max(Time.unscaledDeltaTime, 0.01f);
                    float t = bounceTimer2 / bounceDuration;
                    float scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI) * (1f - t);
                    equipRT.localScale = origScale * scale;
                    yield return null;
                }
                equipRT.localScale = origScale;
            }

            int newLevel = EquipmentManager.Instance.GetEnhancementLevel(selectedRefineSlot);
            var equipItem = EquipmentManager.Instance.GetEquippedItemByIndex(selectedRefineSlot);
            string eName = equipItem != null ? equipItem.itemName : "Equipment";

            // Show result
            if (resultPanel && resultText)
            {
                resultPanel.SetActive(true);
                ConfigureResultText();
                resultText.text = $"REFINE SUCCESS!\n{eName} +{newLevel - 1} >> +{newLevel}";
                resultText.color = new Color(0.2f, 0.9f, 0.3f);
                StopCoroutine(nameof(HideResultCoroutine));
                StartCoroutine(HideResultCoroutine());
            }

            // Level text glow animation (3 blinks)
            if (refineLevelText != null)
            {
                Color origLevelColor = refineLevelText.color;
                for (int i = 0; i < 3; i++)
                {
                    refineLevelText.color = new Color(1f, 1f, 0.5f);
                    yield return new WaitForSecondsRealtime(0.12f);
                    refineLevelText.color = new Color(1f, 0.84f, 0f);
                    yield return new WaitForSecondsRealtime(0.12f);
                }
                refineLevelText.color = origLevelColor;
            }
        }
        else if (result == RefinementResult.Fail)
        {
            // Fail animation: shake + red tint
            SoundManager.PlaySound(SoundType.Blacksmith_Refine_Fail);

            // Shake equipment icon
            if (equipRT != null)
            {
                Vector2 origEqPos = equipRT.anchoredPosition;
                float shakeT = 0f;
                while (shakeT < 0.3f)
                {
                    shakeT += Mathf.Max(Time.unscaledDeltaTime, 0.01f);
                    float ox = Mathf.Sin(shakeT * 50f) * 10f * (1f - shakeT / 0.3f);
                    equipRT.anchoredPosition = origEqPos + new Vector2(ox, 0);
                    yield return null;
                }
                equipRT.anchoredPosition = origEqPos;
            }

            // Red tint
            if (equipImg != null)
            {
                Color origC = equipImg.color;
                equipImg.color = new Color(1f, 0.3f, 0.3f);
                yield return new WaitForSecondsRealtime(0.3f);
                equipImg.color = origC;
            }

            string stoneName = selectedRefineMaterial != null ? selectedRefineMaterial.itemName : "Stone";
            if (resultPanel && resultText)
            {
                resultPanel.SetActive(true);
                ConfigureResultText();
                resultText.text = $"REFINE FAILED!\nLost {stoneName}";
                resultText.color = new Color(0.9f, 0.2f, 0.2f);
                StopCoroutine(nameof(HideResultCoroutine));
                StartCoroutine(HideResultCoroutine());
            }
        }

        // Clear material (consumed) and refresh
        if (result == RefinementResult.Success || result == RefinementResult.Fail)
        {
            // Check if we still have this material
            if (selectedRefineMaterial != null && InventoryManager.Instance != null
                && InventoryManager.Instance.GetItemAmount(selectedRefineMaterial.id) <= 0)
            {
                selectedRefineMaterial = null;
            }
        }

        isRefining = false;
        RefreshAll();
    }

    // ─── Fusion ───────────────────────────────────────────────────

    void RefreshFusionDisplay()
    {
        if (currentTab != ActiveTab.Refinement) return;
        if (RefinementManager.Instance == null) return;

        if (selectedRefineMaterial != null && selectedRefineMaterial.refinementTier > 0 && selectedRefineMaterial.refinementTier < 7)
        {
            // Show fusion info
            if (fusionSourceIcon) { fusionSourceIcon.sprite = selectedRefineMaterial.icon; fusionSourceIcon.enabled = true; fusionSourceIcon.color = Color.white; }

            Item resultStone = RefinementManager.Instance.GetFusionResultStone(selectedRefineMaterial);
            if (resultStone != null)
            {
                if (fusionResultIcon) { fusionResultIcon.sprite = resultStone.icon; fusionResultIcon.enabled = true; fusionResultIcon.color = Color.white; }
                int have = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemAmount(selectedRefineMaterial.id) : 0;
                if (fusionInfoText) fusionInfoText.text = $"4x ({have}) >> 1x {resultStone.itemName}";
                if (fusionButton) fusionButton.interactable = RefinementManager.Instance.CanFuse(selectedRefineMaterial);
            }
            else
            {
                if (fusionResultIcon) fusionResultIcon.enabled = false;
                if (fusionInfoText) fusionInfoText.text = "";
                if (fusionButton) fusionButton.interactable = false;
            }
        }
        else
        {
            // Clear fusion display
            if (fusionSourceIcon) fusionSourceIcon.enabled = false;
            if (fusionResultIcon) fusionResultIcon.enabled = false;
            if (fusionInfoText) fusionInfoText.text = "Select stone to fuse";
            if (fusionButton) fusionButton.interactable = false;
        }
    }

    void OnFusionButtonClicked()
    {
        if (selectedRefineMaterial == null || RefinementManager.Instance == null) return;

        bool success = RefinementManager.Instance.TryFuse(selectedRefineMaterial);
        if (success)
        {
            // Check if still have source stones
            if (InventoryManager.Instance != null && InventoryManager.Instance.GetItemAmount(selectedRefineMaterial.id) <= 0)
                selectedRefineMaterial = null;

            if (resultPanel && resultText)
            {
                resultPanel.SetActive(true);
                ConfigureResultText();
                resultText.text = "FUSION SUCCESS!";
                resultText.color = new Color(0.5f, 0.9f, 0.3f);
                StopCoroutine(nameof(HideResultCoroutine));
                StartCoroutine(HideResultCoroutine());
            }

            SoundManager.PlaySound(SoundType.Blacksmith_Socket_Success);
        }

        RefreshAll();
    }
}
