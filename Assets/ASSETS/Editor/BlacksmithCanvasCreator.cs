using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor tool: "Tools → Create Blacksmith Canvas"
/// Tạo toàn bộ cây hierarchy UI giống hệt BlacksmithUIBuilder.BuildUI()
/// trong Scene (edit mode) để có thể kéo thả chỉnh sửa.
/// Tự động gán tất cả references vào BlacksmithUI component.
/// </summary>
public class BlacksmithCanvasCreator : Editor
{
    // ─── Color Palette (giống hệt BlacksmithUIBuilder) ──────────
    private static readonly Color BG_DARK = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private static readonly Color PANEL_BG = new Color(0.12f, 0.12f, 0.18f, 0.9f);
    private static readonly Color HEADER_BG = new Color(0.15f, 0.12f, 0.08f, 0.95f);
    private static readonly Color SLOT_EMPTY = new Color(0.18f, 0.18f, 0.25f, 0.8f);
    private static readonly Color SLOT_BORDER = new Color(0.4f, 0.35f, 0.25f, 0.8f);
    private static readonly Color BTN_NORMAL = new Color(0.25f, 0.22f, 0.18f, 0.9f);
    private static readonly Color BTN_HIGHLIGHT = new Color(0.35f, 0.30f, 0.22f, 1f);
    private static readonly Color GOLD = new Color(1f, 0.84f, 0f);
    private static readonly Color SUCCESS_GREEN = new Color(0.2f, 0.9f, 0.3f);
    private static readonly Color TEXT_WHITE = new Color(0.9f, 0.9f, 0.9f);
    private static readonly Color TEXT_DIM = new Color(0.6f, 0.6f, 0.6f);
    private static readonly Color CRYSTAL_BG = new Color(0.15f, 0.1f, 0.25f, 0.8f);

    private static TMP_FontAsset _font;

    [MenuItem("Tools/Create Blacksmith Canvas")]
    public static void CreateBlacksmithCanvas()
    {
        // Find font
        _font = TMP_Settings.defaultFontAsset;
        if (_font == null)
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // ================================================================
        // ROOT CANVAS
        // ================================================================
        GameObject canvasGO = new GameObject("Canvas_Blacksmith");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Blacksmith Canvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Add BlacksmithUI component
        var blacksmithUI = canvasGO.AddComponent<BlacksmithUI>();

        // ================================================================
        // MAIN PANEL (full-screen overlay)
        // ================================================================
        GameObject mainPanel = CreatePanel(canvasGO.transform, "BlacksmithMainPanel", Vector2.zero, Vector2.zero,
            Vector2.zero, Vector2.one, BG_DARK);
        var mainRect = mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;

        // ================================================================
        // CENTER CONTAINER (fixed size 900×750)
        // ================================================================
        GameObject centerContainer = CreatePanel(mainPanel.transform, "CenterContainer",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, PANEL_BG);
        SetSize(centerContainer, 900, 750);
        AddOutline(centerContainer, SLOT_BORDER, 2);

        // ================================================================
        // HEADER BAR (top 50px)
        // ================================================================
        GameObject header = CreatePanel(centerContainer.transform, "Header",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), Vector2.zero, HEADER_BG);
        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(15, 10, 8, 8);
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childForceExpandWidth = true;

        // Title
        TextMeshProUGUI titleText = CreateText(header.transform, "Title", "NPC THO REN", 22, GOLD, TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold;

        // Close button [X]
        Button closeButton = CreateButton(header.transform, "CloseBtn", "X", 24, 40, 34,
            new Color(0.7f, 0.2f, 0.2f, 0.9f), Color.white);

        // ================================================================
        // BODY (below header)
        // ================================================================
        GameObject body = CreatePanel(centerContainer.transform, "Body",
            Vector2.zero, new Vector2(1, 1), Vector2.zero, new Vector2(0, -50), new Color(0, 0, 0, 0));

        // ─── LEFT SIDEBAR: Tabs (width 140) ─────────────────────
        GameObject sidebar = CreatePanel(body.transform, "Sidebar",
            new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(140, 0),
            new Color(0.1f, 0.1f, 0.15f, 0.9f));

        var sidebarLayout = sidebar.AddComponent<VerticalLayoutGroup>();
        sidebarLayout.padding = new RectOffset(8, 8, 15, 15);
        sidebarLayout.spacing = 10;
        sidebarLayout.childAlignment = TextAnchor.UpperCenter;
        sidebarLayout.childForceExpandWidth = true;
        sidebarLayout.childForceExpandHeight = false;

        Button weaponTabButton = CreateButton(sidebar.transform, "WeaponTabBtn", "VU KHI", 16, 120, 45,
            GOLD, Color.black);
        Button equipmentTabButton = CreateButton(sidebar.transform, "EquipTabBtn", "TRANG BI", 16, 120, 45,
            BTN_NORMAL, TEXT_WHITE);

        // ─── RIGHT CONTENT AREA (right of sidebar) ──────────────
        GameObject content = CreatePanel(body.transform, "ContentArea",
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(140, 0), Vector2.zero,
            new Color(0, 0, 0, 0));

        // ================================================================
        // WEAPON TAB PANEL
        // ================================================================
        GameObject weaponTabPanel = CreatePanel(content.transform, "WeaponTabPanel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));

        var weaponLayout = weaponTabPanel.AddComponent<VerticalLayoutGroup>();
        weaponLayout.padding = new RectOffset(15, 15, 10, 10);
        weaponLayout.spacing = 8;
        weaponLayout.childAlignment = TextAnchor.UpperCenter;
        weaponLayout.childForceExpandWidth = true;
        weaponLayout.childForceExpandHeight = false;

        // ── Weapon Info Row ──
        GameObject weaponInfoRow = CreateLayoutRow(weaponTabPanel.transform, "WeaponInfoRow", 70);
        Image weaponIcon = CreateImageSlot(weaponInfoRow.transform, "WeaponIcon", 60, 60, SLOT_EMPTY);
        TextMeshProUGUI weaponNameText = CreateText(weaponInfoRow.transform, "WeaponName", "Vũ khí hiện tại", 18, TEXT_WHITE, TextAlignmentOptions.Left);
        weaponNameText.fontStyle = FontStyles.Bold;
        var weaponNameLE = weaponNameText.gameObject.AddComponent<LayoutElement>();
        weaponNameLE.flexibleWidth = 1;

        // ── Weapon Gem Slots Row ──
        GameObject weaponGemRow = CreateLayoutRow(weaponTabPanel.transform, "WeaponGemRow", 105);
        var wgResult = CreateGemSlotsRow(weaponGemRow.transform, "WeaponGemSlots", 3);
        Transform weaponGemSlotsParent = wgResult.parent;
        Button[] weaponRemoveBtns = wgResult.removeButtons;

        // ── Weapon Crystal Slot ──
        GameObject weaponCrystalRow = CreateLayoutRow(weaponTabPanel.transform, "WeaponCrystalRow", 65);
        var wcResult = CreateCrystalSlot(weaponCrystalRow.transform, "WeaponCrystal");

        // ================================================================
        // EQUIPMENT TAB PANEL
        // ================================================================
        GameObject equipmentTabPanel = CreatePanel(content.transform, "EquipmentTabPanel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));

        var equipLayout = equipmentTabPanel.AddComponent<VerticalLayoutGroup>();
        equipLayout.padding = new RectOffset(15, 15, 10, 10);
        equipLayout.spacing = 8;
        equipLayout.childAlignment = TextAnchor.UpperCenter;
        equipLayout.childForceExpandWidth = true;
        equipLayout.childForceExpandHeight = false;

        // ── Equipment Selection Row (4 slots) ──
        GameObject equipSelectRow = CreateLayoutRow(equipmentTabPanel.transform, "EquipSelectRow", 70);
        string[] slotLabels = { "Head", "Body", "Legs", "Acc" };
        Button[] equipSlotButtons = new Button[4];
        Image[] equipSlotIcons = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject slotGO = CreatePanel(equipSelectRow.transform, $"EquipSlot_{i}",
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, SLOT_EMPTY);
            var slotLE = slotGO.AddComponent<LayoutElement>();
            slotLE.preferredWidth = 60;
            slotLE.preferredHeight = 60;
            AddOutline(slotGO, SLOT_BORDER, 1);

            equipSlotButtons[i] = slotGO.AddComponent<Button>();
            SetButtonColors(equipSlotButtons[i], SLOT_EMPTY, BTN_HIGHLIGHT);

            equipSlotIcons[i] = slotGO.GetComponent<Image>();

            TextMeshProUGUI slotLabel = CreateText(slotGO.transform, "Label", slotLabels[i], 10, TEXT_DIM, TextAlignmentOptions.Bottom);
            var labelRect = slotLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        // ── Equipment Name ──
        TextMeshProUGUI equipmentNameText = CreateText(equipmentTabPanel.transform, "EquipName", "Chọn slot trang bị", 16, TEXT_WHITE, TextAlignmentOptions.Center);
        var eqNameLE = equipmentNameText.gameObject.AddComponent<LayoutElement>();
        eqNameLE.preferredHeight = 30;

        // ── Equipment Gem Slots Row ──
        GameObject equipGemRow = CreateLayoutRow(equipmentTabPanel.transform, "EquipGemRow", 105);
        var egResult = CreateGemSlotsRow(equipGemRow.transform, "EquipGemSlots", 1);
        Transform equipmentGemSlotsParent = egResult.parent;
        Button[] equipRemoveBtns = egResult.removeButtons;

        // ── Equipment Crystal Slot ──
        GameObject equipCrystalRow = CreateLayoutRow(equipmentTabPanel.transform, "EquipCrystalRow", 65);
        var ecResult = CreateCrystalSlot(equipCrystalRow.transform, "EquipCrystal");

        // ================================================================
        // SUCCESS RATE BAR (shared, at bottom of content)
        // ================================================================
        GameObject successArea = CreatePanel(content.transform, "SuccessArea",
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(15, 260), new Vector2(-15, 390),
            new Color(0, 0, 0, 0));

        var successLayout = successArea.AddComponent<VerticalLayoutGroup>();
        successLayout.padding = new RectOffset(0, 0, 0, 0);
        successLayout.spacing = 8;
        successLayout.childAlignment = TextAnchor.MiddleCenter;
        successLayout.childForceExpandWidth = true;
        successLayout.childForceExpandHeight = false;

        // ── Success Rate Text ──
        TextMeshProUGUI successRateText = CreateText(successArea.transform, "SuccessRateText",
            "Tỉ lệ thành công: 0%", 15, TEXT_WHITE, TextAlignmentOptions.Center);
        var srTextLE = successRateText.gameObject.AddComponent<LayoutElement>();
        srTextLE.preferredHeight = 28;

        // ── Success Rate Bar ──
        GameObject barBG = CreatePanel(successArea.transform, "BarBG",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
            new Color(0.15f, 0.15f, 0.2f, 0.9f));
        var barBGLE = barBG.AddComponent<LayoutElement>();
        barBGLE.preferredHeight = 20;
        AddOutline(barBG, SLOT_BORDER, 1);

        GameObject barFill = CreatePanel(barBG.transform, "BarFill",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, SUCCESS_GREEN);
        var barFillRect = barFill.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
        Image successRateBar = barFill.GetComponent<Image>();
        successRateBar.type = Image.Type.Filled;
        successRateBar.fillMethod = Image.FillMethod.Horizontal;
        successRateBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        successRateBar.fillAmount = 0f;

        // ── Socket Button ──
        Button socketButton = CreateButton(successArea.transform, "SocketBtn", "KHAM GEM", 18, 250, 45,
            new Color(0.6f, 0.4f, 0.1f, 0.9f), Color.white);
        TextMeshProUGUI socketButtonText = socketButton.GetComponentInChildren<TextMeshProUGUI>();
        socketButton.GetComponent<LayoutElement>().preferredHeight = 45;

        // ── Gem Drop Slot (shared, below socket button, center) ──
        var gemDropResult = CreateGemDropSlot(successArea.transform, "GemDrop");

        // ================================================================
        // RESULT PANEL (overlay, centered)
        // ================================================================
        GameObject resultPanel = CreatePanel(centerContainer.transform, "ResultPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            new Color(0.05f, 0.05f, 0.1f, 0.92f));
        SetSize(resultPanel, 350, 120);
        AddOutline(resultPanel, GOLD, 2);
        resultPanel.SetActive(false);

        var resultLayout = resultPanel.AddComponent<VerticalLayoutGroup>();
        resultLayout.padding = new RectOffset(15, 15, 15, 15);
        resultLayout.spacing = 10;
        resultLayout.childAlignment = TextAnchor.MiddleCenter;

        Image resultIcon = CreateImageSlot(resultPanel.transform, "ResultIcon", 40, 40, new Color(0, 0, 0, 0));
        TextMeshProUGUI resultText = CreateText(resultPanel.transform, "ResultText", "", 20, SUCCESS_GREEN, TextAlignmentOptions.Center);
        resultText.fontStyle = FontStyles.Bold;

        // ================================================================
        // INVENTORY SCROLL VIEW (bottom section of content)
        // ================================================================
        GameObject scrollArea = CreatePanel(content.transform, "InventoryScrollArea",
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(15, 10), new Vector2(-15, 255),
            new Color(0, 0, 0, 0));

        // Label
        GameObject scrollLabelGO = CreatePanel(scrollArea.transform, "ScrollLabel",
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -22), Vector2.zero, new Color(0, 0, 0, 0));
        TextMeshProUGUI scrollLabel = CreateText(scrollLabelGO.transform, "Label", "HANH TRANG", 13, GOLD, TextAlignmentOptions.Left);
        scrollLabel.fontStyle = FontStyles.Bold;
        var scrollLabelRect = scrollLabel.GetComponent<RectTransform>();
        scrollLabelRect.anchorMin = Vector2.zero;
        scrollLabelRect.anchorMax = Vector2.one;
        scrollLabelRect.offsetMin = new Vector2(5, 0);
        scrollLabelRect.offsetMax = Vector2.zero;

        // Scroll View
        GameObject scrollView = CreateScrollView(scrollArea.transform, "InventoryScroll",
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -24));
        Transform inventoryContent = scrollView.transform.Find("Viewport/Content");

        // ================================================================
        // ASSIGN REFERENCES via SerializedObject
        // ================================================================
        var so = new SerializedObject(blacksmithUI);

        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("closeButton").objectReferenceValue = closeButton;
        so.FindProperty("weaponTabButton").objectReferenceValue = weaponTabButton;
        so.FindProperty("equipmentTabButton").objectReferenceValue = equipmentTabButton;
        so.FindProperty("weaponTabPanel").objectReferenceValue = weaponTabPanel;
        so.FindProperty("equipmentTabPanel").objectReferenceValue = equipmentTabPanel;
        so.FindProperty("weaponIcon").objectReferenceValue = weaponIcon;
        so.FindProperty("weaponNameText").objectReferenceValue = weaponNameText;
        so.FindProperty("weaponGemSlotsParent").objectReferenceValue = weaponGemSlotsParent;
        so.FindProperty("weaponCrystalSlotIcon").objectReferenceValue = wcResult.icon;
        so.FindProperty("weaponCrystalSlotText").objectReferenceValue = wcResult.text;
        so.FindProperty("weaponCrystalClearButton").objectReferenceValue = wcResult.clearBtn;

        // Equipment slot buttons & icons (arrays)
        var equipBtnsProp = so.FindProperty("equipmentSlotButtons");
        equipBtnsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
            equipBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = equipSlotButtons[i];

        var equipIconsProp = so.FindProperty("equipmentSlotIcons");
        equipIconsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
            equipIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = equipSlotIcons[i];

        so.FindProperty("equipmentNameText").objectReferenceValue = equipmentNameText;
        so.FindProperty("equipmentGemSlotsParent").objectReferenceValue = equipmentGemSlotsParent;
        so.FindProperty("equipmentCrystalSlotIcon").objectReferenceValue = ecResult.icon;
        so.FindProperty("equipmentCrystalSlotText").objectReferenceValue = ecResult.text;
        so.FindProperty("equipmentCrystalClearButton").objectReferenceValue = ecResult.clearBtn;

        so.FindProperty("gemDropIcon").objectReferenceValue = gemDropResult.icon;
        so.FindProperty("gemDropText").objectReferenceValue = gemDropResult.text;

        so.FindProperty("successRateBar").objectReferenceValue = successRateBar;
        so.FindProperty("successRateText").objectReferenceValue = successRateText;
        so.FindProperty("socketButton").objectReferenceValue = socketButton;
        so.FindProperty("socketButtonText").objectReferenceValue = socketButtonText;

        so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        so.FindProperty("resultText").objectReferenceValue = resultText;
        so.FindProperty("resultIcon").objectReferenceValue = resultIcon;

        so.FindProperty("inventoryContent").objectReferenceValue = inventoryContent;

        // Try to find ItemUIPrefab from existing InventoryController in scene or project
        var invCtrl = FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
        if (invCtrl != null)
        {
            var invSO = new SerializedObject(invCtrl);
            var itemUIPrefabProp = invSO.FindProperty("ItemUIPrefab");
            if (itemUIPrefabProp == null)
                itemUIPrefabProp = invSO.FindProperty("itemUIPrefab");
            if (itemUIPrefabProp != null && itemUIPrefabProp.objectReferenceValue != null)
                so.FindProperty("itemUIPrefab").objectReferenceValue = itemUIPrefabProp.objectReferenceValue;
        }

        so.ApplyModifiedProperties();

        // Hide at start
        mainPanel.SetActive(false);

        // Select in hierarchy
        Selection.activeGameObject = canvasGO;

        Debug.Log("[BlacksmithCanvasCreator] ✅ Canvas_Blacksmith created successfully in Scene! " +
                  "All references assigned to BlacksmithUI. " +
                  "You can now drag it to your Prefabs folder.");
    }

    // ================================================================
    // HELPER METHODS (giống hệt BlacksmithUIBuilder)
    // ================================================================

    static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var img = go.GetComponent<Image>();
        img.color = color;
        return go;
    }

    static void SetSize(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
    }

    static void AddOutline(GameObject go, Color color, float dist)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(dist, dist);
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (_font != null) tmp.font = _font;
        return tmp;
    }

    static Button CreateButton(Transform parent, string name, string label, float fontSize, float w, float h,
        Color bgColor, Color textColor)
    {
        GameObject go = CreatePanel(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, bgColor);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;

        var btn = go.AddComponent<Button>();
        SetButtonColors(btn, bgColor, BTN_HIGHLIGHT);

        TextMeshProUGUI txt = CreateText(go.transform, "Text", label, fontSize, textColor, TextAlignmentOptions.Center);
        var txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        txt.fontStyle = FontStyles.Bold;

        return btn;
    }

    static void SetButtonColors(Button btn, Color normal, Color highlight)
    {
        var colors = btn.colors;
        colors.normalColor = normal;
        colors.highlightedColor = highlight;
        colors.pressedColor = new Color(highlight.r * 0.8f, highlight.g * 0.8f, highlight.b * 0.8f, 1f);
        colors.selectedColor = highlight;
        btn.colors = colors;
    }

    static Image CreateImageSlot(Transform parent, string name, float w, float h, Color color)
    {
        GameObject go = CreatePanel(parent, name, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, color);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;
        return go.GetComponent<Image>();
    }

    static GameObject CreateLayoutRow(Transform parent, string name, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;
        return go;
    }

    // ─── Gem Slots ───────────────────────────────────────────────

    struct GemSlotsResult2
    {
        public Transform parent;
        public Button[] removeButtons;
    }

    static GemSlotsResult2 CreateGemSlotsRow(Transform parent, string name, int count)
    {
        GameObject container = new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);
        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var le = container.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;

        Button[] removeBtns = new Button[count];

        for (int i = 0; i < count; i++)
        {
            // Vertical group: slot + remove button
            GameObject slotGroup = new GameObject($"SlotGroup_{i}", typeof(RectTransform));
            slotGroup.transform.SetParent(container.transform, false);
            var vlg = slotGroup.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Gem slot
            GameObject slot = CreatePanel(slotGroup.transform, $"GemSlot_{i}",
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, SLOT_EMPTY);
            var slotLE = slot.AddComponent<LayoutElement>();
            slotLE.preferredWidth = 60;
            slotLE.preferredHeight = 60;
            AddOutline(slot, SLOT_BORDER, 1);

            slot.AddComponent<SocketingSlotUI>();

            // gem icon
            CreatePanel(slot.transform, "GemIcon",
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero,
                new Color(0.5f, 0.5f, 0.5f, 0.3f));
            // highlight border
            GameObject borderGO = CreatePanel(slot.transform, "HighlightBorder",
                Vector2.zero, Vector2.one, new Vector2(-2, -2), new Vector2(2, 2),
                new Color(1f, 0.84f, 0, 0.8f));
            borderGO.GetComponent<Image>().enabled = false;
            borderGO.GetComponent<Image>().raycastTarget = false;

            // label
            TextMeshProUGUI label = CreateText(slot.transform, "Label", "Trống", 9, TEXT_DIM, TextAlignmentOptions.Bottom);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = new Vector2(1, 0.3f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            // Remove button "X" below slot
            removeBtns[i] = CreateButton(slotGroup.transform, $"RemoveBtn_{i}", "X", 11, 50, 18,
                new Color(0.5f, 0.15f, 0.15f, 0.9f), Color.white);
        }

        return new GemSlotsResult2 { parent = container.transform, removeButtons = removeBtns };
    }

    // ─── Crystal Slot ────────────────────────────────────────────

    struct CrystalSlotResult
    {
        public Image icon;
        public TextMeshProUGUI text;
        public Button clearBtn;
    }

    static CrystalSlotResult CreateCrystalSlot(Transform parent, string name)
    {
        // Container
        GameObject container = CreatePanel(parent, name,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, CRYSTAL_BG);
        var containerLE = container.AddComponent<LayoutElement>();
        containerLE.preferredWidth = 250;
        containerLE.preferredHeight = 55;
        AddOutline(container, new Color(0.5f, 0.3f, 0.7f, 0.6f), 1);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 5, 5);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Crystal icon
        Image crystalIcon = CreateImageSlot(container.transform, "CrystalIcon", 40, 40,
            new Color(0.3f, 0.3f, 0.3f, 0.3f));

        // Crystal name text
        TextMeshProUGUI crystalText = CreateText(container.transform, "CrystalText",
            "Chọn Crystal Stone", 13, TEXT_DIM, TextAlignmentOptions.Left);
        var textLE = crystalText.gameObject.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1;

        // Clear button
        Button clearBtn = CreateButton(container.transform, "ClearBtn", "✕", 14, 28, 28,
            new Color(0.5f, 0.2f, 0.2f, 0.8f), Color.white);

        // Add CrystalSlotUI component
        container.AddComponent<CrystalSlotUI>();

        return new CrystalSlotResult { icon = crystalIcon, text = crystalText, clearBtn = clearBtn };
    }

    // ─── Gem Drop Slot ────────────────────────────────────────────

    struct GemDropResult
    {
        public Image icon;
        public TextMeshProUGUI text;
    }

    static GemDropResult CreateGemDropSlot(Transform parent, string name)
    {
        GameObject container = CreatePanel(parent, name,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
            new Color(0.15f, 0.2f, 0.15f, 0.8f));
        var containerLE = container.AddComponent<LayoutElement>();
        containerLE.preferredWidth = 250;
        containerLE.preferredHeight = 60;
        AddOutline(container, new Color(0.3f, 0.7f, 0.3f, 0.6f), 1);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 5, 5);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Gem icon
        Image gemDropIcon = CreateImageSlot(container.transform, "GemDropIcon", 45, 45,
            new Color(0.3f, 0.3f, 0.3f, 0.3f));

        // Label
        TextMeshProUGUI gemDropText = CreateText(container.transform, "GemDropText",
            "Chon Gem de kham", 13, TEXT_DIM, TextAlignmentOptions.Left);
        var textLE = gemDropText.gameObject.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1;

        return new GemDropResult { icon = gemDropIcon, text = gemDropText };
    }

    // ─── Scroll View ─────────────────────────────────────────────

    static GameObject CreateScrollView(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        // ScrollView root
        GameObject scrollView = CreatePanel(parent, name,
            anchorMin, anchorMax, offsetMin, offsetMax, new Color(0.1f, 0.1f, 0.15f, 0.7f));
        var scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        AddOutline(scrollView, SLOT_BORDER, 1);

        // Viewport
        GameObject viewport = CreatePanel(scrollView.transform, "Viewport",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        viewport.GetComponent<Image>().color = Color.white; // Mask needs opaque image

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = new Vector2(5, 0);
        contentRT.offsetMax = new Vector2(-5, 0);

        // Grid layout for items
        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(90, 100);
        grid.spacing = new Vector2(8, 8);
        grid.padding = new RectOffset(5, 5, 5, 5);
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        // Content size fitter
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Wire scroll rect
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRT;

        return scrollView;
    }

    static T FindFirstObjectByType<T>(FindObjectsInactive includeInactive = FindObjectsInactive.Exclude) where T : Object
    {
        return Object.FindFirstObjectByType<T>(includeInactive);
    }
}
