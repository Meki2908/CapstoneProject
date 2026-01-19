using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// FIX NGAY - Bật Override Sorting cho tất cả Canvas
/// </summary>
public class EnableOverrideTool
{
    [MenuItem("Tools/!!! FIX NGAY - Enable Override Sorting !!!")]
    public static void FixNow()
    {
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        
        int fixedCount = 0;
        string report = "";
        
        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name.ToLower();
            
            // Bật Override Sorting
            canvas.overrideSorting = true;
            
            // Set Sort Order
            if (canvasName.Contains("background") || canvasName.Contains("___background"))
            {
                canvas.sortingOrder = -32000;
                report += $"[BG] {canvas.gameObject.name} → -32000\n";
            }
            else
            {
                // Đảm bảo UI có sort order cao hơn -32000
                if (canvas.sortingOrder <= -32000)
                {
                    canvas.sortingOrder = 0;
                }
                report += $"[UI] {canvas.gameObject.name} → {canvas.sortingOrder}\n";
            }
            
            EditorUtility.SetDirty(canvas);
            fixedCount++;
        }
        
        // Save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Debug.Log("=== FIX RESULT ===\n" + report);
        
        EditorUtility.DisplayDialog("Đã Fix!", 
            $"Đã bật Override Sorting cho {fixedCount} Canvas!\n\n" +
            "Background sẽ nằm phía sau UI.\n\n" +
            "⚠️ Nhớ SAVE SCENE (Ctrl+S)!", "OK");
    }
    
    [MenuItem("Tools/Force Background to Back")]
    public static void ForceBackgroundToBack()
    {
        // Tìm background canvas
        GameObject bgCanvas = GameObject.Find("___BACKGROUND_CANVAS___");
        if (bgCanvas == null)
        {
            bgCanvas = GameObject.Find("BackgroundCanvas");
        }
        
        if (bgCanvas == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", "Không có Background Canvas!", "OK");
            return;
        }
        
        Canvas canvas = bgCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = -32000;
            EditorUtility.SetDirty(canvas);
            
            Debug.Log($"[FIX] {bgCanvas.name}: Override=true, SortOrder=-32000");
        }
        
        // Tìm và fix tất cả Canvas UI khác
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject == bgCanvas) continue;
            
            c.overrideSorting = true;
            if (c.sortingOrder < 0)
            {
                c.sortingOrder = Mathf.Max(c.sortingOrder, 0);
            }
            EditorUtility.SetDirty(c);
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Đã Fix!", 
            "Background Canvas → Sort Order: -32000\n" +
            "Các Canvas khác → Sort Order >= 0\n\n" +
            "SAVE SCENE để lưu thay đổi!", "OK");
    }
}
#endif
