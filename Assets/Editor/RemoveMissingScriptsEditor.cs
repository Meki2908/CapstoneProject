using UnityEngine;
using UnityEditor;

public class RemoveMissingScriptsEditor : Editor
{
    [MenuItem("Tools/Remove Missing Scripts In Selected Prefab")]
    public static void RemoveMissingScripts()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Please select a GameObject (or the root of your Prefab) first.");
            return;
        }

        int totalRemoved = 0;
        foreach (GameObject go in selectedObjects)
        {
            // This works recursively on the GameObject and all its children
            int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            totalRemoved += removedCount;
            
            if (removedCount > 0)
            {
                Debug.Log($"Removed {removedCount} missing scripts from {go.name} and its children.");
                EditorUtility.SetDirty(go);
            }
        }

        if (totalRemoved > 0)
        {
            Debug.Log($"<color=green>Successfully removed a total of {totalRemoved} missing scripts!</color>");
            AssetDatabase.SaveAssets();
        }
        else
        {
            Debug.Log("No missing scripts found on the selected objects.");
        }
    }
}
