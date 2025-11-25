using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drop zone for equipment slots - accepts equipment drops and equips them
/// Supports double-click to remove equipment
/// </summary>
public class EquipmentSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Slot Settings")]
    [SerializeField] private int slotIndex = 0; // Slot index (0-3) instead of slot type
    [SerializeField] private Color emptySlotColor = Color.black; // Color when slot is empty (default: black)
    [SerializeField] private float doubleClickTime = 0.3f; // Time window for double click (seconds)

    private EquipmentPanelUI panelUI;
    private Image slotImage; // Use this component's Image to display equipment icon
    private Color originalColor; // Store original color
    
    // Double click detection
    private float lastClickTime = 0f;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = gameObject.AddComponent<Image>();
        }

        // Store original color
        originalColor = slotImage.color;

        panelUI = GetComponentInParent<EquipmentPanelUI>();
    }

    private void Start()
    {
        // Initialize slot color on start - set to empty color if no item is equipped
        // This ensures slots show empty color immediately, even if panel is inactive
        InitializeSlotColor();
    }

    /// <summary>
    /// Initialize slot color based on current equipment state
    /// </summary>
    private void InitializeSlotColor()
    {
        if (slotImage == null) return;

        // Try to get equipped item
        Item item = null;
        if (EquipmentManager.Instance != null)
        {
            item = EquipmentManager.Instance.GetEquippedItemByIndex(slotIndex);
        }

        if (item == null)
        {
            // Slot is empty - set to empty slot color
            slotImage.sprite = null;
            slotImage.color = emptySlotColor;
            slotImage.enabled = true;
        }
        else if (item.icon != null)
        {
            // Slot has item - show icon
            slotImage.sprite = item.icon;
            slotImage.color = originalColor;
            slotImage.enabled = true;
        }
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    public bool CanAcceptEquipment(Item equipmentItem)
    {
        if (equipmentItem == null || equipmentItem.itemType != ItemType.Equipment) return false;
        if (EquipmentManager.Instance == null) return false;

        // Any equipment can go into any slot (no restriction)
        return true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        EquipmentItemUI equipmentUI = eventData.pointerDrag?.GetComponent<EquipmentItemUI>();
        if (equipmentUI == null || equipmentUI.EquipmentItem == null) return;

        if (CanAcceptEquipment(equipmentUI.EquipmentItem))
        {
            // Equip the item into this slot (by index)
            if (EquipmentManager.Instance != null)
            {
                EquipmentManager.Instance.EquipItemByIndex(slotIndex, equipmentUI.EquipmentItem);
            }
            // Refresh panel UI
            if (panelUI != null)
            {
                panelUI.RefreshAfterEquip();
            }
        }
    }

    /// <summary>
    /// Set the slot icon (called after equipment is equipped)
    /// </summary>
    public void SetSlotIcon(Sprite icon)
    {
        if (slotImage != null)
        {
            if (icon != null)
            {
                // Slot has item - show icon
                slotImage.sprite = icon;
                slotImage.color = originalColor; // Use original color when has icon
            }
            else
            {
                // Slot is empty - show empty slot color
                slotImage.sprite = null;
                slotImage.color = emptySlotColor; // Use empty slot color when no item
            }
            // Ensure image is enabled
            slotImage.enabled = true;
        }
        else
        {
            Debug.LogWarning($"[EquipmentSlotDropZone] slotImage is null on slot {slotIndex}!");
        }
    }

    /// <summary>
    /// Show tooltip when mouse enters slot (if slot has equipped item)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null && EquipmentManager.Instance != null)
        {
            var equippedItem = EquipmentManager.Instance.GetEquippedItemByIndex(slotIndex);
            if (equippedItem != null)
            {
                ItemTooltipManager.Instance.ShowTooltip(equippedItem);
            }
        }
    }

    /// <summary>
    /// Hide tooltip when mouse exits slot
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.HideTooltip();
        }
    }

    /// <summary>
    /// Handle double-click to remove equipment from slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Check for double click
        float currentTime = Time.time;
        if (currentTime - lastClickTime < doubleClickTime)
        {
            // Double click detected - remove equipment
            RemoveEquipment();
        }
        lastClickTime = currentTime;
    }

    /// <summary>
    /// Remove equipment from this slot
    /// </summary>
    private void RemoveEquipment()
    {
        if (EquipmentManager.Instance == null) return;

        // Check if slot has an item
        var item = EquipmentManager.Instance.GetEquippedItemByIndex(slotIndex);
        if (item == null)
        {
            Debug.Log($"[EquipmentSlotDropZone] Slot {slotIndex} is already empty");
            return;
        }

        // Remove item
        bool removed = EquipmentManager.Instance.RemoveItemByIndex(slotIndex);
        if (removed)
        {
            Debug.Log($"[EquipmentSlotDropZone] Removed equipment from slot {slotIndex}");
            // Refresh panel UI
            if (panelUI != null)
            {
                panelUI.RefreshAfterEquip();
            }
        }
        else
        {
            Debug.LogWarning($"[EquipmentSlotDropZone] Failed to remove equipment from slot {slotIndex}");
        }
    }
}

