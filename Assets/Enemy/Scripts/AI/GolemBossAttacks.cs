using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GOLEM BOSS ATTACKS
/// Chứa tất cả logic cho các attack patterns và abilities của boss
/// Được gọi bởi GolemBossAI và GolemBossAnimator
/// </summary>
public class GolemBossAttacks : MonoBehaviour
{
    [Header("=== DAMAGE VALUES ===")]
    [Tooltip("Damage của đòn đánh thường")]
    public float basicAttackDamage = 50f;
    
    [Tooltip("Damage của combo attack")]
    public float comboAttackDamage = 35f;
    
    [Tooltip("Damage của ground slam")]
    public float groundSlamDamage = 80f;
    
    [Tooltip("Damage của rage attack")]
    public float rageAttackDamage = 100f;
    
    [Header("=== ATTACK RANGES ===")]
    [Tooltip("Bán kính đòn đánh cận chiến")]
    public float meleeAttackRadius = 3f;
    
    [Tooltip("Bán kính ground slam AOE")]
    public float groundSlamRadius = 6f;
    
    [Tooltip("Bán kính rage attack AOE")]
    public float rageAttackRadius = 10f;
    
    [Header("=== KNOCKBACK ===")]
    [Tooltip("Lực đẩy lùi của basic attack")]
    public float basicKnockbackForce = 5f;
    
    [Tooltip("Lực đẩy lùi của ground slam")]
    public float groundSlamKnockbackForce = 15f;
    
    [Tooltip("Lực đẩy lùi của rage attack")]
    public float rageKnockbackForce = 20f;
    
    [Header("=== EFFECTS ===")]
    [Tooltip("Effect khi đánh trúng")]
    public GameObject basicAttackEffect;
    
    [Tooltip("Effect của ground slam")]
    public GameObject groundSlamEffect;
    
    [Tooltip("Effect của rage attack")]
    public GameObject rageAttackEffect;
    
    [Tooltip("Shockwave effect prefab")]
    public GameObject shockwavePrefab;
    
    [Header("=== PROJECTILES ===")]
    [Tooltip("Rock projectile for ranged attacks")]
    public GameObject rockProjectilePrefab;
    
    [Tooltip("Tốc độ của rock projectile")]
    public float rockProjectileSpeed = 15f;
    
    [Header("=== SPAWN POINTS ===")]
    [Tooltip("Điểm spawn cho attack effects (tay phải)")]
    public Transform rightHandTransform;
    
    [Tooltip("Điểm spawn cho attack effects (tay trái)")]
    public Transform leftHandTransform;
    
    [Tooltip("Điểm spawn cho ground slam (dưới chân)")]
    public Transform groundPointTransform;
    
    [Header("=== LAYERS ===")]
    [Tooltip("Layer của player để detect hit")]
    public LayerMask playerLayer = -1;
    
    [Tooltip("Layer của ground để detect impact")]
    public LayerMask groundLayer = -1;
    
    [Header("=== REFERENCES ===")]
    public GolemBossAI bossAI;
    public GolemBossAnimator bossAnimator;
    public GolemBossHealth bossHealth;
    
    [Header("=== DEBUG ===")]
    public bool showDebugLogs = true;
    public bool showDebugGizmos = true;
    
    // Internal
    private Transform currentTarget;
    private bool isExecutingAttack = false;
    
    private void Awake()
    {
        // Auto-find components
        if (bossAI == null) bossAI = GetComponent<GolemBossAI>();
        if (bossAnimator == null) bossAnimator = GetComponent<GolemBossAnimator>();
        if (bossHealth == null) bossHealth = GetComponent<GolemBossHealth>();
        
        // Auto-find spawn points if not assigned
        if (rightHandTransform == null || leftHandTransform == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (var child in children)
            {
                if (child.name.Contains("RightHand") || child.name.Contains("Hand_R"))
                    rightHandTransform = child;
                if (child.name.Contains("LeftHand") || child.name.Contains("Hand_L"))
                    leftHandTransform = child;
            }
        }
        
        // Auto-set layers
        if (playerLayer.value == 0)
            playerLayer = LayerMask.GetMask("Player");
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Default", "Ground");
    }
    
    private void Update()
    {
        // Update current target from AI
        if (bossAI != null)
        {
            currentTarget = bossAI.currentTarget;
        }
    }
    
    #region ATTACK EXECUTIONS
    
    /// <summary>
    /// Execute basic attack
    /// </summary>
    public void ExecuteBasicAttack()
    {
        if (isExecutingAttack) return;
        
        StartCoroutine(BasicAttackRoutine());
    }
    
    private IEnumerator BasicAttackRoutine()
    {
        isExecutingAttack = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] ⚔️ Executing Basic Attack");
        }
        
        // Wait for animation to reach hit frame (will be called by animation event)
        // Or wait fixed time if no animation events
        yield return new WaitForSeconds(0.5f);
        
        isExecutingAttack = false;
    }
    
    /// <summary>
    /// Execute combo attack (2-hit combo)
    /// </summary>
    public void ExecuteComboAttack()
    {
        if (isExecutingAttack) return;
        
        StartCoroutine(ComboAttackRoutine());
    }
    
    private IEnumerator ComboAttackRoutine()
    {
        isExecutingAttack = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 🥊 Executing COMBO Attack");
        }
        
        // First hit
        yield return new WaitForSeconds(0.5f);
        DealDamageInRadius(meleeAttackRadius, comboAttackDamage, basicKnockbackForce);
        SpawnAttackEffect(basicAttackEffect, rightHandTransform);
        
        // Second hit
        yield return new WaitForSeconds(0.6f);
        DealDamageInRadius(meleeAttackRadius, comboAttackDamage, basicKnockbackForce);
        SpawnAttackEffect(basicAttackEffect, leftHandTransform);
        
        isExecutingAttack = false;
    }
    
    /// <summary>
    /// Execute ground slam attack
    /// </summary>
    public void ExecuteGroundSlam()
    {
        if (isExecutingAttack) return;
        
        StartCoroutine(GroundSlamRoutine());
    }
    
    private IEnumerator GroundSlamRoutine()
    {
        isExecutingAttack = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 🌍 Executing GROUND SLAM");
        }
        
        // Wait for slam impact (animation event will call OnGroundSlamImpact)
        yield return new WaitForSeconds(1.2f);
        
        isExecutingAttack = false;
    }
    
    /// <summary>
    /// Execute rage attack (360° AOE)
    /// </summary>
    public void ExecuteRageAttack()
    {
        if (isExecutingAttack) return;
        
        StartCoroutine(RageAttackRoutine());
    }
    
    private IEnumerator RageAttackRoutine()
    {
        isExecutingAttack = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 💥 Executing RAGE ATTACK");
        }
        
        // Wind-up
        yield return new WaitForSeconds(0.8f);
        
        // Release rage wave (will be called by animation event)
        // Or trigger manually
        yield return new WaitForSeconds(0.2f);
        
        isExecutingAttack = false;
    }
    
    #endregion
    
    #region ANIMATION EVENT CALLBACKS
    
    /// <summary>
    /// Called by animation event when attack hits
    /// </summary>
    public void OnAttackHitFrame()
    {
        DealDamageInRadius(meleeAttackRadius, basicAttackDamage, basicKnockbackForce);
        SpawnAttackEffect(basicAttackEffect, rightHandTransform);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 💥 Attack Hit Frame!");
        }
    }
    
    /// <summary>
    /// Called by animation event when ground slam impacts
    /// </summary>
    public void OnGroundSlamImpact()
    {
        // AOE damage
        DealDamageInRadius(groundSlamRadius, groundSlamDamage, groundSlamKnockbackForce);
        
        // Spawn ground slam effect
        Vector3 slamPos = groundPointTransform != null ? groundPointTransform.position : transform.position;
        SpawnAttackEffect(groundSlamEffect, slamPos);
        
        // Create shockwave
        if (shockwavePrefab != null)
        {
            var shockwave = Instantiate(shockwavePrefab, slamPos, Quaternion.identity);
            
            // Animate shockwave expansion
            StartCoroutine(ExpandShockwave(shockwave.transform, groundSlamRadius));
        }
        
        // Camera shake
        CameraShake(0.5f, 0.3f);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 🌍 Ground Slam Impact!");
        }
    }
    
    /// <summary>
    /// Called by animation event when rage wave releases
    /// </summary>
    public void OnRageWaveRelease()
    {
        // Massive AOE damage
        DealDamageInRadius(rageAttackRadius, rageAttackDamage, rageKnockbackForce);
        
        // Spawn rage effect
        SpawnAttackEffect(rageAttackEffect, transform);
        
        // Multiple shockwaves
        if (shockwavePrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(SpawnDelayedShockwave(i * 0.2f, rageAttackRadius * (i + 1) / 3f));
            }
        }
        
        // Big camera shake
        CameraShake(1f, 0.5f);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 💥 RAGE WAVE RELEASED!");
        }
    }
    
    #endregion
    
    #region DAMAGE & EFFECTS
    
    /// <summary>
    /// Deal damage to all players in radius
    /// </summary>
    private void DealDamageInRadius(float radius, float damage, float knockbackForce)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, playerLayer);
        
        if (showDebugLogs)
        {
            Debug.Log($"[GolemBossAttacks] Checking damage in radius {radius}m - Found {hits.Length} targets");
        }
        
        foreach (var hit in hits)
        {
            // Try to damage player
            var playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[GolemBossAttacks] 💥 Hit {hit.name} for {damage} damage");
                }
            }
            
            // Try TakeDamageTest (for testing)
            var takeDamage = hit.GetComponent<TakeDamageTest>();
            if (takeDamage != null)
            {
                takeDamage.TakeDamage(damage);
            }
            
            // Apply knockback
            var rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 knockbackDir = (hit.transform.position - transform.position).normalized;
                knockbackDir.y = 0.5f; // Add upward force
                rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
            }
        }
    }
    
    /// <summary>
    /// Spawn attack effect at position
    /// </summary>
    private void SpawnAttackEffect(GameObject effectPrefab, Transform spawnPoint)
    {
        if (effectPrefab == null) return;
        
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        var effect = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
        Destroy(effect, 2f);
    }
    
    private void SpawnAttackEffect(GameObject effectPrefab, Vector3 position)
    {
        if (effectPrefab == null) return;
        
        var effect = Instantiate(effectPrefab, position, Quaternion.identity);
        Destroy(effect, 2f);
    }
    
    /// <summary>
    /// Expand shockwave animation
    /// </summary>
    private IEnumerator ExpandShockwave(Transform shockwave, float maxRadius)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * maxRadius * 2f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            shockwave.localScale = Vector3.Lerp(startScale, endScale, t);
            
            yield return null;
        }
        
        Destroy(shockwave.gameObject);
    }
    
    /// <summary>
    /// Spawn delayed shockwave
    /// </summary>
    private IEnumerator SpawnDelayedShockwave(float delay, float radius)
    {
        yield return new WaitForSeconds(delay);
        
        if (shockwavePrefab != null)
        {
            Vector3 spawnPos = groundPointTransform != null ? groundPointTransform.position : transform.position;
            var shockwave = Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);
            StartCoroutine(ExpandShockwave(shockwave.transform, radius));
        }
    }
    
    /// <summary>
    /// Camera shake effect
    /// </summary>
    private void CameraShake(float intensity, float duration)
    {
        // TODO: Implement camera shake
        // For now, just log
        if (showDebugLogs)
        {
            Debug.Log($"[GolemBossAttacks] 📷 Camera Shake: {intensity} for {duration}s");
        }
    }
    
    #endregion
    
    #region SPECIAL ATTACKS
    
    /// <summary>
    /// Throw rock projectile at target
    /// </summary>
    public void ThrowRockProjectile()
    {
        if (rockProjectilePrefab == null || currentTarget == null) return;
        
        Vector3 spawnPos = rightHandTransform != null ? rightHandTransform.position : transform.position + Vector3.up * 2f;
        var projectile = Instantiate(rockProjectilePrefab, spawnPos, Quaternion.identity);
        
        // Add projectile script if not already present
        var projectileScript = projectile.GetComponent<BossProjectile>();
        if (projectileScript == null)
        {
            projectileScript = projectile.AddComponent<BossProjectile>();
        }
        
        projectileScript.Initialize(currentTarget, rockProjectileSpeed, groundSlamDamage * 0.5f, playerLayer);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 🪨 Threw rock projectile!");
        }
    }
    
    /// <summary>
    /// Summon rock pillars around target
    /// </summary>
    public void SummonRockPillars()
    {
        if (currentTarget == null) return;
        
        int pillarCount = 5;
        float radius = 3f;
        
        for (int i = 0; i < pillarCount; i++)
        {
            float angle = i * (360f / pillarCount);
            Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Vector3 pillarPos = currentTarget.position + offset;
            
            // Spawn pillar effect (or actual damaging pillar)
            if (groundSlamEffect != null)
            {
                StartCoroutine(SpawnDelayedPillar(pillarPos, i * 0.2f));
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAttacks] 🗿 Summoned rock pillars!");
        }
    }
    
    private IEnumerator SpawnDelayedPillar(Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Spawn warning indicator
        if (shockwavePrefab != null)
        {
            var warning = Instantiate(shockwavePrefab, position, Quaternion.identity);
            warning.transform.localScale = Vector3.one * 2f;
            Destroy(warning, 0.5f);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Spawn pillar and deal damage
        if (groundSlamEffect != null)
        {
            var pillar = Instantiate(groundSlamEffect, position, Quaternion.identity);
            Destroy(pillar, 2f);
        }
        
        // Check for hits
        Collider[] hits = Physics.OverlapSphere(position, 1.5f, playerLayer);
        foreach (var hit in hits)
        {
            var playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(groundSlamDamage * 0.7f);
            }
        }
    }
    
    #endregion
    
    #region DEBUG
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Melee attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);
        
        // Ground slam range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, groundSlamRadius);
        
        // Rage attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, rageAttackRadius);
        
        // Spawn points
        if (rightHandTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rightHandTransform.position, 0.3f);
        }
        
        if (leftHandTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(leftHandTransform.position, 0.3f);
        }
    }
    
    #endregion
}

/// <summary>
/// Simple projectile script for rock throws
/// </summary>
public class BossProjectile : MonoBehaviour
{
    private Transform target;
    private float speed;
    private float damage;
    private LayerMask targetLayer;
    private bool hasHit = false;
    
    public void Initialize(Transform target, float speed, float damage, LayerMask layer)
    {
        this.target = target;
        this.speed = speed;
        this.damage = damage;
        this.targetLayer = layer;
    }
    
    private void Update()
    {
        if (hasHit || target == null)
        {
            Destroy(gameObject, 0.1f);
            return;
        }
        
        // Move towards target
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
        
        // Check if reached target
        if (Vector3.Distance(transform.position, target.position) < 1f)
        {
            OnImpact();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            var playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            
            OnImpact();
        }
    }
    
    private void OnImpact()
    {
        hasHit = true;
        // Spawn impact effect here
        Destroy(gameObject);
    }
}
