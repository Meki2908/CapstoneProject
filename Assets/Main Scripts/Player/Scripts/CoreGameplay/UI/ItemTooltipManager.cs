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

    /// <summary>
    /// When true, all tooltip display is suppressed (used by Blacksmith UI)
    /// </summary>
    public bool SuppressTooltip { get; set; } = false;

    [Header("Tooltip References")]
    [SerializeField] private GameObject tooltipPanel; // The Image GameObject containing the tooltip
    [SerializeField] private TextMeshProUGUI tooltipText; // Legacy single TMP (backward compatibility)
    [SerializeField] private TextMeshProUGUI itemNameText; // Optional dedicated TMP for item name
    [SerializeField] private TextMeshProUGUI itemDescriptionText; // Optional dedicated TMP for description + stats
    [SerializeField] private Image tooltipBackground; // The Image component (for resizing)

    [Header("Font")]
    [Tooltip("Font cho tên item. Nếu null = dùng font từ prefab.")]
    [SerializeField] private TMP_FontAsset nameFont;
    [Tooltip("Font cho phần mô tả/stats. Nếu null = dùng font từ prefab.")]
    [SerializeField] private TMP_FontAsset bodyFont;
    
    [Header("Font Size")]
    [Tooltip("Cỡ chữ tên item (0 = giữ nguyên từ prefab)")]
    [SerializeField] private float nameFontSize = 0f;
    [Tooltip("Cỡ chữ mô tả/stats (0 = giữ nguyên từ prefab)")]
    [SerializeField] private float bodyFontSize = 0f;

    [Header("Layout — Padding (viền trang trí)")]
    [SerializeField] private float paddingTop = 25f;
    [SerializeField] private float paddingBottom = 40f;
    [SerializeField] private float paddingLeft = 30f;
    [SerializeField] private float paddingRight = 30f;
    [SerializeField] private float minWidth = 350f;
    [SerializeField] private float maxWidth = 700f;
    [Tooltip("Chiều cao tối đa (0 = không giới hạn)")]
    [SerializeField] private float maxHeight = 0f;
    [Tooltip("Khoảng cách giữa tên item và phần mô tả")]
    [SerializeField] private float sectionGap = 10f;
    [Tooltip("Chiều cao tối thiểu của tên item")]
    [SerializeField] private float minNameHeight = 40f;
    [Tooltip("Chiều cao tối thiểu của phần mô tả")]
    [SerializeField] private float minDescHeight = 50f;

    [Header("Cursor")]
    [Tooltip("Khoảng cách tooltip từ chuột (screen pixels)")]
    [SerializeField] private float cursorOffsetPixels = 30f;

    [Header("Stat Colors")]
    [SerializeField] private Color colorHP = new Color(0.118f, 0.549f, 0.118f, 1f);           // #1E8C1E
    [SerializeField] private Color colorDefense = new Color(0.157f, 0.471f, 0.722f, 1f);      // #2878B8
    [SerializeField] private Color colorCrit = new Color(0.722f, 0.145f, 0.431f, 1f);         // #B8256E
    [SerializeField] private Color colorMoveSpeed = new Color(0.055f, 0.557f, 0.518f, 1f);    // #0E8E84
    [SerializeField] private Color colorAtkSpeed = new Color(0.8f, 0.447f, 0f, 1f);           // #CC7200
    [SerializeField] private Color colorSlotPassive = new Color(0.8f, 0.533f, 0f, 1f);        // #CC8800
    [SerializeField] private Color colorPassiveDesc = new Color(0.69f, 0.49f, 0.063f, 1f);    // #B07D10
    [SerializeField] private Color colorRarity = new Color(0.176f, 0.125f, 0.082f, 1f);       // #2D2015
    [SerializeField] private Color colorDescription = new Color(0.176f, 0.125f, 0.082f, 1f);  // #2D2015

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

        // Auto-find named text fields inside tooltip panel if available
        if (itemNameText == null && tooltipPanel != null)
        {
            Transform nameTf = tooltipPanel.transform.Find("Item name");
            if (nameTf != null) itemNameText = nameTf.GetComponent<TextMeshProUGUI>();
        }
        if (itemDescriptionText == null && tooltipPanel != null)
        {
            Transform descTf = tooltipPanel.transform.Find("Item description");
            if (descTf != null) itemDescriptionText = descTf.GetComponent<TextMeshProUGUI>();
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
        if (tooltipText != null) tooltipText.raycastTarget = false;
        if (itemNameText != null) itemNameText.raycastTarget = false;
        if (itemDescriptionText != null) itemDescriptionText.raycastTarget = false;

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
            // Live update: re-apply settings + resize every frame
            // so Inspector changes take effect immediately in Play Mode
            if (currentItem != null && itemNameText != null && itemDescriptionText != null)
            {
                // Re-apply fonts and sizes
                if (nameFont != null) itemNameText.font = nameFont;
                if (bodyFont != null) itemDescriptionText.font = bodyFont;
                if (nameFontSize > 0f) itemNameText.fontSize = nameFontSize;
                if (bodyFontSize > 0f) itemDescriptionText.fontSize = bodyFontSize;
                itemNameText.enableWordWrapping = true;
                itemDescriptionText.enableWordWrapping = true;
            }
            ResizeTooltipToContent();
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
        if (SuppressTooltip) { HideTooltip(); return; }
        if (item == null || tooltipPanel == null) return;

        currentItem = item;
        currentRarity = rarity;

        string nameContent = GetTooltipNameText(item, rarity);
        string bodyContent = GetTooltipBodyText(item, rarity);

        // New mode: separate name/description fields
        if (itemNameText != null && itemDescriptionText != null)
        {
            itemNameText.text = nameContent;
            itemDescriptionText.text = bodyContent;
            // Apply separate fonts
            if (nameFont != null) itemNameText.font = nameFont;
            if (bodyFont != null) itemDescriptionText.font = bodyFont;
            // Apply font sizes if configured
            if (nameFontSize > 0f) itemNameText.fontSize = nameFontSize;
            if (bodyFontSize > 0f) itemDescriptionText.fontSize = bodyFontSize;
            // Force word wrapping so text stays inside panel
            itemNameText.enableWordWrapping = true;
            itemDescriptionText.enableWordWrapping = true;
            itemNameText.overflowMode = TMPro.TextOverflowModes.Overflow;
            itemDescriptionText.overflowMode = TMPro.TextOverflowModes.Overflow;
            // Force anchors to center so positioning math is correct
            itemNameText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            itemNameText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            itemNameText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            itemDescriptionText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            itemDescriptionText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            itemDescriptionText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // Alignment
            itemNameText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            itemDescriptionText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        }
        else
        {

            // Backward compatibility: single TMP
            if (tooltipText == null)
            {
                HideTooltip();
                return;
            }

            string tooltipContent = GetTooltipText(item, rarity);
            if (string.IsNullOrEmpty(tooltipContent))
            {
                HideTooltip();
                return;
            }
            tooltipText.text = tooltipContent;
            if (bodyFont != null) tooltipText.font = bodyFont;
        }

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
        if (tooltipBackground == null || tooltipRectTransform == null) return;

        bool hasSplitFields = itemNameText != null && itemDescriptionText != null;

        if (!hasSplitFields)
        {
            if (tooltipText == null) return;

            tooltipText.enableWordWrapping = true;
            tooltipText.ForceMeshUpdate();
            Vector2 preferredSize = tooltipText.GetPreferredValues();
            float clampedWidth = Mathf.Clamp(preferredSize.x, minWidth, maxWidth);

            // Set width constraint first, then re-measure with wrapping
            tooltipText.rectTransform.sizeDelta = new Vector2(clampedWidth, 0f);
            tooltipText.ForceMeshUpdate();
            float wrappedHeight = tooltipText.GetPreferredValues(clampedWidth, 0f).y;
            preferredSize = new Vector2(clampedWidth, wrappedHeight);

            preferredSize.x = clampedWidth;
            Vector2 newSize = preferredSize + new Vector2(paddingLeft + paddingRight, paddingTop + paddingBottom);
            tooltipRectTransform.sizeDelta = newSize;
            return;
        }

        // Split mode layout: itemNameText on top, itemDescriptionText below

        // Ensure tooltip panel anchor is center so sizeDelta = absolute size
        tooltipRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // Step 1: Get unconstrained preferred widths to determine target width
        itemNameText.enableWordWrapping = true;
        itemDescriptionText.enableWordWrapping = true;
        itemNameText.ForceMeshUpdate();
        itemDescriptionText.ForceMeshUpdate();

        Vector2 prefName = itemNameText.GetPreferredValues();
        Vector2 prefDesc = itemDescriptionText.GetPreferredValues();
        float targetWidth = Mathf.Clamp(Mathf.Max(prefName.x, prefDesc.x), minWidth, maxWidth);

        // Step 2: Set width FIRST so text wraps correctly, then re-measure height
        itemNameText.rectTransform.sizeDelta = new Vector2(targetWidth, 0f);
        itemDescriptionText.rectTransform.sizeDelta = new Vector2(targetWidth, 0f);

        itemNameText.ForceMeshUpdate();
        itemDescriptionText.ForceMeshUpdate();

        float nameHeight = itemNameText.GetPreferredValues(targetWidth, 0f).y;
        float descHeight = itemDescriptionText.GetPreferredValues(targetWidth, 0f).y;

        itemNameText.rectTransform.sizeDelta = new Vector2(targetWidth, Mathf.Max(minNameHeight, nameHeight));
        itemDescriptionText.rectTransform.sizeDelta = new Vector2(targetWidth, Mathf.Max(minDescHeight, descHeight));

        float contentHeight = Mathf.Max(minNameHeight, nameHeight) + sectionGap + Mathf.Max(minDescHeight, descHeight);
        float panelW = targetWidth + paddingLeft + paddingRight;
        float panelH = contentHeight + paddingTop + paddingBottom;
        if (maxHeight > 0f) panelH = Mathf.Min(panelH, maxHeight);
        Vector2 panelSize = new Vector2(panelW, panelH);
        tooltipRectTransform.sizeDelta = panelSize;

        // Reposition to keep text fully inside tooltip panel
        float halfH = panelSize.y * 0.5f;
        // Offset X to account for left/right padding difference
        float offsetX = (paddingLeft - paddingRight) * 0.5f;
        float nameY = halfH - paddingTop - Mathf.Max(minNameHeight, nameHeight) * 0.5f;
        float descY = nameY - Mathf.Max(minNameHeight, nameHeight) * 0.5f - sectionGap - Mathf.Max(minDescHeight, descHeight) * 0.5f;

        itemNameText.rectTransform.anchoredPosition = new Vector2(offsetX, nameY);
        itemDescriptionText.rectTransform.anchoredPosition = new Vector2(offsetX, descY);
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
        
        string levelStr = "";
        if (item.itemType == ItemType.Equipment && EquipmentManager.Instance != null)
        {
            for (int i = 0; i < 4; i++)
            {
                Item eqItem = EquipmentManager.Instance.GetEquippedItemByIndex(i);
                Rarity eqRarity = EquipmentManager.Instance.GetEquippedRarity(i);
                if (eqItem != null && eqItem.id == item.id && eqRarity == rarity)
                {
                    int level = EquipmentManager.Instance.GetEnhancementLevel(i);
                    if (level > 0)
                    {
                        levelStr = $" (+{level})";
                    }
                    break;
                }
            }
        }
        
        sb.AppendLine($"<color={rarityColor}><b>{item.itemName}{levelStr}</b></color>");
        
        if (item.itemType == ItemType.Material && item.refinementTier > 0)
        {
            sb.AppendLine($"<color=#888888>Tier {item.refinementTier}</color>");
        }
        else
        {
            sb.AppendLine($"<color=#888888>{rarity}</color>");
        }
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

    private string GetTooltipNameText(Item item, Rarity rarity)
    {
        if (item == null) return string.Empty;
        string rarityColor = GetRarityColor(rarity);
        
        string levelStr = "";
        if (item.itemType == ItemType.Equipment && EquipmentManager.Instance != null)
        {
            // Check if this specific item is currently equipped
            for (int i = 0; i < 4; i++)
            {
                Item eqItem = EquipmentManager.Instance.GetEquippedItemByIndex(i);
                Rarity eqRarity = EquipmentManager.Instance.GetEquippedRarity(i);
                // Simple match: if same ID and Rarity, assume it's the equipped one
                if (eqItem != null && eqItem.id == item.id && eqRarity == rarity)
                {
                    int level = EquipmentManager.Instance.GetEnhancementLevel(i);
                    if (level > 0)
                    {
                        levelStr = $" (+{level})";
                    }
                    break;
                }
            }
        }
        
        return $"<color={rarityColor}><b>{item.itemName}{levelStr}</b></color>";
    }

    private string GetTooltipBodyText(Item item, Rarity rarity)
    {
        if (item == null) return string.Empty;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (item.itemType == ItemType.Material && item.refinementTier > 0)
        {
            sb.AppendLine($"<color=#{Hex(colorRarity)}>Tier {item.refinementTier}</color>");
        }
        else
        {
            sb.AppendLine($"<color=#{Hex(colorRarity)}>{rarity}</color>");
        }
        sb.AppendLine();

        if (!string.IsNullOrEmpty(item.description))
        {
            sb.AppendLine($"<color=#{Hex(colorDescription)}>{item.description}</color>");
            sb.AppendLine();
        }

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
        string hSlot = Hex(colorSlotPassive);
        string hRar = Hex(colorRarity);
        
        sb.AppendLine($"<color=#{hSlot}>Slot: {item.equipmentSlot}</color>");
        sb.AppendLine();

        bool hasStats = false;
        
        // --- Calculate roll and enhancement multiplier ---
        float rollMultiplier = -1f;
        int level = 0;
        
        if (EquipmentManager.Instance != null && InventoryManager.Instance != null)
        {
            bool isEquipped = false;
            for (int i = 0; i < 4; i++)
            {
                Item eqItem = EquipmentManager.Instance.GetEquippedItemByIndex(i);
                Rarity eqRarity = EquipmentManager.Instance.GetEquippedRarity(i);
                if (eqItem != null && eqItem.id == item.id && eqRarity == rarity)
                {
                    rollMultiplier = EquipmentManager.Instance.GetEquipStatRoll(i);
                    level = EquipmentManager.Instance.GetEnhancementLevel(i);
                    isEquipped = true;
                    break;
                }
            }
            if (!isEquipped && item.HasRandomStats)
            {
                float inventoryRoll = InventoryManager.Instance.PeekNextRoll(item.id, rarity);
                if (inventoryRoll >= 0f) rollMultiplier = inventoryRoll;
            }
        }
        
        if (rollMultiplier >= 0f)
        {
            sb.AppendLine($"<color=#{Hex(colorPassiveDesc)}>⚡ Stat Roll: {rollMultiplier*100f:F0}%</color>");
        }
        
        float finalMultiplier = (rollMultiplier >= 0f ? rollMultiplier : 1f) * (1.0f + level * 0.03f);

        if (item.ScaledHPBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#{Hex(colorHP)}>HP: +{item.ScaledHPBonus(rarity)*finalMultiplier:F0}</color> <color=#{hRar}>(max {item.ScaledHPBonus(rarity):F0})</color>");
            hasStats = true;
        }
        if (item.ScaledDefenseBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#{Hex(colorDefense)}>Defense: +{item.ScaledDefenseBonus(rarity)*finalMultiplier:F0}</color> <color=#{hRar}>(max {item.ScaledDefenseBonus(rarity):F0})</color>");
            hasStats = true;
        }
        if (item.ScaledCritRateBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#{Hex(colorCrit)}>Crit Rate: +{item.ScaledCritRateBonus(rarity)*finalMultiplier * 100f:F1}%</color> <color=#{hRar}>(max {item.ScaledCritRateBonus(rarity) * 100f:F1}%)</color>");
            hasStats = true;
        }
        if (item.ScaledCritDamageMultiplier(rarity) > 1f)
        {
            sb.AppendLine($"<color=#{Hex(colorCrit)}>Crit Damage: +{(item.ScaledCritDamageMultiplier(rarity) - 1f)*finalMultiplier * 100f:F1}%</color> <color=#{hRar}>(max {(item.ScaledCritDamageMultiplier(rarity) - 1f) * 100f:F1}%)</color>");
            hasStats = true;
        }
        if (item.ScaledMovementSpeedBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#{Hex(colorMoveSpeed)}>Movement Speed: +{item.ScaledMovementSpeedBonus(rarity)*finalMultiplier * 100f:F1}%</color> <color=#{hRar}>(max {item.ScaledMovementSpeedBonus(rarity) * 100f:F1}%)</color>");
            hasStats = true;
        }
        if (item.ScaledAttackSpeedBonus(rarity) > 0f)
        {
            sb.AppendLine($"<color=#{Hex(colorAtkSpeed)}>Attack Speed: +{item.ScaledAttackSpeedBonus(rarity)*finalMultiplier * 100f:F1}%</color> <color=#{hRar}>(max {item.ScaledAttackSpeedBonus(rarity) * 100f:F1}%)</color>");
            hasStats = true;
        }

        if (!hasStats)
        {
            sb.AppendLine($"<color=#{hRar}>No stats</color>");
        }

        // Passive description
        if (!string.IsNullOrEmpty(item.passiveDescription))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#{hSlot}>Passive:</color>");
            sb.AppendLine($"<color=#{Hex(colorPassiveDesc)}>{item.passiveDescription}</color>");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get formatted gem stats
    /// </summary>
    private string GetGemStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"<color=#{Hex(colorSlotPassive)}>Type: {item.gemType}</color>");
        sb.AppendLine();
        
        string statText = item.GetGemStatText();
        if (!string.IsNullOrEmpty(statText))
        {
            sb.AppendLine($"<color=#{Hex(colorHP)}>{statText} <color=#{Hex(colorRarity)}>(max)</color></color>");
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
            sb.AppendLine($"<color=#{Hex(colorHP)}>+ 50% HP</color>");
        }
        else
        {
            sb.AppendLine($"<color=#{Hex(colorSlotPassive)}>Consumable Item</color>");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Get formatted material stats
    /// </summary>
    private string GetMaterialStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        sb.AppendLine($"<color=#{Hex(colorSlotPassive)}>Material</color>");
        
        return sb.ToString();
    }

    /// <summary>
    /// Get formatted crystal stone stats with success rates
    /// </summary>
    private string GetCrystalStoneStats(Item item)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"<color=#{Hex(colorMoveSpeed)}>Crystal Stone</color>");
        sb.AppendLine($"<color=#{Hex(colorRarity)}>Socketing material - increases success rate</color>");
        sb.AppendLine();
        sb.AppendLine($"<color=#{Hex(colorSlotPassive)}>Success Rate:</color>");

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

    /// <summary>
    /// Convert Color to hex string (RRGGBB) for TMP rich text tags
    /// </summary>
    private string Hex(Color c)
    {
        return ColorUtility.ToHtmlStringRGB(c);
    }
}

