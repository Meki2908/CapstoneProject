using System;
using UnityEngine;
using DamageNumbersPro;

/// <summary>
/// Enhanced enemy HEALTH and DETECTION system - ENEMY RECEIVES DAMAGE FROM PLAYER
/// This component handles enemy taking damage from player attacks, NOT enemy dealing damage to player.
///
/// For enemy DEALING damage to player, use EnemyContactDamage.cs instead.
///
/// Usage:
/// - For normal attacks: TakeDamage(damage, weaponType, isCrit)
/// - For skills: Use specific skill methods like TakeSwordSkillDamage(damage, isCrit)
/// - For projectiles: TakeProjectileDamage(damage, weaponType, isCrit)
/// - For area effects: TakeAreaDamage(damage, weaponType, damageSourcePosition)
///
/// IMPORTANT: If skills don't work with spider/catfish enemies, ensure that:
/// 1. Skill projectiles/weapons have correct collision layers
/// 2. Skill scripts call the appropriate TakeDamage methods
/// 3. Enemy colliders are set to trigger if needed for projectile detection
///
/// WARNING: ActiveDamage() method is DISABLED to prevent conflicts with EnemyContactDamage.cs
/// </summary>
public class TakeDamageTest : MonoBehaviour
{
    public event Action OnEnemyDied;

    [Header("Damage Number Settings")]
    public DamageNumber damageNumberPrefab;

    [Header("Visual Feedback")]
    public GameObject hitEffect;
    public float hitEffectLifetime = 1f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    [Header("Effectiveness Settings")]
    // [SerializeField] float thrownAwayForce = 5f; // Unused - commented out
    // [SerializeField] float knockupForce = 3f; // Unused - commented out
    // [SerializeField] float pullForce = 2f; // Unused - commented out
    // [SerializeField] float tornadoRotationSpeed = 180f; // Unused - commented out
    // [SerializeField] float tornadoDuration = 2f; // Unused - commented out

    [Header("Enemy Detection Settings")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float detectionBufferZone = 2f; // Buffer zone to prevent spam detection
    [SerializeField] private float detectionAngle = 120f; // Field of view angle
    [SerializeField] private LayerMask playerLayerMask = -1;
    [SerializeField] private LayerMask obstacleLayerMask = -1;
    [SerializeField] private bool enableDetection = true;

    [Header("Detection Debug")]
    [SerializeField] private bool showDetectionGizmos = true;

    [Header("Raycast Damage Settings")]
    [SerializeField] private float raycastRange = 5f;
    [SerializeField] private float raycastBufferZone = 0.5f; // Buffer zone to prevent spam raycast damage
    [SerializeField] private float damagePerHit = 1f;
    [SerializeField] private float damageInterval = 2f; // Damage every 2 seconds
    [SerializeField] private bool enableRaycastDamage = false; // Disabled by default to prevent conflicts with contact damage

    [Header("Enemy Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isAlive = true;

    [Header("Health Bar Settings")]
    [SerializeField] private bool useHealthBar = false;
    [SerializeField] private string bossName = "";
    [SerializeField] private Color healthBarColor = Color.red;


    // Detection state
    private Transform player;
    private Character playerCharacter;
    private PlayerHealth playerHealth;
    private WeaponController playerWeaponController;
    private bool hasDetectedPlayer = false;
    private float lastDetectionTime = 0f;
    private float detectionCooldown = 0.5f; // Minimum time between detection checks
    private float lastDetectionDistance = float.MaxValue; // Track last detection distance for hysteresis

    // Optimization variables
    private float detectionRangeSquared; // Cache squared detection range
    private float raycastRangeSquared; // Cache squared raycast range
    private RaycastHit[] raycastHitsCache = new RaycastHit[10]; // Reusable raycast hits array
    private bool isCameraObject; // Cache camera check result

    // Raycast damage state
    private float lastDamageTime = 0f;
    private float lastRaycastDistance = float.MaxValue; // Track last raycast distance for hysteresis
    private float lastRaycastDamageTime = 0f; // Track last time we damaged player with raycast

    void Start()
    {
        // Initialize health
        currentHealth = maxHealth;
        isAlive = true;

        // Cache squared ranges for faster distance checks
        detectionRangeSquared = detectionRange * detectionRange;
        raycastRangeSquared = raycastRange * raycastRange;

        // Cache camera check result
        isCameraObject = IsCameraObject();

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

        // Raycast damage check every interval - DISABLED to prevent conflicts with EnemyContactDamage
        // TakeDamageTest should only handle enemy receiving damage from player, not dealing damage to player
        /*
        if (enableRaycastDamage && player != null && playerHealth != null)
        {
            if (Time.time - lastDamageTime >= damageInterval)
            {
                Debug.Log($"[TakeDamageTest] {gameObject.name} attempting raycast damage to player");
                ActiveDamage();
            }
        }
        */

        // Also check for skill projectiles hitting this enemy
        CheckForSkillDamage();
    }

    #region Player Detection
    private void CheckForPlayer()
    {
        // Cooldown to prevent excessive detection checks
        if (Time.time - lastDetectionTime < detectionCooldown) return;
        lastDetectionTime = Time.time;

        bool wasDetected = hasDetectedPlayer;

        // Check distance using squared distance (faster)
        Vector3 toPlayer = player.position - transform.position;
        float distanceSquared = toPlayer.sqrMagnitude;
        float currentDistance = Mathf.Sqrt(distanceSquared);

        // Use hysteresis to prevent spam detection at range boundaries
        float effectiveRange = hasDetectedPlayer ?
            detectionRange + detectionBufferZone : // Larger range when already detected
            detectionRange; // Normal range when not detected

        if (distanceSquared > effectiveRange * effectiveRange)
        {
            hasDetectedPlayer = false;
            if (wasDetected)
            {
                Debug.Log($"[TakeDamageTest] {gameObject.name} lost sight of player (too far: {currentDistance:F1}m, effective range: {effectiveRange:F1}m)");
                OnPlayerLost();
            }
            lastDetectionDistance = currentDistance;
            return;
        }

        // Check angle (field of view) - only if within distance
        Vector3 directionToPlayer = toPlayer.normalized;
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

        // Check line of sight (no obstacles) - only perform raycast if other conditions met
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Eye level
        Vector3 rayDirection = (player.position + Vector3.up * 1.5f) - rayOrigin;
        float rayDistance = rayDirection.magnitude;

        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out RaycastHit hit, rayDistance, obstacleLayerMask))
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
            float distance = Mathf.Sqrt(distanceSquared);
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
        // Prevent camera from damaging player - only allow enemies (cached check)
        if (isCameraObject)
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
            lastDamageTime = Time.time;
            return;
        }

        // Early check: Skip raycast if player is on "Nothing" layer (dash invincibility)
        if (player != null && player.gameObject.layer == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[TakeDamageTest] Player is on Nothing layer (invincible) - skipping raycast damage");
            }
            lastDamageTime = Time.time;
            return;
        }

        // Cast ray from enemy to player - use squared distance check first
        // Adjust ray origin based on enemy size and facing direction
        Vector3 rayOrigin = transform.position + Vector3.up * 1.2f; // Lower ray origin to prevent going through player
        Vector3 targetPos = player.position + Vector3.up * 1.0f; // Target player's center mass
        Vector3 toTarget = targetPos - rayOrigin;
        float distanceSquared = toTarget.sqrMagnitude;

        // Use hysteresis for raycast damage to prevent spam
        float effectiveRaycastRange = (lastRaycastDistance <= raycastRange) ?
            raycastRange + raycastBufferZone : // Larger range if was recently in range
            raycastRange; // Normal range

        // Only damage if player is within effective raycast range
        if (distanceSquared > effectiveRaycastRange * effectiveRaycastRange)
        {
            lastRaycastDistance = Mathf.Sqrt(distanceSquared);
            return;
        }

        lastRaycastDistance = Mathf.Sqrt(distanceSquared);

        // Perform raycast using cached array to avoid allocations
        int hitCount = Physics.RaycastNonAlloc(rayOrigin, toTarget.normalized, raycastHitsCache, raycastRange, playerLayerMask);

        // Find first valid hit (not camera) - optimized loop
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHitsCache[i];

            // Skip camera colliders - simplified check (camera components are rare)
            if (hit.collider.CompareTag("MainCamera") || hit.collider.GetComponent<Camera>() != null)
            {
                continue;
            }

            // Check if we hit the player
            Transform hitTransform = hit.collider.transform;
            if (hitTransform == player || hitTransform.IsChildOf(player))
            {
                // Check if player is invincible (dashing or layer is Nothing)
                if (playerCharacter != null && playerCharacter.IsDashing)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[TakeDamageTest] Player is dashing (invincible) - damage ignored");
                    }
                    lastDamageTime = Time.time;
                    return;
                }

                // Check if player is on "Nothing" layer
                if (hit.collider.gameObject.layer == 0)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[TakeDamageTest] Player is on Nothing layer (invincible) - damage ignored");
                    }
                    lastDamageTime = Time.time;
                    return;
                }

                // Check if we recently damaged player to avoid spam
        if (Time.time - lastRaycastDamageTime < 0.5f) // Minimum 0.5 seconds between raycast damage
        {
            return;
        }

        // Player is not invincible - apply damage
        playerHealth.TakeDamage(damagePerHit, hit.point);
        lastDamageTime = Time.time;
        lastRaycastDamageTime = Time.time;

        if (showDebugInfo)
        {
            Debug.Log($"[TakeDamageTest] {gameObject.name} damaged player for {damagePerHit} via raycast! Player HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
        }
        return; // Found player, exit
            }
        }

        // If we got here, raycast didn't hit player
        if (showDebugInfo && hitCount > 0)
        {
            Debug.Log($"[TakeDamageTest] Raycast hit {hitCount} collider(s) but none were valid player colliders");
        }
    }

    /// <summary>
    /// Check if this object is a camera or part of camera hierarchy
    /// </summary>
    public bool IsCameraObject()
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
        TakeDamage(damage, weaponType, isCrit, false);
    }

    // Full overload with weapon type, crit status, and skill flag
    public void TakeDamage(float damage, WeaponType weaponType, bool isCrit, bool isSkill)
    {
        TakeDamage(damage, weaponType, isCrit, isSkill, Vector3.zero);
    }

    // Area damage method (for skills that hit multiple enemies)
    public void TakeAreaDamage(float damage, WeaponType weaponType, Vector3 damageSource)
    {
        TakeDamage(damage, weaponType, false, true, (transform.position - damageSource).normalized);
    }

    // Projectile damage method
    public void TakeProjectileDamage(float damage, WeaponType weaponType, bool isCrit = false)
    {
        TakeDamage(damage, weaponType, isCrit, true);
    }

    // Special skill damage methods for specific weapon types
    public void TakeSwordSkillDamage(float damage, bool isCrit = false)
    {
        TakeDamage(damage, WeaponType.Sword, isCrit, true);
    }

    public void TakeAxeSkillDamage(float damage, bool isCrit = false)
    {
        TakeDamage(damage, WeaponType.Axe, isCrit, true);
    }

    public void TakeMageSkillDamage(float damage, bool isCrit = false)
    {
        TakeDamage(damage, WeaponType.Mage, isCrit, true);
    }

    // Check for skill projectiles hitting this enemy
    private void CheckForSkillDamage()
    {
        // This method can be used for additional skill damage checks if needed
        // Currently relying on SkillDamageHelper for projectile damage
    }

    // Skill damage method (convenience method)
    public void TakeSkillDamage(float damage, WeaponType weaponType, bool isCrit = false)
    {
        TakeDamage(damage, weaponType, isCrit, true);
    }

    // Full overload with weapon type, crit status, skill flag, and hit direction
    public void TakeDamage(float damage, WeaponType weaponType, bool isCrit, bool isSkill, Vector3 hitDirection)
    {
        if (!isAlive)
        {
            return; // Already dead, don't process damage
        }
        
        // === SHIELD BLOCK — boss bất tử khi có shield ===
        var bossSkill = GetComponent<BossMultiSkill>();
        if (bossSkill == null) bossSkill = GetComponentInParent<BossMultiSkill>();
        if (bossSkill != null && bossSkill.ShouldBlockDamage())
        {
            if (showDebugInfo) Debug.Log($"[TakeDamageTest] {gameObject.name} SHIELD BLOCKED {damage} damage!");
            return;
        }

        // Debug skill damage
        if (showDebugInfo && isSkill)
        {
            Debug.Log($"[TakeDamageTest] {gameObject.name} taking SKILL damage: {damage} from {weaponType} (crit: {isCrit})");
        }

        // Apply damage
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth);

        // Visual feedback
        if (hitEffect != null)
        {
            var hit = Instantiate(hitEffect, transform.position + Vector3.up * 1.2f, Quaternion.identity);
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

        // Play hit animation if still alive (non-lethal hit)
        if (currentHealth > 0f)
        {
            PlayHitAnimation(isSkill);
            // === ENEMY HIT SOUND ===
            SoundManager.PlaySound(SoundType.Enemy_GetHit, GetComponent<AudioSource>(), 0.7f);
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
        OnEnemyDied?.Invoke();

        // === ENEMY DIE SOUND ===
        SoundManager.PlaySound(SoundType.Enemy_Die, GetComponent<AudioSource>(), 0.8f);

        if (showDebugInfo)
        {
            Debug.Log($"[TakeDamageTest] {gameObject.name} has been defeated!");
        }

        // EXP vũ khí giờ được cộng khi nhặt EXP Orb (xem ItemDropOrb.OnPickup)
        // Không cộng ngay khi giết nữa

        // Disable enemy behavior
        if (enableDetection)
        {
            SetDetectionEnabled(false);
        }
        enableRaycastDamage = false;

        // === DUNGEON MANIA: Trigger EnemyDamage.Death() ===
        // EnemyDamage quản lý death animation, gold, score, quest progress
        var enemyDamage = GetComponent<EnemyDamage>();
        if (enemyDamage == null) enemyDamage = GetComponentInParent<EnemyDamage>();
        if (enemyDamage == null) enemyDamage = GetComponentInChildren<EnemyDamage>();
        if (enemyDamage != null)
        {
            // Đảm bảo EnemyScript.enemy.helth cũng = 0 
            var enemyScript = GetComponent<EnemyScript>();
            if (enemyScript == null) enemyScript = GetComponentInParent<EnemyScript>();
            if (enemyScript != null && enemyScript.enemy != null)
            {
                enemyScript.enemy.helth.value = 0;
                enemyScript.alive = false;
            }
            // QUAN TRỌNG: Chạy Death trên EnemyDamage's MonoBehaviour, KHÔNG phải TakeDamageTest
            // Vì Death() gọi gameObject.SetActive(false) → nếu chạy trên cùng GameObject thì coroutine bị hủy giữa chừng
            enemyDamage.StartCoroutine(enemyDamage.Death());
            if (showDebugInfo) Debug.Log("[TakeDamageTest] → EnemyDamage.Death() triggered on EnemyDamage!");
            return; // EnemyDamage.Death() handles everything
        }

        // Fallback: Trigger enemy-specific death behaviour
        var deathController = GetComponent<EnemyDeathController>();
        if (deathController != null)
        {
            deathController.Die();
        }
        else
        {
            var spiderAI = GetComponent<SpiderEnemyAI>();
            if (spiderAI != null)
            {
                spiderAI.Die();
            }
            else
            {
                var animator = GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger("Die");
                }
            }
        }

        // Optionally destroy the enemy after a short delay
        // Destroy(gameObject, 3f);
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

    #endregion

    // Try to play "Hit" animation on this enemy if Animator supports it.
    private void PlayHitAnimation(bool isSkill = false)
    {
        // Tìm EnemyScript để dùng animator đúng + interrupt attack
        var enemyScript = GetComponent<EnemyScript>();
        if (enemyScript == null) enemyScript = GetComponentInParent<EnemyScript>();
        
        if (enemyScript == null || enemyScript.animator == null) return;
        
        // Boss: CHỈ bị hit khi dính SKILL, đánh thường KHÔNG bị
        // Dùng isBoss field (set trong Inspector) thay vì check enemyType enum
        bool isBossType = enemyScript.isBoss;
        if (isBossType && !isSkill)
            return; // Boss không bị choáng bởi đánh thường
        
        // Hit animation
        enemyScript.animator.SetBool("hit", true);
        enemyScript.animator.SetBool("attack", false);
        enemyScript.hit = true;
        
        // Dừng di chuyển khi bị hit
        if (enemyScript.navMeshAgent != null && enemyScript.navMeshAgent.isOnNavMesh)
        {
            enemyScript.navMeshAgent.isStopped = true;
        }
        
        // Auto-reset — tránh bị đơ vĩnh viễn
        float staggerTime = isBossType ? 0.6f : 0.4f;
        StartCoroutine(ResetHitState(enemyScript, staggerTime));
    }
    
    private System.Collections.IEnumerator ResetHitState(EnemyScript es, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (es == null || !es.alive) yield break;
        
        es.animator.SetBool("hit", false);
        es.hit = false;
        if (es.navMeshAgent != null && es.navMeshAgent.isOnNavMesh)
        {
            es.navMeshAgent.isStopped = false;
        }
    }

    private System.Collections.IEnumerator ResetAnimatorBool(string param, float delay)
    {
        yield return new WaitForSeconds(delay);
        var animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetBool(param, false);
    }

    #region Visual Debug
    private void OnDrawGizmosSelected()
    {
        if (!showDetectionGizmos) return;

        // Draw detection range
        Gizmos.color = hasDetectedPlayer ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw buffer zone
        if (detectionBufferZone > 0)
        {
            Gizmos.color = hasDetectedPlayer ? new Color(1, 0.5f, 0.5f, 0.3f) : new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, detectionRange + detectionBufferZone);
        }

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
            Gizmos.DrawLine(transform.position + Vector3.up * 1.2f, player.position + Vector3.up * 1.0f);
        }

        // Draw damage number spawn position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

        // Draw raycast damage range
        if (enableRaycastDamage)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, raycastRange);

            // Draw buffer zone for raycast
            if (raycastBufferZone > 0)
            {
                Gizmos.color = new Color(1, 0, 1, 0.3f);
                Gizmos.DrawWireSphere(transform.position, raycastRange + raycastBufferZone);
            }

            // Draw raycast direction to player
            if (player != null)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 1.2f;
                Vector3 rayDirection = (player.position + Vector3.up * 1.0f) - rayOrigin;
                if (rayDirection.magnitude <= raycastRange + raycastBufferZone)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(rayOrigin, player.position + Vector3.up * 1.0f);
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
