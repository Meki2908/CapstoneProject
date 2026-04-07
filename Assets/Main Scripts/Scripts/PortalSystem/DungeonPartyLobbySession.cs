using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gắn cùng GameObject có <see cref="NetworkObject"/> trong scene (vd. Map_Chinh).
/// Host chọn số người → server đếm host là 1/N, gửi invite (panel Join Dungeon) cho client;
/// mỗi lần client Accept thì tăng đếm; đủ N → đếm ngược 3s → load scene (trên mọi client).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class DungeonPartyLobbySession : NetworkBehaviour
{
    public static DungeonPartyLobbySession Instance { get; private set; }

    [SerializeField] float finalCountdownSeconds = 3f;
    [SerializeField] float lobbyInviteTimeoutSeconds = 30f;

    readonly HashSet<ulong> _acceptedClients = new HashSet<ulong>();
    int _maxPlayers;
    string _pendingSceneName;
    ulong _initiatorClientId;
    Coroutine _timeoutRoutine;
    Coroutine _finalClientRoutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Instance = this;
        DungeonLobbyInviteNetwork.EnsureRegistered();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Đảm bảo có session đã spawn trước khi host gọi ServerRpc (tránh Instance null / in-scene chưa spawn kịp).
    /// </summary>
    public static bool TryEnsureSessionForHost(out DungeonPartyLobbySession session)
    {
        session = null;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost)
            return false;

        if (Instance != null && Instance.IsSpawned)
        {
            session = Instance;
            return true;
        }

        var sessions = Object.FindObjectsByType<DungeonPartyLobbySession>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in sessions)
        {
            if (s == null) continue;
            var no = s.GetComponent<NetworkObject>();
            if (no == null) continue;

            if (no.IsSpawned)
            {
                Instance = s;
                session = s;
                return true;
            }
        }

        if (!NetworkManager.Singleton.IsServer)
            return false;

        foreach (var s in sessions)
        {
            if (s == null) continue;
            var no = s.GetComponent<NetworkObject>();
            if (no == null || no.IsSpawned) continue;
            try
            {
                no.Spawn();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DungeonPartyLobby] Spawn NetworkObject thất bại: " + ex.Message);
                continue;
            }
            if (no.IsSpawned)
            {
                Instance = s;
                session = s;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Lấy session cho mọi peer đang online.
    /// - Host: cho phép đảm bảo spawn in-scene object khi cần.
    /// - Client: chỉ cần bản replicate đã spawn.
    /// </summary>
    public static bool TryGetSessionForNetwork(out DungeonPartyLobbySession session)
    {
        session = null;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return false;

        if (NetworkManager.Singleton.IsHost)
            return TryEnsureSessionForHost(out session);

        if (Instance != null && Instance.IsSpawned)
        {
            session = Instance;
            return true;
        }

        var sessions = Object.FindObjectsByType<DungeonPartyLobbySession>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in sessions)
        {
            if (s != null && s.IsSpawned)
            {
                Instance = s;
                session = s;
                return true;
            }
        }
        return false;
    }

    /// <summary>Chỉ host gọi (từ DungeonPortalLobbyCoordinator sau khi chọn số người).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void StartDungeonLobbyServerRpc(int maxPlayers, int difficulty, int mapType, string sceneName, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (NetworkManager.Singleton == null) return;
        // Chỉ host (máy server) được tạo lobby từ UI. Không chặn theo SenderClientId —
        // NGO 2.x đôi khi giá trị sender không khớp ServerClientId dù gọi từ host.

        _maxPlayers = Mathf.Max(1, maxPlayers);
        _pendingSceneName = sceneName;
        _initiatorClientId = rpcParams.Receive.SenderClientId;
        _acceptedClients.Clear();
        _acceptedClients.Add(_initiatorClientId);
        DungeonConfig.SelectedDifficulty = (DungeonDifficulty)difficulty;
        DungeonConfig.SelectedMapType = mapType;
        Debug.Log($"[DungeonPartyLobby] Start lobby server: initiator={_initiatorClientId}, max={_maxPlayers}, scene={_pendingSceneName}");

        SyncLobbyUiClientRpc(1, _maxPlayers);
        DungeonLobbyInviteNetwork.ServerSendInviteToAllClients(_initiatorClientId);

        if (_acceptedClients.Count >= _maxPlayers)
        {
            DungeonLobbyInviteNetwork.ServerHideInviteAllClients();
            FinalCountdownClientRpc(_pendingSceneName, finalCountdownSeconds);
            return;
        }

        if (_timeoutRoutine != null)
            StopCoroutine(_timeoutRoutine);
        _timeoutRoutine = StartCoroutine(LobbyInviteTimeoutRoutine());
    }

    IEnumerator LobbyInviteTimeoutRoutine()
    {
        float t = 0f;
        while (t < lobbyInviteTimeoutSeconds)
        {
            if (_acceptedClients.Count >= _maxPlayers)
            {
                _timeoutRoutine = null;
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
        if (_acceptedClients.Count < _maxPlayers)
        {
            ResetLobbyServerState();
            LobbyCancelledClientRpc();
        }
        _timeoutRoutine = null;
    }

    void ResetLobbyServerState()
    {
        if (!IsServer) return;
        _acceptedClients.Clear();
        if (_timeoutRoutine != null)
        {
            StopCoroutine(_timeoutRoutine);
            _timeoutRoutine = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AcceptDungeonInviteServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong id = rpcParams.Receive.SenderClientId;
        if (id == NetworkManager.ServerClientId) return;
        if (_acceptedClients.Contains(id)) return;
        if (_acceptedClients.Count >= _maxPlayers) return;

        _acceptedClients.Add(id);
        int n = _acceptedClients.Count;
        SyncLobbyUiClientRpc(n, _maxPlayers);

        if (n >= _maxPlayers)
        {
            if (_timeoutRoutine != null)
            {
                StopCoroutine(_timeoutRoutine);
                _timeoutRoutine = null;
            }
            DungeonLobbyInviteNetwork.ServerHideInviteAllClients();
            FinalCountdownClientRpc(_pendingSceneName, finalCountdownSeconds);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeclineDungeonInviteServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong id = rpcParams.Receive.SenderClientId;
        if (id == NetworkManager.ServerClientId) return;
        ResetLobbyServerState();
        LobbyCancelledClientRpc();
    }

    [ClientRpc]
    void SyncLobbyUiClientRpc(int joined, int expected)
    {
        DungeonPortalLobbyCoordinator.ApplyJoinedExpectedToAll(joined, expected);
    }

    [ClientRpc]
    void FinalCountdownClientRpc(string sceneName, float seconds)
    {
        if (_finalClientRoutine != null)
            StopCoroutine(_finalClientRoutine);
        _finalClientRoutine = StartCoroutine(ClientFinalCountdown(sceneName, seconds));
    }

    IEnumerator ClientFinalCountdown(string sceneName, float seconds)
    {
        float t = seconds;
        while (t > 0f)
        {
            DungeonPortalLobbyCoordinator.ApplyCountdownTextToAllPreparation(Mathf.CeilToInt(t).ToString());
            t -= Time.deltaTime;
            yield return null;
        }
        DungeonPortalLobbyCoordinator.ApplyCountdownTextToAllPreparation("0");
        DungeonPortalLobbyCoordinator.LoadDungeonSceneStatic(sceneName);
        _finalClientRoutine = null;
    }

    [ClientRpc]
    void LobbyCancelledClientRpc()
    {
        DungeonPortalLobbyCoordinator.ApplyLobbyCancelledToAll();
    }
}
