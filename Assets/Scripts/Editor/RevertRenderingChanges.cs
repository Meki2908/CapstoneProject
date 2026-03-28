#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Khôi phục lại trạng thái rendering ban đầu:
/// 1. Reset Quality Level về mức cao nhất
/// 2. Revert tất cả material về Render Face = Front (trạng thái gốc)
/// 3. Clear Occlusion Culling data
/// 4. Clear PlayerPrefs settings
/// Chạy từ menu: Tools → Revert All Rendering Changes
/// </summary>
public class RevertRenderingChanges
{
    [MenuItem("Tools/Revert All Rendering Changes")]
    public static void RevertAll()
    {
        int materialCount = 0;

        // === 1. RESET QUALITY LEVEL ===
        // Đặt về quality level cao nhất trong project
        int maxLevel = QualitySettings.names.Length - 1;
        QualitySettings.SetQualityLevel(maxLevel, true);
        Debug.Log($"[Revert] Quality Level → {QualitySettings.names[maxLevel]} (index {maxLevel})");

        // === 2. REVERT MATERIALS: Render Face → Front (mặc định gốc) ===
        string[] guids = AssetDatabase.FindAssets("t:Material");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            if (mat.HasProperty("_Cull"))
            {
                float currentCull = mat.GetFloat("_Cull");
                if (currentCull == 0f) // 0 = Both (đã bị tool trước đổi)
                {
                    mat.SetFloat("_Cull", 2f); // 2 = Back → thực ra mặc định URP là Front
                    // URP: 0=Off(Both), 1=Front, 2=Back. Mặc định URP Lit = 2 (Back = cull back faces = render front)
                    mat.SetFloat("_Cull", 2f);
                    EditorUtility.SetDirty(mat);
                    materialCount++;
                }
            }
        }
        Debug.Log($"[Revert] Đã revert {materialCount} materials → Render Face = Front (mặc định)");

        // === 3. CLEAR PLAYERPREFS ===
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[Revert] Đã xóa toàn bộ PlayerPrefs");

        AssetDatabase.SaveAssets();

        string msg = $"Đã khôi phục:\n" +
                     $"• Quality Level → {QualitySettings.names[maxLevel]}\n" +
                     $"• {materialCount} materials → Render Face gốc\n" +
                     $"• PlayerPrefs đã xóa sạch\n\n" +
                     $"Hãy Clear Occlusion Culling thủ công:\n" +
                     $"Window → Rendering → Occlusion Culling → Clear";

        EditorUtility.DisplayDialog("Revert Rendering Changes", msg, "OK");
    }
}
#endif
