using UnityEngine;
using UnityEditor;

/// <summary>
/// DEBUG TOOL - Kiểm tra tại sao Animator không hoạt động
/// </summary>
public class GolemBossDebugTool : EditorWindow
{
    private GameObject selectedBoss;

    [MenuItem("Tools/Golem Boss Debug 🔍")]
    public static void ShowWindow()
    {
        GetWindow<GolemBossDebugTool>("🔍 Golem Debug");
    }

    private void OnGUI()
    {
        GUILayout.Label("=== GOLEM BOSS DEBUG TOOL ===", EditorStyles.boldLabel);
        GUILayout.Space(10);

        selectedBoss = (GameObject)EditorGUILayout.ObjectField(
            "Boss GameObject:",
            selectedBoss,
            typeof(GameObject),
            true
        );

        GUILayout.Space(10);

        if (GUILayout.Button("🔍 DIAGNOSE ANIMATOR ISSUES", GUILayout.Height(40)))
        {
            DiagnoseAnimator();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🔧 FIX ANIMATOR REFERENCES", GUILayout.Height(40)))
        {
            FixAnimatorReferences();
        }
    }

    private void DiagnoseAnimator()
    {
        if (selectedBoss == null)
        {
            Debug.LogError("❌ Please select a Boss GameObject!");
            return;
        }

        Debug.Log("═══════════════════════════════════════");
        Debug.Log($"🔍 DIAGNOSING: {selectedBoss.name}");
        Debug.Log("═══════════════════════════════════════");

        // Check components
        var bossAI = selectedBoss.GetComponent<GolemBossAI>();
        var bossAnimator = selectedBoss.GetComponent<GolemBossAnimator>();
        var animator = selectedBoss.GetComponent<Animator>();

        Debug.Log("\n📦 COMPONENTS CHECK:");
        Debug.Log($"   GolemBossAI: {(bossAI != null ? "✅ FOUND" : "❌ MISSING")}");
        Debug.Log($"   GolemBossAnimator: {(bossAnimator != null ? "✅ FOUND" : "❌ MISSING")}");
        Debug.Log($"   Unity Animator: {(animator != null ? "✅ FOUND" : "❌ MISSING")}");

        if (bossAI == null || bossAnimator == null || animator == null)
        {
            Debug.LogError("❌ CRITICAL: Missing required components!");
            return;
        }

        // Check references
        Debug.Log("\n🔗 REFERENCES CHECK:");
        Debug.Log($"   bossAI.bossAnimator → {(bossAI.bossAnimator != null ? $"✅ {bossAI.bossAnimator.name}" : "❌ NULL")}");
        Debug.Log($"   bossAnimator.animator → {(bossAnimator.animator != null ? $"✅ {bossAnimator.animator.name}" : "❌ NULL")}");

        // Check Animator Controller
        if (animator.runtimeAnimatorController != null)
        {
            Debug.Log($"   Animator Controller: ✅ {animator.runtimeAnimatorController.name}");
            
            // Check parameters
            var parameters = animator.parameters;
            Debug.Log($"\n🎛️ ANIMATOR PARAMETERS ({parameters.Length}):");
            foreach (var param in parameters)
            {
                Debug.Log($"      - {param.name} ({param.type})");
            }
        }
        else
        {
            Debug.LogError("   ❌ Animator Controller is NULL!");
        }

        // Check Behavior Tree
        Debug.Log("\n🌳 BEHAVIOR TREE CHECK:");
        if (Application.isPlaying)
        {
            Debug.Log("   ✅ Game is running - Behavior Tree should be active");
        }
        else
        {
            Debug.LogWarning("   ⚠️ Game is NOT running - Enter Play Mode to test");
        }

        // Summary
        Debug.Log("\n═══════════════════════════════════════");
        bool allGood = 
            bossAI != null && 
            bossAnimator != null && 
            animator != null &&
            bossAI.bossAnimator != null &&
            bossAnimator.animator != null &&
            animator.runtimeAnimatorController != null;

        if (allGood)
        {
            Debug.Log("✅ ALL CHECKS PASSED!");
            Debug.Log("💡 If animations still don't work:");
            Debug.Log("   1. Enter Play Mode");
            Debug.Log("   2. Open Animator window (Window → Animation → Animator)");
            Debug.Log("   3. Watch parameters change during gameplay");
        }
        else
        {
            Debug.LogError("❌ FOUND ISSUES - Use 'FIX ANIMATOR REFERENCES' button");
        }
        Debug.Log("═══════════════════════════════════════\n");
    }

    private void FixAnimatorReferences()
    {
        if (selectedBoss == null)
        {
            Debug.LogError("❌ Please select a Boss GameObject!");
            return;
        }

        Debug.Log("🔧 FIXING ANIMATOR REFERENCES...\n");

        var bossAI = selectedBoss.GetComponent<GolemBossAI>();
        var bossAnimator = selectedBoss.GetComponent<GolemBossAnimator>();
        var animator = selectedBoss.GetComponent<Animator>();

        if (bossAI == null || bossAnimator == null || animator == null)
        {
            Debug.LogError("❌ Missing components! Run Auto Setup first!");
            return;
        }

        // Fix references using SerializedObject
        SerializedObject soAI = new SerializedObject(bossAI);
        SerializedProperty propBossAnimator = soAI.FindProperty("bossAnimator");
        if (propBossAnimator != null)
        {
            propBossAnimator.objectReferenceValue = bossAnimator;
            soAI.ApplyModifiedProperties();
            Debug.Log("   ✅ Fixed: bossAI.bossAnimator");
        }

        SerializedObject soAnimator = new SerializedObject(bossAnimator);
        SerializedProperty propAnimator = soAnimator.FindProperty("animator");
        if (propAnimator != null)
        {
            propAnimator.objectReferenceValue = animator;
            soAnimator.ApplyModifiedProperties();
            Debug.Log("   ✅ Fixed: bossAnimator.animator");
        }

        // Enable debug logs
        SerializedProperty propDebug = soAnimator.FindProperty("showDebugLogs");
        if (propDebug != null)
        {
            propDebug.boolValue = true;
            soAnimator.ApplyModifiedProperties();
            Debug.Log("   ✅ Enabled: showDebugLogs");
        }

        EditorUtility.SetDirty(selectedBoss);
        Debug.Log("\n✅ ALL REFERENCES FIXED!");
        Debug.Log("💡 Now enter Play Mode and watch Console for animation logs\n");
    }
}
