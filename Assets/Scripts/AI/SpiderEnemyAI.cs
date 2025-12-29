using UnityEngine;

/// <summary>
/// Optimized spider enemy AI inheriting from BaseEnemyAI.
/// Provides spider-specific behaviors while using optimized base AI.
/// </summary>
public class SpiderEnemyAI : BaseEnemyAI
{
    // Spider-specific variables can be added here if needed
    // Inherits all common AI from BaseEnemyAI

    protected override void OnInitialize()
    {
        // Spider-specific initialization can be added here
        // For example: special spider behaviors, venom attacks, etc.
        patrolSpeed = 2.5f; // Nhện chậm hơn sói
        chaseSpeed = 4.5f;
        detectionRadius = 8f;
        attackRange = 1.8f;
    }

    // Override attack for spider-specific behavior if needed
    protected override void Attack()
    {
        // Spider can have special attack logic here
        // For example: web shooting, jumping attack, etc.
        base.Attack(); // Use base attack logic
    }
}
