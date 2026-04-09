using UnityEditor;
using UnityEngine;
using Fusion;

/// <summary>
/// Editor tool: Tự động thêm các component multiplayer cho kẻ địch (Enemy).
/// Chạy: Tools → Setup Enemy Mutiplayer (One-Click)
/// </summary>
public class SetupEnemyPrefab : EditorWindow
{
    [MenuItem("Tools/Setup All Enemy Prefabs (Multiplayer)")]
    static void SetupAllEnemies()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Lọc các prefab liên quan đến Enemy
            if (!path.Contains("Enemy") && !path.Contains("Demon") && !path.Contains("Ifrit") && !path.Contains("Lich"))
                continue;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            // Kiểm tra xem nó có phải Enemy không (dựa vào TakeDamageTest hoặc EnemyScript)
            if (prefabRoot.GetComponentInChildren<TakeDamageTest>(true) != null || 
                prefabRoot.GetComponentInChildren<EnemyScript>(true) != null)
            {
                bool modified = false;

                // Thêm NetworkObject
                if (prefabRoot.GetComponent<NetworkObject>() == null)
                {
                    prefabRoot.AddComponent<NetworkObject>();
                    modified = true;
                }

                // Thêm NetworkEnemyHealth
                if (prefabRoot.GetComponent<NetworkEnemyHealth>() == null)
                {
                    prefabRoot.AddComponent<NetworkEnemyHealth>();
                    modified = true;
                }

                // Thêm NetworkTransform để nhảy vị trí
                if (prefabRoot.GetComponent<NetworkTransform>() == null)
                {
                    prefabRoot.AddComponent<NetworkTransform>();
                    modified = true;
                }

                // Thêm NetworkAnimatorSync để truyền hoạt ảnh
                if (prefabRoot.GetComponent<NetworkAnimatorSync>() == null)
                {
                    prefabRoot.AddComponent<NetworkAnimatorSync>();
                    modified = true;
                }

                if (modified)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    Debug.Log($"[SetupEnemyPrefab] ✅ Đã upgrade: {path}");
                    count++;
                }
            }
            
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        EditorUtility.DisplayDialog("Setup Enemy Complete", $"Đã gắn NetworkObject và NetworkEnemyHealth vào {count} kẻ địch.\n\nHãy nhớ ấn Fusion -> Rebuild Object Table!", "OK");
    }
}
