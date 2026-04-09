using UnityEditor;
using UnityEngine;
using Fusion;

/// <summary>
/// Editor tool: Tự động thêm các component multiplayer cần thiết vào Player_3.0 prefab.
/// Chạy: Tools → Setup Player Prefab (Multiplayer)
/// </summary>
public class SetupPlayerPrefab : EditorWindow
{
    [MenuItem("Tools/Setup Player Prefab (Multiplayer)")]
    static void ShowWindow()
    {
        GetWindow<SetupPlayerPrefab>("Setup Player Prefab");
    }

    private GameObject playerPrefab;

    void OnGUI()
    {
        GUILayout.Label("🔧 Setup Player Prefab for Multiplayer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        playerPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Player Prefab", playerPrefab, typeof(GameObject), false);

        GUILayout.Space(5);

        // Auto-find Player_3.0 if not assigned
        if (playerPrefab == null)
        {
            string[] guids = AssetDatabase.FindAssets("Player_3.0 t:Prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                EditorGUILayout.HelpBox($"Auto-found: {path}", MessageType.Info);
            }
        }

        if (playerPrefab == null)
        {
            EditorGUILayout.HelpBox("Kéo Player_3.0 prefab vào đây.", MessageType.Warning);
            return;
        }

        GUILayout.Space(10);
        GUILayout.Label("📋 Component Status:", EditorStyles.boldLabel);

        string[] required = new string[] {
            "NetworkObject",
            "NetworkPlayerName",
            "NetworkPlayerLocalOwnership",
            "NetworkPlayerRootFollowBody",
            "NetworkPlayerSpawnSnap",
            "NetworkAnimatorSync",
            "NetworkPlayerStats",
            "NetworkPlayerDeathManager"
        };

        foreach (var name in required)
        {
            bool has = HasComponent(playerPrefab, name);
            string icon = has ? "✅" : "❌ MISSING";
            EditorGUILayout.LabelField($"  {icon}  {name}");
        }

        GUILayout.Space(15);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("⚡ SETUP ALL MISSING COMPONENTS", GUILayout.Height(40)))
        {
            SetupPrefab();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Sau khi setup, nhớ:\n" +
            "1. Fusion → Rebuild Object Table\n" +
            "2. Gán Player_3.0 vào MultiplayerManager → playerPrefab slot",
            MessageType.Info);
    }

    bool HasComponent(GameObject go, string typeName)
    {
        foreach (var c in go.GetComponents<Component>())
        {
            if (c != null && c.GetType().Name == typeName) return true;
        }
        return false;
    }

    void SetupPrefab()
    {
        if (playerPrefab == null) return;

        string prefabPath = AssetDatabase.GetAssetPath(playerPrefab);
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

        int added = 0;

        // 1. NetworkObject (BẮT BUỘC — phải thêm đầu tiên)
        if (prefabRoot.GetComponent<NetworkObject>() == null)
        {
            prefabRoot.AddComponent<NetworkObject>();
            Debug.Log("[SetupPlayerPrefab] ✅ Added NetworkObject");
            added++;
        }

        // Loại bỏ NetworkTransform cũ vì ta xài NetworkPlayerRootFollowBody
        if (prefabRoot.GetComponent<NetworkTransform>() != null)
        {
            DestroyImmediate(prefabRoot.GetComponent<NetworkTransform>(), true);
            Debug.Log("[SetupPlayerPrefab] ❌ Removed NetworkTransform (Sử dụng RootFollowBody thay thế)");
        }

        // 3. NetworkPlayerName (tên trên đầu)
        if (prefabRoot.GetComponent<NetworkPlayerName>() == null)
        {
            prefabRoot.AddComponent<NetworkPlayerName>();
            added++;
        }

        // 4. NetworkPlayerLocalOwnership (input authority + camera toggle)
        if (prefabRoot.GetComponent<NetworkPlayerLocalOwnership>() == null)
        {
            prefabRoot.AddComponent<NetworkPlayerLocalOwnership>();
            added++;
        }

        // 5. NetworkPlayerRootFollowBody (Sync Toạ độ mạng custom)
        if (prefabRoot.GetComponent<NetworkPlayerRootFollowBody>() == null)
        {
            prefabRoot.AddComponent<NetworkPlayerRootFollowBody>();
            added++;
        }

        // 6. NetworkPlayerSpawnSnap (Spawn Position custom)
        if (prefabRoot.GetComponent<NetworkPlayerSpawnSnap>() == null)
        {
            prefabRoot.AddComponent<NetworkPlayerSpawnSnap>();
            added++;
        }

        // 7. NetworkAnimatorSync (Sync Animation)
        if (prefabRoot.GetComponent<NetworkAnimatorSync>() == null)
        {
            prefabRoot.AddComponent<NetworkAnimatorSync>();
            added++;
        }

        // 8. NetworkPlayerStats
        if (prefabRoot.GetComponent<NetworkPlayerStats>() == null)
        {
            prefabRoot.AddComponent<NetworkPlayerStats>();
            added++;
        }

        // 9. NetworkPlayerDeathManager
        if (prefabRoot.GetComponent<NetworkPlayerDeathManager>() == null)
        {
            prefabRoot.AddComponent<NetworkPlayerDeathManager>();
            added++;
        }

        // 10. NetworkCombatSync (Sync VFX & Sound Combat)
        if (prefabRoot.GetComponent<NetworkCombatSync>() == null)
        {
            prefabRoot.AddComponent<NetworkCombatSync>();
            added++;
        }

        // 11. NetworkWeaponSync (Sync Weapon Type)
        if (prefabRoot.GetComponent<NetworkWeaponSync>() == null)
        {
            prefabRoot.AddComponent<NetworkWeaponSync>();
            added++;
        }

        if (added > 0)
        {
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[SetupPlayerPrefab] 🎉 Added {added} components, saved: {prefabPath}");
            EditorUtility.DisplayDialog("Setup Complete ✅",
                $"Đã thêm {added} component(s)!\n\n" +
                "• NetworkObject → Fusion replicate\n" +
                "• NetworkPlayerName → Tên trên đầu\n" +
                "• NetworkPlayerLocalOwnership → Input + Camera\n" +
                "• NetworkPlayerRootFollowBody → Đồng bộ toạ độ\n" +
                "• NetworkPlayerSpawnSnap → Spawn snap\n" +
                "• NetworkAnimatorSync → Đồng bộ hoạt ảnh\n" +
                "• NetworkPlayerStats + DeathManager\n\n" +
                "⚠️ RẤT QUAN TRỌNG: Fusion → Rebuild Object Table để nhận diện các biến [Networked]!",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Already Setup ✅",
                "Tất cả components đã có sẵn!",
                "OK");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
        AssetDatabase.Refresh();
    }
}
