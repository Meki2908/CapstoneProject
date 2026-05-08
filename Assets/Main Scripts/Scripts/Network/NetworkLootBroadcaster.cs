using Fusion;
using UnityEngine;

public class NetworkLootBroadcaster : NetworkBehaviour
{
    public static NetworkLootBroadcaster Instance;

    public override void Spawned()
    {
        Instance = this;
        Debug.Log("[LootBroadcaster] Đã khởi động hệ thống phát loa rớt đồ!");
    }

    // Server gọi hàm này khi quái chết
    public void BroadcastDrop(Vector3 position, int enemyType)
    {
        if (HasStateAuthority)
        {
            Rpc_TriggerLocalDrops(position, enemyType);
        }
    }

    // Loa phát cho TẤT CẢ mọi máy (bao gồm cả Host và Client)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_TriggerLocalDrops(Vector3 position, int enemyType)
    {
        if (DungeonWaveManager.Instance != null)
        {
            DungeonWaveManager.Instance.SpawnLocalLoot(position, enemyType);
        }
    }
}

