using System;
using UnityEngine;
using Fusion;

/// <summary>
/// Kết nối TakeDamageTest với DungeonMania Enemy System
/// - Theo dõi khi enemy chết (qua TakeDamageTest.IsAlive() hoặc EnemyScript.alive)
/// - Gọi EnemyEvent.DeadEvent để thông báo cho hệ thống
/// - Set EnemyScript.alive = false khi enemy chết (vì TakeDamageTest không làm điều này)
/// 
/// MULTIPLAYER FIX:
/// - Khi chạy dưới Fusion, gửi RPC tới server để server gọi OnEnemyKilled
/// - Client không gọi trực tiếp DungeonWaveManager vì nó có thể không tồn tại trên máy client
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class EnemyDeathBridge : NetworkBehaviour
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
            bool currentAlive = takeDamage.IsAlive();
            
            if (wasAlive && !currentAlive)
            {
                OnEnemyDead();
            }
            
            wasAlive = currentAlive;
        }
        else if (enemyScript != null)
        {
            if (wasAlive && !enemyScript.alive)
            {
                OnEnemyDead();
            }
            wasAlive = enemyScript.alive;
        }
    }

    void OnEnemyDead()
    {
        if (hasCalledDeadEvent) return;
        hasCalledDeadEvent = true;
        OnEnemyDied?.Invoke();

        if (enemyScript != null)
        {
            enemyScript.alive = false;
            Debug.Log("[EnemyDeathBridge] Set EnemyScript.alive = false");
            
            var navAgent = enemyScript.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                if (navAgent.isOnNavMesh) navAgent.isStopped = true;
                navAgent.enabled = false;
            }
            
            var collider = enemyScript.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
        
        if (takeDamage != null)
        {
            takeDamage.enabled = false;
        }
        
        EnemyEvent.EnemyEventSystem(5);
        
        Debug.Log("[EnemyDeathBridge] Enemy dead, called DeadEvent");

        // ─── MULTIPLAYER: Thông báo cho DungeonWaveManager qua network ───
        NotifyWaveManager();

        // SPAWN ITEM DROPS (Genshin-style)
        var dropSpawner = GetComponent<ItemDropSpawner>();
        if (dropSpawner == null) dropSpawner = GetComponentInParent<ItemDropSpawner>();
        if (dropSpawner != null)
        {
            dropSpawner.SpawnDrops(transform.position);
            Debug.Log("[EnemyDeathBridge] Item drops spawned!");
        }

        // Hủy enemy sau khi chết (với delay nhỏ để hoàn thành animation nếu có)
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
        
        Destroy(objToDestroy, 5.5f);
    }

    /// <summary>
    /// Thông báo cho DungeonWaveManager rằng enemy đã chết.
    /// Client gửi RPC tới server → server gọi OnEnemyKilled.
    /// </summary>
    private void NotifyWaveManager()
    {
        int enemyTypeValue = enemyScript != null ? (int)enemyScript.enemyType : 0;
        int exp = GetExpByType(enemyTypeValue);

        if (HasStateAuthority)
        {
            // Server: gọi trực tiếp
            CallOnEnemyKilled(enemyTypeValue, exp);
        }
        else if (HasInputAuthority)
        {
            // Client: gửi RPC tới server
            RPC_NotifyEnemyDeath(enemyTypeValue, exp);
        }
        else
        {
            // Proxy (remote player object): không làm gì
            Debug.Log("[EnemyDeathBridge] Proxy enemy death — no action needed");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_NotifyEnemyDeath(int enemyType, int exp)
    {
        Debug.Log($"[EnemyDeathBridge] RPC: Enemy {enemyType} died, exp={exp}");
        CallOnEnemyKilled(enemyType, exp);
    }

    private void CallOnEnemyKilled(int enemyType, int exp)
    {
        if (DungeonWaveManager.Instance != null)
        {
            DungeonWaveManager.Instance.OnEnemyKilled(enemyType, exp);
        }
        else
        {
            Debug.LogWarning("[EnemyDeathBridge] DungeonWaveManager.Instance is null!");
        }
    }

    /// <summary>
    /// Gọi trực tiếp từ EnemyScript/EnemyAttack (single-player fallback).
    /// </summary>
    public void NotifyDeathDirect(int enemyType, int exp)
    {
        if (hasCalledDeadEvent) return;
        hasCalledDeadEvent = true;

        int expVal = exp > 0 ? exp : GetExpByType(enemyType);

        if (DungeonWaveManager.Instance != null)
        {
            DungeonWaveManager.Instance.OnEnemyKilled(enemyType, expVal);
        }

        if (DungeonOSTManager.Instance != null && enemyScript != null &&
            DungeonOSTManager.IsOstBossCategory(enemyScript.enemyType))
            DungeonOSTManager.Instance.BossPresenceLeave();
    }

    int GetExpByType(int enemyType)
    {
        switch (enemyType)
        {
            case 0: return 100;
            case 1: return 150;
            case 2: return 300;
            case 3: return 350;
            case 4: return 1500;
            case 5: return 3000;
            case 6: return 1500;
            case 7: return 1800;
            case 8: return 2000;
            case 9: return 2500;
            default: return 100;
        }
    }

    void OnDestroy()
    {
    }
}
