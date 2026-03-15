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
    [SerializeField] private float padding = 20f; // Padding around text (canvas units)
    [SerializeField] private float minWidth = 350f; // Minimum tooltip width (canvas units)
    [SerializeField] private float maxWidth = 700f; // Maximum tooltip width (canvas units)
    [Header("Cursor Offset (Screen Pixels)")]
    [Tooltip("Khoảng cách tooltip từ chuột tính bằng screen pixels.\nChỉ dùng X (sang trái). Y không dùng vì tooltip luôn ở giữa màn hình.")]
    [SerializeField] private float cursorOffsetPixels = 30f;

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

        // === FIX FLICKER: Tooltip KHÔNG BAO GIỜ chặn raycast ===
        // Nếu tooltip chặn pointer → OnPointerExit trên item → hide → OnPointerEnter → show → chớp
        CanvasGroup cg = tooltipPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = tooltipPanel.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;  // Tooltip không chặn click/hover
        cg.interactable = false;    // Không tương tác được

        // Tắt raycastTarget trên từng component
        if (tooltipBackground != null)
            tooltipBackground.raycastTarget = false;
        if (tooltipText != null)
            tooltipText.raycastTarget = false;

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
    /// Update tooltip position to follow mouse cursor.
    /// Tính toán hoàn toàn bằng screen pixels → convert sang world position.
    /// Không phụ thuộc vào hierarchy, anchor, hay parent.
    /// </summary>
    private void UpdateTooltipPosition()
    {
        if (tooltipRectTransform == null || canvas == null || canvasRectTransform == null) return;

        // Pivot: right-center → cạnh phải ở giữa dọc = điểm neo
        tooltipRectTransform.pivot = new Vector2(1f, 0.5f);

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // Lấy kích thước tooltip THỰC trong screen pixels qua world corners
        Vector3[] corners = new Vector3[4];
        tooltipRectTransform.GetWorldCorners(corners);
        Vector2 sBL = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 sTR = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        float tooltipScreenW = sTR.x - sBL.x;
        float tooltipScreenH = sTR.y - sBL.y;

        // --- Tính vị trí screen pixels ---
        Vector2 mouse = Input.mousePosition;

        // X: bên TRÁI chuột (pivot right → posX = cạnh phải tooltip)
        float posX = mouse.x - cursorOffsetPixels;

        // Y: theo chuột
        float posY = mouse.y;

        // --- Clamp trong screen ---

        // Nếu cạnh trái tooltip tràn ra ngoài → flip sang PHẢI chuột
        if (posX - tooltipScreenW < 0)
            posX = mouse.x + cursorOffsetPixels + tooltipScreenW;

        // Nếu vẫn tràn phải
        if (posX > Screen.width)
            posX = Screen.width;

        // Nếu tràn dưới
        if (posY - tooltipScreenH * 0.5f < 0)
            posY = tooltipScreenH * 0.5f;

        // Nếu tràn trên
        if (posY + tooltipScreenH * 0.5f > Screen.height)
            posY = Screen.height - tooltipScreenH * 0.5f;

        // --- Convert screen pixels → world position qua canvas ---
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, new Vector2(posX, posY), cam, out localPos
        );
        tooltipRectTransform.position = canvasRectTransform.TransformPoint(localPos);
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

        // Description
        if (!string.IsNullOrEmpty(item.description))
        {
            sb.AppendLine(item.description);
            sb.AppendLine();
        }

        // Item type specific stats
        switch (item.itemType)
        {
            case ItemType.Equipment:
                sb.AppendLine(GetEquipmentStats(item, rarity));
                break;
            case ItemType.Gems:
                sb.AppendLine(GetGemStats(item));
                break;
            case ItemType.CrystalStone:
                sb.AppendLine(GetCrystalStoneStats(item));
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
        
        return sb.ToString();
    }

    /// <summary>
    /// Get formatted crystal stone stats with success rates
    /// </summary>
    private string GetCrystalStoneStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("<color=#00FFFF>Crystal Stone</color>");
        sb.AppendLine("<color=#888888>Nguyên liệu khảm - tăng tỉ lệ thành công</color>");
        sb.AppendLine();
        sb.AppendLine("<color=#FFD700>Tỉ lệ thành công:</color>");

        if (SocketingManager.Instance != null)
        {
            string[] rarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythic" };
            string[] rarityHexes = { "FFFFFF", "00FF00", "3498DB", "9B59B6", "FFD700", "FF4444" };
            for (int ri = 0; ri < rarityNames.Length; ri++)
            {
                float rate = SocketingManager.Instance.CalculateSuccessRate((Rarity)(ri + 1), item.rarity);
                sb.AppendLine($"  <color=#{rarityHexes[ri]}>{rarityNames[ri]}</color>: {rate * 100f:F0}%");
            }
        }

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

