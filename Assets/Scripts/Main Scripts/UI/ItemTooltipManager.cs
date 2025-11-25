using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Singleton manager for item tooltip that displays item stats and follows mouse cursor
/// Automatically resizes to fit content
/// </summary>
public class ItemTooltipManager : MonoBehaviour
{
    public static ItemTooltipManager Instance { get; private set; }

    [Header("Tooltip References")]
    [SerializeField] private GameObject tooltipPanel; // The Image GameObject containing the tooltip
    [SerializeField] private TextMeshProUGUI tooltipText; // The TMP text component
    [SerializeField] private Image tooltipBackground; // The Image component (for resizing)
    
    [Header("Settings")]
    [SerializeField] private Vector2 offset = new Vector2(10f, 10f); // Offset from mouse cursor
    [SerializeField] private float padding = 10f; // Padding around text
    [SerializeField] private float minWidth = 200f; // Minimum tooltip width
    [SerializeField] private float maxWidth = 400f; // Maximum tooltip width

    private Canvas canvas;
    private RectTransform canvasRectTransform;
    private RectTransform tooltipRectTransform;
    private Item currentItem;
    private bool isShowing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-find components if not assigned
        if (tooltipPanel == null)
        {
            tooltipPanel = gameObject;
        }
        
        if (tooltipBackground == null)
        {
            tooltipBackground = GetComponent<Image>();
        }
        
        if (tooltipText == null)
        {
            tooltipText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Find canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }

        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        if (tooltipPanel != null)
        {
            tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
        }
        
        // Hide tooltip by default
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (isShowing && tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdateTooltipPosition();
        }
    }

    /// <summary>
    /// Show tooltip for an item
    /// </summary>
    public void ShowTooltip(Item item)
    {
        if (item == null || tooltipText == null || tooltipPanel == null) return;

        currentItem = item;
        string tooltipContent = GetTooltipText(item);
        
        if (string.IsNullOrEmpty(tooltipContent))
        {
            HideTooltip();
            return;
        }

        tooltipText.text = tooltipContent;
        ResizeTooltipToContent();
        
        tooltipPanel.SetActive(true);
        isShowing = true;
        
        // Update position immediately
        UpdateTooltipPosition();
    }

    /// <summary>
    /// Hide the tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        isShowing = false;
        currentItem = null;
    }

    /// <summary>
    /// Update tooltip position to follow mouse cursor
    /// </summary>
    private void UpdateTooltipPosition()
    {
        if (canvas == null || canvasRectTransform == null || tooltipRectTransform == null) return;

        Vector2 mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            Input.mousePosition,
            canvas.worldCamera,
            out mousePosition
        );

        // Apply offset
        mousePosition += offset;

        // Clamp to canvas bounds to prevent tooltip from going off-screen
        Vector2 tooltipSize = tooltipRectTransform.sizeDelta;
        float maxX = canvasRectTransform.rect.width - tooltipSize.x;
        float maxY = canvasRectTransform.rect.height - tooltipSize.y;
        
        mousePosition.x = Mathf.Clamp(mousePosition.x, 0f, maxX);
        mousePosition.y = Mathf.Clamp(mousePosition.y, 0f, maxY);

        tooltipRectTransform.anchoredPosition = mousePosition;
    }

    /// <summary>
    /// Resize tooltip background to fit text content
    /// </summary>
    private void ResizeTooltipToContent()
    {
        if (tooltipText == null || tooltipBackground == null || tooltipRectTransform == null) return;

        // Force text to update its preferred size
        tooltipText.ForceMeshUpdate();
        
        // Get preferred size from text (unconstrained)
        Vector2 preferredSize = tooltipText.GetPreferredValues();
        
        // Clamp width to min/max
        float clampedWidth = Mathf.Clamp(preferredSize.x, minWidth, maxWidth);
        
        // If width was clamped, recalculate height with the clamped width
        if (Mathf.Abs(clampedWidth - preferredSize.x) > 0.01f)
        {
            // Set text width constraint and recalculate
            tooltipText.rectTransform.sizeDelta = new Vector2(clampedWidth, 0f);
            tooltipText.ForceMeshUpdate();
            preferredSize = tooltipText.GetPreferredValues();
        }
        
        // Use clamped width
        preferredSize.x = clampedWidth;
        
        // Add padding
        Vector2 newSize = preferredSize + new Vector2(padding * 2f, padding * 2f);
        
        // Set tooltip size
        tooltipRectTransform.sizeDelta = newSize;
    }

    /// <summary>
    /// Get formatted tooltip text based on item type
    /// </summary>
    private string GetTooltipText(Item item)
    {
        if (item == null) return "";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // Item name and rarity
        string rarityColor = GetRarityColor(item.rarity);
        sb.AppendLine($"<color={rarityColor}><b>{item.itemName}</b></color>");
        sb.AppendLine($"<color=#888888>{item.rarity}</color>");
        sb.AppendLine();

        // Item type specific stats (description removed)
        switch (item.itemType)
        {
            case ItemType.Equipment:
                sb.AppendLine(GetEquipmentStats(item));
                break;
            case ItemType.Gems:
                sb.AppendLine(GetGemStats(item));
                break;
            case ItemType.Consumable:
                sb.AppendLine(GetConsumableStats(item));
                break;
            case ItemType.Material:
                sb.AppendLine(GetMaterialStats(item));
                break;
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Get formatted equipment stats
    /// </summary>
    private string GetEquipmentStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"<color=#FFD700>Slot: {item.equipmentSlot}</color>");
        sb.AppendLine();

        bool hasStats = false;

        if (item.hpBonus > 0f)
        {
            sb.AppendLine($"<color=#00FF00>HP: +{item.hpBonus:F0}</color>");
            hasStats = true;
        }
        if (item.defenseBonus > 0f)
        {
            sb.AppendLine($"<color=#00AAFF>Defense: +{item.defenseBonus:F0}</color>");
            hasStats = true;
        }
        if (item.critRateBonus > 0f)
        {
            sb.AppendLine($"<color=#FF00FF>Crit Rate: +{item.critRateBonus * 100f:F1}%</color>");
            hasStats = true;
        }
        if (item.critDamageMultiplier > 1f)
        {
            sb.AppendLine($"<color=#FF00FF>Crit Damage: +{(item.critDamageMultiplier - 1f) * 100f:F1}%</color>");
            hasStats = true;
        }
        if (item.movementSpeedBonus > 0f)
        {
            sb.AppendLine($"<color=#00FFFF>Movement Speed: +{item.movementSpeedBonus * 100f:F1}%</color>");
            hasStats = true;
        }
        if (item.attackSpeedBonus > 0f)
        {
            sb.AppendLine($"<color=#FFAA00>Attack Speed: +{item.attackSpeedBonus * 100f:F1}%</color>");
            hasStats = true;
        }

        if (!hasStats)
        {
            sb.AppendLine("<color=#888888>No stats</color>");
        }

        // Passive description
        if (!string.IsNullOrEmpty(item.passiveDescription))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#FFD700>Passive:</color>");
            sb.AppendLine($"<color=#FFFF00>{item.passiveDescription}</color>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get formatted gem stats
    /// </summary>
    private string GetGemStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"<color=#FFD700>Type: {item.gemType}</color>");
        sb.AppendLine();
        
        string statText = item.GetGemStatText();
        if (!string.IsNullOrEmpty(statText))
        {
            sb.AppendLine($"<color=#00FF00>{statText}</color>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get formatted consumable stats
    /// </summary>
    private string GetConsumableStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        // Special handling for Health Potion
        if (item != null && item.itemName != null && item.itemName.ToLower().Contains("health potion"))
        {
            sb.AppendLine("<color=#00FF00>+ 50% HP</color>");
        }
        else
        {
            sb.AppendLine("<color=#FFD700>Consumable Item</color>");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Get formatted material stats
    /// </summary>
    private string GetMaterialStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine("<color=#FFD700>Material</color>");
        
        // Add any material-specific stats here if needed
        
        return sb.ToString();
    }

    /// <summary>
    /// Get color hex code for rarity
    /// </summary>
    private string GetRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return "#FFFFFF"; // White
            case Rarity.Epic:
                return "#9B59B6"; // Purple
            case Rarity.Legendary:
                return "#FFD700"; // Gold
            default:
                return "#FFFFFF";
        }
    }
}

