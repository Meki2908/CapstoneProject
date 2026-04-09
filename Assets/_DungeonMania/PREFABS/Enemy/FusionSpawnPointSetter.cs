using UnityEngine;

/// <summary>
/// Gắn trong gameplay scene. Set PlayerSpawnConfig spawn points khi scene load.
///
/// Ưu tiên:
///   1. GameObject "_SpawnPoints" — lấy TẤT CẢ con trực tiếp theo thứ tự Hierarchy (SpawnPoint_1..4)
///   2. SpawnPoint có sẵn trong scene (tag "SpawnPoint" hoặc tên "PlayerSpawnPoint")
///   3. SpawnPoint gán trong Inspector (playerSpawnPoint)
///   4. Fallback offset quanh tâm
///
/// MULTIPLAYER: Mỗi player spawn tại 1 điểm khác nhau (offset nhỏ nếu chỉ có 1 điểm).
/// </summary>
public class FusionSpawnPointSetter : MonoBehaviour
{
    [Tooltip("Điểm spawn chính — player đầu tiên sẽ spawn ở đây.")]
    [SerializeField] private Transform playerSpawnPoint;

    [Tooltip("Auto-tìm tất cả spawn points trong scene.")]
    [SerializeField] private bool autoFindSpawnPoints = true;

    [Tooltip("Offset giữa các player nếu chỉ có 1 spawn point (mét).")]
    [SerializeField] private float multiPlayerOffset = 2f;

    void Awake()
    {
        // Xóa danh sách cũ
        PlayerSpawnConfig.ClearSpawnPoints();

        // Tìm spawn points
        bool usedMultiSpawnRoot = false;
        if (autoFindSpawnPoints)
            usedMultiSpawnRoot = AutoFindSpawnPoints();

        // Inspector: chỉ khi KHÔNG dùng _SpawnPoints (tránh ghi đè thứ tự 4 điểm)
        if (playerSpawnPoint != null && !usedMultiSpawnRoot)
        {
            PlayerSpawnConfig.SpawnPoint = playerSpawnPoint;
            PlayerSpawnConfig.AddSpawnPoint(playerSpawnPoint);
        }

        // Nếu chỉ có 1 hoặc 0 spawn point → tạo thêm offset points cho multiplayer
        if (PlayerSpawnConfig.SpawnPointCount <= 1)
        {
            CreateOffsetSpawnPoints();
        }

        Debug.Log($"[SpawnPointSetter] ✅ Awake done — SpawnPoint={PlayerSpawnConfig.SpawnPoint?.name ?? "null"}, Count={PlayerSpawnConfig.SpawnPointCount}");

        // Log tất cả positions
        for (int i = 0; i < PlayerSpawnConfig.SpawnPointCount; i++)
        {
            var sp = PlayerSpawnConfig.GetSpawnPoint(i);
            if (sp != null)
                Debug.Log($"[SpawnPointSetter]   [{i}] {sp.name} → pos={sp.position}");
        }

        // Nếu không có spawn point nào → cảnh báo rõ ràng
        if (PlayerSpawnConfig.SpawnPointCount == 0)
        {
            Debug.LogError("[SpawnPointSetter] ❌ NO SPAWN POINTS FOUND! Players will spawn at (0,0,0)! " +
                           "Add parent '_SpawnPoints' with children SpawnPoint_1..4, or 'PlayerSpawnPoint', or tag 'SpawnPoint'.");
        }
    }

    /// <returns>true nếu đã đăng ký từ _SpawnPoints (bỏ qua playerSpawnPoint Inspector).</returns>
    private bool AutoFindSpawnPoints()
    {
        // 1. Ưu tiên: _SpawnPoints với các con SpawnPoint_1, SpawnPoint_2, ... (multiplayer map)
        var multiRoot = GameObject.Find("_SpawnPoints");
        if (multiRoot != null && multiRoot.transform.childCount > 0)
        {
            var t = multiRoot.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i);
                PlayerSpawnConfig.AddSpawnPoint(child);
                Debug.Log($"[SpawnPointSetter] From _SpawnPoints: [{i}] {child.name} → pos={child.position}");
            }

            PlayerSpawnConfig.SpawnPoint = t.GetChild(0);
            Debug.Log($"[SpawnPointSetter] Using _SpawnPoints — {t.childCount} point(s)");
            return true;
        }

        // 2. Tìm object có tên "PlayerSpawnPoint" (spawn point gốc trong map)
        var originalSpawn = GameObject.Find("PlayerSpawnPoint");
        if (originalSpawn != null)
        {
            PlayerSpawnConfig.SpawnPoint = originalSpawn.transform;
            PlayerSpawnConfig.AddSpawnPoint(originalSpawn.transform);
            Debug.Log($"[SpawnPointSetter] Found original PlayerSpawnPoint at {originalSpawn.transform.position}");
        }

        // 3. Tìm tất cả object có tag "SpawnPoint"
        GameObject[] spawnPointObjects = new GameObject[0];
        try { spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint"); }
        catch { /* Tag chưa tồn tại */ }

        foreach (var obj in spawnPointObjects)
        {
            // Bỏ qua nếu đã thêm (PlayerSpawnPoint)
            if (obj.transform == playerSpawnPoint) continue;
            if (originalSpawn != null && obj.transform == originalSpawn.transform) continue;

            PlayerSpawnConfig.AddSpawnPoint(obj.transform);
            Debug.Log($"[SpawnPointSetter] Auto-found spawn point: {obj.name} at {obj.transform.position}");
        }

        return false;
    }

    /// <summary>
    /// Nếu chỉ có 1 spawn point → tạo thêm các điểm offset cho multiplayer.
    /// </summary>
    private void CreateOffsetSpawnPoints()
    {
        var mainPoint = PlayerSpawnConfig.SpawnPoint;
        Vector3 center = mainPoint != null ? mainPoint.position : new Vector3(0, 1, 0);

        Debug.Log($"[SpawnPointSetter] Only {PlayerSpawnConfig.SpawnPointCount} spawn point(s) — creating offset points around {center}");

        // Tạo 3 điểm offset xung quanh spawn point chính (cho 4 players total)
        for (int i = 1; i <= 3; i++)
        {
            float angle = i * (360f / 4f);
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * multiPlayerOffset,
                0,
                Mathf.Sin(rad) * multiPlayerOffset
            );

            var go = new GameObject($"SpawnPoint_Auto_{i + 1}");
            go.transform.position = center + offset;
            go.transform.rotation = mainPoint != null ? mainPoint.rotation : Quaternion.identity;

            PlayerSpawnConfig.AddSpawnPoint(go.transform);
        }
    }
}
