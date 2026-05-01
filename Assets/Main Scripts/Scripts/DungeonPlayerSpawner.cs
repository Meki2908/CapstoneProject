using Fusion;
using UnityEngine;
using System.Collections;

public class DungeonPlayerSpawner : MonoBehaviour
{
    [Header("=== Cấu hình Spawn ===")]
    public NetworkPrefabRef playerPrefab;
    public Transform spawnPoint;

    private IEnumerator Start()
    {
        NetworkRunner runner = null;

        // BÍ KÍP Ở ĐÂY: Dùng WaitUntil để check liên tục thay vì đếm giây cứng
        // Nó sẽ chỉ đi tiếp khi tìm thấy Runner VÀ Runner đã khởi động xong (IsRunning)
        yield return new WaitUntil(() => 
        {
            runner = FindFirstObjectByType<NetworkRunner>();
            return runner != null && runner.IsRunning;
        });

        // Thêm 1 frame nghỉ để chắc chắn các Object trong scene đã Awake xong
        yield return null; 

        if (runner.IsServer)
        {
            Debug.Log("<color=yellow>[DungeonPlayerSpawner]</color> Runner đã sẵn sàng! Bắt đầu đẻ Player...");
            if (playerPrefab.IsValid)
            {
                Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
                Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

                NetworkObject playerObj = runner.Spawn(playerPrefab, pos, rot, runner.LocalPlayer);
                Debug.Log($"<color=green>[DungeonPlayerSpawner]</color> THÀNH CÔNG! Đã đẻ Player: {playerObj.name}");

                // 4. BƠM PLAYER VÀO DUNGEON WAVE MANAGER
                var waveMgr = FindFirstObjectByType<DungeonWaveManager>();
                if (waveMgr != null)
                {
                    waveMgr.player = playerObj.transform;

                    // Tạo cục "player" (chữ p thường) để đàn em EnemyScript không bị mù
                    GameObject playerLower = GameObject.Find("player");
                    if (playerLower == null)
                    {
                        playerLower = new GameObject("player");
                        playerLower.transform.SetParent(playerObj.transform);
                        playerLower.transform.localPosition = Vector3.zero;
                    }
                    Debug.Log("<color=cyan>[DungeonPlayerSpawner]</color> Setup: Đã truyền Player cho DungeonWaveManager!");
                }
            }
        }
    }
}
