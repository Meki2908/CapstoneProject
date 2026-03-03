using UnityEngine;
using UnityEditor;

/// <summary>
/// Fixes EGA particle shaders that are missing _Color property in newer Unity versions
/// Run this from Unity Editor: Window > Fix EGAShaders
/// </summary>
public class EGAShaderFixer : EditorWindow
{
    [MenuItem("Window/Fix EGAShaders")]
    public static void ShowWindow()
    {
        GetWindow<EGAShaderFixer>("EGA Shader Fixer");
    }

    void OnGUI()
    {
        GUILayout.Label("Fix EGA Particle Shaders for New Unity Versions", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Fix All EGA Materials"))
        {
            FixAllEGAMaterials();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "This will find all materials using EGA shaders and ensure they have the correct properties.\n\n" +
            "If materials still show purple, try re-importing the EGA asset packages.",
            MessageType.Info);
    }

    void FixAllEGAMaterials()
    {
        int fixedCount = 0;
        string[] shaderNames = new string[] {
            "EGA/Particles/Add_CenterGlow",
            "EGA/Particles/Blend_CenterGlow",
            "EGA/Particles/Lit_CenterGlow",
            "EGA/Particles/Add_DistortTexture",
            "EGA/Particles/Blend_DistortTexture",
            "EGA/Particles/Blend_Electricity",
            "EGA/Particles/Add_Trail"
        };

        string[] guids = AssetDatabase.FindAssets("t:Material");
        int count = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            foreach (string shaderName in shaderNames)
            {
                if (mat.shader != null && mat.shader.name == shaderName)
                {
                    // Ensure emission is enabled
                    if (mat.HasProperty("_Emission"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", Color.white * mat.GetFloat("_Emission"));
                    }

                    // Add default color if missing (for newer Unity versions)
                    if (!mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", new Color(0.5f, 0.5f, 0.5f, 1f));
                    }

                    fixedCount++;
                    Debug.Log($"Fixed material: {path} with shader: {shaderName}");
                    break;
                }
            }
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Fixed {fixedCount} EGA materials out of {count} total materials");
        EditorUtility.DisplayDialog("EGA Shader Fixer", $"Fixed {fixedCount} materials!", "OK");
    }
}
