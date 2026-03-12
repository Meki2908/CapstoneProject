using UnityEngine;

/// <summary>
/// Detects when player is stuck between enemies and helps them escape.
/// Automatically triggers dash when player is surrounded.
/// </summary>
public class StuckDetection : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private int minEnemiesToTrigger = 2;
    [SerializeField] private float stuckDurationThreshold = 1.5f; // How long player must be stuck before auto-dash
    [SerializeField] private float autoDashCooldown = 3f; // Cooldown between auto-dashes

    [Header("Movement Settings")]
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.3f;

    private Character character;
    private CharacterController controller;
    private float lastStuckTime;
    private float lastAutoDashTime;
    private Vector3 lastPosition;
    private float timeStuck;
    private bool isStuck;

    private void Start()
    {
        character = GetComponent<Character>();
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
        timeStuck = 0f;
        isStuck = false;
    }

    private void Update()
    {
        if (character == null || controller == null) return;

        // Skip if already dashing or in certain states
        // Skip if already dashing or in certain states
        if (character.IsDashing || character.movementSM == null || character.movementSM.currentState == null || character.movementSM.currentState == character.getHit) return;

        DetectStuckCondition();
        HandleStuckEscape();
    }

    private void DetectStuckCondition()
    {
        // Check if player has moved significantly
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);

        if (distanceMoved < 0.1f) // Player barely moved
        {
            timeStuck += Time.deltaTime;

            // Check for enemies nearby
            int nearbyEnemies = CountNearbyEnemies();

            if (nearbyEnemies >= minEnemiesToTrigger && timeStuck >= stuckDurationThreshold)
            {
                if (!isStuck)
                {
                    isStuck = true;
                    lastStuckTime = Time.time;
                    Debug.Log($"[StuckDetection] Player stuck between {nearbyEnemies} enemies for {timeStuck:F1}s");
                }
            }
        }
        else
        {
            // Player moved, reset stuck timer
            timeStuck = 0f;
            isStuck = false;
        }

        lastPosition = transform.position;
    }

    private int CountNearbyEnemies()
    {
        // Find all enemies within detection radius
        TakeDamageTest[] enemies = FindObjectsByType<TakeDamageTest>(FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None);
        int count = 0;

        foreach (TakeDamageTest enemy in enemies)
        {
            if (enemy != null && enemy.IsAlive() && !enemy.IsCameraObject())
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= detectionRadius)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void HandleStuckEscape()
    {
        if (!isStuck || Time.time - lastAutoDashTime < autoDashCooldown) return;

        // Find escape direction (away from nearest enemy)
        Vector3 escapeDirection = FindEscapeDirection();

        if (escapeDirection != Vector3.zero)
        {
            TriggerAutoDash(escapeDirection);
            isStuck = false;
            timeStuck = 0f;
            lastAutoDashTime = Time.time;

            Debug.Log($"[StuckDetection] Auto-dash triggered to escape stuck condition");
        }
    }

    private Vector3 FindEscapeDirection()
    {
        TakeDamageTest[] enemies = FindObjectsByType<TakeDamageTest>(FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None);
        Vector3 totalRepulsion = Vector3.zero;
        int validEnemies = 0;

        foreach (TakeDamageTest enemy in enemies)
        {
            if (enemy != null && enemy.IsAlive() && !enemy.IsCameraObject())
            {
                Vector3 enemyPos = enemy.transform.position;
                float distance = Vector3.Distance(transform.position, enemyPos);

                if (distance <= detectionRadius && distance > 0.1f)
                {
                    // Calculate repulsion direction (away from enemy)
                    Vector3 repulsion = (transform.position - enemyPos).normalized;
                    // Stronger repulsion for closer enemies
                    float strength = 1f / (distance * distance);
                    totalRepulsion += repulsion * strength;
                    validEnemies++;
                }
            }
        }

        if (validEnemies > 0)
        {
            // Normalize and keep horizontal
            totalRepulsion.y = 0;
            return totalRepulsion.normalized;
        }

        return Vector3.zero;
    }

    private void TriggerAutoDash(Vector3 direction)
    {
        // Set player velocity for dash
        character.playerVelocity = direction * dashForce;

        // Temporarily disable gravity during dash
        float originalGravity = character.gravityValue;
        character.gravityValue = 0;

        // Reset after dash duration
        Invoke(nameof(ResetAfterDash), dashDuration);

        // Also trigger dash animation/state if possible
        if (character.dashing != null)
        {
            character.movementSM.ChangeState(character.dashing);
        }
    }

    private void ResetAfterDash()
    {
        character.playerVelocity = Vector3.zero;
        character.gravityValue = -9.81f * character.gravityMultiplier;

        // Return to appropriate state
        if (character.movementSM.currentState == character.dashing)
        {
            character.movementSM.ChangeState(character.combatMove);
        }
    }
}
