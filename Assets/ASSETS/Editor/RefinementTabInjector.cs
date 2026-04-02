using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor tool: "Tools → Add Refinement Tab to Blacksmith"
/// Adds ONLY the Refinement tab to an EXISTING Canvas_Blacksmith prefab in the scene.
/// Does NOT modify existing Weapon/Equipment tabs.
/// 
/// Usage:
/// 1. Open your scene that has the Canvas_Blacksmith (or drag prefab into scene)
/// 2. Select Canvas_Blacksmith in Hierarchy
/// 3. Tools → Add Refinement Tab to Blacksmith
/// 4. Apply prefab overrides to save changes
/// </summary>
public class RefinementTabInjector : Editor
{
    // ─── Same color palette as BlacksmithCanvasCreator ──────────
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

    [MenuItem("Tools/Add Refinement Tab to Blacksmith")]
    public static void AddRefinementTab()
    {
        // ── Find existing Canvas_Blacksmith ──
        BlacksmithUI blacksmithUI = null;
        
        // Priority 1: Selected object
        if (Selection.activeGameObject != null)
        {
            blacksmithUI = Selection.activeGameObject.GetComponent<BlacksmithUI>();
            if (blacksmithUI == null)
                blacksmithUI = Selection.activeGameObject.GetComponentInChildren<BlacksmithUI>(true);
            if (blacksmithUI == null)
                blacksmithUI = Selection.activeGameObject.GetComponentInParent<BlacksmithUI>();
        }

        // Priority 2: Prefab Stage (khi double-click prefab để edit)
        if (blacksmithUI == null)
        {
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var prefabRoot = prefabStage.prefabContentsRoot;
                blacksmithUI = prefabRoot.GetComponent<BlacksmithUI>();
                if (blacksmithUI == null)
                    blacksmithUI = prefabRoot.GetComponentInChildren<BlacksmithUI>(true);
                Debug.Log($"[RefinementTabInjector] Found BlacksmithUI in Prefab Stage: {(blacksmithUI != null ? "YES" : "NO")}");
            }
        }

        // Priority 3: Find in scene
        if (blacksmithUI == null)
            blacksmithUI = Object.FindFirstObjectByType<BlacksmithUI>(FindObjectsInactive.Include);

        if (blacksmithUI == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Canvas_Blacksmith not found!\n\n" +
                "Cách 1: Double-click prefab Canvas_Blacksmith để mở Prefab Mode → chạy lại\n" +
                "Cách 2: Kéo prefab Canvas_Blacksmith vào Scene → select nó → chạy lại",
                "OK");
            return;
        }

        Debug.Log($"[RefinementTabInjector] Found BlacksmithUI on: {blacksmithUI.gameObject.name}");

        // ── Check if already has refinement tab ──
        var existingSO = new SerializedObject(blacksmithUI);
        var existingRefTab = existingSO.FindProperty("refinementTabPanel");
        if (existingRefTab != null && existingRefTab.objectReferenceValue != null)
        {
            if (!EditorUtility.DisplayDialog("Warning",
                "Refinement tab already exists!\nDo you want to recreate it?",
                "Recreate", "Cancel"))
                return;

            var oldPanel = existingRefTab.objectReferenceValue as GameObject;
            if (oldPanel != null) DestroyImmediate(oldPanel);

            var existingRefBtn = existingSO.FindProperty("refinementTabButton");
            if (existingRefBtn != null && existingRefBtn.objectReferenceValue != null)
            {
                var oldBtn = (existingRefBtn.objectReferenceValue as Button)?.gameObject;
                if (oldBtn != null) DestroyImmediate(oldBtn);
            }
        }

        Undo.RegisterCompleteObjectUndo(blacksmithUI.gameObject, "Add Refinement Tab");

        _font = TMP_Settings.defaultFontAsset;
        if (_font == null)
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // ================================================================
        // FIND EXISTING UI ELEMENTS
        // ================================================================
        Transform root = blacksmithUI.transform;

        // Find Sidebar (contains tab buttons) — try multiple strategies
        Transform sidebar = FindChildRecursive(root, "Sidebar");
        if (sidebar == null)
        {
            // Fallback: find any VerticalLayoutGroup that contains "WeaponTabBtn" or "EquipTabBtn"
            var weapBtn = FindChildRecursive(root, "WeaponTabBtn");
            if (weapBtn != null) sidebar = weapBtn.parent;
        }
        if (sidebar == null)
        {
            Debug.LogError("[RefinementTabInjector] Cannot find 'Sidebar' or tab buttons parent!");
            EditorUtility.DisplayDialog("Error",
                "Cannot find 'Sidebar' in prefab hierarchy!\n" +
                "Hierarchy dump logged to Console.", "OK");
            DumpHierarchy(root, 0);
            return;
        }
        Debug.Log($"[RefinementTabInjector] Found Sidebar: {GetFullPath(sidebar)}");

        // Find ContentArea (where tab panels live)
        Transform contentArea = FindChildRecursive(root, "ContentArea");
        if (contentArea == null)
        {
            // Fallback: find parent of WeaponTabPanel or EquipmentTabPanel
            var weapPanel = FindChildRecursive(root, "WeaponTabPanel");
            if (weapPanel != null) contentArea = weapPanel.parent;
        }
        if (contentArea == null)
        {
            var equipPanel = FindChildRecursive(root, "EquipmentTabPanel");
            if (equipPanel != null) contentArea = equipPanel.parent;
        }
        if (contentArea == null)
        {
            Debug.LogError("[RefinementTabInjector] Cannot find 'ContentArea' or tab panels parent!");
            EditorUtility.DisplayDialog("Error",
                "Cannot find 'ContentArea' in prefab hierarchy!\n" +
                "Hierarchy dump logged to Console.", "OK");
            DumpHierarchy(root, 0);
            return;
        }
        Debug.Log($"[RefinementTabInjector] Found ContentArea: {GetFullPath(contentArea)}");

        // ================================================================
        // 1. ADD TAB BUTTON TO SIDEBAR
        // ================================================================
        // Check if RefineTabBtn already exists
        Transform existingTabBtn = sidebar.Find("RefineTabBtn");
        if (existingTabBtn != null)
            DestroyImmediate(existingTabBtn.gameObject);

        Button refinementTabButton = CreateButton(sidebar, "RefineTabBtn", "REFINE", 28, 240, 90,
            BTN_NORMAL, TEXT_WHITE);

        // ================================================================
        // 2. CREATE REFINEMENT TAB PANEL
        // ================================================================
        GameObject refinementTabPanel = CreatePanel(contentArea, "RefinementTabPanel",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0));
        refinementTabPanel.SetActive(false);

        var refineLayout = refinementTabPanel.AddComponent<VerticalLayoutGroup>();
        refineLayout.padding = new RectOffset(30, 30, 20, 20);
        refineLayout.spacing = 14;
        refineLayout.childAlignment = TextAnchor.UpperCenter;
        refineLayout.childForceExpandWidth = true;
        refineLayout.childForceExpandHeight = false;

        // ── Refine Equipment Selection Row (4 slots) ──
        GameObject refineEquipRow = CreateLayoutRow(refinementTabPanel.transform, "RefineEquipRow", 130);
        string[] refineSlotLabels = { "Head", "Body", "Legs", "Acc" };
        Button[] refineEquipSlotButtons = new Button[4];
        Image[] refineEquipSlotIcons = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject slotGO = CreatePanel(refineEquipRow.transform, $"RefineEquipSlot_{i}",
                Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, SLOT_EMPTY);
            var slotLE = slotGO.AddComponent<LayoutElement>();
            slotLE.preferredWidth = 110;
            slotLE.preferredHeight = 110;
            AddOutline(slotGO, SLOT_BORDER, 2);
            refineEquipSlotButtons[i] = slotGO.AddComponent<Button>();
            SetButtonColors(refineEquipSlotButtons[i], SLOT_EMPTY, BTN_HIGHLIGHT);
            refineEquipSlotIcons[i] = slotGO.GetComponent<Image>();
            TextMeshProUGUI rSlotLabel = CreateText(slotGO.transform, "Label", refineSlotLabels[i], 18, TEXT_DIM, TextAlignmentOptions.Bottom);
            var rLabelRect = rSlotLabel.GetComponent<RectTransform>();
            rLabelRect.anchorMin = Vector2.zero;
            rLabelRect.anchorMax = Vector2.one;
            rLabelRect.offsetMin = Vector2.zero;
            rLabelRect.offsetMax = Vector2.zero;
        }

        // ── Refine Equip Name ──
        TextMeshProUGUI refineEquipNameText = CreateText(refinementTabPanel.transform, "RefineEquipName",
            "Select equipment to refine", 30, GOLD, TextAlignmentOptions.Center);
        refineEquipNameText.fontStyle = FontStyles.Bold;
        var reNameLE = refineEquipNameText.gameObject.AddComponent<LayoutElement>();
        reNameLE.preferredHeight = 50;

        // ── Refine Level Display ──
        TextMeshProUGUI refineLevelText = CreateText(refinementTabPanel.transform, "RefineLevelText",
            "", 34, GOLD, TextAlignmentOptions.Center);
        refineLevelText.fontStyle = FontStyles.Bold;
        var rlLE = refineLevelText.gameObject.AddComponent<LayoutElement>();
        rlLE.preferredHeight = 50;

        // ── Star Images Row (7 stars) ──
        GameObject starRow = CreateLayoutRow(refinementTabPanel.transform, "RefineStarRow", 50);
        var starRowHLG = starRow.GetComponent<HorizontalLayoutGroup>();
        if (starRowHLG != null) { starRowHLG.spacing = 6; starRowHLG.childAlignment = TextAnchor.MiddleCenter; }
        Image[] refineStarImages = new Image[7];
        Color starGray = new Color(0.3f, 0.3f, 0.35f);
        for (int i = 0; i < 7; i++)
        {
            GameObject starGO = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
            starGO.transform.SetParent(starRow.transform, false);
            var starLE = starGO.AddComponent<LayoutElement>();
            starLE.preferredWidth = 40;
            starLE.preferredHeight = 40;
            refineStarImages[i] = starGO.GetComponent<Image>();
            refineStarImages[i].color = starGray;
            // Star sprite will use the generated star sprite or Unity default
        }

        GameObject statsPreviewGO = CreatePanel(refinementTabPanel.transform, "RefineStatsPreview",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.1f, 0.1f, 0.15f, 0.8f));
        var statsPreviewLE = statsPreviewGO.AddComponent<LayoutElement>();
        statsPreviewLE.preferredHeight = 180;
        statsPreviewLE.flexibleWidth = 1;
        AddOutline(statsPreviewGO, SLOT_BORDER, 1);
        TextMeshProUGUI refineStatsText = CreateText(statsPreviewGO.transform, "StatsText", "", 24, TEXT_WHITE, TextAlignmentOptions.Left);
        refineStatsText.enableWordWrapping = true;
        var rstRect = refineStatsText.GetComponent<RectTransform>();
        rstRect.anchorMin = Vector2.zero;
        rstRect.anchorMax = Vector2.one;
        rstRect.offsetMin = new Vector2(20, 10);
        rstRect.offsetMax = new Vector2(-20, -10);

        // ── Material Slot ──
        GameObject refineMaterialRow = CreateLayoutRow(refinementTabPanel.transform, "RefineMaterialRow", 110);
        var rmResult = CreateCrystalSlot(refineMaterialRow.transform, "RefineMaterial");
        rmResult.text.text = "Select Refinement Stone";

        // ── Refine Success Rate ──
        TextMeshProUGUI refineSuccessText = CreateText(refinementTabPanel.transform, "RefineSuccessText",
            "Success Rate: 0%", 28, TEXT_WHITE, TextAlignmentOptions.Center);
        var rsTextLE = refineSuccessText.gameObject.AddComponent<LayoutElement>();
        rsTextLE.preferredHeight = 44;

        GameObject refineBarBG = CreatePanel(refinementTabPanel.transform, "RefineBarBG",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
            new Color(0.15f, 0.15f, 0.2f, 0.9f));
        var rbLE = refineBarBG.AddComponent<LayoutElement>();
        rbLE.preferredHeight = 36;
        rbLE.flexibleWidth = 1;
        AddOutline(refineBarBG, SLOT_BORDER, 2);

        GameObject refineBarFill = CreatePanel(refineBarBG.transform, "RefineBarFill",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, SUCCESS_GREEN);
        Image refineSuccessBar = refineBarFill.GetComponent<Image>();
        refineSuccessBar.type = Image.Type.Filled;
        refineSuccessBar.fillMethod = Image.FillMethod.Horizontal;
        refineSuccessBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        refineSuccessBar.fillAmount = 0f;

        // ── Refine Button ──
        Button refineButton = CreateButton(refinementTabPanel.transform, "RefineBtn", "REFINE", 34, 500, 85,
            new Color(0.5f, 0.3f, 0.1f, 0.9f), Color.white);
        TextMeshProUGUI refineButtonText = refineButton.GetComponentInChildren<TextMeshProUGUI>();
        refineButton.GetComponent<LayoutElement>().preferredHeight = 85;

        // ── Fusion Section ──
        GameObject fusionSection = CreatePanel(refinementTabPanel.transform, "FusionSection",
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero,
            new Color(0.12f, 0.1f, 0.18f, 0.85f));
        var fsLE = fusionSection.AddComponent<LayoutElement>();
        fsLE.preferredHeight = 100;
        fsLE.flexibleWidth = 1;
        AddOutline(fusionSection, new Color(0.5f, 0.3f, 0.7f, 0.5f), 2);

        var fusionHLG = fusionSection.AddComponent<HorizontalLayoutGroup>();
        fusionHLG.padding = new RectOffset(20, 20, 10, 10);
        fusionHLG.spacing = 16;
        fusionHLG.childAlignment = TextAnchor.MiddleCenter;
        fusionHLG.childForceExpandWidth = false;
        fusionHLG.childForceExpandHeight = false;

        TextMeshProUGUI fusionTitle = CreateText(fusionSection.transform, "FusionTitle", "FUSION", 24, GOLD, TextAlignmentOptions.Center);
        fusionTitle.fontStyle = FontStyles.Bold;
        var ftLE = fusionTitle.gameObject.AddComponent<LayoutElement>();
        ftLE.preferredWidth = 120;

        Image fusionSourceIcon = CreateImageSlot(fusionSection.transform, "FusionSourceIcon", 60, 60, new Color(0.3f, 0.3f, 0.3f, 0.5f));

        TextMeshProUGUI fusionInfoText = CreateText(fusionSection.transform, "FusionInfoText", "4x >> 1x", 24, TEXT_WHITE, TextAlignmentOptions.Center);
        var fiLE = fusionInfoText.gameObject.AddComponent<LayoutElement>();
        fiLE.flexibleWidth = 1;

        Image fusionResultIcon = CreateImageSlot(fusionSection.transform, "FusionResultIcon", 60, 60, new Color(0.3f, 0.3f, 0.3f, 0.5f));

        Button fusionButton = CreateButton(fusionSection.transform, "FusionBtn", "FUSE", 24, 120, 60,
            new Color(0.3f, 0.2f, 0.5f, 0.9f), Color.white);

        // ================================================================
        // 3. ASSIGN REFERENCES TO BlacksmithUI
        // ================================================================
        var so = new SerializedObject(blacksmithUI);

        so.FindProperty("refinementTabButton").objectReferenceValue = refinementTabButton;
        so.FindProperty("refinementTabPanel").objectReferenceValue = refinementTabPanel;

        var refEquipBtnsProp = so.FindProperty("refineEquipSlotButtons");
        refEquipBtnsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
            refEquipBtnsProp.GetArrayElementAtIndex(i).objectReferenceValue = refineEquipSlotButtons[i];

        var refEquipIconsProp = so.FindProperty("refineEquipSlotIcons");
        refEquipIconsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
            refEquipIconsProp.GetArrayElementAtIndex(i).objectReferenceValue = refineEquipSlotIcons[i];

        so.FindProperty("refineEquipNameText").objectReferenceValue = refineEquipNameText;
        so.FindProperty("refineLevelText").objectReferenceValue = refineLevelText;

        var refStarsProp = so.FindProperty("refineStarImages");
        refStarsProp.arraySize = 7;
        for (int i = 0; i < 7; i++)
            refStarsProp.GetArrayElementAtIndex(i).objectReferenceValue = refineStarImages[i];

        so.FindProperty("refineStatsText").objectReferenceValue = refineStatsText;
        so.FindProperty("refineMaterialIcon").objectReferenceValue = rmResult.icon;
        so.FindProperty("refineMaterialText").objectReferenceValue = rmResult.text;
        so.FindProperty("refineMaterialClearButton").objectReferenceValue = rmResult.clearBtn;
        so.FindProperty("refineSuccessBar").objectReferenceValue = refineSuccessBar;
        so.FindProperty("refineSuccessText").objectReferenceValue = refineSuccessText;
        so.FindProperty("refineButton").objectReferenceValue = refineButton;
        so.FindProperty("refineButtonText").objectReferenceValue = refineButtonText;
        so.FindProperty("fusionSourceIcon").objectReferenceValue = fusionSourceIcon;
        so.FindProperty("fusionResultIcon").objectReferenceValue = fusionResultIcon;
        so.FindProperty("fusionButton").objectReferenceValue = fusionButton;
        so.FindProperty("fusionInfoText").objectReferenceValue = fusionInfoText;

        so.ApplyModifiedProperties();

        // Mark dirty for prefab save
        EditorUtility.SetDirty(blacksmithUI);
        EditorUtility.SetDirty(blacksmithUI.gameObject);

        Debug.Log("[RefinementTabInjector] ✅ Refinement tab added successfully to existing Canvas_Blacksmith!\n" +
                  "All references assigned. Apply prefab overrides to save changes.");

        EditorUtility.DisplayDialog("Success!",
            "Refinement tab đã được thêm vào Canvas_Blacksmith!\n\n" +
            "→ Nhấn Apply All trong Prefab Overrides để lưu vào prefab.\n" +
            "→ Chạy Tools → Create Refinement Stones để tạo item đá.",
            "OK");
    }

    // ================================================================
    // HELPER: Find child recursively by name
    // ================================================================
    static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ================================================================
    // UI HELPER METHODS (duplicated from BlacksmithCanvasCreator for independence)
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
        go.GetComponent<Image>().color = color;
        return go;
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
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;
        return go;
    }

    // ── Crystal Slot (reused for Material slot) ──
    struct CrystalSlotResult
    {
        public Image icon;
        public TextMeshProUGUI text;
        public Button clearBtn;
    }

    static CrystalSlotResult CreateCrystalSlot(Transform parent, string name)
    {
        GameObject container = CreatePanel(parent, name,
            Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, CRYSTAL_BG);
        var containerLE = container.AddComponent<LayoutElement>();
        containerLE.preferredWidth = 500;
        containerLE.preferredHeight = 110;
        AddOutline(container, new Color(0.5f, 0.3f, 0.7f, 0.6f), 2);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(16, 16, 10, 10);
        hlg.spacing = 16;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        Image crystalIcon = CreateImageSlot(container.transform, "CrystalIcon", 80, 80,
            new Color(0.3f, 0.3f, 0.3f, 0.3f));

        TextMeshProUGUI crystalText = CreateText(container.transform, "CrystalText",
            "Chọn Crystal Stone", 26, TEXT_DIM, TextAlignmentOptions.Left);
        var textLE = crystalText.gameObject.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1;

        Button clearBtn = CreateButton(container.transform, "ClearBtn", "✕", 28, 56, 56,
            new Color(0.5f, 0.2f, 0.2f, 0.8f), Color.white);

        return new CrystalSlotResult { icon = crystalIcon, text = crystalText, clearBtn = clearBtn };
    }

    // ── Debug helpers ──
    static void DumpHierarchy(Transform t, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent}[{t.name}] children={t.childCount}");
        foreach (Transform child in t)
            DumpHierarchy(child, depth + 1);
    }

    static string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
