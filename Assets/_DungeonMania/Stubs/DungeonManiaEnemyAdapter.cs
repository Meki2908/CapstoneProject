using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Adapter để kết nối Enemy từ Dungeon Mania với project của bạn
/// Attach vào prefab enemy để nó hoạt động với WaveSpawner
/// </summary>
public class DungeonManiaEnemyAdapter : MonoBehaviour
{
    [Header("🎮 Kết nối với Project")]
    public Transform playerTransform;
    public MonoBehaviour playerHealthSystem;
    
    [Header("⚙️ Cài đặt")]
    public bool autoFindPlayer = true;
    
    // References đến Dungeon Mania scripts
    private EnemyScript enemyScript;
    private EnemyDamage enemyDamage;
    
    // References
    private NavMeshAgent navMeshAgent;
    private Animator animator;
    
    // Event để thông báo cho WaveSpawner
    public System.Action OnEnemyDeath;
    public System.Action<int> OnEnemyDamaged;
    
    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        enemyScript = GetComponent<EnemyScript>();
        enemyDamage = GetComponent<EnemyDamage>();
        
        if (autoFindPlayer && playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
    
    void Start()
    {
        if (enemyScript != null && playerTransform != null)
        {
            // Set target cho enemy
            var targetField = typeof(EnemyScript).GetField("target", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetField != null)
            {
                targetField.SetValue(enemyScript, playerTransform);
            }
        }
    }
    
    /// <summary>
    /// Gọi method này để gây damage cho enemy (từ player attack)
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (enemyDamage != null)
        {
            Damage damageStruct = new Damage();
            damageStruct.damage = damage;
            damageStruct.damageElemental = 0;
            damageStruct.crit = 0;
            damageStruct.elementalType = 0;
            damageStruct.isBow = false;
            damageStruct.isSpell = false;
            damageStruct.spellID = 0;
            
            enemyDamage.Damage(damageStruct);
            OnEnemyDamaged?.Invoke(damage);
        }
    }
    
    /// <summary>
    /// Gọi method này để kill enemy
    /// </summary>
    public void Die()
    {
        if (enemyScript != null)
        {
            enemyScript.alive = false;
            OnEnemyDeath?.Invoke();
        }
    }
    
    /// <summary>
    /// Kiểm tra enemy còn sống không
    /// </summary>
    public bool IsAlive()
    {
        return enemyScript != null && enemyScript.alive;
    }
    
    /// <summary>
    /// Lấy health hiện tại của enemy
    /// FIX #5: Trả về helth.value (current) thay vì mainHelth (max)
    /// </summary>
    public float GetCurrentHealth()
    {
        if (enemyScript != null && enemyScript.enemy != null)
        {
            return enemyScript.enemy.helth.value;
        }
        return 100;
    }
    
    /// <summary>
    /// Lấy max health của enemy
    /// </summary>
    public float GetMaxHealth()
    {
        if (enemyScript != null && enemyScript.enemy != null)
        {
            return enemyScript.enemy.mainHelth;
        }
        return 100;
    }
    
    /// <summary>
    /// Enable/Disable enemy
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (enemyScript != null)
        {
            enemyScript.enabled = enabled;
            enemyScript.gameObject.SetActive(enabled);
        }
    }
}

/// <summary>
/// Static helper để quản lý tất cả Dungeon Mania enemies
/// </summary>
public static class DungeonManiaEnemyManager
{
    public static DungeonManiaEnemyAdapter[] FindAllEnemies()
    {
        return Object.FindObjectsOfType<DungeonManiaEnemyAdapter>();
    }
    
    public static DungeonManiaEnemyAdapter FindNearestEnemy(Vector3 position, float maxDistance = Mathf.Infinity)
    {
        DungeonManiaEnemyAdapter nearest = null;
        float minDist = maxDistance;
        
        foreach (var enemy in FindAllEnemies())
        {
            if (enemy.IsAlive())
            {
                float dist = Vector3.Distance(position, enemy.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }
        }
        
        return nearest;
    }
}
