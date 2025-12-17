using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI component for an equipment item in the equipment panel viewport, supports drag & drop
/// Uses the same prefab structure as ItemUI (inventory item)
/// </summary>
public class EquipmentItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Item equipmentItem;
    private int equipmentAmount;
    private Transform originalParent;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private ItemUI itemUI; // Reference to ItemUI component (if exists on same GameObject)

    public Item EquipmentItem => equipmentItem;
    public int EquipmentAmount => equipmentAmount;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Try to find ItemUI component (if using shared prefab)
        itemUI = GetComponent<ItemUI>();

        // Auto-find canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
        }
    }

    public void Initialize(Item item, int amount)
    {
        equipmentItem = item;
        equipmentAmount = amount;
        originalParent = transform.parent;

        // If ItemUI exists, initialize it (it will handle UI display)
        if (itemUI != null)
        {
            // ItemUI needs InventoryController, but we don't need it for equipment panel
            // So we'll just set the item data manually
            var itemIcon = transform.Find("Item Icon")?.GetComponent<Image>();
            var itemNameText = transform.Find("Item name")?.GetComponent<TMPro.TextMeshProUGUI>();
            var itemAmountText = transform.Find("Item amount")?.GetComponent<TMPro.TextMeshProUGUI>();

            if (itemIcon != null && item != null && item.icon != null)
            {
                itemIcon.sprite = item.icon;
            }
            if (itemNameText != null && item != null)
            {
                itemNameText.text = item.itemName;
            }
            if (itemAmountText != null && item != null)
            {
                itemAmountText.text = amount > 1 ? amount.ToString() : "";
                itemAmountText.gameObject.SetActive(item.isStackable && amount > 1);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (equipmentItem == null || equipmentItem.itemType != ItemType.Equipment) return;

        originalParent = transform.parent;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Move to top of hierarchy for dragging
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (equipmentItem == null || equipmentItem.itemType != ItemType.Equipment) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Check if dropped on an equipment slot
        EquipmentSlotDropZone dropZone = null;
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            dropZone = eventData.pointerCurrentRaycast.gameObject.GetComponent<EquipmentSlotDropZone>();
        }

        if (dropZone != null && dropZone.CanAcceptEquipment(equipmentItem))
        {
            // Drop successful - OnDrop in EquipmentSlotDropZone will handle equip and inventory removal
            // Return to original position (the equipment will be removed from viewport by refresh)
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            // Return to original position
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// Show tooltip when mouse enters equipment
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null && equipmentItem != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(equipmentItem);
        }
    }

    /// <summary>
    /// Hide tooltip when mouse exits equipment
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.HideTooltip();
        }
    }
}

