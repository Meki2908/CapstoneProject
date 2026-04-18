using UnityEngine;

/// <summary>
/// Gắn trong gameplay scene. Đăng ký spawn points cho multiplayer.
/// Kéo thả SpawnPoint_1..4 vào mảng spawnPoints trong Inspector.
/// </summary>
[DefaultExecutionOrder(-300)]
public class FusionSpawnPointSetter : MonoBehaviour
{
    [Header("Spawn Points — Kéo SpawnPoint_1..4 vào đây")]
    [Tooltip("Danh sách spawn points theo thứ tự Player 1, Player 2, Player 3, Player 4.")]
    [SerializeField] private Transform[] spawnPoints;

    void Awake()
    {
        // Xóa danh sách cũ
        PlayerSpawnConfig.ClearSpawnPoints();

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[SpawnPointSetter] ❌ Chưa gán spawn points! Kéo SpawnPoint_1..4 vào Inspector.");
            return;
        }

        // Đăng ký từng spawn point theo thứ tự
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                PlayerSpawnConfig.AddSpawnPoint(spawnPoints[i]);
                Debug.Log($"[SpawnPointSetter] [{i}] {spawnPoints[i].name} → pos={spawnPoints[i].position}");
            }
            else
            {
                Debug.LogError($"[SpawnPointSetter] ❌ spawnPoints[{i}] is NULL! Kéo Transform vào slot {i} trong Inspector.");
            }
        }

        // Set spawn point chính
        if (spawnPoints.Length > 0 && spawnPoints[0] != null)
            PlayerSpawnConfig.SpawnPoint = spawnPoints[0];

        Debug.Log($"[SpawnPointSetter] ✅ Done — {PlayerSpawnConfig.SpawnPointCount} spawn point(s) registered.");

        if (PlayerSpawnConfig.SpawnPointCount < 4)
            Debug.LogWarning($"[SpawnPointSetter] ⚠️ Chỉ có {PlayerSpawnConfig.SpawnPointCount} spawn point(s). Cần 4 cho multiplayer 4 người.");
    }
}
