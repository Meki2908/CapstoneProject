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
    [SerializeField] private Vector2 offset = new Vector2(-15f, -15f); // Offset from mouse cursor (4K)
    [SerializeField] private float padding = 20f; // Padding around text (4K)
    [SerializeField] private float minWidth = 350f; // Minimum tooltip width (4K)
    [SerializeField] private float maxWidth = 700f; // Maximum tooltip width (4K)
    [Header("Cursor Offset (Screen Pixels)")]
    [Tooltip("X: khoảng cách ngang (tooltip sang phải chuột)\nY: khoảng cách dọc (tooltip xuống dưới chuột)\nGiá trị càng lớn = tooltip càng xa chuột")]
    [SerializeField] private float cursorOffsetRight = 25f;
    [SerializeField] private float cursorOffsetDown = 25f;

    private Canvas canvas;
    private RectTransform canvasRectTransform;
    private RectTransform tooltipRectTransform;
    private Item currentItem;
    private Rarity currentRarity;
    private bool isShowing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // CHỈ xóa component, KHÔNG xóa gameObject (có thể là child của Canvas)
            Destroy(this);
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
            canvas = FindFirstObjectByType<Canvas>();
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
        ShowTooltip(item, item.rarity);
    }

    /// <summary>
    /// Show tooltip with runtime rarity (scaled stats)
    /// </summary>
    public void ShowTooltip(Item item, Rarity rarity)
    {
        if (item == null || tooltipText == null || tooltipPanel == null) return;

        currentItem = item;
        currentRarity = rarity;
        string tooltipContent = GetTooltipText(item, rarity);
        
        if (string.IsNullOrEmpty(tooltipContent))
        {
            HideTooltip();
            return;
        }

        tooltipText.text = tooltipContent;
        ResizeTooltipToContent();
        
        tooltipPanel.SetActive(true);
        isShowing = true;
        
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
        if (tooltipRectTransform == null) return;

        // Force anchors & pivot: top-left → tooltip mở rộng sang phải + xuống dưới
        tooltipRectTransform.anchorMin = Vector2.zero;
        tooltipRectTransform.anchorMax = Vector2.zero;
        tooltipRectTransform.pivot = new Vector2(0f, 1f);

        // Lấy kích thước tooltip trong screen pixels
        Vector3[] corners = new Vector3[4];
        tooltipRectTransform.GetWorldCorners(corners);
        // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right (world space)
        float tooltipScreenWidth = 0f;
        float tooltipScreenHeight = 0f;
        
        if (canvas != null)
        {
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenBL = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 screenTR = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            tooltipScreenWidth = screenTR.x - screenBL.x;
            tooltipScreenHeight = screenTR.y - screenBL.y;
        }

        // Vị trí chuột (screen space)
        Vector2 mouseScreen = Input.mousePosition;

        // Mặc định: tooltip bên phải + xuống dưới chuột
        float posX = mouseScreen.x + cursorOffsetRight;
        float posY = mouseScreen.y - cursorOffsetDown;

        // Nếu vượt cạnh PHẢI → flip sang trái
        if (posX + tooltipScreenWidth > Screen.width)
            posX = mouseScreen.x - cursorOffsetRight - tooltipScreenWidth;

        // Nếu vượt cạnh TRÊN (y lớn = trên)
        // pivot(0,1) nên tooltip mở xuống dưới từ posY
        if (posY > Screen.height)
            posY = Screen.height;

        // Nếu tooltip quá dài xuống dưới → đẩy lên
        if (posY - tooltipScreenHeight < 0)
            posY = tooltipScreenHeight;

        // Nếu vượt cạnh TRÁI
        if (posX < 0) posX = 0;

        // Convert screen → canvas local
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            new Vector2(posX, posY),
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPos
        );

        tooltipRectTransform.anchoredPosition = localPos;
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
    private string GetTooltipText(Item item, Rarity rarity)
    {
        if (item == null) return string.Empty;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // Item name and rarity (dùng runtime rarity)
        string rarityColor = GetRarityColor(rarity);
        sb.AppendLine($"<color={rarityColor}><b>{item.itemName}</b></color>");
        sb.AppendLine($"<color=#888888>{rarity}</color>");
        sb.AppendLine();

        // Item type specific stats
        switch (item.itemType)
        {
            case ItemType.Equipment:
                sb.AppendLine(GetEquipmentStats(item, rarity));
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
    private string GetEquipmentStats(Item item) => GetEquipmentStats(item, item.rarity);

    private string GetEquipmentStats(Item item, Rarity rarity)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"<color=#FFD700>Slot: {item.equipmentSlot}</color>");
        sb.AppendLine();

        bool hasStats = false;

        if (item.ScaledHPBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#00FF00>HP: +{item.ScaledHPBonus(rarity):F0}</color>");
            hasStats = true;
        }
        if (item.ScaledDefenseBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#00AAFF>Defense: +{item.ScaledDefenseBonus(rarity):F0}</color>");
            hasStats = true;
        }
        if (item.ScaledCritRateBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#FF00FF>Crit Rate: +{item.ScaledCritRateBonus(rarity) * 100f:F1}%</color>");
            hasStats = true;
        }
        if (item.ScaledCritDamageMultiplier(rarity) > 1f)
        {
            sb.AppendLine($"<color=#FF00FF>Crit Damage: +{(item.ScaledCritDamageMultiplier(rarity) - 1f) * 100f:F1}%</color>");
            hasStats = true;
        }
        if (item.ScaledMovementSpeedBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#00FFFF>Movement Speed: +{item.ScaledMovementSpeedBonus(rarity) * 100f:F1}%</color>");
            hasStats = true;
        }
        if (item.ScaledAttackSpeedBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#FFAA00>Attack Speed: +{item.ScaledAttackSpeedBonus(rarity) * 100f:F1}%</color>");
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
        return Item.GetRarityColorHex(rarity);
    }
}

