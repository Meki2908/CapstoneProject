using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Tool sửa lỗi khi chuyển Canv_Options từ World Space sang Screen Space - Overlay
/// Các vấn đề thường gặp:
/// 1. Scale quá lớn (World Space dùng scale lớn)
/// 2. Thiếu GraphicRaycaster
/// 3. Thiếu CanvasScaler đúng
/// 4. RectTransform positions sai
/// 5. Text bị ẩn dưới Background/Button (layer order)
/// </summary>
public class FixOverlayCanvasTool : EditorWindow
{
    private GameObject targetCanvas;
    private bool deepFix = true;
    private float scaleFactor = 0.01f; // World Space thường có scale lớn gấp 100 lần
    
    [MenuItem("Tools/UI Tools/Fix Overlay Canvas")]
    public static void ShowWindow()
    {
        GetWindow<FixOverlayCanvasTool>("Fix Overlay Canvas");
    }
    
    void OnGUI()
    {
        GUILayout.Label("=== FIX OVERLAY CANVAS ===", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Kéo Canvas cần fix vào đây:", EditorStyles.label);
        targetCanvas = (GameObject)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(GameObject), true);
        
        GUILayout.Space(10);
        
        if (targetCanvas == null)
        {
            EditorGUILayout.HelpBox("Vui lòng chọn Canvas (Canv_Options) cần fix", MessageType.Info);
            
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
        
        deepFix = EditorGUILayout.Toggle("Deep Fix (Fix tất cả children)", deepFix);
        
        if (deepFix)
        {
            scaleFactor = EditorGUILayout.Slider("Scale Factor", scaleFactor, 0.001f, 0.1f);
            EditorGUILayout.HelpBox(
                "Deep Fix sẽ:\n" +
                "• Đổi Canvas sang Overlay\n" +
                "• Reset tất cả Transform positions về 0\n" +
                "• Scale down elements theo factor\n" +
                "• Fix TextMeshPro font sizes\n" +
                "• Thêm GraphicRaycaster", 
                MessageType.Warning);
        }
        
        GUILayout.Space(10);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("FIX CANVAS", GUILayout.Height(40)))
        {
            FixCanvas();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(10);
        
        // === NÚT MỚI: FIX TEXT LAYER ORDER ===
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("FIX TEXT LAYER ORDER", GUILayout.Height(35)))
        {
            FixTextLayerOrder();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.HelpBox(
            "Sửa lỗi Text bị ẩn dưới Background/Button.\n" +
            "Đưa Text lên trên cùng trong mỗi parent.", 
            MessageType.Info);
        
        GUILayout.Space(10);
        
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Chỉ thêm Components (không đổi positions)"))
        {
            FixComponentsOnly();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(5);
        
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("UNDO (Ctrl+Z)"))
        {
            Undo.PerformUndo();
        }
        GUI.backgroundColor = Color.white;
    }
    
    /// <summary>
    /// Sửa lỗi Text bị ẩn dưới Background/Button
    /// Trong Unity UI, sibling cuối cùng sẽ render trên cùng
    /// </summary>
    void FixTextLayerOrder()
    {
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn Canvas trước!", "OK");
            return;
        }
        
        Undo.RegisterFullObjectHierarchyUndo(targetCanvas, "Fix Text Layer Order");
        
        int fixedCount = 0;
        FixTextLayerInChildren(targetCanvas.transform, ref fixedCount);
        
        EditorUtility.SetDirty(targetCanvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            $"Đã sắp xếp lại {fixedCount} Text elements lên trên cùng!\n\n" +
            "Text giờ sẽ hiển thị trên Background/Button.", 
            "OK");
    }
    
    void FixTextLayerInChildren(Transform parent, ref int fixedCount)
    {
        // Thu thập tất cả children trước để tránh lỗi khi iterate
        List<Transform> children = new List<Transform>();
        foreach (Transform child in parent)
        {
            children.Add(child);
        }
        
        foreach (Transform child in children)
        {
            // === FIX 1: TextMeshPro 3D (có MeshRenderer) - Không tương thích với Overlay ===
            MeshRenderer meshRenderer = child.GetComponent<MeshRenderer>();
            TMP_Text tmpText = child.GetComponent<TMP_Text>();
            
            if (meshRenderer != null && tmpText != null)
            {
                // TextMeshPro 3D dùng MeshRenderer không hiển thị trong Overlay Canvas
                // Cần disable MeshRenderer và đảm bảo có CanvasRenderer
                Debug.LogWarning($"[FixLayer] Found TMP 3D with MeshRenderer on '{child.name}' - disabling MeshRenderer for Overlay mode");
                
                // Disable MeshRenderer (gây ra vấn đề z-fighting và rendering issues)
                meshRenderer.enabled = false;
                
                // Đảm bảo có CanvasRenderer
                CanvasRenderer canvasRenderer = child.GetComponent<CanvasRenderer>();
                if (canvasRenderer == null)
                {
                    canvasRenderer = child.gameObject.AddComponent<CanvasRenderer>();
                    Debug.Log($"[FixLayer] Added CanvasRenderer to '{child.name}'");
                }
                
                // Chuyển TextMeshPro sang orthographic mode cho Canvas
                tmpText.isOrthographic = true;
                
                fixedCount++;
            }
            
            // === FIX 2: Đảm bảo CanvasRenderer hoạt động đúng cho TMP_Text ===
            if (tmpText != null)
            {
                CanvasRenderer canvasRenderer = child.GetComponent<CanvasRenderer>();
                if (canvasRenderer != null)
                {
                    // Force rebuild
                    canvasRenderer.cull = false;
                }
                
                // Đảm bảo text visible
                if (tmpText.color.a < 0.1f)
                {
                    Color c = tmpText.color;
                    c.a = 1f;
                    tmpText.color = c;
                    Debug.Log($"[FixLayer] Fixed alpha on '{child.name}'");
                    fixedCount++;
                }
            }
            
            // === FIX 3: Legacy Text ===
            Text legacyText = child.GetComponent<Text>();
            if (legacyText != null)
            {
                child.SetAsLastSibling();
                fixedCount++;
            }
            
            // Đệ quy vào children
            if (child.childCount > 0)
            {
                FixTextLayerInChildren(child, ref fixedCount);
            }
        }
    }
    
    void FixCanvas()
    {
        if (targetCanvas == null) return;
        
        Undo.RegisterFullObjectHierarchyUndo(targetCanvas, "Fix Overlay Canvas");
        
        Canvas canvas = targetCanvas.GetComponent<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "GameObject này không có Canvas component!", "OK");
            return;
        }
        
        // 1. Đổi sang Overlay
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        // 2. Thêm GraphicRaycaster nếu chưa có
        GraphicRaycaster raycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = targetCanvas.AddComponent<GraphicRaycaster>();
            Debug.Log("[Fix] Đã thêm GraphicRaycaster");
        }
        
        // 3. Fix CanvasScaler
        CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = targetCanvas.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        
        // 4. Reset Transform của Canvas
        targetCanvas.transform.localPosition = Vector3.zero;
        targetCanvas.transform.localRotation = Quaternion.identity;
        targetCanvas.transform.localScale = Vector3.one;
        
        // 5. Deep fix nếu được chọn
        if (deepFix)
        {
            int fixedCount = DeepFixChildren(targetCanvas.transform, 0);
            Debug.Log($"[Fix] Đã fix {fixedCount} elements");
        }
        
        // 6. Kiểm tra EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[Fix] Đã tạo EventSystem mới");
        }
        
        EditorUtility.SetDirty(targetCanvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            "Đã fix Canvas sang Overlay mode!\n\n" +
            "• Render Mode: Screen Space - Overlay\n" +
            "• Sort Order: 100\n" +
            "• GraphicRaycaster: Có\n" +
            "• CanvasScaler: Scale With Screen Size (1920x1080)\n\n" +
            "Nếu UI vẫn không đúng, thử Ctrl+Z và chạy lại với Scale Factor khác.", 
            "OK");
    }
    
    void FixComponentsOnly()
    {
        if (targetCanvas == null) return;
        
        Undo.RegisterFullObjectHierarchyUndo(targetCanvas, "Fix Components Only");
        
        Canvas canvas = targetCanvas.GetComponent<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "GameObject này không có Canvas component!", "OK");
            return;
        }
        
        // Đổi sang Overlay
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        // Thêm GraphicRaycaster
        if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            targetCanvas.AddComponent<GraphicRaycaster>();
        }
        
        // Fix CanvasScaler
        CanvasScaler scaler = targetCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = targetCanvas.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        // Reset Canvas transform only
        targetCanvas.transform.localPosition = Vector3.zero;
        targetCanvas.transform.localRotation = Quaternion.identity;
        targetCanvas.transform.localScale = Vector3.one;
        
        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        
        EditorUtility.SetDirty(targetCanvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            "Đã thêm components cần thiết!\n" +
            "UI giữ nguyên positions.", "OK");
    }
    
    int DeepFixChildren(Transform parent, int count)
    {
        foreach (Transform child in parent)
        {
            count++;
            
            // Fix RectTransform
            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect != null)
            {
                // Reset Z position
                Vector3 pos = rect.localPosition;
                rect.localPosition = new Vector3(pos.x, pos.y, 0);
                
                // Scale down nếu quá lớn
                Vector2 size = rect.sizeDelta;
                if (size.x > 2000 || size.y > 2000)
                {
                    rect.sizeDelta = size * scaleFactor;
                }
                
                // Scale down anchored position nếu quá lớn
                Vector2 anchoredPos = rect.anchoredPosition;
                if (Mathf.Abs(anchoredPos.x) > 1000 || Mathf.Abs(anchoredPos.y) > 1000)
                {
                    rect.anchoredPosition = anchoredPos * scaleFactor;
                }
            }
            
            // Reset scale nếu quá lớn
            if (child.localScale.x > 10 || child.localScale.y > 10)
            {
                child.localScale = Vector3.one;
            }
            
            // Fix TextMeshPro
            TMP_Text tmpText = child.GetComponent<TMP_Text>();
            if (tmpText != null)
            {
                if (tmpText.fontSize > 100)
                {
                    tmpText.fontSize = Mathf.Clamp(tmpText.fontSize * scaleFactor, 12f, 72f);
                    tmpText.enableAutoSizing = true;
                    tmpText.fontSizeMin = 8;
                    tmpText.fontSizeMax = 72;
                }
            }
            
            // Đảm bảo Image có Raycast Target
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                // Buttons cần raycast target
                if (child.GetComponent<Button>() != null)
                {
                    img.raycastTarget = true;
                }
            }
            
            // Đệ quy
            if (child.childCount > 0)
            {
                count = DeepFixChildren(child, count);
            }
        }
        
        return count;
    }
}

