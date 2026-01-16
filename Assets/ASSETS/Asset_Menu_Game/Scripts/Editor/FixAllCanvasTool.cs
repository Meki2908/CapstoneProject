using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Tool FIX TOÀN BỘ - đảm bảo background nằm dưới UI
/// </summary>
public class FixAllCanvasTool
{
    [MenuItem("Tools/FIX ALL - Background Behind UI")]
    public static void FixAllCanvas()
    {
        // Tìm tất cả Canvas trong scene
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        
        if (allCanvases.Length == 0)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", "Không có Canvas nào trong scene!", "OK");
            return;
        }
        
        int backgroundCount = 0;
        int uiCount = 0;
        string report = "=== KẾT QUẢ FIX ===\n\n";
        
        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name.ToLower();
            
            // Kiểm tra xem có phải background canvas không
            bool isBackground = canvasName.Contains("background") || 
                               canvasName.Contains("___background") ||
                               canvasName.Contains("bg_");
            
            if (isBackground)
            {
                // Background: Sort Order = -32000
                canvas.overrideSorting = true;
                canvas.sortingOrder = -32000;
                backgroundCount++;
                report += $"[BG] {canvas.gameObject.name} → Sort Order: -32000\n";
            }
            else
            {
                // UI khác: Sort Order >= 0
                canvas.overrideSorting = true;
                if (canvas.sortingOrder < 0)
                {
                    canvas.sortingOrder = 0;
                }
                uiCount++;
                report += $"[UI] {canvas.gameObject.name} → Sort Order: {canvas.sortingOrder}\n";
            }
            
            EditorUtility.SetDirty(canvas);
        }
        
        // Đánh dấu scene thay đổi
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        report += $"\n=== TỔNG KẾT ===\n";
        report += $"Background Canvas: {backgroundCount}\n";
        report += $"UI Canvas: {uiCount}\n";
        
        Debug.Log(report);
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            $"Đã fix {allCanvases.Length} Canvas!\n\n" +
            $"• Background: {backgroundCount} (Sort Order: -32000)\n" +
            $"• UI: {uiCount} (Sort Order: >= 0)\n\n" +
            "Xem Console để biết chi tiết.\n" +
            "Nhớ Save Scene (Ctrl+S)!", "OK");
    }
    
    [MenuItem("Tools/Set Selected Canvas to Background")]
    public static void SetAsBackground()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Chọn một Canvas!", "OK");
            return;
        }
        
        Canvas canvas = selected.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = selected.GetComponentInParent<Canvas>();
        }
        
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Object không có Canvas!", "OK");
            return;
        }
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = -32000;
        EditorUtility.SetDirty(canvas);
        
        EditorUtility.DisplayDialog("Đã set", 
            $"{canvas.gameObject.name}\n→ Sort Order = -32000\n\nĐây là background canvas.", "OK");
    }
    
    [MenuItem("Tools/Set Selected Canvas to UI (Front)")]
    public static void SetAsUI()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Chọn một Canvas!", "OK");
            return;
        }
        
        Canvas canvas = selected.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = selected.GetComponentInParent<Canvas>();
        }
        
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Object không có Canvas!", "OK");
            return;
        }
        
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        EditorUtility.SetDirty(canvas);
        
        EditorUtility.DisplayDialog("Đã set", 
            $"{canvas.gameObject.name}\n→ Sort Order = 100\n\nĐây là UI canvas (phía trước).", "OK");
    }
    
    [MenuItem("Tools/Show All Canvas Sort Orders")]
    public static void ShowAllCanvasSortOrders()
    {
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        
        string report = "=== TẤT CẢ CANVAS TRONG SCENE ===\n\n";
        
        foreach (Canvas canvas in allCanvases)
        {
            report += $"• {canvas.gameObject.name}\n";
            report += $"  Override: {canvas.overrideSorting}, Sort Order: {canvas.sortingOrder}\n";
            report += $"  Render Mode: {canvas.renderMode}\n\n";
        }
        
        Debug.Log(report);
        EditorUtility.DisplayDialog("Danh sách Canvas", 
            $"Tìm thấy {allCanvases.Length} Canvas.\n\nXem Console (Ctrl+Shift+C) để biết chi tiết.", "OK");
    }
}
#endif
