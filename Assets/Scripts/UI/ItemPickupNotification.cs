using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Hệ thống thông báo nhặt item kiểu Genshin Impact
/// - Vị trí: phải dưới màn hình (giống Genshin)
/// - Style: dark glass panel, icon viền glow theo rarity, bounce animation
/// - Dùng Unity UI (Canvas/Image/TextMeshPro)
/// </summary>
public class ItemPickupNotification : MonoBehaviour
{
    // ===== SINGLETON =====
    private static ItemPickupNotification _instance;
    public static ItemPickupNotification Instance => _instance;

    // ===== SETTINGS =====
    [Header("=== Container ===")]
    [Tooltip("Parent RectTransform chứa các notification entry (tự tạo nếu null)")]
    [SerializeField] private RectTransform notificationContainer;



    [Header("=== Timing ===")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float slideInDuration = 0.35f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("=== Limits ===")]
    [SerializeField] private int maxVisibleEntries = 5;
    [SerializeField] private float delayBetweenEntries = 0.12f;

    [Header("=== SCALE TỔNG QUÁT ===")]
    [Tooltip("Kéo thanh này để phóng to/thu nhỏ TOÀN BỘ panel (1 = mặc định)")]
    [Range(0.5f, 2f)]
    [SerializeField] private float globalScale = 1f;

    [Header("=== Kích Thước Entry (trước khi nhân scale) ===")]
    [SerializeField] private float entryWidth = 360f;
    [SerializeField] private float entryHeight = 52f;
    [SerializeField] private float iconSize = 42f;
    [SerializeField] private float nameFontSize = 17f;
    [SerializeField] private float quantityFontSize = 15f;
    [SerializeField] private float entrySpacing = 5f;

    [Header("=== Vị Trí Panel ===")]
    [SerializeField] private float marginRight = 30f;
    [SerializeField] private float marginBottom = 100f;

    // Getter nhân scale
    private float EntryW => entryWidth * globalScale;
    private float EntryH => entryHeight * globalScale;
    private float IconS => iconSize * globalScale;
    private float NameFS => nameFontSize * globalScale;
    private float QtyFS => quantityFontSize * globalScale;
    private float Spacing => entrySpacing * globalScale;

    [Header("=== Rarity Colors (Genshin-style) ===")]
    [SerializeField] private Color commonColor = new Color(0.62f, 0.62f, 0.64f);       // ★1 Xám
    [SerializeField] private Color uncommonColor = new Color(0.31f, 0.78f, 0.47f);      // ★2 Xanh lá
    [SerializeField] private Color rareColor = new Color(0.33f, 0.64f, 0.95f);          // ★3 Xanh dương
    [SerializeField] private Color epicColor = new Color(0.66f, 0.41f, 0.94f);          // ★4 Tím
    [SerializeField] private Color legendaryColor = new Color(0.95f, 0.69f, 0.17f);     // ★5 Vàng cam

    [Header("=== Rarity BG Colors (icon background) ===")]
    [SerializeField] private Color commonBgColor = new Color(0.35f, 0.35f, 0.38f);
    [SerializeField] private Color uncommonBgColor = new Color(0.18f, 0.42f, 0.27f);
    [SerializeField] private Color rareBgColor = new Color(0.18f, 0.32f, 0.55f);
    [SerializeField] private Color epicBgColor = new Color(0.35f, 0.22f, 0.52f);
    [SerializeField] private Color legendaryBgColor = new Color(0.52f, 0.38f, 0.12f);

    // ===== INTERNAL =====
    private List<NotificationEntry> activeEntries = new List<NotificationEntry>();
    private Queue<ItemData> pendingQueue = new Queue<ItemData>();
    private bool isProcessing = false;
    private Sprite roundedSprite; // Runtime-generated rounded rect sprite

    private class NotificationEntry
    {
        public GameObject gameObject;     // wrapper (LayoutGroup controls this)
        public GameObject innerGO;        // actual entry visual (animation controls this)
        public CanvasGroup canvasGroup;
        public RectTransform wrapperRect; // controlled by LayoutGroup
        public RectTransform innerRect;   // animated (slide X)
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI quantityText;
        public Image iconImage;
        public Image iconBgImage;
        public Image glowImage;
        public string itemName;
        public int totalQuantity;
        public Coroutine lifetimeCoroutine;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Tạo sprite bo góc runtime
        roundedSprite = CreateRoundedRectSprite(128, 64, 16);

        // Chỉ dùng container đã gán từ Inspector
        if (notificationContainer == null)
        {
            Debug.LogError("[ItemPickupNotification] notificationContainer chưa được gán trong Inspector!");
        }

        Debug.Log($"[ItemPickupNotification] ✅ Initialized (DontDestroyOnLoad, container={notificationContainer?.name})");
    }



    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ===== PUBLIC API =====

    /// <summary>
    /// Hiển thị notification nhặt item (đầy đủ)
    /// </summary>
    public void ShowNotification(string itemName, Sprite icon, ItemRarity rarity, int quantity = 1)
    {
        ShowNotification(new ItemData(itemName, icon, rarity, quantity));
    }

    /// <summary>
    /// Hiển thị notification từ ItemData
    /// </summary>
    public void ShowNotification(ItemData itemData)
    {
        // Safety: đảm bảo host luôn active để coroutines chạy được
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        Debug.Log($"[ItemPickupNotification] ShowNotification called: {itemData.itemName} ×{itemData.quantity} [{itemData.rarity}], container={(notificationContainer != null ? notificationContainer.name : "NULL")}, gameObject active={gameObject.activeInHierarchy}");

        // Safety check
        if (notificationContainer == null)
        {
            Debug.LogError("[ItemPickupNotification] notificationContainer is NULL! Hãy gán trong Inspector.");
            return;
        }

        // Merge duplicate
        foreach (var entry in activeEntries)
        {
            if (entry.itemName == itemData.itemName && entry.gameObject != null)
            {
                entry.totalQuantity += itemData.quantity;
                entry.quantityText.text = $"×{entry.totalQuantity}";

                // Reset lifetime + bounce lại
                if (entry.lifetimeCoroutine != null) StopCoroutine(entry.lifetimeCoroutine);
                entry.lifetimeCoroutine = StartCoroutine(EntryLifetime(entry));
                StartCoroutine(BounceScale(entry));
                return;
            }
        }

        pendingQueue.Enqueue(itemData);
        if (!isProcessing) StartCoroutine(ProcessQueue());
    }

    /// <summary>
    /// Hiển thị notification từ Item ScriptableObject
    /// </summary>
    public void ShowNotification(Item item, int quantity = 1)
    {
        ItemRarity mapped;
        switch (item.rarity)
        {
            case Rarity.Common:    mapped = ItemRarity.Common; break;
            case Rarity.Uncommon:  mapped = ItemRarity.Uncommon; break;
            case Rarity.Rare:      mapped = ItemRarity.Rare; break;
            case Rarity.Epic:      mapped = ItemRarity.Epic; break;
            case Rarity.Legendary: mapped = ItemRarity.Legendary; break;
            case Rarity.Mythic:    mapped = ItemRarity.Mythic; break;
            default: mapped = ItemRarity.Common; break;
        }
        ShowNotification(item.itemName, item.icon, mapped, quantity);
    }

    /// <summary>
    /// Hiển thị notification đơn giản (không icon)
    /// </summary>
    public void ShowNotification(string itemName, int quantity = 1, ItemRarity rarity = ItemRarity.Common)
    {
        ShowNotification(itemName, null, rarity, quantity);
    }

    // ===== INTERNAL =====

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;
        while (pendingQueue.Count > 0)
        {
            while (activeEntries.Count >= maxVisibleEntries)
                yield return null;

            CreateEntry(pendingQueue.Dequeue());
            yield return new WaitForSeconds(delayBetweenEntries);
        }
        isProcessing = false;
    }

    private void CreateEntry(ItemData itemData)
    {
        Debug.Log($"[ItemPickupNotification] CreateEntry: {itemData.itemName} ×{itemData.quantity}, container={(notificationContainer != null ? notificationContainer.name : "NULL")}, roundedSprite={(roundedSprite != null ? "OK" : "NULL")}");

        // Safety: tạo lại roundedSprite nếu bị mất
        if (roundedSprite == null)
            roundedSprite = CreateRoundedRectSprite(128, 64, 16);

        // Safety: kiểm tra container
        if (notificationContainer == null)
        {
            Debug.LogError("[ItemPickupNotification] Container NULL in CreateEntry! Hãy gán trong Inspector.");
            return;
        }

        // === WRAPPER: LayoutGroup kiểm soát wrapper, animation kiểm soát inner ===
        var wrapperGO = new GameObject("NotificationWrapper");
        var wrapperRT = wrapperGO.AddComponent<RectTransform>();
        wrapperRT.SetParent(notificationContainer, false);
        wrapperRT.sizeDelta = new Vector2(EntryW, EntryH);
        var wrapperLE = wrapperGO.AddComponent<LayoutElement>();
        wrapperLE.preferredWidth = EntryW;
        wrapperLE.preferredHeight = EntryH;
        wrapperGO.transform.SetAsLastSibling();

        // === INNER: luôn tạo bằng CreateGenshinEntry (tránh prefab 3D không có RectTransform) ===
        GameObject innerGO = CreateGenshinEntry(itemData.rarity);
        innerGO.transform.SetParent(wrapperRT, false);

        // Inner stretch toàn bộ wrapper
        var innerRT = innerGO.GetComponent<RectTransform>();
        if (innerRT == null) innerRT = innerGO.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero;
        innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = Vector2.zero;
        innerRT.offsetMax = Vector2.zero;

        var entry = new NotificationEntry
        {
            gameObject = wrapperGO,
            innerGO = innerGO,
            canvasGroup = innerGO.GetComponent<CanvasGroup>() ?? innerGO.AddComponent<CanvasGroup>(),
            wrapperRect = wrapperRT,
            innerRect = innerRT,
            nameText = innerGO.transform.Find("Content/ItemName")?.GetComponent<TextMeshProUGUI>(),
            quantityText = innerGO.transform.Find("Content/Quantity")?.GetComponent<TextMeshProUGUI>(),
            iconImage = innerGO.transform.Find("IconFrame/Icon")?.GetComponent<Image>(),
            iconBgImage = innerGO.transform.Find("IconFrame/IconBG")?.GetComponent<Image>(),
            glowImage = innerGO.transform.Find("IconFrame/Glow")?.GetComponent<Image>(),
            itemName = itemData.itemName,
            totalQuantity = itemData.quantity
        };

        // Content
        if (entry.nameText != null) entry.nameText.text = itemData.itemName;
        if (entry.quantityText != null) entry.quantityText.text = $"×{itemData.quantity}";

        if (entry.iconImage != null)
        {
            if (itemData.icon != null)
            {
                entry.iconImage.sprite = itemData.icon;
                entry.iconImage.color = Color.white;
            }
            else
            {
                entry.iconImage.color = new Color(1, 1, 1, 0.4f);
            }
        }

        // Rarity colors
        Color rarityCol = GetRarityColor(itemData.rarity);
        Color rarityBg = GetRarityBgColor(itemData.rarity);
        if (entry.iconBgImage != null) entry.iconBgImage.color = rarityBg;
        if (entry.glowImage != null) entry.glowImage.color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.6f);

        activeEntries.Add(entry);
        StartCoroutine(SlideInFromRight(entry));
        entry.lifetimeCoroutine = StartCoroutine(EntryLifetime(entry));
    }

    // ===== ANIMATIONS =====

    /// <summary>
    /// Slide vào từ PHẢI + fade in (dùng innerRect — không xung đột LayoutGroup)
    /// </summary>
    private IEnumerator SlideInFromRight(NotificationEntry entry)
    {
        if (entry.innerRect == null) yield break;

        float slideOffset = 400f;
        entry.canvasGroup.alpha = 0f;
        entry.innerRect.anchoredPosition = new Vector2(slideOffset, 0);

        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            if (entry.gameObject == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / slideInDuration;

            // Ease-out cubic
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            entry.innerRect.anchoredPosition = new Vector2(Mathf.Lerp(slideOffset, 0, eased), 0);
            entry.canvasGroup.alpha = Mathf.Clamp01(t * 2.5f);

            yield return null;
        }

        entry.innerRect.anchoredPosition = Vector2.zero;
        entry.canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Bounce scale khi merge duplicate
    /// </summary>
    private IEnumerator BounceScale(NotificationEntry entry)
    {
        if (entry.innerRect == null) yield break;

        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (entry.gameObject == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.1f;
            entry.innerRect.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        entry.innerRect.localScale = Vector3.one;
    }

    private IEnumerator EntryLifetime(NotificationEntry entry)
    {
        yield return new WaitForSeconds(displayDuration);

        // Fade out + slide ra phải
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            if (entry.gameObject == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;

            entry.canvasGroup.alpha = 1f - t;
            entry.innerRect.anchoredPosition = new Vector2(Mathf.Lerp(0, 150f, t), 0);

            yield return null;
        }

        RemoveEntry(entry);
    }

    private void RemoveEntry(NotificationEntry entry)
    {
        activeEntries.Remove(entry);
        if (entry.gameObject != null) Destroy(entry.gameObject);
    }

    // ===== UI CREATION =====

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return commonColor;
            case ItemRarity.Uncommon: return uncommonColor;
            case ItemRarity.Rare: return rareColor;
            case ItemRarity.Epic: return epicColor;
            case ItemRarity.Legendary: return legendaryColor;
            case ItemRarity.Mythic: return new Color(1f, 0.2f, 0.3f); // Mythic: đỏ hồng
            default: return commonColor;
        }
    }

    private Color GetRarityBgColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return commonBgColor;
            case ItemRarity.Uncommon: return uncommonBgColor;
            case ItemRarity.Rare: return rareBgColor;
            case ItemRarity.Epic: return epicBgColor;
            case ItemRarity.Legendary: return legendaryBgColor;
            case ItemRarity.Mythic: return new Color(0.55f, 0.12f, 0.18f); // Mythic bg
            default: return commonBgColor;
        }
    }



    /// <summary>
    /// Tạo 1 entry notification — layout Genshin:
    /// ┌─────────────────────────────────────┐
    /// │ [IconFrame]  Item Name          ×3  │
    /// │  ▪ glow                              │
    /// │  ▪ bg (rarity color)                 │
    /// │  ▪ icon sprite                       │
    /// └─────────────────────────────────────┘
    /// </summary>
    private GameObject CreateGenshinEntry(ItemRarity rarity)
    {
        Color rarityCol = GetRarityColor(rarity);
        Color rarityBg = GetRarityBgColor(rarity);

        // === ROOT ===
        var root = new GameObject("NotificationEntry");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(EntryW, EntryH);

        var rootBg = root.AddComponent<Image>();
        rootBg.sprite = roundedSprite;
        rootBg.type = Image.Type.Sliced;
        rootBg.color = new Color(0.06f, 0.06f, 0.10f, 0.88f); // dark glass

        // CanvasGroup cho fade animation
        root.AddComponent<CanvasGroup>();

        var rootLayout = root.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(5, 14, 5, 5);
        rootLayout.spacing = 10;
        rootLayout.childAlignment = TextAnchor.MiddleLeft;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = false;
        rootLayout.childControlWidth = false;
        rootLayout.childControlHeight = false;

        // === ICON FRAME (parent for glow + bg + icon) ===
        var iconFrame = new GameObject("IconFrame");
        var iconFrameRT = iconFrame.AddComponent<RectTransform>();
        iconFrameRT.SetParent(rootRT, false);
        iconFrameRT.sizeDelta = new Vector2(IconS, IconS);
        var iconFrameLE = iconFrame.AddComponent<LayoutElement>();
        iconFrameLE.preferredWidth = IconS;
        iconFrameLE.preferredHeight = IconS;

        // Glow — viền sáng nhẹ xung quanh icon (rarity color)
        var glowGO = new GameObject("Glow");
        var glowRT = glowGO.AddComponent<RectTransform>();
        glowRT.SetParent(iconFrameRT, false);
        glowRT.anchorMin = Vector2.zero;
        glowRT.anchorMax = Vector2.one;
        glowRT.offsetMin = new Vector2(-3, -3);
        glowRT.offsetMax = new Vector2(3, 3);
        var glowImg = glowGO.AddComponent<Image>();
        glowImg.sprite = roundedSprite;
        glowImg.type = Image.Type.Sliced;
        glowImg.color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.55f);

        // Icon BG — nền gradient theo rarity
        var iconBgGO = new GameObject("IconBG");
        var iconBgRT = iconBgGO.AddComponent<RectTransform>();
        iconBgRT.SetParent(iconFrameRT, false);
        iconBgRT.anchorMin = Vector2.zero;
        iconBgRT.anchorMax = Vector2.one;
        iconBgRT.offsetMin = Vector2.zero;
        iconBgRT.offsetMax = Vector2.zero;
        var iconBgImg = iconBgGO.AddComponent<Image>();
        iconBgImg.sprite = roundedSprite;
        iconBgImg.type = Image.Type.Sliced;
        iconBgImg.color = rarityBg;

        // Icon Sprite
        var iconGO = new GameObject("Icon");
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.SetParent(iconFrameRT, false);
        iconRT.anchorMin = new Vector2(0.1f, 0.1f);
        iconRT.anchorMax = new Vector2(0.9f, 0.9f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color = new Color(1, 1, 1, 0.4f);

        // === CONTENT (name + quantity) ===
        var content = new GameObject("Content");
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.SetParent(rootRT, false);
        contentRT.sizeDelta = new Vector2(280, 42);
        var contentLE = content.AddComponent<LayoutElement>();
        contentLE.flexibleWidth = 1;
        contentLE.preferredHeight = EntryH - 10;

        var contentLayout = content.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 4;
        contentLayout.childAlignment = TextAnchor.MiddleLeft;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = true;
        contentLayout.childControlWidth = false;
        contentLayout.childControlHeight = true;

        // Item Name
        var nameGO = new GameObject("ItemName");
        nameGO.AddComponent<RectTransform>().SetParent(contentRT, false);
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = "Item";
        nameTMP.fontSize = NameFS;
        nameTMP.fontStyle = FontStyles.Normal;
        nameTMP.color = new Color(0.92f, 0.92f, 0.90f);
        nameTMP.alignment = TextAlignmentOptions.MidlineLeft;
        nameTMP.enableWordWrapping = false;
        nameTMP.overflowMode = TextOverflowModes.Ellipsis;
        var nameLE = nameGO.AddComponent<LayoutElement>();
        nameLE.flexibleWidth = 1;
        nameLE.preferredHeight = EntryH - 10;

        // Quantity
        var qtyGO = new GameObject("Quantity");
        qtyGO.AddComponent<RectTransform>().SetParent(contentRT, false);
        var qtyTMP = qtyGO.AddComponent<TextMeshProUGUI>();
        qtyTMP.text = "×1";
        qtyTMP.fontSize = QtyFS;
        qtyTMP.fontStyle = FontStyles.Normal;
        qtyTMP.color = new Color(0.85f, 0.85f, 0.65f);
        qtyTMP.alignment = TextAlignmentOptions.MidlineRight;
        qtyTMP.enableWordWrapping = false;
        var qtyLE = qtyGO.AddComponent<LayoutElement>();
        qtyLE.preferredWidth = 48;
        qtyLE.preferredHeight = EntryH - 10;

        return root;
    }

    // ===== SPRITE GENERATION =====

    /// <summary>
    /// Tạo sprite bo góc runtime (không cần import ảnh)
    /// </summary>
    private Sprite CreateRoundedRectSprite(int width, int height, int radius)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        var pixels = new Color32[width * height];
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Kiểm tra 4 góc
                bool inCorner = false;
                Vector2 cornerCenter = Vector2.zero;

                if (x < radius && y < radius) { inCorner = true; cornerCenter = new Vector2(radius, radius); }
                else if (x >= width - radius && y < radius) { inCorner = true; cornerCenter = new Vector2(width - radius - 1, radius); }
                else if (x < radius && y >= height - radius) { inCorner = true; cornerCenter = new Vector2(radius, height - radius - 1); }
                else if (x >= width - radius && y >= height - radius) { inCorner = true; cornerCenter = new Vector2(width - radius - 1, height - radius - 1); }

                if (inCorner)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), cornerCenter);
                    if (dist > radius + 0.5f)
                        pixels[y * width + x] = clear;
                    else if (dist > radius - 0.5f)
                    {
                        byte alpha = (byte)(255 * (1f - (dist - (radius - 0.5f))));
                        pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                    }
                    else
                        pixels[y * width + x] = white;
                }
                else
                {
                    pixels[y * width + x] = white;
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        // Border cho 9-slice: [left, bottom, right, top]
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100,
            0, SpriteMeshType.FullRect, new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
    }
}
