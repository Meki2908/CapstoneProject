using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Main controller for the NPC Blacksmith GUI.
/// 2 tabs: Weapon Socketing (3 slots) and Equipment Socketing (4 slots).
/// Handles gem/crystal selection, success rate display, and socketing execution.
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

    [Header("Blacksmith Tooltip (self-contained)")]
    private GameObject bsTooltipPanel;
    private TextMeshProUGUI bsTooltipText;
    private RectTransform bsTooltipRect;



    // ─── Runtime State ───────────────────────────────────────────
    private enum ActiveTab { Weapon, Equipment }
    private ActiveTab currentTab = ActiveTab.Weapon;

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
        // Create self-contained tooltip (previously done in SetupReferences)
        CreateBlacksmithTooltip();

        // Button listeners
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (weaponTabButton) weaponTabButton.onClick.AddListener(() => SwitchTab(ActiveTab.Weapon));
        if (equipmentTabButton) equipmentTabButton.onClick.AddListener(() => SwitchTab(ActiveTab.Equipment));
        if (socketButton) socketButton.onClick.AddListener(OnSocketButtonClicked);

        // Crystal clear buttons
        if (weaponCrystalClearButton) weaponCrystalClearButton.onClick.AddListener(ClearSelectedCrystal);
        if (equipmentCrystalClearButton) equipmentCrystalClearButton.onClick.AddListener(ClearSelectedCrystal);

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

        // Wire remove buttons for weapon gem slots
        if (weaponGemRemoveButtons != null)
        {
            for (int i = 0; i < weaponGemRemoveButtons.Length; i++)
            {
                int slotIdx = i;
                if (weaponGemRemoveButtons[i] != null)
                    weaponGemRemoveButtons[i].onClick.AddListener(() => OnRemoveGemSlot(slotIdx, true));
            }
        }
        // Wire remove buttons for equipment gem slots
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

        // Tab button colors
        UpdateTabButtonColor(weaponTabButton, tab == ActiveTab.Weapon);
        UpdateTabButtonColor(equipmentTabButton, tab == ActiveTab.Equipment);

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

    void SelectEquipmentSlot(int index)
    {
        selectedEquipmentSlot = index;
        ClearGemAndCrystalSelection();
        RefreshEquipmentDisplay();
        RefreshGemSlots();
        RefreshViewports();
        UpdateSuccessRate();
    }

    // ─── Gem Slot Interaction ────────────────────────────────────

    void OnGemSlotClicked(int slotIndex)
    {
        // If we have a selected gem → place it in this slot (tracked for socketing)
        selectedGemSlotIndex = slotIndex;
        UpdateSuccessRate();
    }

    void OnGemSlotDoubleClicked(int slotIndex)
    {
        // Double-click on gem slot → trigger removal with confirmation
        bool isWeapon = (currentTab == ActiveTab.Weapon);
        OnRemoveGemSlot(slotIndex, isWeapon);
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
        resultText.text = $"⚠ Removing <color={Item.GetRarityColorHex(gem.rarity)}>{gem.itemName}</color> will DESTROY it permanently!\nClick again to confirm.";
        resultText.color = new Color(1f, 0.7f, 0.2f); // Warning orange
    }

    void ShowRemovalSuccess(Item gem)
    {
        if (resultPanel == null || resultText == null) return;
        resultPanel.SetActive(true);
        resultText.text = $"Removed {gem.itemName}. Gem has been destroyed.";
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
        if (SocketingManager.Instance == null) return;

        // Validate based on tab
        if (currentTab == ActiveTab.Weapon)
        {
            if (selectedGem == null || selectedCrystal == null || selectedGemSlotIndex < 0) return;
        }
        else
        {
            if (selectedEquipment == null || selectedCrystal == null) return;
        }

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

        switch (result)
        {
            case SocketResult.Success:
                resultText.text = "SOCKETING SUCCESS!";
                resultText.color = new Color(0.2f, 0.9f, 0.3f);
                break;
            case SocketResult.Fail:
                if (currentTab == ActiveTab.Weapon)
                    resultText.text = "SOCKETING FAILED!\nCrystal Stone and Gem destroyed.";
                else
                    resultText.text = "SOCKETING FAILED!\nCrystal Stone consumed. Equipment returned.";
                resultText.color = new Color(0.9f, 0.3f, 0.2f);
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

        // Auto-hide after 2 seconds (unscaled time — works even when timeScale=0)
        StopCoroutine(nameof(HideResultCoroutine));
        StartCoroutine(HideResultCoroutine());
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
            if (weaponGemSlots == null) return;
            var wc = FindFirstObjectByType<WeaponController>();
            WeaponType wt = (wc != null) ? wc.GetCurrentWeapon().weaponType : WeaponType.None;

            for (int i = 0; i < weaponGemSlots.Length && i < 3; i++)
            {
                Item gem = null;
                if (wt != WeaponType.None && WeaponGemManager.Instance != null)
                    gem = WeaponGemManager.Instance.GetEquippedGem(wt, i);

                weaponGemSlots[i].SetGem(gem, i == selectedGemSlotIndex);
            }
        }
        // Equipment tab: no gem slots (equipment uses equipment items, not gems)
    }

    void RefreshViewports()
    {
        RefreshInventoryPanel();
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
                if (item.itemType != ItemType.Equipment && item.itemType != ItemType.CrystalStone)
                    continue;
            }

            GameObject go = Instantiate(itemUIPrefab, inventoryContent);
            var itemUI = go.GetComponent<ItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(item, amount, null, rarity);
                itemUI.SetRemoveButtonVisible(false);
            }

            // ── Rarity background color + border ──
            var bgImage = go.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = GetRarityBgColor(rarity);
            }
            var rarityOutline = go.AddComponent<Outline>();
            rarityOutline.effectColor = GetRarityBorderColor(rarity);
            rarityOutline.effectDistance = new Vector2(6, 6);

            // ── Resize to fit Blacksmith grid cells (180×200 for 4K) ──
            var iconTf = go.transform.Find("Item Icon");
            if (iconTf != null)
            {
                var iconRT = iconTf.GetComponent<RectTransform>();
                if (iconRT != null) iconRT.sizeDelta = new Vector2(130, 130);
            }
            var nameText = go.transform.Find("Item name");
            if (nameText != null)
            {
                var tmp = nameText.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 33;
                    tmp.enableWordWrapping = true;
                    tmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                    tmp.maxVisibleLines = 2;
                }
            }
            var amountText = go.transform.Find("Item amount");
            if (amountText != null)
            {
                var tmp = amountText.GetComponent<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = 26;
                    tmp.enableWordWrapping = false;
                    tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
                }
            }

            // ── Click behavior — use existing Button from prefab ──
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
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

        // Create panel on the Blacksmith Canvas
        bsTooltipPanel = new GameObject("BS_Tooltip", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        bsTooltipPanel.transform.SetParent(mainPanel.transform, false);

        // Override sorting → always on top
        var tooltipCanvas = bsTooltipPanel.GetComponent<Canvas>();
        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 10000;

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
                if (item.ScaledHPBonus(rarity) > 0f) sb.AppendLine($"<color=#00FF00>HP: +{item.ScaledHPBonus(rarity)*mult:F0}</color>");
                if (item.ScaledDefenseBonus(rarity) > 0f) sb.AppendLine($"<color=#00AAFF>Defense: +{item.ScaledDefenseBonus(rarity)*mult:F0}</color>");
                if (item.ScaledCritRateBonus(rarity) > 0f) sb.AppendLine($"<color=#FF00FF>Crit Rate: +{item.ScaledCritRateBonus(rarity)*mult*100:F1}%</color>");
                if (item.ScaledCritDamageMultiplier(rarity) > 1f) sb.AppendLine($"<color=#FF00FF>Crit Dmg: +{(item.ScaledCritDamageMultiplier(rarity)-1)*mult*100:F1}%</color>");
                if (item.ScaledMovementSpeedBonus(rarity) > 0f) sb.AppendLine($"<color=#00FFFF>Move Speed: +{item.ScaledMovementSpeedBonus(rarity)*mult*100:F1}%</color>");
                if (item.ScaledAttackSpeedBonus(rarity) > 0f) sb.AppendLine($"<color=#FFAA00>Atk Speed: +{item.ScaledAttackSpeedBonus(rarity)*mult*100:F1}%</color>");
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
}
