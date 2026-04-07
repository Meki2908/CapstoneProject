using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gắn trên root prefab player (cùng <see cref="NetworkObject"/>).
/// NGO khuyên chỉnh transform trước khi gọi base.OnNetworkSpawn (spawn sequence).
/// </summary>
[DefaultExecutionOrder(-200)]
public class NetworkPlayerSpawnSnap : NetworkBehaviour
{
    void Awake()
    {
        if (!TryGetComponent<NetworkPlayerRootFollowBody>(out _))
            gameObject.AddComponent<NetworkPlayerRootFollowBody>();
        if (!TryGetComponent<NetworkPlayerLocalOwnership>(out _))
            gameObject.AddComponent<NetworkPlayerLocalOwnership>();
        if (!TryGetComponent<NetworkAnimatorSync>(out _))
            gameObject.AddComponent<NetworkAnimatorSync>();
        if (!TryGetComponent<NetworkPlayerStats>(out _))
            gameObject.AddComponent<NetworkPlayerStats>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var pt = PlayerSpawnConfig.SpawnPoint;
            if (pt != null)
                transform.SetPositionAndRotation(pt.position, pt.rotation);
        }

        base.OnNetworkSpawn();
    }
}
