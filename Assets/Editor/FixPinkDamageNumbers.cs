using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FixPinkDamageNumbers : EditorWindow
{
    [MenuItem("Tools/Fix Pink DamageNumbers")]
    public static void ShowWindow()
    {
        GetWindow<FixPinkDamageNumbers>("Fix Pink DamageNumbers");
    }

    void OnGUI()
    {
        GUILayout.Label("Fix Pink Materials in DamageNumbersPro", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("This tool fixes pink materials by re-assigning\nthe 'TextMeshPro/Mobile/Distance Field' shader.", EditorStyles.wordWrappedLabel);
        
        GUILayout.Space(10);

        if (GUILayout.Button("Fix Pink Materials"))
        {
            FixMaterials();
        }
    }

    void FixMaterials()
    {
        string folderPath = "Assets/ASSETS/Package_Asset/DamageNumbersPro";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError($"Folder not found: {folderPath}");
            EditorUtility.DisplayDialog("Error", $"Folder not found:\n{folderPath}", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        
        // Try to find the correct shader. 
        // "TextMeshPro/Mobile/Distance Field" is the standard mobile SDF shader.
        Shader targetShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
        
        if (targetShader == null)
        {
            Debug.LogWarning("'TextMeshPro/Mobile/Distance Field' not found. Trying 'TextMeshPro/Distance Field'...");
            targetShader = Shader.Find("TextMeshPro/Distance Field");
        }

        if (targetShader == null)
        {
            Debug.LogError("Could not find any suitable TextMeshPro shader. Please ensure TextMeshPro is imported.");
            EditorUtility.DisplayDialog("Error", "Could not find 'TextMeshPro/Mobile/Distance Field' or 'TextMeshPro/Distance Field'.\n\nPlease ensure TextMeshPro is imported correctly.", "OK");
            return;
        }

        Debug.Log($"Targeting shader: {targetShader.name}");

        int fixCount = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Load all assets at path because materials might be sub-assets inside FontAssets
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            foreach (Object asset in assets)
            {
                if (asset is Material mat)
                {
                    // Check if shader is missing (Hidden/InternalErrorShader)
                    if (mat.shader.name == "Hidden/InternalErrorShader" || mat.shader == null)
                    {
                        mat.shader = targetShader;
                        EditorUtility.SetDirty(mat);
                        fixCount++;
                        Debug.Log($"Fixed material: {mat.name} in {path} (Switched to {targetShader.name})");
                    }
                }
            }
        }

        if (fixCount > 0)
        {
             AssetDatabase.SaveAssets();
             AssetDatabase.Refresh();
             Debug.Log($"Successfully fixed {fixCount} materials!");
             EditorUtility.DisplayDialog("Success", $"Fixed {fixCount} pink materials.\n\nMaterial shader set to: {targetShader.name}", "OK");
        }
        else
        {
            Debug.Log("No pink materials found in the target folder to fix.");
            EditorUtility.DisplayDialog("Info", "No materials with 'Hidden/InternalErrorShader' found in 'DamageNumbersPro' folder.", "OK");
        }
    }
}
