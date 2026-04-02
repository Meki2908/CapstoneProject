using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Editor tool: "Tools → Fix Refinement Text to English"
/// Updates existing Vietnamese text on prefab to English.
/// Safe to run multiple times — only changes matching Vietnamese text.
/// </summary>
public class RefinementTextFixer : Editor
{
    [MenuItem("Tools/Fix Refinement Text to English")]
    public static void FixText()
    {
        Transform root = null;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null)
            root = prefabStage.prefabContentsRoot.transform;

        if (root == null)
        {
            // Try scene
            var bsUI = Object.FindFirstObjectByType<BlacksmithUI>(FindObjectsInactive.Include);
            if (bsUI != null) root = bsUI.transform;
        }

        if (root == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Open Canvas_Blacksmith in Prefab Mode first!", "OK");
            return;
        }

        int count = 0;

        // Fix all TMP text components
        var allTMP = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in allTMP)
        {
            string original = tmp.text;
            string replaced = ReplaceVietnamese(original);
            if (replaced != original)
            {
                Undo.RecordObject(tmp, "Fix Text to English");
                tmp.text = replaced;
                EditorUtility.SetDirty(tmp);
                Debug.Log($"[TextFixer] '{tmp.gameObject.name}': \"{original}\" >> \"{replaced}\"");
                count++;
            }
        }

        // Also fix button text on sidebar tab
        var allButtons = root.GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnText != null)
            {
                string original = btnText.text;
                string replaced = ReplaceVietnamese(original);
                if (replaced != original)
                {
                    Undo.RecordObject(btnText, "Fix Button Text");
                    btnText.text = replaced;
                    EditorUtility.SetDirty(btnText);
                    Debug.Log($"[TextFixer] Button '{btn.gameObject.name}': \"{original}\" >> \"{replaced}\"");
                    count++;
                }
            }
        }

        EditorUtility.DisplayDialog("Done!",
            $"Fixed {count} text elements to English.\n\n" +
            "Apply Prefab Overrides to save.", "OK");
    }

    static string ReplaceVietnamese(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Tab buttons
        text = text.Replace("TINH LUYEN", "REFINE");
        text = text.Replace("TINH LUY\u1EC6N", "REFINE");

        // Default texts
        text = text.Replace("Ch\u1ECDn trang b\u1ECB \u0111\u1EC3 tinh luy\u1EC7n", "Select equipment to refine");
        text = text.Replace("Chon trang bi de tinh luyen", "Select equipment to refine");

        text = text.Replace("Ch\u1ECDn \u0110\u00E1 Tinh Luy\u1EC7n", "Select Refinement Stone");
        text = text.Replace("Chon Da Tinh Luyen", "Select Refinement Stone");

        text = text.Replace("T\u1EC9 l\u1EC7 th\u00E0nh c\u00F4ng: 0%", "Success Rate: 0%");
        text = text.Replace("Ti le thanh cong: 0%", "Success Rate: 0%");

        // Fusion section
        text = text.Replace("GHEP DA", "FUSION");
        text = text.Replace("GH\u00C9P \u0110\u00C1", "FUSION");
        text = text.Replace("GHEP", "FUSE");
        text = text.Replace("4\u00D7 \u2192 1\u00D7", "4x >> 1x");

        // Fusion placeholder
        text = text.Replace("Ch\u1ECDn \u0111\u00E1 \u0111\u1EC3 gh\u00E9p", "Select stone to fuse");
        text = text.Replace("Chon da de ghep", "Select stone to fuse");

        return text;
    }
}
