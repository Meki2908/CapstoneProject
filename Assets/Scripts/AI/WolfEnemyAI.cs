using UnityEngine;

/// <summary>
/// Example wolf enemy AI - demonstrates how easy it is to create different enemy types.
/// </summary>
public class WolfEnemyAI : BaseEnemyAI
{
    [Header("Wolf-Specific Settings")]
    public float howlCooldown = 10f;
    public float packBonusSpeed = 1.2f; // Speed bonus when in pack

    private float lastHowlTime;

    protected override void OnInitialize()
    {
        // Wolf-specific initialization
        patrolSpeed = 3.5f; // Wolves are faster than spiders
        chaseSpeed = 6.0f;
        detectionRadius = 12f; // Wolves have better senses
        attackRange = 2.5f;

        lastHowlTime = -howlCooldown;
    }

    protected override void Attack()
    {
        // Wolf-specific attack: might howl sometimes
        if (Time.time - lastHowlTime >= howlCooldown && Random.value < 0.3f)
        {
            // Howl animation instead of attack
            anim.SetTrigger("Howl");
            lastHowlTime = Time.time;
        }
        else
        {
            // Normal attack
            base.Attack();
        }
    }

    protected override void Chase()
    {
        // Check if there are other wolves nearby for pack bonus
        int nearbyWolves = CheckNearbyWolves();
        if (nearbyWolves > 0)
        {
            agent.speed = chaseSpeed * packBonusSpeed;
        }
        else
        {
            agent.speed = chaseSpeed;
        }

        base.Chase();
    }

    private int CheckNearbyWolves()
    {
        // Simple check for nearby wolves (you could optimize this with spatial partitioning)
        WolfEnemyAI[] allWolves = FindObjectsByType<WolfEnemyAI>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var wolf in allWolves)
        {
            if (wolf != this && Vector3.Distance(transform.position, wolf.transform.position) < 15f)
            {
                count++;
            }
        }

        return count;
    }
}
