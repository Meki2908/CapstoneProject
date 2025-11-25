using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the UI display and interactions for a single inventory item
/// </summary>
public class ItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Item References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemAmountText;
    [SerializeField] private Button removeButton; // The X button
    [SerializeField] private Button itemButton; // The main item button

    [Header("Tooltip")]
    [SerializeField] private bool useTooltip = true; // Enable/disable tooltip for this item

    [Header("Item Data")]
    private Item itemData;
    private int itemAmount = 1;

    // Reference to inventory controller for removal
    private InventoryController inventoryController;

    private void Awake()
    {
        // Auto-find components if not assigned
        if (itemIcon == null)
            itemIcon = transform.Find("Item Icon")?.GetComponent<Image>();

        if (itemNameText == null)
            itemNameText = transform.Find("Item name")?.GetComponent<TextMeshProUGUI>();

        if (itemAmountText == null)
            itemAmountText = transform.Find("Item amount")?.GetComponent<TextMeshProUGUI>();

        if (removeButton == null)
            removeButton = transform.Find("Remove Button")?.GetComponent<Button>();

        if (itemButton == null)
            itemButton = GetComponent<Button>();

        // Hide remove button by default
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(false);
        }

        // Setup remove button click
        if (removeButton != null)
        {
            removeButton.onClick.AddListener(OnRemoveButtonClicked);
        }
    }

    /// <summary>
    /// Initialize the item UI with data
    /// </summary>
    public void Initialize(Item item, int amount, InventoryController controller)
    {
        itemData = item;
        itemAmount = amount;
        inventoryController = controller;

        UpdateUI();
    }

    /// <summary>
    /// Update the UI elements with current item data
    /// </summary>
    private void UpdateUI()
    {
        if (itemData == null) return;

        // Update icon
        if (itemIcon != null && itemData.icon != null)
        {
            itemIcon.sprite = itemData.icon;
        }

        // Update name
        if (itemNameText != null)
        {
            itemNameText.text = itemData.itemName;
        }

        // Update amount (only show if stackable and amount > 1)
        if (itemAmountText != null)
        {
            if (itemData.isStackable && itemAmount > 1)
            {
                itemAmountText.text = itemAmount.ToString();
                itemAmountText.gameObject.SetActive(true);
            }
            else
            {
                itemAmountText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Show or hide the remove button
    /// </summary>
    public void SetRemoveButtonVisible(bool visible)
    {
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Called when the X button is clicked
    /// </summary>
    private void OnRemoveButtonClicked()
    {
        RemoveItem();
    }

    /// <summary>
    /// Public method to remove this item - can be assigned to button OnClick in Unity Inspector
    /// </summary>
    public void RemoveItem()
    {
        if (inventoryController != null && itemData != null)
        {
            inventoryController.RemoveItem(this, itemData, itemAmount);
        }
        else
        {
            Debug.LogWarning("[ItemUI] Cannot remove item: InventoryController or ItemData is null");
        }
    }

    /// <summary>
    /// Get the item data
    /// </summary>
    public Item GetItemData()
    {
        return itemData;
    }

    /// <summary>
    /// Get the item amount
    /// </summary>
    public int GetItemAmount()
    {
        return itemAmount;
    }

    /// <summary>
    /// Update the item amount
    /// </summary>
    public void SetItemAmount(int amount)
    {
        itemAmount = amount;
        UpdateUI();
    }

    /// <summary>
    /// Show tooltip when mouse enters item
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (useTooltip && ItemTooltipManager.Instance != null && itemData != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(itemData);
        }
    }

    /// <summary>
    /// Hide tooltip when mouse exits item
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (useTooltip && ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.HideTooltip();
        }
    }
}

