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
        // TUTORIAL SPIDER - only for tutorial, no damage, no EXP, always aggro
        patrolSpeed = 2.5f;
        chaseSpeed = 4.5f;
        detectionRadius = 60f;    // Very large: always detects player for tutorial
        attackRange = 1.8f;
        returnThreshold = 999f;   // Never returns to spawn
        hysteresisBuffer = 0f;

        // IMPORTANT: Awake() caches squared values before OnInitialize() runs,
        // so we must recalculate them here after overriding the base values.
        detectionRadiusSquared = detectionRadius * detectionRadius;
        attackRangeSquared = attackRange * attackRange;
        returnThresholdSquared = returnThreshold * returnThreshold;
        returnThresholdWithHysteresis = (returnThreshold + hysteresisBuffer) * (returnThreshold + hysteresisBuffer);
    }

    // Override attack for spider-specific behavior if needed
    protected override void Attack()
    {
        // Spider can have special attack logic here
        // For example: web shooting, jumping attack, etc.
        base.Attack(); // Use base attack logic
    }
}
