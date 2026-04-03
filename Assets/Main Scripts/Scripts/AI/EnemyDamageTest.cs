using UnityEngine;

/// <summary>
/// Script để test enemy damage system
/// Attach vào enemy để debug damage issues
/// </summary>
public class EnemyDamageTest : MonoBehaviour
{
    [Header("Test Settings")]
    public bool testOnStart = false;
    public float testDamage = 5f;

    private EnemyContactDamage contactDamage;
    private BaseEnemyAI enemyAI;
    private PlayerHealth playerHealth;

    void Start()
    {
        contactDamage = GetComponent<EnemyContactDamage>();
        enemyAI = GetComponent<BaseEnemyAI>();

        // Find player health
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (testOnStart)
        {
            TestDamage();
        }
    }

    [ContextMenu("Test Damage")]
    public void TestDamage()
    {
        if (playerHealth == null)
        {
            Debug.LogError("[EnemyDamageTest] PlayerHealth not found!");
            return;
        }

        if (contactDamage == null)
        {
            Debug.LogError("[EnemyDamageTest] EnemyContactDamage not found!");
            return;
        }

        Debug.Log($"[EnemyDamageTest] Testing enemy damage. ContactDamage={contactDamage.contactDamage}, EnemyState={enemyAI?.GetCurrentState()}");

        // Force enemy into attack state to test damage
        if (enemyAI != null)
        {
            // Temporarily change state to test
            var originalState = enemyAI.GetCurrentState();
            // Note: We can't directly set state, but we can test the logic
            Debug.Log($"[EnemyDamageTest] Current enemy state: {originalState}");
        }

        // Test direct damage call
        playerHealth.TakeDamage(testDamage, transform.position);
        Debug.Log($"[EnemyDamageTest] Direct damage test: {testDamage} applied to player");
    }

    [ContextMenu("Log Enemy State")]
    public void LogEnemyState()
    {
        if (enemyAI != null)
        {
            Debug.Log($"[EnemyDamageTest] Enemy {gameObject.name} state: {enemyAI.GetCurrentState()}");
        }
        else
        {
            Debug.LogError("[EnemyDamageTest] No BaseEnemyAI found!");
        }
    }

    [ContextMenu("Force Attack State")]
    public void ForceAttackState()
    {
        if (enemyAI != null)
        {
            enemyAI.ForceAttackForTesting();
        }
        else
        {
            Debug.LogError("[EnemyDamageTest] No BaseEnemyAI found!");
        }
    }
}
