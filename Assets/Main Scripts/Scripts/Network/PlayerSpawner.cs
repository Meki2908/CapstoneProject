using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Spawns player NetworkObject when a player joins the session.
/// Designed for Option B (scene already loaded by Unity).
/// </summary>
public class PlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private NetworkPrefabRef lootBroadcasterPrefab;
    private bool _broadcasterSpawned = false;

    [Header("Spawn")]
    [Tooltip("Fallback spawn position if no SpawnPoint tag exists and no return point is available.")]
    [SerializeField] private Vector3 fallbackSpawnPos = new Vector3(0, 5f, 0);

    [Tooltip("Optional override. If set, use this transform as default spawn point (when no return point).")]
    [SerializeField] private Transform defaultSpawnPoint;

    [Header("Return Point")]
    [Tooltip("If true, prefer PlayerWorldData.ReturnPosition when returning from dungeon.")]
    [SerializeField] private bool useReturnPoint = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;
    
    [Header("Loading Screen")]
    [Tooltip("Giữ loading tối thiểu để người chơi kịp nhìn thấy (unscaled seconds).")]
    [SerializeField] private float minLoadingSeconds = 0.75f;
    
    [Tooltip("Thời gian tối đa chờ camera local ready trước khi tắt loading (unscaled seconds).")]
    [SerializeField] private float cameraWaitTimeoutSeconds = 2.5f;
    
    [Tooltip("Nếu true: chỉ tắt loading sau khi thấy Player NetworkObject (Player_3.0(Clone)) xuất hiện trong scene.")]
    [SerializeField] private bool requireLocalPlayerObjectPresentToFinishLoading = true;

    private Vector3 ResolveSpawnPosition(PlayerRef player)
    {
        // 1) Return point (preferred)
        if (useReturnPoint && PlayerWorldData.HasReturnPoint)
        {
            Vector3 p = PlayerWorldData.ReturnPosition + Vector3.up * 1.5f;
            PlayerWorldData.HasReturnPoint = false;
            return p;
        }

        // 2) Explicit default spawn point
        if (defaultSpawnPoint != null)
        {
            return defaultSpawnPoint.position + Vector3.up * 1.5f;
        }

        // 3) Any object tagged SpawnPoint
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = player.PlayerId % spawnPoints.Length;
            return spawnPoints[index].transform.position + Vector3.up * 1.5f;
        }

        return fallbackSpawnPos;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] OnPlayerJoined player={player} local={runner.LocalPlayer} isServer={runner.IsServer} scene={SceneManager.GetActiveScene().name}");

        if (player == runner.LocalPlayer)
        {
            // Do NOT finish loading here: PlayerJoined can fire before the player object/camera is fully ready.
            CursorUIPriority.EndAllUiOverlays();
            if (verboseLogs) Debug.Log("[PlayerSpawner] EndAllUiOverlays() called.");
        }

        if (!runner.IsServer)
            return;

        // Spawn loot broadcaster exactly once per session (server authority).
        if (!_broadcasterSpawned && lootBroadcasterPrefab.IsValid)
        {
            runner.Spawn(lootBroadcasterPrefab, Vector3.zero, Quaternion.identity);
            _broadcasterSpawned = true;
        }

        if (!playerPrefab.IsValid)
        {
            Debug.LogError("[PlayerSpawner] playerPrefab is not set/invalid.");
            return;
        }

        Vector3 spawnPos = ResolveSpawnPosition(player);
        var obj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] Spawned={(obj != null)} pos={spawnPos} objName={(obj != null ? obj.name : "null")}");

        // After spawning local player, force-enable movement input (common case: action map stuck in UI).
        if (obj != null && player == runner.LocalPlayer)
        {
            if (verboseLogs) Debug.Log("[PlayerSpawner] Post-spawn input restore begin.");
            var cc = obj.GetComponentInChildren<CharacterController>(true);
            if (cc != null) cc.enabled = true;
            if (verboseLogs) Debug.Log($"[PlayerSpawner] CharacterController enabled={(cc != null && cc.enabled)}");

            var pi = obj.GetComponentInChildren<PlayerInput>(true);
            if (pi != null)
            {
                pi.enabled = true;
                if (verboseLogs)
                    Debug.Log($"[PlayerSpawner] PlayerInput enabled={pi.enabled} currentMap={(pi.currentActionMap != null ? pi.currentActionMap.name : "null")}");
                try
                {
                    if (pi.currentActionMap != null && pi.currentActionMap.name != "Player")
                    {
                        string before = pi.currentActionMap.name;
                        pi.SwitchCurrentActionMap("Player");
                        if (verboseLogs) Debug.Log($"[PlayerSpawner] Switched action map to 'Player' (was '{before}'). now={(pi.currentActionMap != null ? pi.currentActionMap.name : "null")}");
                    }
                }
                catch { }
            }
            else if (verboseLogs)
            {
                Debug.LogWarning("[PlayerSpawner] No PlayerInput found on spawned player.");
            }

            // Finish loading only when local player object exists + camera is ready (prevents "blank world" after fade out).
            StartCoroutine(FinishLoadingWhenReady(runner));
        }
    }

    private System.Collections.IEnumerator FinishLoadingWhenReady(NetworkRunner runner)
    {
        float start = Time.realtimeSinceStartup;

        // If StartNetworkingLoadingUI() was called earlier, measure min time from that moment for better UX.
        float baseStart = Mathf.Max(SceneTransitionManager.LastNetworkingLoadingStartUnscaledTime, start);

        // 1) Wait until local player object exists (optional) AND camera is ready OR timeout
        while (Time.realtimeSinceStartup - start < cameraWaitTimeoutSeconds)
        {
            bool playerObjReady = true;
            if (requireLocalPlayerObjectPresentToFinishLoading)
            {
                playerObjReady = false;
                try
                {
                    // Fusion-compat: GetPlayerObject(localPlayer) when available.
                    if (runner != null && runner.IsRunning && runner.LocalPlayer.IsRealPlayer)
                    {
                        var pobj = runner.GetPlayerObject(runner.LocalPlayer);
                        if (pobj != null && pobj.name.StartsWith("Player_3.0", StringComparison.OrdinalIgnoreCase))
                            playerObjReady = true;
                        else if (pobj != null)
                            playerObjReady = true; // still acceptable; name may differ
                    }
                }
                catch { }
            }

            CinemachineCamera cam = PlayerNetworkSetup.LocalCinemachineCamera;
            bool camReady = (cam != null);

            if (playerObjReady && camReady)
                break;
            yield return null;
        }
        
        // 2) Wait one extra frame after readiness to ensure render/camera has applied.
        yield return null;

        // 3) Ensure a minimum display time for UX (even if ready instantly)
        while (Time.realtimeSinceStartup - baseStart < minLoadingSeconds)
        {
            yield return null;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.FinishLoadingUI();
        }
        else if (verboseLogs)
        {
            Debug.LogWarning("[PlayerSpawner] SceneTransitionManager.Instance is NULL at FinishLoadingWhenLocalCameraReady().");
        }
    }

    /// <summary>
    /// Use when a runner is already running (e.g. persisted from Menu) but LocalPlayerObject is still null.
    /// This can happen if the spawner callbacks were added after the session started.
    /// </summary>
    public void EnsureLocalPlayerSpawned(NetworkRunner runner)
    {
        if (runner == null) return;
        if (!runner.IsRunning) return;
        if (!runner.IsServer) return;
        
        // Fusion version compatibility: use GetPlayerObject instead of LocalPlayerObject property.
        try
        {
            var existing = runner.LocalPlayer.IsRealPlayer ? runner.GetPlayerObject(runner.LocalPlayer) : null;
            if (existing != null) return;
        }
        catch
        {
            // If GetPlayerObject isn't available for some reason, continue to attempt spawn.
        }

        if (!playerPrefab.IsValid)
        {
            Debug.LogError("[PlayerSpawner] EnsureLocalPlayerSpawned: playerPrefab is not set/invalid.");
            return;
        }

        PlayerRef p = runner.LocalPlayer;
        Vector3 spawnPos = ResolveSpawnPosition(p);
        var obj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, p);
        Debug.Log($"[PlayerSpawner] EnsureLocalPlayerSpawned: Spawned={(obj != null)} pos={spawnPos} objName={(obj != null ? obj.name : "null")}");
    }

    // Unused callbacks
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Provide input from the local player object (Fusion authoritative input pipeline).
        // Without this, Character will not receive movement even if PlayerInput action map is correct.
        if (PlayerNetworkInput.LocalInstance != null)
        {
            input.Set(PlayerNetworkInput.LocalInstance.GetLocalInput());
        }
    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}

