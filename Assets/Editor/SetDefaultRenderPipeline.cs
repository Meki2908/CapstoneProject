using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class SetDefaultRenderPipeline : EditorWindow
{
    [MenuItem("Tools/Fix Render Pipeline")]
    public static void ShowWindow()
    {
        GetWindow<SetDefaultRenderPipeline>("Fix Render Pipeline");
    }

    private void OnGUI()
    {
        GUILayout.Label("Render Pipeline Asset Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Set Default to PC_RPAsset"))
        {
            FixRenderPipeline();
        }
    }

    private static void FixRenderPipeline()
    {
        // 1. Find the PC_RPAsset
        string[] guids = AssetDatabase.FindAssets("PC_RPAsset t:RenderPipelineAsset");
        if (guids.Length == 0)
        {
            Debug.LogError("Could not find 'PC_RPAsset' in the project!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        RenderPipelineAsset targetAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);

        if (targetAsset == null)
        {
            Debug.LogError($"Failed to load asset at {path}");
            return;
        }

        // 2. Set in Graphics Settings
        GraphicsSettings.defaultRenderPipeline = targetAsset;
        Debug.Log($"[Graphics] Set default Render Pipeline to: {targetAsset.name}");

        // 3. Set in Quality Settings (all levels)
        int qualityLevelCount = QualitySettings.names.Length;
        for (int i = 0; i < qualityLevelCount; i++)
        {
            QualitySettings.SetQualityLevel(i);
            QualitySettings.renderPipeline = targetAsset; 
            // Note: In newer Unity versions, setting QualitySettings.renderPipeline might affect the current level only or override.
            // But usually, we only need to set it in GraphicsSettings if QualitySettings are set to "None" (Use Default).
            // However, to be sure, we explicitly set it or clear it to use default.
            
            // Actually, best practice is:
            // If we want GLOBAL default, set GraphicsSettings.renderPipelineAsset.
            // And ensure Quality Levels have 'None' so they use the global default.
            
            QualitySettings.renderPipeline = null; // Set to null to use the GraphicsSettings asset
            Debug.Log($"[Quality] Level {i} ({QualitySettings.names[i]}): Set to use Global Default (PC_RPAsset)");
        }
        
        // Save changes
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("Success", 
            $"Default Render Pipeline has been set to '{targetAsset.name}'.\n\n" +
            "Graphics Settings: Updated\n" +
            "Quality Settings: All levels set to use Global Default.", "OK");
    }
}
