using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên NPC (ví dụ Leona). Sau hội thoại / quest → lưu game, tùy chọn tắt Fusion runner, rồi load scene qua <see cref="SceneTransitionManager"/>.
/// </summary>
public class QuestSceneTeleporter : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Tên scene trong Build Settings (VD: Tutorial).")]
    public string targetScene = "Tutorial";

    [Header("Timing")]
    public float delayBeforeLoad = 1.5f;

    [Header("Network")]
    [Tooltip("Tắt NetworkRunner trước khi load (cần khi sang Tutorial solo / bootstrap lại Fusion).")]
    [SerializeField] private bool shutdownNetworkRunnerBeforeLoad = true;

    [Header("Scene transition")]
    [SerializeField] private string loadingMessage = "Đang vào khu vực Hướng Dẫn...";
    [SerializeField] private bool interruptIfTransitioning = true;
    [SerializeField] private bool waitForNetworkSpawn = true;

    [Header("Legacy (không dùng trong code — giữ field để prefab cũ không mất serial)")]
    public bool useFade = true;

    /// <summary>Gọi từ <see cref="LeonaDialogue"/> hoặc UnityEvent.</summary>
    public void TeleportToScene()
    {
        StartCoroutine(DoTeleport());
    }

    IEnumerator DoTeleport()
    {
        try { GameController.PlayerSave(); }
        catch (System.Exception e) { Debug.LogWarning($"[QuestSceneTeleporter] PlayerSave failed: {e.Message}"); }

        yield return new WaitForSeconds(delayBeforeLoad);

        if (shutdownNetworkRunnerBeforeLoad)
        {
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null && runner.IsRunning)
            {
                runner.Shutdown(false, ShutdownReason.Ok);
            }
        }

        var stm = SceneTransitionManager.Instance != null
            ? SceneTransitionManager.Instance
            : SceneTransitionManager.EnsureInstance();

        stm.GoToScene(
            targetScene,
            loadingMessage,
            interruptIfTransitioning,
            waitForNetworkSpawn);
    }

    /// <summary>Dự phòng: load thuần Unity, không shutdown / không loading UI.</summary>
    public void LoadSceneSimple()
    {
        SceneManager.LoadScene(targetScene);
    }
}
