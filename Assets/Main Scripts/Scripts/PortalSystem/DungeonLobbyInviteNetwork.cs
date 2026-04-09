using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System.Text;

public static class DungeonLobbyInviteNetwork
{
    private static bool _registered = false;
    private static NetworkRunner _runner;
    private static string _pendingSceneName;
    private static DungeonDifficulty _pendingDifficulty;
    private static int _pendingMapType;
    
    // Server state
    private static DungeonPortalLobbyCoordinator _serverCoordinator;
    private static int _expectedPlayers;
    private static Dictionary<PlayerRef, bool> _partyMembers = new Dictionary<PlayerRef, bool>();
    private static ReliableKey DUNGEON_INVITE_KEY = ReliableKey.FromInts(99, 100);
    private static ReliableKey DUNGEON_ACCEPT_KEY = ReliableKey.FromInts(99, 101);
    private static ReliableKey DUNGEON_CANCEL_KEY = ReliableKey.FromInts(99, 102);
    private static ReliableKey DUNGEON_SYNC_ROOM_KEY = ReliableKey.FromInts(99, 103);
    private static ReliableKey DUNGEON_SET_READY_KEY = ReliableKey.FromInts(99, 104);
    private static ReliableKey DUNGEON_LEAVE_ROOM_KEY = ReliableKey.FromInts(99, 105);

    // Client state
    private static bool _amIReady = false;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;

        if (MultiplayerManager.Instance != null && MultiplayerManager.Runner != null)
        {
            _runner = MultiplayerManager.Runner;
            Debug.Log("[LobbyInvite] Registered with MultiplayerManager");
            
            // Hook into unreliable/reliable data if needed, but MultiplayerManager handles the event.
            // We will hook directly into MultiplayerManager's OnReliableDataReceived safely.
        }
    }

    public static void ServerStartLobbyWaiting(DungeonPortalLobbyCoordinator coordinator, int maxPlayers)
    {
        _serverCoordinator = coordinator;
        _expectedPlayers = maxPlayers;
        _partyMembers.Clear();
        _partyMembers[MultiplayerManager.Runner.LocalPlayer] = true; // Host assumes implicitly ready
        
        UpdateServerLobbyUI();

        if (coordinator.startNowButton != null)
        {
            coordinator.startNowButton.gameObject.SetActive(true);
            coordinator.startNowButton.onClick.RemoveAllListeners();
            coordinator.startNowButton.onClick.AddListener(ServerForceStartDungeon);
        }
    }

    public static void ServerSendInviteToAllClients(string sceneName, DungeonDifficulty difficulty, int mapType)
    {
        EnsureRegistered();

        if (_runner == null || !_runner.IsServer) return;

        _pendingSceneName = sceneName;
        _pendingDifficulty = difficulty;
        _pendingMapType = mapType;

        string payload = $"{sceneName}|{(int)difficulty}|{mapType}";
        byte[] data = Encoding.UTF8.GetBytes(payload);

        foreach (var player in _runner.ActivePlayers)
        {
            if (player == _runner.LocalPlayer) continue;
            _runner.SendReliableDataToPlayer(player, DUNGEON_INVITE_KEY, data);
        }
    }

    public static void ClientAcceptInvite()
    {
        if (string.IsNullOrEmpty(_pendingSceneName)) return;

        DungeonConfig.SelectedDifficulty = _pendingDifficulty;
        DungeonConfig.SelectedMapType = _pendingMapType;

        ClientHideInviteNotification();

        // Send accept back to server
        byte[] data = Encoding.UTF8.GetBytes("YES");
        _runner.SendReliableDataToPlayer(PlayerRef.FromIndex(1), DUNGEON_ACCEPT_KEY, data); // Send to Host
    }

    public static void ClientToggleReady()
    {
        if (_runner == null) return;
        _amIReady = !_amIReady;
        byte[] data = Encoding.UTF8.GetBytes(_amIReady ? "READY" : "UNREADY");
        _runner.SendReliableDataToPlayer(PlayerRef.FromIndex(1), DUNGEON_SET_READY_KEY, data);
    }

    public static void ClientLeaveRoom()
    {
        if (_runner == null) return;
        byte[] data = new byte[1];
        _runner.SendReliableDataToPlayer(PlayerRef.FromIndex(1), DUNGEON_LEAVE_ROOM_KEY, data);
        
        var coordinators = Object.FindObjectsByType<DungeonPortalLobbyCoordinator>(FindObjectsSortMode.None);
        foreach (var c in coordinators)
        {
            if (c.partyReadyRoomPanel != null) c.partyReadyRoomPanel.SetActive(false);
            if (c.difficultyButtonsRoot != null) c.difficultyButtonsRoot.SetActive(true);
        }
    }

    public static void ClientDeclineInvite()
    {
        _pendingSceneName = null;
        ClientHideInviteNotification();
    }

    public static void ServerHideInviteAllClients()
    {
        if (_runner == null || !_runner.IsServer) return;
        byte[] data = new byte[1];
        foreach (var p in _runner.ActivePlayers)
        {
            if (p != _runner.LocalPlayer) _runner.SendReliableDataToPlayer(p, DUNGEON_CANCEL_KEY, data);
        }
    }

    private static void UpdateServerLobbyUI()
    {
        BroadcastRoomStateToClients();

        if (_serverCoordinator != null)
        {
            _serverCoordinator.SetJoinedExpected(_partyMembers.Count, _expectedPlayers);
            // Rebuild host UI list
            RebuildLobbyListUI(_serverCoordinator, _partyMembers);

            // Check if all ready
            bool allReady = true;
            foreach (var kvp in _partyMembers)
            {
                if (!kvp.Value) allReady = false;
            }

            if (_partyMembers.Count >= _expectedPlayers && allReady)
            {
                ServerForceStartDungeon();
            }
        }
    }

    private static void BroadcastRoomStateToClients()
    {
        if (_runner == null || !_runner.IsServer) return;

        // payload format: "P1:1|P2:0|P3:1"
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in _partyMembers)
        {
            sb.Append($"{kvp.Key.PlayerId}:{(kvp.Value ? 1 : 0)}|");
        }
        string payload = sb.ToString().TrimEnd('|');
        byte[] data = Encoding.UTF8.GetBytes(payload);

        foreach (var p in _runner.ActivePlayers)
        {
            _runner.SendReliableDataToPlayer(p, DUNGEON_SYNC_ROOM_KEY, data);
        }
    }

    private static void ServerForceStartDungeon()
    {
        if (_serverCoordinator == null) return;
        if (_partyMembers.Count < 2)
        {
            Debug.LogWarning("[LobbyInvite] Minimum 2 players required for multiplayer dungeon!");
            if (_serverCoordinator.countdownText != null)
                _serverCoordinator.countdownText.text = "Cần tối thiểu 2 người!";
            return;
        }

        ServerHideInviteAllClients();
        _serverCoordinator.StartNetworkFinalCountdown(_pendingSceneName, 5f); // 5 sec countdown
        _serverCoordinator = null;
    }

    // Handles the Data received physically inside MultiplayerManager
    public static void HandleReliableData(NetworkRunner runner, PlayerRef player, ReliableKey key, byte[] data)
    {
        if (key.Equals(DUNGEON_INVITE_KEY))
        {
            if (runner.IsServer) return; // Only clients receive this
            string payload = Encoding.UTF8.GetString(data);
            var parts = payload.Split('|');
            if (parts.Length == 3)
            {
                _pendingSceneName = parts[0];
                _pendingDifficulty = (DungeonDifficulty)int.Parse(parts[1]);
                _pendingMapType = int.Parse(parts[2]);
                ClientShowInviteNotification(_pendingSceneName, _pendingDifficulty, _pendingMapType);
            }
        }
        else if (key.Equals(DUNGEON_ACCEPT_KEY))
        {
            if (!runner.IsServer) return; // Only host receives this
            _partyMembers[player] = false; // Add player as NOT ready
            UpdateServerLobbyUI();
        }
        else if (key.Equals(DUNGEON_CANCEL_KEY))
        {
            if (runner.IsServer) return;
            ClientHideInviteNotification();
        }
        else if (key.Equals(DUNGEON_SET_READY_KEY))
        {
            if (!runner.IsServer) return;
            string state = Encoding.UTF8.GetString(data);
            if (_partyMembers.ContainsKey(player))
            {
                _partyMembers[player] = (state == "READY");
                UpdateServerLobbyUI();
            }
        }
        else if (key.Equals(DUNGEON_LEAVE_ROOM_KEY))
        {
            if (!runner.IsServer) return;
            if (_partyMembers.ContainsKey(player))
            {
                _partyMembers.Remove(player);
                UpdateServerLobbyUI();
            }
        }
        else if (key.Equals(DUNGEON_SYNC_ROOM_KEY))
        {
            string payload = Encoding.UTF8.GetString(data);
            var parts = payload.Split('|');
            Dictionary<PlayerRef, bool> parsedList = new Dictionary<PlayerRef, bool>();
            foreach (var p in parts)
            {
                var kv = p.Split(':');
                if (kv.Length == 2)
                {
                    int pId = int.Parse(kv[0]);
                    bool ready = kv[1] == "1";
                    parsedList[PlayerRef.FromIndex(pId)] = ready;
                }
            }

            // Sync visual Room panel
            var coordinators = Object.FindObjectsByType<DungeonPortalLobbyCoordinator>(FindObjectsSortMode.None);
            foreach (var c in coordinators)
            {
                if (c.partyReadyRoomPanel != null && c.partyReadyRoomPanel.activeInHierarchy)
                {
                    RebuildLobbyListUI(c, parsedList);
                }
            }
        }
    }

    private static void ClientShowInviteNotification(string sceneName, DungeonDifficulty diff, int mapType)
    {
        var coordinators = Object.FindObjectsByType<DungeonPortalLobbyCoordinator>(FindObjectsSortMode.None);
        foreach (var c in coordinators)
        {
            if (c.inviteNotificationPanel != null)
            {
                c.inviteNotificationPanel.SetActive(true);
                if (c.inviteMessageText != null)
                    c.inviteMessageText.text = $"Host mời đánh Map {mapType} ({diff}). Tham gia?";
                
                if (c.inviteYesButton != null)
                {
                    c.inviteYesButton.onClick.RemoveAllListeners();
                    c.inviteYesButton.onClick.AddListener(ClientAcceptInvite);
                }
                if (c.inviteNoButton != null)
                {
                    c.inviteNoButton.onClick.RemoveAllListeners();
                    c.inviteNoButton.onClick.AddListener(ClientDeclineInvite);
                }
            }
        }
    }

    private static void RebuildLobbyListUI(DungeonPortalLobbyCoordinator coordinator, Dictionary<PlayerRef, bool> memberList)
    {
        if (coordinator.partyMemberListText != null)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var mem in memberList)
            {
                string pName = MultiplayerManager.Instance.GetPlayerName(mem.Key);
                string status = mem.Value ? "<color=green>Đã Sẵn Sàng</color>" : "<color=yellow>Đang Chờ...</color>";
                sb.AppendLine($"- {pName} : {status}");
            }
            coordinator.partyMemberListText.text = sb.ToString();
        }

        if (coordinator.partyReadyButton != null)
        {
            coordinator.partyReadyButton.onClick.RemoveAllListeners();
            coordinator.partyReadyButton.onClick.AddListener(ClientToggleReady);
            var img = coordinator.partyReadyButton.GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = _amIReady ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.2f, 0.6f, 1f); // Green if ready, Blue if not
        }

        if (coordinator.partyExitButton != null)
        {
            coordinator.partyExitButton.onClick.RemoveAllListeners();
            coordinator.partyExitButton.onClick.AddListener(ClientLeaveRoom);
        }
    }

    private static void ClientHideInviteNotification()
    {
        var coordinators = Object.FindObjectsByType<DungeonPortalLobbyCoordinator>(FindObjectsSortMode.None);
        foreach (var c in coordinators)
        {
            if (c.inviteNotificationPanel != null)
                c.inviteNotificationPanel.SetActive(false);
            
            // Show Party Room
            if (c.partyReadyRoomPanel != null && _pendingSceneName != null)
            {
                c.partyReadyRoomPanel.SetActive(true);
            }
        }
    }

    public static string PendingSceneName => _pendingSceneName;
    public static bool HasPendingInvite => !string.IsNullOrEmpty(_pendingSceneName);
}
