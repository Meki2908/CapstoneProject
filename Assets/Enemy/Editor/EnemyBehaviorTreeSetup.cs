using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool để tự động setup Behavior Tree cho Enemy
/// </summary>
public class EnemyBehaviorTreeSetup : EditorWindow
{
    private GameObject enemyPrefab;
    private bool setupAnimator = true;
    private bool createPatrolPoints = true;
    private int numberOfPatrolPoints = 4;
    private float patrolRadius = 10f;

    private Vector2 scrollPosition;

    [MenuItem("Tools/Enemy Behavior Tree Setup")]
    public static void ShowWindow()
    {
        EnemyBehaviorTreeSetup window = GetWindow<EnemyBehaviorTreeSetup>("BT Setup");
        window.minSize = new Vector2(400, 400);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Enemy Behavior Tree Auto Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Main Setup
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Enemy Configuration", EditorStyles.boldLabel);

        enemyPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Enemy Prefab/Object",
            enemyPrefab,
            typeof(GameObject),
            true
        );

        GUILayout.Space(5);

        setupAnimator = EditorGUILayout.Toggle("Setup Animator", setupAnimator);
        createPatrolPoints = EditorGUILayout.Toggle("Create Patrol Points", createPatrolPoints);

        if (createPatrolPoints)
        {
            EditorGUI.indentLevel++;
            numberOfPatrolPoints = EditorGUILayout.IntSlider("Number of Points", numberOfPatrolPoints, 2, 10);
            patrolRadius = EditorGUILayout.Slider("Patrol Radius", patrolRadius, 5f, 50f);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Setup Behavior Tree", GUILayout.Height(40)))
        {
            if (enemyPrefab != null)
            {
                SetupBehaviorTree();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please select an Enemy first!", "OK");
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Batch Setup
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Batch Operations", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup All Selected in Hierarchy", GUILayout.Height(30)))
        {
            SetupAllSelected();
        }

        if (GUILayout.Button("Setup All Enemies in Scene", GUILayout.Height(30)))
        {
            SetupAllInScene();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Templates
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Quick Templates", EditorStyles.boldLabel);

        if (GUILayout.Button("🎯 Aggressive Enemy (Fast Attack)", GUILayout.Height(30)))
        {
            ApplyTemplate(EnemyTemplate.Aggressive);
        }

        if (GUILayout.Button("🛡️ Defensive Enemy (Patrol Focus)", GUILayout.Height(30)))
        {
            ApplyTemplate(EnemyTemplate.Defensive);
        }

        if (GUILayout.Button("⚡ Boss Enemy (Large Range)", GUILayout.Height(30)))
        {
            ApplyTemplate(EnemyTemplate.Boss);
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Help Box
        EditorGUILayout.HelpBox(
            "🎮 BEHAVIOR TREE AUTO SETUP\n\n" +
            "Công cụ này sẽ:\n" +
            "✓ Thêm EnemyBT component (AI Controller)\n" +
            "✓ Setup Animator nếu cần\n" +
            "✓ Tạo Patrol Points tự động\n" +
            "✓ Cấu hình detection & attack ranges\n\n" +
            "Behavior Tree bao gồm:\n" +
            "• Death Check → Combat → Patrol\n" +
            "• Auto detect & chase player\n" +
            "• Smart attack trong range\n" +
            "• Patrol khi không có target\n\n" +
            "📝 Note: Đảm bảo enemy có Animator và Layer được setup đúng!",
            MessageType.Info
        );

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
        if (enemyPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy first!", "OK");
            return;
        }

        EnemyBT bt = enemyPrefab.GetComponent<EnemyBT>();
        if (bt == null)
        {
            bt = enemyPrefab.AddComponent<EnemyBT>();
        }

        switch (template)
        {
            case EnemyTemplate.Aggressive:
                bt.detectionRange = 15f;
                bt.attackRange = 2.5f;
                bt.moveSpeed = 5f;
                bt.attackCooldown = 0.8f;
                bt.patrolRadius = 8f;
                Debug.Log("✓ Applied Aggressive template");
                break;

            case EnemyTemplate.Defensive:
                bt.detectionRange = 8f;
                bt.attackRange = 2f;
                bt.moveSpeed = 2.5f;
                bt.attackCooldown = 2f;
                bt.patrolRadius = 15f;
                Debug.Log("✓ Applied Defensive template");
                break;

            case EnemyTemplate.Boss:
                bt.detectionRange = 25f;
                bt.attackRange = 5f;
                bt.moveSpeed = 3.5f;
                bt.attackCooldown = 1.2f;
                bt.patrolRadius = 20f;
                Debug.Log("✓ Applied Boss template");
                break;
        }

        EditorUtility.SetDirty(enemyPrefab);
        EditorUtility.DisplayDialog("Success", $"Applied {template} template successfully!", "OK");
    }

    private void SetupBehaviorTree()
    {
        // Add EnemyBT component
        EnemyBT bt = enemyPrefab.GetComponent<EnemyBT>();
        if (bt == null)
        {
            bt = enemyPrefab.AddComponent<EnemyBT>();
            Debug.Log($"✓ Added EnemyBT component to {enemyPrefab.name}");
        }

        // Setup Animator
        if (setupAnimator)
        {
            Animator animator = enemyPrefab.GetComponent<Animator>();
            if (animator == null)
            {
                animator = enemyPrefab.AddComponent<Animator>();
                Debug.Log($"✓ Added Animator component");
            }
        }

        // Create Patrol Points
        if (createPatrolPoints)
        {
            CreatePatrolPointsForEnemy(enemyPrefab, bt);
        }

        // Set target layer (assuming player is on "Player" layer)
        bt.targetLayer = LayerMask.GetMask("Player");
        if (bt.targetLayer == 0)
        {
            Debug.LogWarning("⚠ 'Player' layer not found. Please create it manually.");
        }

        EditorUtility.SetDirty(enemyPrefab);

        EditorUtility.DisplayDialog(
            "Success",
            $"Behavior Tree setup completed for {enemyPrefab.name}!\n\n" +
            "Next steps:\n" +
            "1. Assign Animator Controller\n" +
            "2. Verify Target Layer\n" +
            "3. Adjust ranges as needed",
            "OK"
        );
    }

    private void CreatePatrolPointsForEnemy(GameObject enemy, EnemyBT bt)
    {
        // Tìm hoặc tạo parent object cho patrol points
        Transform patrolParent = enemy.transform.Find("PatrolPoints");
        if (patrolParent == null)
        {
            GameObject patrolObj = new GameObject("PatrolPoints");
            patrolObj.transform.SetParent(enemy.transform);
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
        List<Transform> points = new List<Transform>();
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

        Debug.Log($"✓ Created {numberOfPatrolPoints} patrol points");
    }

    private void SetupAllSelected()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select objects in Hierarchy!", "OK");
            return;
        }

        int count = 0;
        foreach (GameObject obj in selected)
        {
            enemyPrefab = obj;
            SetupBehaviorTree();
            count++;
        }

        EditorUtility.DisplayDialog("Batch Complete", $"Setup {count} enemies successfully!", "OK");
    }

    private void SetupAllInScene()
    {
        // Tìm tất cả objects có tag "Enemy"
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
        {
            EditorUtility.DisplayDialog("No Enemies", "No objects with 'Enemy' tag found in scene!", "OK");
            return;
        }

        int count = 0;
        foreach (GameObject enemy in enemies)
        {
            enemyPrefab = enemy;
            SetupBehaviorTree();
            count++;
        }

        EditorUtility.DisplayDialog("Batch Complete", $"Setup {count} enemies in scene!", "OK");
    }
}
