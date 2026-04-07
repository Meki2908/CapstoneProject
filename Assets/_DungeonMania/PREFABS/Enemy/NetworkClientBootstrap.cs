using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Gắn cùng GameObject có <see cref="NetworkManager"/> trong scene gameplay.
/// Khi vào từ menu (cờ pending client), gọi <see cref="NetworkManager.StartClient"/> sau khi load.
///
/// --- RELAY ---
/// Nếu có pending relay data → áp dụng lên UnityTransport trước StartClient.
/// Nếu không (LAN) → giữ logic IP/Port cũ.
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

        // Thử áp dụng Relay data trước (internet)
        bool usedRelay = RelayManager.ApplyPendingRelayData(NetworkManager.Singleton);

        if (!usedRelay)
        {
            // LAN: giữ logic IP/Port cũ
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
                port = pendingPort;
                Debug.Log($"[NetworkClientBootstrap] Join: {addr}:{port} (cổng cố định từ MultiplayerManager).");
            }
            else
            {
                port = ParrelSyncTransportPort.GetClientConnectPortWithoutCloneOffset(utp);
                Debug.Log($"[NetworkClientBootstrap] Join: {addr}:{port} (cổng scene; LAN).");
            }

            utp.SetConnectionData(addr, port, cd.ServerListenAddress);
        }

        if (!NetworkManager.Singleton.StartClient())
            Debug.LogError("[NetworkClientBootstrap] StartClient() thất bại.");
        else
            Debug.Log($"[NetworkClientBootstrap] StartClient() thành công. Relay={usedRelay}");
    }
}
