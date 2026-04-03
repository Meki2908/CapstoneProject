using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// Simple enemy spawner for desert biome dungeon.
/// Spawns Spider and Minotaur enemies at random valid NavMesh positions.
/// Designed for quick demo setup.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("Spider prefab for desert biome")]
    public GameObject spiderPrefab;
    [Tooltip("Minotaur prefab for desert biome")]
    public GameObject minotaurPrefab;

    [Header("Spawn Counts")]
    [Tooltip("Number of spiders to spawn (4-8 recommended)")]
    public int spiderCount = 6;
    [Tooltip("Number of minotaurs to spawn (2-4 recommended)")]
    public int minotaurCount = 3;

    [Header("Spawn Settings")]
    [Tooltip("Maximum attempts to find valid spawn position")]
    public int maxSpawnAttempts = 30;
    [Tooltip("Minimum distance between spawned enemies")]
    public float minDistanceBetweenEnemies = 5f;
    [Tooltip("Search radius for finding spawn positions")]
    public float spawnSearchRadius = 50f;

    // Track spawned enemies for debugging
    private System.Collections.Generic.List<GameObject> spawnedEnemies = new System.Collections.Generic.List<GameObject>();

    void Start()
    {
        // Wait a frame to ensure NavMesh is fully loaded
        StartCoroutine(SpawnEnemiesDelayed());
    }

    private IEnumerator SpawnEnemiesDelayed()
    {
        yield return null; // Wait one frame

        Debug.Log("[EnemySpawner] Starting enemy spawn...");

        // Spawn spiders
        for (int i = 0; i < spiderCount; i++)
        {
            if (SpawnEnemy(spiderPrefab, "Spider"))
            {
                Debug.Log($"[EnemySpawner] Spawned Spider {i + 1}/{spiderCount}");
            }
        }

        // Spawn minotaurs
        for (int i = 0; i < minotaurCount; i++)
        {
            if (SpawnEnemy(minotaurPrefab, "Minotaur"))
            {
                Debug.Log($"[EnemySpawner] Spawned Minotaur {i + 1}/{minotaurCount}");
            }
        }

        Debug.Log($"[EnemySpawner] Spawn complete! Total enemies: {spawnedEnemies.Count}");
    }

    private bool SpawnEnemy(GameObject prefab, string enemyType)
    {
        if (prefab == null)
        {
            Debug.LogError($"[EnemySpawner] {enemyType} prefab is null!");
            return false;
        }

        Vector3 spawnPos = FindValidSpawnPosition();
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning($"[EnemySpawner] Could not find valid spawn position for {enemyType}");
            return false;
        }

        // Spawn enemy
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Ensure enemy has required components
        if (enemy.GetComponent<NavMeshAgent>() == null)
        {
            Debug.LogError($"[EnemySpawner] {enemyType} prefab missing NavMeshAgent component!");
            Destroy(enemy);
            return false;
        }

        // Find and assign player reference if enemy AI needs it
        var enemyAI = enemy.GetComponent<BaseEnemyAI>();
        if (enemyAI != null && enemyAI.player == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                enemyAI.player = player.transform;
            }
        }

        spawnedEnemies.Add(enemy);
        Debug.Log($"[EnemySpawner] Successfully spawned {enemyType} at {spawnPos}");
        return true;
    }

    private Vector3 FindValidSpawnPosition()
    {
        // Try multiple times to find a valid position
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Generate random position around spawner
            Vector3 randomPos = transform.position + Random.insideUnitSphere * spawnSearchRadius;
            randomPos.y = 0; // Keep on ground level

            // Use NavMesh.SamplePosition to find valid position on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 10f, NavMesh.AllAreas))
            {
                Vector3 candidatePos = hit.position;

                // Check minimum distance from other spawned enemies
                bool tooClose = false;
                foreach (var spawnedEnemy in spawnedEnemies)
                {
                    if (spawnedEnemy != null)
                    {
                        float distance = Vector3.Distance(candidatePos, spawnedEnemy.transform.position);
                        if (distance < minDistanceBetweenEnemies)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }

                if (!tooClose)
                {
                    return candidatePos;
                }
            }
        }

        // Fallback: try to find ANY valid position near spawner
        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(transform.position, out fallbackHit, spawnSearchRadius, NavMesh.AllAreas))
        {
            return fallbackHit.position;
        }

        return Vector3.zero; // No valid position found
    }

    // Debug method to visualize spawn area
    void OnDrawGizmosSelected()
    {
        // Draw spawn search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnSearchRadius);

        // Draw minimum distance between enemies
        Gizmos.color = Color.red;
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, minDistanceBetweenEnemies);
            }
        }
    }

    // Public method to respawn all enemies (useful for testing)
    public void RespawnAllEnemies()
    {
        // Clean up existing enemies
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();

        // Respawn
        StartCoroutine(SpawnEnemiesDelayed());
    }
}
