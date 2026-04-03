using UnityEngine;

/// <summary>
/// Simple Catfish enemy AI that reuses BaseEnemyAI.
/// Configure stats here specific to the catfish enemy.
/// </summary>
public class CatfishEnemyAI : BaseEnemyAI
{
    protected override void OnInitialize()
    {
        // Tunable defaults for catfish
        patrolSpeed = 1.5f;
        chaseSpeed = 3.2f;
        detectionRadius = 8f;
        attackRange = 0.8f; // we will use contact damage instead of attack animation
        returnThreshold = 12f;
        hysteresisBuffer = 1.5f;
        waypointPause = 1.2f;
        attackCooldown = 1.5f; // slower attacks to match animation length
    }

    // Optional hook called from animation events if needed in future
    public void OnContactDamage()
    {
        // Intentionally left blank — damage handled by EnemyContactDamage component
    }

    // Override to use single primary attack animation (attack1) to avoid timing issues.
    protected override void Attack()
    {
        if (anim != null)
        {
            // Ensure animator uses primary attack (if parameter exists)
            try { anim.SetInteger("attackIndex", 0); } catch { }
        }
        base.Attack();
    }
}














