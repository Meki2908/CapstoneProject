using UnityEngine;

/// <summary>
/// Specific configuration for Monster 03 (fantasy monster 03).
/// Uses RangedEnemyAI for melee + ranged behavior.
/// </summary>
public class Monster03EnemyAI : RangedEnemyAI
{
    protected override void OnInitialize()
    {
        // Tuning values tuned for Monster03
        meleeRange = 1.6f;
        rangedRange = 8f;
        patrolSpeed = 1.4f;
        chaseSpeed = 4.2f;
        detectionRadius = 12f;
        attackCooldown = 1.3f;
        projectileDamage = 20f;
        projectileSpeed = 18f;
        patrolRadius = 5f;
    }
}



