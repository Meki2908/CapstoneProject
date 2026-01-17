using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Object pooling system for enemies to avoid Instantiate/Destroy performance issues.
/// </summary>
public class EnemyPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemyPool
    {
        public string enemyType;
        public GameObject enemyPrefab;
        public int initialPoolSize = 10;
        public int maxPoolSize = 50;

        [HideInInspector] public Queue<GameObject> pool = new Queue<GameObject>();
        [HideInInspector] public int activeCount = 0;
    }

    public static EnemyPoolManager Instance { get; private set; }

    [Header("Enemy Pools")]
    public EnemyPool[] enemyPools;

    private Dictionary<string, EnemyPool> poolDictionary;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    void InitializePools()
    {
        poolDictionary = new Dictionary<string, EnemyPool>();

        foreach (var pool in enemyPools)
        {
            poolDictionary[pool.enemyType] = pool;

            // Pre-instantiate initial pool
            for (int i = 0; i < pool.initialPoolSize; i++)
            {
                GameObject enemy = Instantiate(pool.enemyPrefab);
                enemy.SetActive(false);
                enemy.transform.SetParent(transform);
                pool.pool.Enqueue(enemy);
            }
        }
    }

    /// <summary>
    /// Spawn an enemy from the pool
    /// </summary>
    public GameObject SpawnEnemy(string enemyType, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(enemyType))
        {
            Debug.LogError($"Enemy type '{enemyType}' not found in pool!");
            return null;
        }

        EnemyPool pool = poolDictionary[enemyType];

        GameObject enemy = null;

        if (pool.pool.Count > 0)
        {
            // Get from pool
            enemy = pool.pool.Dequeue();
        }
        else if (pool.activeCount < pool.maxPoolSize)
        {
            // Create new if pool is empty but under max limit
            enemy = Instantiate(pool.enemyPrefab);
            enemy.transform.SetParent(transform);
        }
        else
        {
            Debug.LogWarning($"Max pool size reached for {enemyType}, cannot spawn more enemies");
            return null;
        }

        // Setup enemy
        enemy.transform.position = position;
        enemy.transform.rotation = rotation;
        enemy.SetActive(true);
        pool.activeCount++;

        return enemy;
    }

    /// <summary>
    /// Return an enemy to the pool
    /// </summary>
    public void ReturnEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        // Find which pool this enemy belongs to
        foreach (var pool in enemyPools)
        {
            if (pool.enemyPrefab.name == enemy.name.Replace("(Clone)", "").Trim())
            {
                enemy.SetActive(false);
                enemy.transform.SetParent(transform);
                pool.pool.Enqueue(enemy);
                pool.activeCount--;
                return;
            }
        }

        // If not found in any pool, just destroy it
        Debug.LogWarning($"Enemy {enemy.name} not found in any pool, destroying");
        Destroy(enemy);
    }

    /// <summary>
    /// Get pool statistics for debugging
    /// </summary>
    public void LogPoolStats()
    {
        foreach (var pool in enemyPools)
        {
            Debug.Log($"{pool.enemyType}: Active={pool.activeCount}, Pooled={pool.pool.Count}");
        }
    }

    /// <summary>
    /// Pre-warm pools (create additional instances)
    /// </summary>
    public void PrewarmPool(string enemyType, int additionalCount)
    {
        if (!poolDictionary.ContainsKey(enemyType)) return;

        EnemyPool pool = poolDictionary[enemyType];

        for (int i = 0; i < additionalCount; i++)
        {
            if (pool.pool.Count + pool.activeCount >= pool.maxPoolSize) break;

            GameObject enemy = Instantiate(pool.enemyPrefab);
            enemy.SetActive(false);
            enemy.transform.SetParent(transform);
            pool.pool.Enqueue(enemy);
        }
    }
}


















