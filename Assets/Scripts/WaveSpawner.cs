using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Wave-based enemy spawner for desert dungeon demo.
/// Manages multiple waves with increasing difficulty.
/// Based on Brackeys wave spawner pattern.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {

        [System.Serializable]
        public class EnemySpawn
        {
            [Tooltip("Enemy prefab to spawn")]
            public GameObject enemyPrefab;
            [Tooltip("How many of this enemy to spawn")]
            public int count;
            [Tooltip("Time between spawning each enemy (seconds)")]
            public float spawnRate = 0.5f;
        }

        [Tooltip("List of enemies to spawn in this wave")]
        public EnemySpawn[] enemies;

        [Tooltip("Time to wait before starting next wave (seconds)")]
        public float timeBetweenWaves = 5f;
    }

    public enum SpawnState { SPAWNING, WAITING, COUNTING }

    [Header("Wave Configuration")]
    [Tooltip("Array of waves to spawn")]
    public Wave[] waves;

    [Header("Spawn Settings")]
    [Tooltip("Maximum attempts to find valid spawn position per enemy")]
    public int maxSpawnAttempts = 30;
    [Tooltip("Minimum distance between spawned enemies")]
    public float minDistanceBetweenEnemies = 5f;
    [Tooltip("Search radius for finding spawn positions")]
    public float spawnSearchRadius = 50f;

    [Header("UI Settings")]
    [Tooltip("Unity UI Text to display wave status (optional)")]
    public Text waveText;
    [Tooltip("TextMeshPro Text to display wave status (optional) - use this if using TMP")]
    public TMP_Text waveTextTMP;
    [Tooltip("How long to show wave messages (seconds)")]
    public float messageDisplayTime = 2f;

    // Private variables
    private SpawnState state = SpawnState.COUNTING;
    private float countdown;
    private int currentWaveIndex = 0;
    private float waveCountdown;
    private List<GameObject> enemiesAlive = new List<GameObject>();
    private bool isWaveActive = false; // Prevent multiple waves spawning
    #if UNITY_EDITOR
    private SpawnState lastDebugState = SpawnState.COUNTING;
    #endif

    void Start()
    {
        // Safety checks
        if (waves.Length == 0)
        {
            Debug.LogError("[WaveSpawner] No waves configured! Please add waves in the Inspector.");
            enabled = false;
            return;
        }

        // Check if enemy prefabs are assigned
        foreach (Wave wave in waves)
        {
            foreach (Wave.EnemySpawn enemySpawn in wave.enemies)
            {
                if (enemySpawn.enemyPrefab == null)
                {
                    Debug.LogError("[WaveSpawner] Enemy prefab is null in wave configuration!");
                    enabled = false;
                    return;
                }
            }
        }

        Debug.Log($"[WaveSpawner] Initialized with {waves.Length} waves");

        waveCountdown = 2f; // Start first wave after 2 seconds
        countdown = waveCountdown;

        UpdateWaveUI("Get Ready!");
    }

    void Update()
    {
        // Debug log state changes (only in editor)
        #if UNITY_EDITOR
        if (state != lastDebugState)
        {
            Debug.Log($"[WaveSpawner] State changed: {lastDebugState} -> {state}");
            lastDebugState = state;
        }
        #endif

        if (state == SpawnState.WAITING)
        {
            // Check if wave is completed
            if (!EnemyIsAlive())
            {
                // Wave completed!
                WaveCompleted();
                return;
            }
        }

        if (waveCountdown <= 0)
        {
            if (state == SpawnState.COUNTING && !isWaveActive)
            {
                // Start spawning wave
                isWaveActive = true;
                StartCoroutine(SpawnWave(waves[currentWaveIndex]));
            }
        }
        else
        {
            waveCountdown -= Time.deltaTime;
        }
    }

    void WaveCompleted()
    {
        Debug.Log($"[WaveSpawner] Wave {currentWaveIndex + 1} completed!");

        isWaveActive = false;
        state = SpawnState.COUNTING;
        waveCountdown = waves[currentWaveIndex].timeBetweenWaves;

        UpdateWaveUI($"Wave {currentWaveIndex + 1} Cleared!");

        currentWaveIndex++;

        if (currentWaveIndex >= waves.Length)
        {
            // All waves completed!
            UpdateWaveUI("All Waves Completed! Victory!");
            Debug.Log("[WaveSpawner] All waves completed!");
            enabled = false; // Stop the spawner
        }
        else
        {
            UpdateWaveUI($"Wave {currentWaveIndex + 1} in {waveCountdown:F1}s...");
        }
    }

    bool EnemyIsAlive()
    {
        // Use FindGameObjectsWithTag for reliable enemy counting
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        bool hasEnemies = enemies.Length > 0;

        // Also clean up our tracking list
        enemiesAlive.RemoveAll(enemy => enemy == null);

        // Debug log to track enemy count
        if (hasEnemies)
        {
            Debug.Log($"[WaveSpawner] {enemies.Length} enemies still alive");
        }

        return hasEnemies;
    }

    IEnumerator SpawnWave(Wave wave)
    {
        Debug.Log($"[WaveSpawner] Starting wave {currentWaveIndex + 1}");
        UpdateWaveUI($"Wave {currentWaveIndex + 1} Starting!");

        state = SpawnState.SPAWNING;

        // Spawn all enemy types in this wave
        foreach (Wave.EnemySpawn enemySpawn in wave.enemies)
        {
            for (int i = 0; i < enemySpawn.count; i++)
            {
                SpawnEnemy(enemySpawn.enemyPrefab);
                yield return new WaitForSeconds(1f / enemySpawn.spawnRate);
            }
        }

        state = SpawnState.WAITING;
        Debug.Log($"[WaveSpawner] Wave {currentWaveIndex + 1} spawning completed, waiting for completion...");
        yield break;
    }

    void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[WaveSpawner] Enemy prefab is null!");
            return;
        }

        Vector3 spawnPos = FindValidSpawnPosition();
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[WaveSpawner] Could not find valid spawn position!");
            return;
        }

        // Spawn enemy
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Ensure enemy has required components
        if (enemy.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
        {
            Debug.LogError("[WaveSpawner] Enemy prefab missing NavMeshAgent component!");
            Destroy(enemy);
            return;
        }

        // Tag the enemy for wave completion checking
        enemy.tag = "Enemy";

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

        // Track this enemy for wave completion
        enemiesAlive.Add(enemy);

        Debug.Log($"[WaveSpawner] Spawned {prefab.name} at {spawnPos}");
    }

    Vector3 FindValidSpawnPosition()
    {
        // Try multiple times to find a valid position
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            // Generate random position around spawner
            Vector3 randomPos = transform.position + Random.insideUnitSphere * spawnSearchRadius;
            randomPos.y = 0; // Keep on ground level

            // Use NavMesh.SamplePosition to find valid position on NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(randomPos, out hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Vector3 candidatePos = hit.position;

                // Check minimum distance from other spawned enemies
                bool tooClose = false;
                foreach (var spawnedEnemy in enemiesAlive)
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
        UnityEngine.AI.NavMeshHit fallbackHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out fallbackHit, spawnSearchRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            return fallbackHit.position;
        }

        return Vector3.zero; // No valid position found
    }

    void UpdateWaveUI(string message)
    {
        // Support both Unity UI Text and TextMeshPro
        if (waveTextTMP != null)
        {
            waveTextTMP.text = message;
            waveTextTMP.gameObject.SetActive(true);

            // Auto-hide message after display time
            StartCoroutine(HideWaveText());
        }
        else if (waveText != null)
        {
            waveText.text = message;
            waveText.gameObject.SetActive(true);

            // Auto-hide message after display time
            StartCoroutine(HideWaveText());
        }

        Debug.Log($"[WaveSpawner] UI: {message}");
    }

    IEnumerator HideWaveText()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        if (waveTextTMP != null)
        {
            waveTextTMP.gameObject.SetActive(false);
        }
        else if (waveText != null)
        {
            waveText.gameObject.SetActive(false);
        }
    }

    // Debug method to visualize spawn area
    void OnDrawGizmosSelected()
    {
        // Draw spawn search radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnSearchRadius);

        // Draw minimum distance between enemies
        Gizmos.color = Color.red;
        foreach (var enemy in enemiesAlive)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, minDistanceBetweenEnemies);
            }
        }
    }

    // Public methods for debugging/testing
    public void ForceNextWave()
    {
        if (currentWaveIndex < waves.Length - 1)
        {
            currentWaveIndex++;
            waveCountdown = 0;
            Debug.Log($"[WaveSpawner] Forced to wave {currentWaveIndex + 1}");
        }
    }

    public void RestartWaves()
    {
        // Clean up existing enemies
        foreach (var enemy in enemiesAlive)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        enemiesAlive.Clear();

        // Reset counters
        currentWaveIndex = 0;
        waveCountdown = 2f;
        state = SpawnState.COUNTING;
        isWaveActive = false;
        enabled = true;

        Debug.Log("[WaveSpawner] Restarted wave system");
    }
}
