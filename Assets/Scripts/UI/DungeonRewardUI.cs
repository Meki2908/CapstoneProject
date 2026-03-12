using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Dungeon Reward Screen — kiểu Genshin Impact
/// Hiện popup khi hoàn thành dungeon, liệt kê items thu được
/// Singleton — gọi DungeonRewardUI.Instance.ShowRewards(items)
/// </summary>
public class DungeonRewardUI : MonoBehaviour
{
    public static DungeonRewardUI Instance { get; private set; }

    [Header("=== UI References ===")]
    [Tooltip("Root panel của reward screen (chứa tất cả)")]
    [SerializeField] private GameObject rewardPanel;
    [Tooltip("Title text (VD: 'Challenge Complete!')")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("Content container trong ScrollRect — nơi spawn item cards")]
    [SerializeField] private Transform itemContainer;
    [Tooltip("Prefab cho mỗi item card")]
    [SerializeField] private GameObject rewardItemPrefab;
    [Tooltip("Nút Claim / OK")]
    [SerializeField] private Button claimButton;
    [Tooltip("Overlay mờ phía sau popup")]
    [SerializeField] private Image backgroundOverlay;

    [Header("=== Animation ===")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float cardAppearDelay = 0.1f;
    [SerializeField] private float cardScaleUpDuration = 0.2f;

    [Header("=== Size Settings ===")]
    [Tooltip("Kích thước popup (width x height)")]
    [SerializeField] private Vector2 popupSize = new Vector2(800f, 400f);
    [Tooltip("Kích thước mỗi item card (width x height)")]
    [SerializeField] private Vector2 cardSize = new Vector2(120f, 150f);
    [Tooltip("Khoảng cách giữa các card")]
    [SerializeField] private float cardSpacing = 15f;
    [Tooltip("Cỡ chữ tiêu đề")]
    [SerializeField] private float titleFontSize = 32f;
    [Tooltip("Cỡ chữ tên item")]
    [SerializeField] private float itemNameFontSize = 12f;

    [Header("=== Auto Create UI ===")]
    [Tooltip("Tự tạo UI nếu chưa gắn references")]
    [SerializeField] private bool autoCreateUI = true;

    // Tracking items trong dungeon run
    private List<RewardEntry> collectedItems = new List<RewardEntry>();
    private bool isShowing = false;

    [System.Serializable]
    public class RewardEntry
    {
        public Item item;
        public int quantity;
        public Rarity rarity;

        public RewardEntry(Item i, int qty, Rarity r)
        {
            item = i;
            quantity = qty;
            rarity = r;
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
            Destroy(gameObject);
            return;
        }

        if (autoCreateUI && rewardPanel == null)
        {
            CreateUI();
        }

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimClicked);
    }

    /// <summary>
    /// Ghi nhận item thu được trong dungeon (gọi khi nhặt item)
    /// </summary>
    public void TrackItem(Item item, int quantity, Rarity rarity)
    {
        if (item == null) return;

        // Tìm entry đã tồn tại (cùng item + cùng rarity)
        var existing = collectedItems.Find(e => e.item.id == item.id && e.rarity == rarity);
        if (existing != null)
        {
            existing.quantity += quantity;
        }
        else
        {
            collectedItems.Add(new RewardEntry(item, quantity, rarity));
        }

        Debug.Log($"[DungeonReward] Tracked: {item.itemName} [{rarity}] x{quantity}");
    }

    /// <summary>
    /// Ghi nhận EXP thu được
    /// </summary>
    public void TrackEXP(int amount)
    {
        // EXP không phải Item SO — tạo entry đặc biệt
        var existing = collectedItems.Find(e => e.item == null);
        if (existing != null)
        {
            existing.quantity += amount;
        }
        else
        {
            collectedItems.Add(new RewardEntry(null, amount, Rarity.Common));
        }
    }

    /// <summary>
    /// Reset tracking (gọi khi bắt đầu dungeon mới)
    /// </summary>
    public void ResetTracking()
    {
        collectedItems.Clear();
        Debug.Log("[DungeonReward] Tracking reset");
    }

    /// <summary>
    /// Hiện reward screen với items đã track
    /// </summary>
    public void ShowRewards()
    {
        ShowRewards(collectedItems);
    }

    /// <summary>
    /// Hiện reward screen với custom item list
    /// </summary>
    public void ShowRewards(List<RewardEntry> items)
    {
        if (isShowing) return;
        isShowing = true;

        // Show panel
        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        // Pause game
        Time.timeScale = 0f;

        // Show cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Clear old items
        if (itemContainer != null)
        {
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Spawn item cards
        StartCoroutine(SpawnItemCards(items));
    }

    private IEnumerator SpawnItemCards(List<RewardEntry> items)
    {
        if (items == null || items.Count == 0)
        {
            // Hiện "No items" nếu trống
            if (titleText != null)
                titleText.text = "Challenge Complete!\nNo rewards this time.";
            yield break;
        }

        if (titleText != null)
            titleText.text = "Challenge Complete!";

        // Animate từng card xuất hiện
        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            if (rewardItemPrefab != null && itemContainer != null)
            {
                GameObject cardGO = Instantiate(rewardItemPrefab, itemContainer);
                DungeonRewardItemUI card = cardGO.GetComponent<DungeonRewardItemUI>();

                if (card != null)
                {
                    if (entry.item != null)
                    {
                        card.Setup(entry.item.itemName, entry.item.icon, entry.rarity, entry.quantity);
                    }
                    else
                    {
                        // EXP entry
                        card.Setup($"EXP +{entry.quantity}", null, Rarity.Common, entry.quantity);
                    }
                }

                // Scale animation
                cardGO.transform.localScale = Vector3.zero;
                StartCoroutine(ScaleUpCard(cardGO.transform, cardScaleUpDuration));

                // Delay trước card tiếp theo (dùng unscaled time vì game paused)
                yield return new WaitForSecondsRealtime(cardAppearDelay);
            }
        }
    }

    private IEnumerator ScaleUpCard(Transform card, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Ease out back effect (overshoot rồi bounce lại)
            float scale = EaseOutBack(t);
            if (card != null)
                card.localScale = Vector3.one * scale;
            yield return null;
        }
        if (card != null)
            card.localScale = Vector3.one;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void OnClaimClicked()
    {
        HideRewards();
    }

    public void HideRewards()
    {
        if (!isShowing) return;
        isShowing = false;

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        // Resume game
        Time.timeScale = 1f;

        // Lock cursor
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Clear tracking
        collectedItems.Clear();

        Debug.Log("[DungeonReward] Reward screen closed");
    }

    // ========================================
    // AUTO CREATE UI
    // Tạo UI hoàn chỉnh bằng code nếu chưa gắn
    // ========================================
    private void CreateUI()
    {
        // Tìm Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[DungeonRewardUI] No Canvas found!");
            return;
        }

        // === ROOT PANEL ===
        rewardPanel = new GameObject("DungeonRewardPanel");
        rewardPanel.transform.SetParent(canvas.transform, false);
        RectTransform rootRT = rewardPanel.AddComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // === BACKGROUND OVERLAY (mờ đen) ===
        GameObject overlayGO = new GameObject("Overlay");
        overlayGO.transform.SetParent(rewardPanel.transform, false);
        RectTransform overlayRT = overlayGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        backgroundOverlay = overlayGO.AddComponent<Image>();
        backgroundOverlay.color = new Color(0f, 0f, 0f, 0.75f);

        // === CENTER POPUP ===
        GameObject popupGO = new GameObject("Popup");
        popupGO.transform.SetParent(rewardPanel.transform, false);
        RectTransform popupRT = popupGO.AddComponent<RectTransform>();
        popupRT.anchorMin = new Vector2(0.5f, 0.5f);
        popupRT.anchorMax = new Vector2(0.5f, 0.5f);
        popupRT.sizeDelta = popupSize;
        popupRT.anchoredPosition = Vector2.zero;
        Image popupBg = popupGO.AddComponent<Image>();
        popupBg.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

        // Thêm Outline cho popup
        var popupOutline = popupGO.AddComponent<Outline>();
        popupOutline.effectColor = new Color(1f, 0.84f, 0f, 0.6f); // Gold border
        popupOutline.effectDistance = new Vector2(2f, 2f);

        // === TITLE ===
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(popupGO.transform, false);
        RectTransform titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.75f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = new Vector2(20f, 0f);
        titleRT.offsetMax = new Vector2(-20f, -10f);
        titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Challenge Complete!";
        titleText.fontSize = titleFontSize;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.84f, 0f); // Gold

        // === SCROLL VIEW ===
        GameObject scrollGO = new GameObject("ScrollView");
        scrollGO.transform.SetParent(popupGO.transform, false);
        RectTransform scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0f, 0.18f);
        scrollRT.anchorMax = new Vector2(1f, 0.75f);
        scrollRT.offsetMin = new Vector2(20f, 0f);
        scrollRT.offsetMax = new Vector2(-20f, 0f);
        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.3f);
        scrollGO.AddComponent<Mask>().showMaskGraphic = true;

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.scrollSensitivity = 20f;

        // === CONTENT (chứa item cards) ===
        GameObject contentGO = new GameObject("Content");
        contentGO.transform.SetParent(scrollGO.transform, false);
        RectTransform contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(0f, 1f);
        contentRT.pivot = new Vector2(0f, 0.5f);
        contentRT.offsetMin = new Vector2(10f, 10f);
        contentRT.offsetMax = new Vector2(0f, -10f);

        // Horizontal Layout Group
        HorizontalLayoutGroup hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = cardSpacing;
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        // Content Size Fitter (để scroll hoạt động)
        ContentSizeFitter csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRT;
        itemContainer = contentGO.transform;

        // === CLAIM BUTTON ===
        GameObject btnGO = new GameObject("ClaimButton");
        btnGO.transform.SetParent(popupGO.transform, false);
        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.3f, 0.02f);
        btnRT.anchorMax = new Vector2(0.7f, 0.15f);
        btnRT.offsetMin = Vector2.zero;
        btnRT.offsetMax = Vector2.zero;
        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(1f, 0.84f, 0f, 0.9f); // Gold
        claimButton = btnGO.AddComponent<Button>();
        claimButton.targetGraphic = btnBg;

        // Button hover colors
        ColorBlock cb = claimButton.colors;
        cb.normalColor = new Color(1f, 0.84f, 0f, 0.9f);
        cb.highlightedColor = new Color(1f, 0.9f, 0.3f, 1f);
        cb.pressedColor = new Color(0.8f, 0.65f, 0f, 1f);
        claimButton.colors = cb;

        // Button text
        GameObject btnTextGO = new GameObject("Text");
        btnTextGO.transform.SetParent(btnGO.transform, false);
        RectTransform btnTextRT = btnTextGO.AddComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;
        TextMeshProUGUI btnText = btnTextGO.AddComponent<TextMeshProUGUI>();
        btnText.text = "Claim All";
        btnText.fontSize = 24;
        btnText.fontStyle = FontStyles.Bold;
        btnText.alignment = TextAlignmentOptions.Center;
        btnText.color = new Color(0.1f, 0.1f, 0.15f);

        claimButton.onClick.AddListener(OnClaimClicked);

        // === TẠO REWARD ITEM PREFAB ===
        CreateRewardItemPrefab();

        rewardPanel.SetActive(false);
        Debug.Log("[DungeonRewardUI] Auto-created UI successfully!");
    }

    private void CreateRewardItemPrefab()
    {
        // Tạo prefab tại runtime
        rewardItemPrefab = new GameObject("RewardItemCard");
        rewardItemPrefab.SetActive(false); // Deactivate template

        RectTransform cardRT = rewardItemPrefab.AddComponent<RectTransform>();
        cardRT.sizeDelta = cardSize;

        // Card background
        Image cardBg = rewardItemPrefab.AddComponent<Image>();
        cardBg.color = new Color(0.2f, 0.2f, 0.28f, 0.9f);

        // Rarity border (Outline)
        Outline borderOutline = rewardItemPrefab.AddComponent<Outline>();
        borderOutline.effectColor = Color.white;
        borderOutline.effectDistance = new Vector2(2f, 2f);

        // Add DungeonRewardItemUI component
        DungeonRewardItemUI itemUI = rewardItemPrefab.AddComponent<DungeonRewardItemUI>();

        // === ICON ===
        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(rewardItemPrefab.transform, false);
        RectTransform iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.15f, 0.35f);
        iconRT.anchorMax = new Vector2(0.85f, 0.90f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        Image iconImg = iconGO.AddComponent<Image>();
        iconImg.preserveAspect = true;
        itemUI.iconImage = iconImg;

        // === QUANTITY ===
        GameObject qtyGO = new GameObject("Quantity");
        qtyGO.transform.SetParent(rewardItemPrefab.transform, false);
        RectTransform qtyRT = qtyGO.AddComponent<RectTransform>();
        qtyRT.anchorMin = new Vector2(0.5f, 0.85f);
        qtyRT.anchorMax = new Vector2(1f, 1f);
        qtyRT.offsetMin = Vector2.zero;
        qtyRT.offsetMax = new Vector2(-5f, -2f);
        TextMeshProUGUI qtyText = qtyGO.AddComponent<TextMeshProUGUI>();
        qtyText.fontSize = 16;
        qtyText.fontStyle = FontStyles.Bold;
        qtyText.alignment = TextAlignmentOptions.TopRight;
        qtyText.color = Color.white;
        itemUI.quantityText = qtyText;

        // === NAME ===
        GameObject nameGO = new GameObject("Name");
        nameGO.transform.SetParent(rewardItemPrefab.transform, false);
        RectTransform nameRT = nameGO.AddComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0f);
        nameRT.anchorMax = new Vector2(1f, 0.32f);
        nameRT.offsetMin = new Vector2(5f, 2f);
        nameRT.offsetMax = new Vector2(-5f, 0f);
        TextMeshProUGUI nameText = nameGO.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = itemNameFontSize;
        nameText.alignment = TextAlignmentOptions.Bottom;
        nameText.color = Color.white;
        nameText.enableWordWrapping = true;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        itemUI.nameText = nameText;

        // === RARITY BAR (dải màu dưới cùng) ===
        GameObject barGO = new GameObject("RarityBar");
        barGO.transform.SetParent(rewardItemPrefab.transform, false);
        RectTransform barRT = barGO.AddComponent<RectTransform>();
        barRT.anchorMin = new Vector2(0f, 0f);
        barRT.anchorMax = new Vector2(1f, 0.04f);
        barRT.offsetMin = Vector2.zero;
        barRT.offsetMax = Vector2.zero;
        Image barImg = barGO.AddComponent<Image>();
        itemUI.rarityBar = barImg;

        // Không add vào scene — giữ làm template
        rewardItemPrefab.transform.SetParent(this.transform, false);
    }
}
