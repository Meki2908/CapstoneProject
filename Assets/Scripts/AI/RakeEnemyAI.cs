using UnityEngine;

/// <summary>
/// Rake enemy AI inheriting common behaviour from BaseEnemyAI.
/// Configure per-enemy tuning here. Animation events should be wired
/// to the public methods below (BeginContactDamage / EndContactDamage).
/// </summary>
public class RakeEnemyAI : BaseEnemyAI
{
    protected override void OnInitialize()
    {
        // Tunable defaults for Rake
        patrolSpeed = 1.6f;
        chaseSpeed = 4.2f;
        detectionRadius = 9f;
        attackRange = 1.6f;
        returnThreshold = 12f;
        hysteresisBuffer = 1.2f;
        waypointPause = 1.0f;
        attackCooldown = 1.2f;
        patrolRadius = 5f;
    }

    // Animation event: enable contact damage window
    public void AnimationBeginContactDamage()
    {
        var contact = GetComponent<EnemyContactDamage>();
        if (contact != null) contact.BeginContactDamage();
    }

    // Animation event: disable contact damage window
    public void AnimationEndContactDamage()
    {
        var contact = GetComponent<EnemyContactDamage>();
        if (contact != null) contact.EndContactDamage();
    }

    // Animation event: attempt a direct damage tick (if animation wants to call it)
    public void AnimationAttemptDealDamage()
    {
        var contact = GetComponent<EnemyContactDamage>();
        if (contact != null) contact.AttemptDealContactDamage();
    }

    // Optionally set animator parameters specific to Rake before attacking
    protected override void Attack()
    {
        if (anim != null)
        {
            try { anim.SetTrigger("Attack"); } catch { }
            // Some controllers use an integer index for multiple attacks
            try { anim.SetInteger("attackIndex", 0); } catch { }
        }
        base.Attack();
    }

    // Optionally override TakeHit to add behavior specific to Rake
    public override void TakeHit()
    {
        base.TakeHit();
        // e.g. short stagger or audio cue could be triggered here via animation events
    }

    // Cleanup / additional logic when dying
    public override void Die()
    {
        base.Die();
        // Disable collider so corpse doesn't interfere with navigation
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}


