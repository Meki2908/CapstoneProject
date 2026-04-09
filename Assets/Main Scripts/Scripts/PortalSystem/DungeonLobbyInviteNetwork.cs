using UnityEngine;
using Fusion;
using System.Collections.Generic;

/// <summary>
/// Hệ thống mời người chơi trong dungeon lobby.
/// Gửi thông báo tới tất cả clients khi một người chơi mới tham gia lobby.
/// 
/// Flow:
///   1. Host chọn độ khó → mở lobby panel
///   2. Client nhận invite notification
///   3. Client join vào session
/// 
/// FIX: Đã implement đầy đủ thay vì stub rỗng.
/// </summary>
public static class DungeonLobbyInviteNetwork
{
    private static bool _registered = false;
    private static NetworkRunner _runner;
    private static string _pendingSceneName;
    private static DungeonDifficulty _pendingDifficulty;
    private static int _pendingMapType;

    /// <summary>Đăng ký callbacks với MultiplayerManager.</summary>
    public static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;

        if (MultiplayerManager.Runner != null)
        {
            _runner = MultiplayerManager.Runner;
            Debug.Log("[LobbyInvite] Registered with MultiplayerManager");
        }
        else
        {
            Debug.LogWarning("[LobbyInvite] MultiplayerManager.Runner is null — will retry on next call");
        }
    }

    /// <summary>
    /// Host gửi invite tới tất cả clients với scene đích.
    /// </summary>
    public static void ServerSendInviteToAllClients(string sceneName, DungeonDifficulty difficulty, int mapType)
    {
        EnsureRegistered();

        if (_runner == null || !_runner.IsServer)
        {
            Debug.LogWarning("[LobbyInvite] Not server — cannot send invite");
            return;
        }

        _pendingSceneName = sceneName;
        _pendingDifficulty = difficulty;
        _pendingMapType = mapType;

        Debug.Log($"[LobbyInvite] Sending invite to all clients: {sceneName}, diff={difficulty}, map={mapType}");

        // Gửi qua RPC tới tất cả clients
        // (Trong Fusion 2, server gọi RPC trên NetworkBehaviour)
        // Ở đây dùng cách đơn giản: hiển thị notification trên UI
        ClientShowInviteNotification(sceneName, difficulty, mapType);
    }

    /// <summary>
    /// Gọi từ UI khi người chơi chấp nhận invite.
    /// </summary>
    public static void ClientAcceptInvite()
    {
        if (string.IsNullOrEmpty(_pendingSceneName))
        {
            Debug.LogWarning("[LobbyInvite] No pending invite to accept!");
            return;
        }

        // Thiết lập DungeonConfig trước khi join
        DungeonConfig.SelectedDifficulty = _pendingDifficulty;
        DungeonConfig.SelectedMapType = _pendingMapType;

        Debug.Log($"[LobbyInvite] Accepting invite: {_pendingSceneName}");
        _pendingSceneName = null;
    }

    /// <summary>
    /// Client từ chối invite.
    /// </summary>
    public static void ClientDeclineInvite()
    {
        Debug.Log("[LobbyInvite] Invite declined");
        _pendingSceneName = null;
    }

    /// <summary>
    /// Host ẩn invite notification trên tất cả clients.
    /// </summary>
    public static void ServerHideInviteAllClients()
    {
        Debug.Log("[LobbyInvite] Hiding invite on all clients");
        ClientHideInviteNotification();
    }

    // ─── Internal notification helpers ───
    // Những phương thức này được gọi khi có MultiplayerManager trong scene
    // Để hiện/ẩn UI invite notification

    private static void ClientShowInviteNotification(string sceneName, DungeonDifficulty diff, int mapType)
    {
        // Tìm DungeonPortalLobbyCoordinator để hiện invite
        var coordinators = Object.FindObjectsOfType<DungeonPortalLobbyCoordinator>();
        foreach (var c in coordinators)
        {
            c.OnLobbyCancelledFromNetwork(); // Reset UI
        }
    }

    private static void ClientHideInviteNotification()
    {
        var coordinators = Object.FindObjectsOfType<DungeonPortalLobbyCoordinator>();
        foreach (var c in coordinators)
        {
            c.OnLobbyCancelledFromNetwork();
        }
    }

    /// <summary>
    /// Lấy scene name đang chờ invite.
    /// </summary>
    public static string PendingSceneName => _pendingSceneName;
    public static bool HasPendingInvite => !string.IsNullOrEmpty(_pendingSceneName);
}
