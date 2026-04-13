using UnityEditor;
using UnityEngine;

public class MissingScriptCleaner
{
    [MenuItem("Tools/Clean Missing Scripts from Selected")]
    public static void CleanSelected()
    {
        CleanMissingScripts(Selection.gameObjects);
    }

    [MenuItem("Tools/Clean Missing Scripts from ALL PREFABS (Dọn toàn bộ Project)")]
    public static void CleanAllPrefabs()
    {
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab");
        GameObject[] allPrefabs = new GameObject[allPrefabGuids.Length];
        
        for (int i = 0; i < allPrefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allPrefabGuids[i]);
            allPrefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        CleanMissingScripts(allPrefabs);
        Debug.Log($"[Trình dọn rác] Đã quét toàn bộ {allPrefabs.Length} prefabs trong Project.");
    }

    public static void CleanMissingScripts(GameObject[] selections)
    {
        if (selections == null || selections.Length == 0)
        {
            Debug.LogWarning("Vui lòng chọn ít nhất 1 object.");
            return;
        }

        int totalRemoved = 0;
        int filesModified = 0;

        foreach (var go in selections)
        {
            if (go == null) continue;

            string assetPath = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(assetPath)) 
            {
                // Is a scene object
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                CleanChildren(go.transform, ref removed);
                if (removed > 0)
                {
                    Debug.Log($"[Scene Object] Đã xóa {removed} missing scripts khỏi GameObject {go.name}.");
                    totalRemoved += removed;
                }
                continue;
            }

            // Is a prefab - We must use LoadPrefabContents to modify it
            try 
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
                if (prefabContents == null) continue;

                int removedCount = 0;
                
                removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabContents);
                CleanChildren(prefabContents.transform, ref removedCount);
                
                if (removedCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
                    Debug.Log($"[Prefab] Đã dọn {removedCount} missing scripts trên: {assetPath}");
                    totalRemoved += removedCount;
                    filesModified++;
                }

                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Không thể dọn rác file {assetPath}: {e.Message}");
            }
        }

        if (totalRemoved > 0)
        {
            Debug.Log($"<b><color=green>HOÀN TẤT DỌN RÁC!</color></b> Đã xóa tổng cộng {totalRemoved} missing scripts trên {filesModified} file Prefabs.");
        }
    }

    private static void CleanChildren(Transform parent, ref int removedCount)
    {
        foreach (Transform child in parent)
        {
            removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            CleanChildren(child, ref removedCount);
        }
    }
}
