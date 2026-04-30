using Fusion;
using UnityEngine;

public class NetworkEnemySpawner : NetworkBehaviour
{
    public NetworkPrefabRef enemyPrefab; // Kéo prefab EnemyNew vào đây
    public Transform spawnPoint; // Chỗ bạn muốn quái rớt xuống

    // Chỉ khi nào Host đã vào phòng, Host mới có quyền gọi đẻ quái
    public void Update()
    {
        // Bấm phím T để đẻ quái test (Chỉ Host mới đẻ được)
        if (Input.GetKeyDown(KeyCode.T) && Runner != null && Runner.IsServer)
        {
            Runner.Spawn(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            Debug.Log("Host vừa đẻ ra 1 con quái mạng!");
        }
    }
}