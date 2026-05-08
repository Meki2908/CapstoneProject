using Fusion;
using UnityEngine;

/// <summary>
/// Bootstrap networking for Map_Chinh when returning from dungeon after Runner.Shutdown().
/// Option B: Scene is already loaded by SceneTransitionManager/Unity, so we DO NOT pass Scene/NetworkSceneInfo.
/// </summary>
public class MapChinhBootstrapper : MonoBehaviour
{
    [Header("Runner")]
    [Tooltip("A prefab (or scene object reference) that has a NetworkRunner + NetworkSceneManagerDefault configured.")]
    public NetworkRunner runnerPrefab;

    [Tooltip("Session name for the main map.")]
    public string sessionName = "MainMapSession";

    private async void Start()
    {
        var spawner = FindFirstObjectByType<PlayerSpawner>();
        
        // Ensure a loading overlay is visible during "runner recovery" and spawn.
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartNetworkingLoadingUI();
        }
        else
        {
            Debug.LogWarning("[MapChinhBootstrapper] SceneTransitionManager.Instance is NULL, cannot show loading UI.");
        }

        // If a runner already exists and is running, attach spawner callbacks and ensure local player exists.
        var existingRunner = FindFirstObjectByType<NetworkRunner>();
        if (existingRunner != null && existingRunner.IsRunning)
        {
            Debug.Log($"[MapChinhBootstrapper] Runner already running: {existingRunner.name} mode={existingRunner.GameMode}");
            if (spawner != null)
            {
                existingRunner.AddCallbacks(spawner);
                spawner.EnsureLocalPlayerSpawned(existingRunner);
            }
            else
            {
                Debug.LogWarning("[MapChinhBootstrapper] Runner is running but PlayerSpawner not found in scene.");
            }
            return;
        }

        if (runnerPrefab == null)
        {
            Debug.LogError("[MapChinhBootstrapper] runnerPrefab is NULL. Please assign a NetworkRunner prefab.");
            return;
        }

        // If there is a runner component in scene but it's not running (after Shutdown), destroy ONLY the component.
        // Do not destroy the whole GameObject (it may be the persistent Network Manager).
        if (existingRunner != null && !existingRunner.IsRunning)
        {
            Debug.LogWarning($"[MapChinhBootstrapper] Found non-running runner '{existingRunner.name}'. Destroying runner component before recreating.");
            Destroy(existingRunner);
        }

        // Create a fresh runner
        var runner = Instantiate(runnerPrefab);
        runner.name = "NetworkRunner_MainMap";
        runner.ProvideInput = true;

        // Attach spawner callbacks if present
        if (spawner != null)
        {
            runner.AddCallbacks(spawner);
        }
        else
        {
            Debug.LogWarning("[MapChinhBootstrapper] No PlayerSpawner found in scene. Player will not be spawned on join.");
        }

        Debug.Log("[MapChinhBootstrapper] Starting Runner on current scene (Option B)...");

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = sessionName,
        });

        Debug.Log($"[MapChinhBootstrapper] StartGame ok={result.Ok} reason={result.ShutdownReason}");
    }
}
