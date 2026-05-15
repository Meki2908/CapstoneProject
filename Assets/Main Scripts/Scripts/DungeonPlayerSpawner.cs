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

    Vector3 ResolveSpawnPosition(PlayerRef player)
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        GameObject[] taggedSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        if (taggedSpawnPoints != null && taggedSpawnPoints.Length > 0)
        {
            int index = Mathf.Abs(player.PlayerId) % taggedSpawnPoints.Length;
            return taggedSpawnPoints[index].transform.position;
        }

        return Vector3.zero;
    }

    Quaternion ResolveSpawnRotation()
    {
        return spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
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

        Vector3 pos = ResolveSpawnPosition(player);
        Quaternion rot = ResolveSpawnRotation();
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
