using System;
using System.Collections;
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

    [Header("Join near host (server only, remote clients)")]
    [Tooltip("Random horizontal offset radius (m) around host when a client joins after return-point logic did not apply.")]
    [SerializeField] private float hostJoinNearRadius = 4f;
    [SerializeField, Min(4)] private int hostJoinMaxAttempts = 24;
    [SerializeField, Min(0f)] private float hostJoinMinHorizontalSeparation = 0.35f;
    [Tooltip("Capsule used for Physics.CheckCapsule (approximate player).")]
    [SerializeField] private float spawnCapsuleHeight = 1.75f;
    [SerializeField] private float spawnCapsuleRadius = 0.32f;
    [Tooltip("Layers treated as blocking spawn (walls, props). If None (0), only host separation + ground ray are used.")]
    [SerializeField] private LayerMask spawnObstacleLayers;
    [Tooltip("Ground for downward ray. If None (0), uses default raycast layers.")]
    [SerializeField] private LayerMask spawnGroundLayers;
    [SerializeField] private float groundRayStartHeight = 18f;
    [SerializeField] private float groundRayMaxDistance = 48f;
    [SerializeField] private float spawnFeetYOffsetFromGround = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = true;
    
    [Header("Loading Screen")]
    [Tooltip("Giữ loading tối thiểu để người chơi kịp nhìn thấy (unscaled seconds).")]
    [SerializeField] private float minLoadingSeconds = 0.75f;
    
    [Tooltip("Thời gian tối đa chờ camera local ready trước khi tắt loading (unscaled seconds).")]
    [SerializeField] private float cameraWaitTimeoutSeconds = 2.5f;
    
    [Tooltip("Nếu true: chỉ tắt loading sau khi thấy Player NetworkObject (Player_3.0(Clone)) xuất hiện trong scene.")]
    [SerializeField] private bool requireLocalPlayerObjectPresentToFinishLoading = true;
    private Coroutine _finishLoadingRoutine;
    private Coroutine _retryFinishLoadingRoutine;
    private bool _mainMapSceneLoadHandled;
    private bool _spawnMissingInProgress;

    /// <param name="usedReturnPoint">True when dungeon / return flow consumed <see cref="PlayerWorldData"/> return position.</param>
    Vector3 ResolveSpawnPosition(NetworkRunner runner, PlayerRef player, out bool usedReturnPoint)
    {
        usedReturnPoint = false;

        // 1) Return point (preferred)
        if (useReturnPoint && PlayerWorldData.HasReturnPoint)
        {
            usedReturnPoint = true;
            Vector3 p = PlayerWorldData.ReturnPosition + Vector3.up * 1.5f;
            PlayerWorldData.HasReturnPoint = false;
            return p;
        }

        // 2) Remote client joining: spawn near host with clearance + overlap retries (host position re-read every attempt)
        if (runner != null && runner.IsServer && player != runner.LocalPlayer &&
            TryGetNearHostSpawnPosition(runner, out Vector3 nearHost))
        {
            if (verboseLogs)
                Debug.Log($"[PlayerSpawner] Spawn near host for player={player} at {nearHost}");
            return nearHost;
        }

        // 3) Explicit default spawn point
        if (defaultSpawnPoint != null)
        {
            return defaultSpawnPoint.position + Vector3.up * 1.5f;
        }

        // 4) Any object tagged SpawnPoint
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int index = player.PlayerId % spawnPoints.Length;
            return spawnPoints[index].transform.position + Vector3.up * 1.5f;
        }

        return fallbackSpawnPos;
    }

    static int GroundMaskOrDefault(LayerMask mask) => mask.value != 0 ? mask.value : Physics.DefaultRaycastLayers;

    bool TryGetNearHostSpawnPosition(NetworkRunner runner, out Vector3 spawnPosition)
    {
        spawnPosition = default;
        if (runner == null || !runner.IsServer)
            return false;

        NetworkObject hostObj;
        try
        {
            hostObj = runner.GetPlayerObject(runner.LocalPlayer);
        }
        catch
        {
            return false;
        }

        if (hostObj == null)
            return false;

        var hostCc = hostObj.GetComponentInChildren<CharacterController>(true);
        int groundMask = GroundMaskOrDefault(spawnGroundLayers);
        int attempts = Mathf.Max(4, hostJoinMaxAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector3 hostRoot = hostObj.transform.position;
            Vector3 hostCenter = hostCc != null
                ? hostObj.transform.TransformPoint(hostCc.center)
                : hostRoot + Vector3.up * (spawnCapsuleHeight * 0.5f);
            float hostRadius = hostCc != null ? hostCc.radius : 0.35f;

            Vector2 disk = UnityEngine.Random.insideUnitCircle * hostJoinNearRadius;
            Vector3 planar = new Vector3(hostRoot.x + disk.x, hostRoot.y, hostRoot.z + disk.y);

            float feetY = hostRoot.y;
            var ray = new Ray(planar + Vector3.up * groundRayStartHeight, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, groundRayMaxDistance, groundMask, QueryTriggerInteraction.Ignore))
                feetY = hit.point.y + spawnFeetYOffsetFromGround;

            Vector3 feet = new Vector3(planar.x, feetY, planar.z);

            Vector3 pBottom = feet + Vector3.up * spawnCapsuleRadius;
            Vector3 pTop = feet + Vector3.up * (spawnCapsuleHeight - spawnCapsuleRadius);

            Vector2 delta = new Vector2(feet.x - hostCenter.x, feet.z - hostCenter.z);
            float minHoriz = hostRadius + spawnCapsuleRadius + hostJoinMinHorizontalSeparation;
            if (delta.magnitude < minHoriz)
                continue;

            if (spawnObstacleLayers.value != 0)
            {
                if (Physics.CheckCapsule(pBottom, pTop, spawnCapsuleRadius, spawnObstacleLayers, QueryTriggerInteraction.Ignore))
                    continue;
            }

            Vector3 spawnMid = 0.5f * (pBottom + pTop);
            if (!HasClearanceFromHostColliders(hostObj, spawnMid, spawnCapsuleRadius + 0.06f))
                continue;

            spawnPosition = feet;
            return true;
        }

        return false;
    }

    /// <summary>True if <paramref name="worldPoint"/> is not inside / tight against any enabled non-trigger collider on the host hierarchy.</summary>
    static bool HasClearanceFromHostColliders(NetworkObject hostObj, Vector3 worldPoint, float clearanceRadius)
    {
        float minDistSq = clearanceRadius * clearanceRadius;
        foreach (var col in hostObj.GetComponentsInChildren<Collider>(true))
        {
            if (col == null || !col.enabled || col.isTrigger)
                continue;
            Vector3 cp = col.ClosestPoint(worldPoint);
            if ((cp - worldPoint).sqrMagnitude < minDistSq)
                return false;
        }

        return true;
    }

    /// <summary>Re-evaluate near-host spawn with fresh host transform immediately before <see cref="NetworkRunner.Spawn"/> (host may have moved while client was loading).</summary>
    void TryRefreshNearHostSpawnBeforeSpawn(NetworkRunner runner, PlayerRef player, bool usedReturnPoint, ref Vector3 spawnPos)
    {
        if (usedReturnPoint)
            return;
        if (runner == null || !runner.IsServer || player == runner.LocalPlayer)
            return;
        if (!TryGetNearHostSpawnPosition(runner, out var refreshed))
            return;
        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] Pre-spawn refresh near-host for player={player}: {spawnPos} -> {refreshed}");
        spawnPos = refreshed;
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

        EnsureLootBroadcasterSpawned(runner);
        EnsurePlayerSpawned(runner, player, "OnPlayerJoined");
    }

    void EnsureLootBroadcasterSpawned(NetworkRunner runner)
    {
        if (_broadcasterSpawned || !lootBroadcasterPrefab.IsValid || runner == null || !runner.IsServer)
            return;

        runner.Spawn(lootBroadcasterPrefab, Vector3.zero, Quaternion.identity);
        _broadcasterSpawned = true;
    }

    NetworkObject EnsurePlayerSpawned(NetworkRunner runner, PlayerRef player, string reason)
    {
        if (runner == null || !runner.IsServer || !player.IsRealPlayer || !playerPrefab.IsValid)
            return null;

        if (TryGetLivePlayerObject(runner, player, out var existing))
        {
            if (verboseLogs)
                Debug.Log($"[PlayerSpawner] EnsurePlayerSpawned skip ({reason}) player={player} existing={existing.name}");
            return existing;
        }

        Vector3 spawnPos = ResolveSpawnPosition(runner, player, out bool usedReturnPoint);
        TryRefreshNearHostSpawnBeforeSpawn(runner, player, usedReturnPoint, ref spawnPos);
        var obj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        if (obj == null)
        {
            Debug.LogWarning($"[PlayerSpawner] EnsurePlayerSpawned FAILED ({reason}) player={player}");
            return null;
        }

        try
        {
            runner.SetPlayerObject(player, obj);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayerSpawner] EnsurePlayerSpawned SetPlayerObject failed ({reason}) player={player}: {ex.Message}");
        }

        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] EnsurePlayerSpawned ({reason}) player={player} obj={obj.name} pos={spawnPos}");

        PostSpawnOnServer(runner, obj, player);
        return obj;
    }

    static bool TryGetLivePlayerObject(NetworkRunner runner, PlayerRef player, out NetworkObject existing)
    {
        existing = null;
        if (runner == null || !player.IsRealPlayer)
            return false;

        try
        {
            existing = runner.GetPlayerObject(player);
        }
        catch
        {
            return false;
        }

        if (existing == null)
            return false;

        // After Fusion scene unload, GetPlayerObject can still reference a despawned object.
        if (!existing.IsValid)
        {
            existing = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Called when Map_Chinh is ready after Fusion scene load (return from dungeon or bootstrap).
    /// Server respawns all peers; every machine dismisses networking loading when not in a dungeon scene.
    /// </summary>
    public void HandleMainMapSceneLoaded(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (_mainMapSceneLoadHandled)
        {
            if (verboseLogs)
                Debug.Log("[PlayerSpawner] HandleMainMapSceneLoaded skipped (already handled this scene load).");
            return;
        }

        _mainMapSceneLoadHandled = true;

        string scene = SceneManager.GetActiveScene().name;
        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] HandleMainMapSceneLoaded scene={scene} isServer={runner.IsServer} local={runner.LocalPlayer}");

        if (runner.IsServer)
            SpawnMissingForAllActivePlayers(runner);

        if (DungeonWaveManager.Instance != null)
        {
            if (verboseLogs)
                Debug.Log("[PlayerSpawner] HandleMainMapSceneLoaded: DungeonWaveManager still active -> skip FinishLoading (dungeon flow).");
            return;
        }

        StartFinishLoadingRoutine(runner, "HandleMainMapSceneLoaded");

        if (_retryFinishLoadingRoutine != null)
            StopCoroutine(_retryFinishLoadingRoutine);
        _retryFinishLoadingRoutine = StartCoroutine(RetryFinishLoadingAfterReturn(runner));
    }

    /// <summary>Backup if <see cref="OnSceneLoadDone"/> ran before this component registered callbacks.</summary>
    public void EnsureMainMapReadyDeferred(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return;
        StartCoroutine(EnsureMainMapReadyDeferredRoutine(runner));
    }

    IEnumerator EnsureMainMapReadyDeferredRoutine(NetworkRunner runner)
    {
        yield return null;
        if (runner == null || !runner.IsRunning)
            yield break;
        HandleMainMapSceneLoaded(runner);
    }

    IEnumerator RetryFinishLoadingAfterReturn(NetworkRunner runner)
    {
        const int attempts = 5;
        for (int i = 0; i < attempts; i++)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            if (runner == null || !runner.IsRunning)
                yield break;
            if (DungeonWaveManager.Instance != null)
                yield break;

            if (runner.IsServer && !TryGetLivePlayerObject(runner, runner.LocalPlayer, out _))
                SpawnMissingForAllActivePlayers(runner);

            StartFinishLoadingRoutine(runner, $"RetryFinish#{i}");
        }

        _retryFinishLoadingRoutine = null;
    }

    void SpawnMissingForAllActivePlayers(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning || !runner.IsServer)
            return;

        if (_spawnMissingInProgress)
        {
            if (verboseLogs)
                Debug.Log("[PlayerSpawner] SpawnMissingForAllActivePlayers skipped (already in progress).");
            return;
        }

        _spawnMissingInProgress = true;
        try
        {
            EnsureLootBroadcasterSpawned(runner);

            foreach (PlayerRef player in runner.ActivePlayers)
                EnsurePlayerSpawned(runner, player, "SpawnMissingForAllActivePlayers");
        }
        finally
        {
            _spawnMissingInProgress = false;
        }
    }

    /// <summary>
    /// CharacterController and PlayerInput are configured in <see cref="PlayerNetworkSetup.Spawned"/>.
    /// Here we only run loading UI completion for <see cref="NetworkRunner.LocalPlayer"/> on this machine.
    /// </summary>
    void PostSpawnOnServer(NetworkRunner runner, NetworkObject netObj, PlayerRef player)
    {
        if (netObj == null) return;

        if (player != runner.LocalPlayer)
            return;

        if (verboseLogs) Debug.Log("[PlayerSpawner] Post-spawn loading UI (local player on server).");
        StartFinishLoadingRoutine(runner, "PostSpawnOnServer");
    }

    void StartFinishLoadingRoutine(NetworkRunner runner, string reason)
    {
        if (runner == null || !runner.IsRunning)
            return;

        if (_finishLoadingRoutine != null)
            StopCoroutine(_finishLoadingRoutine);

        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] StartFinishLoadingRoutine reason={reason} scene={SceneManager.GetActiveScene().name}");
        _finishLoadingRoutine = StartCoroutine(FinishLoadingWhenReady(runner));
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
                playerObjReady = TryGetLivePlayerObject(runner, runner.LocalPlayer, out _);
            }

            CinemachineCamera cam = PlayerNetworkSetup.LocalCinemachineCamera;
            bool camReady = (cam != null);

            if (playerObjReady && camReady)
                break;
            yield return null;
        }

        if (verboseLogs)
        {
            bool hasPlayer = TryGetLivePlayerObject(runner, runner.LocalPlayer, out var po);
            Debug.Log($"[PlayerSpawner] FinishLoadingWhenReady exit-wait hasPlayer={hasPlayer} player={(po != null ? po.name : "null")} cam={(PlayerNetworkSetup.LocalCinemachineCamera != null)} scene={SceneManager.GetActiveScene().name}");
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

        _finishLoadingRoutine = null;
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
        if (TryGetLivePlayerObject(runner, runner.LocalPlayer, out _))
            return;

        EnsureLootBroadcasterSpawned(runner);
        var obj = EnsurePlayerSpawned(runner, runner.LocalPlayer, "EnsureLocalPlayerSpawned");
        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] EnsureLocalPlayerSpawned result={(obj != null ? obj.name : "null")}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || !runner.IsServer)
            return;

        NetworkObject playerObj = null;
        try
        {
            playerObj = runner.GetPlayerObject(player);
        }
        catch (Exception ex)
        {
            if (verboseLogs)
                Debug.LogWarning($"[PlayerSpawner] OnPlayerLeft GetPlayerObject exception player={player}: {ex.Message}");
        }

        if (playerObj == null)
        {
            if (verboseLogs)
                Debug.Log($"[PlayerSpawner] OnPlayerLeft player={player} — no PlayerObject (already despawned or never set).");
            return;
        }

        if (verboseLogs)
            Debug.Log($"[PlayerSpawner] OnPlayerLeft player={player} → Despawn {playerObj.name}");

        try
        {
            runner.Despawn(playerObj);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[PlayerSpawner] OnPlayerLeft Despawn failed player={player}: {ex.Message}");
        }
    }
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
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return;

        // Only main-map spawner should handle Map_Chinh return; dungeon uses DungeonPlayerSpawner.
        if (DungeonWaveManager.Instance != null)
        {
            if (verboseLogs)
                Debug.Log("[PlayerSpawner] OnSceneLoadDone in dungeon scene -> loading managed by DungeonWaveManager flow.");
            return;
        }

        HandleMainMapSceneLoaded(runner);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        _mainMapSceneLoadHandled = false;
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}

