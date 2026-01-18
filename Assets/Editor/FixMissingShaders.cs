using UnityEditor;
using UnityEngine;

// Small editor utility to replace materials that reference a missing shader
// (Hidden/InternalErrorShader) with a sane fallback (URP/Lit -> Toon -> Standard).
// Usage: Window -> Tools -> Repair -> Replace Missing Shaders
public static class FixMissingShaders
{
    [MenuItem("Tools/Repair/Replace Missing Shaders with URP Lit")]
    public static void ReplaceMissingShadersWithURPLit()
    {
        ReplaceMissingShaders();
    }

    public static void ReplaceMissingShaders()
    {
        // Find all materials in project
        var guids = AssetDatabase.FindAssets("t:Material");
        int replaced = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            Shader shader = null;
            try { shader = mat.shader; } catch { shader = null; }

            if (shader == null || shader.name == "Hidden/InternalErrorShader")
            {
                // Prefer URP Lit, then Toon (if present), then Standard
                Shader newShader = Shader.Find("Universal Render Pipeline/Lit")
                                   ?? Shader.Find("Toon/Lit")
                                   ?? Shader.Find("Toon")
                                   ?? Shader.Find("Standard");

                if (newShader != null)
                {
                    Undo.RecordObject(mat, "Replace missing shader");
                    mat.shader = newShader;
                    EditorUtility.SetDirty(mat);
                    replaced++;
                    Debug.Log($"[FixMissingShaders] Replaced shader on {path} -> {newShader.name}");
                }
                else
                {
                    Debug.LogWarning($"[FixMissingShaders] No suitable replacement shader found for {path}. Install URP or a Toon shader package.");
                }
            }
        }

        if (replaced > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("FixMissingShaders", $"Finished. Materials replaced: {replaced}", "OK");
    }
}


