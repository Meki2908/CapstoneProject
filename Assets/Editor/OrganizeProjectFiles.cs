using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor script để tổ chức lại cấu trúc thư mục project.
/// Mở từ menu: Tools > Organize Project Files
/// </summary>
public class OrganizeProjectFiles : EditorWindow
{
    private Vector2 scrollPos;
    private bool showAnimals = true;
    private bool showNPC = true;
    private bool showDragon = true;
    private bool showTeleport = true;
    private bool showUI = true;
    private bool showMap = true;
    private bool showInput = true;
    private bool showAnimators = true;
    private bool showUIToolkit = true;
    private bool showTerrain = true;

    [MenuItem("Tools/Organize Project Files")]
    public static void ShowWindow()
    {
        GetWindow<OrganizeProjectFiles>("Organize Project Files");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tổ Chức Lại Cấu Trúc Thư Mục Project", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Script này sẽ di chuyển các file rời rạc vào thư mục phù hợp.\n" +
            "Unity sẽ tự động cập nhật references (GUIDs).", MessageType.Info);

        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Nhóm 1: Animals
        showAnimals = EditorGUILayout.Foldout(showAnimals, "Nhóm Animals (→ Asset_Animals_FREE/Scripts/)");
        if (showAnimals)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• AnimalFleeOnTouch_CreatureMover.cs");
            EditorGUILayout.LabelField("• AnimalFleeTriggerInput.cs");
            if (GUILayout.Button("Di chuyển nhóm Animals"))
            {
                MoveAnimalScripts();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 2: NPC
        showNPC = EditorGUILayout.Foldout(showNPC, "Nhóm NPC/Path (→ Scripts/NPC/)");
        if (showNPC)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• NPCPathFollower_CC.cs");
            EditorGUILayout.LabelField("• WayPointPath.cs");
            EditorGUILayout.LabelField("• Npc_chat.cs");
            if (GUILayout.Button("Di chuyển nhóm NPC"))
            {
                MoveNPCScripts();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 3: Dragon
        showDragon = EditorGUILayout.Foldout(showDragon, "Nhóm Dragon (→ Asset_RedDragon 1.2/)");
        if (showDragon)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• DragonPathFly.cs");
            if (GUILayout.Button("Di chuyển nhóm Dragon"))
            {
                MoveDragonScripts();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 4: Teleport
        showTeleport = EditorGUILayout.Foldout(showTeleport, "Nhóm Teleport (→ Scripts/Teleport/)");
        if (showTeleport)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• GateTeleporter.cs");
            if (GUILayout.Button("Di chuyển nhóm Teleport"))
            {
                MoveTeleportScripts();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 5: UI Scripts
        showUI = EditorGUILayout.Foldout(showUI, "Nhóm UI Scripts (→ Scripts/UI/)");
        if (showUI)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• TeleportPortal.cs (CloseCanvasButton)");
            EditorGUILayout.LabelField("• ButtonAnimation.cs");
            EditorGUILayout.LabelField("• InteractShowCanvas.cs");
            EditorGUILayout.LabelField("• StatsCanvasController.cs");
            if (GUILayout.Button("Di chuyển nhóm UI Scripts"))
            {
                MoveUIScripts();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 6: Map
        showMap = EditorGUILayout.Foldout(showMap, "Nhóm Map (→ Scripts/Map/)");
        if (showMap)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• Minimap.cs (HeightmapExporter)");
            if (GUILayout.Button("Di chuyển nhóm Map"))
            {
                MoveMapScripts();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 7: Input System
        showInput = EditorGUILayout.Foldout(showInput, "Nhóm Input System (→ Settings/Input/)");
        if (showInput)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• InputSystem_Actions.inputactions");
            EditorGUILayout.LabelField("• Player Controls.cs");
            if (GUILayout.Button("Di chuyển nhóm Input"))
            {
                MoveInputFiles();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 8: UI Animators
        showAnimators = EditorGUILayout.Foldout(showAnimators, "Nhóm UI Animators (→ UI/Animators/)");
        if (showAnimators)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• Choise easy.controller");
            EditorGUILayout.LabelField("• Exit.controller");
            EditorGUILayout.LabelField("• LeftButton.controller");
            if (GUILayout.Button("Di chuyển nhóm Animators"))
            {
                MoveAnimatorFiles();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 9: UI Toolkit
        showUIToolkit = EditorGUILayout.Foldout(showUIToolkit, "Nhóm UI Toolkit (→ UI Toolkit/)");
        if (showUIToolkit)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• MenuDugeon.uss");
            EditorGUILayout.LabelField("• MenuDugeon.uxml");
            if (GUILayout.Button("Di chuyển nhóm UI Toolkit"))
            {
                MoveUIToolkitFiles();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Nhóm 10: Terrain
        showTerrain = EditorGUILayout.Foldout(showTerrain, "Nhóm Terrain Data (→ Terrain/)");
        if (showTerrain)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("• New Terrain.asset (+ 1-5)");
            if (GUILayout.Button("Di chuyển nhóm Terrain"))
            {
                MoveTerrainFiles();
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(20);

        // Di chuyển tất cả
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("DI CHUYỂN TẤT CẢ", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận",
                "Bạn có chắc muốn di chuyển tất cả file?\n\n" +
                "Unity sẽ tự động cập nhật references.", "Có", "Không"))
            {
                MoveAllFiles();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    #region Move Methods

    private void MoveAnimalScripts()
    {
        CreateDirectoryIfNotExists("Assets/ASSETS/Asset_Animals_FREE/Scripts");

        MoveAsset("Assets/AnimalFleeOnTouch_CreatureMover.cs",
                  "Assets/ASSETS/Asset_Animals_FREE/Scripts/AnimalFleeOnTouch_CreatureMover.cs");
        MoveAsset("Assets/AnimalFleeTriggerInput.cs",
                  "Assets/ASSETS/Asset_Animals_FREE/Scripts/AnimalFleeTriggerInput.cs");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm Animals");
    }

    private void MoveNPCScripts()
    {
        CreateDirectoryIfNotExists("Assets/Scripts/NPC");

        MoveAsset("Assets/NPCPathFollower_CC.cs", "Assets/Scripts/NPC/NPCPathFollower_CC.cs");
        MoveAsset("Assets/WayPointPath.cs", "Assets/Scripts/NPC/WaypointPath.cs");
        MoveAsset("Assets/Scripts/Npc_chat.cs", "Assets/Scripts/NPC/Npc_chat.cs");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm NPC");
    }

    private void MoveDragonScripts()
    {
        CreateDirectoryIfNotExists("Assets/ASSETS/Asset_RedDragon 1.2");

        MoveAsset("Assets/DragonPathFly.cs", "Assets/ASSETS/Asset_RedDragon 1.2/DragonPathFly.cs");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm Dragon");
    }

    private void MoveTeleportScripts()
    {
        CreateDirectoryIfNotExists("Assets/Scripts/Teleport");

        MoveAsset("Assets/GateTeleporter.cs", "Assets/Scripts/Teleport/GateTeleporter.cs");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm Teleport");
    }

    private void MoveUIScripts()
    {
        CreateDirectoryIfNotExists("Assets/Scripts/UI");

        MoveAsset("Assets/TeleportPortal.cs", "Assets/Scripts/UI/CloseCanvasButton.cs");
        MoveAsset("Assets/Scripts/ButtonAnimation.cs", "Assets/Scripts/UI/ButtonAnimation.cs");
        MoveAsset("Assets/Scripts/InteractShowCanvas.cs", "Assets/Scripts/UI/InteractShowCanvas.cs");
        MoveAsset("Assets/Scripts/StatsCanvasController.cs", "Assets/Scripts/UI/StatsCanvasController.cs");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm UI Scripts");
    }

    private void MoveMapScripts()
    {
        CreateDirectoryIfNotExists("Assets/Scripts/Map");

        MoveAsset("Assets/Minimap.cs", "Assets/Scripts/Map/HeightmapExporter.cs");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm Map");
    }

    private void MoveInputFiles()
    {
        CreateDirectoryIfNotExists("Assets/Settings/Input");

        MoveAsset("Assets/InputSystem_Actions.inputactions", "Assets/Settings/Input/InputSystem_Actions.inputactions");
        MoveAsset("Assets/Player Controls.cs", "Assets/Settings/Input/Player Controls.cs");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm Input");
    }

    private void MoveAnimatorFiles()
    {
        CreateDirectoryIfNotExists("Assets/UI/Animators");

        MoveAsset("Assets/Choise easy.controller", "Assets/UI/Animators/Choise easy.controller");
        MoveAsset("Assets/Exit.controller", "Assets/UI/Animators/Exit.controller");
        MoveAsset("Assets/LeftButton.controller", "Assets/UI/Animators/LeftButton.controller");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm Animators");
    }

    private void MoveUIToolkitFiles()
    {
        CreateDirectoryIfNotExists("Assets/UI Toolkit");

        MoveAsset("Assets/MenuDugeon.uss", "Assets/UI Toolkit/MenuDugeon.uss");
        MoveAsset("Assets/MenuDugeon.uxml", "Assets/UI Toolkit/MenuDugeon.uxml");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm UI Toolkit");
    }

    private void MoveTerrainFiles()
    {
        CreateDirectoryIfNotExists("Assets/Terrain");

        MoveAsset("Assets/New Terrain.asset", "Assets/Terrain/New Terrain.asset");
        MoveAsset("Assets/New Terrain 1.asset", "Assets/Terrain/New Terrain 1.asset");
        MoveAsset("Assets/New Terrain 2.asset", "Assets/Terrain/New Terrain 2.asset");
        MoveAsset("Assets/New Terrain 3.asset", "Assets/Terrain/New Terrain 3.asset");
        MoveAsset("Assets/New Terrain 4.asset", "Assets/Terrain/New Terrain 4.asset");
        MoveAsset("Assets/New Terrain 5.asset", "Assets/Terrain/New Terrain 5.asset");

        AssetDatabase.Refresh();
        Debug.Log("✓ Đã di chuyển nhóm Terrain");
    }

    private void MoveAllFiles()
    {
        MoveAnimalScripts();
        MoveNPCScripts();
        MoveDragonScripts();
        MoveTeleportScripts();
        MoveUIScripts();
        MoveMapScripts();
        MoveInputFiles();
        MoveAnimatorFiles();
        MoveUIToolkitFiles();
        MoveTerrainFiles();

        Debug.Log("=== HOÀN TẤT DI CHUYỂN TẤT CẢ FILE ===");
        EditorUtility.DisplayDialog("Hoàn tất",
            "Đã di chuyển tất cả file!\n\n" +
            "Kiểm tra Console để xem chi tiết.", "OK");
    }

    #endregion

    #region Helper Methods

    private void CreateDirectoryIfNotExists(string path)
    {
        string fullPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), path);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            AssetDatabase.Refresh();
        }
    }

    private void MoveAsset(string sourcePath, string destPath)
    {
        // Kiểm tra file nguồn tồn tại
        if (!AssetExists(sourcePath))
        {
            Debug.LogWarning($"⚠ Không tìm thấy: {sourcePath}");
            return;
        }

        // Kiểm tra file đích đã tồn tại chưa
        if (AssetExists(destPath))
        {
            Debug.LogWarning($"⚠ File đích đã tồn tại: {destPath}");
            return;
        }

        string result = AssetDatabase.MoveAsset(sourcePath, destPath);
        if (string.IsNullOrEmpty(result))
        {
            Debug.Log($"✓ Di chuyển: {sourcePath} → {destPath}");
        }
        else
        {
            Debug.LogError($"✗ Lỗi di chuyển {sourcePath}: {result}");
        }
    }

    private bool AssetExists(string path)
    {
        return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets));
    }

    #endregion
}
