using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UI component cho 1 ô gem trong Blacksmith GUI.
/// Hiện icon gem đã gắn hoặc trạng thái trống.
/// Hỗ trợ click chọn + double-click tháo gem.
/// Uses Button component for reliable click detection.
/// </summary>
[RequireComponent(typeof(Button))]
public class SocketingSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
    private Button slotButton;

    void Awake()
    {
        // ── Auto-find references if not assigned in Inspector ──
        if (gemIcon == null)
            gemIcon = transform.Find("GemIcon")?.GetComponent<Image>()
                   ?? transform.Find("Gem Icon")?.GetComponent<Image>()
                   ?? transform.Find("Icon")?.GetComponent<Image>();

        if (slotBackground == null)
            slotBackground = GetComponent<Image>(); // Usually on the same GO

        if (slotLabel == null)
            slotLabel = GetComponentInChildren<TextMeshProUGUI>();

        if (highlightBorder == null)
        {
            var border = transform.Find("HighlightBorder")?.GetComponent<Image>()
                      ?? transform.Find("Highlight")?.GetComponent<Image>()
                      ?? transform.Find("Border")?.GetComponent<Image>();
            highlightBorder = border;
        }

        // If gemIcon STILL not found, search ALL child Images (skip slotBackground)
        if (gemIcon == null)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
            {
                if (img != slotBackground && img != highlightBorder && img.gameObject != this.gameObject)
                {
                    gemIcon = img;
                    Debug.Log($"[SocketingSlotUI] Auto-found gemIcon: {img.gameObject.name}");
                    break;
                }
            }
        }

        Debug.Log($"[SocketingSlotUI] Awake on {gameObject.name}: gemIcon={gemIcon != null}, slotBg={slotBackground != null}, label={slotLabel != null}, border={highlightBorder != null}");

        // ── Ensure this GO has a Graphic for the Button to use as raycast target ──
        var graphic = GetComponent<Graphic>();
        if (graphic == null)
        {
            // Add a transparent Image so Button can receive clicks
            var img2 = gameObject.AddComponent<Image>();
            img2.color = new Color(0, 0, 0, 0.01f); // Nearly invisible but raycastable
            img2.raycastTarget = true;
        }
        else
        {
            graphic.raycastTarget = true;
        }

        // ── Disable raycastTarget on CHILDREN so clicks go to THIS GO ──
        if (gemIcon != null) gemIcon.raycastTarget = false;
        if (highlightBorder != null) highlightBorder.raycastTarget = false;
        // Keep slotBackground raycast OFF too — we want all clicks on this GO
        if (slotBackground != null && slotBackground.gameObject != this.gameObject)
            slotBackground.raycastTarget = false;

        // ── Setup Button component ──
        slotButton = GetComponent<Button>();
        if (slotButton == null) slotButton = gameObject.AddComponent<Button>();
        slotButton.transition = Selectable.Transition.None; // No visual transition
        slotButton.onClick.AddListener(HandleClick);
    }

    void HandleClick()
    {
        float timeSinceLastClick = Time.unscaledTime - lastClickTime;
        lastClickTime = Time.unscaledTime;

        if (timeSinceLastClick < DOUBLE_CLICK_THRESHOLD)
        {
            // Double click → tháo gem
            Debug.Log($"[SocketingSlotUI] Double-click on {gameObject.name}");
            OnSlotDoubleClicked?.Invoke();
        }
        else
        {
            // Single click → chọn slot
            Debug.Log($"[SocketingSlotUI] Click on {gameObject.name}");
            OnSlotClicked?.Invoke();
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
            Debug.Log($"[SocketingSlotUI] SetGem({gem.itemName}) on {gameObject.name}: gemIcon={(gemIcon != null ? gemIcon.gameObject.name : "NULL!")}, label={slotLabel != null}");

            // Có gem — ensure icon is visible
            if (gemIcon)
            {
                gemIcon.gameObject.SetActive(true);
                gemIcon.sprite = gem.icon;
                gemIcon.type = Image.Type.Simple;
                gemIcon.enabled = true;
                gemIcon.color = Color.white;
                // Reset sizeDelta so GemIcon follows its anchors (0.1→0.9 = 80% of slot)
                gemIcon.rectTransform.sizeDelta = Vector2.zero;
                gemIcon.rectTransform.anchoredPosition = Vector2.zero;
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
            if (slotLabel) slotLabel.text = "Empty";
        }

        // Highlight: use Outline component (always works, no child dependency)
        var outline = GetComponent<Outline>();
        if (selected)
        {
            if (outline == null) outline = gameObject.AddComponent<Outline>();
            outline.effectColor = selectedColor;
            outline.effectDistance = new Vector2(4, 4);
            outline.enabled = true;
        }
        else
        {
            if (outline != null) outline.enabled = false;
        }
    }

    // ─── Pointer Events (hover only) ────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotBackground && !isSelected)
            slotBackground.color = hoverColor;
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
