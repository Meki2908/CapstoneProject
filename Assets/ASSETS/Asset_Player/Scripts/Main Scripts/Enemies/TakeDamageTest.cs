using UnityEngine;
using DamageNumbersPro;

public class TakeDamageTest : MonoBehaviour
{
    [Header("Damage Number Settings")]
    public DamageNumber damageNumberPrefab;

    [Header("Visual Feedback")]
    public GameObject hitEffect;
    public float hitEffectLifetime = 1f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    [Header("Effectiveness Settings")]
#pragma warning disable CS0414 // Unused fields kept for future effect implementations
    [SerializeField] float thrownAwayForce = 5f;
    [SerializeField] float knockupForce = 3f;
    [SerializeField] float pullForce = 2f;
    [SerializeField] float tornadoRotationSpeed = 180f;
    [SerializeField] float tornadoDuration = 2f;
#pragma warning restore CS0414

    [Header("Enemy Detection Settings")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float detectionAngle = 120f; // Field of view angle
    [SerializeField] private LayerMask playerLayerMask = -1;
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private bool enableDetection = true;

    [Header("Detection Debug")]
    [SerializeField] private bool showDetectionGizmos = true;

    [Header("Raycast Damage Settings")]
    [SerializeField] private float raycastRange = 5f;
    [SerializeField] private float damagePerHit = 1f;
    [SerializeField] private float damageInterval = 2f; // Damage every 2 seconds
    [SerializeField] private bool enableRaycastDamage = true;

    [Header("Enemy Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isAlive = true;

    [Header("Health Bar Settings")]
    [SerializeField] private bool useHealthBar = false;
    [SerializeField] private string bossName = "";
    [SerializeField] private Color healthBarColor = Color.red;

    [Header("EXP Reward Settings")]
    [Tooltip("Amount of EXP granted when this enemy is defeated")]
    [SerializeField] private float expReward = 1000f;

    // Detection state
    private Transform player;
    private Character playerCharacter;
    private PlayerHealth playerHealth;
    private WeaponController playerWeaponController;
    private bool hasDetectedPlayer = false;
    private float lastDetectionTime = 0f;
    private float detectionCooldown = 0.5f; // Minimum time between detection checks

    // Raycast damage state
    private float lastDamageTime = 0f;

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        isAlive = true;

        // Ensure we have a damage number prefab
        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("[TakeDamageTest] No DamageNumber prefab assigned! Please assign one in the inspector.");
        }

        // Find player for detection
        if (enableDetection || enableRaycastDamage)
        {
            playerCharacter = FindFirstObjectByType<Character>();
            if (playerCharacter != null)
            {
                player = playerCharacter.transform;
                playerHealth = playerCharacter.GetComponent<PlayerHealth>();
                playerWeaponController = playerCharacter.GetComponent<WeaponController>();

                if (playerHealth == null)
                {
                    Debug.LogWarning("[TakeDamageTest] PlayerHealth component not found on Character!");
                }

                Debug.Log("[TakeDamageTest] Player found for detection");
            }
            else
            {
                Debug.LogWarning("[TakeDamageTest] No Character found in scene for detection");
            }
        }
    }

    void Update()
    {
        // Don't process if dead
        if (!isAlive) return;

        if (enableDetection && player != null)
        {
            CheckForPlayer();
        }

        // Raycast damage check every interval
        if (enableRaycastDamage && player != null && playerHealth != null)
        {
            if (Time.time - lastDamageTime >= damageInterval)
            {
                ActiveDamage();
            }
        }
    }

    #region Player Detection
    private void CheckForPlayer()
    {
        // Cooldown to prevent excessive detection checks
        if (Time.time - lastDetectionTime < detectionCooldown) return;
        lastDetectionTime = Time.time;

        bool wasDetected = hasDetectedPlayer;

        // Check distance
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange)
        {
            hasDetectedPlayer = false;
            if (wasDetected)
            {
                Debug.Log($"[TakeDamageTest] {gameObject.name} lost sight of player (too far: {distance:F1}m)");
                OnPlayerLost();
            }
            return;
        }

        // Check angle (field of view)
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > detectionAngle / 2f)
        {
            hasDetectedPlayer = false;
            if (wasDetected)
            {
                Debug.Log($"[TakeDamageTest] {gameObject.name} lost sight of player (out of FOV: {angle:F1}°)");
                OnPlayerLost();
            }
            return;
        }

        // Check line of sight (no obstacles)
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Eye level
        Vector3 rayDirection = (player.position + Vector3.up * 1.5f) - rayOrigin;

        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out hit, distance, obstacleLayerMask))
        {
            // Check if hit is the player
            if (hit.collider.transform != player)
            {
                hasDetectedPlayer = false;
                if (wasDetected)
                {
                    Debug.Log($"[TakeDamageTest] {gameObject.name} lost sight of player (obstacle: {hit.collider.name})");
                    OnPlayerLost();
                }
                return;
            }
        }

        // Player is detected
        hasDetectedPlayer = true;
        if (!wasDetected)
        {
            Debug.Log($"[TakeDamageTest] {gameObject.name} detected player! (Distance: {distance:F1}m, Angle: {angle:F1}°)");
            OnPlayerDetected();
        }
    }

    private void OnPlayerDetected()
    {
        // Notify player's EnemyDetection that this enemy detected the player
        var playerEnemyDetection = playerCharacter.GetComponent<EnemyDetection>();
        if (playerEnemyDetection != null)
        {
            playerEnemyDetection.OnEnemyDetectedPlayer(transform);
        }
    }

    private void OnPlayerLost()
    {
        // Notify player's EnemyDetection that this enemy lost the player
        var playerEnemyDetection = playerCharacter.GetComponent<EnemyDetection>();
        if (playerEnemyDetection != null)
        {
            playerEnemyDetection.OnEnemyLostPlayer(transform);
        }
    }
    #endregion

    #region Raycast Damage
    /// <summary>
    /// Active damage method - casts a raycast and damages player if in range
    /// Only enemies can damage players, not camera or other objects with this script
    /// </summary>
    public void ActiveDamage()
    {
        // Prevent camera from damaging player - only allow enemies
        if (IsCameraObject())
        {
            if (showDebugInfo)
            {
                Debug.Log($"[TakeDamageTest] ActiveDamage called from camera object - ignoring to prevent camera damage");
            }
            return;
        }

        if (player == null || playerHealth == null)
        {
            Debug.LogWarning("[TakeDamageTest] Cannot perform ActiveDamage - player or PlayerHealth not found!");
            return;
        }

        // Check if player is alive
        if (!playerHealth.IsAlive)
        {
            return; // Don't damage if player is already dead
        }

        // Early check: Skip raycast if player is invincible (dashing)
        if (playerCharacter != null && playerCharacter.IsDashing)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[TakeDamageTest] Player is dashing (invincible) - skipping raycast damage");
            }
            // Update lastDamageTime to prevent spam attempts
            lastDamageTime = Time.time;
            return; // Don't even cast raycast if player is invincible
        }

        // Early check: Skip raycast if player is on "Nothing" layer (dash invincibility)
        if (player != null && player.gameObject.layer == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[TakeDamageTest] Player is on Nothing layer (invincible) - skipping raycast damage");
            }
            // Update lastDamageTime to prevent spam attempts
            lastDamageTime = Time.time;
            return; // Don't even cast raycast if player layer is Nothing
        }

        // Cast ray from enemy to player
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Enemy eye level
        Vector3 rayDirection = (player.position + Vector3.up * 1.5f) - rayOrigin;
        float distance = rayDirection.magnitude;

        // Only damage if player is within raycast range
        if (distance > raycastRange)
        {
            return;
        }

        // Perform raycast - use RaycastAll to find all hits, then filter out camera
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection.normalized, raycastRange, playerLayerMask);

        // Sort hits by distance to get closest first
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Find first valid hit (not camera)
        foreach (RaycastHit hit in hits)
        {
            // Skip camera colliders - check if the hit has a Camera component or is a child of a camera
            if (hit.collider.GetComponent<Camera>() != null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[TakeDamageTest] Skipping camera collider: {hit.collider.name}");
                }
                continue;
            }

            // Check parent hierarchy for camera
            Transform parent = hit.collider.transform.parent;
            bool isCameraChild = false;
            while (parent != null)
            {
                if (parent.GetComponent<Camera>() != null)
                {
                    isCameraChild = true;
                    break;
                }
                parent = parent.parent;
            }

            if (isCameraChild)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[TakeDamageTest] Skipping camera child collider: {hit.collider.name}");
                }
                continue;
            }

            // Check if we hit the player
            if (hit.collider.transform == player || hit.collider.transform.IsChildOf(player))
            {
                // Check if player is invincible (dashing or layer is Nothing)
                if (playerCharacter != null && playerCharacter.IsDashing)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[TakeDamageTest] Player is dashing (invincible) - damage ignored");
                    }
                    // Still update lastDamageTime to prevent spam, but don't damage
                    lastDamageTime = Time.time;
                    return; // Found player but invincible, exit
                }

                // Check if player is on "Nothing" layer (dash invincibility via layer)
                if (hit.collider.gameObject.layer == 0) // "Nothing" layer
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[TakeDamageTest] Player is on Nothing layer (invincible) - damage ignored");
                    }
                    // Still update lastDamageTime to prevent spam, but don't damage
                    lastDamageTime = Time.time;
                    return; // Found player but invincible via layer, exit
                }

                // Player is not invincible - apply damage
                playerHealth.TakeDamage(damagePerHit, hit.point);

                lastDamageTime = Time.time;

                if (showDebugInfo)
                {
                    Debug.Log($"[TakeDamageTest] {gameObject.name} damaged player for {damagePerHit} via raycast! Player HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
                }
                return; // Found player, exit
            }
        }

        // If we got here, raycast didn't hit player (probably hit camera or other collider)
        if (showDebugInfo && hits.Length > 0)
        {
            Debug.Log($"[TakeDamageTest] Raycast hit {hits.Length} collider(s) but none were valid player colliders");
        }
    }

    /// <summary>
    /// Check if this object is a camera or part of camera hierarchy
    /// </summary>
    private bool IsCameraObject()
    {
        // Check if this object has Camera component
        if (GetComponent<Camera>() != null)
        {
            return true;
        }

        // Check parent hierarchy for camera
        Transform current = transform;
        while (current != null)
        {
            if (current.GetComponent<Camera>() != null)
            {
                return true;
            }
            current = current.parent;
        }

        return false;
    }
    #endregion

    // Simple damage method for testing normal attacks (backward compatibility)
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, WeaponType.Sword, false);
    }

    // Overload with weapon type and crit status
    public void TakeDamage(float damage, WeaponType weaponType, bool isCrit)
    {
        if (!isAlive)
        {
            return; // Already dead, don't process damage
        }

        // Apply damage
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        // Visual feedback
        if (hitEffect != null)
        {
            var hit = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(hit, hitEffectLifetime);
        }

        // Spawn damage number using DamageTextManager
        if (DamageTextManager.Instance != null)
        {
            DamageTextManager.Instance.SpawnDamageText(transform.position, damage, weaponType, isCrit);
        }
        else if (damageNumberPrefab != null)
        {
            // Fallback to old system if DamageTextManager not available
            Vector3 spawnPosition = transform.position + Vector3.up * 2f;
            var damageNumber = damageNumberPrefab.Spawn(spawnPosition, damage);
            damageNumber.SetColor(isCrit ? Color.yellow : Color.red);
            damageNumber.SetScale(isCrit ? 1.5f : 1.2f);
        }
        else
        {
            Debug.LogWarning("[TakeDamageTest] Cannot spawn damage number - no DamageTextManager or prefab assigned!");
        }

        // Check for death
        if (currentHealth <= 0f && isAlive)
        {
            Die();
        }
        else if (showDebugInfo)
        {
            Debug.Log($"[TakeDamageTest] {gameObject.name} took {damage} damage (weapon: {weaponType}, crit: {isCrit}). HP: {currentHealth}/{maxHealth}");
        }
    }

    private void Die()
    {
        if (!isAlive) return; // Already dead

        isAlive = false;
        currentHealth = 0f;

        if (showDebugInfo)
        {
            Debug.Log($"[TakeDamageTest] {gameObject.name} has been defeated!");
        }

        // Grant EXP to player's current weapon
        if (WeaponMasteryManager.Instance != null && playerWeaponController != null)
        {
            WeaponSO currentWeapon = playerWeaponController.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                WeaponMasteryManager.Instance.AddExp(currentWeapon.weaponType, expReward, currentWeapon);

                if (showDebugInfo)
                {
                    Debug.Log($"[TakeDamageTest] Granted {expReward} EXP to {currentWeapon.weaponType} weapon!");
                }
            }
            else
            {
                // If no weapon equipped, grant EXP to all weapons (or you can choose a default)
                if (showDebugInfo)
                {
                    Debug.LogWarning("[TakeDamageTest] No weapon equipped, cannot grant EXP!");
                }
            }
        }

        // Disable enemy behavior
        if (enableDetection)
        {
            SetDetectionEnabled(false);
        }
        enableRaycastDamage = false;

        // You can add death animation, effects, or destroy the object here
        // For now, we'll just disable the enemy
        // Destroy(gameObject, 2f); // Destroy after 2 seconds (optional)
    }


    #region Public API
    public bool HasDetectedPlayer()
    {
        return hasDetectedPlayer;
    }

    public void SetDetectionEnabled(bool enabled)
    {
        enableDetection = enabled;
        if (!enabled && hasDetectedPlayer)
        {
            OnPlayerLost();
            hasDetectedPlayer = false;
        }
    }

    public void ForceDetectPlayer()
    {
        if (player != null)
        {
            hasDetectedPlayer = true;
            OnPlayerDetected();
        }
    }

    public void ForceLosePlayer()
    {
        if (hasDetectedPlayer)
        {
            hasDetectedPlayer = false;
            OnPlayerLost();
        }
    }

    // Health API
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => isAlive;
    public float GetHealthPercentage() => maxHealth > 0 ? currentHealth / maxHealth : 0f;

    // Health Bar API (for boss enemies) - Added for Golem boss compatibility
    public bool UseHealthBar
    {
        get => useHealthBar;
        set => useHealthBar = value;
    }
    public string BossName
    {
        get => bossName;
        set => bossName = value;
    }
    public float MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }
    public float CurrentHealth
    {
        get => currentHealth;
        set => currentHealth = value;
    }
    public Color HealthBarColor
    {
        get => healthBarColor;
        set => healthBarColor = value;
    }

    // Raycast Damage API
    public bool EnableRaycastDamage { get => enableRaycastDamage; set => enableRaycastDamage = value; }
    public void DisableRaycastDamage() => enableRaycastDamage = false;

    // EXP API
    public void SetExpReward(float exp)
    {
        expReward = exp;
    }

    public float GetExpReward() => expReward;
    #endregion

    #region Visual Debug
    private void OnDrawGizmosSelected()
    {
        if (!showDetectionGizmos) return;

        // Draw detection range
        Gizmos.color = hasDetectedPlayer ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw field of view
        Gizmos.color = hasDetectedPlayer ? Color.red : Color.yellow;
        Vector3 leftBoundary = Quaternion.AngleAxis(-detectionAngle / 2f, Vector3.up) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.AngleAxis(detectionAngle / 2f, Vector3.up) * transform.forward * detectionRange;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // Draw line to player if detected
        if (hasDetectedPlayer && player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position + Vector3.up * 1.5f);
        }

        // Draw damage number spawn position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

        // Draw raycast damage range
        if (enableRaycastDamage)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, raycastRange);

            // Draw raycast direction to player
            if (player != null)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
                Vector3 rayDirection = (player.position + Vector3.up * 1.5f) - rayOrigin;
                if (rayDirection.magnitude <= raycastRange)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(rayOrigin, player.position + Vector3.up * 1.5f);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDetectionGizmos) return;

        // Draw detection range (always visible)
        Gizmos.color = hasDetectedPlayer ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
    #endregion
}
