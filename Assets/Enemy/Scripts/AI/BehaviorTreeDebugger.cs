using UnityEngine;

/// <summary>
/// Debug tool để visualize và test Enemy AI
/// Gắn vào Enemy để xem real-time AI decisions
/// </summary>
public class BehaviorTreeDebugger : MonoBehaviour
{
    private BaseEnemyAI enemyAI;

    [Header("Debug Display")]
    public bool showDebugInfo = true;
    public bool showGizmos = true;
    public Color detectionColor = Color.yellow;
    public Color attackColor = Color.red;
    public Color patrolColor = Color.blue;
    public Color targetLineColor = Color.green;

    [Header("State Info (Read Only)")]
    [SerializeField] private string currentState = "Initializing...";
    [SerializeField] private float distanceToTarget = 0f;
    [SerializeField] private string targetName = "None";
    [SerializeField] private bool hasTarget = false;

    private Transform currentTarget;
    private GUIStyle guiStyle;

    void Start()
    {
        enemyAI = GetComponent<BaseEnemyAI>();

        // Setup GUI style
        guiStyle = new GUIStyle();
        guiStyle.fontSize = 12;
        guiStyle.normal.textColor = Color.white;
        guiStyle.alignment = TextAnchor.MiddleCenter;
    }

    void Update()
    {
        if (enemyAI == null) return;

        // Get current target from enemy AI
        UpdateDebugInfo();
    }

    void UpdateDebugInfo()
    {
        // Use BaseEnemyAI state and target
        if (enemyAI.player != null)
        {
            currentTarget = enemyAI.player;
            hasTarget = true;
            targetName = currentTarget.name;
            distanceToTarget = enemyAI.GetDistanceToPlayer();

            // Get current state from BaseEnemyAI
            var state = enemyAI.GetCurrentState();
            switch (state)
            {
                case BaseEnemyAI.EnemyState.Idle:
                    currentState = "😴 IDLE";
                    break;
                case BaseEnemyAI.EnemyState.Patrol:
                    currentState = "🚶 PATROLLING";
                    break;
                case BaseEnemyAI.EnemyState.Chase:
                    currentState = "🏃 CHASING";
                    break;
                case BaseEnemyAI.EnemyState.Return:
                    currentState = "🏠 RETURNING";
                    break;
                case BaseEnemyAI.EnemyState.Attack:
                    currentState = "⚔️ ATTACKING";
                    break;
                case BaseEnemyAI.EnemyState.Attacking:
                    currentState = "🗡️ ATTACKING";
                    break;
                case BaseEnemyAI.EnemyState.Dead:
                    currentState = "💀 DEAD";
                    break;
                default:
                    currentState = "❓ UNKNOWN";
                    break;
            }
        }
        else
        {
            currentTarget = null;
            hasTarget = false;
            targetName = "None";
            distanceToTarget = 0f;
            currentState = "❓ NO TARGET";
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || enemyAI == null) return;

        Vector3 position = transform.position;

        // Detection Range
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(position, enemyAI.detectionRadius);

        // Attack Range
        Gizmos.color = attackColor;
        Gizmos.DrawWireSphere(position, enemyAI.attackRange);

        // Patrol Radius
        Gizmos.color = patrolColor;
        Gizmos.DrawWireSphere(position, enemyAI.patrolRadius);

        // Line to target
        if (currentTarget != null)
        {
            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(position + Vector3.up, currentTarget.position + Vector3.up);

            // Target sphere
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }

        // Patrol points - BaseEnemyAI doesn't use explicit patrol points
        // if (enemyAI has patrol points) - would need to add to BaseEnemyAI

        // Forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(position + Vector3.up, transform.forward * 2f);
    }

    void OnGUI()
    {
        if (!showDebugInfo || enemyAI == null) return;

        // Get screen position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 3f);

        if (screenPos.z > 0) // In front of camera
        {
            // Background box
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(screenPos.x - 100, Screen.height - screenPos.y - 60, 200, 80), "");

            // Text info
            GUI.color = Color.white;
            string info = $"<b>{gameObject.name}</b>\n" +
                         $"State: {currentState}\n" +
                         $"Target: {targetName}\n" +
                         $"Distance: {distanceToTarget:F1}m\n" +
                         $"Has Target: {hasTarget}";

            GUI.Label(new Rect(screenPos.x - 95, Screen.height - screenPos.y - 55, 190, 70), info, guiStyle);
        }
    }
}
