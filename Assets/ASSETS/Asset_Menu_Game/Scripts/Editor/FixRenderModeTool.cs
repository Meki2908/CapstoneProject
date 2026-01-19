using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// FIX Render Mode - Đảm bảo Background nằm dưới WorldSpace UI
/// </summary>
public class FixRenderModeTool
{
    [MenuItem("Tools/!!! FIX RENDER MODE - Background Behind WorldSpace !!!")]
    public static void FixRenderMode()
    {
        // Tìm background canvas
        GameObject bgCanvas = GameObject.Find("___BACKGROUND_CANVAS___");
        if (bgCanvas == null)
        {
            bgCanvas = GameObject.Find("BackgroundCanvas");
        }
        
        if (bgCanvas == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", 
                "Không có Background Canvas!\n\nDùng Tools > Camera Background > Setup Main Camera For Background trước.", "OK");
            return;
        }
        
        Canvas canvas = bgCanvas.GetComponent<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Object không có Canvas component!", "OK");
            return;
        }
        
        // Tìm Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            // Tìm camera khác
            mainCam = Object.FindObjectOfType<Camera>();
        }
        
        if (mainCam == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Camera!", "OK");
            return;
        }
        
        // Đổi Background sang ScreenSpaceCamera với depth thấp
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCam;
        canvas.planeDistance = 1000; // Xa camera = nằm sau
        canvas.overrideSorting = true;
        canvas.sortingOrder = -32000;
        
        EditorUtility.SetDirty(canvas);
        
        // Đảm bảo UI WorldSpace có sort order cao hơn
        Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject == bgCanvas) continue;
            
            if (c.renderMode == RenderMode.WorldSpace)
            {
                c.overrideSorting = true;
                c.sortingOrder = Mathf.Max(c.sortingOrder, 100);
                EditorUtility.SetDirty(c);
            }
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Đã Fix!", 
            "Background Canvas:\n" +
            "• Render Mode: ScreenSpaceCamera\n" +
            "• Plane Distance: 1000 (xa = nằm sau)\n" +
            "• Sort Order: -32000\n\n" +
            "WorldSpace UI → Sort Order: 100+\n\n" +
            "SAVE SCENE (Ctrl+S)!", "OK");
    }
    
    [MenuItem("Tools/Change Background to WorldSpace")]
    public static void ChangeToWorldSpace()
    {
        GameObject bgCanvas = GameObject.Find("___BACKGROUND_CANVAS___");
        if (bgCanvas == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", "Không có ___BACKGROUND_CANVAS___!", "OK");
            return;
        }
        
        Canvas canvas = bgCanvas.GetComponent<Canvas>();
        
        // Đổi sang WorldSpace
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -32000;
        
        // Đặt vị trí xa camera
        RectTransform rect = bgCanvas.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localPosition = new Vector3(0, 0, 1000); // Xa camera
            rect.sizeDelta = new Vector2(1920, 1080);
        }
        
        EditorUtility.SetDirty(canvas);
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Đã đổi!", 
            "Background Canvas → WorldSpace\n" +
            "Position Z: 1000 (xa camera)\n" +
            "Sort Order: -32000\n\n" +
            "SAVE SCENE!", "OK");
    }
}
#endif
