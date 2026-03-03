using UnityEngine;

/// <summary>
/// Debug script để kiểm tra Bow attack system
/// Attach vào enemy có Bow script để debug
/// </summary>
public class BowAttackDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool enableDebug = true;
    
    private EnemyScript enemyScript;
    private EnemyAttack enemyAttack;
    private Bow[] bowScripts;
    
    void Start()
    {
        enemyScript = GetComponent<EnemyScript>();
        enemyAttack = GetComponent<EnemyAttack>();
        
        if (enemyScript != null && enemyScript.bow != null)
        {
            bowScripts = new Bow[enemyScript.bow.Length];
            for (int i = 0; i < enemyScript.bow.Length; i++)
            {
                if (enemyScript.bow[i] != null)
                {
                    bowScripts[i] = enemyScript.bow[i].GetComponent<Bow>();
                }
            }
            
            if (enableDebug)
            {
                Debug.Log($"[BowDebugger] {gameObject.name}: Found {enemyScript.bow.Length} bow particle systems");
                for (int i = 0; i < enemyScript.bow.Length; i++)
                {
                    if (enemyScript.bow[i] != null)
                    {
                        ParticleSystem ps = enemyScript.bow[i].GetComponent<ParticleSystem>();
                        Debug.Log($"[BowDebugger] Bow[{i}]: {enemyScript.bow[i].name}, PS={(ps != null ? "found" : "NULL")}, BowScript={(bowScripts[i] != null ? "found" : "NULL")}");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"[BowDebugger] {gameObject.name}: EnemyScript or bow array is null!");
        }
    }
    
    /// <summary>
    /// Test method - gọi từ Animation Event để test
    /// </summary>
    public void TestBowAttack()
    {
        if (enableDebug)
        {
            Debug.Log($"[BowDebugger] TestBowAttack called on {gameObject.name}");
        }
        
        if (enemyAttack != null)
        {
            enemyAttack.Bow(1);
        }
        else
        {
            Debug.LogError($"[BowDebugger] EnemyAttack is null!");
        }
    }
    
    void Update()
    {
        if (enableDebug && bowScripts != null)
        {
            for (int i = 0; i < bowScripts.Length; i++)
            {
                if (bowScripts[i] != null && enemyScript.bow[i] != null)
                {
                    ParticleSystem ps = enemyScript.bow[i].GetComponent<ParticleSystem>();
                    if (ps != null && ps.isPlaying)
                    {
                        Debug.Log($"[BowDebugger] Bow[{i}] particle is PLAYING!");
                    }
                }
            }
        }
    }
}
