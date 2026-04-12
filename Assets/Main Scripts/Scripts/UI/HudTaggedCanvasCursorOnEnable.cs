using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn trên object tag "HUD" được bật/tắt bằng SetActive khi cần cursor tự do
/// (vd: Blacksmith NPC menu, Dungeon complete panel, World Map...).
/// 
/// OnEnable → báo MouseLockManager rằng 1 HUD object vừa hiện (P3 priority).
/// OnDisable → báo MouseLockManager rằng 1 HUD object vừa ẩn.
/// 
/// Dùng counter trong MouseLockManager, không dùng toggle → spam-safe.
/// Không tự set Cursor.visible / Cursor.lockState.
/// </summary>
[DefaultExecutionOrder(50)]
public sealed class HudTaggedCanvasCursorOnEnable : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Để trống = không lọc theo tag. Nếu gán (vd. HUD) thì chỉ chạy khi CompareTag khớp.")]
    private string requiredTag = "";

    [SerializeField]
    [Tooltip("Bỏ qua OnEnable nếu quá sớm sau khi load scene — tránh mở cursor nhầm.")]
    private bool suppressDuringEarlySceneFrames = true;

    [SerializeField]
    [Min(0)]
    private int earlyFramesAfterSceneLoad = 2;

    private static int s_lastSingleSceneLoadFrame = int.MinValue / 4;

    // Track xem đã notify "active" cho MouseLockManager chưa
    private bool _notifiedActive = false;

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
        if (!isActiveAndEnabled || !PassesTagFilter()) return;
        StartCoroutine(NotifyActiveAfterFrame());
    }

    private void OnDisable()
    {
        if (!PassesTagFilter()) return;
        if (_notifiedActive)
        {
            _notifiedActive = false;
            MouseLockManager.Instance?.NotifyHudVisibilityChanged(false);
        }
    }

    private void OnDestroy()
    {
        // Đảm bảo không để counter bị leak khi object bị destroy khi đang active
        if (_notifiedActive)
        {
            _notifiedActive = false;
            MouseLockManager.Instance?.NotifyHudVisibilityChanged(false);
        }
    }

    private bool PassesTagFilter()
    {
        if (string.IsNullOrWhiteSpace(requiredTag)) return true;
        try { return CompareTag(requiredTag.Trim()); }
        catch (UnityException) { return false; }
    }

    private IEnumerator NotifyActiveAfterFrame()
    {
        yield return null;
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;
        if (!PassesTagFilter()) yield break;
        if (suppressDuringEarlySceneFrames &&
            earlyFramesAfterSceneLoad >= 0 &&
            Time.frameCount - s_lastSingleSceneLoadFrame <= earlyFramesAfterSceneLoad)
            yield break;

        if (!_notifiedActive)
        {
            _notifiedActive = true;
            MouseLockManager.Instance?.NotifyHudVisibilityChanged(true);
        }
    }
}
