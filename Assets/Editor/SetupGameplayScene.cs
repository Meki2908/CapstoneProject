using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Fusion;

/// <summary>
/// Editor tool: tự động setup tất cả component cần thiết trong gameplay scene (Map_Chinh).
/// 
/// CÁCH DÙNG:
///   1. Mở scene Map_Chinh trong Unity Editor
///   2. Menu: Tools → Setup Gameplay Scene (Multiplayer)
///   3. Tool sẽ tự động:
///      - Tạo/cập nhật FusionSpawnPointSetter (quản lý spawn points)
///      - Tạo các SpawnPoint objects (tag "SpawnPoint") cho 4 players
///      - Tạo NetworkLobbyManager (lobby overlay khi vào map)
///      - Kiểm tra tag "SpawnPoint" tồn tại
///   4. Save scene
/// </summary>
public class SetupGameplayScene : EditorWindow
{
    [MenuItem("Tools/Setup Gameplay Scene (Multiplayer)")]
    static void ShowWindow()
    {
        GetWindow<SetupGameplayScene>("Setup Gameplay Scene").Show();
    }

    private int spawnPointCount = 4;
    private float spawnRadius = 5f;
    private Vector3 spawnCenter = new Vector3(0, 1, 0);
    private bool createLobbyManager = true;
    private bool createSpawnPoints = true;
    private bool createSpawnPointSetter = true;

    void OnGUI()
    {
        GUILayout.Label("═══ Setup Gameplay Scene (Multiplayer) ═══", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "Tool này tự động tạo các component cần thiết cho multiplayer trong gameplay scene.\n\n" +
            "BƯỚC DÙNG:\n" +
            "1. Mở scene gameplay (vd: Map_Chinh)\n" +
            "2. Cấu hình bên dưới\n" +
            "3. Bấm [Setup All]\n" +
            "4. Save scene (Ctrl+S)",
            MessageType.Info);

        GUILayout.Space(10);

        // ─── Spawn Points ───
        GUILayout.Label("── Spawn Points ──", EditorStyles.boldLabel);
        createSpawnPoints = EditorGUILayout.Toggle("Tạo Spawn Points", createSpawnPoints);
        if (createSpawnPoints)
        {
            spawnPointCount = EditorGUILayout.IntSlider("Số lượng spawn points", spawnPointCount, 2, 8);
            spawnCenter = EditorGUILayout.Vector3Field("Trung tâm spawn", spawnCenter);
            spawnRadius = EditorGUILayout.FloatField("Bán kính vòng tròn", spawnRadius);
        }

        GUILayout.Space(5);

        // ─── SpawnPointSetter ───
        GUILayout.Label("── FusionSpawnPointSetter ──", EditorStyles.boldLabel);
        createSpawnPointSetter = EditorGUILayout.Toggle("Tạo SpawnPointSetter", createSpawnPointSetter);
        EditorGUILayout.HelpBox(
            "FusionSpawnPointSetter: tự động tìm tất cả SpawnPoint trong scene khi load.\n" +
            "Gắn trên 1 GameObject trong scene.",
            MessageType.None);

        GUILayout.Space(5);

        // ─── Lobby Manager ───
        GUILayout.Label("── NetworkLobbyManager ──", EditorStyles.boldLabel);
        createLobbyManager = EditorGUILayout.Toggle("Tạo Lobby Manager", createLobbyManager);
        EditorGUILayout.HelpBox(
            "NetworkLobbyManager: hiển thị lobby overlay khi vào gameplay scene.\n" +
            "Host bấm START, client đợi. Cần NetworkObject component.\n" +
            "Được tạo như NetworkObject để Fusion tự quản lý.",
            MessageType.None);

        GUILayout.Space(15);

        // ─── Status ───
        var existingSetter = Object.FindFirstObjectByType<FusionSpawnPointSetter>();
        var existingLobby = Object.FindFirstObjectByType<NetworkLobbyManager>();

        // Tag "SpawnPoint" có thể chưa tồn tại → try-catch
        GameObject[] existingSpawnPoints = new GameObject[0];
        try { existingSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint"); }
        catch { /* Tag chưa được tạo */ }

        GUILayout.Label("── Trạng thái hiện tại ──", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("FusionSpawnPointSetter:", existingSetter != null ? "✅ Có" : "❌ Chưa có");
        EditorGUILayout.LabelField("NetworkLobbyManager:", existingLobby != null ? "✅ Có" : "❌ Chưa có");
        EditorGUILayout.LabelField("SpawnPoint objects:", existingSpawnPoints.Length > 0 ? $"✅ {existingSpawnPoints.Length} điểm" : "❌ Chưa có");
        var existingHandler = Object.FindFirstObjectByType<LocalPlayerHandler>();
        EditorGUILayout.LabelField("LocalPlayerHandler:", existingHandler != null ? "✅ Có" : "❌ Chưa có");

        GUILayout.Space(10);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("🔧  SETUP ALL", GUILayout.Height(40)))
        {
            SetupAll();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️  Xóa hết (Reset)", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Confirm", "Xóa tất cả SpawnPoints, SpawnPointSetter, LobbyManager?", "Xóa", "Hủy"))
            {
                CleanupAll();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    void SetupAll()
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Setup Gameplay Scene (Multiplayer)");

        // ─── 1. Ensure tag "SpawnPoint" exists ───
        EnsureTagExists("SpawnPoint");

        // ─── 2. Create Spawn Points ───
        if (createSpawnPoints)
        {
            CreateSpawnPoints();
        }

        // ─── 3. Create FusionSpawnPointSetter ───
        if (createSpawnPointSetter)
        {
            CreateOrUpdateSpawnPointSetter();
        }

        // ─── 4. Create NetworkLobbyManager ───
        if (createLobbyManager)
        {
            CreateOrUpdateLobbyManager();
        }

        // ─── 5. Create LocalPlayerHandler ───
        CreateOrUpdateLocalPlayerHandler();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SetupGameplayScene] ✅ Setup hoàn tất! Nhớ Save Scene (Ctrl+S).");
        EditorUtility.DisplayDialog("Done!", "Setup hoàn tất!\n\nNhớ Save Scene (Ctrl+S).", "OK");
    }

    // ═══════════════════════════════════════
    //  SPAWN POINTS
    // ═══════════════════════════════════════

    void CreateSpawnPoints()
    {
        // Tìm hoặc tạo parent container
        var container = GameObject.Find("_SpawnPoints");
        if (container == null)
        {
            container = new GameObject("_SpawnPoints");
            Undo.RegisterCreatedObjectUndo(container, "Create SpawnPoints Container");
        }

        // Xóa spawn points cũ trong container
        for (int i = container.transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(container.transform.GetChild(i).gameObject);
        }

        // Tạo spawn points xếp vòng tròn
        for (int i = 0; i < spawnPointCount; i++)
        {
            float angle = i * (360f / spawnPointCount);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = spawnCenter + new Vector3(
                Mathf.Cos(rad) * spawnRadius,
                0,
                Mathf.Sin(rad) * spawnRadius
            );

            var spawnGO = new GameObject($"SpawnPoint_{i + 1}");
            spawnGO.transform.SetParent(container.transform, false);
            spawnGO.transform.position = pos;
            // Quay mặt về trung tâm
            spawnGO.transform.LookAt(spawnCenter);
            spawnGO.tag = "SpawnPoint";

            // Thêm icon để dễ thấy trong Scene View
            SetEditorIcon(spawnGO, i == 0 ? "sv_icon_dot1_pix16_gizmo" : "sv_icon_dot6_pix16_gizmo");

            Undo.RegisterCreatedObjectUndo(spawnGO, $"Create SpawnPoint_{i + 1}");
            Debug.Log($"[SetupGameplayScene] Created SpawnPoint_{i + 1} at {pos}");
        }

        Debug.Log($"[SetupGameplayScene] ✅ Created {spawnPointCount} spawn points around ({spawnCenter})");
    }

    // ═══════════════════════════════════════
    //  FUSION SPAWN POINT SETTER
    // ═══════════════════════════════════════

    void CreateOrUpdateSpawnPointSetter()
    {
        var existing = Object.FindFirstObjectByType<FusionSpawnPointSetter>();
        if (existing != null)
        {
            Debug.Log("[SetupGameplayScene] FusionSpawnPointSetter already exists, skipping.");
            // Cập nhật reference nếu cần
            var firstSpawn = GameObject.FindWithTag("SpawnPoint");
            if (firstSpawn != null)
            {
                var so = new SerializedObject(existing);
                var spawnPointProp = so.FindProperty("playerSpawnPoint");
                if (spawnPointProp != null)
                {
                    spawnPointProp.objectReferenceValue = firstSpawn.transform;
                    so.ApplyModifiedProperties();
                    Debug.Log($"[SetupGameplayScene] Updated SpawnPointSetter → primarySpawnPoint = {firstSpawn.name}");
                }
            }
            return;
        }

        var go = new GameObject("_FusionSpawnPointSetter");
        Undo.RegisterCreatedObjectUndo(go, "Create FusionSpawnPointSetter");

        var setter = go.AddComponent<FusionSpawnPointSetter>();

        // Gán spawn point đầu tiên
        var firstSpawnGO = GameObject.FindWithTag("SpawnPoint");
        if (firstSpawnGO != null)
        {
            var so = new SerializedObject(setter);
            var prop = so.FindProperty("playerSpawnPoint");
            if (prop != null)
            {
                prop.objectReferenceValue = firstSpawnGO.transform;
                so.ApplyModifiedProperties();
            }
        }

        Debug.Log("[SetupGameplayScene] ✅ Created FusionSpawnPointSetter");
    }

    // ═══════════════════════════════════════
    //  NETWORK LOBBY MANAGER
    // ═══════════════════════════════════════

    void CreateOrUpdateLobbyManager()
    {
        var existing = Object.FindFirstObjectByType<NetworkLobbyManager>();
        if (existing != null)
        {
            Debug.Log("[SetupGameplayScene] NetworkLobbyManager already exists, skipping.");
            // Đảm bảo có NetworkObject
            if (!existing.TryGetComponent<NetworkObject>(out _))
            {
                Undo.AddComponent<NetworkObject>(existing.gameObject);
                Debug.Log("[SetupGameplayScene] Added missing NetworkObject to LobbyManager");
            }
            return;
        }

        var go = new GameObject("_NetworkLobbyManager");
        Undo.RegisterCreatedObjectUndo(go, "Create NetworkLobbyManager");

        // NetworkObject (bắt buộc cho NetworkBehaviour)
        go.AddComponent<NetworkObject>();
        // LobbyManager
        go.AddComponent<NetworkLobbyManager>();

        Debug.Log("[SetupGameplayScene] ✅ Created NetworkLobbyManager (with NetworkObject)");
    }

    // ═══════════════════════════════════════
    //  LOCAL PLAYER HANDLER
    // ═══════════════════════════════════════

    void CreateOrUpdateLocalPlayerHandler()
    {
        var existing = Object.FindFirstObjectByType<LocalPlayerHandler>();
        if (existing != null)
        {
            Debug.Log("[SetupGameplayScene] LocalPlayerHandler already exists, skipping.");
            return;
        }

        var go = new GameObject("_LocalPlayerHandler");
        Undo.RegisterCreatedObjectUndo(go, "Create LocalPlayerHandler");
        go.AddComponent<LocalPlayerHandler>();

        Debug.Log("[SetupGameplayScene] ✅ Created LocalPlayerHandler (auto-disable local player in multiplayer)");
    }

    // ═══════════════════════════════════════
    //  CLEANUP
    // ═══════════════════════════════════════

    void CleanupAll()
    {
        // Xóa SpawnPoints container
        var container = GameObject.Find("_SpawnPoints");
        if (container != null) Undo.DestroyObjectImmediate(container);

        // Xóa SpawnPointSetter
        var setter = Object.FindFirstObjectByType<FusionSpawnPointSetter>();
        if (setter != null) Undo.DestroyObjectImmediate(setter.gameObject);

        // Xóa LobbyManager
        var lobby = Object.FindFirstObjectByType<NetworkLobbyManager>();
        if (lobby != null) Undo.DestroyObjectImmediate(lobby.gameObject);

        // Xóa LocalPlayerHandler
        var handler = Object.FindFirstObjectByType<LocalPlayerHandler>();
        if (handler != null) Undo.DestroyObjectImmediate(handler.gameObject);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SetupGameplayScene] 🗑️ Cleaned up all multiplayer components.");
    }

    // ═══════════════════════════════════════
    //  UTILITIES
    // ═══════════════════════════════════════

    static void EnsureTagExists(string tagName)
    {
        var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0) return;

        var tagManager = new SerializedObject(asset[0]);
        var tagsProp = tagManager.FindProperty("tags");

        // Kiểm tra tag đã tồn tại chưa
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                Debug.Log($"[SetupGameplayScene] Tag '{tagName}' already exists.");
                return;
            }
        }

        // Thêm tag mới
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[SetupGameplayScene] ✅ Created tag '{tagName}'");
    }

    static void SetEditorIcon(GameObject go, string iconName)
    {
#if UNITY_2021_2_OR_NEWER
        var icon = EditorGUIUtility.IconContent(iconName);
        if (icon != null && icon.image != null)
        {
            EditorGUIUtility.SetIconForObject(go, (Texture2D)icon.image);
        }
#endif
    }
}
