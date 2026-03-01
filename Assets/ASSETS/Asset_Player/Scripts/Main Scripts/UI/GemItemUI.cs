using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI component for a gem item in the forge viewport, supports drag & drop
/// Uses the same prefab structure as ItemUI (inventory item)
/// </summary>
public class GemItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Item gemItem;
    private int gemAmount;
    private Transform originalParent;
    private Canvas canvas;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private ItemUI itemUI; // Reference to ItemUI component (if exists on same GameObject)

    public Item GemItem => gemItem;
    public int GemAmount => gemAmount;

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
            canvas = FindFirstObjectByType<Canvas>();
        }
    }

    public void Initialize(Item item, int amount)
    {
        gemItem = item;
        gemAmount = amount;
        originalParent = transform.parent;

        // If ItemUI exists, initialize it (it will handle UI display)
        if (itemUI != null)
        {
            // ItemUI needs InventoryController, but we don't need it for forge
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
        if (gemItem == null || gemItem.itemType != ItemType.Gems) return;

        originalParent = transform.parent;
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        // Move to top of hierarchy for dragging
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (gemItem == null || gemItem.itemType != ItemType.Gems) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Check if dropped on a gem slot
        GemSlotDropZone dropZone = null;
        if (eventData.pointerCurrentRaycast.gameObject != null)
        {
            dropZone = eventData.pointerCurrentRaycast.gameObject.GetComponent<GemSlotDropZone>();
        }

        if (dropZone != null && dropZone.CanAcceptGem(gemItem))
        {
            // Drop successful - OnDrop in GemSlotDropZone will handle equip and inventory removal
            // Note: RefreshGemSlots() will be called by RefreshAfterGemEquip() which will update the icon
            // Return to original position (the gem will be removed from viewport by refresh)
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
    /// Show tooltip when mouse enters gem
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null && gemItem != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(gemItem);
        }
    }

    /// <summary>
    /// Hide tooltip when mouse exits gem
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.HideTooltip();
        }
    }
}

