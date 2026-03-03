using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Phát hiện enemy bị kẹt trên terrain và tự động warp tới vị trí NavMesh hợp lệ gần player.
/// Thêm component này vào enemy (được DungeonWaveManager tự động thêm khi spawn).
/// 
/// Cách hoạt động:
/// 1. Mỗi checkInterval giây, kiểm tra xem enemy đã di chuyển đáng kể chưa
/// 2. Nếu enemy không di chuyển đủ xa trong stuckTimeThreshold giây → coi là bị kẹt
/// 3. Khi kẹt, tìm vị trí NavMesh hợp lệ gần player và warp enemy tới đó
/// </summary>
public class EnemyStuckDetection : MonoBehaviour
{
    [Header("=== STUCK DETECTION SETTINGS ===")]
    [Tooltip("Thời gian (giây) không di chuyển trước khi coi là bị kẹt")]
    public float stuckTimeThreshold = 3f;
    
    [Tooltip("Khoảng cách tối thiểu phải di chuyển mỗi chu kỳ kiểm tra (mét)")]
    public float minMovementDistance = 0.5f;
    
    [Tooltip("Khoảng cách xa nhất từ player trước khi kích hoạt stuck detection")]
    public float maxDistanceFromPlayer = 5f;
    
    [Tooltip("Bán kính tìm vị trí NavMesh hợp lệ khi warp")]
    public float warpSearchRadius = 15f;
    
    [Tooltip("Khoảng cách warp cách player")]
    public float warpDistanceFromPlayer = 5f;
    
    [Tooltip("Thời gian cooldown giữa các lần warp (giây)")]
    public float warpCooldown = 8f;
    
    [Tooltip("Số lần warp tối đa (-1 = không giới hạn)")]
    public int maxWarpCount = 5;

    // Private variables
    private NavMeshAgent navAgent;
    private EnemyScript enemyScript;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private float lastWarpTime = -999f;
    private int warpCount = 0;
    private bool isInitialized = false;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        enemyScript = GetComponent<EnemyScript>();
        
        if (navAgent == null || enemyScript == null)
        {
            // Thử tìm trên parent nếu không có trên this
            if (navAgent == null) navAgent = GetComponentInParent<NavMeshAgent>();
            if (enemyScript == null) enemyScript = GetComponentInParent<EnemyScript>();
        }
        
        if (navAgent != null)
        {
            lastPosition = transform.position;
            isInitialized = true;
        }
        else
        {
            Debug.LogWarning($"[EnemyStuck] NavMeshAgent not found on {gameObject.name}!");
            enabled = false;
        }
    }

    void Update()
    {
        if (!isInitialized || navAgent == null || enemyScript == null) return;
        if (!enemyScript.alive) return;
        if (enemyScript.target == null) return;
        
        // Chỉ kiểm tra khi enemy đang chase (không bị dừng)
        if (navAgent.isStopped || enemyScript.wait || enemyScript.attack) 
        {
            // Reset timer khi enemy đang tấn công hoặc chờ
            stuckTimer = 0f;
            lastPosition = transform.position;
            return;
        }

        // Kiểm tra khoảng cách di chuyển
        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        float distToPlayer = Vector3.Distance(transform.position, enemyScript.target.position);
        
        // Chỉ detect stuck khi enemy ở xa player (gần player = đang tấn công, không kẹt)
        if (distToPlayer > maxDistanceFromPlayer)
        {
            if (movedDistance < minMovementDistance)
            {
                stuckTimer += Time.deltaTime;
                
                // Kiểm tra NavMesh path status
                bool hasPath = navAgent.hasPath;
                bool pathPending = navAgent.pathPending;
                bool pathComplete = navAgent.pathStatus == NavMeshPathStatus.PathComplete;
                
                if (stuckTimer >= stuckTimeThreshold)
                {
                    // Enemy bị kẹt!
                    Debug.LogWarning($"[EnemyStuck] {gameObject.name} bị kẹt! " +
                        $"Moved: {movedDistance:F2}m in {stuckTimeThreshold}s, " +
                        $"Distance to player: {distToPlayer:F1}m, " +
                        $"HasPath: {hasPath}, PathComplete: {pathComplete}");
                    
                    TryWarpToPlayer();
                    stuckTimer = 0f;
                    lastPosition = transform.position;
                }
            }
            else
            {
                // Enemy di chuyển OK, reset timer
                stuckTimer = 0f;
                lastPosition = transform.position;
            }
        }
        else
        {
            // Gần player, reset
            stuckTimer = 0f;
            lastPosition = transform.position;
        }
    }

    /// <summary>
    /// Warp enemy tới vị trí NavMesh hợp lệ gần player
    /// </summary>
    private void TryWarpToPlayer()
    {
        // Check cooldown
        if (Time.time - lastWarpTime < warpCooldown)
        {
            Debug.Log($"[EnemyStuck] {gameObject.name} warp cooldown active ({warpCooldown - (Time.time - lastWarpTime):F1}s remaining)");
            return;
        }
        
        // Check max warp count
        if (maxWarpCount >= 0 && warpCount >= maxWarpCount)
        {
            Debug.LogWarning($"[EnemyStuck] {gameObject.name} reached max warp count ({maxWarpCount})");
            return;
        }

        Transform playerTarget = enemyScript.target;
        if (playerTarget == null) return;

        // Thử tìm vị trí NavMesh hợp lệ xung quanh player
        Vector3 targetPos = playerTarget.position;
        
        // Thử nhiều hướng xung quanh player
        Vector3[] offsets = new Vector3[]
        {
            Vector3.forward * warpDistanceFromPlayer,
            Vector3.back * warpDistanceFromPlayer,
            Vector3.left * warpDistanceFromPlayer,
            Vector3.right * warpDistanceFromPlayer,
            (Vector3.forward + Vector3.right).normalized * warpDistanceFromPlayer,
            (Vector3.forward + Vector3.left).normalized * warpDistanceFromPlayer,
            (Vector3.back + Vector3.right).normalized * warpDistanceFromPlayer,
            (Vector3.back + Vector3.left).normalized * warpDistanceFromPlayer,
        };

        foreach (Vector3 offset in offsets)
        {
            Vector3 testPos = targetPos + offset;
            NavMeshHit hit;
            
            if (NavMesh.SamplePosition(testPos, out hit, warpSearchRadius, NavMesh.AllAreas))
            {
                // Kiểm tra xem vị trí mới có path hợp lệ tới player không
                NavMeshPath testPath = new NavMeshPath();
                if (NavMesh.CalculatePath(hit.position, targetPos, NavMesh.AllAreas, testPath))
                {
                    if (testPath.status == NavMeshPathStatus.PathComplete)
                    {
                        // Warp!
                        navAgent.Warp(hit.position);
                        navAgent.SetDestination(targetPos);
                        
                        lastWarpTime = Time.time;
                        warpCount++;
                        
                        Debug.Log($"[EnemyStuck] {gameObject.name} warped to {hit.position} " +
                            $"(near player, tries: {warpCount}/{(maxWarpCount >= 0 ? maxWarpCount.ToString() : "∞")})");
                        return;
                    }
                }
            }
        }
        
        // Fallback: warp trực tiếp gần player nếu không tìm được vị trí tốt
        NavMeshHit fallbackHit;
        if (NavMesh.SamplePosition(targetPos, out fallbackHit, warpSearchRadius, NavMesh.AllAreas))
        {
            navAgent.Warp(fallbackHit.position);
            navAgent.SetDestination(targetPos);
            
            lastWarpTime = Time.time;
            warpCount++;
            
            Debug.Log($"[EnemyStuck] {gameObject.name} warped to fallback position {fallbackHit.position}");
        }
        else
        {
            Debug.LogWarning($"[EnemyStuck] {gameObject.name} could not find valid NavMesh position near player!");
        }
    }

    /// <summary>
    /// Reset stuck detection (gọi khi enemy được respawn hoặc re-enable)
    /// </summary>
    public void ResetStuckDetection()
    {
        stuckTimer = 0f;
        warpCount = 0;
        lastWarpTime = -999f;
        lastPosition = transform.position;
    }
}
