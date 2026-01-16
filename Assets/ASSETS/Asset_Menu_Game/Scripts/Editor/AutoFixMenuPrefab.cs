using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

/// <summary>
/// Script tự động chạy khi mở Unity để fix prefab Main Menu
/// Chỉ cần đặt file này trong thư mục Editor
/// </summary>
public class AutoFixMenuPrefab
{
#if UNITY_EDITOR
    
    // Chạy tự động khi scripts được compile xong
    [InitializeOnLoadMethod]
    static void OnScriptsReloaded()
    {
        // Đợi 1 frame để đảm bảo Unity đã load xong
        EditorApplication.delayCall += () =>
        {
            // Kiểm tra xem đã fix chưa bằng EditorPrefs
            string fixKey = "MainMenuPrefabFixed_v1";
            if (!EditorPrefs.GetBool(fixKey, false))
            {
                // Hỏi user có muốn fix không
                if (EditorUtility.DisplayDialog("Fix Text Over Image", 
                    "Phát hiện prefab Main Menu chưa được fix.\n\nBạn có muốn fix text hiển thị trên image không?", 
                    "Fix Now", "Later"))
                {
                    FixMainMenuPrefab();
                    EditorPrefs.SetBool(fixKey, true);
                }
            }
        };
    }
    
    static void FixMainMenuPrefab()
    {
        string prefabPath = "Assets/Assets/Asset_Menu_Game/Modern Menu 1/Prefabs/Canvas Templates/Canvas_DefaultTemplate1.prefab";
        
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[AutoFixMenuPrefab] Cannot find prefab at: {prefabPath}");
            return;
        }
        
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        
        int fixedCount = 0;
        
        // Fix TextMeshProUGUI
        TextMeshProUGUI[] tmpTexts = prefabRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in tmpTexts)
        {
            Canvas textCanvas = text.GetComponent<Canvas>();
            if (textCanvas == null)
            {
                textCanvas = text.gameObject.AddComponent<Canvas>();
                textCanvas.overrideSorting = true;
                textCanvas.sortingOrder = 10;
                
                if (text.GetComponent<GraphicRaycaster>() == null)
                {
                    text.gameObject.AddComponent<GraphicRaycaster>();
                }
                fixedCount++;
            }
        }
        
        // Fix TextMeshPro (3D)
        TextMeshPro[] tmp3DTexts = prefabRoot.GetComponentsInChildren<TextMeshPro>(true);
        foreach (var text in tmp3DTexts)
        {
            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sortingOrder < 10)
            {
                renderer.sortingOrder = 10;
                fixedCount++;
            }
        }
        
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        
        AssetDatabase.SaveAssets();
        
        Debug.Log($"[AutoFixMenuPrefab] Fixed {fixedCount} text components. Text will now display over images.");
        
        EditorUtility.DisplayDialog("Fix Complete", 
            $"Đã fix {fixedCount} text components.\nText sẽ hiển thị trên Image.", "OK");
    }
    
    // Menu item để reset và chạy lại
    [MenuItem("Tools/UI Fix/Reset and Re-fix Main Menu Prefab")]
    static void ResetAndRefix()
    {
        EditorPrefs.DeleteKey("MainMenuPrefabFixed_v1");
        FixMainMenuPrefab();
        EditorPrefs.SetBool("MainMenuPrefabFixed_v1", true);
    }
#endif
}
