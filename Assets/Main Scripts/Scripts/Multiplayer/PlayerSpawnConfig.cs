using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn points cho player khi vào scene — gán từ scene (empty Transform) trước khi spawn/load.
/// MULTIPLAYER: Hỗ trợ nhiều spawn points cho nhiều người chơi.
/// </summary>
public static class PlayerSpawnConfig
{
    /// <summary>Điểm spawn chính (SpawnPoint_1 — dùng khi index > số điểm).</summary>
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
        Debug.LogError($"[SpawnConfig] SpawnPoint[{playerIndex}] out of range! Count={_spawnPoints.Count}. Add spawn points in scene.");
        return null;
    }

    /// <summary>Lấy vị trí spawn cho player thứ index (không fallback — bắt buộc setup).</summary>
    public static Vector3 GetSpawnPosition(int playerIndex)
    {
        var point = GetSpawnPoint(playerIndex);
        if (point != null)
            return point.position;

        // KHÔNG dùng fallback vòng tròn — bắt buộc phải setup spawn point trong scene
        Debug.LogError($"[SpawnConfig] No spawn point found for playerIndex={playerIndex}! Add spawn points in scene.");
        return Vector3.zero;
    }

    /// <summary>
    /// Lấy rotation cho player thứ index.
    /// </summary>
    public static Quaternion GetSpawnRotation(int playerIndex)
    {
        var point = GetSpawnPoint(playerIndex);
        if (point != null)
            return point.rotation;
        Debug.LogError($"[SpawnConfig] No spawn point found for playerIndex={playerIndex}! Add spawn points in scene.");
        return Quaternion.identity;
    }

    /// <summary>Số lượng spawn points hiện có.</summary>
    public static int SpawnPointCount => _spawnPoints.Count;
}
