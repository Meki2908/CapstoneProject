using UnityEngine;

/// <summary>
/// Test script để verify enemy system hoạt động đúng
/// </summary>
public class TestEnemySystem : MonoBehaviour
{
    [Header("Test Settings")]
    public GameObject spiderPrefab;
    public Transform spawnPoint;
    public Transform player;

    private SpiderEnemyAI spawnedSpider;

    void Start()
    {
        // Test spawn enemy từ pool (nếu có)
        if (EnemyPoolManager.Instance != null && spiderPrefab != null)
        {
            GameObject enemy = EnemyPoolManager.Instance.SpawnEnemy("Spider", spawnPoint.position, spawnPoint.rotation);
            if (enemy != null)
            {
                spawnedSpider = enemy.GetComponent<SpiderEnemyAI>();
                Debug.Log("✅ Spider spawned from pool!");
            }
        }
        else if (spiderPrefab != null)
        {
            // Fallback: Instantiate normally
            GameObject enemy = Instantiate(spiderPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedSpider = enemy.GetComponent<SpiderEnemyAI>();
            Debug.Log("✅ Spider instantiated normally!");
        }

        // Test BaseEnemyAI functionality
        if (spawnedSpider != null)
        {
            Debug.Log($"Enemy State: {spawnedSpider.GetCurrentState()}");
            Debug.Log($"Player in detection range: {spawnedSpider.IsPlayerInDetectionRange()}");
            Debug.Log($"Player in attack range: {spawnedSpider.IsPlayerInAttackRange()}");
        }
    }

    void Update()
    {
        // Test các method public
        if (spawnedSpider != null && Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Testing TakeHit...");
            spawnedSpider.TakeHit();
        }

        if (spawnedSpider != null && Input.GetKeyDown(KeyCode.Y))
        {
            Debug.Log("Testing Die...");
            spawnedSpider.Die();
        }

        if (spawnedSpider != null && Input.GetKeyDown(KeyCode.U))
        {
            Debug.Log($"Current State: {spawnedSpider.GetCurrentState()}");
        }
    }

    void OnGUI()
    {
        if (spawnedSpider != null)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Enemy State: {spawnedSpider.GetCurrentState()}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Player Distance: {spawnedSpider.GetDistanceToPlayer():F1}m");
            GUI.Label(new Rect(10, 50, 300, 20), $"In Detection: {spawnedSpider.IsPlayerInDetectionRange()}");
            GUI.Label(new Rect(10, 70, 300, 20), $"In Attack: {spawnedSpider.IsPlayerInAttackRange()}");
        }

        GUI.Label(new Rect(10, 100, 400, 20), "Press T: TakeHit | Y: Die | U: Log State");
    }
}















