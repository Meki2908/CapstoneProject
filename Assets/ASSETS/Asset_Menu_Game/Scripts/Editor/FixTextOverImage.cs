using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Editor Tool để fix text bị đè bởi Image trong UI
/// Hỗ trợ cả Scene và Prefab
/// </summary>
public class FixTextOverImage : MonoBehaviour
{
#if UNITY_EDITOR
    
    [MenuItem("Tools/UI Fix/Fix Main Menu Prefab (Canvas_DefaultTemplate1)")]
    public static void FixMainMenuPrefab()
    {
        string prefabPath = "Assets/Assets/Asset_Menu_Game/Modern Menu 1/Prefabs/Canvas Templates/Canvas_DefaultTemplate1.prefab";
        
        // Load prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error", $"Cannot find prefab at:\n{prefabPath}", "OK");
            return;
        }
        
        // Open prefab for editing
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        
        int fixedCount = 0;
        
        // Fix TextMeshProUGUI (UI Text)
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
                Debug.Log($"Fixed UI Text: {text.gameObject.name}");
            }
        }
        
        // Fix TextMeshPro (3D Text) - set sorting order in renderer
        TextMeshPro[] tmp3DTexts = prefabRoot.GetComponentsInChildren<TextMeshPro>(true);
        foreach (var text in tmp3DTexts)
        {
            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sortingOrder < 10)
            {
                renderer.sortingOrder = 10;
                fixedCount++;
                Debug.Log($"Fixed 3D Text: {text.gameObject.name}");
            }
        }
        
        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Fix Complete", 
            $"Fixed {fixedCount} text components in prefab.\nText will now display over images.", "OK");
    }
    
    [MenuItem("Tools/UI Fix/Fix All Text Over Image (Current Scene)")]
    public static void FixAllTextInScene()
    {
        // Tìm tất cả TextMeshProUGUI trong scene
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        int fixedCount = 0;
        
        foreach (var text in allTexts)
        {
            // Kiểm tra xem text có parent là Button hoặc có Image sibling không
            Transform parent = text.transform.parent;
            if (parent != null)
            {
                Image parentImage = parent.GetComponent<Image>();
                Button parentButton = parent.GetComponent<Button>();
                
                if (parentImage != null || parentButton != null)
                {
                    // Thêm Canvas để text render trên
                    Canvas textCanvas = text.GetComponent<Canvas>();
                    if (textCanvas == null)
                    {
                        textCanvas = text.gameObject.AddComponent<Canvas>();
                        textCanvas.overrideSorting = true;
                        textCanvas.sortingOrder = 1;
                        
                        // Thêm GraphicRaycaster
                        if (text.GetComponent<GraphicRaycaster>() == null)
                        {
                            text.gameObject.AddComponent<GraphicRaycaster>();
                        }
                        
                        EditorUtility.SetDirty(text.gameObject);
                        fixedCount++;
                    }
                }
            }
        }
        
        // Tìm tất cả Text (Legacy) trong scene
        Text[] allLegacyTexts = FindObjectsOfType<Text>(true);
        foreach (var text in allLegacyTexts)
        {
            Transform parent = text.transform.parent;
            if (parent != null)
            {
                Image parentImage = parent.GetComponent<Image>();
                Button parentButton = parent.GetComponent<Button>();
                
                if (parentImage != null || parentButton != null)
                {
                    Canvas textCanvas = text.GetComponent<Canvas>();
                    if (textCanvas == null)
                    {
                        textCanvas = text.gameObject.AddComponent<Canvas>();
                        textCanvas.overrideSorting = true;
                        textCanvas.sortingOrder = 1;
                        
                        if (text.GetComponent<GraphicRaycaster>() == null)
                        {
                            text.gameObject.AddComponent<GraphicRaycaster>();
                        }
                        
                        EditorUtility.SetDirty(text.gameObject);
                        fixedCount++;
                    }
                }
            }
        }
        
        Debug.Log($"[FixTextOverImage] Fixed {fixedCount} text components to display over images.");
        
        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog("Fix Complete", 
                $"Fixed {fixedCount} text components.\nPlease save your scene/prefab.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No Changes", 
                "All text components are already configured correctly.", "OK");
        }
    }
    
    [MenuItem("Tools/UI Fix/Fix Selected Text Over Image")]
    public static void FixSelectedText()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        int fixedCount = 0;
        
        foreach (var obj in selectedObjects)
        {
            // Tìm tất cả text trong object được chọn
            TextMeshProUGUI[] texts = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                Canvas textCanvas = text.GetComponent<Canvas>();
                if (textCanvas == null)
                {
                    textCanvas = text.gameObject.AddComponent<Canvas>();
                    textCanvas.overrideSorting = true;
                    textCanvas.sortingOrder = 1;
                    
                    if (text.GetComponent<GraphicRaycaster>() == null)
                    {
                        text.gameObject.AddComponent<GraphicRaycaster>();
                    }
                    
                    EditorUtility.SetDirty(text.gameObject);
                    fixedCount++;
                }
            }
            
            Text[] legacyTexts = obj.GetComponentsInChildren<Text>(true);
            foreach (var text in legacyTexts)
            {
                Canvas textCanvas = text.GetComponent<Canvas>();
                if (textCanvas == null)
                {
                    textCanvas = text.gameObject.AddComponent<Canvas>();
                    textCanvas.overrideSorting = true;
                    textCanvas.sortingOrder = 1;
                    
                    if (text.GetComponent<GraphicRaycaster>() == null)
                    {
                        text.gameObject.AddComponent<GraphicRaycaster>();
                    }
                    
                    EditorUtility.SetDirty(text.gameObject);
                    fixedCount++;
                }
            }
        }
        
        Debug.Log($"[FixTextOverImage] Fixed {fixedCount} text components in selected objects.");
        
        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog("Fix Complete", 
                $"Fixed {fixedCount} text components.\nPlease save your scene/prefab.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No Changes", 
                "No text components need fixing in selection.", "OK");
        }
    }
#endif
}
