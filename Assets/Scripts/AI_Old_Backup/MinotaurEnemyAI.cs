using UnityEngine;
/// <summary>
/// Simple Minotaur enemy AI that reuses BaseEnemyAI.
/// Configure stats here specific to the Minotaur imported from Asset Store.
/// Add this script to the Minotaur prefab and configure Animator/Agent.
/// </summary>
public class MinotaurEnemyAI : BaseEnemyAI
{
    protected override void OnInitialize()
    {
        // Tunable defaults for Minotaur
        patrolSpeed = 1.2f;
        chaseSpeed = 3.6f;
        detectionRadius = 10f;
        attackRange = 1.8f;
        returnThreshold = 14f;
        hysteresisBuffer = 1.2f;
        waypointPause = 1.0f;
        attackCooldown = 1.4f;
        patrolRadius = 6f;
    }

    // These methods are intended to be called from animation events
    // to control when contact damage windows open/close.
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

    // Convenience method if animation wants to directly attempt a hit
    public void AnimationAttemptDealDamage()
    {
        var contact = GetComponent<EnemyContactDamage>();
        if (contact != null) contact.AttemptDealContactDamage();
    }

    // Optionally override Attack to set animator parameters used by the Minotaur controller
    protected override void Attack()
    {
        if (anim != null)
        {
            // Try to trigger attack parameter if present
            try { anim.SetTrigger("Attack"); } catch { }
        }
        base.Attack();
    }
}



