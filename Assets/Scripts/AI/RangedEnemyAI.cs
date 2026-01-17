using UnityEngine;

/// <summary>
/// Generic ranged-capable enemy AI that supports both melee and ranged attacks.
/// Subclasses should set tuning values in OnInitialize().
/// Animation events:
///  - AnimationBeginContactDamage / AnimationEndContactDamage : for EnemyContactDamage windows
///  - AnimationFireProjectile : instantiate projectile at the spawn point (called from ranged attack anim)
/// </summary>
public class RangedEnemyAI : BaseEnemyAI
{
    [Header("Ranged Settings")]
    [Tooltip("Distance (units) considered melee range")]
    public float meleeRange = 1.8f;
    [Tooltip("Distance (units) considered ranged/shooting range")]
    public float rangedRange = 8f;
    [Tooltip("Prefab for the projectile to spawn when performing ranged attack")]
    public GameObject projectilePrefab;
    [Tooltip("Spawn point transform (assign a child Transform on the prefab)")]
    public Transform projectileSpawnPoint;
    [Tooltip("Speed to apply to projectile if not using Projectile manager")]
    public float projectileSpeed = 20f;
    [Tooltip("Damage value applied by projectile (used if SkillProjectileManager not present)")]
    public float projectileDamage = 25f;

    protected override void OnInitialize()
    {
        // Default tuning — subclasses should override as needed
        patrolSpeed = 1.6f;
        chaseSpeed = 4.0f;
        detectionRadius = 12f;
        attackRange = meleeRange;
        attackCooldown = 1.2f;
    }

    protected override void UpdateState()
    {
        // Use a slightly modified state machine to account for ranged behavior.
        if (Time.time - lastStateChangeTime < STATE_CHANGE_COOLDOWN) return;

        float distFromSpawnSquared = (new Vector2(transform.position.x, transform.position.z) -
                                     new Vector2(spawnPos.x, spawnPos.z)).sqrMagnitude;

        bool playerInDetect = lastDistanceToPlayerSquared <= detectionRadiusSquared;
        bool playerInMelee = lastDistanceToPlayerSquared <= (meleeRange * meleeRange);
        bool playerInRanged = lastDistanceToPlayerSquared <= (rangedRange * rangedRange);
        bool tooFarFromSpawn = distFromSpawnSquared > returnThresholdSquared;
        bool playerWithinReturnAreaWithHysteresis = distFromSpawnSquared <= returnThresholdWithHysteresis;

        EnemyState newState = currentState;

        if (currentState == EnemyState.Attacking && Time.time >= nextAttackTime)
        {
            newState = EnemyState.Chase;
        }
        else if ((playerInMelee || playerInRanged) && Time.time >= nextAttackTime)
        {
            // Prefer melee when very close; ranged if further but within rangedRange
            newState = EnemyState.Attack;
        }
        else if (playerInDetect && playerWithinReturnAreaWithHysteresis)
        {
            newState = EnemyState.Chase;
        }
        else if (tooFarFromSpawn && currentState != EnemyState.Attack)
        {
            if (!playerInDetect)
            {
                newState = EnemyState.Return;
            }
        }
        else if (!playerInDetect && !tooFarFromSpawn)
        {
            newState = EnemyState.Patrol;
        }

        if (newState != currentState)
        {
            currentState = newState;
            lastStateChangeTime = Time.time;
        }
    }

    protected override void Attack()
    {
        // Decide which attack to perform based on current distance
        float distanceToPlayer = GetDistanceToPlayer();

        // Trigger appropriate animator parameters so the controller plays the correct clip.
        if (anim != null)
        {
            try
            {
                if (distanceToPlayer <= meleeRange)
                {
                    anim.SetInteger("attackIndex", 0); // melee
                    anim.SetTrigger("Attack");
                }
                else
                {
                    anim.SetInteger("attackIndex", 1); // ranged
                    anim.SetTrigger("Attack");
                }
            }
            catch { }
        }

        // Set cooldown and state
        nextAttackTime = Time.time + attackCooldown;
        currentState = EnemyState.Attacking;
    }

    // Animation event - used for melee damage windows
    public void AnimationBeginContactDamage()
    {
        var contact = GetComponent<EnemyContactDamage>();
        if (contact != null) contact.BeginContactDamage();
    }

    public void AnimationEndContactDamage()
    {
        var contact = GetComponent<EnemyContactDamage>();
        if (contact != null) contact.EndContactDamage();
    }

    // Animation event - called from ranged attack animation where projectile should be spawned
    public void AnimationFireProjectile()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null || player == null) return;

        // Instantiate projectile at spawn point and orient toward player
        GameObject proj = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        Vector3 dir = (player.position - projectileSpawnPoint.position).normalized;
        proj.transform.forward = dir;

        // Try to use SkillProjectileManager to setup damage if available
        try
        {
            if (SkillProjectileManager.Instance != null)
            {
                SkillProjectileManager.Instance.SetupSkillProjectile(proj, projectileDamage, WeaponType.Mage, AbilityInput.E, true);
            }
            else
            {
                // Basic velocity application if no manager
                var rb = proj.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = dir * projectileSpeed;
            }
        }
        catch { }
    }
}



