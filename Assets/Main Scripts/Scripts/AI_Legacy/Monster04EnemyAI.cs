using UnityEngine;

/// <summary>
/// Specific configuration for Monster 04 (fantasy monster 04).
/// Uses RangedEnemyAI for melee + ranged behavior.
/// </summary>
public class Monster04EnemyAI : RangedEnemyAI
{
    protected override void OnInitialize()
    {
        // Tuning values tuned for Monster04
        meleeRange = 1.8f;
        rangedRange = 8f;
        patrolSpeed = 1.8f;
        chaseSpeed = 4.0f;
        detectionRadius = 12f;
        attackCooldown = 1.1f;
        projectileDamage = 30f;
        projectileSpeed = 22f;
        patrolRadius = 6f;
    }
}



