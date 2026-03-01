using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// Tool to restore DamageNumbersPro materials back to original TMP shader
/// </summary>
public class RestoreDamageNumberMaterials : EditorWindow
{
    [MenuItem("Tools/Restore DamageNumber Materials (Fix Missing Shader)")]
    public static void RestoreMaterials()
    {
        // Find TMP Distance Field shader
        Shader tmpShader = Shader.Find("TextMeshPro/Distance Field");
        
        if (tmpShader == null)
        {
            tmpShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
        }
        
        if (tmpShader == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "Could not find TextMeshPro shader!", "OK");
            return;
        }
        
        string damageNumbersPath = "Assets/ASSETS/Package_Asset/DamageNumbersPro/Materials";
        
        if (!AssetDatabase.IsValidFolder(damageNumbersPath))
        {
            EditorUtility.DisplayDialog("Error", 
                "DamageNumbersPro Materials folder not found.", "OK");
            return;
        }
        
        int count = 0;
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { damageNumbersPath });
        
        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null && mat.shader.name.Contains("Custom") || mat.shader.name == "Hidden/InternalErrorShader")
            {
                // Restore to TMP shader
                mat.shader = tmpShader;
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"Restored shader on: {path}");
            }
        }
        
        // Also check TMP Font Assets
        string[] fontAssetGuids = AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { damageNumbersPath });
        foreach (string guid in fontAssetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            
            if (fontAsset != null && fontAsset.material != null)
            {
                if (fontAsset.material.shader.name.Contains("Custom") || fontAsset.material.shader.name == "Hidden/InternalErrorShader")
                {
                    fontAsset.material.shader = tmpShader;
                    EditorUtility.SetDirty(fontAsset);
                    count++;
                    Debug.Log($"Restored Font Asset shader on: {path}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Complete", 
            $"Restored {count} materials to original TMP shader.", "OK");
    }
}
