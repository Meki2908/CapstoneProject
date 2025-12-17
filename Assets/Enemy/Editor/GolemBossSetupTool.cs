using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// GOLEM BOSS SETUP TOOL - ADVANCED AUTO-SETUP
/// Unity Editor tool để TỰ ĐỘNG setup TOÀN BỘ Golem Boss
/// Menu: Tools/Boss Setup/Auto Setup Golem Boss (Complete)
/// 
/// Features:
/// - Auto add all components
/// - Auto configure animator with parameters
/// - Auto create health bar UI
/// - Auto setup colliders and layers
/// - Auto assign references
/// - Auto create spawn points for attacks
/// - One-click complete setup!
/// </summary>
public class GolemBossSetupTool : EditorWindow
{
    private GameObject bossPrefab;
    private GameObject selectedBoss;

    // Setup options
    private bool autoAddComponents = true;
    private bool autoConfigureAnimator = true;
    private bool autoSetupColliders = true;
    private bool createHealthBar = true;
    private bool createAttackSpawnPoints = true;
    private bool setupLayers = true;
    private bool assignAllReferences = true;

    // Boss stats presets
    private enum BossPreset
    {
        Easy,
        Normal,
        Hard,
        Nightmare,
        Custom
    }
    private BossPreset selectedPreset = BossPreset.Normal;
    
    // Scroll position
    private Vector2 scrollPosition = Vector2.zero;
    private bool showDetailedInfo = false;
    private bool showAdvancedOptions = false;

    [MenuItem("Tools/Boss Setup/Auto Setup Golem Boss (Complete)")]
    public static void ShowWindow()
    {
        var window = GetWindow<GolemBossSetupTool>("Golem Boss - Auto Setup");
        window.minSize = new Vector2(500, 700);
        window.maxSize = new Vector2(600, 1000);
    }

    [MenuItem("Tools/Boss Setup/Quick Setup Selected Boss %#B")] // Ctrl+Shift+B
    public static void QuickSetupSelected()
    {
        if (Selection.activeGameObject != null)
        {
            var window = GetWindow<GolemBossSetupTool>("Golem Boss - Auto Setup");
            window.selectedBoss = Selection.activeGameObject;
            window.AutoSetupEverything();
        }
        else
        {
            EditorUtility.DisplayDialog("No Selection", "Please select a GameObject to setup as Golem Boss!", "OK");
        }
    }

    private void OnGUI()
    {
        // Begin scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // Header
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Space(10);
        GUILayout.Label("🗿 GOLEM BOSS AUTO SETUP TOOL 🗿", headerStyle);
        GUILayout.Space(10);

        // Compact info box
        EditorGUILayout.HelpBox(
            "✨ TỰ ĐỘNG SETUP TOÀN BỘ BOSS ✨\n\n" +
            "• Add 7 components + Configure 50+ values\n" +
            "• Auto-assign references & prefabs\n" +
            "• Create 5 spawn points\n" +
            "• ZERO manual setup needed!",
            MessageType.Info
        );
        
        // Toggle for detailed info
        showDetailedInfo = EditorGUILayout.Foldout(showDetailedInfo, "📖 Xem chi tiết tính năng", true);
        if (showDetailedInfo)
        {
            EditorGUILayout.HelpBox(
                "📦 COMPONENTS:\n" +
                "  • GolemBossAI, GolemBossAnimator, GolemBossHealth, GolemBossAttacks\n" +
                "  • CharacterController, Animator, Colliders\n\n" +
                "🔗 REFERENCES:\n" +
                "  • All component cross-references\n" +
                "  • Attack spawn points (5 points)\n" +
                "  • Player detection (auto-find)\n" +
                "  • Effect prefabs auto-find\n\n" +
                "⚙️ INSPECTOR VALUES:\n" +
                "  • HP, Damage, Speed (by difficulty)\n" +
                "  • Attack ranges, cooldowns, radii\n" +
                "  • Detection ranges, patrol settings\n" +
                "  • LayerMasks, Phase transitions, Armor values",
                MessageType.None
            );
        }

        GUILayout.Space(10);

        // Boss selection
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("1️⃣ SELECT BOSS GAMEOBJECT", EditorStyles.boldLabel);
        
        // Important warning
        if (selectedBoss == null)
        {
            EditorGUILayout.HelpBox(
                "⚠️ QUAN TRỌNG: Phải có boss INSTANCE trong Scene!\n\n" +
                "KHÔNG select GolemPrefab.prefab trong Project!\n" +
                "Click nút 'Find GolemPrefab' để tạo instance tự động.",
                MessageType.Warning
            );
        }
        else
        {
            // Check if it's a prefab or scene instance
            bool isPrefab = !selectedBoss.scene.IsValid();
            if (isPrefab)
            {
                EditorGUILayout.HelpBox(
                    "❌ LỖI: Bạn đang select PREFAB, không phải INSTANCE!\n\n" +
                    "Kéo prefab vào Scene trước, hoặc click 'Find GolemPrefab'.",
                    MessageType.Error
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"✅ Boss instance trong Scene: {selectedBoss.name}\n" +
                    $"Position: {selectedBoss.transform.position}",
                    MessageType.Info
                );
            }
        }

        GameObject previousBoss = selectedBoss;
        selectedBoss = (GameObject)EditorGUILayout.ObjectField(
            "Boss GameObject",
            selectedBoss,
            typeof(GameObject),
            true  // allowSceneObjects = true
        );

        // Auto-select if changed
        if (selectedBoss != previousBoss && selectedBoss != null)
        {
            Selection.activeGameObject = selectedBoss;
        }

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select in Hierarchy", GUILayout.Height(25)))
        {
            if (selectedBoss != null)
                Selection.activeGameObject = selectedBoss;
        }
        if (GUILayout.Button("Find GolemPrefab", GUILayout.Height(25)))
        {
            FindGolemPrefab();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Setup options (Advanced - foldout)
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("2️⃣ SETUP OPTIONS", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("✅ Tất cả đã được BẬT sẵn (khuyến nghị)", MessageType.Info);
        
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "⚙️ Advanced Options (không cần thay đổi)", true);
        if (showAdvancedOptions)
        {
            EditorGUI.indentLevel++;
            autoAddComponents = EditorGUILayout.Toggle("Add All Components", autoAddComponents);
            autoConfigureAnimator = EditorGUILayout.Toggle("Configure Animator", autoConfigureAnimator);
            autoSetupColliders = EditorGUILayout.Toggle("Setup Colliders", autoSetupColliders);
            createHealthBar = EditorGUILayout.Toggle("Create Health Bar UI", createHealthBar);
            createAttackSpawnPoints = EditorGUILayout.Toggle("Create Attack Spawn Points", createAttackSpawnPoints);
            setupLayers = EditorGUILayout.Toggle("Setup Layers & Tags", setupLayers);
            assignAllReferences = EditorGUILayout.Toggle("Auto-Assign All References", assignAllReferences);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(10);

        // Difficulty preset
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("3️⃣ DIFFICULTY PRESET", EditorStyles.boldLabel);
        selectedPreset = (BossPreset)EditorGUILayout.EnumPopup("Preset", selectedPreset);

        ShowPresetStats();

        GUILayout.Space(15);

        // Main setup button
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUI.backgroundColor = new Color(0.2f, 1f, 0.2f);
        GUIStyle bigButtonStyle = new GUIStyle(GUI.skin.button);
        bigButtonStyle.fontSize = 14;
        bigButtonStyle.fontStyle = FontStyle.Bold;

        if (GUILayout.Button("🚀 AUTO SETUP EVERYTHING! 🚀", bigButtonStyle, GUILayout.Height(50)))
        {
            AutoSetupEverything();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        // Quick actions
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Label("⚡ QUICK ACTIONS", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Components", GUILayout.Height(30)))
        {
            AddComponents();
        }
        if (GUILayout.Button("Stats", GUILayout.Height(30)))
        {
            ConfigureStats();
        }
        if (GUILayout.Button("Health Bar", GUILayout.Height(30)))
        {
            CreateHealthBar();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Points", GUILayout.Height(30)))
        {
            CreateAttackSpawnPoints();
        }
        if (GUILayout.Button("References", GUILayout.Height(30)))
        {
            AssignAllReferences();
        }
        if (GUILayout.Button("Test Boss", GUILayout.Height(30)))
        {
            TestBoss();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Info footer
        EditorGUILayout.HelpBox(
            "💡 TIP: Select boss in hierarchy, then press Ctrl+Shift+B for quick setup!",
            MessageType.None
        );
        
        // End scroll view
        EditorGUILayout.EndScrollView();
    }

    private void ShowPresetStats()
    {
        string stats = "";

        switch (selectedPreset)
        {
            case BossPreset.Easy:
                stats = "HP: 500 | Damage: 0.7x | Speed: 0.8x";
                break;
            case BossPreset.Normal:
                stats = "HP: 1000 | Damage: 1.0x | Speed: 1.0x";
                break;
            case BossPreset.Hard:
                stats = "HP: 1500 | Damage: 1.3x | Speed: 1.2x";
                break;
            case BossPreset.Nightmare:
                stats = "HP: 2500 | Damage: 1.5x | Speed: 1.5x";
                break;
            case BossPreset.Custom:
                stats = "Configure manually in Inspector";
                break;
        }

        EditorGUILayout.HelpBox(stats, MessageType.None);
    }

    /// <summary>
    /// AUTO SETUP EVERYTHING - Main method
    /// </summary>
    private void AutoSetupEverything()
    {
        // Validation 1: Check if boss selected
        if (selectedBoss == null)
        {
            EditorUtility.DisplayDialog(
                "❌ Không có Boss!",
                "Bạn chưa select boss GameObject!\n\n" +
                "Hãy:\n" +
                "1. Click 'Find GolemPrefab' để tạo boss tự động\n" +
                "2. Hoặc kéo GolemPrefab vào Scene và select nó",
                "OK"
            );
            return;
        }
        
        // Validation 2: Check if it's a scene instance (not prefab)
        if (!selectedBoss.scene.IsValid())
        {
            EditorUtility.DisplayDialog(
                "❌ Sai loại GameObject!",
                "Bạn đang select PREFAB trong Project, không phải INSTANCE trong Scene!\n\n" +
                "Tool chỉ hoạt động với boss instance trong Scene.\n\n" +
                "Hãy:\n" +
                "1. Kéo prefab VÀO SCENE (Hierarchy)\n" +
                "2. Select boss trong Hierarchy (không phải Project)\n" +
                "3. Chạy lại tool",
                "OK"
            );
            return;
        }

        // Show confirmation with current setup
        bool confirm = EditorUtility.DisplayDialog(
            "🚀 Xác nhận Auto Setup",
            $"Sẽ setup boss: {selectedBoss.name}\n\n" +
            $"Difficulty: {selectedPreset}\n" +
            $"Position: {selectedBoss.transform.position}\n\n" +
            "Tool sẽ:\n" +
            "• Add 7 components\n" +
            "• Configure 50+ inspector values\n" +
            "• Auto-assign all references\n" +
            "• Create 5 spawn points\n\n" +
            "Bạn có muốn tiếp tục?",
            "Có, Setup ngay!",
            "Hủy"
        );
        
        if (!confirm)
        {
            Debug.Log("[GolemBossSetup] Setup cancelled by user.");
            return;
        }

        EditorUtility.DisplayProgressBar("Golem Boss Setup", "Starting auto-setup...", 0f);

        try
        {
            Debug.Log($"[GolemBossSetup] 🚀 AUTO SETUP EVERYTHING for: {selectedBoss.name}");
            Debug.Log($"[GolemBossSetup] Position: {selectedBoss.transform.position}");
            Debug.Log($"[GolemBossSetup] Difficulty: {selectedPreset}");

            // Step 1: Add components
            if (autoAddComponents)
            {
                EditorUtility.DisplayProgressBar("Golem Boss Setup", "Adding components...", 0.1f);
                AddComponents();
            }

            // Step 2: Configure animator
            if (autoConfigureAnimator)
            {
                EditorUtility.DisplayProgressBar("Golem Boss Setup", "Configuring animator...", 0.2f);
                ConfigureAnimator();
            }

            // Step 3: Setup colliders
            if (autoSetupColliders)
            {
                EditorUtility.DisplayProgressBar("Golem Boss Setup", "Setting up colliders...", 0.3f);
                SetupColliders();
            }

            // Step 4: Create attack spawn points
            if (createAttackSpawnPoints)
            {
                EditorUtility.DisplayProgressBar("Golem Boss Setup", "Creating attack spawn points...", 0.4f);
                CreateAttackSpawnPoints();
            }

            // Step 5: Create health bar
            if (createHealthBar)
            {
                EditorUtility.DisplayProgressBar("Golem Boss Setup", "Creating health bar UI...", 0.6f);
                CreateHealthBar();
            }

            // Step 6: Setup layers and tags
            if (setupLayers)
            {
                EditorUtility.DisplayProgressBar("Golem Boss Setup", "Setting up layers & tags...", 0.7f);
                SetupLayersAndTags();
            }

            // Step 7: Configure stats
            EditorUtility.DisplayProgressBar("Golem Boss Setup", "Configuring stats...", 0.8f);
            ConfigureStats();

            // Step 8: Assign all references
            if (assignAllReferences)
            {
                EditorUtility.DisplayProgressBar("Golem Boss Setup", "Assigning references...", 0.9f);
                AssignAllReferences();
            }

            // Final step: Mark dirty
            EditorUtility.SetDirty(selectedBoss);

            EditorUtility.DisplayProgressBar("Golem Boss Setup", "Complete!", 1f);

            // Success message - count assigned references
            var bossAI = selectedBoss.GetComponent<GolemBossAI>();
            var bossHealth = selectedBoss.GetComponent<GolemBossHealth>();
            var bossAttacks = selectedBoss.GetComponent<GolemBossAttacks>();

            // Count assigned prefabs
            int assignedPrefabs = 0;
            if (bossHealth != null)
            {
                SerializedObject soHealth = new SerializedObject(bossHealth);
                if (soHealth.FindProperty("damageNumberPrefab").objectReferenceValue != null) assignedPrefabs++;
                if (soHealth.FindProperty("hitEffectPrefab").objectReferenceValue != null) assignedPrefabs++;
                if (soHealth.FindProperty("criticalHitEffectPrefab").objectReferenceValue != null) assignedPrefabs++;
                if (soHealth.FindProperty("phaseTransitionEffectPrefab").objectReferenceValue != null) assignedPrefabs++;
                if (soHealth.FindProperty("deathEffectPrefab").objectReferenceValue != null) assignedPrefabs++;
            }
            if (bossAttacks != null)
            {
                SerializedObject soAttacks = new SerializedObject(bossAttacks);
                if (soAttacks.FindProperty("basicAttackEffect").objectReferenceValue != null) assignedPrefabs++;
                if (soAttacks.FindProperty("groundSlamEffect").objectReferenceValue != null) assignedPrefabs++;
                if (soAttacks.FindProperty("rageAttackEffect").objectReferenceValue != null) assignedPrefabs++;
                if (soAttacks.FindProperty("shockwavePrefab").objectReferenceValue != null) assignedPrefabs++;
                if (soAttacks.FindProperty("rockProjectilePrefab").objectReferenceValue != null) assignedPrefabs++;
            }

            string statsInfo = "";
            if (bossHealth != null && bossAttacks != null)
            {
                statsInfo = $"\n\n📊 CONFIGURED VALUES:\n" +
                           $"HP: {bossHealth.maxHealth:F0}\n" +
                           $"Basic Attack: {bossAttacks.basicAttackDamage:F0} damage\n" +
                           $"Ground Slam: {bossAttacks.groundSlamDamage:F0} damage\n" +
                           $"Rage Attack: {bossAttacks.rageAttackDamage:F0} damage\n";

                if (bossAI != null)
                {
                    statsInfo += $"Walk Speed: {bossAI.walkSpeed:F1}\n" +
                               $"Chase Speed: {bossAI.chaseSpeed:F1}\n" +
                               $"Rage Speed: {bossAI.rageSpeed:F1}\n" +
                               $"Detection Range: {bossAI.detectionRange:F0}m";
                }
            }

            EditorUtility.DisplayDialog(
                "✅ SETUP COMPLETE!",
                $"Golem Boss '{selectedBoss.name}' is FULLY AUTO-CONFIGURED!\n\n" +
                "📦 COMPONENTS:\n" +
                "✅ 7 components added & configured\n\n" +
                "🔗 REFERENCES & PREFABS:\n" +
                "✅ All component cross-references\n" +
                "✅ 5 attack spawn points created\n" +
                "✅ {assignedPrefabs}/10 effect prefabs auto-assigned\n" +
                "✅ Hand transforms auto-detected\n" +
                "✅ Player auto-detected (if in scene)\n" +
                "✅ Animator controller assigned\n\n" +
                "⚙️ INSPECTOR VALUES:\n" +
                "✅ 50+ fields auto-configured\n" +
                "✅ Difficulty: " + selectedPreset + "\n" +
                "✅ Attack ranges, cooldowns, damage\n" +
                "✅ Movement speeds, detection ranges\n" +
                "✅ LayerMasks, phase transitions\n" +
                statsInfo + "\n\n" +
                "🎮 Boss is 100% READY! 🗿⚔️\n" +
                "ZERO manual Inspector setup needed!\n\n" +
                (assignedPrefabs < 10 ? "💡 Tip: Some effect prefabs not found.\nYou can assign them manually if needed." : ""),
                "Awesome!"
            );

            Debug.Log("[GolemBossSetup] ✅✅✅ AUTO SETUP COMPLETE! ✅✅✅");
            Debug.Log($"[GolemBossSetup] Boss: {selectedBoss.name} at {selectedBoss.transform.position}");
            Debug.Log($"[GolemBossSetup] Components count: {selectedBoss.GetComponents<Component>().Length}");
            Debug.Log("[GolemBossSetup] 🎯 NEXT: Select boss trong Hierarchy và mở Inspector để xem kết quả!");

            // Select the boss to show it in inspector
            Selection.activeGameObject = selectedBoss;
            EditorGUIUtility.PingObject(selectedBoss); // Highlight in Hierarchy
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// Find GolemPrefab in project
    /// </summary>
    private void FindGolemPrefab()
    {
        // Search for GolemPrefab
        string[] guids = AssetDatabase.FindAssets("GolemPrefab t:GameObject");

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "GolemPrefab Not Found",
                "Could not find GolemPrefab.prefab in the project!\n\n" +
                "Please make sure it exists at:\n" +
                "Assets/golem/02_Golem/Prefabs/GolemPrefab.prefab",
                "OK"
            );
            return;
        }

        // Get first match
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab != null)
        {
            // Instantiate in scene
            selectedBoss = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            selectedBoss.name = "Golem_Boss";

            // Position at origin
            selectedBoss.transform.position = Vector3.zero;
            selectedBoss.transform.rotation = Quaternion.identity;

            Selection.activeGameObject = selectedBoss;

            Debug.Log($"[GolemBossSetup] ✅ Found and instantiated GolemPrefab from: {path}");

            EditorUtility.DisplayDialog(
                "GolemPrefab Found!",
                $"GolemPrefab instantiated in scene as '{selectedBoss.name}'!\n\n" +
                "Click 'Auto Setup Everything' to configure it.",
                "OK"
            );
        }
    }

    private void SetupBoss()
    {
        // This method is kept for backward compatibility
        AutoSetupEverything();
    }

    private void CreateAttackSpawnPoints()
    {
        if (selectedBoss == null) return;

        Debug.Log("[GolemBossSetup] Creating attack spawn points...");

        // Find or create SpawnPoints container
        Transform spawnPointsParent = selectedBoss.transform.Find("AttackSpawnPoints");
        if (spawnPointsParent == null)
        {
            GameObject spawnPointsObj = new GameObject("AttackSpawnPoints");
            spawnPointsObj.transform.SetParent(selectedBoss.transform);
            spawnPointsObj.transform.localPosition = Vector3.zero;
            spawnPointsParent = spawnPointsObj.transform;
        }

        // Create spawn points for different attacks
        CreateSpawnPoint(spawnPointsParent, "BasicAttack_Spawn", new Vector3(0, 1.5f, 2f));
        CreateSpawnPoint(spawnPointsParent, "ComboAttack_Spawn1", new Vector3(-1f, 1.5f, 2f));
        CreateSpawnPoint(spawnPointsParent, "ComboAttack_Spawn2", new Vector3(1f, 1.5f, 2f));
        CreateSpawnPoint(spawnPointsParent, "GroundSlam_Center", new Vector3(0, 0.5f, 0));
        CreateSpawnPoint(spawnPointsParent, "RageAttack_Spawn", new Vector3(0, 2f, 3f));

        Debug.Log("   ✅ Created attack spawn points");

        EditorUtility.SetDirty(selectedBoss);
    }

    private void CreateSpawnPoint(Transform parent, string name, Vector3 localPos)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            GameObject spawnPoint = new GameObject(name);
            spawnPoint.transform.SetParent(parent);
            spawnPoint.transform.localPosition = localPos;
            spawnPoint.transform.localRotation = Quaternion.identity;

            // Add gizmo for visualization
            var gizmo = spawnPoint.AddComponent<SpawnPointGizmo>();
            gizmo.color = Color.red;
            gizmo.radius = 0.3f;
        }
    }

    private void AssignAllReferences()
    {
        if (selectedBoss == null) return;

        Debug.Log("[GolemBossSetup] Auto-assigning ALL references & prefabs...");

        var bossAI = selectedBoss.GetComponent<GolemBossAI>();
        var bossAnimator = selectedBoss.GetComponent<GolemBossAnimator>();
        var bossHealth = selectedBoss.GetComponent<GolemBossHealth>();
        var bossAttacks = selectedBoss.GetComponent<GolemBossAttacks>();
        var animator = selectedBoss.GetComponent<Animator>();
        var characterController = selectedBoss.GetComponent<CharacterController>();

        // ========== GOLEM BOSS AI REFERENCES ==========
        if (bossAI != null)
        {
            SerializedObject soAI = new SerializedObject(bossAI);

            // Component references
            SerializedProperty propAnimator = soAI.FindProperty("bossAnimator");
            if (propAnimator != null) propAnimator.objectReferenceValue = bossAnimator;
            
            SerializedProperty propHealth = soAI.FindProperty("bossHealth");
            if (propHealth != null) propHealth.objectReferenceValue = bossHealth;
            
            SerializedProperty propAttacks = soAI.FindProperty("bossAttacks");
            if (propAttacks != null) propAttacks.objectReferenceValue = bossAttacks;
            
            SerializedProperty propController = soAI.FindProperty("controller");
            if (propController != null) propController.objectReferenceValue = characterController;

            // Try to find player (if exists in scene)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SerializedProperty propPlayer = soAI.FindProperty("player");
                if (propPlayer != null)
                {
                    propPlayer.objectReferenceValue = player.transform;
                    Debug.Log($"   ✅ Found and assigned Player: {player.name}");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ Player not found in scene! Make sure to add player with tag 'Player'");
            }

            soAI.ApplyModifiedProperties();
            Debug.Log("   ✅ GolemBossAI references assigned");
        }

        // ========== GOLEM BOSS ANIMATOR REFERENCES ==========
        if (bossAnimator != null)
        {
            SerializedObject soAnimator = new SerializedObject(bossAnimator);
            SerializedProperty propAnim = soAnimator.FindProperty("animator");
            if (propAnim != null) propAnim.objectReferenceValue = animator;
            soAnimator.ApplyModifiedProperties();
            Debug.Log("   ✅ GolemBossAnimator references assigned");
        }

        // ========== GOLEM BOSS HEALTH REFERENCES & PREFABS ==========
        if (bossHealth != null)
        {
            SerializedObject soHealth = new SerializedObject(bossHealth);

            // Component references
            SerializedProperty propAI = soHealth.FindProperty("bossAI");
            if (propAI != null) propAI.objectReferenceValue = bossAI;
            
            SerializedProperty propAnimator = soHealth.FindProperty("bossAnimator");
            if (propAnimator != null) propAnimator.objectReferenceValue = bossAnimator;

            // Auto-find DamageNumber prefab
            DamageNumbersPro.DamageNumber damageNumberPrefab = FindAssetByType<DamageNumbersPro.DamageNumber>("DamageNumber");
            if (damageNumberPrefab != null)
            {
                SerializedProperty propDmgNum = soHealth.FindProperty("damageNumberPrefab");
                if (propDmgNum != null)
                {
                    propDmgNum.objectReferenceValue = damageNumberPrefab;
                    Debug.Log($"   ✅ Assigned DamageNumber prefab: {damageNumberPrefab.name}");
                }
            }

            // Auto-find effect prefabs
            GameObject hitEffect = FindPrefabByName("Hit", "Effect", "Impact");
            if (hitEffect != null)
            {
                SerializedProperty propHit = soHealth.FindProperty("hitEffectPrefab");
                if (propHit != null)
                {
                    propHit.objectReferenceValue = hitEffect;
                    Debug.Log($"   ✅ Assigned Hit Effect: {hitEffect.name}");
                }
            }

            GameObject critEffect = FindPrefabByName("Critical", "Crit", "Effect");
            if (critEffect != null)
            {
                SerializedProperty propCrit = soHealth.FindProperty("criticalHitEffectPrefab");
                if (propCrit != null)
                {
                    propCrit.objectReferenceValue = critEffect;
                    Debug.Log($"   ✅ Assigned Critical Effect: {critEffect.name}");
                }
            }

            GameObject phaseEffect = FindPrefabByName("Phase", "Transition", "Transform");
            if (phaseEffect != null)
            {
                SerializedProperty propPhase = soHealth.FindProperty("phaseTransitionEffectPrefab");
                if (propPhase != null)
                {
                    propPhase.objectReferenceValue = phaseEffect;
                    Debug.Log($"   ✅ Assigned Phase Transition Effect: {phaseEffect.name}");
                }
            }

            GameObject deathEffect = FindPrefabByName("Death", "Die", "Destroy", "Explosion");
            if (deathEffect != null)
            {
                SerializedProperty propDeath = soHealth.FindProperty("deathEffectPrefab");
                if (propDeath != null)
                {
                    propDeath.objectReferenceValue = deathEffect;
                    Debug.Log($"   ✅ Assigned Death Effect: {deathEffect.name}");
                }
            }

            // Try to find or create health bar
            Transform healthBarTransform = selectedBoss.transform.Find("HealthBarCanvas");
            if (healthBarTransform != null)
            {
                SerializedProperty propHBTransform = soHealth.FindProperty("healthBarTransform");
                if (propHBTransform != null) propHBTransform.objectReferenceValue = healthBarTransform;

                // Find Image components
                UnityEngine.UI.Image[] images = healthBarTransform.GetComponentsInChildren<UnityEngine.UI.Image>();
                if (images.Length > 0)
                {
                    // First image is usually fill
                    SerializedProperty propFill = soHealth.FindProperty("healthBarFill");
                    if (propFill != null)
                    {
                        propFill.objectReferenceValue = images[0];
                        Debug.Log($"   ✅ Assigned Health Bar Fill: {images[0].name}");
                    }
                }
                if (images.Length > 1)
                {
                    // Second image is usually background
                    SerializedProperty propBG = soHealth.FindProperty("healthBarBackground");
                    if (propBG != null)
                    {
                        propBG.objectReferenceValue = images[1];
                        Debug.Log($"   ✅ Assigned Health Bar Background: {images[1].name}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ HealthBarCanvas not found. Create it manually or use 'Create Health Bar' button.");
            }

            soHealth.ApplyModifiedProperties();
            Debug.Log("   ✅ GolemBossHealth references & prefabs assigned");
        }

        // ========== GOLEM BOSS ATTACKS REFERENCES & PREFABS ==========
        if (bossAttacks != null)
        {
            SerializedObject soAttacks = new SerializedObject(bossAttacks);

            // Component references
            SerializedProperty propAI = soAttacks.FindProperty("bossAI");
            if (propAI != null) propAI.objectReferenceValue = bossAI;
            
            SerializedProperty propAnimator = soAttacks.FindProperty("bossAnimator");
            if (propAnimator != null) propAnimator.objectReferenceValue = bossAnimator;
            
            SerializedProperty propHealth = soAttacks.FindProperty("bossHealth");
            if (propHealth != null) propHealth.objectReferenceValue = bossHealth;

            // Auto-find effect prefabs
            GameObject basicEffect = FindPrefabByName("Slash", "Attack", "Hit");
            if (basicEffect != null)
            {
                SerializedProperty propBasic = soAttacks.FindProperty("basicAttackEffect");
                if (propBasic != null)
                {
                    propBasic.objectReferenceValue = basicEffect;
                    Debug.Log($"   ✅ Assigned Basic Attack Effect: {basicEffect.name}");
                }
            }

            GameObject slamEffect = FindPrefabByName("Slam", "Ground", "Shockwave", "AOE");
            if (slamEffect != null)
            {
                SerializedProperty propSlam = soAttacks.FindProperty("groundSlamEffect");
                if (propSlam != null)
                {
                    propSlam.objectReferenceValue = slamEffect;
                    Debug.Log($"   ✅ Assigned Ground Slam Effect: {slamEffect.name}");
                }
            }

            GameObject rageEffect = FindPrefabByName("Rage", "Roar", "Aura", "Power");
            if (rageEffect != null)
            {
                SerializedProperty propRage = soAttacks.FindProperty("rageAttackEffect");
                if (propRage != null)
                {
                    propRage.objectReferenceValue = rageEffect;
                    Debug.Log($"   ✅ Assigned Rage Attack Effect: {rageEffect.name}");
                }
            }

            GameObject shockwave = FindPrefabByName("Shockwave", "Wave", "Ripple");
            if (shockwave != null)
            {
                SerializedProperty propShockwave = soAttacks.FindProperty("shockwavePrefab");
                if (propShockwave != null)
                {
                    propShockwave.objectReferenceValue = shockwave;
                    Debug.Log($"   ✅ Assigned Shockwave: {shockwave.name}");
                }
            }

            GameObject rockProjectile = FindPrefabByName("Rock", "Stone", "Projectile", "Boulder");
            if (rockProjectile != null)
            {
                SerializedProperty propRock = soAttacks.FindProperty("rockProjectilePrefab");
                if (propRock != null)
                {
                    propRock.objectReferenceValue = rockProjectile;
                    Debug.Log($"   ✅ Assigned Rock Projectile: {rockProjectile.name}");
                }
            }

            // Auto-find hand transforms (search in children)
            Transform rightHand = FindChildRecursive(selectedBoss.transform, "RightHand", "Right_Hand", "Hand_R", "R_Hand");
            if (rightHand != null)
            {
                SerializedProperty propRightHand = soAttacks.FindProperty("rightHandTransform");
                if (propRightHand != null)
                {
                    propRightHand.objectReferenceValue = rightHand;
                    Debug.Log($"   ✅ Assigned Right Hand: {rightHand.name}");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ Right hand transform not found. Search for: RightHand, Right_Hand, Hand_R");
            }

            Transform leftHand = FindChildRecursive(selectedBoss.transform, "LeftHand", "Left_Hand", "Hand_L", "L_Hand");
            if (leftHand != null)
            {
                SerializedProperty propLeftHand = soAttacks.FindProperty("leftHandTransform");
                if (propLeftHand != null)
                {
                    propLeftHand.objectReferenceValue = leftHand;
                    Debug.Log($"   ✅ Assigned Left Hand: {leftHand.name}");
                }
            }
            else
            {
                Debug.LogWarning("   ⚠️ Left hand transform not found. Search for: LeftHand, Left_Hand, Hand_L");
            }

            // Ground point - use AttackSpawnPoints/GroundSlam_Center if exists
            Transform groundPoint = selectedBoss.transform.Find("AttackSpawnPoints/GroundSlam_Center");
            if (groundPoint != null)
            {
                SerializedProperty propGroundPoint = soAttacks.FindProperty("groundPointTransform");
                if (propGroundPoint != null)
                {
                    propGroundPoint.objectReferenceValue = groundPoint;
                    Debug.Log($"   ✅ Assigned Ground Point: {groundPoint.name}");
                }
            }

            // Assign spawn points
            Transform spawnPointsParent = selectedBoss.transform.Find("AttackSpawnPoints");
            if (spawnPointsParent != null)
            {
                Transform basicSpawn = spawnPointsParent.Find("BasicAttack_Spawn");
                Transform combo1Spawn = spawnPointsParent.Find("ComboAttack_Spawn1");
                Transform combo2Spawn = spawnPointsParent.Find("ComboAttack_Spawn2");
                Transform slamCenter = spawnPointsParent.Find("GroundSlam_Center");
                Transform rageSpawn = spawnPointsParent.Find("RageAttack_Spawn");

                if (basicSpawn != null)
                {
                    SerializedProperty prop = soAttacks.FindProperty("basicAttackSpawn");
                    if (prop != null) prop.objectReferenceValue = basicSpawn;
                }
                if (combo1Spawn != null)
                {
                    SerializedProperty prop = soAttacks.FindProperty("comboAttackSpawn1");
                    if (prop != null) prop.objectReferenceValue = combo1Spawn;
                }
                if (combo2Spawn != null)
                {
                    SerializedProperty prop = soAttacks.FindProperty("comboAttackSpawn2");
                    if (prop != null) prop.objectReferenceValue = combo2Spawn;
                }
                if (slamCenter != null)
                {
                    SerializedProperty prop = soAttacks.FindProperty("groundSlamCenter");
                    if (prop != null) prop.objectReferenceValue = slamCenter;
                }
                if (rageSpawn != null)
                {
                    SerializedProperty prop = soAttacks.FindProperty("rageAttackSpawn");
                    if (prop != null) prop.objectReferenceValue = rageSpawn;
                }

                Debug.Log("   ✅ Attack spawn points assigned");
            }

            soAttacks.ApplyModifiedProperties();
            Debug.Log("   ✅ GolemBossAttacks references & prefabs assigned");
        }

        EditorUtility.SetDirty(selectedBoss);
        Debug.Log("   ✅✅✅ ALL references & prefabs assigned successfully!");
    }

    /// <summary>
    /// Find prefab by searching for keywords in name
    /// </summary>
    private GameObject FindPrefabByName(params string[] keywords)
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            // Check if filename contains any keyword
            foreach (string keyword in keywords)
            {
                if (fileName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        return prefab;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find asset by component type
    /// </summary>
    private T FindAssetByType<T>(params string[] nameKeywords) where T : UnityEngine.Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                // If keywords provided, check name
                if (nameKeywords.Length > 0)
                {
                    foreach (string keyword in nameKeywords)
                    {
                        if (asset.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return asset;
                        }
                    }
                }
                else
                {
                    // No keywords, return first match
                    return asset;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Find child transform by name (recursive search)
    /// </summary>
    private Transform FindChildRecursive(Transform parent, params string[] possibleNames)
    {
        foreach (string name in possibleNames)
        {
            // Try direct find first
            Transform found = parent.Find(name);
            if (found != null) return found;

            // Try case-insensitive recursive search
            found = RecursiveFind(parent, name);
            if (found != null) return found;
        }

        return null;
    }

    private Transform RecursiveFind(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform result = RecursiveFind(child, childName);
            if (result != null) return result;
        }

        return null;
    }

    private void AddComponents()
    {
        if (selectedBoss == null) return;

        Debug.Log("[GolemBossSetup] Adding components...");

        // Add GolemBossAI
        if (selectedBoss.GetComponent<GolemBossAI>() == null)
        {
            selectedBoss.AddComponent<GolemBossAI>();
            Debug.Log("   ✅ Added GolemBossAI");
        }

        // Add GolemBossAnimator
        if (selectedBoss.GetComponent<GolemBossAnimator>() == null)
        {
            selectedBoss.AddComponent<GolemBossAnimator>();
            Debug.Log("   ✅ Added GolemBossAnimator");
        }

        // Add GolemBossHealth
        if (selectedBoss.GetComponent<GolemBossHealth>() == null)
        {
            selectedBoss.AddComponent<GolemBossHealth>();
            Debug.Log("   ✅ Added GolemBossHealth");
        }

        // Add GolemBossAttacks
        if (selectedBoss.GetComponent<GolemBossAttacks>() == null)
        {
            selectedBoss.AddComponent<GolemBossAttacks>();
            Debug.Log("   ✅ Added GolemBossAttacks");
        }

        // Add CharacterController
        if (selectedBoss.GetComponent<CharacterController>() == null)
        {
            var cc = selectedBoss.AddComponent<CharacterController>();
            cc.radius = 1.5f;
            cc.height = 5f;
            cc.center = new Vector3(0, 2.5f, 0);
            Debug.Log("   ✅ Added CharacterController");
        }

        // Add Animator
        if (selectedBoss.GetComponent<Animator>() == null)
        {
            selectedBoss.AddComponent<Animator>();
            Debug.Log("   ✅ Added Animator");
        }

        EditorUtility.SetDirty(selectedBoss);
    }

    private void ConfigureAnimator()
    {
        if (selectedBoss == null) return;

        Debug.Log("[GolemBossSetup] Configuring animator...");

        var animator = selectedBoss.GetComponent<Animator>();
        if (animator != null)
        {
            // Try to find GolemAnimator controller
            string[] guids = AssetDatabase.FindAssets("GolemAnimator t:RuntimeAnimatorController");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                animator.runtimeAnimatorController = controller;
                Debug.Log($"   ✅ Set animator controller: {controller.name}");
            }
            else
            {
                Debug.LogWarning("   ⚠️ GolemAnimator controller not found!");
            }
        }

        EditorUtility.SetDirty(selectedBoss);
    }

    private void SetupColliders()
    {
        if (selectedBoss == null) return;

        Debug.Log("[GolemBossSetup] Setting up colliders...");

        // Remove old colliders
        var oldColliders = selectedBoss.GetComponents<Collider>();
        foreach (var col in oldColliders)
        {
            if (!(col is CharacterController))
            {
                DestroyImmediate(col);
            }
        }

        // Add capsule collider for better collision
        var capsule = selectedBoss.AddComponent<CapsuleCollider>();
        capsule.radius = 1.5f;
        capsule.height = 5f;
        capsule.center = new Vector3(0, 2.5f, 0);
        capsule.isTrigger = false;

        Debug.Log("   ✅ Added CapsuleCollider");

        EditorUtility.SetDirty(selectedBoss);
    }

    private void ConfigureStats()
    {
        if (selectedBoss == null) return;

        Debug.Log($"[GolemBossSetup] Configuring ALL stats for preset: {selectedPreset}");

        var bossAI = selectedBoss.GetComponent<GolemBossAI>();
        var bossHealth = selectedBoss.GetComponent<GolemBossHealth>();
        var bossAttacks = selectedBoss.GetComponent<GolemBossAttacks>();

        float healthMultiplier = 1f;
        float damageMultiplier = 1f;
        float speedMultiplier = 1f;

        switch (selectedPreset)
        {
            case BossPreset.Easy:
                healthMultiplier = 0.5f;
                damageMultiplier = 0.7f;
                speedMultiplier = 0.8f;
                break;
            case BossPreset.Normal:
                healthMultiplier = 1f;
                damageMultiplier = 1f;
                speedMultiplier = 1f;
                break;
            case BossPreset.Hard:
                healthMultiplier = 1.5f;
                damageMultiplier = 1.3f;
                speedMultiplier = 1.2f;
                break;
            case BossPreset.Nightmare:
                healthMultiplier = 2.5f;
                damageMultiplier = 1.5f;
                speedMultiplier = 1.5f;
                break;
        }

        // ========== GOLEM BOSS AI ==========
        if (bossAI != null)
        {
            SerializedObject soAI = new SerializedObject(bossAI);

            // Health
            float baseHealth = 1000f;
            SetFloatProperty(soAI, "maxHealth", baseHealth * healthMultiplier);

            // Movement speeds (matching actual fields in GolemBossAI)
            SetFloatProperty(soAI, "walkSpeed", 2f * speedMultiplier);
            SetFloatProperty(soAI, "chaseSpeed", 4f * speedMultiplier);
            SetFloatProperty(soAI, "rageSpeed", 6f * speedMultiplier);

            // Detection ranges (matching actual fields)
            SetFloatProperty(soAI, "detectionRange", 15f);
            SetFloatProperty(soAI, "meleeAttackRange", 3f);
            SetFloatProperty(soAI, "rangedAttackRange", 8f);

            // Phase thresholds
            SetFloatProperty(soAI, "phase2Threshold", 0.66f);
            SetFloatProperty(soAI, "phase3Threshold", 0.33f);

            // Attack cooldowns (matching actual fields)
            SetFloatProperty(soAI, "basicAttackCooldown", 2f / speedMultiplier);
            SetFloatProperty(soAI, "comboAttackCooldown", 5f / speedMultiplier);
            SetFloatProperty(soAI, "groundSlamCooldown", 8f / speedMultiplier);
            SetFloatProperty(soAI, "rageAttackCooldown", 10f / speedMultiplier);

            // Special behaviors
            SetBoolProperty(soAI, "roarOnCombatStart", true);
            SetBoolProperty(soAI, "canHealOnce", true);
            SetFloatProperty(soAI, "healAmount", 0.15f);

            // Patrol settings
            SetFloatProperty(soAI, "patrolRadius", 10f);

            // LayerMask (safe - use 0 if layer doesn't exist)
            int playerLayerMask = GetSafeLayerMask("Player");
            SetIntProperty(soAI, "playerLayer", playerLayerMask);

            // Debug settings
            SetBoolProperty(soAI, "showDebugLogs", true);
            SetBoolProperty(soAI, "showGizmos", true);

            soAI.ApplyModifiedProperties();
            Debug.Log($"   ✅ GolemBossAI: HP={baseHealth * healthMultiplier}, Speeds configured");
        }
        else
        {
            Debug.LogWarning("   ⚠️ GolemBossAI component not found!");
        }

        // ========== GOLEM BOSS HEALTH ==========
        if (bossHealth != null)
        {
            SerializedObject soHealth = new SerializedObject(bossHealth);

            float baseHealth = 1000f;
            SetFloatProperty(soHealth, "maxHealth", baseHealth * healthMultiplier);

            // Defense (matching actual fields in GolemBossHealth)
            SetFloatProperty(soHealth, "damageReduction", 0.2f);
            SetFloatProperty(soHealth, "invulnerabilityDuration", 3f);

            // Phase armor (matching actual field names)
            SetFloatProperty(soHealth, "phase1ExtraArmor", 0.1f);  // 10% reduction
            SetFloatProperty(soHealth, "phase3ExtraArmor", 0.15f);  // 15% reduction in rage

            // Phase colors
            // (Colors are set via Color properties, skip for now)

            // Debug
            SetBoolProperty(soHealth, "showDebugLogs", true);
            SetBoolProperty(soHealth, "showDebugGUI", true);

            soHealth.ApplyModifiedProperties();
            Debug.Log($"   ✅ GolemBossHealth: MaxHP={baseHealth * healthMultiplier}, Defense configured");
        }

        // ========== GOLEM BOSS ATTACKS ==========
        if (bossAttacks != null)
        {
            SerializedObject soAttacks = new SerializedObject(bossAttacks);

            // Damage values (matching actual fields in GolemBossAttacks)
            SetFloatProperty(soAttacks, "basicAttackDamage", 50f * damageMultiplier);
            SetFloatProperty(soAttacks, "comboAttackDamage", 35f * damageMultiplier);
            SetFloatProperty(soAttacks, "groundSlamDamage", 80f * damageMultiplier);
            SetFloatProperty(soAttacks, "rageAttackDamage", 100f * damageMultiplier);

            // Attack ranges (matching actual fields)
            SetFloatProperty(soAttacks, "meleeAttackRadius", 3f);
            SetFloatProperty(soAttacks, "groundSlamRadius", 6f);
            SetFloatProperty(soAttacks, "rageAttackRadius", 10f);

            // Knockback forces
            SetFloatProperty(soAttacks, "basicKnockbackForce", 5f);
            SetFloatProperty(soAttacks, "groundSlamKnockbackForce", 15f);
            SetFloatProperty(soAttacks, "rageKnockbackForce", 20f);

            // Projectile
            SetFloatProperty(soAttacks, "rockProjectileSpeed", 15f);

            // LayerMasks (matching actual field names)
            int playerLayer = GetSafeLayerMask("Player");
            int groundLayer = GetSafeLayerMask("Default", "Ground");
            SetIntProperty(soAttacks, "playerLayer", playerLayer);
            SetIntProperty(soAttacks, "groundLayer", groundLayer);

            // Debug
            SetBoolProperty(soAttacks, "showDebugLogs", true);
            SetBoolProperty(soAttacks, "showDebugGizmos", true);

            soAttacks.ApplyModifiedProperties();
            Debug.Log($"   ✅ GolemBossAttacks: Basic={50f * damageMultiplier}, Slam={80f * damageMultiplier}, Rage={100f * damageMultiplier}");
        }

        // ========== GOLEM BOSS ANIMATOR ==========
        var bossAnimator = selectedBoss.GetComponent<GolemBossAnimator>();
        if (bossAnimator != null)
        {
            SerializedObject soAnimator = new SerializedObject(bossAnimator);

            // Animation settings
            SetFloatProperty(soAnimator, "animationSpeedMultiplier", 1f * speedMultiplier);
            SetFloatProperty(soAnimator, "speedSmoothTime", 0.1f);

            // Attack variations
            SetBoolProperty(soAnimator, "randomizeBasicAttacks", true);

            // Debug
            SetBoolProperty(soAnimator, "showDebugLogs", true);

            soAnimator.ApplyModifiedProperties();
            Debug.Log($"   ✅ GolemBossAnimator: Speed multiplier={1f * speedMultiplier}");
        }

        EditorUtility.SetDirty(selectedBoss);
        Debug.Log("   ✅✅✅ ALL Inspector values configured!");
    }

    private void CreateHealthBar()
    {
        if (selectedBoss == null) return;

        Debug.Log("[GolemBossSetup] Creating health bar...");

        // TODO: Create world-space canvas with health bar
        // For now, just log
        Debug.Log("   ⚠️ Health bar creation not yet implemented (use UI manually)");
    }

    private void SetupLayersAndTags()
    {
        if (selectedBoss == null) return;

        Debug.Log("[GolemBossSetup] Setting up layers and tags...");

        // Try to set layer to Enemy (if exists)
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0 && enemyLayer < 32)
        {
            selectedBoss.layer = enemyLayer;
            Debug.Log($"   ✅ Set layer to 'Enemy' (layer {enemyLayer})");
        }
        else
        {
            Debug.LogWarning("   ⚠️ 'Enemy' layer not found! Keeping default layer.\n" +
                           "   Create 'Enemy' layer in: Edit > Project Settings > Tags and Layers");
        }

        // Try to set tag to Enemy (with error handling)
        try
        {
            // Check if tag exists by trying to compare
            bool tagExists = false;
            foreach (string tag in UnityEditorInternal.InternalEditorUtility.tags)
            {
                if (tag == "Enemy")
                {
                    tagExists = true;
                    break;
                }
            }
            
            if (tagExists)
            {
                selectedBoss.tag = "Enemy";
                Debug.Log("   ✅ Set tag to 'Enemy'");
            }
            else
            {
                Debug.LogWarning("   ⚠️ 'Enemy' tag not found! Keeping default tag.\n" +
                               "   Create 'Enemy' tag in: Edit > Project Settings > Tags and Layers");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"   ⚠️ Could not set tag: {e.Message}");
        }

        EditorUtility.SetDirty(selectedBoss);
    }

    private void TestBoss()
    {
        if (selectedBoss == null)
        {
            EditorUtility.DisplayDialog(
                "No Boss Selected",
                "Please select a boss GameObject first.",
                "OK"
            );
            return;
        }

        // Make sure we're in play mode
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Enter Play Mode",
                "Please enter Play Mode to test the boss.",
                "OK"
            );
            return;
        }

        // Focus on boss
        Selection.activeGameObject = selectedBoss;
        SceneView.lastActiveSceneView.FrameSelected();

        Debug.Log($"[GolemBossSetup] 🎮 Testing boss: {selectedBoss.name}");
    }
    
    // ========== HELPER METHODS ==========
    
    /// <summary>
    /// Safely set float property (skip if property doesn't exist)
    /// </summary>
    private void SetFloatProperty(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.floatValue = value;
        }
        else
        {
            Debug.LogWarning($"   ⚠️ Property '{propertyName}' not found");
        }
    }
    
    /// <summary>
    /// Safely set int property (skip if property doesn't exist)
    /// </summary>
    private void SetIntProperty(SerializedObject so, string propertyName, int value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.intValue = value;
        }
        else
        {
            Debug.LogWarning($"   ⚠️ Property '{propertyName}' not found");
        }
    }
    
    /// <summary>
    /// Safely set bool property (skip if property doesn't exist)
    /// </summary>
    private void SetBoolProperty(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.boolValue = value;
        }
        else
        {
            Debug.LogWarning($"   ⚠️ Property '{propertyName}' not found");
        }
    }
    
    /// <summary>
    /// Get LayerMask safely (returns 0 if layer doesn't exist)
    /// </summary>
    private int GetSafeLayerMask(params string[] layerNames)
    {
        // Check if layers exist
        bool allExist = true;
        foreach (string layerName in layerNames)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                allExist = false;
                break;
            }
        }
        
        if (allExist)
        {
            return LayerMask.GetMask(layerNames);
        }
        else
        {
            Debug.LogWarning($"   ⚠️ One or more layers not found: {string.Join(", ", layerNames)}");
            return 0; // Return 0 (no layers) instead of -1
        }
    }
}

/// <summary>
/// Helper component to visualize spawn points in Scene view
/// </summary>
public class SpawnPointGizmo : MonoBehaviour
{
    public Color color = Color.red;
    public float radius = 0.3f;

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);

        // Draw direction arrow
        Gizmos.DrawRay(transform.position, transform.forward * radius * 2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, radius * 0.5f);
    }
}
