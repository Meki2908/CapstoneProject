#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Fusion;

/// <summary>
/// Gắn Fusion <see cref="NetworkTransform"/> lên root player (cùng GameObject với <see cref="NetworkObject"/>).
/// Chạy: menu Fusion → Setup Player_3.0 (NetworkObject + NetworkTransform).
/// </summary>
public static class FusionPlayerPrefabSetup
{
    const string PrefabPath = "Assets/_Prefabs/Player_3.0.prefab";

    /// <summary>
    /// Chạy từ CLI khi không có Unity Editor nào mở project:
    /// Unity.exe -batchmode -quit -projectPath ... -executeMethod FusionPlayerPrefabSetup.BatchSetup
    /// </summary>
    public static void BatchSetup() => SetupPlayerPrefab(silent: true);

    [MenuItem("Fusion/Setup Player_3.0 (NetworkObject + NetworkTransform)")]
    public static void SetupPlayerPrefabFromMenu() => SetupPlayerPrefab(silent: false);

    static void SetupPlayerPrefab(bool silent)
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (prefabRoot.GetComponent<NetworkObject>() == null)
                prefabRoot.AddComponent<NetworkObject>();

            if (prefabRoot.GetComponent<NetworkTransform>() == null)
                prefabRoot.AddComponent<NetworkTransform>();

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            if (!silent)
                EditorUtility.DisplayDialog("Fusion", "Đã lưu prefab Player_3.0:\n- NetworkObject (root)\n- NetworkTransform (đồng bộ vị trí/góc)", "OK");
            Debug.Log("[FusionPlayerPrefabSetup] Updated " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
