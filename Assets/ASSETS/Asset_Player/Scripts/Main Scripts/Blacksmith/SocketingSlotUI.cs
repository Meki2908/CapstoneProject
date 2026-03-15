using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component cho 1 ô gem trong Blacksmith GUI.
/// Hiện icon gem đã gắn hoặc trạng thái trống.
/// Hỗ trợ click chọn + double-click tháo gem.
/// </summary>
public class SocketingSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image gemIcon;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image highlightBorder;
    [SerializeField] private TextMeshProUGUI slotLabel;

    [Header("Visual Settings")]
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private Color filledColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.84f, 0f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
    [SerializeField] private Sprite emptySlotSprite;

    // Events
    public event Action OnSlotClicked;
    public event Action OnSlotDoubleClicked;

    // State
    private Item currentGem;
    private bool isSelected;
    private float lastClickTime;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

    void Awake()
    {
        // Auto-wire references if not assigned in Inspector
        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        if (gemIcon == null)
        {
            var iconTf = transform.Find("GemIcon");
            if (iconTf != null) gemIcon = iconTf.GetComponent<Image>();
        }

        if (highlightBorder == null)
        {
            var borderTf = transform.Find("HighlightBorder");
            if (borderTf != null) highlightBorder = borderTf.GetComponent<Image>();
        }

        if (slotLabel == null)
        {
            var labelTf = transform.Find("Label");
            if (labelTf != null) slotLabel = labelTf.GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// Cập nhật hiển thị gem slot
    /// </summary>
    public void SetGem(Item gem, bool selected = false)
    {
        currentGem = gem;
        isSelected = selected;

        if (gem != null)
        {
            // Có gem
            if (gemIcon)
            {
                gemIcon.sprite = gem.icon;
                gemIcon.enabled = true;
                gemIcon.color = Color.white;
            }
            if (slotBackground)
            {
                string colorHex = Item.GetRarityColorHex(gem.rarity);
                Color rarityColor;
                ColorUtility.TryParseHtmlString(colorHex, out rarityColor);
                slotBackground.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
            }
            if (slotLabel) slotLabel.text = gem.itemName;
        }
        else
        {
            // Trống
            if (gemIcon)
            {
                if (emptySlotSprite)
                    gemIcon.sprite = emptySlotSprite;
                gemIcon.enabled = emptySlotSprite != null;
                gemIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            if (slotBackground) slotBackground.color = emptyColor;
            if (slotLabel) slotLabel.text = "Trống";
        }

        // Highlight border
        if (highlightBorder)
        {
            highlightBorder.enabled = selected;
            highlightBorder.color = selectedColor;
        }
    }

    // ─── Pointer Events ──────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        float timeSinceLastClick = Time.unscaledTime - lastClickTime;
        lastClickTime = Time.unscaledTime;

        if (timeSinceLastClick < DOUBLE_CLICK_THRESHOLD)
        {
            // Double click → tháo gem
            OnSlotDoubleClicked?.Invoke();
        }
        else
        {
            // Single click → chọn slot
            OnSlotClicked?.Invoke();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotBackground && !isSelected)
            slotBackground.color = hoverColor;

        // Show tooltip if gem exists
        if (currentGem != null && ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(currentGem);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Restore color
        if (!isSelected)
        {
            if (currentGem != null)
            {
                if (slotBackground) slotBackground.color = filledColor;
            }
            else
            {
                if (slotBackground) slotBackground.color = emptyColor;
            }
        }

        if (ItemTooltipManager.Instance != null)
            ItemTooltipManager.Instance.HideTooltip();
    }

    /// <summary>
    /// Get current gem in this slot
    /// </summary>
    public Item GetCurrentGem() => currentGem;

    /// <summary>
    /// Whether slot is empty
    /// </summary>
    public bool IsEmpty() => currentGem == null;
}
