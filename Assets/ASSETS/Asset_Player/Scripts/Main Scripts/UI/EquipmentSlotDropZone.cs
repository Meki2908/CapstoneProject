using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Equipment slot hiển thị trong tab Trang bị (Inventory)
/// CHỈ XEM — di chuột vào để xem tooltip, KHÔNG kéo thả, KHÔNG remove
/// Kéo thả equipment chỉ dùng ở NPC Thợ Rèn (BlacksmithUI)
/// </summary>
public class EquipmentSlotDropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Settings")]
    [SerializeField] private int slotIndex = 0; // Slot index (0-3)
    [SerializeField] private Color emptySlotColor = Color.black;

    public Image slotImage;
    private Color originalColor;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = gameObject.AddComponent<Image>();
        }
        originalColor = slotImage.color;
    }

    private void Start()
    {
        InitializeSlotColor();
    }

    /// <summary>
    /// Initialize slot color based on current equipment state
    /// </summary>
    private void InitializeSlotColor()
    {
        if (slotImage == null) return;

        Item item = null;
        if (EquipmentManager.Instance != null)
        {
            item = EquipmentManager.Instance.GetEquippedItemByIndex(slotIndex);
        }

        if (item == null)
        {
            slotImage.sprite = null;
            slotImage.color = emptySlotColor;
            slotImage.enabled = true;
        }
        else if (item.icon != null)
        {
            slotImage.sprite = item.icon;
            slotImage.color = originalColor;
            slotImage.enabled = true;
        }
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// Set the slot icon (called by EquipmentPanelUI.RefreshEquipmentSlots)
    /// </summary>
    public void SetSlotIcon(Sprite icon)
    {
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
            if (slotImage == null)
                slotImage = gameObject.AddComponent<Image>();
            if (originalColor == Color.clear)
                originalColor = slotImage.color;
        }

        if (slotImage != null)
        {
            if (icon != null)
            {
                slotImage.sprite = icon;
                slotImage.color = originalColor;
            }
            else
            {
                slotImage.sprite = null;
                slotImage.color = emptySlotColor;
            }
            slotImage.enabled = true;
        }
    }

    /// <summary>
    /// Hover → hiện tooltip info equipment đang trang bị
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (EquipmentManager.Instance == null) return;

        var equippedItem = EquipmentManager.Instance.GetEquippedItemByIndex(slotIndex);
        if (equippedItem != null && ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(equippedItem);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.HideTooltip();
        }
    }
}
