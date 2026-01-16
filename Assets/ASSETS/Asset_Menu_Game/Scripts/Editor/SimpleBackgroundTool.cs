using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Tool đơn giản để thêm Background vào Main_Menu
/// Cách dùng: Tools > Add Background to Main Menu
/// </summary>
public class SimpleBackgroundTool
{
    [MenuItem("Tools/Add Background to Main Menu")]
    public static void AddBackgroundToMainMenu()
    {
        // Tìm Main_Menu trong scene
        GameObject mainMenu = GameObject.Find("Main_Menu");
        
        if (mainMenu == null)
        {
            // Tìm theo tên khác
            mainMenu = GameObject.Find("Canvas_Main_Menu");
            if (mainMenu == null)
            {
                mainMenu = GameObject.Find("MainMenu");
            }
        }
        
        if (mainMenu == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", 
                "Không tìm thấy Main_Menu trong scene!\n\n" +
                "Hãy dùng 'Add Background to Selected' thay thế.", "OK");
            return;
        }
        
        AddBackgroundToObject(mainMenu);
    }
    
    [MenuItem("Tools/Add Background to Selected")]
    public static void AddBackgroundToSelected()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn một object trong Hierarchy!", "OK");
            return;
        }
        
        AddBackgroundToObject(selected);
    }
    
    static void AddBackgroundToObject(GameObject target)
    {
        // Kiểm tra đã có background chưa
        Transform existingBg = target.transform.Find("===BACKGROUND===");
        if (existingBg != null)
        {
            Selection.activeGameObject = existingBg.gameObject;
            EditorUtility.DisplayDialog("Đã có sẵn", 
                "Background đã tồn tại!\n\n" +
                "Đang chọn nó cho bạn. Kéo ảnh vào component Image.", "OK");
            return;
        }
        
        // Tạo Background Image
        GameObject bgObj = new GameObject("===BACKGROUND===");
        Undo.RegisterCreatedObjectUndo(bgObj, "Add Background");
        
        bgObj.transform.SetParent(target.transform);
        bgObj.transform.SetAsFirstSibling(); // ĐẶT ĐẦU TIÊN = nằm dưới cùng
        
        // Setup RectTransform để stretch full màn hình
        RectTransform rect = bgObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        
        // Thêm Image component
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.white; // TRẮNG để sprite hiển thị đúng
        bgImage.raycastTarget = false;
        bgImage.preserveAspect = false; // Stretch để fill màn hình
        
        // Log để debug
        Debug.Log("[Background] Đã tạo ===BACKGROUND=== tại index 0. Kéo sprite vào Source Image trong Inspector.");
        Debug.Log("[Background] Nếu không thấy, kiểm tra: 1) Sprite đã import đúng chưa 2) Color = White 3) Canvas có Render Mode đúng không");
        
        // Chọn object mới tạo
        Selection.activeGameObject = bgObj;
        
        EditorUtility.DisplayDialog("Thành công!", 
            "Đã thêm ===BACKGROUND=== vào " + target.name + "!\n\n" +
            "Bây giờ:\n" +
            "1. Chọn ===BACKGROUND=== trong Hierarchy\n" +
            "2. Trong Inspector, tìm component 'Image'\n" +
            "3. Kéo sprite vào field 'Source Image'\n\n" +
            "Background sẽ nằm phía sau tất cả UI!", "OK");
    }
    
    [MenuItem("Tools/Remove Background from Selected")]
    public static void RemoveBackground()
    {
        GameObject selected = Selection.activeGameObject;
        
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn object chứa background!", "OK");
            return;
        }
        
        Transform bg = selected.transform.Find("===BACKGROUND===");
        if (bg == null)
        {
            EditorUtility.DisplayDialog("Không tìm thấy", 
                "Object này không có ===BACKGROUND===!", "OK");
            return;
        }
        
        Undo.DestroyObjectImmediate(bg.gameObject);
        EditorUtility.DisplayDialog("Đã xóa", "Background đã được xóa!", "OK");
    }
}
#endif
