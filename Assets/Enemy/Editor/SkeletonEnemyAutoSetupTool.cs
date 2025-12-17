using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Editor tool to fully configure the Skeleton enemy:
/// - Auto builds Animator Controller with 1D blend tree (Idle/Walk/Run)
/// - Adds Attack / Hit / Die states + transitions + parameters
/// - Configures EnemyBT AI, CharacterController, layer, patrol points
/// </summary>
public class SkeletonEnemyAutoSetupTool : EditorWindow
{
    private class ClipMapping
    {
        public string key;
        public string label;
        public string[] keywords;
        public bool required;
        public bool loop;

        public ClipMapping(string key, string label, string[] keywords, bool required = false, bool loop = true)
        {
            this.key = key;
            this.label = label;
            this.keywords = keywords;
            this.required = required;
            this.loop = loop;
        }
    }

    private static readonly ClipMapping[] CLIP_MAPPINGS = new[]
    {
        new ClipMapping("idle", "Idle", new[] { "idle", "Idle" }, true, true),
        new ClipMapping("walk", "Walk", new[] { "walk", "Walk" }, true, true),
        new ClipMapping("run", "Run", new[] { "run", "Run" }, true, true),
        new ClipMapping("attack", "Attack", new[] { "attack", "Attack01", "Slash" }, true, false),
        new ClipMapping("attackAlt", "Attack (Alt)", new[] { "attack02", "attack2", "LeftAttack", "RightAttack" }, false, false),
        new ClipMapping("hit", "Hit/Damage", new[] { "hit", "hurt", "damage" }, false, false),
        new ClipMapping("die", "Death", new[] { "die", "death" }, true, false),
        new ClipMapping("battleIdle", "Battle Idle", new[] { "battle", "combatidle" }, false, true)
    };

    private GameObject targetSkeleton;
    private readonly Dictionary<string, AnimationClip> clipSelections = new Dictionary<string, AnimationClip>();
    private Vector2 scrollPosition;

    // Animator options
    private string controllerName = "Skeleton_AutoAnimator";
    private bool overwriteController = true;
    private float walkThreshold = 0.35f;
    private float runThreshold = 3.0f;
    private float attackExitTime = 0.84f;
    private float hitExitTime = 0.6f;

    // AI / CharacterController defaults
    private float ccRadius = 0.35f;
    private float ccHeight = 1.8f;
    private Vector3 ccCenter = new Vector3(0f, 0.9f, 0f);

    private float detectionRange = 18f;
    private float attackRange = 2.2f;
    private float moveSpeed = 2.4f;
    private float chaseSpeed = 4.5f;
    private float attackCooldown = 1.35f;
    private float attackAnimationDuration = 1.5f;
    private float patrolRadius = 12f;
    private bool createPatrolPoints = true;
    private int patrolPointCount = 4;

    [MenuItem("Tools/Enemy Setup/Skeleton Auto Setup")]
    public static void ShowWindow()
    {
        SkeletonEnemyAutoSetupTool window = GetWindow<SkeletonEnemyAutoSetupTool>("Skeleton Auto Setup");
        window.minSize = new Vector2(520f, 720f);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("🦴 Skeleton Enemy - Animator + AI Auto Config", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Select Skeleton prefab or instance\n" +
            "2. Scan & assign animation clips\n" +
            "3. Build animator (blend tree + logic)\n" +
            "4. Configure AI (EnemyBT + CharacterController)\n\n" +
            "Use the FULL AUTO button to do all steps in one click.",
            MessageType.Info);

        EditorGUILayout.Space(6f);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Target Skeleton", EditorStyles.boldLabel);
        targetSkeleton = (GameObject)EditorGUILayout.ObjectField("GameObject / Prefab", targetSkeleton, typeof(GameObject), true);
        if (targetSkeleton == null)
        {
            EditorGUILayout.HelpBox("Select a Skeleton prefab in Project or an instance in the Scene.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6f);

        DrawAnimationSection();
        EditorGUILayout.Space(6f);
        DrawAnimatorOptions();
        EditorGUILayout.Space(6f);
        DrawAISection();

        EditorGUILayout.Space(10f);

        EditorGUI.BeginDisabledGroup(targetSkeleton == null);
        if (GUILayout.Button("🔍 Scan Skeleton Animations", GUILayout.Height(36f)))
        {
            ScanAnimations();
        }

        if (GUILayout.Button("🎬 Build Animator + Blend Tree", GUILayout.Height(40f)))
        {
            BuildAnimator();
        }

        if (GUILayout.Button("🤖 Configure AI + Components", GUILayout.Height(40f)))
        {
            ConfigureAI();
        }

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("🚀 FULL AUTO (Animator + AI)", GUILayout.Height(46f)))
        {
            FullAuto();
        }
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(12f);

        EditorGUILayout.HelpBox(
            "Animator output:\n" +
            "• Parameters: Speed, Attack, Die, IsHurt\n" +
            "• Locomotion 1D blend tree (Idle/Walk/Run)\n" +
            "• States: Attack, Hit (optional), Die\n\n" +
            "AI output:\n" +
            "• Adds EnemyBT + CharacterController\n" +
            "• Sets detection/attack ranges & speeds\n" +
            "• Auto layer = Enemy, target layer = Player\n" +
            "• Patrol points generated (optional)",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void DrawAnimationSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Animation Clips", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Auto scan looks inside the Skeleton folder + Kevin Iglesias Skeleton Animations package. You can override any slot manually.", MessageType.None);

        foreach (ClipMapping mapping in CLIP_MAPPINGS)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(mapping.label + (mapping.required ? " *" : ""), GUILayout.Width(140f));
            AnimationClip current = clipSelections.ContainsKey(mapping.key) ? clipSelections[mapping.key] : null;
            AnimationClip newClip = (AnimationClip)EditorGUILayout.ObjectField(current, typeof(AnimationClip), false);
            clipSelections[mapping.key] = newClip;
            if (newClip == null && mapping.required)
            {
                EditorGUILayout.LabelField("Missing", EditorStyles.miniBoldLabel, GUILayout.Width(60f));
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAnimatorOptions()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Animator / Blend Tree Options", EditorStyles.boldLabel);
        controllerName = EditorGUILayout.TextField("Controller File Name", controllerName);
        overwriteController = EditorGUILayout.Toggle("Overwrite Controller File", overwriteController);
        walkThreshold = EditorGUILayout.FloatField("Walk Threshold (Speed)", walkThreshold);
        runThreshold = EditorGUILayout.FloatField("Run Threshold (Speed)", runThreshold);
        attackExitTime = EditorGUILayout.Slider("Attack Exit Time", attackExitTime, 0.5f, 1.2f);
        hitExitTime = EditorGUILayout.Slider("Hit Exit Time", hitExitTime, 0.4f, 1.0f);
        EditorGUILayout.EndVertical();
    }

    private void DrawAISection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("AI + CharacterController", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("CharacterController", EditorStyles.miniBoldLabel);
        ccRadius = EditorGUILayout.FloatField("Radius", ccRadius);
        ccHeight = EditorGUILayout.FloatField("Height", ccHeight);
        ccCenter = EditorGUILayout.Vector3Field("Center", ccCenter);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("EnemyBT Settings", EditorStyles.miniBoldLabel);
        detectionRange = EditorGUILayout.FloatField("Detection Range", detectionRange);
        attackRange = EditorGUILayout.FloatField("Attack Range", attackRange);
        moveSpeed = EditorGUILayout.FloatField("Patrol Speed", moveSpeed);
        chaseSpeed = EditorGUILayout.FloatField("Chase Speed", chaseSpeed);
        attackCooldown = EditorGUILayout.FloatField("Attack Cooldown", attackCooldown);
        attackAnimationDuration = EditorGUILayout.FloatField("Attack Animation Duration", attackAnimationDuration);
        patrolRadius = EditorGUILayout.FloatField("Patrol Radius", patrolRadius);
        createPatrolPoints = EditorGUILayout.Toggle("Regenerate Patrol Points", createPatrolPoints);
        if (createPatrolPoints)
        {
            patrolPointCount = EditorGUILayout.IntSlider("Patrol Point Count", patrolPointCount, 2, 8);
        }
        EditorGUILayout.EndVertical();
    }

    private void ScanAnimations()
    {
        if (targetSkeleton == null)
        {
            EditorUtility.DisplayDialog("Select Skeleton", "Please select a Skeleton prefab or instance first.", "OK");
            return;
        }

        string[] searchFolders = GetAnimationSearchFolders();
        int foundCount = 0;

        foreach (string folder in searchFolders)
        {
            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
            foreach (string guid in clipGuids)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                    continue;

                if (TryMatchClip(clip))
                {
                    foundCount++;
                }
            }
        }

        EditorUtility.DisplayDialog("Scan Complete", $"Matched {foundCount} animation clips.", "Great!");
    }

    private string[] GetAnimationSearchFolders()
    {
        HashSet<string> folders = new HashSet<string>();

        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetSkeleton);
        if (string.IsNullOrEmpty(prefabPath))
        {
            prefabPath = AssetDatabase.GetAssetPath(targetSkeleton);
        }

        if (!string.IsNullOrEmpty(prefabPath))
        {
            string directory = Path.GetDirectoryName(prefabPath)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(directory))
            {
                folders.Add(directory);
            }
        }

        folders.Add("Assets/Enemy/_Enemies-SaMac/Skeleton");
        folders.Add("Assets/Enemy/Kevin Iglesias/Skeleton Animations");

        return folders.ToArray();
    }

    private bool TryMatchClip(AnimationClip clip)
    {
        string nameLower = clip.name.ToLower();
        foreach (ClipMapping mapping in CLIP_MAPPINGS)
        {
            if (clipSelections.ContainsKey(mapping.key) && clipSelections[mapping.key] != null)
                continue;

            if (mapping.keywords.Any(keyword => nameLower.Contains(keyword.ToLower())))
            {
                clipSelections[mapping.key] = clip;
                return true;
            }
        }

        return false;
    }

    private void BuildAnimator()
    {
        RunOnEditableTarget("build the animator", (workingTarget, _) => BuildAnimatorInternal(workingTarget));
    }

    private bool ValidateRequiredClips()
    {
        List<string> missing = new List<string>();
        foreach (ClipMapping mapping in CLIP_MAPPINGS.Where(m => m.required))
        {
            if (!clipSelections.ContainsKey(mapping.key) || clipSelections[mapping.key] == null)
            {
                missing.Add(mapping.label);
            }
        }

        if (missing.Count > 0)
        {
            string list = string.Join(", ", missing);
            EditorUtility.DisplayDialog("Missing Clips", $"Required animation clips missing: {list}", "OK");
            return false;
        }

        return true;
    }

    private AnimatorController CreateOrLoadController()
    {
        string folder = GetControllerFolder();
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string parent = Path.GetDirectoryName(folder);
            string child = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parent.Replace("\\", "/"), child);
            }
        }

        string controllerPath = Path.Combine(folder, $"{controllerName}.controller").Replace("\\", "/");

        if (overwriteController && AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controllerAsset = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controllerAsset == null)
        {
            controllerAsset = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        }

        return controllerAsset;
    }

    private string GetControllerFolder()
    {
        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetSkeleton);
        if (string.IsNullOrEmpty(prefabPath))
        {
            prefabPath = AssetDatabase.GetAssetPath(targetSkeleton);
        }

        string directory = string.IsNullOrEmpty(prefabPath)
            ? "Assets/Enemy/_Enemies-SaMac/Skeleton"
            : Path.GetDirectoryName(prefabPath).Replace("\\", "/");

        string controllerFolder = Path.Combine(directory, "Controller").Replace("\\", "/");
        if (!AssetDatabase.IsValidFolder(controllerFolder))
        {
            string parent = Path.GetDirectoryName(controllerFolder)?.Replace("\\", "/");
            string child = Path.GetFileName(controllerFolder);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(child) && !AssetDatabase.IsValidFolder(controllerFolder))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        return controllerFolder;
    }

    private void SetupController(AnimatorController controller)
    {
        if (controller == null)
            return;

        if (controller.layers.Length == 0)
        {
            AnimatorControllerLayer baseLayer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine()
            };
            controller.AddLayer(baseLayer);
        }

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine ?? new AnimatorStateMachine();
        layer.stateMachine = stateMachine;
        controller.layers[0] = layer;

        // Clear old states/transitions to keep things deterministic
        foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
        {
            stateMachine.RemoveState(childState.state);
        }
        foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions.ToArray())
        {
            stateMachine.RemoveAnyStateTransition(transition);
        }

        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Die", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsHurt", AnimatorControllerParameterType.Bool);

        AnimatorState locomotionState = stateMachine.AddState("Locomotion");
        locomotionState.motion = BuildBlendTree(controller);
        locomotionState.writeDefaultValues = true;
        stateMachine.defaultState = locomotionState;

        AnimatorState attackState = null;
        if (clipSelections.TryGetValue("attack", out AnimationClip attackClip) && attackClip != null)
        {
            attackState = stateMachine.AddState("Attack");
            attackState.motion = attackClip;
            AnimatorStateTransition attackExit = attackState.AddTransition(locomotionState);
            attackExit.hasExitTime = true;
            attackExit.exitTime = attackExitTime;
            attackExit.duration = 0.1f;

            AnimatorStateTransition anyToAttack = stateMachine.AddAnyStateTransition(attackState);
            anyToAttack.hasExitTime = false;
            anyToAttack.duration = 0.1f;
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
        }

        if (clipSelections.TryGetValue("attackAlt", out AnimationClip attackAlt) && attackAlt != null && attackState != null)
        {
            // Keep alternate clip as sub-state machine by using blend tree inside Attack state
            BlendTree attackBlend = new BlendTree
            {
                name = "AttackBlend",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "AttackIndex",
                useAutomaticThresholds = false
            };
            EnsureParameter(controller, "AttackIndex", AnimatorControllerParameterType.Float);
            AssetDatabase.AddObjectToAsset(attackBlend, controller);
            attackBlend.AddChild(clipSelections["attack"], 0f);
            attackBlend.AddChild(attackAlt, 1f);
            attackState.motion = attackBlend;
        }

        if (clipSelections.TryGetValue("hit", out AnimationClip hitClip) && hitClip != null)
        {
            AnimatorState hitState = stateMachine.AddState("Hit");
            hitState.motion = hitClip;

            AnimatorStateTransition anyToHit = stateMachine.AddAnyStateTransition(hitState);
            anyToHit.hasExitTime = false;
            anyToHit.duration = 0.05f;
            anyToHit.AddCondition(AnimatorConditionMode.If, 0f, "IsHurt");

            AnimatorStateTransition hitExit = hitState.AddTransition(locomotionState);
            hitExit.hasExitTime = true;
            hitExit.exitTime = hitExitTime;
            hitExit.duration = 0.1f;
        }

        if (clipSelections.TryGetValue("die", out AnimationClip dieClip) && dieClip != null)
        {
            AnimatorState dieState = stateMachine.AddState("Die");
            dieState.motion = dieClip;
            AnimatorStateTransition anyToDie = stateMachine.AddAnyStateTransition(dieState);
            anyToDie.hasExitTime = false;
            anyToDie.duration = 0.1f;
            anyToDie.AddCondition(AnimatorConditionMode.If, 0f, "Die");
        }
    }

    private BlendTree BuildBlendTree(AnimatorController controller)
    {
        BlendTree blendTree = new BlendTree
        {
            name = "LocomotionBlend",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(blendTree, controller);

        AnimationClip idle = clipSelections.TryGetValue("idle", out AnimationClip clip) ? clip : null;
        AnimationClip walk = clipSelections.TryGetValue("walk", out AnimationClip clip2) ? clip2 : null;
        AnimationClip run = clipSelections.TryGetValue("run", out AnimationClip clip3) ? clip3 : null;

        if (idle != null)
        {
            blendTree.AddChild(idle, 0f);
        }
        if (walk != null)
        {
            blendTree.AddChild(walk, walkThreshold);
        }
        if (run != null)
        {
            blendTree.AddChild(run, runThreshold);
        }

        return blendTree;
    }

    private void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == name)
            {
                if (parameter.type != type)
                {
                    controller.RemoveParameter(parameter);
                    break;
                }
                return;
            }
        }

        controller.AddParameter(name, type);
    }

    private void ConfigureAI()
    {
        RunOnEditableTarget("configure AI components", (workingTarget, isPrefabAssetContext) => ConfigureAIInternal(workingTarget, isPrefabAssetContext));
    }

    private Transform[] GeneratePatrolPoints(Transform root, int count, float radius)
    {
        Transform container = root.Find("PatrolPoints");
        if (container == null)
        {
            GameObject go = new GameObject("PatrolPoints");
            go.transform.SetParent(root);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            container = go.transform;
        }
        else
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(container.GetChild(i).gameObject);
            }
        }

        List<Transform> points = new List<Transform>();
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            GameObject point = new GameObject($"PatrolPoint_{i + 1}");
            point.transform.SetParent(container);
            float angle = step * i * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            point.transform.localPosition = position;
            points.Add(point.transform);
#if UNITY_EDITOR
            Texture2D icon = EditorGUIUtility.IconContent("sv_icon_dot6_pix16_gizmo").image as Texture2D;
            if (icon != null)
            {
                EditorGUIUtility.SetIconForObject(point, icon);
            }
#endif
        }

        return points.ToArray();
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void FullAuto()
    {
        RunOnEditableTarget("run FULL AUTO", (workingTarget, isPrefabAssetContext) =>
        {
            bool animatorReady = BuildAnimatorInternal(workingTarget);
            if (!animatorReady)
            {
                return false;
            }

            return ConfigureAIInternal(workingTarget, isPrefabAssetContext);
        });
    }

    private bool BuildAnimatorInternal(GameObject workingTarget)
    {
        if (workingTarget == null)
            return false;

        if (!ValidateRequiredClips())
        {
            return false;
        }

        Animator animator = workingTarget.GetComponent<Animator>();
        if (animator == null)
        {
            animator = workingTarget.AddComponent<Animator>();
        }
        animator.applyRootMotion = false;

        AnimatorController controller = CreateOrLoadController();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "Unable to create animator controller.", "OK");
            return false;
        }

        SetupController(controller);
        animator.runtimeAnimatorController = controller;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(animator);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Animator Ready", "Skeleton animator + blend tree configured successfully!", "Nice!");
        return true;
    }

    private bool ConfigureAIInternal(GameObject workingTarget, bool isPrefabAssetContext)
    {
        if (workingTarget == null)
            return false;

        if (!isPrefabAssetContext)
        {
            Undo.RecordObject(workingTarget, "Configure Skeleton AI");
        }

        CharacterController cc = workingTarget.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = workingTarget.AddComponent<CharacterController>();
        }
        else if (!isPrefabAssetContext)
        {
            Undo.RecordObject(cc, "Configure Skeleton AI");
        }

        cc.radius = ccRadius;
        cc.height = ccHeight;
        cc.center = ccCenter;
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;

        EnemyBT bt = workingTarget.GetComponent<EnemyBT>();
        if (bt == null)
        {
            bt = workingTarget.AddComponent<EnemyBT>();
        }
        else if (!isPrefabAssetContext)
        {
            Undo.RecordObject(bt, "Configure Skeleton AI");
        }

        bt.detectionRange = detectionRange;
        bt.attackRange = attackRange;
        bt.moveSpeed = moveSpeed;
        bt.chaseSpeed = chaseSpeed;
        bt.attackCooldown = attackCooldown;
        bt.attackAnimationDuration = attackAnimationDuration;
        bt.patrolRadius = patrolRadius;
        bt.shouldPatrol = true;
        bt.characterController = cc;
        bt.targetLayer = LayerMask.GetMask("Player");

        if (createPatrolPoints)
        {
            bt.patrolPoints = GeneratePatrolPoints(workingTarget.transform, patrolPointCount, patrolRadius);
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer != -1)
        {
            SetLayerRecursively(workingTarget, enemyLayer);
        }
        else
        {
            Debug.LogWarning("Layer 'Enemy' not found. Please create it under Project Settings > Tags and Layers.");
        }

        EditorUtility.SetDirty(workingTarget);
        EditorUtility.SetDirty(cc);
        EditorUtility.SetDirty(bt);

        EditorUtility.DisplayDialog("AI Ready", "EnemyBT + CharacterController configured for Skeleton.", "Done");
        return true;
    }

    private void RunOnEditableTarget(string actionDescription, System.Func<GameObject, bool, bool> operation)
    {
        if (targetSkeleton == null)
            return;

        GameObject workingTarget = targetSkeleton;
        GameObject prefabContentsRoot = null;
        string prefabPath = null;
        bool editingPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(targetSkeleton);

        if (editingPrefabAsset)
        {
            prefabPath = AssetDatabase.GetAssetPath(targetSkeleton);
            if (string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog(
                    "Prefab Asset Unavailable",
                    $"Unity could not resolve the asset path for the selected prefab, so the tool cannot {actionDescription}.",
                    "OK");
                return;
            }

            try
            {
                prefabContentsRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(
                    "Prefab Load Failed",
                    $"Unable to {actionDescription} because the prefab asset could not be opened.\n\n{ex.Message}",
                    "OK");
                return;
            }

            if (prefabContentsRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Prefab Load Failed",
                    "Unity returned a null root when attempting to open the prefab asset.",
                    "OK");
                return;
            }

            workingTarget = prefabContentsRoot;
        }

        bool success = false;
        try
        {
            success = operation?.Invoke(workingTarget, editingPrefabAsset) ?? false;
            if (success && editingPrefabAsset)
            {
                PrefabUtility.SaveAsPrefabAsset(workingTarget, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        finally
        {
            if (prefabContentsRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
            }
        }
    }
}

