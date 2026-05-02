using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn trên object được bật bằng SetActive khi cần chuột tự do + Normal Cursor texture.
/// Tag: để <see cref="requiredTag"/> trống thì không kiểm tra tag (dùng cho object con với tag riêng).
/// Lưu ý: nếu UI đã gọi <see cref="CursorUIPriority.BeginUiOverlay"/> trước khi bật panel, vẫn áp texture + đồng bộ MouseLockManager.
/// </summary>
[DefaultExecutionOrder(50)]
public sealed class HudTaggedCanvasCursorOnEnable : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Để trống = không lọc theo tag. Nếu gán (vd. HUD) thì chỉ chạy khi CompareTag khớp.")]
    private string requiredTag = "";

    [SerializeField]
    [Tooltip("Bỏ qua OnEnable nếu quá sớm sau khi load scene (Single) — tránh mở chuột nhầm cho HUD mặc định luôn active.")]
    private bool suppressDuringEarlySceneFrames = true;

    [SerializeField]
    [Min(0)]
    private int earlyFramesAfterSceneLoad = 2;

    private static int s_lastSingleSceneLoadFrame = int.MinValue / 4;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoadedStatic;
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            s_lastSingleSceneLoadFrame = Time.frameCount;
    }

    private void OnEnable()
    {
        if (!isActiveAndEnabled || !PassesTagFilter())
            return;
        StartCoroutine(ApplyAfterDeferredFrame());
    }

    private bool PassesTagFilter()
    {
        if (string.IsNullOrWhiteSpace(requiredTag))
            return true;
        try
        {
            return CompareTag(requiredTag.Trim());
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private IEnumerator ApplyAfterDeferredFrame()
    {
        yield return null;
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            yield break;
        if (!PassesTagFilter())
            yield break;
        if (suppressDuringEarlySceneFrames &&
            earlyFramesAfterSceneLoad >= 0 &&
            Time.frameCount - s_lastSingleSceneLoadFrame <= earlyFramesAfterSceneLoad)
            yield break;

        // UI thường gọi BeginUiOverlay trước khi SetActive(panel) → không được bỏ qua, chỉ cần áp texture + lock.
        if (CursorUIPriority.IsUiOverlayActive)
        {
            if (MouseLockManager.Instance != null)
            {
                MouseLockManager.Instance.SetGameplayCursorLocked(false);
                MouseLockManager.Instance.ClearGameplayLockRetries();
            }
            GameCursorManager.TryApplyNormalCursorTextureFromScene();
            yield break;
        }

        MovementSystem.CameraCursor.ApplyFreeCursorForHudCanvasActivated();
    }
}
