using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Hiện/ẩn panel invite qua Custom Messaging — không phụ thuộc ClientRpc trên NetworkObject lobby
/// (tránh trường hợp client không replicate/spawn object đó nên không nhận RPC).
/// </summary>
public static class DungeonLobbyInviteNetwork
{
    const string MsgInvite = "DungeonLobbyInviteUi";
    const string MsgHide = "DungeonLobbyInviteHide";

    static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.CustomMessagingManager == null) return;

        nm.CustomMessagingManager.RegisterNamedMessageHandler(MsgInvite, OnInviteMessage);
        nm.CustomMessagingManager.RegisterNamedMessageHandler(MsgHide, OnHideMessage);
        _registered = true;
        Debug.Log("[DungeonLobbyInvite] Handlers registered.");
    }

    static void OnInviteMessage(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out ulong initiatorClientId);
        if (NetworkManager.Singleton == null) return;
        bool show = NetworkManager.Singleton.LocalClientId != initiatorClientId;
        DungeonInviteJoinController.SetInvitePanelVisible(show);
        Debug.Log($"[DungeonLobbyInvite] recv initiator={initiatorClientId} local={NetworkManager.Singleton.LocalClientId} show={show}");
    }

    static void OnHideMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (reader.Length > 0)
            reader.ReadValueSafe(out byte _);
        DungeonInviteJoinController.SetInvitePanelVisible(false);
        Debug.Log("[DungeonLobbyInvite] recv hide all");
    }

    /// <summary>Chỉ gọi trên server.</summary>
    public static void ServerSendInviteToAllClients(ulong initiatorClientId)
    {
        EnsureRegistered();
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        foreach (var kv in nm.ConnectedClients)
        {
            ulong clientId = kv.Key;
            using (var writer = new FastBufferWriter(sizeof(ulong), Allocator.Temp))
            {
                writer.WriteValueSafe(initiatorClientId);
                nm.CustomMessagingManager.SendNamedMessage(MsgInvite, clientId, writer, NetworkDelivery.Reliable);
            }
        }

        Debug.Log($"[DungeonLobbyInvite] server sent invite to all peers initiator={initiatorClientId}");
    }

    /// <summary>Chỉ gọi trên server.</summary>
    public static void ServerHideInviteAllClients()
    {
        EnsureRegistered();
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        foreach (var kv in nm.ConnectedClients)
        {
            ulong clientId = kv.Key;
            using (var writer = new FastBufferWriter(1, Allocator.Temp))
            {
                writer.WriteValueSafe((byte)0);
                nm.CustomMessagingManager.SendNamedMessage(MsgHide, clientId, writer, NetworkDelivery.Reliable);
            }
        }
    }
}
