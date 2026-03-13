using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Dungeon Reward UI — Hiển thị tổng hợp item thu được khi hoàn thành dungeon
/// Kiểu Genshin Impact domain reward screen
/// Tự tạo UI bằng code, không cần setup prefab
/// </summary>
public class DungeonRewardUI : MonoBehaviour
{
    public static DungeonRewardUI Instance { get; private set; }

    [Header("=== Settings ===")]
    [Tooltip("Kích thước mỗi ô item")]
    [SerializeField] private float slotSize = 100f;
    [Tooltip("Khoảng cách giữa các ô")]
    [SerializeField] private float slotSpacing = 10f;
    [Tooltip("Chiều rộng panel")]
    [SerializeField] private float panelWidth = 800f;
    [Tooltip("Chiều cao panel")]
    [SerializeField] private float panelHeight = 300f;

    [Header("=== Custom Assets (kéo vào Inspector) ===")]
    [Tooltip("Sprite nền panel chính (null = dùng màu mặc định)")]
    [SerializeField] private Sprite panelBackgroundSprite;
    [Tooltip("Sprite nền mỗi ô item (null = dùng màu mặc định)")]
    [SerializeField] private Sprite slotBackgroundSprite;
    [Tooltip("Sprite viền rarity mỗi ô (null = dùng màu mặc định)")]
    [SerializeField] private Sprite slotBorderSprite;
    [Tooltip("Image Type cho panel/slot sprites")]
    [SerializeField] private Image.Type spriteImageType = Image.Type.Sliced;

    // Danh sách item thu được trong dungeon
    private List<RewardEntry> collectedItems = new List<RewardEntry>();

    // UI references (tự tạo bằng code)
    private Canvas rewardCanvas;
    private GameObject panelRoot;
    private GameObject contentContainer;
    private bool isShowing = false;
    private int originalTooltipSortingOrder = 0; // lưu sortingOrder gốc

    [System.Serializable]
    public class RewardEntry
    {
        public Item item;
        public Rarity rarity;
        public int quantity;

        public RewardEntry(Item i, Rarity r, int q)
        {
            item = i;
            rarity = r;
            quantity = q;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // CHỈ xóa component — gameObject có thể là child của Canvas player
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        // FIX #10: Cleanup canvas trước khi null Instance
        if (Instance == this)
        {
            if (isShowing)
            {
                if (panelRoot != null) Destroy(panelRoot);
                if (rewardCanvas != null) Destroy(rewardCanvas.gameObject);
                isShowing = false;
            }
            Instance = null;
        }
    }

    /// <summary>
    /// Gọi khi player nhặt item trong dungeon — track lại
    /// </summary>
    public void TrackItem(Item item, Rarity rarity, int quantity)
    {
        if (item == null) return;

        // Tìm xem đã có item+rarity này chưa
        var existing = collectedItems.Find(e => e.item.id == item.id && e.rarity == rarity);
        if (existing != null)
        {
            existing.quantity += quantity;
        }
        else
        {
            collectedItems.Add(new RewardEntry(item, rarity, quantity));
        }

        Debug.Log($"[DungeonReward] Tracked: {item.itemName} [{rarity}] x{quantity} (total types: {collectedItems.Count})");
    }

    /// <summary>
    /// Xóa danh sách item (khi bắt đầu dungeon mới)
    /// </summary>
    public void ClearTrackedItems()
    {
        collectedItems.Clear();
    }

    /// <summary>
    /// Hiển thị Reward Panel
    /// </summary>
    public void ShowRewardPanel()
    {
        if (isShowing) return;

        CreateRewardUI();
        isShowing = true;

        // Raise tooltip canvas lên trên reward canvas
        RaiseTooltipCanvas(true);

        // Hiện cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Ẩn Reward Panel
    /// </summary>
    public void HideRewardPanel()
    {
        if (!isShowing) return;

        // Restore tooltip canvas sortingOrder
        RaiseTooltipCanvas(false);

        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }
        if (rewardCanvas != null)
        {
            Destroy(rewardCanvas.gameObject);
        }

        isShowing = false;
    }

    /// <summary>
    /// Tạo toàn bộ UI bằng code
    /// </summary>
    private void CreateRewardUI()
    {
        // Xóa UI cũ nếu có
        if (rewardCanvas != null) Destroy(rewardCanvas.gameObject);

        // === CANVAS ===
        GameObject canvasGO = new GameObject("DungeonRewardCanvas");
        rewardCanvas = canvasGO.AddComponent<Canvas>();
        rewardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rewardCanvas.sortingOrder = 999; // Luôn hiện trên cùng
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // === DIMMED BACKGROUND ===
        GameObject dimBG = CreateUIElement("DimBackground", canvasGO.transform);
        RectTransform dimRT = dimBG.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.sizeDelta = Vector2.zero;
        Image dimImg = dimBG.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.6f);

        // === MAIN PANEL ===
        panelRoot = CreateUIElement("RewardPanel", canvasGO.transform);
        RectTransform panelRT = panelRoot.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(panelWidth, panelHeight);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelBG = panelRoot.AddComponent<Image>();
        if (panelBackgroundSprite != null)
        {
            panelBG.sprite = panelBackgroundSprite;
            panelBG.type = spriteImageType;
            panelBG.color = Color.white;
        }
        else
        {
            panelBG.color = new Color(0.08f, 0.08f, 0.15f, 0.95f);
        }

        // === SCROLL AREA (full height, no title) ===
        GameObject scrollGO = CreateUIElement("ScrollArea", panelRoot.transform);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0.1f);
        scrollRT.anchorMax = new Vector2(1, 0.95f);
        scrollRT.sizeDelta = new Vector2(-40, 0);
        scrollRT.anchoredPosition = Vector2.zero;

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.scrollSensitivity = 30;

        // Bắt scroll chuột dọc → scroll ngang
        HorizontalScrollCapture scrollCapture = scrollGO.AddComponent<HorizontalScrollCapture>();
        scrollCapture.Setup(scrollRect);

        // Mask
        Image scrollBG = scrollGO.AddComponent<Image>();
        scrollBG.color = new Color(0, 0, 0, 0.01f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = false;

        // === CONTENT CONTAINER (horizontal) ===
        GameObject contentGO = CreateUIElement("Content", scrollGO.transform);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(0, 1);
        contentRT.pivot = new Vector2(0, 0.5f);
        contentRT.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = contentGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = slotSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRT;
        contentContainer = contentGO;

        // === HORIZONTAL SCROLLBAR ===
        GameObject scrollbarGO = CreateUIElement("HScrollbar", scrollGO.transform);
        RectTransform sbRT = scrollbarGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0, 0);
        sbRT.anchorMax = new Vector2(1, 0);
        sbRT.pivot = new Vector2(0.5f, 0);
        sbRT.sizeDelta = new Vector2(0, 12);
        sbRT.anchoredPosition = Vector2.zero;

        Image sbBG = scrollbarGO.AddComponent<Image>();
        sbBG.color = new Color(0.15f, 0.15f, 0.25f, 0.8f);

        Scrollbar scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.LeftToRight;

        // Scrollbar handle
        GameObject handleArea = CreateUIElement("HandleArea", scrollbarGO.transform);
        RectTransform haRT = handleArea.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.sizeDelta = Vector2.zero;

        GameObject handleGO = CreateUIElement("Handle", handleArea.transform);
        RectTransform hRT = handleGO.GetComponent<RectTransform>();
        hRT.anchorMin = Vector2.zero;
        hRT.anchorMax = Vector2.one;
        hRT.sizeDelta = Vector2.zero;

        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = new Color(0.5f, 0.5f, 0.6f, 0.9f);

        scrollbar.handleRect = hRT;
        scrollbar.targetGraphic = handleImg;
        scrollRect.horizontalScrollbar = scrollbar;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // === POPULATE ITEMS ===
        if (collectedItems.Count == 0)
        {
            // No items
            GameObject noItemGO = CreateUIElement("NoItems", contentGO.transform);
            TextMeshProUGUI noItemText = noItemGO.AddComponent<TextMeshProUGUI>();
            noItemText.text = "Không có vật phẩm";
            noItemText.fontSize = 24;
            noItemText.alignment = TextAlignmentOptions.Center;
            noItemText.color = Color.gray;
            noItemGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, slotSize);
        }
        else
        {
            foreach (var entry in collectedItems)
            {
                CreateItemSlot(contentGO.transform, entry);
            }
        }

        // === ITEM COUNT TEXT ===
        GameObject countGO = CreateUIElement("ItemCount", panelRoot.transform);
        RectTransform countRT = countGO.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0, 0);
        countRT.anchorMax = new Vector2(1, 0.15f);
        countRT.sizeDelta = Vector2.zero;
        countRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI countText = countGO.AddComponent<TextMeshProUGUI>();
        int totalItems = 0;
        foreach (var e in collectedItems) totalItems += e.quantity;
        countText.text = $"Tổng: {collectedItems.Count} loại, {totalItems} vật phẩm";
        countText.fontSize = 18;
        countText.alignment = TextAlignmentOptions.Center;
        countText.color = new Color(0.7f, 0.7f, 0.7f);
    }

    /// <summary>
    /// Tạo 1 ô item slot
    /// </summary>
    private void CreateItemSlot(Transform parent, RewardEntry entry)
    {
        if (entry.item == null) return;

        string rarityColor = Item.GetRarityColorHex(entry.rarity);
        Color borderColor;
        ColorUtility.TryParseHtmlString(rarityColor, out borderColor);

        // === SLOT ROOT (border) ===
        GameObject slotGO = CreateUIElement($"Slot_{entry.item.itemName}", parent);
        RectTransform slotRT = slotGO.GetComponent<RectTransform>();
        slotRT.sizeDelta = new Vector2(slotSize, slotSize + 35);

        LayoutElement le = slotGO.AddComponent<LayoutElement>();
        le.preferredWidth = slotSize;
        le.preferredHeight = slotSize + 35;

        // Border image (rarity color)
        Image borderImg = slotGO.AddComponent<Image>();
        if (slotBorderSprite != null)
        {
            borderImg.sprite = slotBorderSprite;
            borderImg.type = spriteImageType;
        }
        borderImg.color = borderColor;

        // === INNER BG ===
        GameObject innerGO = CreateUIElement("Inner", slotGO.transform);
        RectTransform innerRT = innerGO.GetComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.sizeDelta = new Vector2(-4, -4);
        innerRT.anchoredPosition = Vector2.zero;

        Image innerBG = innerGO.AddComponent<Image>();
        if (slotBackgroundSprite != null)
        {
            innerBG.sprite = slotBackgroundSprite;
            innerBG.type = spriteImageType;
            innerBG.color = Color.white;
        }
        else
        {
            innerBG.color = new Color(0.12f, 0.12f, 0.2f, 0.95f);
        }

        // === ICON ===
        if (entry.item.icon != null)
        {
            GameObject iconGO = CreateUIElement("Icon", innerGO.transform);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.25f);
            iconRT.anchorMax = new Vector2(0.9f, 0.95f);
            iconRT.sizeDelta = Vector2.zero;
            iconRT.anchoredPosition = Vector2.zero;

            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = entry.item.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false; // Không chặn hover trên slot
        }

        // === QUANTITY TEXT ===
        if (entry.quantity > 1)
        {
            GameObject qtyGO = CreateUIElement("Qty", innerGO.transform);
            RectTransform qtyRT = qtyGO.GetComponent<RectTransform>();
            qtyRT.anchorMin = new Vector2(0.5f, 0.95f);
            qtyRT.anchorMax = new Vector2(1f, 1.15f);
            qtyRT.sizeDelta = Vector2.zero;
            qtyRT.anchoredPosition = Vector2.zero;

            TextMeshProUGUI qtyText = qtyGO.AddComponent<TextMeshProUGUI>();
            qtyText.text = $"x{entry.quantity}";
            qtyText.fontSize = 16;
            qtyText.alignment = TextAlignmentOptions.Right;
            qtyText.color = Color.white;
        }

        // === NAME TEXT ===
        GameObject nameGO = CreateUIElement("Name", innerGO.transform);
        RectTransform nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 0);
        nameRT.anchorMax = new Vector2(1, 0.25f);
        nameRT.sizeDelta = Vector2.zero;
        nameRT.anchoredPosition = Vector2.zero;

        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.text = entry.item.itemName;
        nameText.fontSize = 12;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = borderColor;
        nameText.enableWordWrapping = true;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.raycastTarget = false; // Không chặn hover

        // === TOOLTIP ON HOVER ===
        RewardSlotHover hover = slotGO.AddComponent<RewardSlotHover>();
        hover.Setup(entry.item, entry.rarity);
    }

    /// <summary>
    /// Helper tạo UI element
    /// </summary>
    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>
    /// Raise/restore tooltip canvas để hiện trên reward panel
    /// </summary>
    private void RaiseTooltipCanvas(bool raise)
    {
        if (ItemTooltipManager.Instance == null) return;

        Canvas tooltipCanvas = ItemTooltipManager.Instance.GetComponentInParent<Canvas>();
        if (tooltipCanvas == null) return;

        if (raise)
        {
            originalTooltipSortingOrder = tooltipCanvas.sortingOrder;
            tooltipCanvas.overrideSorting = true;
            tooltipCanvas.sortingOrder = 1000;
        }
        else
        {
            tooltipCanvas.sortingOrder = originalTooltipSortingOrder;
        }
    }
}

/// <summary>
/// Hover tooltip cho reward slot — dùng ItemTooltipManager
/// </summary>
public class RewardSlotHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    private Item itemData;
    private Rarity itemRarity;

    public void Setup(Item item, Rarity rarity)
    {
        itemData = item;
        itemRarity = rarity;
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (itemData != null && ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.ShowTooltip(itemData, itemRarity);
        }
    }

    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (ItemTooltipManager.Instance != null)
        {
            ItemTooltipManager.Instance.HideTooltip();
        }
    }
}

/// <summary>
/// Bắt scroll chuột dọc → chuyển thành scroll ngang cho ScrollRect
/// </summary>
public class HorizontalScrollCapture : MonoBehaviour, UnityEngine.EventSystems.IScrollHandler
{
    private ScrollRect scrollRect;

    public void Setup(ScrollRect sr)
    {
        scrollRect = sr;
    }

    public void OnScroll(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (scrollRect == null) return;

        // Chuyển scroll dọc (y) → ngang
        Vector2 pos = scrollRect.normalizedPosition;
        pos.x -= eventData.scrollDelta.y * 0.05f; // tốc độ scroll
        pos.x = Mathf.Clamp01(pos.x);
        scrollRect.normalizedPosition = pos;
    }
}
