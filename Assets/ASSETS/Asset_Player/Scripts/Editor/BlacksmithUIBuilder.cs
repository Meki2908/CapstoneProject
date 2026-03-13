#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool: Tự động tạo toàn bộ Blacksmith UI hierarchy + wiring references.
/// Chạy từ menu: Tools > Create Blacksmith UI
/// Sau khi tạo xong, chỉ cần gắn BlacksmithNPC lên NPC trong scene.
/// </summary>
public class BlacksmithUIBuilder
{
    // ─── Colors ───
    static readonly Color BG_DARK = new Color(0.08f, 0.06f, 0.12f, 0.95f);
    static readonly Color PANEL_BG = new Color(0.12f, 0.10f, 0.18f, 0.9f);
    static readonly Color TAB_ACTIVE = new Color(1f, 0.75f, 0.1f, 1f);
    static readonly Color TAB_INACTIVE = new Color(0.25f, 0.22f, 0.30f, 1f);
    static readonly Color SLOT_EMPTY = new Color(0.15f, 0.13f, 0.20f, 0.8f);
    static readonly Color SLOT_BORDER = new Color(0.4f, 0.35f, 0.5f, 0.6f);
    static readonly Color BTN_SOCKET = new Color(0.9f, 0.55f, 0.1f, 1f);
    static readonly Color BTN_CLOSE = new Color(0.7f, 0.15f, 0.15f, 1f);
    static readonly Color TEXT_WHITE = new Color(0.95f, 0.93f, 0.90f);
    static readonly Color TEXT_GOLD = new Color(1f, 0.84f, 0.0f);
    static readonly Color CRYSTAL_BG = new Color(0.18f, 0.10f, 0.28f, 0.8f);
    static readonly Color VIEWPORT_BG = new Color(0.10f, 0.08f, 0.14f, 0.85f);
    static readonly Color SUCCESS_GREEN = new Color(0.2f, 0.8f, 0.3f, 1f);

    [MenuItem("Tools/Create Blacksmith UI")]
    public static void CreateBlacksmithUI()
    {
        // ═══════════════════════════════════════════════════
        // 1. CANVAS
        // ═══════════════════════════════════════════════════
        GameObject canvasGO = new GameObject("Canvas_Blacksmith");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // ═══════════════════════════════════════════════════
        // 2. MAIN PANEL (background overlay)
        // ═══════════════════════════════════════════════════
        GameObject mainPanel = CreatePanel(canvasGO, "BlacksmithPanel", BG_DARK);
        Stretch(mainPanel);

        // Inner frame (centered, 1200x750)
        GameObject innerFrame = CreatePanel(mainPanel, "InnerFrame", PANEL_BG);
        SetAnchored(innerFrame, 960, 720, 0, 0);
        var innerOutline = innerFrame.AddComponent<Outline>();
        innerOutline.effectColor = new Color(0.6f, 0.45f, 0.1f, 0.6f);
        innerOutline.effectDistance = new Vector2(2, 2);

        // ═══════════════════════════════════════════════════
        // 3. HEADER
        // ═══════════════════════════════════════════════════
        GameObject header = CreateTMP(innerFrame, "HeaderText", "⚒  NPC THỢ RÈN", 28, TEXT_GOLD, TextAlignmentOptions.Center);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 50);
        headerRT.anchoredPosition = new Vector2(0, -5);

        // Close button
        GameObject closeBtn = CreateButton(innerFrame, "CloseButton", "✕", 24, BTN_CLOSE);
        RectTransform closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 1);
        closeBtnRT.anchorMax = new Vector2(1, 1);
        closeBtnRT.pivot = new Vector2(1, 1);
        closeBtnRT.sizeDelta = new Vector2(45, 45);
        closeBtnRT.anchoredPosition = new Vector2(-8, -8);

        // ═══════════════════════════════════════════════════
        // 4. TAB PANEL (left side)
        // ═══════════════════════════════════════════════════
        GameObject tabPanel = CreatePanel(innerFrame, "TabPanel", new Color(0.08f, 0.06f, 0.10f, 0.9f));
        RectTransform tabRT = tabPanel.GetComponent<RectTransform>();
        tabRT.anchorMin = new Vector2(0, 0);
        tabRT.anchorMax = new Vector2(0, 1);
        tabRT.pivot = new Vector2(0, 0.5f);
        tabRT.sizeDelta = new Vector2(160, -60);
        tabRT.anchoredPosition = new Vector2(5, -25);
        var tabVLG = tabPanel.AddComponent<VerticalLayoutGroup>();
        tabVLG.padding = new RectOffset(8, 8, 15, 15);
        tabVLG.spacing = 10;
        tabVLG.childForceExpandWidth = true;
        tabVLG.childForceExpandHeight = false;

        GameObject weaponTab = CreateButton(tabPanel, "WeaponTabButton", "⚔  Vũ Khí", 18, TAB_ACTIVE);
        weaponTab.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 55);
        var weaponTabLE = weaponTab.AddComponent<LayoutElement>();
        weaponTabLE.preferredHeight = 55;

        GameObject equipTab = CreateButton(tabPanel, "EquipmentTabButton", "🛡  Trang Bị", 18, TAB_INACTIVE);
        equipTab.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 55);
        var equipTabLE = equipTab.AddComponent<LayoutElement>();
        equipTabLE.preferredHeight = 55;

        // ═══════════════════════════════════════════════════
        // 5. CONTENT AREA (right of tabs)
        // ═══════════════════════════════════════════════════
        GameObject contentArea = CreatePanel(innerFrame, "ContentArea", Color.clear);
        RectTransform contentRT = contentArea.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 0);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.offsetMin = new Vector2(170, 5);
        contentRT.offsetMax = new Vector2(-5, -55);

        // ─── WEAPON TAB PANEL ────────────────────────────
        GameObject weaponTabPanel = CreatePanel(contentArea, "WeaponTabPanel", Color.clear);
        Stretch(weaponTabPanel);

        // Weapon display row
        GameObject weaponDisplayRow = CreatePanel(weaponTabPanel, "WeaponDisplayRow", Color.clear);
        RectTransform wdRT = weaponDisplayRow.GetComponent<RectTransform>();
        wdRT.anchorMin = new Vector2(0, 1);
        wdRT.anchorMax = new Vector2(1, 1);
        wdRT.pivot = new Vector2(0.5f, 1);
        wdRT.sizeDelta = new Vector2(0, 80);
        wdRT.anchoredPosition = Vector2.zero;
        var wdHLG = weaponDisplayRow.AddComponent<HorizontalLayoutGroup>();
        wdHLG.padding = new RectOffset(10, 10, 5, 5);
        wdHLG.spacing = 15;
        wdHLG.childAlignment = TextAnchor.MiddleLeft;
        wdHLG.childForceExpandWidth = false;

        // Weapon icon
        GameObject weaponIcon = CreateImage(weaponDisplayRow, "WeaponIcon", null, Color.white);
        var wiLE = weaponIcon.AddComponent<LayoutElement>();
        wiLE.preferredWidth = 64;
        wiLE.preferredHeight = 64;

        // Weapon name
        GameObject weaponName = CreateTMP(weaponDisplayRow, "WeaponNameText", "Không có vũ khí", 20, TEXT_WHITE, TextAlignmentOptions.Left);
        var wnLE = weaponName.AddComponent<LayoutElement>();
        wnLE.preferredWidth = 250;
        wnLE.flexibleWidth = 1;

        // Weapon gem slots row
        GameObject weaponGemRow = CreatePanel(weaponTabPanel, "WeaponGemSlots", Color.clear);
        RectTransform wgRT = weaponGemRow.GetComponent<RectTransform>();
        wgRT.anchorMin = new Vector2(0, 1);
        wgRT.anchorMax = new Vector2(0.6f, 1);
        wgRT.pivot = new Vector2(0, 1);
        wgRT.sizeDelta = new Vector2(0, 90);
        wgRT.anchoredPosition = new Vector2(0, -85);
        var wgHLG = weaponGemRow.AddComponent<HorizontalLayoutGroup>();
        wgHLG.padding = new RectOffset(10, 10, 5, 5);
        wgHLG.spacing = 12;
        wgHLG.childAlignment = TextAnchor.MiddleCenter;
        wgHLG.childForceExpandWidth = false;

        // 3 weapon gem slots
        for (int i = 0; i < 3; i++)
            CreateGemSlot(weaponGemRow, $"WeaponGemSlot_{i}", 75);

        // Crystal slot (weapon tab)
        GameObject wCrystalSlot = CreateCrystalSlot(weaponTabPanel, "WeaponCrystalSlot");
        RectTransform wcRT = wCrystalSlot.GetComponent<RectTransform>();
        wcRT.anchorMin = new Vector2(0.62f, 1);
        wcRT.anchorMax = new Vector2(1, 1);
        wcRT.pivot = new Vector2(0.5f, 1);
        wcRT.sizeDelta = new Vector2(0, 90);
        wcRT.anchoredPosition = new Vector2(0, -85);

        // ─── EQUIPMENT TAB PANEL ─────────────────────────
        GameObject equipTabPanel = CreatePanel(contentArea, "EquipmentTabPanel", Color.clear);
        Stretch(equipTabPanel);
        equipTabPanel.SetActive(false); // hidden by default

        // Equipment slots row (4 buttons)
        GameObject equipSlotRow = CreatePanel(equipTabPanel, "EquipmentSlotRow", Color.clear);
        RectTransform esRT = equipSlotRow.GetComponent<RectTransform>();
        esRT.anchorMin = new Vector2(0, 1);
        esRT.anchorMax = new Vector2(1, 1);
        esRT.pivot = new Vector2(0.5f, 1);
        esRT.sizeDelta = new Vector2(0, 80);
        esRT.anchoredPosition = Vector2.zero;
        var esHLG = equipSlotRow.AddComponent<HorizontalLayoutGroup>();
        esHLG.padding = new RectOffset(10, 10, 5, 5);
        esHLG.spacing = 10;
        esHLG.childAlignment = TextAnchor.MiddleCenter;
        esHLG.childForceExpandWidth = false;

        string[] slotNames = { "Head", "Body", "Legs", "Accessory" };
        Button[] equipButtons = new Button[4];
        Image[] equipIcons = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject slotBtn = CreateEquipmentSlot(equipSlotRow, $"EquipSlot_{slotNames[i]}", slotNames[i]);
            equipButtons[i] = slotBtn.GetComponent<Button>();
            equipIcons[i] = slotBtn.transform.Find("SlotIcon")?.GetComponent<Image>();
        }

        // Equipment name
        GameObject equipNameText = CreateTMP(equipTabPanel, "EquipmentNameText", "Chọn slot trang bị", 18, TEXT_WHITE, TextAlignmentOptions.Center);
        RectTransform enRT = equipNameText.GetComponent<RectTransform>();
        enRT.anchorMin = new Vector2(0, 1);
        enRT.anchorMax = new Vector2(0.6f, 1);
        enRT.pivot = new Vector2(0, 1);
        enRT.sizeDelta = new Vector2(0, 30);
        enRT.anchoredPosition = new Vector2(0, -85);

        // Equipment gem slots row (4 slots)
        GameObject equipGemRow = CreatePanel(equipTabPanel, "EquipmentGemSlots", Color.clear);
        RectTransform egRT = equipGemRow.GetComponent<RectTransform>();
        egRT.anchorMin = new Vector2(0, 1);
        egRT.anchorMax = new Vector2(0.6f, 1);
        egRT.pivot = new Vector2(0, 1);
        egRT.sizeDelta = new Vector2(0, 90);
        egRT.anchoredPosition = new Vector2(0, -115);
        var egHLG = equipGemRow.AddComponent<HorizontalLayoutGroup>();
        egHLG.padding = new RectOffset(10, 10, 5, 5);
        egHLG.spacing = 10;
        egHLG.childAlignment = TextAnchor.MiddleCenter;
        egHLG.childForceExpandWidth = false;

        for (int i = 0; i < 4; i++)
            CreateGemSlot(equipGemRow, $"EquipGemSlot_{i}", 70);

        // Crystal slot (equipment tab)
        GameObject eCrystalSlot = CreateCrystalSlot(equipTabPanel, "EquipmentCrystalSlot");
        RectTransform ecRT = eCrystalSlot.GetComponent<RectTransform>();
        ecRT.anchorMin = new Vector2(0.62f, 1);
        ecRT.anchorMax = new Vector2(1, 1);
        ecRT.pivot = new Vector2(0.5f, 1);
        ecRT.sizeDelta = new Vector2(0, 90);
        ecRT.anchoredPosition = new Vector2(0, -115);

        // ═══════════════════════════════════════════════════
        // 6. SUCCESS RATE BAR (shared)
        // ═══════════════════════════════════════════════════
        GameObject successPanel = CreatePanel(contentArea, "SuccessRatePanel", Color.clear);
        RectTransform spRT = successPanel.GetComponent<RectTransform>();
        spRT.anchorMin = new Vector2(0, 1);
        spRT.anchorMax = new Vector2(1, 1);
        spRT.pivot = new Vector2(0.5f, 1);
        spRT.sizeDelta = new Vector2(-20, 35);
        spRT.anchoredPosition = new Vector2(0, -210);

        // Bar background
        GameObject barBG = CreateImage(successPanel, "BarBackground", null, new Color(0.1f, 0.1f, 0.1f, 0.8f));
        Stretch(barBG);
        var barBGOutline = barBG.AddComponent<Outline>();
        barBGOutline.effectColor = SLOT_BORDER;

        // Bar fill
        GameObject barFill = CreateImage(successPanel, "SuccessRateBar", null, SUCCESS_GREEN);
        Stretch(barFill);
        Image barFillImg = barFill.GetComponent<Image>();
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillAmount = 0f;

        // Bar text
        GameObject barText = CreateTMP(successPanel, "SuccessRateText", "Tỉ lệ thành công: 0%", 16, TEXT_WHITE, TextAlignmentOptions.Center);
        Stretch(barText);

        // ═══════════════════════════════════════════════════
        // 7. SOCKET BUTTON
        // ═══════════════════════════════════════════════════
        GameObject socketBtn = CreateButton(contentArea, "SocketButton", "🔨  KHẢM GEM", 22, BTN_SOCKET);
        RectTransform sbRT = socketBtn.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(0.25f, 1);
        sbRT.anchorMax = new Vector2(0.75f, 1);
        sbRT.pivot = new Vector2(0.5f, 1);
        sbRT.sizeDelta = new Vector2(0, 45);
        sbRT.anchoredPosition = new Vector2(0, -255);
        var sbOutline = socketBtn.AddComponent<Outline>();
        sbOutline.effectColor = new Color(1f, 0.7f, 0f, 0.5f);
        sbOutline.effectDistance = new Vector2(2, 2);

        // Result panel
        GameObject resultPanel = CreatePanel(contentArea, "ResultPanel", new Color(0, 0, 0, 0.7f));
        RectTransform rpRT = resultPanel.GetComponent<RectTransform>();
        rpRT.anchorMin = new Vector2(0.15f, 1);
        rpRT.anchorMax = new Vector2(0.85f, 1);
        rpRT.pivot = new Vector2(0.5f, 1);
        rpRT.sizeDelta = new Vector2(0, 50);
        rpRT.anchoredPosition = new Vector2(0, -305);
        resultPanel.SetActive(false);

        GameObject resultText = CreateTMP(resultPanel, "ResultText", "", 18, Color.white, TextAlignmentOptions.Center);
        Stretch(resultText);

        // ═══════════════════════════════════════════════════
        // 8. INVENTORY PANEL (HÀNH TRANG — hiện tất cả item)
        // ═══════════════════════════════════════════════════
        GameObject inventoryPanel = CreatePanel(contentArea, "InventoryPanel", Color.clear);
        RectTransform ipRT = inventoryPanel.GetComponent<RectTransform>();
        ipRT.anchorMin = new Vector2(0, 0);
        ipRT.anchorMax = new Vector2(1, 1);
        ipRT.offsetMin = new Vector2(0, 0);
        ipRT.offsetMax = new Vector2(0, -360);

        // Label
        GameObject invLabel = CreateTMP(inventoryPanel, "InventoryLabel", "🎒  HÀNH TRANG", 15, new Color(0.8f, 0.75f, 0.6f), TextAlignmentOptions.TopLeft);
        RectTransform invLabelRT = invLabel.GetComponent<RectTransform>();
        invLabelRT.anchorMin = new Vector2(0, 1);
        invLabelRT.anchorMax = new Vector2(1, 1);
        invLabelRT.pivot = new Vector2(0, 1);
        invLabelRT.sizeDelta = new Vector2(0, 22);
        invLabelRT.anchoredPosition = new Vector2(8, 0);

        // Hint text
        GameObject hintText = CreateTMP(inventoryPanel, "HintText", "Click Gem (viền vàng) hoặc Crystal (viền tím) để chọn", 11, new Color(0.5f, 0.5f, 0.5f), TextAlignmentOptions.TopRight);
        RectTransform hintRT = hintText.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 1);
        hintRT.anchorMax = new Vector2(1, 1);
        hintRT.pivot = new Vector2(1, 1);
        hintRT.sizeDelta = new Vector2(0, 22);
        hintRT.anchoredPosition = new Vector2(-8, 0);

        // Scroll area for inventory grid
        GameObject invScrollGO = new GameObject("InventoryScroll", typeof(RectTransform));
        invScrollGO.transform.SetParent(inventoryPanel.transform, false);
        Image invScrollBG = invScrollGO.AddComponent<Image>();
        invScrollBG.color = VIEWPORT_BG;
        var invScrollOutline = invScrollGO.AddComponent<Outline>();
        invScrollOutline.effectColor = SLOT_BORDER;
        RectTransform invScrollRT = invScrollGO.GetComponent<RectTransform>();
        invScrollRT.anchorMin = new Vector2(0, 0);
        invScrollRT.anchorMax = new Vector2(1, 1);
        invScrollRT.offsetMin = new Vector2(0, 0);
        invScrollRT.offsetMax = new Vector2(0, -24);

        ScrollRect invScroll = invScrollGO.AddComponent<ScrollRect>();
        invScroll.horizontal = false;
        invScroll.vertical = true;

        // Viewport (mask)
        GameObject invViewport = new GameObject("Viewport", typeof(RectTransform));
        invViewport.transform.SetParent(invScrollGO.transform, false);
        invViewport.AddComponent<Image>().color = Color.clear;
        invViewport.AddComponent<Mask>().showMaskGraphic = false;
        Stretch(invViewport);

        // Content (Grid Layout — same as main inventory)
        GameObject invContent = new GameObject("Content", typeof(RectTransform));
        invContent.transform.SetParent(invViewport.transform, false);
        RectTransform invContentRT = invContent.GetComponent<RectTransform>();
        invContentRT.anchorMin = new Vector2(0, 1);
        invContentRT.anchorMax = new Vector2(1, 1);
        invContentRT.pivot = new Vector2(0.5f, 1);
        invContentRT.sizeDelta = new Vector2(0, 300);
        var gridLG = invContent.AddComponent<GridLayoutGroup>();
        gridLG.cellSize = new Vector2(80, 80);
        gridLG.spacing = new Vector2(6, 6);
        gridLG.padding = new RectOffset(8, 8, 8, 8);
        gridLG.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLG.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLG.childAlignment = TextAnchor.UpperLeft;
        gridLG.constraint = GridLayoutGroup.Constraint.Flexible;
        var invCSF = invContent.AddComponent<ContentSizeFitter>();
        invCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        invScroll.viewport = invViewport.GetComponent<RectTransform>();
        invScroll.content = invContentRT;

        // ═══════════════════════════════════════════════════
        // 9. WIRE UP BlacksmithUI COMPONENT
        // ═══════════════════════════════════════════════════
        BlacksmithUI bsUI = mainPanel.AddComponent<BlacksmithUI>();

        // Use SerializedObject to set private [SerializeField] references
        SerializedObject so = new SerializedObject(bsUI);

        so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();

        so.FindProperty("weaponTabButton").objectReferenceValue = weaponTab.GetComponent<Button>();
        so.FindProperty("equipmentTabButton").objectReferenceValue = equipTab.GetComponent<Button>();
        so.FindProperty("weaponTabPanel").objectReferenceValue = weaponTabPanel;
        so.FindProperty("equipmentTabPanel").objectReferenceValue = equipTabPanel;

        so.FindProperty("weaponIcon").objectReferenceValue = weaponIcon.GetComponent<Image>();
        so.FindProperty("weaponNameText").objectReferenceValue = weaponName.GetComponent<TextMeshProUGUI>();
        so.FindProperty("weaponGemSlotsParent").objectReferenceValue = weaponGemRow.transform;

        // Weapon crystal
        Transform wCrystalContent = wCrystalSlot.transform;
        so.FindProperty("weaponCrystalSlotIcon").objectReferenceValue = wCrystalContent.Find("CrystalIcon")?.GetComponent<Image>();
        so.FindProperty("weaponCrystalSlotText").objectReferenceValue = wCrystalContent.Find("CrystalText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("weaponCrystalClearButton").objectReferenceValue = wCrystalContent.Find("ClearButton")?.GetComponent<Button>();

        // Equipment slots
        var esBtnsProp = so.FindProperty("equipmentSlotButtons");
        esBtnsProp.arraySize = 4;
        var esIconsProp = so.FindProperty("equipmentSlotIcons");
        esIconsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            esBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = equipButtons[i];
            esIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = equipIcons[i];
        }

        so.FindProperty("equipmentNameText").objectReferenceValue = equipNameText.GetComponent<TextMeshProUGUI>();
        so.FindProperty("equipmentGemSlotsParent").objectReferenceValue = equipGemRow.transform;

        // Equipment crystal
        Transform eCrystalContent = eCrystalSlot.transform;
        so.FindProperty("equipmentCrystalSlotIcon").objectReferenceValue = eCrystalContent.Find("CrystalIcon")?.GetComponent<Image>();
        so.FindProperty("equipmentCrystalSlotText").objectReferenceValue = eCrystalContent.Find("CrystalText")?.GetComponent<TextMeshProUGUI>();
        so.FindProperty("equipmentCrystalClearButton").objectReferenceValue = eCrystalContent.Find("ClearButton")?.GetComponent<Button>();

        // Success rate
        so.FindProperty("successRateBar").objectReferenceValue = barFillImg;
        so.FindProperty("successRateText").objectReferenceValue = barText.GetComponent<TextMeshProUGUI>();

        // Socket button
        so.FindProperty("socketButton").objectReferenceValue = socketBtn.GetComponent<Button>();
        so.FindProperty("socketButtonText").objectReferenceValue = socketBtn.GetComponentInChildren<TextMeshProUGUI>();

        // Result
        so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        so.FindProperty("resultText").objectReferenceValue = resultText.GetComponent<TextMeshProUGUI>();

        // Inventory panel
        so.FindProperty("inventoryContent").objectReferenceValue = invContent.transform;

        // Try to find existing Item prefab
        GameObject itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ASSETS/Asset_Player/Prefabs/Inventory shit/Item.prefab");
        if (itemPrefab != null)
        {
            so.FindProperty("itemUIPrefab").objectReferenceValue = itemPrefab;
        }

        so.ApplyModifiedProperties();

        // ═══════════════════════════════════════════════════
        // 10. SELECT & DONE
        // ═══════════════════════════════════════════════════
        Selection.activeGameObject = canvasGO;
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Blacksmith UI");

        EditorUtility.DisplayDialog(
            "Blacksmith UI Builder",
            "✅ Đã tạo Canvas_Blacksmith!\n\n" +
            "Bước tiếp theo:\n" +
            "1. Gắn BlacksmithNPC.cs lên NPC trong scene\n" +
            "2. Kéo BlacksmithUI vào field 'blacksmithUI' trên NPC\n" +
            "3. Gán Icon sprite cho 6 Crystal Stone SO\n" +
            (itemPrefab == null ? "\n⚠️ Chưa tìm thấy Item prefab — hãy gán thủ công 'itemUIPrefab' trong BlacksmithUI" : ""),
            "OK"
        );
    }

    // ─── HELPER: Create Gem Slot with SocketingSlotUI ─────────

    static void CreateGemSlot(GameObject parent, string name, int size)
    {
        GameObject slot = new GameObject(name, typeof(RectTransform));
        slot.transform.SetParent(parent.transform, false);
        var le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;

        // Background
        Image bg = slot.AddComponent<Image>();
        bg.color = SLOT_EMPTY;
        var bgOutline = slot.AddComponent<Outline>();
        bgOutline.effectColor = SLOT_BORDER;
        bgOutline.effectDistance = new Vector2(1.5f, 1.5f);

        // Gem icon
        GameObject icon = CreateImage(slot, "GemIcon", null, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.15f, 0.15f);
        iconRT.anchorMax = new Vector2(0.85f, 0.85f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        // Highlight border
        GameObject highlight = CreateImage(slot, "HighlightBorder", null, TAB_ACTIVE);
        Stretch(highlight);
        highlight.GetComponent<Image>().enabled = false;

        // Label
        GameObject label = CreateTMP(slot, "SlotLabel", "Trống", 10, new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Bottom);
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 0.3f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        // Add SocketingSlotUI component and wire
        SocketingSlotUI slotUI = slot.AddComponent<SocketingSlotUI>();
        SerializedObject sso = new SerializedObject(slotUI);
        sso.FindProperty("gemIcon").objectReferenceValue = icon.GetComponent<Image>();
        sso.FindProperty("slotBackground").objectReferenceValue = bg;
        sso.FindProperty("highlightBorder").objectReferenceValue = highlight.GetComponent<Image>();
        sso.FindProperty("slotLabel").objectReferenceValue = label.GetComponent<TextMeshProUGUI>();
        sso.ApplyModifiedProperties();
    }

    // ─── HELPER: Create Crystal Slot ─────────────────────────

    static GameObject CreateCrystalSlot(GameObject parent, string name)
    {
        GameObject slot = CreatePanel(parent, name, CRYSTAL_BG);
        var outline = slot.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.2f, 0.8f, 0.5f);

        // Title
        GameObject title = CreateTMP(slot, "CrystalTitle", "ĐÁ CRYSTAL", 12, new Color(0.7f, 0.5f, 0.9f), TextAlignmentOptions.Top);
        RectTransform titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 0.75f);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;

        // Icon
        GameObject icon = CreateImage(slot, "CrystalIcon", null, new Color(0.4f, 0.3f, 0.5f, 0.4f));
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.3f, 0.2f);
        iconRT.anchorMax = new Vector2(0.7f, 0.7f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        // Text
        GameObject text = CreateTMP(slot, "CrystalText", "Chọn Crystal", 11, TEXT_WHITE, TextAlignmentOptions.Bottom);
        RectTransform textRT = text.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 0.22f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Clear button
        GameObject clearBtn = CreateButton(slot, "ClearButton", "✕", 12, new Color(0.6f, 0.2f, 0.2f, 0.8f));
        RectTransform clearRT = clearBtn.GetComponent<RectTransform>();
        clearRT.anchorMin = new Vector2(1, 1);
        clearRT.anchorMax = new Vector2(1, 1);
        clearRT.pivot = new Vector2(1, 1);
        clearRT.sizeDelta = new Vector2(22, 22);
        clearRT.anchoredPosition = new Vector2(-2, -2);

        return slot;
    }

    // ─── HELPER: Create Equipment Slot Button ────────────────

    static GameObject CreateEquipmentSlot(GameObject parent, string name, string label)
    {
        GameObject slot = new GameObject(name, typeof(RectTransform));
        slot.transform.SetParent(parent.transform, false);
        var le = slot.AddComponent<LayoutElement>();
        le.preferredWidth = 70;
        le.preferredHeight = 70;

        Image bg = slot.AddComponent<Image>();
        bg.color = SLOT_EMPTY;
        var outline = slot.AddComponent<Outline>();
        outline.effectColor = SLOT_BORDER;

        Button btn = slot.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.4f, 0.35f, 0.5f);
        btn.colors = colors;

        // Slot icon
        GameObject icon = CreateImage(slot, "SlotIcon", null, new Color(0.4f, 0.4f, 0.4f, 0.5f));
        RectTransform iconRT = icon.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.12f, 0.22f);
        iconRT.anchorMax = new Vector2(0.88f, 0.88f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;

        // Label
        GameObject labelGO = CreateTMP(slot, "Label", label, 10, TEXT_WHITE, TextAlignmentOptions.Bottom);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0);
        labelRT.anchorMax = new Vector2(1, 0.25f);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        return slot;
    }

    // ─── HELPER: Create Scroll Viewport ──────────────────────

    static GameObject CreateScrollViewport(GameObject parent, string name, string label, float yPos)
    {
        GameObject container = CreatePanel(parent, name, Color.clear);
        RectTransform cRT = container.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.sizeDelta = new Vector2(-10, 110);
        cRT.anchoredPosition = new Vector2(0, yPos);

        // Label
        GameObject labelGO = CreateTMP(container, "Label", label, 13, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.TopLeft);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 1);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.pivot = new Vector2(0, 1);
        labelRT.sizeDelta = new Vector2(0, 20);
        labelRT.anchoredPosition = new Vector2(5, 0);

        // Scroll area
        GameObject scrollGO = new GameObject("Scroll", typeof(RectTransform));
        scrollGO.transform.SetParent(container.transform, false);
        Image scrollBG = scrollGO.AddComponent<Image>();
        scrollBG.color = VIEWPORT_BG;
        var scrollOutline = scrollGO.AddComponent<Outline>();
        scrollOutline.effectColor = SLOT_BORDER;
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(0, 0);
        scrollRT.offsetMax = new Vector2(0, -22);

        ScrollRect scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;

        // Viewport (mask)
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(scrollGO.transform, false);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Stretch(viewport);

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT2 = content.GetComponent<RectTransform>();
        contentRT2.anchorMin = new Vector2(0, 0);
        contentRT2.anchorMax = new Vector2(0, 1);
        contentRT2.pivot = new Vector2(0, 0.5f);
        contentRT2.sizeDelta = new Vector2(800, 0);
        var hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 5, 5);
        hlg.spacing = 8;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRT2;

        return container;
    }

    // ═══════════════════════════════════════════════════════
    // LOW-LEVEL HELPERS
    // ═══════════════════════════════════════════════════════

    static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject CreateImage(GameObject parent, string name, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = true;
        return go;
    }

    static GameObject CreateTMP(GameObject parent, string name, string text, int fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return go;
    }

    static GameObject CreateButton(GameObject parent, string name, string text, int fontSize, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1, 1, 1, 0.8f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        btn.targetGraphic = img;

        // Button text
        GameObject txtGO = CreateTMP(go, "Text", text, fontSize, Color.white, TextAlignmentOptions.Center);
        Stretch(txtGO);

        return go;
    }

    static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchored(GameObject go, float width, float height, float x, float y)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(x, y);
    }
}
#endif
