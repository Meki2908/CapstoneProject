using System;
using UnityEngine;

/// <summary>
/// Kết nối TakeDamageTest với DungeonMania Enemy System
/// - Theo dõi khi enemy chết (qua TakeDamageTest.IsAlive() hoặc EnemyScript.alive)
/// - Gọi EnemyEvent.DeadEvent để thông báo cho hệ thống
/// - Set EnemyScript.alive = false khi enemy chết (vì TakeDamageTest không làm điều này)
/// </summary>
public class EnemyDeathBridge : MonoBehaviour
{
    public event Action OnEnemyDied;

    private TakeDamageTest takeDamage;
    private EnemyScript enemyScript;
    private bool hasCalledDeadEvent = false;
    private bool wasAlive = true;

    void Start()
    {
        // Tìm TakeDamageTest
        takeDamage = GetComponent<TakeDamageTest>();
        if (takeDamage == null)
            takeDamage = GetComponentInChildren<TakeDamageTest>();
        
        // Tìm EnemyScript
        enemyScript = GetComponent<EnemyScript>();
        if (enemyScript == null)
            enemyScript = GetComponentInParent<EnemyScript>();
        if (enemyScript == null)
            enemyScript = GetComponentInChildren<EnemyScript>();
        
        if (takeDamage == null && enemyScript == null)
        {
            Debug.LogWarning("[EnemyDeathBridge] No TakeDamageTest or EnemyScript found!");
        }
    }

    void Update()
    {
        if (hasCalledDeadEvent) return;

        // Kiểm tra enemy có chết không (ưu tiên TakeDamageTest)
        if (takeDamage != null)
        {
            // Theo dõi TakeDamageTest
            bool currentAlive = takeDamage.IsAlive();
            
            // Khi chuyển từ sống sang chết
            if (wasAlive && !currentAlive)
            {
                OnEnemyDead();
            }
            
            wasAlive = currentAlive;
        }
        else if (enemyScript != null)
        {
            // Fallback: theo dõi EnemyScript.alive
            if (wasAlive && !enemyScript.alive)
            {
                OnEnemyDead();
            }
            wasAlive = enemyScript.alive;
        }
    }

    void OnEnemyDead()
    {
        hasCalledDeadEvent = true;
        OnEnemyDied?.Invoke();

        // QUAN TRỌNG: Set EnemyScript.alive = false để AI dừng lại
        // TakeDamageTest Die() không làm điều này!
        if (enemyScript != null)
        {
            enemyScript.alive = false;
            Debug.Log("[EnemyDeathBridge] Set EnemyScript.alive = false");
            
            // Disable NavMeshAgent để enemy dừng di chuyển
            var navAgent = enemyScript.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.isStopped = true;
                navAgent.enabled = false;
            }
            
            // Disable collider để không còn tương tác
            var collider = enemyScript.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
        
        // Disable TakeDamageTest để không nhận damage nữa
        if (takeDamage != null)
        {
            takeDamage.enabled = false;
        }
        
        // Gọi EnemyEvent.DeadEvent để thông báo cho hệ thống
        // Sử dụng EnemyEventSystem(5) thay vì gọi trực tiếp event
        EnemyEvent.EnemyEventSystem(5);
        
        Debug.Log("[EnemyDeathBridge] Enemy dead, called DeadEvent");

        // Gửi thông báo cho DungeonWaveManager nếu có
        if (DungeonWaveManager.Instance != null && enemyScript != null)
        {
            int enemyTypeValue = (int)enemyScript.enemyType;
            int exp = GetExpByType(enemyTypeValue);
            DungeonWaveManager.Instance.OnEnemyKilled(enemyTypeValue, exp);
        }

        if (DungeonOSTManager.Instance != null && enemyScript != null &&
            DungeonOSTManager.IsOstBossCategory(enemyScript.enemyType))
            DungeonOSTManager.Instance.BossPresenceLeave();
        
        // Hủy enemy sau khi chết (với delay nhỏ để hoàn thành animation nếu có)
        // === SPAWN ITEM DROPS (Genshin-style) ===
        var dropSpawner = GetComponent<ItemDropSpawner>();
        if (dropSpawner == null) dropSpawner = GetComponentInParent<ItemDropSpawner>();
        if (dropSpawner != null)
        {
            dropSpawner.SpawnDrops(transform.position);
            Debug.Log("[EnemyDeathBridge] Item drops spawned!");
        }

        GameObject objToDestroy = gameObject;
        Transform parent = objToDestroy.transform.parent;
        if (parent != null && parent.name.Contains("EnemyNew"))
        {
            Debug.Log("[EnemyDeathBridge] Queueing parent EnemyNew for destruction: " + parent.name);
            objToDestroy = parent.gameObject;
        }
        else
        {
            Debug.Log("[EnemyDeathBridge] Queueing enemy directly for destruction: " + objToDestroy.name);
        }
        
        // Destroy sau 5.5s để đảm bảo EnemyDamage.Death() kịp chạy animation và spawn effect
        Destroy(objToDestroy, 5.5f);
    }

    int GetExpByType(int enemyType)
    {
        switch (enemyType)
        {
            case 0: return 100;    // Skeleton
            case 1: return 150;    // Archer
            case 2: return 300;    // Monster
            case 3: return 350;    // Lich
            case 4: return 1500;   // Boss (chung)
            case 5: return 3000;   // Demon
            case 6: return 1500;   // Stoneogre
            case 7: return 1800;   // Golem
            case 8: return 2000;   // Minotaur
            case 9: return 2500;   // Ifrit
            default: return 100;
        }
    }

    void OnDestroy()
    {
        // Cleanup nếu cần
    }
}
