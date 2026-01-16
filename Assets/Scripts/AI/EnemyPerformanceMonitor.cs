using UnityEngine;

/// <summary>
/// Monitors performance with many enemies and provides optimization recommendations
/// Attach to a GameObject in your scene to monitor enemy performance
/// </summary>
public class EnemyPerformanceMonitor : MonoBehaviour
{
    [Header("Performance Monitoring")]
    public bool enableMonitoring = true;
    public float updateInterval = 1.0f;
    public bool showGUI = true;

    [Header("GUI Settings")]
    public int fontSize = 16;
    public bool boldText = true;

    [Header("Performance Thresholds")]
    public int maxRecommendedEnemies = 15;
    public float minAcceptableFPS = 30f;
    public float goodFPS = 50f;

    // Performance data
    private float currentFPS;
    private int enemyCount;
    private float lastUpdateTime;
    private float[] fpsHistory = new float[10];
    private int fpsHistoryIndex = 0;

    void Start()
    {
        if (!enableMonitoring) return;

        Debug.Log("[EnemyPerformanceMonitor] Started monitoring enemy performance");
        Debug.Log("[EnemyPerformanceMonitor] Recommendations for 10+ enemies:");
        Debug.Log("- BaseEnemyAI.ANIMATOR_UPDATE_INTERVAL = 0.2f");
        Debug.Log("- BaseEnemyAI.STATE_CHANGE_COOLDOWN = 1.0f");
        Debug.Log("- EnemyContactDamage.rangeCheckInterval = 0.1f");
        Debug.Log("- Disable debug logging in production");
        Debug.Log("- Use distance-based update throttling");
    }

    void Update()
    {
        if (!enableMonitoring) return;

        // Calculate FPS
        currentFPS = 1.0f / Time.deltaTime;

        // Store in history for averaging
        fpsHistory[fpsHistoryIndex] = currentFPS;
        fpsHistoryIndex = (fpsHistoryIndex + 1) % fpsHistory.Length;

        // Update enemy count periodically
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateEnemyCount();
            CheckPerformance();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateEnemyCount()
    {
        // Count enemies by finding BaseEnemyAI components
        BaseEnemyAI[] enemies = FindObjectsByType<BaseEnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        enemyCount = enemies.Length;
    }

    void CheckPerformance()
    {
        float avgFPS = CalculateAverageFPS();

        // Performance assessment
        if (enemyCount > maxRecommendedEnemies)
        {
            Debug.LogWarning($"[EnemyPerformanceMonitor] HIGH ENEMY COUNT: {enemyCount} enemies (recommended max: {maxRecommendedEnemies})");
            Debug.LogWarning("[EnemyPerformanceMonitor] Consider implementing enemy pooling or LOD system");
        }

        if (avgFPS < minAcceptableFPS)
        {
            Debug.LogError($"[EnemyPerformanceMonitor] POOR PERFORMANCE: {avgFPS:F1} FPS with {enemyCount} enemies");
            Debug.LogError("[EnemyPerformanceMonitor] Recommendations:");
            Debug.LogError("- Increase ANIMATOR_UPDATE_INTERVAL to 0.3f");
            Debug.LogError("- Increase STATE_CHANGE_COOLDOWN to 1.5f");
            Debug.LogError("- Disable useDamageRange on distant enemies");
            Debug.LogError("- Implement enemy culling for off-screen enemies");
        }
        else if (avgFPS < goodFPS)
        {
            Debug.LogWarning($"[EnemyPerformanceMonitor] Moderate performance: {avgFPS:F1} FPS with {enemyCount} enemies");
            Debug.LogWarning("[EnemyPerformanceMonitor] Consider optimizations for better performance");
        }
        else
        {
            Debug.Log($"[EnemyPerformanceMonitor] Good performance: {avgFPS:F1} FPS with {enemyCount} enemies ✓");
        }
    }

    float CalculateAverageFPS()
    {
        float sum = 0f;
        int count = 0;
        for (int i = 0; i < fpsHistory.Length; i++)
        {
            if (fpsHistory[i] > 0)
            {
                sum += fpsHistory[i];
                count++;
            }
        }
        return count > 0 ? sum / count : currentFPS;
    }

    void OnGUI()
    {
        if (!showGUI || !enableMonitoring) return;

        // Create customizable font style
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        if (boldText)
            style.fontStyle = FontStyle.Bold;

        int lineHeight = fontSize + 4; // Add some padding

        GUI.color = Color.white;
        GUI.Label(new Rect(10, 10, 400, lineHeight), $"Enemies: {enemyCount}", style);
        GUI.Label(new Rect(10, 10 + lineHeight, 400, lineHeight), $"FPS: {currentFPS:F1}", style);
        GUI.Label(new Rect(10, 10 + lineHeight * 2, 400, lineHeight), $"Avg FPS: {CalculateAverageFPS():F1}", style);

        // Color coding
        if (CalculateAverageFPS() < minAcceptableFPS)
            GUI.color = Color.red;
        else if (CalculateAverageFPS() < goodFPS)
            GUI.color = Color.yellow;
        else
            GUI.color = Color.green;

        GUI.Label(new Rect(10, 10 + lineHeight * 3, 400, lineHeight), $"Performance: {(CalculateAverageFPS() < minAcceptableFPS ? "POOR" : CalculateAverageFPS() < goodFPS ? "MODERATE" : "GOOD")}", style);
    }

    [ContextMenu("Generate Performance Report")]
    public void GeneratePerformanceReport()
    {
        Debug.Log("=== ENEMY PERFORMANCE REPORT ===");
        Debug.Log($"Current FPS: {currentFPS:F1}");
        Debug.Log($"Average FPS: {CalculateAverageFPS():F1}");
        Debug.Log($"Enemy Count: {enemyCount}");
        Debug.Log($"Recommended Max: {maxRecommendedEnemies}");

        if (enemyCount > maxRecommendedEnemies)
        {
            Debug.Log("WARNING: Too many enemies! Consider:");
            Debug.Log("- Enemy object pooling");
            Debug.Log("- Distance-based culling");
            Debug.Log("- Level of Detail (LOD) system");
        }

        if (CalculateAverageFPS() < minAcceptableFPS)
        {
            Debug.Log("CRITICAL: Poor performance! Immediate optimizations needed:");
            Debug.Log("- ANIMATOR_UPDATE_INTERVAL = 0.3f");
            Debug.Log("- STATE_CHANGE_COOLDOWN = 2.0f");
            Debug.Log("- Disable range damage on distant enemies");
            Debug.Log("- Reduce physics raycasts");
        }

        Debug.Log("================================");
    }
}
