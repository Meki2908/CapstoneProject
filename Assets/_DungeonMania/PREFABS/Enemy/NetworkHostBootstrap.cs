using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Được scene gán trước khi StartHost; <see cref="NetworkPlayerSpawnSnap"/> đọc khi spawn.
/// </summary>
public static class PlayerSpawnConfig
{
    public static Transform SpawnPoint { get; set; }
}

/// <summary>
/// Gắn cùng GameObject có <see cref="NetworkManager"/> trong scene gameplay (vd. Testboss).
/// Khi vào từ menu (cờ pending), gọi <see cref="NetworkManager.StartHost"/> để spawn player prefab đã cấu hình.
/// </summary>
[DefaultExecutionOrder(100)]
public class NetworkHostBootstrap : MonoBehaviour
{
    [Tooltip("Kéo empty object trong scene — player host sẽ spawn đúng vị trí/góc này.")]
    [SerializeField] private Transform playerSpawnPoint;

    private void Start()
    {
        if (playerSpawnPoint != null)
            PlayerSpawnConfig.SpawnPoint = playerSpawnPoint;

        if (!MultiplayerManager.PendingStartHostAfterSceneLoad)
            return;

        MultiplayerManager.ClearPendingHostFlag();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkHostBootstrap] Không tìm thấy NetworkManager.Singleton.");
            return;
        }

        if (NetworkManager.Singleton.IsServer)
            return;

        ParrelSyncTransportPort.ApplyClonePortOffsetIfNeeded(NetworkManager.Singleton);

        if (!NetworkManager.Singleton.StartHost())
            Debug.LogError("[NetworkHostBootstrap] StartHost() thất bại.");
    }
}
