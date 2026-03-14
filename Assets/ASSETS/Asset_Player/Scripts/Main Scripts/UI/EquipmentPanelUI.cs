using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Equipment Panel UI: display 4 equipment slots and actions (Equip Best / Remove All)
/// </summary>
public class EquipmentPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject equipmentPanel;

    [Header("Equipment Slots")]
    [SerializeField] private EquipmentSlotDropZone[] equipmentSlotDropZones = new EquipmentSlotDropZone[4];

    [Header("Equipment Viewport")]
    [SerializeField] private Transform equipmentViewportContent;
    [SerializeField] private InventoryController inventoryController; // To get itemUIPrefab

    private GameObject itemUIPrefab; // Shared prefab from InventoryController
    private List<EquipmentItemUI> currentEquipmentUIs = new List<EquipmentItemUI>();

    private void Awake()
    {
        // Get itemUIPrefab from InventoryController
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        // Setup equipment slot drop zones (by index, not type)
        for (int i = 0; i < equipmentSlotDropZones.Length && i < 4; i++)
        {
            if (equipmentSlotDropZones[i] != null)
            {
                equipmentSlotDropZones[i].SetSlotIndex(i);
            }
        }

        // Subscribe to inventory and equipment changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RefreshEquipmentViewport;
        }
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
        }
    }

    private void Start()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RefreshEquipmentViewport;
        }
        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
        }
    }

    private void OnEquipmentChanged()
    {
        RefreshEquipmentSlots();
        RefreshEquipmentViewport();
    }

    /// <summary>
    /// Open the equipment panel
    /// </summary>
    public void OpenPanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(true);
        }
        RefreshAll();
    }

    /// <summary>
    /// Close the equipment panel
    /// </summary>
    public void ClosePanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }

    private void RefreshAll()
    {
        RefreshEquipmentSlots();
        RefreshEquipmentViewport();
    }

    private void RefreshEquipmentSlots()
    {
        if (equipmentSlotDropZones == null || equipmentSlotDropZones.Length < 4)
        {
            Debug.LogWarning("[EquipmentPanelUI] equipmentSlotDropZones is null or length < 4!");
            return;
        }
        
        if (EquipmentManager.Instance == null)
        {
            // Clear to empty
            for (int i = 0; i < equipmentSlotDropZones.Length; i++)
            {
                if (equipmentSlotDropZones[i] != null)
                {
                    equipmentSlotDropZones[i].SetSlotIcon(null);
                }
            }
            return;
        }

        // Refresh slots by index (not by type)
        for (int i = 0; i < 4; i++)
        {
            var item = EquipmentManager.Instance.GetEquippedItemByIndex(i);
            if (equipmentSlotDropZones[i] != null)
            {
                Sprite itemIcon = item != null && item.icon != null ? item.icon : null;
                equipmentSlotDropZones[i].SetSlotIcon(itemIcon);
            }
        }
    }

    // Button: Equip Best
    public void OnClick_EquipBest()
    {
        if (EquipmentManager.Instance == null) return;
        EquipmentManager.Instance.EquipBest();
        RefreshAfterEquip();
    }

    /// <summary>
    /// Refresh UI after equipment equip/remove
    /// </summary>
    public void RefreshAfterEquip()
    {
        StartCoroutine(RefreshAfterDelay());
    }

    private IEnumerator RefreshAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        RefreshEquipmentSlots();
        RefreshEquipmentViewport();
    }

    // Button: Remove All
    public void OnClick_RemoveAll()
    {
        if (EquipmentManager.Instance == null) return;
        EquipmentManager.Instance.RemoveAll();
        RefreshEquipmentSlots();
        RefreshEquipmentViewport(); // Refresh to show returned items
    }

    /// <summary>
    /// Refresh the equipment viewport with equipment items from inventory
    /// Uses the same itemUIPrefab from InventoryController
    /// </summary>
    private void RefreshEquipmentViewport()
    {
        if (equipmentViewportContent == null) return;

        // Get itemUIPrefab from InventoryController (ensure it's available)
        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (itemUIPrefab == null && inventoryController != null)
        {
            itemUIPrefab = inventoryController.ItemUIPrefab;
        }

        if (itemUIPrefab == null)
        {
            Debug.LogWarning("[EquipmentPanelUI] itemUIPrefab is null! Make sure InventoryController has itemUIPrefab assigned.");
            return;
        }

        // Clear existing UI
        foreach (Transform child in equipmentViewportContent)
        {
            Destroy(child.gameObject);
        }
        currentEquipmentUIs.Clear();

        // Get all equipment items from inventory
        if (InventoryManager.Instance != null)
        {
            var allItems = InventoryManager.Instance.GetAllItems();
            foreach (var (item, amount) in allItems)
            {
                if (item != null && item.itemType == ItemType.Equipment)
                {
                    GameObject equipmentUIObject = Instantiate(itemUIPrefab, equipmentViewportContent);
                    
                    // Initialize ItemUI component first (for icon, name, amount display)
                    ItemUI itemUI = equipmentUIObject.GetComponent<ItemUI>();
                    if (itemUI != null && inventoryController != null)
                    {
                        itemUI.Initialize(item, amount, inventoryController);
                    }
                    
                    // Add EquipmentItemUI component if not exists (for drag & drop)
                    EquipmentItemUI equipmentUI = equipmentUIObject.GetComponent<EquipmentItemUI>();
                    if (equipmentUI == null)
                    {
                        equipmentUI = equipmentUIObject.AddComponent<EquipmentItemUI>();
                    }
                    
                    if (equipmentUI != null)
                    {
                        equipmentUI.Initialize(item, amount);
                        currentEquipmentUIs.Add(equipmentUI);
                    }
                }
            }
        }
    }
}

