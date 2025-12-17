using UnityEngine;
using UnityEditor;

/// <summary>
/// Debug tool để kiểm tra tại sao enemy không attack
/// </summary>
public class EnemyAttackDebugger : EditorWindow
{
    private GameObject selectedEnemy;

    [MenuItem("Tools/Debug Enemy Attack Issue 🔍")]
    public static void ShowWindow()
    {
        EnemyAttackDebugger window = GetWindow<EnemyAttackDebugger>("Attack Debug");
        window.minSize = new Vector2(450, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Enemy Attack Debugger", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "ENEMY KHÔNG TẤN CÔNG - COMMON ISSUES:\n\n" +
            "1. Player không có Layer 'Player'\n" +
            "2. Attack Range quá nhỏ\n" +
            "3. Animator thiếu parameter 'Attack'\n" +
            "4. Attack Cooldown quá dài\n" +
            "5. Target Layer không đúng\n" +
            "6. Collider thiếu trên Player",
            MessageType.Warning
        );

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Select Enemy to Debug:", EditorStyles.boldLabel);
        selectedEnemy = (GameObject)EditorGUILayout.ObjectField("Enemy", selectedEnemy, typeof(GameObject), true);
        GUILayout.Space(5);

        if (GUILayout.Button("🔍 Run Full Diagnostic", GUILayout.Height(40)))
        {
            RunDiagnostic();
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Quick Fixes:", EditorStyles.boldLabel);

        if (GUILayout.Button("✅ Setup Player Layer", GUILayout.Height(30)))
        {
            SetupPlayerLayer();
        }

        if (GUILayout.Button("✅ Increase Attack Range (x2)", GUILayout.Height(30)))
        {
            IncreaseAttackRange();
        }

        if (GUILayout.Button("✅ Add Attack Parameter to Animator", GUILayout.Height(30)))
        {
            AddAttackParameter();
        }

        if (GUILayout.Button("✅ Reduce Attack Cooldown", GUILayout.Height(30)))
        {
            ReduceAttackCooldown();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        if (GUILayout.Button("🔧 AUTO FIX ALL ISSUES", GUILayout.Height(50)))
        {
            AutoFixAll();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "MANUAL CHECKS:\n\n" +
            "1. Play mode → Select Enemy\n" +
            "2. Inspector → EnemyBT component\n" +
            "3. Check:\n" +
            "   • Detection Range: 10-15m\n" +
            "   • Attack Range: 2-3m\n" +
            "   • Target Layer: Player\n" +
            "   • Attack Cooldown: 1-2s\n\n" +
            "4. Animator → Parameters\n" +
            "   • Must have: 'Attack' (Trigger)\n\n" +
            "5. Player GameObject\n" +
            "   • Layer: Player\n" +
            "   • Collider: Active",
            MessageType.Info
        );
    }

    private void RunDiagnostic()
    {
        if (selectedEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy first!", "OK");
            return;
        }

        Debug.Log("🔍 Running diagnostic for: " + selectedEnemy.name);

        string report = "=== ENEMY ATTACK DIAGNOSTIC ===\n\n";
        int issueCount = 0;

        // Check 1: EnemyBT component
        EnemyBT bt = selectedEnemy.GetComponent<EnemyBT>();
        if (bt == null)
        {
            report += "❌ CRITICAL: No EnemyBT component!\n";
            issueCount++;
        }
        else
        {
            report += "✅ EnemyBT component found\n";

            // Check ranges
            if (bt.detectionRange < 5f)
            {
                report += $"⚠️ Detection Range too small: {bt.detectionRange}m (recommend 10-15m)\n";
                issueCount++;
            }
            else
            {
                report += $"✅ Detection Range: {bt.detectionRange}m\n";
            }

            if (bt.attackRange < 1.5f)
            {
                report += $"⚠️ Attack Range too small: {bt.attackRange}m (recommend 2-3m)\n";
                issueCount++;
            }
            else
            {
                report += $"✅ Attack Range: {bt.attackRange}m\n";
            }

            if (bt.attackCooldown > 3f)
            {
                report += $"⚠️ Attack Cooldown too long: {bt.attackCooldown}s (recommend 1-2s)\n";
                issueCount++;
            }
            else
            {
                report += $"✅ Attack Cooldown: {bt.attackCooldown}s\n";
            }

            // Check target layer
            if (bt.targetLayer == 0)
            {
                report += "❌ Target Layer not set!\n";
                issueCount++;
            }
            else
            {
                report += $"✅ Target Layer: {LayerMask.LayerToName((int)Mathf.Log(bt.targetLayer.value, 2))}\n";
            }
        }

        // Check 2: Animator
        Animator animator = selectedEnemy.GetComponent<Animator>();
        if (animator == null)
        {
            report += "❌ No Animator component!\n";
            issueCount++;
        }
        else
        {
            report += "✅ Animator component found\n";

            if (animator.runtimeAnimatorController == null)
            {
                report += "❌ No Animator Controller assigned!\n";
                issueCount++;
            }
            else
            {
                report += "✅ Animator Controller assigned\n";

                // Check for Attack parameter
                bool hasAttack = false;
                foreach (var param in animator.parameters)
                {
                    if (param.name == "Attack")
                    {
                        hasAttack = true;
                        break;
                    }
                }

                if (!hasAttack)
                {
                    report += "❌ Animator missing 'Attack' parameter!\n";
                    issueCount++;
                }
                else
                {
                    report += "✅ Animator has 'Attack' parameter\n";
                }
            }
        }

        // Check 3: Find Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            report += "\n⚠️ WARNING: No Player found with tag 'Player'\n";
            report += "   Create a player or add 'Player' tag\n";
        }
        else
        {
            report += "\n✅ Player found: " + player.name + "\n";

            // Check player layer
            if (player.layer != LayerMask.NameToLayer("Player"))
            {
                report += $"⚠️ Player layer is '{LayerMask.LayerToName(player.layer)}', should be 'Player'\n";
                issueCount++;
            }
            else
            {
                report += "✅ Player has correct layer\n";
            }

            // Check collider
            Collider col = player.GetComponent<Collider>();
            if (col == null)
            {
                report += "❌ Player has no Collider!\n";
                issueCount++;
            }
            else
            {
                report += "✅ Player has Collider\n";
            }

            // Check distance
            if (bt != null && player != null)
            {
                float distance = Vector3.Distance(selectedEnemy.transform.position, player.transform.position);
                report += $"\n📏 Current distance to player: {distance:F2}m\n";

                if (distance > bt.detectionRange)
                {
                    report += $"⚠️ Player too far! (Detection: {bt.detectionRange}m)\n";
                }
                else if (distance > bt.attackRange)
                {
                    report += $"✅ In detection range, but not in attack range (Attack: {bt.attackRange}m)\n";
                }
                else
                {
                    report += $"✅ In attack range!\n";
                }
            }
        }

        report += "\n" + new string('=', 40) + "\n";
        report += $"Total Issues Found: {issueCount}\n";

        if (issueCount == 0)
        {
            report += "\n✅ NO ISSUES FOUND!\n";
            report += "If enemy still not attacking, check:\n";
            report += "- Is enemy script running? (Play mode)\n";
            report += "- Check Console for errors\n";
            report += "- Use BehaviorTreeDebugger for real-time debug\n";
        }
        else
        {
            report += "\n⚠️ FOUND ISSUES! Click 'AUTO FIX ALL' to fix automatically.\n";
        }

        Debug.Log(report);

        EditorUtility.DisplayDialog(
            issueCount == 0 ? "Diagnostic Complete ✅" : "Issues Found ⚠️",
            report,
            "OK"
        );
    }

    private void SetupPlayerLayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.layer = LayerMask.NameToLayer("Player");
            Debug.Log("✅ Set player layer to 'Player'");
            EditorUtility.DisplayDialog("Success", "Player layer set to 'Player'!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "No player found with tag 'Player'!", "OK");
        }
    }

    private void IncreaseAttackRange()
    {
        if (selectedEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy first!", "OK");
            return;
        }

        EnemyBT bt = selectedEnemy.GetComponent<EnemyBT>();
        if (bt != null)
        {
            float oldRange = bt.attackRange;
            bt.attackRange *= 2f;
            EditorUtility.SetDirty(selectedEnemy);
            Debug.Log($"✅ Attack Range: {oldRange}m → {bt.attackRange}m");
            EditorUtility.DisplayDialog("Success", $"Attack Range increased from {oldRange}m to {bt.attackRange}m!", "OK");
        }
    }

    private void AddAttackParameter()
    {
        if (selectedEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy first!", "OK");
            return;
        }

        Animator animator = selectedEnemy.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // This needs to be done through AnimatorController, redirect to other tool
            EditorUtility.DisplayDialog(
                "Use Animator Tool",
                "Please use:\nTools → Fix Catfish Animator (Complete)\n\nto fix animator parameters!",
                "OK"
            );
        }
    }

    private void ReduceAttackCooldown()
    {
        if (selectedEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy first!", "OK");
            return;
        }

        EnemyBT bt = selectedEnemy.GetComponent<EnemyBT>();
        if (bt != null)
        {
            float oldCooldown = bt.attackCooldown;
            bt.attackCooldown = 1.5f;
            EditorUtility.SetDirty(selectedEnemy);
            Debug.Log($"✅ Attack Cooldown: {oldCooldown}s → {bt.attackCooldown}s");
            EditorUtility.DisplayDialog("Success", $"Attack Cooldown reduced from {oldCooldown}s to {bt.attackCooldown}s!", "OK");
        }
    }

    private void AutoFixAll()
    {
        if (selectedEnemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy first!", "OK");
            return;
        }

        int fixCount = 0;
        string report = "Auto Fix Report:\n\n";

        // Fix 1: EnemyBT ranges
        EnemyBT bt = selectedEnemy.GetComponent<EnemyBT>();
        if (bt != null)
        {
            if (bt.detectionRange < 10f)
            {
                bt.detectionRange = 12f;
                report += "✅ Set Detection Range to 12m\n";
                fixCount++;
            }

            if (bt.attackRange < 2f)
            {
                bt.attackRange = 2.5f;
                report += "✅ Set Attack Range to 2.5m\n";
                fixCount++;
            }

            if (bt.attackCooldown > 2f)
            {
                bt.attackCooldown = 1.5f;
                report += "✅ Set Attack Cooldown to 1.5s\n";
                fixCount++;
            }

            if (bt.targetLayer == 0)
            {
                bt.targetLayer = LayerMask.GetMask("Player");
                report += "✅ Set Target Layer to 'Player'\n";
                fixCount++;
            }

            EditorUtility.SetDirty(selectedEnemy);
        }

        // Fix 2: Player layer
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.layer != LayerMask.NameToLayer("Player"))
        {
            player.layer = LayerMask.NameToLayer("Player");
            report += "✅ Set Player layer to 'Player'\n";
            fixCount++;
        }

        report += $"\nTotal fixes applied: {fixCount}";

        Debug.Log(report);

        EditorUtility.DisplayDialog(
            "Auto Fix Complete!",
            report + "\n\nNow test in Play mode!",
            "Awesome!"
        );
    }
}
