using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI cho 1 item card trong Dungeon Reward Screen
/// Setup bằng Setup(name, icon, rarity, qty)
/// </summary>
public class DungeonRewardItemUI : MonoBehaviour
{
    [Header("References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI quantityText;
    public Image rarityBar;

    /// <summary>
    /// Setup card hiển thị
    /// </summary>
    public void Setup(string itemName, Sprite icon, Rarity rarity, int quantity)
    {
        // Icon
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
            }
        }

        // Name — có màu theo rarity
        if (nameText != null)
        {
            string colorHex = Item.GetRarityColorHex(rarity);
            nameText.text = $"<color={colorHex}>{itemName}</color>";
        }

        // Quantity
        if (quantityText != null)
        {
            quantityText.text = quantity > 1 ? $"x{quantity}" : "";
        }

        // Rarity bar color
        if (rarityBar != null)
        {
            Color rarityColor;
            ColorUtility.TryParseHtmlString(Item.GetRarityColorHex(rarity), out rarityColor);
            rarityBar.color = rarityColor;
        }

        // Border (Outline) color theo rarity
        Outline outline = GetComponent<Outline>();
        if (outline != null)
        {
            Color borderColor;
            ColorUtility.TryParseHtmlString(Item.GetRarityColorHex(rarity), out borderColor);
            borderColor.a = 0.8f;
            outline.effectColor = borderColor;
        }

        // Rarity label text on name
        if (nameText != null)
        {
            string rarityName = rarity.ToString();
            string colorHex = Item.GetRarityColorHex(rarity);
            nameText.text = $"<color={colorHex}><size=10>[{rarityName}]</size>\n{itemName}</color>";
        }
    }
}
