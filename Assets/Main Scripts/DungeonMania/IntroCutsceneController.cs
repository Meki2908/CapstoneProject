using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên object intro / timeline (chỉ active khi intro chạy).
/// - Khi host intro đang active + enabled: không đụng HUD (Timeline Control track tự xử lý).
/// - Khi không có host active (object tắt, destroy, hoặc không có trong scene): ép mọi object tag HUD bật.
/// </summary>
[DefaultExecutionOrder(-50)]
public class IntroCutsceneController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Bật cho object đang chạy intro timeline. Tắt nếu bạn chỉ muốn dùng script làm watcher trên HUD (ít dùng).")]
    private bool timelineIntroHost = true;

    private const string DefaultHudTag = "HUD";

    private static int s_activeHostCount;

    static IntroCutsceneController()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Application.isPlaying) return;
        IntroCutsceneDefer.RunNextFrame(ApplyHudPolicyStatic);
    }

    private void OnEnable()
    {
        if (timelineIntroHost)
            s_activeHostCount++;
        ApplyHudPolicyStatic();
    }

    private void OnDisable()
    {
        if (timelineIntroHost)
            s_activeHostCount--;
        ApplyHudPolicyStatic();
    }

    private static void ApplyHudPolicyStatic()
    {
        if (s_activeHostCount > 0) return;
        EnsureHudTaggedObjectsActive(DefaultHudTag);
    }

    private static void EnsureHudTaggedObjectsActive(string tag)
    {
        try
        {
            var objs = GameObject.FindGameObjectsWithTag(tag);
            if (objs == null || objs.Length == 0) return;
            foreach (var go in objs)
            {
                if (go == null) continue;
                if (!go.activeSelf) go.SetActive(true);
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[IntroCutsceneController] Tag \"{tag}\" chưa được định nghĩa trong Tag Manager.");
        }
    }

    /// <summary>Coroutine helper — không cần GameObject trong scene.</summary>
    private sealed class IntroCutsceneDefer : MonoBehaviour
    {
        private System.Action _onDone;

        public static void RunNextFrame(System.Action action)
        {
            var go = new GameObject("__IntroCutsceneHudDefer");
            var d = go.AddComponent<IntroCutsceneDefer>();
            d._onDone = action;
        }

        private IEnumerator Start()
        {
            yield return null;
            _onDone?.Invoke();
            Destroy(gameObject);
        }
    }
}
