#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool: Fix backface culling cho tất cả Material trong project.
/// Chạy từ menu: Tools → Fix Backface Culling (All Materials)
/// </summary>
public class FixBackfaceCulling
{
    [MenuItem("Tools/Fix Backface Culling (All Materials)")]
    public static void FixAllMaterials()
    {
        // Tìm tất cả Material trong project
        string[] guids = AssetDatabase.FindAssets("t:Material");
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            // Chỉ fix material có property _Cull (URP Lit, Simple Lit, etc.)
            if (mat.HasProperty("_Cull"))
            {
                float currentCull = mat.GetFloat("_Cull");
                if (currentCull != 0f) // 0 = Off (render Both), 1 = Front, 2 = Back
                {
                    mat.SetFloat("_Cull", 0f); // Render Both faces
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FixBackfaceCulling] Đã fix {fixedCount}/{guids.Length} materials → Render Face = Both");
        EditorUtility.DisplayDialog("Fix Backface Culling",
            $"Đã fix {fixedCount} materials!\nTất cả material giờ render cả 2 mặt.", "OK");
    }
}
#endif
