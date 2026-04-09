// BossHealthBarTest.cs — ĐÃ VÔ HIỆU HÓA
// File này không còn cần thiết vì DungeonWaveManager đã gọi ShowBossHealth.
// Giữ lại để tham khảo, nhưng không chạy.
using UnityEngine;

// [DISABLED] — uncomment class nếu cần test lại
// public class BossHealthBarTest : MonoBehaviour
// {
//     void Start()
//     {
//         Invoke(nameof(FindAndShowBossHP), 2f);
//     }
//     
//     void FindAndShowBossHP()
//     {
//         foreach (var es in FindObjectsByType<EnemyScript>(FindObjectsSortMode.None))
//         {
//             if (!es.isBoss) continue;
//             var hp = es.GetComponent<TakeDamageTest>();
//             if (hp == null) hp = es.GetComponentInChildren<TakeDamageTest>();
//             if (hp == null) continue;
//             BossHealthBarUI.EnsureInstance();
//             BossHealthBarUI.Instance.ShowBossHealth(hp);
//             Debug.Log($"[Test] Boss HP bar shown: {es.enemyName}");
//             return;
//         }
//     }
// }
