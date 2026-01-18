using UnityEngine;

public class GolemBossAI : MonoBehaviour
{
    public enum BossPhase
    {
        Phase1_Normal,
        Phase2_Aggressive,
        Phase3_Enraged
    }

    [Header("Boss Settings")]
    public BossPhase currentPhase = BossPhase.Phase1_Normal;
    public float maxHealth = 1000f;
    public float currentHealth = 1000f;

    [Header("Phase Thresholds")]
    public float phase2Threshold = 0.7f; // 70% health
    public float phase3Threshold = 0.3f; // 30% health

    [Header("Combat Settings")]
    public float attackRange = 3f;
    public float detectionRange = 15f;
    public float moveSpeed = 2f;
    public LayerMask targetLayer = 1 << 0;

    [Header("References")]
    public Transform target;
    public GolemBossAnimator animator;
    public GolemBossAttacks attacks;

    [Header("State")]
    public bool isAlive = true;
    public bool isAttacking = false;
    public bool isMoving = false;

    void Start()
    {
        // Find references if not set
        if (animator == null)
            animator = GetComponent<GolemBossAnimator>();
        if (attacks == null)
            attacks = GetComponent<GolemBossAttacks>();

        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!isAlive) return;

        UpdatePhase();
        UpdateBehavior();
    }

    void UpdatePhase()
    {
        float healthPercent = currentHealth / maxHealth;

        BossPhase newPhase = currentPhase;

        if (healthPercent <= phase3Threshold)
        {
            newPhase = BossPhase.Phase3_Enraged;
        }
        else if (healthPercent <= phase2Threshold)
        {
            newPhase = BossPhase.Phase2_Aggressive;
        }
        else
        {
            newPhase = BossPhase.Phase1_Normal;
        }

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            OnPhaseChanged(newPhase);
        }
    }

    void OnPhaseChanged(BossPhase newPhase)
    {
        Debug.Log($"Golem Boss entering {newPhase}");

        // Notify animator of phase change
        if (animator != null)
        {
            animator.PlayPhaseTransition(newPhase);
        }

        // Update behavior based on phase
        switch (newPhase)
        {
            case BossPhase.Phase2_Aggressive:
                moveSpeed *= 1.2f; // 20% faster
                break;
            case BossPhase.Phase3_Enraged:
                moveSpeed *= 1.5f; // 50% faster
                // Could add more enraged behaviors here
                break;
        }
    }

    void UpdateBehavior()
    {
        // Find target if none
        if (target == null)
        {
            FindTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange && !isAttacking)
        {
            Attack();
        }
        else if (distance <= detectionRange)
        {
            MoveTowardsTarget();
        }
        else
        {
            // Target out of range, find new target
            target = null;
        }
    }

    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayer);

        if (hits.Length > 0)
        {
            // Find closest target
            float closestDistance = Mathf.Infinity;
            Transform closestTarget = null;

            foreach (Collider hit in hits)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = hit.transform;
                }
            }

            target = closestTarget;
        }
    }

    void MoveTowardsTarget()
    {
        if (target == null || isAttacking) return;

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Rotate towards target
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);

        isMoving = true;
    }

    void Attack()
    {
        if (isAttacking || attacks == null) return;

        isAttacking = true;
        isMoving = false;

        // Choose attack based on phase
        switch (currentPhase)
        {
            case BossPhase.Phase1_Normal:
                attacks.PerformBasicAttack();
                break;
            case BossPhase.Phase2_Aggressive:
                attacks.PerformChargedAttack();
                break;
            case BossPhase.Phase3_Enraged:
                attacks.PerformRageAttack();
                break;
        }
    }

    public void OnAttackFinished()
    {
        isAttacking = false;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isAlive = false;
        if (animator != null)
        {
            animator.PlayDeath();
        }
        // Add death logic here
    }

    // Public methods for external access
    public BossPhase GetCurrentPhase()
    {
        return currentPhase;
    }

    public bool IsInCombat()
    {
        return target != null;
    }
}
