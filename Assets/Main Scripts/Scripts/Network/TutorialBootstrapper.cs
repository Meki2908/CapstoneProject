using Fusion;
using UnityEngine;

/// <summary>
/// Đặt trong scene Tutorial sau khi <see cref="QuestSceneTeleporter"/> (hoặc flow tương đương) đã Shutdown runner ở Map_Chinh.
/// Khởi động lại Fusion <see cref="GameMode.Single"/> và đăng ký <see cref="PlayerSpawner"/> giống <see cref="MapChinhBootstrapper"/>.
/// </summary>
public class TutorialBootstrapper : MonoBehaviour
{
    [Header("Runner")]
    [Tooltip("Prefab NetworkRunner (cùng loại dùng ở Map_Chinh) — phải đã cấu hình scene manager trên prefab.")]
    [SerializeField] private NetworkRunner runnerPrefab;

    [Tooltip("Tên session riêng cho tutorial solo.")]
    [SerializeField] private string sessionName = "Tutorial_Room";

    private async void Start()
    {
        var spawner = FindFirstObjectByType<PlayerSpawner>();

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.StartNetworkingLoadingUI();
        else
            Debug.LogWarning("[TutorialBootstrapper] SceneTransitionManager.Instance is NULL — loading UI có thể không hiện.");

        var existingRunner = FindFirstObjectByType<NetworkRunner>();
        if (existingRunner != null && existingRunner.IsRunning)
        {
            if (spawner != null)
            {
                existingRunner.AddCallbacks(spawner);
                spawner.EnsureLocalPlayerSpawned(existingRunner);
            }
            else
                Debug.LogWarning("[TutorialBootstrapper] PlayerSpawner không tìm thấy trong scene.");
            return;
        }

        if (runnerPrefab == null)
        {
            Debug.LogError("[TutorialBootstrapper] runnerPrefab chưa gán.");
            return;
        }

        if (existingRunner != null && !existingRunner.IsRunning)
        {
            Debug.LogWarning($"[TutorialBootstrapper] Xóa NetworkRunner không chạy: {existingRunner.name}");
            Destroy(existingRunner);
        }

        var runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner_Tutorial";
        runner.ProvideInput = true;

        if (spawner != null)
            runner.AddCallbacks(spawner);
        else
            Debug.LogWarning("[TutorialBootstrapper] Không có PlayerSpawner — nhân vật sẽ không được spawn.");

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Single,
            SessionName = sessionName,
        });

        if (!result.Ok)
            Debug.LogError($"[TutorialBootstrapper] StartGame thất bại: {result.ShutdownReason}");
    }
}
