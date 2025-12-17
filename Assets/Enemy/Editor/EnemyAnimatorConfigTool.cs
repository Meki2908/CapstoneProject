using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Tool cấu hình Animator Controller cho Enemy
/// Tự động tạo states, parameters, transitions dựa trên cấu hình chuẩn
/// </summary>
public class EnemyAnimatorConfigTool : EditorWindow
{
    [System.Serializable]
    public class AnimationMapping
    {
        public string stateName;
        public string[] searchKeywords;
        public bool isRequired;
        public bool shouldLoop = true;
        
        public AnimationMapping(string stateName, string[] keywords, bool required = false, bool loop = true)
        {
            this.stateName = stateName;
            this.searchKeywords = keywords;
            this.isRequired = required;
            this.shouldLoop = loop;
        }
    }

    // Animation mappings - tên state và keywords để tìm animation
    private static readonly AnimationMapping[] ANIMATION_MAPPINGS = new AnimationMapping[]
    {
        new AnimationMapping("idle", new[] { "idle" }, true, true),
        new AnimationMapping("walk", new[] { "walk" }, true, true),
        new AnimationMapping("run", new[] { "run" }, true, true),
        new AnimationMapping("battleidle", new[] { "battleidle", "battle_idle", "combatidle" }, false, true),
        new AnimationMapping("attack1", new[] { "attack1", "attack_1", "attack" }, true, false),
        new AnimationMapping("attack2", new[] { "attack2", "attack_2" }, false, false),
        new AnimationMapping("hit", new[] { "hit", "hurt", "damage" }, false, false),
        new AnimationMapping("sturn", new[] { "sturn", "stun", "stunned" }, false, false),
        new AnimationMapping("die", new[] { "die", "death", "dead" }, false, false)
    };

    // UI Variables
    private GameObject targetEnemy;
    private AnimatorController targetController;
    
    private bool autoFindAnimations = true;
    private bool createNewController = false;
    private bool overwriteExisting = false;
    
    private Dictionary<string, AnimationClip> foundClips = new Dictionary<string, AnimationClip>();
    private Vector2 scrollPosition;
    
    // Settings
    private float walkSpeedThreshold = 0.1f;
    private float runSpeedThreshold = 3f;
    private float transitionDuration = 0.2f;
    private float attackExitTime = 0.84f;
    private float battleIdleExitTime = 0.9f;
    private float hitExitTime = 0.6875f;
    private float sturnExitTime = 0.848f;

    [MenuItem("Tools/Enemy Animator Config Tool")]
    public static void ShowWindow()
    {
        EnemyAnimatorConfigTool window = GetWindow<EnemyAnimatorConfigTool>("Enemy Animator Config");
        window.minSize = new Vector2(500, 700);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("🎬 ENEMY ANIMATOR CONFIGURATION TOOL", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Tool tự động cấu hình Animator Controller cho enemy:\n" +
            "• Tự động tìm animation clips\n" +
            "• Tạo states (idle, walk, run, attack1, attack2, hit, die, etc.)\n" +
            "• Tạo parameters (Speed, Attack, Die, IsHurt)\n" +
            "• Tạo transitions với logic đúng",
            MessageType.Info
        );
        
        EditorGUILayout.Space(10);
        
        // Target Selection
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Target Enemy", EditorStyles.boldLabel);
        targetEnemy = (GameObject)EditorGUILayout.ObjectField("Enemy GameObject/Prefab", targetEnemy, typeof(GameObject), true);
        
        if (targetEnemy != null)
        {
            Animator anim = targetEnemy.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                targetController = anim.runtimeAnimatorController as AnimatorController;
                if (targetController != null)
                {
                    EditorGUILayout.HelpBox($"Current Controller: {targetController.name}", MessageType.Info);
                }
            }
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Options
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        autoFindAnimations = EditorGUILayout.Toggle("Auto Find Animations", autoFindAnimations);
        createNewController = EditorGUILayout.Toggle("Create New Controller", createNewController);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Animation Clips Found
        if (autoFindAnimations && targetEnemy != null)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Found Animation Clips", EditorStyles.boldLabel);
            
            if (foundClips.Count == 0)
            {
                if (GUILayout.Button("🔍 Search Animations"))
                {
                    SearchAnimations();
                }
            }
            else
            {
                foreach (var mapping in ANIMATION_MAPPINGS)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{mapping.stateName}:", GUILayout.Width(120));
                    
                    if (foundClips.ContainsKey(mapping.stateName))
                    {
                        EditorGUILayout.ObjectField(foundClips[mapping.stateName], typeof(AnimationClip), false);
                        EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Not Found", EditorStyles.miniLabel);
                        if (mapping.isRequired)
                        {
                            EditorGUILayout.LabelField("⚠️ Required", EditorStyles.miniLabel);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                if (GUILayout.Button("🔍 Re-search Animations"))
                {
                    SearchAnimations();
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.Space(10);
        
        // Advanced Settings
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Walk Speed Threshold:", GUILayout.Width(150));
        walkSpeedThreshold = EditorGUILayout.FloatField(walkSpeedThreshold);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Run Speed Threshold:", GUILayout.Width(150));
        runSpeedThreshold = EditorGUILayout.FloatField(runSpeedThreshold);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Transition Duration:", GUILayout.Width(150));
        transitionDuration = EditorGUILayout.FloatField(transitionDuration);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Action Buttons
        EditorGUILayout.BeginVertical("box");
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🚀 CONFIGURE ANIMATOR", GUILayout.Height(50)))
        {
            ConfigureAnimator();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Controller Only", GUILayout.Height(30)))
        {
            CreateControllerOnly();
        }
        if (GUILayout.Button("Fix Existing Controller", GUILayout.Height(30)))
        {
            FixExistingController();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Info
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("📋 States & Parameters", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("States: idle, walk, run, battleidle, attack1, attack2, hit, sturn, die");
        EditorGUILayout.LabelField("Parameters: Speed (Float), Attack (Trigger), Die (Trigger), IsHurt (Bool)");
        EditorGUILayout.LabelField("Transitions:");
        EditorGUILayout.LabelField("  • idle ↔ walk (Speed threshold)");
        EditorGUILayout.LabelField("  • walk ↔ run (Speed > 3)");
        EditorGUILayout.LabelField("  • [ANY] → attack1 (Attack trigger)");
        EditorGUILayout.LabelField("  • [ANY] → hit (IsHurt = true)");
        EditorGUILayout.LabelField("  • [ANY] → die (Die trigger)");
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndScrollView();
    }

    private void SearchAnimations()
    {
        if (targetEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target enemy first!", "OK");
            return;
        }

        foundClips.Clear();
        
        // Get enemy folder path
        string prefabPath = AssetDatabase.GetAssetPath(targetEnemy);
        if (string.IsNullOrEmpty(prefabPath))
        {
            prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetEnemy);
        }

        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning("Cannot find prefab path. Searching in Assets/Enemy...");
            SearchInDirectory("Assets/Enemy");
        }
        else
        {
            string directory = Path.GetDirectoryName(prefabPath).Replace("\\", "/");
            SearchInDirectory(directory);
        }

        Debug.Log($"✅ Found {foundClips.Count} animation clips");
    }

    private void SearchInDirectory(string directory)
    {
        // Search for .anim files
        string[] animFiles = Directory.GetFiles(directory, "*.anim", SearchOption.AllDirectories);
        foreach (string file in animFiles)
        {
            string relativePath = file.Replace("\\", "/");
            if (!relativePath.StartsWith("Assets/"))
            {
                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            }
            
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);
            if (clip != null)
            {
                MatchClipToState(clip);
            }
        }

        // Search for FBX files with animations
        string[] fbxFiles = Directory.GetFiles(directory, "*.FBX", SearchOption.AllDirectories);
        string[] fbxFilesLower = Directory.GetFiles(directory, "*.fbx", SearchOption.AllDirectories);
        fbxFiles = fbxFiles.Concat(fbxFilesLower).ToArray();
        
        foreach (string file in fbxFiles)
        {
            string relativePath = file.Replace("\\", "/");
            if (!relativePath.StartsWith("Assets/"))
            {
                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            }
            
            // Load FBX and extract animation clips
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(relativePath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip)
                {
                    MatchClipToState(clip);
                }
            }
        }
    }

    private void MatchClipToState(AnimationClip clip)
    {
        string clipNameLower = clip.name.ToLower();
        
        foreach (var mapping in ANIMATION_MAPPINGS)
        {
            // Skip if already found
            if (foundClips.ContainsKey(mapping.stateName))
                continue;
            
            // Check if clip name matches any keyword
            foreach (string keyword in mapping.searchKeywords)
            {
                if (clipNameLower.Contains(keyword.ToLower()))
                {
                    foundClips[mapping.stateName] = clip;
                    Debug.Log($"  ✓ Matched: {clip.name} → {mapping.stateName}");
                    return;
                }
            }
        }
    }

    private void ConfigureAnimator()
    {
        if (targetEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target enemy!", "OK");
            return;
        }

        // Search animations if needed
        if (autoFindAnimations && foundClips.Count == 0)
        {
            SearchAnimations();
        }

        // Get or create Animator component
        Animator animator = targetEnemy.GetComponent<Animator>();
        if (animator == null)
        {
            animator = targetEnemy.AddComponent<Animator>();
            Debug.Log($"✅ Added Animator component to {targetEnemy.name}");
        }

        // Get or create Controller
        AnimatorController controller = GetOrCreateController();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to create/get controller!", "OK");
            return;
        }

        // Setup controller
        SetupController(controller);

        // Assign to animator
        animator.runtimeAnimatorController = controller;

        // Save
        EditorUtility.SetDirty(targetEnemy);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"✅ Animator configured successfully for {targetEnemy.name}!", "OK");
        Debug.Log($"✅ Animator configuration complete for {targetEnemy.name}");
    }

    private AnimatorController GetOrCreateController()
    {
        // Use existing if not overwriting
        if (!createNewController && targetController != null && !overwriteExisting)
        {
            return targetController;
        }

        // Get path for new controller
        string prefabPath = AssetDatabase.GetAssetPath(targetEnemy);
        if (string.IsNullOrEmpty(prefabPath))
        {
            prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetEnemy);
        }

        string directory = "Assets";
        if (!string.IsNullOrEmpty(prefabPath))
        {
            directory = Path.GetDirectoryName(prefabPath).Replace("\\", "/");
            string controllerFolder = Path.Combine(directory, "Controller").Replace("\\", "/");
            
            if (!AssetDatabase.IsValidFolder(controllerFolder))
            {
                string parentFolder = directory;
                AssetDatabase.CreateFolder(parentFolder, "Controller");
            }
            
            directory = controllerFolder;
        }

        string controllerName = $"{targetEnemy.name}_Controller";
        string controllerPath = Path.Combine(directory, $"{controllerName}.controller").Replace("\\", "/");

        // Check if exists
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null && !overwriteExisting)
        {
            return existing;
        }

        // Create new
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        Debug.Log($"✅ Created controller at: {controllerPath}");
        return controller;
    }

    private void SetupController(AnimatorController controller)
    {
        // Clear if overwriting
        if (overwriteExisting)
        {
            controller.layers = new AnimatorControllerLayer[0];
            controller.parameters = new AnimatorControllerParameter[0];
        }

        // Get or create base layer
        AnimatorControllerLayer baseLayer = controller.layers.Length > 0
            ? controller.layers[0]
            : new AnimatorControllerLayer
            {
                name = "Base Layer",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine()
            };

        if (controller.layers.Length == 0)
        {
            controller.AddLayer(baseLayer);
        }

        AnimatorStateMachine stateMachine = baseLayer.stateMachine;

        // Setup Parameters
        AddParameter(controller, "Speed", AnimatorControllerParameterType.Float, 0f);
        AddParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
        AddParameter(controller, "IsHurt", AnimatorControllerParameterType.Bool, 0f, false);

        // Create States
        Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();
        
        foreach (var mapping in ANIMATION_MAPPINGS)
        {
            AnimationClip clip = foundClips.ContainsKey(mapping.stateName) ? foundClips[mapping.stateName] : null;
            
            if (clip != null || !mapping.isRequired)
            {
                AnimatorState state = CreateOrGetState(stateMachine, mapping.stateName, clip);
                if (state != null)
                {
                    states[mapping.stateName] = state;
                }
            }
        }

        // Set default state
        if (states.ContainsKey("idle"))
        {
            stateMachine.defaultState = states["idle"];
        }

        // Setup Transitions
        SetupTransitions(stateMachine, states);

        Debug.Log($"✅ Controller setup complete: {states.Count} states created");
    }

    private void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, float defaultValue = 0f, bool boolDefaultValue = false)
    {
        // Check if exists
        foreach (var existingParam in controller.parameters)
        {
            if (existingParam.name == name)
            {
                if (overwriteExisting)
                {
                    controller.RemoveParameter(existingParam);
                }
                else
                {
                    return;
                }
            }
        }

        AnimatorControllerParameter newParam = new AnimatorControllerParameter
        {
            name = name,
            type = type
        };

        if (type == AnimatorControllerParameterType.Float)
        {
            newParam.defaultFloat = defaultValue;
        }
        else if (type == AnimatorControllerParameterType.Bool)
        {
            newParam.defaultBool = boolDefaultValue;
        }

        controller.AddParameter(newParam);
        Debug.Log($"  ✓ Added parameter: {name} ({type})");
    }

    private AnimatorState CreateOrGetState(AnimatorStateMachine stateMachine, string name, AnimationClip clip)
    {
        // Check if exists
        foreach (var state in stateMachine.states)
        {
            if (state.state.name == name)
            {
                if (clip != null && overwriteExisting)
                {
                    state.state.motion = clip;
                }
                return state.state;
            }
        }

        // Create new
        AnimatorState newState = stateMachine.AddState(name);
        if (clip != null)
        {
            newState.motion = clip;
        }
        
        Debug.Log($"  ✓ Created state: {name} {(clip != null ? $"({clip.name})" : "(no clip)")}");
        return newState;
    }

    private void SetupTransitions(AnimatorStateMachine stateMachine, Dictionary<string, AnimatorState> states)
    {
        // Idle ↔ Walk
        if (states.ContainsKey("idle") && states.ContainsKey("walk"))
        {
            CreateTransition(states["idle"], states["walk"], "Speed", walkSpeedThreshold, AnimatorConditionMode.Greater);
            CreateTransition(states["walk"], states["idle"], "Speed", walkSpeedThreshold, AnimatorConditionMode.Less);
        }

        // Walk ↔ Run
        if (states.ContainsKey("walk") && states.ContainsKey("run"))
        {
            CreateTransition(states["walk"], states["run"], "Speed", runSpeedThreshold, AnimatorConditionMode.Greater);
            CreateTransition(states["run"], states["walk"], "Speed", runSpeedThreshold, AnimatorConditionMode.Less);
        }

        // Run → Walk (if speed drops)
        if (states.ContainsKey("run") && states.ContainsKey("walk"))
        {
            CreateTransition(states["run"], states["walk"], "Speed", runSpeedThreshold, AnimatorConditionMode.Less);
        }

        // Attack1 from ANY STATE
        if (states.ContainsKey("attack1"))
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(states["attack1"]);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            
            // Attack1 → BattleIdle or Idle
            if (states.ContainsKey("battleidle"))
            {
                CreateTransition(states["attack1"], states["battleidle"], "", 0f, AnimatorConditionMode.If, true, attackExitTime);
            }
            else if (states.ContainsKey("idle"))
            {
                CreateTransition(states["attack1"], states["idle"], "", 0f, AnimatorConditionMode.If, true, attackExitTime);
            }
        }

        // Attack2 from ANY STATE
        if (states.ContainsKey("attack2"))
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(states["attack2"]);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            
            // Attack2 → BattleIdle
            if (states.ContainsKey("battleidle"))
            {
                CreateTransition(states["attack2"], states["battleidle"], "", 0f, AnimatorConditionMode.If, true, 0.8f);
            }
        }

        // BattleIdle → Idle
        if (states.ContainsKey("battleidle") && states.ContainsKey("idle"))
        {
            CreateTransition(states["battleidle"], states["idle"], "", 0f, AnimatorConditionMode.If, true, battleIdleExitTime);
        }

        // Hit from ANY STATE
        if (states.ContainsKey("hit"))
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(states["hit"]);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "IsHurt");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            transition.exitTime = 0.75f;
            
            // Hit → Sturn
            if (states.ContainsKey("sturn"))
            {
                CreateTransition(states["hit"], states["sturn"], "", 0f, AnimatorConditionMode.If, true, hitExitTime);
            }
        }

        // Sturn → Walk
        if (states.ContainsKey("sturn") && states.ContainsKey("walk"))
        {
            CreateTransition(states["sturn"], states["walk"], "", 0f, AnimatorConditionMode.If, true, sturnExitTime);
        }

        // Die from ANY STATE
        if (states.ContainsKey("die"))
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(states["die"]);
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Die");
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            // Die has no exit transitions (END STATE)
        }

        Debug.Log($"  ✓ Transitions configured");
    }

    private void CreateTransition(AnimatorState from, AnimatorState to, string parameterName, float threshold, AnimatorConditionMode mode, bool hasExitTime = false, float exitTime = 0.9f)
    {
        if (from == null || to == null) return;

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? exitTime : 0f;
        transition.duration = transitionDuration;

        if (!string.IsNullOrEmpty(parameterName))
        {
            transition.AddCondition(mode, threshold, parameterName);
        }
    }

    private void CreateControllerOnly()
    {
        if (targetEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target enemy!", "OK");
            return;
        }

        AnimatorController controller = GetOrCreateController();
        if (controller != null)
        {
            SetupController(controller);
            EditorUtility.DisplayDialog("Success", "Controller created and configured!", "OK");
        }
    }

    private void FixExistingController()
    {
        if (targetEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target enemy!", "OK");
            return;
        }

        Animator animator = targetEnemy.GetComponent<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            EditorUtility.DisplayDialog("Error", "Target has no animator or controller!", "OK");
            return;
        }

        AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Controller is not editable!", "OK");
            return;
        }

        bool originalOverwrite = overwriteExisting;
        overwriteExisting = false;

        SearchAnimations();
        SetupController(controller);

        overwriteExisting = originalOverwrite;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", "Existing controller fixed!", "OK");
    }
}

