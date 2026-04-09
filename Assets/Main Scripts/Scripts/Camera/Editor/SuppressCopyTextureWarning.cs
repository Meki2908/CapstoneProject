#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// [EDITOR ONLY] Tự động clear console khi chuyển scene,
/// loại bỏ các warning "CopyTexture null" từ URP internal.
///
/// Lỗi này là bug nội bộ của Unity 6 + URP Editor —
/// KHÔNG ảnh hưởng khi build game (chỉ xảy ra trong Editor).
///
/// Đặt trong folder Editor hoặc dùng #if UNITY_EDITOR.
/// </summary>
[InitializeOnLoad]
public static class SuppressCopyTextureWarning
{
    static SuppressCopyTextureWarning()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        // Clear "CopyTexture null" errors khi mở scene trong Editor
        ClearConsole();
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Clear sau khi scene loaded trong Play mode
        ClearConsole();
    }

    static void ClearConsole()
    {
        // Dùng reflection để gọi LogEntries.Clear() — clear console
        var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
        if (logEntries != null)
        {
            var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            if (clearMethod != null)
            {
                clearMethod.Invoke(null, null);
            }
        }
    }
}
#endif
