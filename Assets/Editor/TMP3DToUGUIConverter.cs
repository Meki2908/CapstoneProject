using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Tool chuyển đổi TextMeshPro 3D (dùng MeshRenderer) sang TextMeshProUGUI (dùng CanvasRenderer)
/// Cần thiết khi chuyển Canvas từ World Space sang Screen Space - Overlay
/// Hỗ trợ Undo đầy đủ
/// </summary>
public class TMP3DToUGUIConverter : EditorWindow
{
    private GameObject targetCanvas;
    private bool previewMode = true;
    private Vector2 scrollPosition;
    private List<TMPConversionInfo> foundTexts = new List<TMPConversionInfo>();
    
    // === FONT SIZE SCALING ===
    private float fontSizeScale = 0.08f; // World Space font ~469 → Screen Space ~37
    private float minFontSize = 12f;
    private float maxFontSize = 72f;
    
    private class TMPConversionInfo
    {
        public GameObject gameObject;
        public string originalName;
        public string text;
        public TMP_FontAsset fontAsset;
        public float fontSize;
        public Color fontColor;
        public FontStyles fontStyle;
        public TextAlignmentOptions alignment;
        public bool hasMeshRenderer;
        public bool selected = true;
    }
    
    [MenuItem("Tools/UI Tools/TMP 3D to UGUI Converter")]
    public static void ShowWindow()
    {
        var window = GetWindow<TMP3DToUGUIConverter>("TMP 3D → UGUI");
        window.minSize = new Vector2(450, 500);
    }
    
    void OnGUI()
    {
        GUILayout.Label("=== CHUYỂN ĐỔI TMP 3D → UGUI ===", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "Tool này chuyển đổi TextMeshPro 3D (MeshRenderer) sang TextMeshProUGUI\n" +
            "để tương thích với Screen Space - Overlay Canvas.\n\n" +
            "✓ Hỗ trợ Undo (Ctrl+Z)\n" +
            "✓ Tự động scale font size", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        // === TARGET CANVAS ===
        GUILayout.Label("Bước 1: Chọn Canvas chứa Text cần chuyển đổi", EditorStyles.label);
        targetCanvas = (GameObject)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(GameObject), true);
        
        if (targetCanvas == null)
        {
            GUILayout.Space(5);
            if (GUILayout.Button("Tự động tìm Canv_Options"))
            {
                targetCanvas = GameObject.Find("Canv_Options");
                if (targetCanvas == null)
                {
                    EditorUtility.DisplayDialog("Không tìm thấy", "Không tìm thấy Canv_Options trong scene!", "OK");
                }
            }
            return;
        }
        
        GUILayout.Space(10);
        
        // === FONT SIZE SETTINGS ===
        GUILayout.Label("Bước 2: Cài đặt Font Size", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical("box");
        fontSizeScale = EditorGUILayout.Slider("Font Scale Factor", fontSizeScale, 0.01f, 0.3f);
        EditorGUILayout.LabelField("  Ví dụ: Font 469 × 0.08 = 37.5", EditorStyles.miniLabel);
        
        GUILayout.Space(5);
        
        GUILayout.BeginHorizontal();
        minFontSize = EditorGUILayout.FloatField("Min Font Size", minFontSize, GUILayout.Width(200));
        maxFontSize = EditorGUILayout.FloatField("Max Font Size", maxFontSize, GUILayout.Width(200));
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // === SCAN BUTTON ===
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("QUÉT TÌM TMP 3D", GUILayout.Height(30)))
        {
            ScanForTMP3D();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(10);
        
        // === RESULTS ===
        if (foundTexts.Count > 0)
        {
            GUILayout.Label($"Tìm thấy {foundTexts.Count} TextMeshPro 3D cần chuyển đổi:", EditorStyles.boldLabel);
            
            GUILayout.Space(5);
            
            // Select/Deselect all
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Chọn tất cả", GUILayout.Width(100)))
            {
                foreach (var info in foundTexts) info.selected = true;
            }
            if (GUILayout.Button("Bỏ chọn tất cả", GUILayout.Width(100)))
            {
                foreach (var info in foundTexts) info.selected = false;
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // List với font size preview
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            foreach (var info in foundTexts)
            {
                GUILayout.BeginHorizontal();
                info.selected = EditorGUILayout.Toggle(info.selected, GUILayout.Width(20));
                
                if (GUILayout.Button(info.originalName, EditorStyles.label, GUILayout.Width(120)))
                {
                    Selection.activeGameObject = info.gameObject;
                    EditorGUIUtility.PingObject(info.gameObject);
                }
                
                // Hiển thị font size gốc và sau khi scale
                float scaledSize = Mathf.Clamp(info.fontSize * fontSizeScale, minFontSize, maxFontSize);
                GUILayout.Label($"{info.fontSize:F0} → {scaledSize:F0}", GUILayout.Width(80));
                
                GUILayout.Label($"[{info.text}]", GUILayout.Width(80));
                
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            // === CONVERT BUTTON ===
            int selectedCount = 0;
            foreach (var info in foundTexts) if (info.selected) selectedCount++;
            
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button($"CHUYỂN ĐỔI {selectedCount} ITEMS", GUILayout.Height(40)))
            {
                ConvertSelectedTMP3D();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Sau khi chuyển đổi:\n" +
                "• MeshRenderer sẽ bị xóa\n" +
                "• Font size sẽ được scale theo cài đặt\n" +
                "• Nhấn Ctrl+Z để hoàn tác", 
                MessageType.Warning);
        }
        else if (targetCanvas != null)
        {
            EditorGUILayout.HelpBox("Nhấn 'QUÉT TÌM TMP 3D' để tìm các Text cần chuyển đổi", MessageType.Info);
        }
        
        GUILayout.FlexibleSpace();
        
        // === UNDO BUTTON ===
        GUILayout.Space(10);
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("UNDO (Ctrl+Z)", GUILayout.Height(25)))
        {
            Undo.PerformUndo();
        }
        GUI.backgroundColor = Color.white;
    }
    
    void ScanForTMP3D()
    {
        foundTexts.Clear();
        
        if (targetCanvas == null) return;
        
        // Tìm tất cả TMP_Text trong children
        TMP_Text[] allTexts = targetCanvas.GetComponentsInChildren<TMP_Text>(true);
        
        foreach (TMP_Text tmpText in allTexts)
        {
            // Kiểm tra xem có MeshRenderer không (TMP 3D)
            MeshRenderer meshRenderer = tmpText.GetComponent<MeshRenderer>();
            
            // Chỉ thêm nếu là TMP 3D (có MeshRenderer)
            if (meshRenderer != null)
            {
                foundTexts.Add(new TMPConversionInfo
                {
                    gameObject = tmpText.gameObject,
                    originalName = tmpText.gameObject.name,
                    text = tmpText.text.Length > 10 ? tmpText.text.Substring(0, 10) + ".." : tmpText.text,
                    fontAsset = tmpText.font,
                    fontSize = tmpText.fontSize,
                    fontColor = tmpText.color,
                    fontStyle = tmpText.fontStyle,
                    alignment = tmpText.alignment,
                    hasMeshRenderer = true,
                    selected = true
                });
            }
        }
        
        if (foundTexts.Count == 0)
        {
            EditorUtility.DisplayDialog("Kết quả", 
                "Không tìm thấy TextMeshPro 3D nào cần chuyển đổi!\n\n" +
                "Canvas này có thể đã sử dụng TextMeshProUGUI.", "OK");
        }
    }
    
    void ConvertSelectedTMP3D()
    {
        int convertedCount = 0;
        
        // Register undo for entire hierarchy
        Undo.RegisterFullObjectHierarchyUndo(targetCanvas, "Convert TMP 3D to UGUI");
        
        foreach (var info in foundTexts)
        {
            if (!info.selected) continue;
            if (info.gameObject == null) continue;
            
            GameObject go = info.gameObject;
            
            // === Lưu thông tin text hiện tại ===
            TMP_Text oldTMP = go.GetComponent<TMP_Text>();
            if (oldTMP == null) continue;
            
            string text = oldTMP.text;
            TMP_FontAsset font = oldTMP.font;
            float fontSize = oldTMP.fontSize;
            Color color = oldTMP.color;
            FontStyles fontStyle = oldTMP.fontStyle;
            TextAlignmentOptions alignment = oldTMP.alignment;
            bool autoSize = oldTMP.enableAutoSizing;
            float fontSizeMinOld = oldTMP.fontSizeMin;
            float fontSizeMaxOld = oldTMP.fontSizeMax;
            Vector4 margin = oldTMP.margin;
            bool raycastTarget = oldTMP.raycastTarget;
            bool richText = oldTMP.richText;
            TextWrappingModes wrappingMode = oldTMP.textWrappingMode;
            TextOverflowModes overflowMode = oldTMP.overflowMode;
            
            // === Xóa components cũ ===
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            
            if (meshRenderer != null)
            {
                Undo.DestroyObjectImmediate(meshRenderer);
            }
            if (meshFilter != null)
            {
                Undo.DestroyObjectImmediate(meshFilter);
            }
            
            // Xóa TMP 3D component
            Undo.DestroyObjectImmediate(oldTMP);
            
            // === Đảm bảo có CanvasRenderer ===
            CanvasRenderer canvasRenderer = go.GetComponent<CanvasRenderer>();
            if (canvasRenderer == null)
            {
                canvasRenderer = Undo.AddComponent<CanvasRenderer>(go);
            }
            
            // === Thêm TextMeshProUGUI ===
            TextMeshProUGUI newTMP = Undo.AddComponent<TextMeshProUGUI>(go);
            
            // === SCALE FONT SIZE ===
            float scaledFontSize = Mathf.Clamp(fontSize * fontSizeScale, minFontSize, maxFontSize);
            float scaledFontSizeMin = Mathf.Clamp(fontSizeMinOld * fontSizeScale, 8f, scaledFontSize);
            float scaledFontSizeMax = Mathf.Clamp(fontSizeMaxOld * fontSizeScale, scaledFontSize, 200f);
            
            // === Áp dụng lại các thuộc tính ===
            newTMP.text = text;
            newTMP.font = font;
            newTMP.fontSize = scaledFontSize;
            newTMP.color = color;
            newTMP.fontStyle = fontStyle;
            newTMP.alignment = alignment;
            newTMP.enableAutoSizing = autoSize;
            newTMP.fontSizeMin = scaledFontSizeMin;
            newTMP.fontSizeMax = scaledFontSizeMax;
            newTMP.margin = margin;
            newTMP.raycastTarget = raycastTarget;
            newTMP.richText = richText;
            newTMP.textWrappingMode = wrappingMode;
            newTMP.overflowMode = overflowMode;
            
            // Đánh dấu là dirty
            EditorUtility.SetDirty(go);
            
            convertedCount++;
            Debug.Log($"[Converter] Converted '{go.name}': Font {fontSize:F0} → {scaledFontSize:F0}");
        }
        
        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        // Refresh list
        ScanForTMP3D();
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            $"Đã chuyển đổi {convertedCount} TextMeshPro 3D sang UGUI!\n\n" +
            $"Font size đã được scale theo hệ số {fontSizeScale}\n\n" +
            "Nhấn Ctrl+Z để hoàn tác nếu cần.", "OK");
    }
}

