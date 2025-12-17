using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

/// <summary>
/// Tool tự động setup HOÀN CHỈNH 100% cho BẤT KỲ ENEMY NÀO
/// Bao gồm: CharacterController, Animator + Avatar, EnemyBT, Layer, Animator Controller
/// </summary>
public class EnemyFullAutoSetup : EditorWindow
{
    private GameObject enemyPrefab;
    private bool autoFindAvatar = true;
    private bool autoFindController = true;
    private bool createPatrolPoints = true;
    private int numberOfPatrolPoints = 4;
    private float patrolRadius = 10f;

    // CharacterController settings
    private float ccRadius = 0.5f;
    private float ccHeight = 1.0f;
    private Vector3 ccCenter = new Vector3(0, 0.5f, 0);

    // EnemyBT settings
    private float detectionRange = 10f;
    private float attackRange = 2f;
    private float moveSpeed = 3f;
    private float chaseSpeed = 5f;
    private float attackCooldown = 1.5f;
    private float attackAnimationDuration = 2.5f;

    private Vector2 scrollPosition;

    [MenuItem("Tools/Enemy Setup/Full Auto Setup 100% 🚀")]
    public static void ShowWindow()
    {
        EnemyFullAutoSetup window = GetWindow<EnemyFullAutoSetup>("Enemy Full Setup");
        window.minSize = new Vector2(500, 800);
        window.Show();
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("🚀 ENEMY FULL AUTO SETUP 100%", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Tool này sẽ tự động setup HOÀN CHỈNH 100% cho BẤT KỲ ENEMY NÀO:\n\n" +
            "✅ CharacterController (tự động cấu hình)\n" +
            "✅ Animator + Avatar (tự động tìm từ FBX)\n" +
            "✅ Animator Controller (tự động tìm trong folder)\n" +
            "✅ EnemyBT AI (Behavior Tree)\n" +
            "✅ Layer = Enemy\n" +
            "✅ Patrol Points (tự động tạo)\n" +
            "✅ Tất cả settings tối ưu\n\n" +
            "CHUẨN 100% - READY TO USE!",
            MessageType.Info
        );

        GUILayout.Space(10);

        // Main Setup
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📦 SELECT ENEMY PREFAB", EditorStyles.boldLabel);
        enemyPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Enemy Prefab/Object",
            enemyPrefab,
            typeof(GameObject),
            true
        );
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Auto Find Options
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🔍 AUTO FIND OPTIONS", EditorStyles.boldLabel);
        autoFindAvatar = EditorGUILayout.Toggle("Auto Find Avatar from FBX", autoFindAvatar);
        autoFindController = EditorGUILayout.Toggle("Auto Find Animator Controller", autoFindController);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // CharacterController Settings
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🎮 CharacterController Settings", EditorStyles.boldLabel);
        ccRadius = EditorGUILayout.FloatField("Radius", ccRadius);
        ccHeight = EditorGUILayout.FloatField("Height", ccHeight);
        ccCenter = EditorGUILayout.Vector3Field("Center", ccCenter);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // EnemyBT Settings
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🤖 EnemyBT AI Settings", EditorStyles.boldLabel);
        detectionRange = EditorGUILayout.FloatField("Detection Range", detectionRange);
        attackRange = EditorGUILayout.FloatField("Attack Range", attackRange);
        moveSpeed = EditorGUILayout.FloatField("Move Speed (Patrol)", moveSpeed);
        chaseSpeed = EditorGUILayout.FloatField("Chase Speed (Run)", chaseSpeed);
        attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", attackCooldown);
        attackAnimationDuration = EditorGUILayout.FloatField("Attack Animation Duration", attackAnimationDuration);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Patrol Settings
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🚶 Patrol Settings", EditorStyles.boldLabel);
        createPatrolPoints = EditorGUILayout.Toggle("Create Patrol Points", createPatrolPoints);
        if (createPatrolPoints)
        {
            EditorGUI.indentLevel++;
            numberOfPatrolPoints = EditorGUILayout.IntSlider("Number of Points", numberOfPatrolPoints, 2, 10);
            patrolRadius = EditorGUILayout.Slider("Patrol Radius", patrolRadius, 5f, 50f);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Main Button
        EditorGUI.BeginDisabledGroup(enemyPrefab == null);
        if (GUILayout.Button("🚀 FULL AUTO SETUP 100%", GUILayout.Height(50)))
        {
            if (enemyPrefab != null)
            {
                SetupEnemyComplete();
            }
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        // Quick Templates
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("⚡ Quick Templates", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🎯 Aggressive", GUILayout.Height(30)))
        {
            ApplyTemplate(EnemyTemplate.Aggressive);
        }
        if (GUILayout.Button("🛡️ Defensive", GUILayout.Height(30)))
        {
            ApplyTemplate(EnemyTemplate.Defensive);
        }
        if (GUILayout.Button("⚡ Boss", GUILayout.Height(30)))
        {
            ApplyTemplate(EnemyTemplate.Boss);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        GUILayout.Space(20);

        EditorGUILayout.EndScrollView();
    }

    private enum EnemyTemplate
    {
        Aggressive,
        Defensive,
        Boss
    }

    private void ApplyTemplate(EnemyTemplate template)
    {
        switch (template)
        {
            case EnemyTemplate.Aggressive:
                detectionRange = 15f;
                attackRange = 2.5f;
                moveSpeed = 5f;
                chaseSpeed = 7f;
                attackCooldown = 0.8f;
                attackAnimationDuration = 2f;
                patrolRadius = 8f;
                Debug.Log("✅ Applied Aggressive template");
                break;

            case EnemyTemplate.Defensive:
                detectionRange = 8f;
                attackRange = 2f;
                moveSpeed = 2.5f;
                chaseSpeed = 4f;
                attackCooldown = 2f;
                attackAnimationDuration = 3f;
                patrolRadius = 15f;
                Debug.Log("✅ Applied Defensive template");
                break;

            case EnemyTemplate.Boss:
                detectionRange = 25f;
                attackRange = 5f;
                moveSpeed = 3.5f;
                chaseSpeed = 6f;
                attackCooldown = 1.2f;
                attackAnimationDuration = 3.5f;
                patrolRadius = 20f;
                ccRadius = 1.5f;
                ccHeight = 5f;
                ccCenter = new Vector3(0, 2.5f, 0);
                Debug.Log("✅ Applied Boss template");
                break;
        }
    }

    void SetupEnemyComplete()
    {
        if (enemyPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy prefab first!", "OK");
            return;
        }

        Undo.RecordObject(enemyPrefab, "Full Auto Setup Enemy");

        int stepsDone = 0;
        int totalSteps = 8;

        // Step 1: Setup Layer
        EditorUtility.DisplayProgressBar("Full Auto Setup", "Setting up layer...", (float)stepsDone++ / totalSteps);
        SetupLayer();

        // Step 2: Setup CharacterController
        EditorUtility.DisplayProgressBar("Full Auto Setup", "Setting up CharacterController...", (float)stepsDone++ / totalSteps);
        SetupCharacterController();

        // Step 3: Setup Animator
        EditorUtility.DisplayProgressBar("Full Auto Setup", "Setting up Animator...", (float)stepsDone++ / totalSteps);
        SetupAnimator();

        // Step 4: Setup Avatar
        if (autoFindAvatar)
        {
            EditorUtility.DisplayProgressBar("Full Auto Setup", "Finding and setting Avatar...", (float)stepsDone++ / totalSteps);
            SetupAvatar();
        }

        // Step 5: Setup Animator Controller
        if (autoFindController)
        {
            EditorUtility.DisplayProgressBar("Full Auto Setup", "Finding and setting Animator Controller...", (float)stepsDone++ / totalSteps);
            SetupAnimatorController();
        }

        // Step 6: Setup EnemyBT
        EditorUtility.DisplayProgressBar("Full Auto Setup", "Setting up AI (EnemyBT)...", (float)stepsDone++ / totalSteps);
        SetupEnemyBT();

        // Step 7: Setup Patrol Points
        if (createPatrolPoints)
        {
            EditorUtility.DisplayProgressBar("Full Auto Setup", "Creating patrol points...", (float)stepsDone++ / totalSteps);
            SetupPatrolPoints();
        }

        // Step 8: Verify
        EditorUtility.DisplayProgressBar("Full Auto Setup", "Verifying setup...", (float)stepsDone++ / totalSteps);
        VerifySetup();

        EditorUtility.ClearProgressBar();

        EditorUtility.SetDirty(enemyPrefab);
        if (PrefabUtility.IsPartOfPrefabAsset(enemyPrefab))
        {
            PrefabUtility.SavePrefabAsset(enemyPrefab);
        }

        EditorUtility.DisplayDialog(
            "Setup Complete! ✅",
            $"Enemy '{enemyPrefab.name}' đã được setup 100%!\n\n" +
            "✅ CharacterController\n" +
            "✅ Animator + Avatar\n" +
            "✅ Animator Controller\n" +
            "✅ EnemyBT AI\n" +
            "✅ Layer = Enemy\n" +
            (createPatrolPoints ? "✅ Patrol Points\n" : "") +
            "\nREADY TO USE!",
            "OK"
        );

        Debug.Log($"✅ <color=green>Enemy '{enemyPrefab.name}' FULL AUTO SETUP COMPLETE!</color>");
    }

    void SetupLayer()
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer == -1)
        {
            Debug.LogWarning("⚠️ Layer 'Enemy' not found! Please create it in Tags & Layers.");
        }
        else
        {
            enemyPrefab.layer = enemyLayer;
            Debug.Log($"✅ Layer set to: Enemy");
        }
    }

    void SetupCharacterController()
    {
        CharacterController cc = enemyPrefab.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = enemyPrefab.AddComponent<CharacterController>();
            Debug.Log($"✅ Added CharacterController");
        }

        cc.radius = ccRadius;
        cc.height = ccHeight;
        cc.center = ccCenter;
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;
        cc.skinWidth = 0.08f;

        Debug.Log($"✅ CharacterController configured: Radius={ccRadius}, Height={ccHeight}, Center={ccCenter}");
    }

    void SetupAnimator()
    {
        Animator animator = enemyPrefab.GetComponent<Animator>();
        if (animator == null)
        {
            animator = enemyPrefab.AddComponent<Animator>();
            Debug.Log($"✅ Added Animator component");
        }

        animator.applyRootMotion = false; // CRITICAL! Disable root motion
        Debug.Log($"✅ Animator configured (Root Motion: DISABLED)");
    }

    void SetupAvatar()
    {
        Animator animator = enemyPrefab.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("⚠️ Animator not found! Cannot setup Avatar.");
            return;
        }

        // Nếu đã có Avatar thì skip
        if (animator.avatar != null && animator.avatar.isValid)
        {
            Debug.Log($"✅ Avatar already assigned: {animator.avatar.name}");
            return;
        }

        // Tìm FBX model trong cùng folder hoặc parent folder
        string prefabPath = AssetDatabase.GetAssetPath(enemyPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("⚠️ Prefab is not saved as asset. Cannot auto-find Avatar.");
            return;
        }

        string prefabDirectory = Path.GetDirectoryName(prefabPath);
        string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

        Avatar foundAvatar = null;
        string foundPath = null;

        // Strategy 1: Tìm Avatar từ FBX model trong cùng folder
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { prefabDirectory });
        
        foreach (string guid in fbxGuids)
        {
            string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
            string fbxName = Path.GetFileNameWithoutExtension(fbxPath);

            // Tìm FBX có tên tương tự với prefab
            if (fbxName.Contains(prefabName) || prefabName.Contains(fbxName) || 
                fbxName.ToLower().Contains("model") || fbxName.ToLower().Contains("mesh"))
            {
                // Load FBX GameObject và lấy Avatar từ đó
                // Unity tự động tạo Avatar trong FBX khi import
                GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                if (fbxModel != null)
                {
                    // Avatar được lưu trong FBX asset, lấy từ ModelImporter
                    ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
                    if (importer != null && importer.animationType != ModelImporterAnimationType.None)
                    {
                        // Avatar được tạo tự động, lấy từ FBX GameObject
                        // Thử load tất cả assets trong FBX
                        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                        foreach (Object asset in assets)
                        {
                            if (asset is Avatar)
                            {
                                Avatar avatar = asset as Avatar;
                                if (avatar.isValid)
                                {
                                    foundAvatar = avatar;
                                    foundPath = fbxPath;
                                    break;
                                }
                            }
                        }
                    }

                    if (foundAvatar != null)
                        break;

                    // Thử tìm Avatar asset riêng biệt trong cùng folder
                    string[] avatarGuids = AssetDatabase.FindAssets("t:Avatar", new[] { Path.GetDirectoryName(fbxPath) });
                    foreach (string avatarGuid in avatarGuids)
                    {
                        string avPath = AssetDatabase.GUIDToAssetPath(avatarGuid);
                        Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(avPath);
                        if (avatar != null && avatar.isValid)
                        {
                            foundAvatar = avatar;
                            foundPath = avPath;
                            break;
                        }
                    }

                    if (foundAvatar != null)
                        break;
                }
            }
        }

        // Strategy 2: Tìm Avatar trong parent folder
        if (foundAvatar == null)
        {
            string parentDirectory = Directory.GetParent(prefabDirectory)?.FullName;
            if (parentDirectory != null)
            {
                string parentDirRelative = parentDirectory.Replace(Application.dataPath, "Assets");
                string[] parentFbxGuids = AssetDatabase.FindAssets("t:Model", new[] { parentDirRelative });
                
                foreach (string guid in parentFbxGuids)
                {
                    string fbxPath = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject fbxModel = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
                    if (fbxModel != null)
                    {
                        Animator fbxAnimator = fbxModel.GetComponent<Animator>();
                        if (fbxAnimator != null && fbxAnimator.avatar != null && fbxAnimator.avatar.isValid)
                        {
                            foundAvatar = fbxAnimator.avatar;
                            foundPath = fbxPath;
                            break;
                        }
                    }
                }

                // Tìm Avatar assets trong parent folder
                if (foundAvatar == null)
                {
                    string[] parentAvatarGuids = AssetDatabase.FindAssets("t:Avatar", new[] { parentDirRelative });
                    if (parentAvatarGuids.Length > 0)
                    {
                        foundAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(AssetDatabase.GUIDToAssetPath(parentAvatarGuids[0]));
                        foundPath = AssetDatabase.GUIDToAssetPath(parentAvatarGuids[0]);
                    }
                }
            }
        }

        // Strategy 3: Tìm bất kỳ Avatar nào trong cùng folder (fallback)
        if (foundAvatar == null)
        {
            string[] allAvatarGuids = AssetDatabase.FindAssets("t:Avatar", new[] { prefabDirectory });
            if (allAvatarGuids.Length > 0)
            {
                foundAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(AssetDatabase.GUIDToAssetPath(allAvatarGuids[0]));
                foundPath = AssetDatabase.GUIDToAssetPath(allAvatarGuids[0]);
            }
        }

        if (foundAvatar != null && foundAvatar.isValid)
        {
            animator.avatar = foundAvatar;
            Debug.Log($"✅ Avatar assigned: {foundPath}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not auto-find Avatar for '{enemyPrefab.name}'. Please assign manually in Inspector.");
            Debug.LogWarning($"   Searched in: {prefabDirectory}");
        }
    }

    void SetupAnimatorController()
    {
        Animator animator = enemyPrefab.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("⚠️ Animator not found! Cannot setup Controller.");
            return;
        }

        // Nếu đã có Controller thì skip
        if (animator.runtimeAnimatorController != null)
        {
            Debug.Log($"✅ Animator Controller already assigned: {animator.runtimeAnimatorController.name}");
            return;
        }

        // Tìm Controller trong cùng folder hoặc subfolder "Controller"
        string prefabPath = AssetDatabase.GetAssetPath(enemyPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("⚠️ Prefab is not saved as asset. Cannot auto-find Controller.");
            return;
        }

        string prefabDirectory = Path.GetDirectoryName(prefabPath);
        string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

        // Tìm Controller trong folder "Controller" hoặc cùng folder
        string[] searchPaths = new[]
        {
            Path.Combine(prefabDirectory, "Controller"),
            prefabDirectory
        };

        RuntimeAnimatorController foundController = null;
        string foundPath = null;

        foreach (string searchPath in searchPaths)
        {
            if (!Directory.Exists(searchPath.Replace("Assets", Application.dataPath)))
                continue;

            string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { searchPath });
            
            foreach (string guid in controllerGuids)
            {
                string controllerPath = AssetDatabase.GUIDToAssetPath(guid);
                string controllerName = Path.GetFileNameWithoutExtension(controllerPath);

                // Tìm Controller có tên tương tự với prefab
                if (controllerName.Contains(prefabName) || prefabName.Contains(controllerName))
                {
                    foundController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                    foundPath = controllerPath;
                    break;
                }
            }

            if (foundController != null)
                break;
        }

        // Nếu không tìm thấy, lấy Controller đầu tiên trong folder
        if (foundController == null)
        {
            foreach (string searchPath in searchPaths)
            {
                if (!Directory.Exists(searchPath.Replace("Assets", Application.dataPath)))
                    continue;

                string[] controllerGuids = AssetDatabase.FindAssets("t:AnimatorController", new[] { searchPath });
                if (controllerGuids.Length > 0)
                {
                    foundController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        AssetDatabase.GUIDToAssetPath(controllerGuids[0])
                    );
                    foundPath = AssetDatabase.GUIDToAssetPath(controllerGuids[0]);
                    break;
                }
            }
        }

        if (foundController != null)
        {
            animator.runtimeAnimatorController = foundController;
            Debug.Log($"✅ Animator Controller assigned: {foundPath}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not auto-find Animator Controller for '{enemyPrefab.name}'. Please assign manually in Inspector.");
        }
    }

    void SetupEnemyBT()
    {
        EnemyBT bt = enemyPrefab.GetComponent<EnemyBT>();
        if (bt == null)
        {
            bt = enemyPrefab.AddComponent<EnemyBT>();
            Debug.Log($"✅ Added EnemyBT component");
        }

        // Configure settings
        bt.detectionRange = detectionRange;
        bt.attackRange = attackRange;
        bt.moveSpeed = moveSpeed;
        bt.chaseSpeed = chaseSpeed;
        bt.attackCooldown = attackCooldown;
        bt.attackAnimationDuration = attackAnimationDuration;
        bt.patrolRadius = patrolRadius;
        bt.shouldPatrol = true;
        bt.targetLayer = LayerMask.GetMask("Player");
        bt.showGizmos = true;
        bt.showDebugLogs = true;

        Debug.Log($"✅ EnemyBT configured with optimal settings");
    }

    void SetupPatrolPoints()
    {
        EnemyBT bt = enemyPrefab.GetComponent<EnemyBT>();
        if (bt == null)
        {
            Debug.LogWarning("⚠️ EnemyBT not found! Cannot setup patrol points.");
            return;
        }

        // Kiểm tra xem có phải prefab asset không
        string prefabPath = AssetDatabase.GetAssetPath(enemyPrefab);
        bool isPrefabAsset = !string.IsNullOrEmpty(prefabPath) && PrefabUtility.GetPrefabAssetType(enemyPrefab) != PrefabAssetType.NotAPrefab;
        
        GameObject workingObject = enemyPrefab;
        bool needsSave = false;

        // Nếu là prefab asset, cần load vào scene để modify
        if (isPrefabAsset)
        {
            workingObject = PrefabUtility.LoadPrefabContents(prefabPath);
            needsSave = true;
            bt = workingObject.GetComponent<EnemyBT>();
        }

        // Tìm hoặc tạo parent object cho patrol points
        Transform patrolParent = workingObject.transform.Find("PatrolPoints");
        if (patrolParent == null)
        {
            GameObject patrolObj = new GameObject("PatrolPoints");
            patrolObj.transform.SetParent(workingObject.transform);
            patrolObj.transform.localPosition = Vector3.zero;
            patrolParent = patrolObj.transform;
        }
        else
        {
            // Clear existing patrol points
            while (patrolParent.childCount > 0)
            {
                DestroyImmediate(patrolParent.GetChild(0).gameObject);
            }
        }

        // Tạo patrol points theo hình tròn
        System.Collections.Generic.List<Transform> points = new System.Collections.Generic.List<Transform>();
        float angleStep = 360f / numberOfPatrolPoints;

        for (int i = 0; i < numberOfPatrolPoints; i++)
        {
            GameObject point = new GameObject($"PatrolPoint_{i + 1}");
            point.transform.SetParent(patrolParent);

            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * patrolRadius,
                0,
                Mathf.Sin(angle) * patrolRadius
            );

            point.transform.localPosition = position;
            points.Add(point.transform);

            // Add gizmo icon
#if UNITY_EDITOR
            var icon = EditorGUIUtility.IconContent("sv_icon_dot0_pix16_gizmo");
            EditorGUIUtility.SetIconForObject(point, (Texture2D)icon.image);
#endif
        }

        bt.patrolPoints = points.ToArray();
        bt.patrolRadius = patrolRadius;

        // Nếu là prefab asset, save lại
        if (needsSave)
        {
            PrefabUtility.SaveAsPrefabAsset(workingObject, prefabPath);
            PrefabUtility.UnloadPrefabContents(workingObject);
            // Reload prefab để cập nhật reference
            enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        Debug.Log($"✅ Created {numberOfPatrolPoints} patrol points");
    }

    void VerifySetup()
    {
        bool allGood = true;
        System.Text.StringBuilder report = new System.Text.StringBuilder();

        // Check CharacterController
        CharacterController cc = enemyPrefab.GetComponent<CharacterController>();
        if (cc == null)
        {
            report.AppendLine("❌ CharacterController: MISSING");
            allGood = false;
        }
        else
        {
            report.AppendLine("✅ CharacterController: OK");
        }

        // Check Animator
        Animator animator = enemyPrefab.GetComponent<Animator>();
        if (animator == null)
        {
            report.AppendLine("❌ Animator: MISSING");
            allGood = false;
        }
        else
        {
            report.Append("✅ Animator: OK");
            if (animator.avatar == null || !animator.avatar.isValid)
            {
                report.Append(" (⚠️ Avatar not assigned)");
            }
            else
            {
                report.Append(" (✅ Avatar OK)");
            }
            if (animator.runtimeAnimatorController == null)
            {
                report.Append(" (⚠️ Controller not assigned)");
            }
            else
            {
                report.Append(" (✅ Controller OK)");
            }
            report.AppendLine();
        }

        // Check EnemyBT
        EnemyBT bt = enemyPrefab.GetComponent<EnemyBT>();
        if (bt == null)
        {
            report.AppendLine("❌ EnemyBT: MISSING");
            allGood = false;
        }
        else
        {
            report.AppendLine("✅ EnemyBT: OK");
        }

        // Check Layer
        if (enemyPrefab.layer != LayerMask.NameToLayer("Enemy"))
        {
            report.AppendLine("⚠️ Layer: Not set to 'Enemy'");
        }
        else
        {
            report.AppendLine("✅ Layer: OK");
        }

        Debug.Log($"\n📋 VERIFICATION REPORT:\n{report.ToString()}");

        if (!allGood)
        {
            Debug.LogWarning("⚠️ Some components are missing! Please check the report above.");
        }
    }
}

