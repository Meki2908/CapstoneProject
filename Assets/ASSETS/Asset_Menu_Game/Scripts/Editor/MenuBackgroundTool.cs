using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor Tool để tự động gắn MenuBackground vào Canvas
/// </summary>
public class MenuBackgroundTool
{
    [MenuItem("Tools/Menu Background/Add to Selected Canvas")]
    public static void AddToSelectedCanvas()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn một Canvas trong Hierarchy!", "OK");
            return;
        }
        
        Canvas canvas = selected.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = selected.GetComponentInParent<Canvas>();
        }
        
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Object được chọn không phải Canvas!", "OK");
            return;
        }
        
        // Kiểm tra đã có script chưa
        MenuBackground existing = canvas.GetComponent<MenuBackground>();
        if (existing != null)
        {
            Selection.activeGameObject = canvas.gameObject;
            EditorUtility.DisplayDialog("Đã có sẵn", 
                "Canvas này đã có MenuBackground!\nĐang chọn nó cho bạn.", "OK");
            return;
        }
        
        // Thêm script
        MenuBackground menuBg = canvas.gameObject.AddComponent<MenuBackground>();
        
        // Set defaults
        menuBg.autoSetSortOrder = true;
        menuBg.sortOrder = -100;
        menuBg.useOverlay = true;
        
        Selection.activeGameObject = canvas.gameObject;
        
        EditorUtility.DisplayDialog("Thành công!", 
            "Đã thêm MenuBackground vào Canvas!\n\n" +
            "Bây giờ bạn có thể:\n" +
            "• Kéo Sprite vào 'Background Sprite'\n" +
            "• Hoặc chọn Video và kéo vào 'Background Video'\n" +
            "• Điều chỉnh Sort Order nếu cần", "OK");
    }
    
    [MenuItem("Tools/Menu Background/Create Background Canvas")]
    public static void CreateBackgroundCanvas()
    {
        // Tạo Canvas mới
        GameObject canvasGO = new GameObject("Canvas_Background");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;
        
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Thêm MenuBackground
        MenuBackground menuBg = canvasGO.AddComponent<MenuBackground>();
        menuBg.autoSetSortOrder = true;
        menuBg.sortOrder = -100;
        menuBg.useOverlay = true;
        
        // Đăng ký Undo
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Background Canvas");
        
        Selection.activeGameObject = canvasGO;
        
        EditorUtility.DisplayDialog("Thành công!", 
            "Đã tạo Canvas_Background!\n\n" +
            "Sort Order = -100 (nằm dưới các Canvas khác)\n\n" +
            "Bây giờ kéo ảnh vào 'Background Sprite' trong Inspector.", "OK");
    }
    
    [MenuItem("Tools/Menu Background/Remove from Selected")]
    public static void RemoveFromSelected()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn một object!", "OK");
            return;
        }
        
        MenuBackground menuBg = selected.GetComponent<MenuBackground>();
        if (menuBg == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", 
                "Object này không có MenuBackground!", "OK");
            return;
        }
        
        // Xóa background container nếu có
        Transform bgContainer = selected.transform.Find("_MenuBackground_");
        if (bgContainer != null)
        {
            Undo.DestroyObjectImmediate(bgContainer.gameObject);
        }
        
        Undo.DestroyObjectImmediate(menuBg);
        
        EditorUtility.DisplayDialog("Đã xóa", "MenuBackground đã được xóa!", "OK");
    }
    
    [MenuItem("Tools/Menu Background/Fix Background Layering")]
    public static void FixBackgroundLayering()
    {
        // Tìm tất cả MenuBackground trong scene
        MenuBackground[] allMenuBgs = Object.FindObjectsOfType<MenuBackground>();
        
        if (allMenuBgs.Length == 0)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", 
                "Không có MenuBackground nào trong scene!", "OK");
            return;
        }
        
        int fixedCount = 0;
        
        foreach (MenuBackground menuBg in allMenuBgs)
        {
            // Fix 1: Đảm bảo _MenuBackground_ là sibling đầu tiên
            Transform bgContainer = menuBg.transform.Find("_MenuBackground_");
            if (bgContainer != null)
            {
                bgContainer.SetAsFirstSibling();
                fixedCount++;
            }
            
            // Fix 2: Set sort order cho Canvas
            Canvas canvas = menuBg.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = menuBg.sortOrder;
            }
        }
        
        // Tìm tất cả Canvas khác và đảm bảo sort order > -100
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>();
        int canvasFixed = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            // Bỏ qua canvas có MenuBackground
            if (canvas.GetComponent<MenuBackground>() != null) continue;
            
            // Nếu sort order <= -100, tăng lên 0
            if (canvas.sortingOrder <= -100)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 0;
                canvasFixed++;
                EditorUtility.SetDirty(canvas);
            }
        }
        
        // Đánh dấu scene đã thay đổi
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            $"Đã fix {fixedCount} MenuBackground!\n" +
            $"Đã điều chỉnh {canvasFixed} Canvas khác.\n\n" +
            "Background giờ sẽ nằm phía sau UI.", "OK");
    }
    
    [MenuItem("Tools/Menu Background/Recreate Background")]
    public static void RecreateBackground()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn object có MenuBackground!", "OK");
            return;
        }
        
        MenuBackground menuBg = selected.GetComponent<MenuBackground>();
        if (menuBg == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Object này không có MenuBackground!", "OK");
            return;
        }
        
        // Xóa và tạo lại
        menuBg.CreateBackground();
        
        EditorUtility.DisplayDialog("Thành công!", 
            "Đã tạo lại background!\n" +
            "_MenuBackground_ giờ nằm đầu tiên trong hierarchy.", "OK");
    }
}
#endif
