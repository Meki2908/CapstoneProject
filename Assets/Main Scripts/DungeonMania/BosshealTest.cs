// BossHealthBarTest.cs — XÓA SAU KHI TEST XONG
using UnityEngine;

public class BossHealthBarTest : MonoBehaviour
{
    void Start()
    {
        // Đợi 2 giây cho boss load xong rồi tìm
        Invoke(nameof(FindAndShowBossHP), 2f);
    }
    
    void FindAndShowBossHP()
    {
        // Tìm tất cả EnemyScript, lọc boss
        foreach (var es in FindObjectsByType<EnemyScript>(FindObjectsSortMode.None))
        {
            if (!es.isBoss) continue;
            
            var hp = es.GetComponent<TakeDamageTest>();
            if (hp == null) hp = es.GetComponentInChildren<TakeDamageTest>();
            if (hp == null) continue;
            
            BossHealthBarUI.EnsureInstance();
            BossHealthBarUI.Instance.ShowBossHealth(hp);
            Debug.Log($"[Test] Boss HP bar shown: {es.enemyName}");
            return;
        }
        Debug.LogWarning("[Test] Không tìm thấy boss trong scene!");
    }
}
