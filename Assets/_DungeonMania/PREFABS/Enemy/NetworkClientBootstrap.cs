using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Gắn cùng GameObject có <see cref="NetworkManager"/> trong scene gameplay.
/// Khi vào từ menu (cờ pending client), gọi <see cref="NetworkManager.StartClient"/> sau khi load.
/// </summary>
[DefaultExecutionOrder(110)]
public class NetworkClientBootstrap : MonoBehaviour
{
    private void Start()
    {
        if (!MultiplayerManager.PendingStartClientAfterSceneLoad)
            return;

        MultiplayerManager.ClearPendingClientFlag();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkClientBootstrap] Không tìm thấy NetworkManager.Singleton.");
            return;
        }

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            return;

        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (utp == null)
        {
            Debug.LogError("[NetworkClientBootstrap] Không có UnityTransport.");
            return;
        }

        var cd = utp.ConnectionData;
        string addr = MultiplayerManager.PendingConnectAddress;
        if (string.IsNullOrWhiteSpace(addr))
            addr = "127.0.0.1";

        ushort port;
        ushort pendingPort = MultiplayerManager.PendingConnectPort;
        if (pendingPort > 0)
        {
            // Cổng cố định (vd. 7777 — join host Editor từ ParrelSync clone, không áp offset clone cho client).
            port = pendingPort;
            Debug.Log($"[NetworkClientBootstrap] Join: {addr}:{port} (cổng cố định từ MultiplayerManager).");
        }
        else
        {
            // Không gọi ApplyClonePortOffsetIfNeeded: offset chỉ cho host bind trên clone; client tới Editor = 7777.
            port = ParrelSyncTransportPort.GetClientConnectPortWithoutCloneOffset(utp);
            Debug.Log($"[NetworkClientBootstrap] Join: {addr}:{port} (cổng scene; join host clone cùng máy → đặt connectPort trong MultiplayerManager).");
        }

        utp.SetConnectionData(addr, port, cd.ServerListenAddress);

        if (!NetworkManager.Singleton.StartClient())
            Debug.LogError("[NetworkClientBootstrap] StartClient() thất bại.");
    }
}
