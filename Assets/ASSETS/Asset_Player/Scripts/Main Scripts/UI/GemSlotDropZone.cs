using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Gem slot hiển thị trong tab Vũ khí (Inventory)
/// CHỈ XEM — di chuột vào để xem tooltip, KHÔNG kéo thả, KHÔNG remove
/// Kéo thả gem chỉ dùng ở NPC Thợ Rèn (BlacksmithUI)
/// </summary>
public class GemSlotDropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Settings")]
    [SerializeField] private int slotIndex; // 0, 1, or 2
    [SerializeField] private Color emptySlotColor = Color.black;

    private WeaponType currentWeaponType = WeaponType.None;
    private Image slotImage;
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

    public void SetWeaponType(WeaponType weaponType)
    {
        currentWeaponType = weaponType;
    }

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// Set the slot icon (called by WeaponForgeUI.RefreshGemSlots)
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
    /// Hover → hiện tooltip info gem đang gắn
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (WeaponGemManager.Instance == null || currentWeaponType == WeaponType.None) return;

        var gem = WeaponGemManager.Instance.GetEquippedGem(currentWeaponType, slotIndex);
        if (gem != null && ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(gem);
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
