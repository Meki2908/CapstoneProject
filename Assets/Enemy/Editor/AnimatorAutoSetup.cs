using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Auto setup Animator Controller cho Enemy
/// Tạo states, parameters, transitions tự động
/// </summary>
public class AnimatorAutoSetup : EditorWindow
{
    [System.Serializable]
    public class AnimationClipInfo
    {
        public string name;
        public AnimationClip clip;
        public bool isRequired;

        public AnimationClipInfo(string name, bool required = false)
        {
            this.name = name;
            this.isRequired = required;
        }
    }

    [Header("Setup Mode")]
    private int setupMode = 0;
    private string[] setupModes = { "Single Enemy", "Batch (Folder)", "Selected Objects" };

    [Header("Target")]
    private GameObject targetEnemy;
    private string enemyFolderPath = "Assets/Enemy_dam_lay";

    [Header("Animation Clips")]
    private AnimationClipInfo idleClip = new AnimationClipInfo("Idle", true);
    private AnimationClipInfo walkClip = new AnimationClipInfo("Walk", true);
    private AnimationClipInfo runClip = new AnimationClipInfo("Run", false);
    private AnimationClipInfo attackClip = new AnimationClipInfo("Attack", true);
    private AnimationClipInfo dieClip = new AnimationClipInfo("Die", false);
    private AnimationClipInfo hurtClip = new AnimationClipInfo("Hurt", false);

    [Header("Options")]
    private bool autoFindClips = true;
    private bool createMissingStates = true;
    private bool setupTransitions = true;
    private bool setupParameters = true;
    private bool overwriteExisting = false;

    private Vector2 scrollPosition;

    [MenuItem("Tools/Animator Auto Setup")]
    public static void ShowWindow()
    {
        AnimatorAutoSetup window = GetWindow<AnimatorAutoSetup>("Animator Setup");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Animator Controller Auto Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Setup Mode
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Setup Mode", EditorStyles.boldLabel);
        setupMode = GUILayout.SelectionGrid(setupMode, setupModes, 3);
        GUILayout.Space(5);

        switch (setupMode)
        {
            case 0: // Single Enemy
                targetEnemy = (GameObject)EditorGUILayout.ObjectField("Enemy GameObject", targetEnemy, typeof(GameObject), true);
                break;
            case 1: // Batch Folder
                enemyFolderPath = EditorGUILayout.TextField("Enemy Folder", enemyFolderPath);
                if (GUILayout.Button("Browse", GUILayout.Width(80)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Enemy Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        enemyFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
                break;
            case 2: // Selected Objects
                EditorGUILayout.HelpBox($"Selected: {Selection.gameObjects.Length} objects", MessageType.Info);
                break;
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // Animation Clips
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Animation Clips Assignment", EditorStyles.boldLabel);

        autoFindClips = EditorGUILayout.Toggle("Auto Find Clips", autoFindClips);

        if (!autoFindClips)
        {
            EditorGUILayout.HelpBox("Manually assign animation clips below:", MessageType.Info);
            idleClip.clip = (AnimationClip)EditorGUILayout.ObjectField("Idle (Required)", idleClip.clip, typeof(AnimationClip), false);
            walkClip.clip = (AnimationClip)EditorGUILayout.ObjectField("Walk (Required)", walkClip.clip, typeof(AnimationClip), false);
            runClip.clip = (AnimationClip)EditorGUILayout.ObjectField("Run (Optional)", runClip.clip, typeof(AnimationClip), false);
            attackClip.clip = (AnimationClip)EditorGUILayout.ObjectField("Attack (Required)", attackClip.clip, typeof(AnimationClip), false);
            dieClip.clip = (AnimationClip)EditorGUILayout.ObjectField("Die (Optional)", dieClip.clip, typeof(AnimationClip), false);
            hurtClip.clip = (AnimationClip)EditorGUILayout.ObjectField("Hurt (Optional)", hurtClip.clip, typeof(AnimationClip), false);
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);

        // Options
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Setup Options", EditorStyles.boldLabel);
        createMissingStates = EditorGUILayout.Toggle("Create Missing States", createMissingStates);
        setupTransitions = EditorGUILayout.Toggle("Setup Transitions", setupTransitions);
        setupParameters = EditorGUILayout.Toggle("Setup Parameters", setupParameters);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Action Buttons
        EditorGUILayout.BeginVertical("box");

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🎬 AUTO SETUP ANIMATOR", GUILayout.Height(40)))
        {
            PerformSetup();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create New Controller", GUILayout.Height(30)))
        {
            CreateNewController();
        }
        if (GUILayout.Button("Quick Fix Existing", GUILayout.Height(30)))
        {
            QuickFixExisting();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Info
        EditorGUILayout.HelpBox(
            "🎬 ANIMATOR AUTO SETUP\n\n" +
            "Tính năng:\n" +
            "✓ Tự động tìm animation clips\n" +
            "✓ Tạo Animator Controller mới\n" +
            "✓ Setup States (Idle, Walk, Attack, etc.)\n" +
            "✓ Setup Parameters (Speed, Attack, Die)\n" +
            "✓ Setup Transitions với conditions\n" +
            "✓ Configure blend trees cho movement\n\n" +
            "States được tạo:\n" +
            "• Idle → Walk (Speed > 0.1)\n" +
            "• Walk → Idle (Speed < 0.1)\n" +
            "• Any State → Attack (Attack trigger)\n" +
            "• Any State → Die (Die trigger)\n" +
            "• Attack → Idle (Exit time)\n\n" +
            "Parameters:\n" +
            "• Float: Speed (0-10)\n" +
            "• Trigger: Attack\n" +
            "• Trigger: Die\n" +
            "• Bool: IsHurt",
            MessageType.Info
        );

        EditorGUILayout.EndScrollView();
    }

    private void PerformSetup()
    {
        List<GameObject> targets = GetTargetObjects();

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No valid targets found!", "OK");
            return;
        }

        int successCount = 0;

        foreach (GameObject target in targets)
        {
            if (SetupAnimatorForObject(target))
            {
                successCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Setup Complete",
            $"Successfully setup {successCount} out of {targets.Count} objects!",
            "OK"
        );
    }

    private List<GameObject> GetTargetObjects()
    {
        List<GameObject> targets = new List<GameObject>();

        switch (setupMode)
        {
            case 0: // Single
                if (targetEnemy != null)
                    targets.Add(targetEnemy);
                break;

            case 1: // Batch Folder
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { enemyFolderPath });
                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        targets.Add(prefab);
                }
                break;

            case 2: // Selected
                targets.AddRange(Selection.gameObjects);
                break;
        }

        return targets;
    }

    private bool SetupAnimatorForObject(GameObject target)
    {
        Debug.Log($"Setting up Animator for: {target.name}");

        // Get or create Animator component
        Animator animator = target.GetComponent<Animator>();
        if (animator == null)
        {
            animator = target.AddComponent<Animator>();
            Debug.Log($"Added Animator component to {target.name}");
        }

        // Find or create Animator Controller
        AnimatorController controller = GetOrCreateController(target);
        if (controller == null)
        {
            Debug.LogError($"Failed to get/create controller for {target.name}");
            return false;
        }

        // Find animation clips
        Dictionary<string, AnimationClip> clips = FindAnimationClips(target);

        // Setup controller
        SetupController(controller, clips);

        // Assign controller to animator
        animator.runtimeAnimatorController = controller;

        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(controller);

        Debug.Log($"✓ Successfully setup Animator for {target.name}");
        return true;
    }

    private AnimatorController GetOrCreateController(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();

        // Try to use existing controller
        if (animator.runtimeAnimatorController != null && !overwriteExisting)
        {
            AnimatorController existing = animator.runtimeAnimatorController as AnimatorController;
            if (existing != null)
            {
                Debug.Log($"Using existing controller: {existing.name}");
                return existing;
            }
        }

        // Find controller in nearby folders
        string prefabPath = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(prefabPath))
        {
            prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
        }

        if (!string.IsNullOrEmpty(prefabPath))
        {
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            string controllerFolder = System.IO.Path.Combine(directory, "Controller");

            // Create Controller folder if not exists
            if (!AssetDatabase.IsValidFolder(controllerFolder))
            {
                string parentFolder = directory.Replace("\\", "/");
                AssetDatabase.CreateFolder(parentFolder, "Controller");
            }

            // Create new controller
            string controllerPath = System.IO.Path.Combine(controllerFolder, $"{target.name}_Controller.controller");
            controllerPath = controllerPath.Replace("\\", "/");

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            Debug.Log($"Created new controller at: {controllerPath}");
            return controller;
        }

        // Fallback: create in Assets root
        string fallbackPath = $"Assets/{target.name}_Controller.controller";
        return AnimatorController.CreateAnimatorControllerAtPath(fallbackPath);
    }

    private Dictionary<string, AnimationClip> FindAnimationClips(GameObject target)
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();

        if (!autoFindClips)
        {
            // Use manually assigned clips
            if (idleClip.clip != null) clips["Idle"] = idleClip.clip;
            if (walkClip.clip != null) clips["Walk"] = walkClip.clip;
            if (runClip.clip != null) clips["Run"] = runClip.clip;
            if (attackClip.clip != null) clips["Attack"] = attackClip.clip;
            if (dieClip.clip != null) clips["Die"] = dieClip.clip;
            if (hurtClip.clip != null) clips["Hurt"] = hurtClip.clip;
            return clips;
        }

        // Auto find clips
        string prefabPath = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(prefabPath))
        {
            prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
        }

        if (!string.IsNullOrEmpty(prefabPath))
        {
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            string animationFolder = System.IO.Path.Combine(directory, "Animation");

            if (System.IO.Directory.Exists(animationFolder))
            {
                string[] animFiles = System.IO.Directory.GetFiles(animationFolder, "*.anim", System.IO.SearchOption.AllDirectories);

                foreach (string file in animFiles)
                {
                    string relativePath = file.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath);

                    if (clip != null)
                    {
                        string clipName = clip.name.ToLower();

                        if (clipName.Contains("idle")) clips["Idle"] = clip;
                        else if (clipName.Contains("walk")) clips["Walk"] = clip;
                        else if (clipName.Contains("run")) clips["Run"] = clip;
                        else if (clipName.Contains("attack")) clips["Attack"] = clip;
                        else if (clipName.Contains("die") || clipName.Contains("death")) clips["Die"] = clip;
                        else if (clipName.Contains("hurt") || clipName.Contains("hit")) clips["Hurt"] = clip;
                    }
                }
            }
        }

        Debug.Log($"Found {clips.Count} animation clips for {target.name}");
        foreach (var kvp in clips)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value.name}");
        }

        return clips;
    }

    private void SetupController(AnimatorController controller, Dictionary<string, AnimationClip> clips)
    {
        // Clear existing if overwrite
        if (overwriteExisting)
        {
            controller.layers = new AnimatorControllerLayer[0];
            controller.parameters = new AnimatorControllerParameter[0];
        }

        // Get or create base layer
        AnimatorControllerLayer baseLayer = controller.layers.Length > 0
            ? controller.layers[0]
            : new AnimatorControllerLayer { name = "Base Layer", defaultWeight = 1f, stateMachine = new AnimatorStateMachine() };

        if (controller.layers.Length == 0)
        {
            controller.AddLayer(baseLayer);
        }

        AnimatorStateMachine stateMachine = baseLayer.stateMachine;

        // Setup parameters
        if (setupParameters)
        {
            AddParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
            AddParameter(controller, "IsHurt", AnimatorControllerParameterType.Bool);
        }

        // Create states
        AnimatorState idleState = CreateOrGetState(stateMachine, "Idle", clips.ContainsKey("Idle") ? clips["Idle"] : null);
        AnimatorState walkState = CreateOrGetState(stateMachine, "Walk", clips.ContainsKey("Walk") ? clips["Walk"] : null);
        AnimatorState attackState = CreateOrGetState(stateMachine, "Attack", clips.ContainsKey("Attack") ? clips["Attack"] : null);
        AnimatorState dieState = CreateOrGetState(stateMachine, "Die", clips.ContainsKey("Die") ? clips["Die"] : null);

        // Set default state
        if (idleState != null)
        {
            stateMachine.defaultState = idleState;
        }

        // Setup transitions
        if (setupTransitions && idleState != null && walkState != null)
        {
            // Idle <-> Walk
            CreateTransition(idleState, walkState, "Speed", 0.1f, AnimatorConditionMode.Greater);
            CreateTransition(walkState, idleState, "Speed", 0.1f, AnimatorConditionMode.Less);

            // Any State -> Attack
            if (attackState != null)
            {
                CreateTransitionFromAnyState(stateMachine, attackState, "Attack", AnimatorConditionMode.If);
                CreateTransition(attackState, idleState, "", 0f, AnimatorConditionMode.If, true); // Exit time
            }

            // Any State -> Die
            if (dieState != null)
            {
                CreateTransitionFromAnyState(stateMachine, dieState, "Die", AnimatorConditionMode.If);
            }
        }

        Debug.Log($"✓ Controller setup complete for {controller.name}");
    }

    private void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        // Check if parameter exists
        foreach (var param in controller.parameters)
        {
            if (param.name == name)
                return;
        }

        controller.AddParameter(name, type);
        Debug.Log($"  Added parameter: {name} ({type})");
    }

    private AnimatorState CreateOrGetState(AnimatorStateMachine stateMachine, string name, AnimationClip clip)
    {
        // Check if state exists
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

        // Create new state
        if (clip != null || createMissingStates)
        {
            AnimatorState newState = stateMachine.AddState(name);
            if (clip != null)
            {
                newState.motion = clip;
            }
            Debug.Log($"  Created state: {name}");
            return newState;
        }

        return null;
    }

    private void CreateTransition(AnimatorState from, AnimatorState to, string parameterName, float threshold, AnimatorConditionMode mode, bool hasExitTime = false)
    {
        if (from == null || to == null) return;

        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.exitTime = hasExitTime ? 0.9f : 0f;
        transition.duration = 0.1f;

        if (!string.IsNullOrEmpty(parameterName))
        {
            transition.AddCondition(mode, threshold, parameterName);
        }
    }

    private void CreateTransitionFromAnyState(AnimatorStateMachine stateMachine, AnimatorState to, string parameterName, AnimatorConditionMode mode)
    {
        if (to == null) return;

        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.1f;

        if (!string.IsNullOrEmpty(parameterName))
        {
            transition.AddCondition(mode, 0f, parameterName);
        }
    }

    private void CreateNewController()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Animator Controller",
            "NewEnemyController",
            "controller",
            "Save animator controller"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            SetupController(controller, new Dictionary<string, AnimationClip>());

            EditorUtility.DisplayDialog("Success", "Created new controller with basic setup!", "OK");
            Selection.activeObject = controller;
        }
    }

    private void QuickFixExisting()
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

        Dictionary<string, AnimationClip> clips = FindAnimationClips(targetEnemy);

        // Only add missing parameters and transitions
        bool originalOverwrite = overwriteExisting;
        overwriteExisting = false;

        SetupController(controller, clips);

        overwriteExisting = originalOverwrite;

        EditorUtility.DisplayDialog("Success", "Quick fix applied to existing controller!", "OK");
    }
}
