using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Fix Text bị ẩn sau khi thay đổi Sort Order
/// </summary>
public class FixTextTool
{
    [MenuItem("Tools/!!! FIX TEXT - Show Hidden Text !!!")]
    public static void FixHiddenText()
    {
        int fixedCount = 0;
        
        // 1. Fix tất cả TextMeshProUGUI (UI Text)
        TextMeshProUGUI[] allTexts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var text in allTexts)
        {
            // Đảm bảo text hiển thị
            CanvasRenderer cr = text.GetComponent<CanvasRenderer>();
            if (cr != null)
            {
                cr.cullTransparentMesh = false;
            }
            EditorUtility.SetDirty(text);
            fixedCount++;
        }
        
        // 2. Fix tất cả TextMeshPro (3D/WorldSpace Text)
        TextMeshPro[] allTMP = Object.FindObjectsOfType<TextMeshPro>(true);
        foreach (var text in allTMP)
        {
            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 1000; // Cao nhất
                EditorUtility.SetDirty(renderer);
            }
            fixedCount++;
        }
        
        // 3. Fix tất cả Unity UI Text (legacy)
        UnityEngine.UI.Text[] legacyTexts = Object.FindObjectsOfType<UnityEngine.UI.Text>(true);
        foreach (var text in legacyTexts)
        {
            CanvasRenderer cr = text.GetComponent<CanvasRenderer>();
            if (cr != null)
            {
                cr.cullTransparentMesh = false;
            }
            EditorUtility.SetDirty(text);
            fixedCount++;
        }
        
        // 4. Tìm tất cả Canvas chứa text và set sort order cao
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in allCanvases)
        {
            string name = canvas.gameObject.name.ToLower();
            if (name.Contains("background") || name.Contains("___background")) continue;
            
            // Kiểm tra có text con không
            bool hasText = canvas.GetComponentInChildren<TextMeshProUGUI>() != null ||
                          canvas.GetComponentInChildren<TextMeshPro>() != null ||
                          canvas.GetComponentInChildren<UnityEngine.UI.Text>() != null;
            
            if (hasText || name.Contains("text") || name.Contains("label") || name.Contains("prompt"))
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 500);
                EditorUtility.SetDirty(canvas);
            }
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log($"[FIX TEXT] Fixed {fixedCount} text elements and updated Canvas sort orders");
        
        EditorUtility.DisplayDialog("Đã Fix!", 
            $"Đã fix {fixedCount} text elements!\n\n" +
            "• TextMeshPro 3D → Sort Order: 1000\n" +
            "• Canvas chứa text → Sort Order: 500+\n\n" +
            "SAVE SCENE (Ctrl+S)!", "OK");
    }
    
    [MenuItem("Tools/Reset All Canvas Sort Orders")]
    public static void ResetAllSortOrders()
    {
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        
        foreach (Canvas canvas in allCanvases)
        {
            string name = canvas.gameObject.name.ToLower();
            
            canvas.overrideSorting = true;
            
            if (name.Contains("background") || name.Contains("___background"))
            {
                canvas.sortingOrder = -32000;
            }
            else if (name.Contains("loading"))
            {
                canvas.sortingOrder = 500;
            }
            else if (name.Contains("text") || name.Contains("label"))
            {
                canvas.sortingOrder = 200;
            }
            else
            {
                canvas.sortingOrder = 100;
            }
            
            EditorUtility.SetDirty(canvas);
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("=== RESET SORT ORDERS ===");
        foreach (Canvas c in allCanvases)
        {
            Debug.Log($"{c.gameObject.name} → {c.sortingOrder}");
        }
        
        EditorUtility.DisplayDialog("Đã Reset!", 
            "Đã reset Sort Order:\n" +
            "• Background: -32000\n" +
            "• UI chính: 100\n" +
            "• Text/Label: 200\n" +
            "• Loading: 500\n\n" +
            "SAVE SCENE!", "OK");
    }
    
    [MenuItem("Tools/Undo All Changes - Reset to Default")]
    public static void UndoAllChanges()
    {
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        
        foreach (Canvas canvas in allCanvases)
        {
            // Reset về mặc định
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
            EditorUtility.SetDirty(canvas);
        }
        
        // Xóa background canvas nếu có
        GameObject bgCanvas = GameObject.Find("___BACKGROUND_CANVAS___");
        if (bgCanvas != null)
        {
            Undo.DestroyObjectImmediate(bgCanvas);
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Đã Undo!", 
            "Đã reset tất cả Canvas về mặc định!\n" +
            "Override Sorting: OFF\n" +
            "Sort Order: 0\n\n" +
            "Background Canvas đã bị xóa.\n" +
            "SAVE SCENE!", "OK");
    }
}
#endif
