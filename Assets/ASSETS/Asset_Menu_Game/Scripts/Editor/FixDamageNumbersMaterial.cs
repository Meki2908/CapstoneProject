using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Tool fix shader bị missing trong DamageNumbersPro materials
/// </summary>
public class FixDamageNumbersMaterial
{
    // Shader cũ bị missing
    private const string OLD_SHADER_GUID = "f8f478ece227f734d88eda1eb4d800f0";
    
    // Shader TMP_SDF mới (có sẵn trong project)
    private const string NEW_SHADER_GUID = "68e6db2ebdc24f95958faec2be5558d6";
    
    [MenuItem("Tools/Fix Materials/Fix DamageNumbersPro Shaders")]
    public static void FixDamageNumbersShaders()
    {
        string materialsPath = "Assets/Assets/Package_Asset/DamageNumbersPro/Materials";
        
        if (!Directory.Exists(materialsPath))
        {
            EditorUtility.DisplayDialog("Lỗi", 
                $"Không tìm thấy thư mục:\n{materialsPath}", "OK");
            return;
        }
        
        // Tìm tất cả .asset files
        string[] assetFiles = Directory.GetFiles(materialsPath, "*.asset", SearchOption.AllDirectories);
        
        int fixedCount = 0;
        
        foreach (string filePath in assetFiles)
        {
            if (filePath.EndsWith(".meta")) continue;
            
            string content = File.ReadAllText(filePath);
            
            if (content.Contains(OLD_SHADER_GUID))
            {
                // Replace shader GUID
                string newContent = content.Replace(OLD_SHADER_GUID, NEW_SHADER_GUID);
                File.WriteAllText(filePath, newContent);
                fixedCount++;
                Debug.Log($"[Fixed] {Path.GetFileName(filePath)}");
            }
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Hoàn thành!", 
                $"Đã fix {fixedCount} material files!\n\n" +
                "Shader đã được thay bằng TMP_SDF.\n\n" +
                "Nếu vẫn còn lỗi, thử restart Unity.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Không có gì để fix", 
                "Không tìm thấy material nào có shader bị missing.", "OK");
        }
    }
    
    [MenuItem("Tools/Fix Materials/Fix ALL Missing Shaders in Project")]
    public static void FixAllMissingShaders()
    {
        string[] allAssets = Directory.GetFiles("Assets", "*.asset", SearchOption.AllDirectories);
        
        int fixedCount = 0;
        
        foreach (string filePath in allAssets)
        {
            if (filePath.EndsWith(".meta")) continue;
            
            string content = File.ReadAllText(filePath);
            
            if (content.Contains(OLD_SHADER_GUID))
            {
                string newContent = content.Replace(OLD_SHADER_GUID, NEW_SHADER_GUID);
                File.WriteAllText(filePath, newContent);
                fixedCount++;
                Debug.Log($"[Fixed] {filePath}");
            }
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Hoàn thành!", 
                $"Đã fix {fixedCount} files!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Không có gì để fix", 
                "Không tìm thấy file nào có shader bị missing.", "OK");
        }
    }
    
    [MenuItem("Tools/Fix Materials/Reimport DamageNumbersPro Materials")]
    public static void ReimportMaterials()
    {
        string materialsPath = "Assets/Assets/Package_Asset/DamageNumbersPro/Materials";
        
        if (!Directory.Exists(materialsPath))
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy thư mục Materials!", "OK");
            return;
        }
        
        AssetDatabase.ImportAsset(materialsPath, ImportAssetOptions.ImportRecursive);
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Hoàn thành!", "Đã reimport tất cả materials!", "OK");
    }
}
#endif
