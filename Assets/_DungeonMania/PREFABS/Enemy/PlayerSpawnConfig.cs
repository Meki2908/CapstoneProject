using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn points cho player khi vào scene — gán từ scene (empty Transform) trước khi spawn/load.
/// MULTIPLAYER: Hỗ trợ nhiều spawn points cho nhiều người chơi.
/// </summary>
public static class PlayerSpawnConfig
{
    /// <summary>Điểm spawn chính (cho player 1 hoặc fallback).</summary>
    public static Transform SpawnPoint { get; set; }

    /// <summary>Danh sách điểm spawn cho multiplayer (theo PlayerRef index).</summary>
    private static readonly List<Transform> _spawnPoints = new List<Transform>();

    /// <summary>Thêm một điểm spawn vào danh sách.</summary>
    public static void AddSpawnPoint(Transform point)
    {
        if (point != null && !_spawnPoints.Contains(point))
        {
            _spawnPoints.Add(point);
            Debug.Log($"[SpawnConfig] Added spawn point: {point.name} (total: {_spawnPoints.Count})");
        }
    }

    /// <summary>Xóa một điểm spawn khỏi danh sách.</summary>
    public static void RemoveSpawnPoint(Transform point)
    {
        if (point != null)
        {
            _spawnPoints.Remove(point);
            Debug.Log($"[SpawnConfig] Removed spawn point: {point.name} (remaining: {_spawnPoints.Count})");
        }
    }

    /// <summary>Xóa tất cả điểm spawn.</summary>
    public static void ClearSpawnPoints()
    {
        _spawnPoints.Clear();
        Debug.Log("[SpawnConfig] Cleared all spawn points");
    }

    /// <summary>
    /// Lấy điểm spawn cho player thứ index (0-based).
    /// Nếu index >= số điểm spawn → dùng SpawnPoint chính.
    /// </summary>
    public static Transform GetSpawnPoint(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < _spawnPoints.Count)
        {
            return _spawnPoints[playerIndex];
        }
        return SpawnPoint;
    }

    /// <summary>
    /// Lấy vị trí spawn cho player thứ index, có fallback.
    /// </summary>
    public static Vector3 GetSpawnPosition(int playerIndex)
    {
        var point = GetSpawnPoint(playerIndex);
        if (point != null)
            return point.position;

        // Fallback: xếp người chơi thành vòng tròn quanh (0,0,0)
        float angle = playerIndex * (360f / 4f);
        float radius = 3f;
        float rad = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
    }

    /// <summary>
    /// Lấy rotation cho player thứ index.
    /// </summary>
    public static Quaternion GetSpawnRotation(int playerIndex)
    {
        var point = GetSpawnPoint(playerIndex);
        if (point != null)
            return point.rotation;
        return Quaternion.identity;
    }

    /// <summary>Số lượng spawn points hiện có.</summary>
    public static int SpawnPointCount => _spawnPoints.Count;
}
