using UnityEngine;

/// <summary>
/// Spawn point cho player khi vào scene — gán từ scene (empty Transform) trước khi spawn/load.
/// </summary>
public static class PlayerSpawnConfig
{
    public static Transform SpawnPoint { get; set; }
}
