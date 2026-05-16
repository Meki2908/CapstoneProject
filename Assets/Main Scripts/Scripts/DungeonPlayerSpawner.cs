using Fusion;
using UnityEngine;
using System.Collections;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

public class DungeonPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("=== Cấu hình Spawn ===")]
    public NetworkPrefabRef playerPrefab;
    public Transform spawnPoint;
    [SerializeField, Min(0f)] private float spawnRadiusAroundPoint = 2.5f;
    [SerializeField, Min(1)] private int spawnRingSteps = 3;
    [SerializeField, Min(0.1f)] private float minPlayerSeparation = 1.5f;
    [SerializeField, Min(0.1f)] private float ringStepRadius = 1.1f;
    [SerializeField] private bool verboseLogs = true;

    private NetworkRunner _runner;
    private bool _callbacksRegistered;

    private IEnumerator Start()
    {
        NetworkRunner runner = null;
        yield return new WaitUntil(() =>
        {
            runner = FindFirstObjectByType<NetworkRunner>();
            return runner != null && runner.IsRunning;
        });

        // Give one more frame so callbacks already added elsewhere settle.
        yield return null;

        RegisterRunnerCallbacks(runner);

        if (runner.IsServer)
            SpawnMissingForAllActivePlayers(runner);
    }

    private void OnDestroy()
    {
        if (_runner != null && _callbacksRegistered)
        {
            _runner.RemoveCallbacks(this);
            _callbacksRegistered = false;
        }
    }

    void RegisterRunnerCallbacks(NetworkRunner runner)
    {
        if (runner == null)
            return;

        if (_runner == runner && _callbacksRegistered)
            return;

        if (_runner != null && _callbacksRegistered)
            _runner.RemoveCallbacks(this);

        _runner = runner;
        _runner.AddCallbacks(this);
        _callbacksRegistered = true;

        if (verboseLogs)
            Debug.Log($"[DungeonPlayerSpawner] Registered callbacks on runner='{runner.name}' isServer={runner.IsServer}");
    }

    Vector3 ResolveSpawnPosition(NetworkRunner runner, PlayerRef player)
    {
        Transform anchor = ResolveSpawnAnchor(player);
        Vector3 center = anchor != null ? anchor.position : Vector3.zero;
        float baseRadius = Mathf.Max(0f, spawnRadiusAroundPoint);

        for (int ring = 0; ring < spawnRingSteps; ring++)
        {
            float ringRadius = baseRadius + ring * Mathf.Max(0.1f, ringStepRadius);
            Vector3 candidate = center + DeterministicOffset(player, ringRadius, ring);
            if (HasEnoughSeparation(runner, candidate))
                return candidate;
        }

        if (verboseLogs)
            Debug.LogWarning($"[DungeonPlayerSpawner] No clear spaced point found, fallback to center for player={player}");
        return center;
    }

    Transform ResolveSpawnAnchor(PlayerRef player)
    {
        if (spawnPoint != null)
            return spawnPoint;

        GameObject[] taggedSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (taggedSpawnPoints != null && taggedSpawnPoints.Length > 0)
        {
            int index = Mathf.Abs(player.PlayerId) % taggedSpawnPoints.Length;
            return taggedSpawnPoints[index].transform;
        }

        return null;
    }

    Vector3 DeterministicOffset(PlayerRef player, float radius, int ring)
    {
        if (radius <= 0f)
            return Vector3.zero;

        // Golden-angle style spacing to avoid players stacking on same side of spawn point.
        float seed = Mathf.Abs(player.PlayerId) * 137.50776f + ring * 53.0f;
        float angle = seed * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
    }

    bool HasEnoughSeparation(NetworkRunner runner, Vector3 candidate)
    {
        if (runner == null)
            return true;

        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            NetworkObject existing = null;
            try
            {
                existing = runner.GetPlayerObject(activePlayer);
            }
            catch
            {
                // ignored
            }

            if (existing == null)
                continue;

            Vector3 delta = existing.transform.position - candidate;
            delta.y = 0f;
            if (delta.sqrMagnitude < minPlayerSeparation * minPlayerSeparation)
                return false;
        }

        return true;
    }

    Quaternion ResolveSpawnRotation(PlayerRef player)
    {
        Transform anchor = ResolveSpawnAnchor(player);
        return anchor != null ? anchor.rotation : Quaternion.identity;
    }

    void SpawnMissingForAllActivePlayers(NetworkRunner runner)
    {
        if (runner == null || !runner.IsServer)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
            EnsurePlayerSpawned(runner, player);
    }

    NetworkObject EnsurePlayerSpawned(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null || !runner.IsServer || !playerPrefab.IsValid || !player.IsRealPlayer)
            return null;

        NetworkObject existing = null;
        try
        {
            existing = runner.GetPlayerObject(player);
        }
        catch (Exception ex)
        {
            if (verboseLogs)
                Debug.LogWarning($"[DungeonPlayerSpawner] GetPlayerObject exception player={player}: {ex.Message}");
        }

        if (existing != null)
            return existing;

        Vector3 pos = ResolveSpawnPosition(runner, player);
        Quaternion rot = ResolveSpawnRotation(player);
        NetworkObject obj = runner.Spawn(playerPrefab, pos, rot, player);
        if (obj == null)
        {
            Debug.LogError($"[DungeonPlayerSpawner] Spawn FAILED for player={player}");
            return null;
        }

        try
        {
            runner.SetPlayerObject(player, obj);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DungeonPlayerSpawner] SetPlayerObject failed player={player}: {ex.Message}");
        }

        if (verboseLogs)
            Debug.Log($"[DungeonPlayerSpawner] Spawned player={player} obj={obj.name} pos={pos}");

        // Keep legacy dungeon wave compatibility: ensure a tracked target exists.
        var waveMgr = FindFirstObjectByType<DungeonWaveManager>();
        if (waveMgr != null && (waveMgr.player == null || player == runner.LocalPlayer))
        {
            waveMgr.player = obj.transform;
            EnsureLegacyLowercasePlayerMarker(obj.transform);
        }

        return obj;
    }

    static void EnsureLegacyLowercasePlayerMarker(Transform playerRoot)
    {
        if (playerRoot == null)
            return;

        Transform existingMarker = playerRoot.Find("player");
        if (existingMarker != null)
            return;

        GameObject marker = new GameObject("player");
        marker.transform.SetParent(playerRoot, false);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        RegisterRunnerCallbacks(runner);
        EnsurePlayerSpawned(runner, player);
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
                Debug.LogWarning($"[DungeonPlayerSpawner] OnPlayerLeft GetPlayerObject exception player={player}: {ex.Message}");
        }

        if (playerObj == null)
            return;

        try
        {
            runner.Despawn(playerObj);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DungeonPlayerSpawner] OnPlayerLeft Despawn failed player={player}: {ex.Message}");
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        RegisterRunnerCallbacks(runner);
        if (runner == null || !runner.IsRunning)
            return;

        if (runner.IsServer)
            SpawnMissingForAllActivePlayers(runner);
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        RegisterRunnerCallbacks(runner);
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
