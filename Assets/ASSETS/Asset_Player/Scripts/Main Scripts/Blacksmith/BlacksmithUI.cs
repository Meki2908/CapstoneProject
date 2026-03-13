using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private Image weaponCrystalSlotIcon;
    [SerializeField] private TextMeshProUGUI weaponCrystalSlotText;
    [SerializeField] private Button weaponCrystalClearButton;

    [Header("Equipment Tab")]
    [SerializeField] private Button[] equipmentSlotButtons = new Button[4]; // Head, Body, Legs, Accessory
    [SerializeField] private Image[] equipmentSlotIcons = new Image[4];
    [SerializeField] private TextMeshProUGUI equipmentNameText;
    [SerializeField] private Transform equipmentGemSlotsParent; // Contains 4 SocketingSlotUI
    [SerializeField] private Image equipmentCrystalSlotIcon;
    [SerializeField] private TextMeshProUGUI equipmentCrystalSlotText;
    [SerializeField] private Button equipmentCrystalClearButton;

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

    // ─── Runtime State ───────────────────────────────────────────
    private enum ActiveTab { Weapon, Equipment }
    private ActiveTab currentTab = ActiveTab.Weapon;

    private int selectedEquipmentSlot = -1; // 0-3 for equipment tab
    private Item selectedGem = null;
    private int selectedGemSlotIndex = -1;
    private Item selectedCrystal = null;

    // Cached gem slot UIs
    private SocketingSlotUI[] weaponGemSlots;
    private SocketingSlotUI[] equipmentGemSlots;

    // ─── Lifecycle ───────────────────────────────────────────────

    void Start()
    {
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
        InitializeGemSlots(equipmentGemSlots, 4);

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
        if (mainPanel) mainPanel.SetActive(false);
        ClearSelection();

        // Notify NPC to close
        var npc = FindFirstObjectByType<BlacksmithNPC>();
        if (npc != null) npc.CloseBlacksmith();
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
        // Remove gem from slot
        if (currentTab == ActiveTab.Weapon)
        {
            var wc = FindFirstObjectByType<WeaponController>();
            if (wc != null && WeaponGemManager.Instance != null)
            {
                WeaponGemManager.Instance.RemoveGem(wc.GetCurrentWeapon().weaponType, slotIndex);
            }
        }
        else if (currentTab == ActiveTab.Equipment && selectedEquipmentSlot >= 0)
        {
            EquipmentManager.Instance?.RemoveGemFromSlot(selectedEquipmentSlot, slotIndex);
        }
        RefreshAll();
    }

    // ─── Gem / Crystal Selection from Viewports ──────────────────

    public void SelectGem(Item gem)
    {
        selectedGem = gem;
        UpdateSuccessRate();
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
    }

    void ClearSelection()
    {
        ClearGemAndCrystalSelection();
        selectedEquipmentSlot = -1;
    }

    // ─── Socket Button ───────────────────────────────────────────

    void OnSocketButtonClicked()
    {
        if (SocketingManager.Instance == null) return;
        if (selectedGem == null || selectedCrystal == null || selectedGemSlotIndex < 0) return;

        SocketResult result;

        if (currentTab == ActiveTab.Weapon)
        {
            var wc = FindFirstObjectByType<WeaponController>();
            if (wc == null) return;
            result = SocketingManager.Instance.TrySocketWeapon(
                wc.GetCurrentWeapon().weaponType, selectedGemSlotIndex, selectedGem, selectedCrystal);
        }
        else
        {
            if (selectedEquipmentSlot < 0) return;
            result = SocketingManager.Instance.TrySocketEquipment(
                selectedEquipmentSlot, selectedGemSlotIndex, selectedGem, selectedCrystal);
        }

        ShowResult(result);

        // Clear crystal selection (consumed)
        if (result == SocketResult.Success || result == SocketResult.Fail)
        {
            selectedCrystal = null;
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
                resultText.text = "KHẢM THÀNH CÔNG!";
                resultText.color = new Color(0.2f, 0.9f, 0.3f);
                break;
            case SocketResult.Fail:
                resultText.text = "KHẢM THẤT BẠI!\nMất Crystal Stone, Gem được trả lại.";
                resultText.color = new Color(0.9f, 0.3f, 0.2f);
                break;
            case SocketResult.NoGem:
                resultText.text = "Chưa chọn Gem!";
                resultText.color = Color.yellow;
                break;
            case SocketResult.NoCrystal:
                resultText.text = "Chưa chọn Crystal Stone!";
                resultText.color = Color.yellow;
                break;
            case SocketResult.NoTarget:
                resultText.text = "Chưa có trang bị/vũ khí!";
                resultText.color = Color.yellow;
                break;
            default:
                resultText.text = "Lỗi khảm.";
                resultText.color = Color.gray;
                break;
        }

        // Auto-hide after 2 seconds (unscaled time since game is paused)
        CancelInvoke(nameof(HideResult));
        Invoke(nameof(HideResult), 2f);
    }

    void HideResult()
    {
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
            if (weaponNameText) weaponNameText.text = "Không có vũ khí";
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
                    equipmentNameText.text = $"Slot {slotNames[selectedEquipmentSlot]} — Trống";
                }
            }
            else
            {
                equipmentNameText.text = "Chọn slot trang bị";
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
        else if (currentTab == ActiveTab.Equipment)
        {
            if (equipmentGemSlots == null || selectedEquipmentSlot < 0) return;

            for (int i = 0; i < equipmentGemSlots.Length && i < 4; i++)
            {
                Item gem = EquipmentManager.Instance?.GetEquippedGem(selectedEquipmentSlot, i);
                equipmentGemSlots[i].SetGem(gem, i == selectedGemSlotIndex);
            }
        }
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

        // Populate with ALL items from inventory
        var allItems = InventoryManager.Instance.GetAllItemsWithRarity();
        foreach (var (item, amount, rarity) in allItems)
        {
            if (item == null || amount <= 0) continue;

            GameObject go = Instantiate(itemUIPrefab, inventoryContent);
            var itemUI = go.GetComponent<ItemUI>();
            if (itemUI != null)
            {
                itemUI.Initialize(item, amount, null, rarity);
            }

            // Add click behavior based on item type
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            Item capturedItem = item;

            if (item.itemType == ItemType.Gems)
            {
                // Click gem → select for socketing
                btn.onClick.AddListener(() => SelectGem(capturedItem));

                // Highlight if selected
                if (selectedGem != null && selectedGem.id == item.id)
                {
                    var outline = go.AddComponent<Outline>();
                    outline.effectColor = new Color(1f, 0.84f, 0f); // Gold
                    outline.effectDistance = new Vector2(3, 3);
                }
            }
            else if (item.itemType == ItemType.CrystalStone)
            {
                // Click crystal → select as crystal stone
                btn.onClick.AddListener(() => SelectCrystal(capturedItem));

                // Highlight if selected
                if (selectedCrystal != null && selectedCrystal.id == item.id)
                {
                    var outline = go.AddComponent<Outline>();
                    outline.effectColor = new Color(0.6f, 0.2f, 0.9f); // Purple
                    outline.effectDistance = new Vector2(3, 3);
                }
            }
            else
            {
                // Other items: display only, dim slightly
                var canvasGroup = go.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0.5f;
                btn.interactable = false;
            }
        }
    }

    void UpdateCrystalSlotDisplay()
    {
        // Update crystal slot display for active tab
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
            if (selectedCrystal != null)
            {
                string colorHex = Item.GetRarityColorHex(selectedCrystal.rarity);
                text.text = $"<color={colorHex}>{selectedCrystal.itemName}</color>";
            }
            else
            {
                text.text = "Chọn Crystal Stone";
            }
        }
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
                // Weapon default rarity = Common (có thể mở rộng)
                targetRarity = Rarity.Common;
                canSocket = selectedGem != null && selectedGemSlotIndex >= 0;
            }
            else if (selectedEquipmentSlot >= 0 && EquipmentManager.Instance != null)
            {
                var equip = EquipmentManager.Instance.GetEquippedItemByIndex(selectedEquipmentSlot);
                if (equip != null)
                {
                    targetRarity = EquipmentManager.Instance.GetEquippedRarity(selectedEquipmentSlot);
                    canSocket = selectedGem != null && selectedGemSlotIndex >= 0;
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
            successRateText.text = $"Tỉ lệ thành công: {rate * 100f:F0}%";
        }

        // Socket button state
        if (socketButton)
        {
            socketButton.interactable = canSocket && selectedCrystal != null;
        }

        if (socketButtonText)
        {
            socketButtonText.text = canSocket ? "🔨 KHẢM GEM" : "Chọn Gem + Crystal + Slot";
        }
    }
}
