using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class DeepMissingScriptCleaner : EditorWindow
{
    [MenuItem("Tools/Force Clean Missing Scripts (Tuyệt đối sạch)")]
    public static void ShowWindow()
    {
        ForceCleanSelected();
    }

    public static void ForceCleanSelected()
    {
        GameObject[] selections = Selection.gameObjects;
        if (selections == null || selections.Length == 0)
        {
            Debug.LogWarning("[DeepCleaner] Vui lòng chọn GameObject hoặc Prefab từ cửa sổ Project/Hierarchy.");
            return;
        }

        int totalCleaned = 0;

        foreach (var go in selections)
        {
            string assetPath = AssetDatabase.GetAssetPath(go);
            bool isPrefab = !string.IsNullOrEmpty(assetPath);

            GameObject root = go;
            if (isPrefab)
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
            }

            int cleaned = DeepCleanGameObjectTree(root);
            
            if (isPrefab)
            {
                if (cleaned > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                    Debug.Log($"[DeepCleaner] Đã ép xóa {cleaned} missing scripts khỏi Prefab: {assetPath}");
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                if (cleaned > 0)
                {
                    Debug.Log($"[DeepCleaner] Đã ép xóa {cleaned} missing scripts khỏi GameObject: {go.name}");
                }
            }

            totalCleaned += cleaned;
        }

        if (totalCleaned > 0)
            Debug.Log($"<b><color=magenta>HOÀN THÀNH!</color></b> Đã nhổ tận gốc {totalCleaned} missing scripts ngoan cố!");
        else
            Debug.Log("[DeepCleaner] Không phát hiện missing scripts nào.");
    }

    private static int DeepCleanGameObjectTree(GameObject go)
    {
        int count = 0;
        
        // Use SerializedObject to bypass Unity's prefab override limitations
        SerializedObject so = new SerializedObject(go);
        SerializedProperty componentsProp = so.FindProperty("m_Component");
        
        if (componentsProp != null)
        {
            int i = 0;
            // Iterate backwards to safely delete
            for (i = componentsProp.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty componentProp = componentsProp.GetArrayElementAtIndex(i).FindPropertyRelative("component");
                
                if (componentProp.objectReferenceValue == null && componentProp.objectReferenceInstanceIDValue != 0)
                {
                    // This is a missing script!
                    componentsProp.DeleteArrayElementAtIndex(i);
                    count++;
                }
            }
            
            if (count > 0)
            {
                so.ApplyModifiedProperties();
            }
        }
        
        // Also call the standard utility just in case
        count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        // Recursively clean children
        foreach (Transform child in go.transform)
        {
            count += DeepCleanGameObjectTree(child.gameObject);
        }

        return count;
    }
}
