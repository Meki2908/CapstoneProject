using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

/// <summary>
/// Tool debug và fix UI không click được
/// </summary>
public class FixFrozenUITool : EditorWindow
{
    private GameObject targetCanvas;
    
    [MenuItem("Tools/UI Tools/Fix Frozen UI (Debug)")]
    public static void ShowWindow()
    {
        GetWindow<FixFrozenUITool>("Fix Frozen UI");
    }
    
    void OnGUI()
    {
        GUILayout.Label("=== FIX FROZEN UI ===", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        targetCanvas = (GameObject)EditorGUILayout.ObjectField("Canvas", targetCanvas, typeof(GameObject), true);
        
        if (GUILayout.Button("Tìm Canv_Options"))
        {
            targetCanvas = GameObject.Find("Canv_Options");
        }
        
        GUILayout.Space(10);
        
        // Diagnostic
        if (GUILayout.Button("🔍 CHẨN ĐOÁN VẤN ĐỀ", GUILayout.Height(30)))
        {
            DiagnoseIssues();
        }
        
        GUILayout.Space(10);
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🔧 FIX TẤT CẢ", GUILayout.Height(40)))
        {
            FixAllIssues();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Công cụ này sẽ:\n" +
            "1. Kiểm tra EventSystem\n" +
            "2. Thêm GraphicRaycaster\n" +
            "3. Bật Raycast Target cho tất cả Buttons\n" +
            "4. Fix Animator Update Mode",
            MessageType.Info);
    }
    
    void DiagnoseIssues()
    {
        Debug.Log("=== BẮT ĐẦU CHẨN ĐOÁN ===");
        
        // 1. Check EventSystem
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ THIẾU EventSystem trong scene!");
        }
        else
        {
            Debug.Log("✓ EventSystem: OK");
            
            // Check input module
            var inputModule = eventSystem.GetComponent<BaseInputModule>();
            if (inputModule == null)
            {
                Debug.LogError("❌ EventSystem thiếu Input Module!");
            }
            else
            {
                Debug.Log($"✓ Input Module: {inputModule.GetType().Name}");
            }
        }
        
        // 2. Check Canvas
        if (targetCanvas == null)
        {
            Debug.LogWarning("⚠ Chưa chọn Canvas để kiểm tra");
            return;
        }
        
        Canvas canvas = targetCanvas.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ Object không có Canvas component!");
            return;
        }
        
        Debug.Log($"✓ Canvas Render Mode: {canvas.renderMode}");
        Debug.Log($"✓ Canvas Sort Order: {canvas.sortingOrder}");
        
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
        {
            Debug.LogError("❌ Canvas là Screen Space Camera nhưng chưa gán Camera!");
        }
        
        // 3. Check GraphicRaycaster
        var raycaster = targetCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("❌ THIẾU GraphicRaycaster! UI sẽ không click được!");
        }
        else
        {
            Debug.Log("✓ GraphicRaycaster: OK");
        }
        
        // 4. Check Buttons
        Button[] buttons = targetCanvas.GetComponentsInChildren<Button>(true);
        Debug.Log($"Tìm thấy {buttons.Length} Buttons");
        
        int badButtons = 0;
        foreach (var btn in buttons)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null && !img.raycastTarget)
            {
                Debug.LogWarning($"⚠ Button '{btn.name}' có raycastTarget = false!");
                badButtons++;
            }
            
            if (!btn.interactable)
            {
                Debug.LogWarning($"⚠ Button '{btn.name}' không interactable!");
                badButtons++;
            }
        }
        
        if (badButtons == 0)
        {
            Debug.Log("✓ Tất cả Buttons: OK");
        }
        
        // 5. Check Animators
        Animator[] animators = targetCanvas.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            if (anim.updateMode != AnimatorUpdateMode.UnscaledTime)
            {
                Debug.LogWarning($"⚠ Animator '{anim.name}' không dùng UnscaledTime - sẽ đơ khi pause!");
            }
        }
        
        Debug.Log("=== KẾT THÚC CHẨN ĐOÁN ===");
    }
    
    void FixAllIssues()
    {
        Debug.Log("=== BẮT ĐẦU FIX ===");
        
        // 1. Fix EventSystem
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("✓ Đã tạo EventSystem");
        }
        
        if (targetCanvas == null)
        {
            targetCanvas = GameObject.Find("Canv_Options");
        }
        
        if (targetCanvas == null)
        {
            Debug.LogError("Không tìm thấy Canvas!");
            return;
        }
        
        Undo.RegisterFullObjectHierarchyUndo(targetCanvas, "Fix Frozen UI");
        
        // 2. Fix GraphicRaycaster
        if (targetCanvas.GetComponent<GraphicRaycaster>() == null)
        {
            targetCanvas.AddComponent<GraphicRaycaster>();
            Debug.Log("✓ Đã thêm GraphicRaycaster");
        }
        
        // 3. Fix Canvas nếu là Screen Space Camera
        Canvas canvas = targetCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
                Debug.Log("✓ Đã gán Camera cho Canvas");
            }
        }
        
        // 4. Fix tất cả Buttons
        Button[] buttons = targetCanvas.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            // Enable interactable
            btn.interactable = true;
            
            // Enable raycast target
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = true;
            }
            
            // Fix navigation (none để tránh conflict)
            Navigation nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;
        }
        Debug.Log($"✓ Đã fix {buttons.Length} Buttons");
        
        // 5. Fix tất cả Animators
        Animator[] animators = targetCanvas.GetComponentsInChildren<Animator>(true);
        foreach (var anim in animators)
        {
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
        Debug.Log($"✓ Đã fix {animators.Length} Animators (UnscaledTime)");
        
        // 6. Fix tất cả Selectable (Slider, Toggle, etc.)
        Selectable[] selectables = targetCanvas.GetComponentsInChildren<Selectable>(true);
        foreach (var sel in selectables)
        {
            sel.interactable = true;
            
            var graphic = sel.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }
        }
        Debug.Log($"✓ Đã fix {selectables.Length} Selectables");
        
        EditorUtility.SetDirty(targetCanvas);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            "Đã fix tất cả vấn đề UI!\n\n" +
            "• EventSystem: OK\n" +
            "• GraphicRaycaster: OK\n" +
            $"• Buttons: {buttons.Length}\n" +
            $"• Animators: {animators.Length}\n" +
            $"• Selectables: {selectables.Length}\n\n" +
            "Hãy thử lại trong Play mode!",
            "OK");
        
        Debug.Log("=== KẾT THÚC FIX ===");
    }
}
