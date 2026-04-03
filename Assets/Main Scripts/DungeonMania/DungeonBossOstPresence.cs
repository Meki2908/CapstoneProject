using UnityEngine;

/// <summary>
/// [Lỗi thời] Trước đây gọi <see cref="DungeonOSTManager.BossPresenceLeave"/> trong OnDisable — gây leave nhầm khi DisableAllChildEnemies.
/// Logic mới: leave chỉ khi boss chết (<see cref="EnemyDeathBridge"/>). Giữ component rỗng để prefab cũ không lỗi.
/// </summary>
[DisallowMultipleComponent]
public class DungeonBossOstPresence : MonoBehaviour
{
}
