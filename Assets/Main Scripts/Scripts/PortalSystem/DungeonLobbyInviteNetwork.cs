using UnityEngine;

/// <summary>
/// NGO custom messaging đã gỡ — chừa API rỗng cho UI không lỗi.
/// </summary>
public static class DungeonLobbyInviteNetwork
{
    public static void EnsureRegistered() { }

    public static void ServerSendInviteToAllClients(ulong initiatorClientId) { }

    public static void ServerHideInviteAllClients() { }
}
