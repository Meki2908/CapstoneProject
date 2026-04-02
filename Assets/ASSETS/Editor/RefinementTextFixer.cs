using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor tool: "Tools → Fix Refinement Text to English"
/// Updates existing prefab text from Vietnamese to English without recreating anything.
/// </summary>
public class RefinementTextFixer : Editor
{
    [MenuItem("Tools/Fix Refinement Text to English")]
    public static void Fix()
    {
        Transform root = null;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            root = prefabStage.prefabContentsRoot.transform;
        if (root == null)
        {
            var bsUI = Object.FindFirstObjectByType<BlacksmithUI>(FindObjectsInactive.Include);
            if (bsUI != null) root = bsUI.transform;
        }
        if (root == null)
        {
            EditorUtility.DisplayDialog("Error", "Open Canvas_Blacksmith prefab first!", "OK");
            return;
        }

        int count = 0;

        // Tab button
        count += FixText(root, "RefineTabBtn", "REFINE");

        // Equipment name default
        count += FixText(root, "RefineEquipName", "Select equipment to refine");

        // Success rate
        count += FixText(root, "RefineSuccessText", "Success Rate: 0%");

        // Refine button
        count += FixText(root, "RefineBtn", "REFINE");

        // Fusion
        count += FixText(root, "FusionTitle", "FUSION");
        count += FixText(root, "FusionInfoText", "4x >> 1x");
        count += FixText(root, "FusionBtn", "FUSE");

        // Material slot text (inside RefineMaterial)
        Transform matSlot = FindChild(root, "RefineMaterial");
        if (matSlot != null)
        {
            var texts = matSlot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.text.Contains("Ch") || t.text.Contains("Da") || t.text.Contains("Tinh"))
                {
                    t.text = "Select Refinement Stone";
                    EditorUtility.SetDirty(t);
                    count++;
                }
            }
        }

        // Sidebar buttons — also fix if they have Vietnamese
        count += FixText(root, "WeaponTabBtn", null);   // don't change weapon tab
        count += FixText(root, "EquipTabBtn", null);     // don't change equip tab

        EditorUtility.DisplayDialog("Done!",
            $"Fixed {count} text elements to English.\n\nApply Prefab Overrides to save.", "OK");
    }

    static int FixText(Transform root, string name, string newText)
    {
        if (newText == null) return 0;
        Transform t = FindChild(root, name);
        if (t == null) return 0;

        var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = newText;
            EditorUtility.SetDirty(tmp);
            Debug.Log($"[TextFixer] {name}: '{tmp.text}' -> '{newText}'");
            return 1;
        }
        return 0;
    }

    static Transform FindChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
