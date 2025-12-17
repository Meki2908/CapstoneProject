using UnityEngine;

/// <summary>
/// Debug tool để visualize và test Behavior Tree
/// Gắn vào Enemy để xem real-time AI decisions
/// </summary>
public class BehaviorTreeDebugger : MonoBehaviour
{
    private EnemyBT enemyBT;

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
        enemyBT = GetComponent<EnemyBT>();

        // Setup GUI style
        guiStyle = new GUIStyle();
        guiStyle.fontSize = 12;
        guiStyle.normal.textColor = Color.white;
        guiStyle.alignment = TextAnchor.MiddleCenter;
    }

    void Update()
    {
        if (enemyBT == null) return;

        // Get current target from behavior tree
        // (Giả sử bạn expose public property trong EnemyBT)
        UpdateDebugInfo();
    }

    void UpdateDebugInfo()
    {
        // Detect current target (simplified)
        Collider[] hits = Physics.OverlapSphere(transform.position, enemyBT.detectionRange, enemyBT.targetLayer);

        if (hits.Length > 0)
        {
            currentTarget = hits[0].transform;
            hasTarget = true;
            targetName = currentTarget.name;
            distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

            // Determine state
            if (distanceToTarget <= enemyBT.attackRange)
            {
                currentState = "🗡️ ATTACKING";
            }
            else if (distanceToTarget <= enemyBT.detectionRange)
            {
                currentState = "🏃 CHASING";
            }
            else
            {
                currentState = "👁️ DETECTING";
            }
        }
        else
        {
            currentTarget = null;
            hasTarget = false;
            targetName = "None";
            distanceToTarget = 0f;
            currentState = "🚶 PATROLLING";
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || enemyBT == null) return;

        Vector3 position = transform.position;

        // Detection Range
        Gizmos.color = detectionColor;
        Gizmos.DrawWireSphere(position, enemyBT.detectionRange);

        // Attack Range
        Gizmos.color = attackColor;
        Gizmos.DrawWireSphere(position, enemyBT.attackRange);

        // Patrol Radius
        Gizmos.color = patrolColor;
        Gizmos.DrawWireSphere(position, enemyBT.patrolRadius);

        // Line to target
        if (currentTarget != null)
        {
            Gizmos.color = targetLineColor;
            Gizmos.DrawLine(position + Vector3.up, currentTarget.position + Vector3.up);

            // Target sphere
            Gizmos.DrawWireSphere(currentTarget.position, 0.5f);
        }

        // Patrol points
        if (enemyBT.patrolPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in enemyBT.patrolPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.3f);
                    Gizmos.DrawLine(position, point.position);
                }
            }
        }

        // Forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(position + Vector3.up, transform.forward * 2f);
    }

    void OnGUI()
    {
        if (!showDebugInfo || enemyBT == null) return;

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
                         $"Distance: {distanceToTarget:F1}m";

            GUI.Label(new Rect(screenPos.x - 95, Screen.height - screenPos.y - 55, 190, 70), info, guiStyle);
        }
    }
}
