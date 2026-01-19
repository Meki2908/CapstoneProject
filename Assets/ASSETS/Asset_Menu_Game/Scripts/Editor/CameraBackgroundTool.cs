using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Tool tạo Background sử dụng Camera - chắc chắn nằm phía sau mọi UI
/// </summary>
public class CameraBackgroundTool
{
    [MenuItem("Tools/Camera Background/Create Background Camera")]
    public static void CreateBackgroundCamera()
    {
        // Kiểm tra đã có chưa
        GameObject existing = GameObject.Find("BackgroundCamera");
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorUtility.DisplayDialog("Đã có sẵn", 
                "BackgroundCamera đã tồn tại!\n\nĐang chọn nó cho bạn.", "OK");
            return;
        }
        
        // === TẠO CAMERA RIÊNG CHO BACKGROUND ===
        GameObject camObj = new GameObject("BackgroundCamera");
        Undo.RegisterCreatedObjectUndo(camObj, "Create Background Camera");
        
        Camera bgCam = camObj.AddComponent<Camera>();
        bgCam.clearFlags = CameraClearFlags.Skybox;
        bgCam.cullingMask = 1 << 31; // Layer 31 - Background only
        bgCam.depth = -100; // Render trước Main Camera
        bgCam.orthographic = true;
        bgCam.orthographicSize = 5;
        
        // === TẠO CANVAS RIÊNG CHO BACKGROUND ===
        GameObject canvasObj = new GameObject("BackgroundCanvas");
        canvasObj.layer = 31; // Layer 31
        canvasObj.transform.SetParent(camObj.transform);
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Background Canvas");
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = bgCam;
        canvas.planeDistance = 10;
        canvas.sortingOrder = -1000;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // === TẠO IMAGE BACKGROUND ===
        GameObject imageObj = new GameObject("BackgroundImage");
        imageObj.layer = 31;
        imageObj.transform.SetParent(canvasObj.transform);
        
        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        
        Image bgImage = imageObj.AddComponent<Image>();
        bgImage.color = Color.white;
        bgImage.raycastTarget = false;
        
        Selection.activeGameObject = imageObj;
        
        EditorUtility.DisplayDialog("Thành công!", 
            "Đã tạo Background Camera + Canvas!\n\n" +
            "Cấu trúc:\n" +
            "• BackgroundCamera (Depth: -100)\n" +
            "  └── BackgroundCanvas\n" +
            "      └── BackgroundImage ← KÉO SPRITE VÀO ĐÂY\n\n" +
            "Background sẽ CHẮC CHẮN nằm phía sau mọi UI!", "OK");
    }
    
    [MenuItem("Tools/Camera Background/Remove Background Camera")]
    public static void RemoveBackgroundCamera()
    {
        GameObject bgCam = GameObject.Find("BackgroundCamera");
        if (bgCam == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", 
                "Không có BackgroundCamera trong scene!", "OK");
            return;
        }
        
        Undo.DestroyObjectImmediate(bgCam);
        EditorUtility.DisplayDialog("Đã xóa", "BackgroundCamera đã được xóa!", "OK");
    }
    
    [MenuItem("Tools/Camera Background/Setup Main Camera For Background")]
    public static void SetupMainCameraBackground()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Main Camera!", "OK");
            return;
        }
        
        // Tạo canvas render bằng Main Camera với sort order rất thấp
        GameObject canvasObj = new GameObject("___BACKGROUND_CANVAS___");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Background Canvas");
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -32000; // Sort order thấp nhất có thể
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Image
        GameObject imageObj = new GameObject("Image");
        imageObj.transform.SetParent(canvasObj.transform);
        
        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        
        Image bgImage = imageObj.AddComponent<Image>();
        bgImage.color = Color.white;
        bgImage.raycastTarget = false;
        
        Selection.activeGameObject = imageObj;
        
        EditorUtility.DisplayDialog("Thành công!", 
            "Đã tạo ___BACKGROUND_CANVAS___!\n\n" +
            "Sort Order = -32000 (thấp nhất)\n\n" +
            "Kéo sprite vào Image để thấy background.", "OK");
    }
}
#endif
