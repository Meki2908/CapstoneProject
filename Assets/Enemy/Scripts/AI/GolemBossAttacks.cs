using UnityEngine;

public class GolemBossAttacks : MonoBehaviour
{
    public GolemBossAI bossAI;

    [Header("Attack Settings")]
    public float basicAttackDamage = 20f;
    public float chargedAttackDamage = 35f;
    public float rageAttackDamage = 50f;

    public float basicAttackRange = 3f;
    public float chargedAttackRange = 4f;
    public float rageAttackRange = 5f;

    void Start()
    {
        if (bossAI == null)
            bossAI = GetComponent<GolemBossAI>();
    }

    public void PerformBasicAttack()
    {
        Debug.Log("Performing basic attack");
        // Basic attack logic
        if (bossAI.target != null)
        {
            // Check if target is in range
            float distance = Vector3.Distance(transform.position, bossAI.target.position);
            if (distance <= basicAttackRange)
            {
                // Deal damage to target
                // This would need a health component on the target
                Debug.Log($"Basic attack hit for {basicAttackDamage} damage");
            }
        }

        // Notify AI that attack is finished
        Invoke(nameof(FinishAttack), 1.0f);
    }

    public void PerformChargedAttack()
    {
        Debug.Log("Performing charged attack");
        // Charged attack logic - stronger but slower
        if (bossAI.target != null)
        {
            float distance = Vector3.Distance(transform.position, bossAI.target.position);
            if (distance <= chargedAttackRange)
            {
                Debug.Log($"Charged attack hit for {chargedAttackDamage} damage");
            }
        }

        Invoke(nameof(FinishAttack), 2.0f);
    }

    public void PerformRageAttack()
    {
        Debug.Log("Performing rage attack");
        // Rage attack logic - area attack
        if (bossAI.target != null)
        {
            float distance = Vector3.Distance(transform.position, bossAI.target.position);
            if (distance <= rageAttackRange)
            {
                Debug.Log($"Rage attack hit for {rageAttackDamage} damage");
            }
        }

        Invoke(nameof(FinishAttack), 3.0f);
    }

    /// <summary>
    /// Called by animation event when attack hits
    /// </summary>
    public void OnAttackHitFrame()
    {
        Debug.Log("Attack hit frame - dealing damage");
        // Deal damage to target
        if (bossAI != null && bossAI.target != null)
        {
            // This would need a health component on the target
            Debug.Log($"Dealing {basicAttackDamage} damage to {bossAI.target.name}");
        }
    }

    /// <summary>
    /// Called by animation event when ground slam impacts
    /// </summary>
    public void OnGroundSlamImpact()
    {
        Debug.Log("Ground slam impact - area damage");
        // Area damage effect
        if (bossAI != null)
        {
            // Deal area damage around boss position
            Debug.Log($"Ground slam dealing {chargedAttackDamage} area damage");
        }
    }

    /// <summary>
    /// Called by animation event when rage wave is released
    /// </summary>
    public void OnRageWaveRelease()
    {
        Debug.Log("Rage wave release - projectile attack");
        // Launch rage wave projectile
        if (bossAI != null)
        {
            // Spawn rage wave projectile
            Debug.Log($"Rage wave dealing {rageAttackDamage} damage");
        }
    }

    void FinishAttack()
    {
        if (bossAI != null)
        {
            bossAI.OnAttackFinished();
        }
    }
}