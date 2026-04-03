using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component cho ô đặt Crystal Stone trong Blacksmith GUI.
/// Click để chọn crystal stone từ viewport, hiện viền glow theo rarity.
/// </summary>
public class CrystalSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    [Header("UI References")]
    [SerializeField] private Image crystalIcon;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image glowBorder;
    [SerializeField] private TextMeshProUGUI crystalNameText;
    [SerializeField] private TextMeshProUGUI rarityText;

    [Header("Visual Settings")]
    [SerializeField] private Color emptyColor = new Color(0.15f, 0.1f, 0.2f, 0.6f);
    [SerializeField] private Sprite emptySlotSprite;

    private Item currentCrystal;
    private BlacksmithUI blacksmithUI;

    void Start()
    {
        blacksmithUI = GetComponentInParent<BlacksmithUI>();
    }

    /// <summary>
    /// Cập nhật hiển thị crystal stone
    /// </summary>
    public void SetCrystal(Item crystal)
    {
        currentCrystal = crystal;

        if (crystal != null)
        {
            if (crystalIcon)
            {
                crystalIcon.sprite = crystal.icon;
                crystalIcon.enabled = true;
                crystalIcon.color = Color.white;
            }

            // Glow border theo rarity
            if (glowBorder)
            {
                glowBorder.enabled = true;
                string colorHex = Item.GetRarityColorHex(crystal.rarity);
                Color rarityColor;
                ColorUtility.TryParseHtmlString(colorHex, out rarityColor);
                glowBorder.color = rarityColor;
            }

            if (slotBackground)
            {
                string colorHex = Item.GetRarityColorHex(crystal.rarity);
                Color rarityColor;
                ColorUtility.TryParseHtmlString(colorHex, out rarityColor);
                slotBackground.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.2f);
            }

            if (crystalNameText)
            {
                string colorHex = Item.GetRarityColorHex(crystal.rarity);
                crystalNameText.text = $"<color={colorHex}>{crystal.itemName}</color>";
            }

            if (rarityText)
            {
                string colorHex = Item.GetRarityColorHex(crystal.rarity);
                rarityText.text = $"<color={colorHex}>[{crystal.rarity}]</color>";
            }
        }
        else
        {
            // Empty
            if (crystalIcon)
            {
                if (emptySlotSprite)
                    crystalIcon.sprite = emptySlotSprite;
                crystalIcon.enabled = emptySlotSprite != null;
                crystalIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }

            if (glowBorder) glowBorder.enabled = false;
            if (slotBackground) slotBackground.color = emptyColor;
            if (crystalNameText) crystalNameText.text = "Chọn Crystal Stone";
            if (rarityText) rarityText.text = "";
        }
    }

    // ─── Drag & Drop Support ─────────────────────────────────────

    public void OnDrop(PointerEventData eventData)
    {
        // Accept crystal stone drops from viewport
        var draggedItemUI = eventData.pointerDrag?.GetComponent<ItemUI>();
        if (draggedItemUI != null)
        {
            Item draggedItem = draggedItemUI.GetItem();
            if (draggedItem != null && draggedItem.itemType == ItemType.CrystalStone)
            {
                if (blacksmithUI != null)
                    blacksmithUI.SelectCrystal(draggedItem);
            }
        }
    }

    // ─── Hover → Tooltip ─────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentCrystal != null && ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(currentCrystal);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null)
            ItemTooltipManager.Instance.HideTooltip();
    }

    public Item GetCurrentCrystal() => currentCrystal;
    public bool IsEmpty() => currentCrystal == null;
}
