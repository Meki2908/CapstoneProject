using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Tool tạo Background Canvas riêng biệt - CHẮC CHẮN nằm phía sau UI
/// </summary>
public class SimpleBackgroundTool
{
    [MenuItem("Tools/Simple Background/Create Background Canvas")]
    public static void CreateBackgroundCanvas()
    {
        // Kiểm tra đã có chưa
        GameObject existing = GameObject.Find("___SCENE_BACKGROUND___");
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorUtility.DisplayDialog("Đã tồn tại", 
                "Background Canvas đã có!\nĐang chọn nó cho bạn.\n\n" +
                "Nếu muốn tạo lại, hãy xóa nó trước.", "OK");
            return;
        }
        

        
        // Tạo Canvas mới ở ROOT level
        GameObject bgCanvas = new GameObject("___SCENE_BACKGROUND___");
        
        // Thêm Canvas component - dùng WORLD SPACE để tương thích với Main_Menu
        Canvas canvas = bgCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Tìm Camera của Main_Menu
        GameObject mainMenu = GameObject.Find("Main_Menu");
        Camera uiCamera = null;
        if (mainMenu != null)
        {
            Canvas mainCanvas = mainMenu.GetComponent<Canvas>();
            if (mainCanvas != null && mainCanvas.worldCamera != null)
            {
                uiCamera = mainCanvas.worldCamera;
            }
        }
        
        // Nếu không tìm thấy, tìm Camera có tên chứa "Camera"
        if (uiCamera == null)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera cam in cameras)
            {
                if (cam.name.Contains("Camera"))
                {
                    uiCamera = cam;
                    break;
                }
            }
        }
        
        // Nếu vẫn không tìm thấy, dùng main camera
        if (uiCamera == null)
        {
            uiCamera = Camera.main;
        }
        
        canvas.worldCamera = uiCamera;
        
        // Thêm CanvasScaler
        CanvasScaler scaler = bgCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 1;
        
        // Setup RectTransform cho World Space (giống Main_Menu)
        RectTransform canvasRect = bgCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1920, 1080);
        
        // Đặt position phía SAU Main_Menu (Z cao hơn = xa camera hơn = phía sau)
        if (mainMenu != null)
        {
            Vector3 menuPos = mainMenu.transform.position;
            bgCanvas.transform.position = new Vector3(menuPos.x, menuPos.y, menuPos.z + 10f);
            bgCanvas.transform.rotation = mainMenu.transform.rotation;
            bgCanvas.transform.localScale = mainMenu.transform.localScale;
        }
        else
        {
            bgCanvas.transform.position = new Vector3(0, 0, 110f); // Xa camera hơn
        }
        
        // KHÔNG thêm GraphicRaycaster để không block input
        
        // Tạo Image background
        GameObject imageObj = new GameObject("BG_Image");
        imageObj.transform.SetParent(bgCanvas.transform, false);
        
        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image img = imageObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Màu tối mặc định
        img.raycastTarget = false; // Không block input
        
        // Đăng ký Undo
        Undo.RegisterCreatedObjectUndo(bgCanvas, "Create Background Canvas");
        
        // TỰ ĐỘNG FIX: Đặt Sort Order = 0 cho Main_Menu
        if (mainMenu != null)
        {
            Canvas mainCanvas = mainMenu.GetComponent<Canvas>();
            if (mainCanvas != null)
            {
                mainCanvas.sortingOrder = 0;
                EditorUtility.SetDirty(mainCanvas);
            }
        }
        
        // Fix tất cả Canvas khác có sort order quá thấp
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas c in allCanvases)
        {
            if (c.gameObject.name.Contains("SCENE_BACKGROUND")) continue;
            if (c.sortingOrder < -1000)
            {
                c.sortingOrder = 0;
                EditorUtility.SetDirty(c);
            }
        }
        
        // Đánh dấu scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        Selection.activeGameObject = bgCanvas;
        
        EditorUtility.DisplayDialog("Thành công!", 
            "Đã tạo ___SCENE_BACKGROUND___!\n\n" +
            "• Sort Order = -32000 (cực thấp)\n" +
            "• Main_Menu Sort Order = 0 (hiện trước)\n" +
            "• Kéo Sprite vào BG_Image > Image > Source Image\n" +
            "• Đổi Color thành trắng nếu dùng sprite\n\n" +
            "Background nằm phía sau UI!", "OK");
    }
    
    [MenuItem("Tools/Simple Background/Fix All Canvas Sort Orders")]
    public static void FixAllCanvasSortOrders()
    {
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        int fixedCount = 0;
        
        foreach (Canvas canvas in allCanvases)
        {
            // Bỏ qua background canvas
            if (canvas.gameObject.name.Contains("BACKGROUND")) continue;
            
            // Đảm bảo các UI canvas có sort order > -32000
            if (canvas.sortingOrder <= -1000)
            {
                canvas.sortingOrder = 0;
                EditorUtility.SetDirty(canvas);
                fixedCount++;
            }
        }
        
        // Đánh dấu scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Hoàn thành!", 
            $"Đã fix {fixedCount} Canvas!\n" +
            "Tất cả UI Canvas giờ có Sort Order >= 0", "OK");
    }
    
    [MenuItem("Tools/Simple Background/Set Image from Selected Sprite")]
    public static void SetImageFromSelectedSprite()
    {
        // Tìm background
        GameObject bg = GameObject.Find("___SCENE_BACKGROUND___");
        if (bg == null)
        {
            EditorUtility.DisplayDialog("Lỗi", 
                "Chưa có ___SCENE_BACKGROUND___!\n" +
                "Hãy tạo bằng 'Create Background Canvas' trước.", "OK");
            return;
        }
        
        // Tìm BG_Image
        Transform imgTrans = bg.transform.Find("BG_Image");
        if (imgTrans == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy BG_Image!", "OK");
            return;
        }
        
        Image img = imgTrans.GetComponent<Image>();
        if (img == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "BG_Image không có Image component!", "OK");
            return;
        }
        
        // Lấy sprite đang được chọn trong Project
        Sprite selectedSprite = null;
        foreach (Object obj in Selection.objects)
        {
            if (obj is Sprite s)
            {
                selectedSprite = s;
                break;
            }
            if (obj is Texture2D tex)
            {
                // Convert texture to sprite path
                string path = AssetDatabase.GetAssetPath(tex);
                selectedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                break;
            }
        }
        
        if (selectedSprite == null)
        {
            EditorUtility.DisplayDialog("Lỗi", 
                "Vui lòng chọn một Sprite trong Project!\n" +
                "(Click vào sprite trong Project window trước)", "OK");
            return;
        }
        
        // Set sprite
        img.sprite = selectedSprite;
        img.color = Color.white;
        EditorUtility.SetDirty(img);
        
        // Đánh dấu scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("Thành công!", 
            $"Đã set sprite: {selectedSprite.name}\n" +
            "Background đã được cập nhật!", "OK");
    }
    
    [MenuItem("Tools/Simple Background/Remove Background")]
    public static void RemoveBackground()
    {
        GameObject bg = GameObject.Find("___SCENE_BACKGROUND___");
        if (bg == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", 
                "Không có ___SCENE_BACKGROUND___ trong scene!", "OK");
            return;
        }
        
        Undo.DestroyObjectImmediate(bg);
        
        EditorUtility.DisplayDialog("Đã xóa", "Background đã được xóa!", "OK");
    }
}
#endif
