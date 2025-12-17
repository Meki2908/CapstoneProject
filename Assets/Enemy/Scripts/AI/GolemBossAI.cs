using UnityEngine;
using AI.BehaviorTree;
using System.Collections.Generic;

/// <summary>
/// GOLEM BOSS AI - Advanced Behavior Tree System
/// Boss có 3 phases với các attack patterns khác nhau
/// Phase 1 (100-66% HP): Basic attacks, slow movement
/// Phase 2 (66-33% HP): Faster, combo attacks, occasional rage
/// Phase 3 (33-0% HP): Aggressive, always enraged, area attacks
/// </summary>
public class GolemBossAI : BehaviorTree
{
    [Header("=== BOSS STATS ===")]
    [Tooltip("Tổng máu của boss")]
    public float maxHealth = 1000f;
    [HideInInspector] public float currentHealth;

    [Header("=== DETECTION & COMBAT ===")]
    [Tooltip("Khoảng cách phát hiện player")]
    public float detectionRange = 15f;
    [Tooltip("Khoảng cách tấn công cận chiến")]
    public float meleeAttackRange = 3f;
    [Tooltip("Khoảng cách tấn công xa (ground slam, etc)")]
    public float rangedAttackRange = 8f;
    [Tooltip("Layer của player")]
    public LayerMask playerLayer = -1;

    [Header("=== MOVEMENT ===")]
    [Tooltip("Tốc độ đi bộ (Phase 1)")]
    public float walkSpeed = 2f;
    [Tooltip("Tốc độ đuổi theo (Phase 2)")]
    public float chaseSpeed = 4f;
    [Tooltip("Tốc độ rage (Phase 3)")]
    public float rageSpeed = 6f;

    [Header("=== PHASE TRANSITIONS ===")]
    [Tooltip("% HP để chuyển sang Phase 2")]
    [Range(0f, 1f)] public float phase2Threshold = 0.66f;
    [Tooltip("% HP để chuyển sang Phase 3 (Rage)")]
    [Range(0f, 1f)] public float phase3Threshold = 0.33f;

    [Header("=== ATTACK COOLDOWNS ===")]
    [Tooltip("Cooldown giữa các đòn đánh thường")]
    public float basicAttackCooldown = 2f;
    [Tooltip("Cooldown cho combo attack")]
    public float comboAttackCooldown = 5f;
    [Tooltip("Cooldown cho ground slam")]
    public float groundSlamCooldown = 8f;
    [Tooltip("Cooldown cho rage attack")]
    public float rageAttackCooldown = 10f;

    [Header("=== SPECIAL BEHAVIORS ===")]
    [Tooltip("Boss sẽ roar khi vào combat")]
    public bool roarOnCombatStart = true;
    [Tooltip("Boss sẽ heal một lần khi xuống dưới 20% HP")]
    public bool canHealOnce = true;
    [Tooltip("% HP được hồi khi heal")]
    [Range(0f, 0.5f)] public float healAmount = 0.15f;

    [Header("=== PATROL (khi không combat) ===")]
    public Transform[] patrolPoints;
    public float patrolRadius = 10f;

    [Header("=== REFERENCES ===")]
    public GolemBossAnimator bossAnimator;
    public GolemBossHealth bossHealth;
    public GolemBossAttacks bossAttacks;
    public CharacterController characterController;

    [Header("=== DEBUG ===")]
    public bool showDebugLogs = true;
    public bool showGizmos = true;

    // ===== INTERNAL STATE =====
    [HideInInspector] public BossPhase currentPhase = BossPhase.Phase1_Normal;
    [HideInInspector] public bool isInCombat = false;
    [HideInInspector] public bool hasRoared = false;
    [HideInInspector] public bool hasHealed = false;
    [HideInInspector] public Transform currentTarget;

    // Attack timers
    [HideInInspector] public float lastBasicAttackTime;
    [HideInInspector] public float lastComboAttackTime;
    [HideInInspector] public float lastGroundSlamTime;
    [HideInInspector] public float lastRageAttackTime;

    // Position lock for attacks
    [HideInInspector] public bool isPositionLocked = false;
    [HideInInspector] public Vector3 lockedPosition;

    public enum BossPhase
    {
        Phase1_Normal,      // 100-66% HP: Slow, basic attacks
        Phase2_Aggressive,  // 66-33% HP: Faster, combo attacks
        Phase3_Enraged      // 33-0% HP: Enraged, devastating attacks
    }

    private new void Start()
    {
        base.Start(); // Call parent BehaviorTree.Start()

        currentHealth = maxHealth;

        // Auto-find components if not assigned
        if (bossAnimator == null) bossAnimator = GetComponent<GolemBossAnimator>();
        if (bossHealth == null) bossHealth = GetComponent<GolemBossHealth>();
        if (bossAttacks == null) bossAttacks = GetComponent<GolemBossAttacks>();
        if (characterController == null) characterController = GetComponent<CharacterController>();

        if (showDebugLogs)
        {
            Debug.Log($"[GOLEM BOSS] 👹 Boss Initialized!");
            Debug.Log($"   - Max Health: {maxHealth}");
            Debug.Log($"   - Phase 2 at: {phase2Threshold * 100}% HP");
            Debug.Log($"   - Phase 3 at: {phase3Threshold * 100}% HP");
        }
    }

    protected override Node SetupTree()
    {
        // Auto-set player layer if not configured
        if (playerLayer.value == 0)
        {
            playerLayer = LayerMask.GetMask("Player");
        }

        // ===== BOSS BEHAVIOR TREE STRUCTURE =====
        Node root = new Selector(new List<Node>
        {
            // Priority 1: Check if dead
            new TaskBossCheckDeath(this),
            
            // Priority 2: Check for phase transition
            new TaskBossCheckPhaseTransition(this),
            
            // Priority 3: Combat behavior
            new Sequence(new List<Node>
            {
                new TaskBossDetectPlayer(this),
                
                // Enter combat if not already
                new TaskBossEnterCombat(this),
                
                // Combat decision tree
                new Selector(new List<Node>
                {
                    // Check for special abilities first
                    new TaskBossCheckHeal(this),
                    new TaskBossCheckRageAttack(this),
                    new TaskBossCheckGroundSlam(this),
                    new TaskBossCheckComboAttack(this),
                    
                    // Then normal combat
                    new Sequence(new List<Node>
                    {
                        new TaskBossCheckAttackRange(this, meleeAttackRange),
                        new TaskBossBasicAttack(this)
                    }),
                    
                    // If not in range, chase
                    new TaskBossChaseTarget(this)
                })
            }),
            
            // Priority 4: Patrol when no target
            new TaskBossPatrol(this)
        });

        if (showDebugLogs)
        {
            Debug.Log("[GOLEM BOSS] ✅ Behavior Tree Setup Complete!");
        }

        return root;
    }

    private void LateUpdate()
    {
        // Force position lock during attacks
        if (isPositionLocked)
        {
            transform.position = lockedPosition;
        }
    }

    /// <summary>
    /// Update current phase based on health percentage
    /// </summary>
    public void UpdatePhase()
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

    /// <summary>
    /// Called when phase changes
    /// </summary>
    private void OnPhaseChanged(BossPhase newPhase)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[GOLEM BOSS] 🔥 PHASE TRANSITION → {newPhase}");
        }

        // Trigger phase transition animation
        if (bossAnimator != null)
        {
            bossAnimator.PlayPhaseTransition(newPhase);
        }
    }

    /// <summary>
    /// Get current move speed based on phase
    /// </summary>
    public float GetCurrentMoveSpeed()
    {
        switch (currentPhase)
        {
            case BossPhase.Phase3_Enraged:
                return rageSpeed;
            case BossPhase.Phase2_Aggressive:
                return chaseSpeed;
            default:
                return walkSpeed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Vector3 pos = transform.position;

        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, detectionRange);

        // Melee attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, meleeAttackRange);

        // Ranged attack range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(pos, rangedAttackRange);

        // Patrol radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pos, patrolRadius);
    }

    #region BOSS HELPER METHODS

    /// <summary>
    /// Helper: Lock boss position during attacks
    /// </summary>
    public static void LockBossPosition(GolemBossAI boss)
    {
        if (!boss.isPositionLocked)
        {
            boss.lockedPosition = boss.transform.position;
            boss.isPositionLocked = true;

            if (boss.characterController != null)
            {
                boss.characterController.enabled = false;
            }
        }
    }

    /// <summary>
    /// Helper: Coroutine to unlock position after delay
    /// </summary>
    public static System.Collections.IEnumerator UnlockPositionAfterDelay(GolemBossAI boss, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (boss.isPositionLocked)
        {
            boss.isPositionLocked = false;
            if (boss.characterController != null)
            {
                boss.characterController.enabled = true;
            }
        }
    }

    #endregion
}

#region BOSS BEHAVIOR TREE TASKS

/// <summary>
/// Task: Check if boss is dead
/// </summary>
public class TaskBossCheckDeath : Node
{
    private GolemBossAI boss;

    public TaskBossCheckDeath(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        if (boss.currentHealth <= 0)
        {
            if (boss.bossAnimator != null)
            {
                boss.bossAnimator.PlayDeath();
            }

            if (boss.showDebugLogs)
            {
                Debug.Log("[GOLEM BOSS] 💀 BOSS DEFEATED!");
            }

            state = NodeState.Success;
            return state;
        }

        state = NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Check and handle phase transitions
/// </summary>
public class TaskBossCheckPhaseTransition : Node
{
    private GolemBossAI boss;

    public TaskBossCheckPhaseTransition(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        boss.UpdatePhase();

        state = NodeState.Failure; // Always continue to combat
        return state;
    }
}

/// <summary>
/// Task: Detect player in range
/// </summary>
public class TaskBossDetectPlayer : Node
{
    private GolemBossAI boss;

    public TaskBossDetectPlayer(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        object target = GetData("target");

        if (target == null)
        {
            Collider[] players = Physics.OverlapSphere(
                boss.transform.position,
                boss.detectionRange,
                boss.playerLayer
            );

            if (players.Length > 0)
            {
                boss.currentTarget = players[0].transform;
                parent.parent.SetData("target", boss.currentTarget);

                if (boss.showDebugLogs)
                {
                    Debug.Log($"[GOLEM BOSS] 🎯 PLAYER DETECTED: {boss.currentTarget.name}");
                }

                state = NodeState.Success;
                return state;
            }

            state = NodeState.Failure;
            return state;
        }

        state = NodeState.Success;
        return state;
    }
}

/// <summary>
/// Task: Enter combat mode (roar, etc)
/// </summary>
public class TaskBossEnterCombat : Node
{
    private GolemBossAI boss;

    public TaskBossEnterCombat(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        if (!boss.isInCombat)
        {
            boss.isInCombat = true;

            if (boss.roarOnCombatStart && !boss.hasRoared)
            {
                boss.hasRoared = true;

                if (boss.bossAnimator != null)
                {
                    boss.bossAnimator.PlayRoar();
                }

                if (boss.showDebugLogs)
                {
                    Debug.Log("[GOLEM BOSS] 🦁 ROAR! Combat begins!");
                }
            }
        }

        state = NodeState.Success;
        return state;
    }
}

/// <summary>
/// Task: Check if boss should heal
/// </summary>
public class TaskBossCheckHeal : Node
{
    private GolemBossAI boss;

    public TaskBossCheckHeal(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        if (!boss.canHealOnce || boss.hasHealed)
        {
            state = NodeState.Failure;
            return state;
        }

        float healthPercent = boss.currentHealth / boss.maxHealth;

        if (healthPercent <= 0.2f)
        {
            boss.hasHealed = true;
            float healValue = boss.maxHealth * boss.healAmount;
            boss.currentHealth = Mathf.Min(boss.currentHealth + healValue, boss.maxHealth);

            if (boss.bossAnimator != null)
            {
                boss.bossAnimator.PlayHeal();
            }

            if (boss.showDebugLogs)
            {
                Debug.Log($"[GOLEM BOSS] 💚 HEALING! +{healValue} HP");
            }

            state = NodeState.Success;
            return state;
        }

        state = NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Check for rage attack (Phase 3)
/// </summary>
public class TaskBossCheckRageAttack : Node
{
    private GolemBossAI boss;

    public TaskBossCheckRageAttack(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        if (boss.currentPhase != GolemBossAI.BossPhase.Phase3_Enraged)
        {
            state = NodeState.Failure;
            return state;
        }

        if (Time.time - boss.lastRageAttackTime < boss.rageAttackCooldown)
        {
            state = NodeState.Failure;
            return state;
        }

        object target = GetData("target");
        if (target == null)
        {
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;
        float distance = Vector3.Distance(boss.transform.position, targetTransform.position);

        if (distance <= boss.rangedAttackRange)
        {
            boss.lastRageAttackTime = Time.time;

            // Lock position during attack
            GolemBossAI.LockBossPosition(boss);
            boss.StartCoroutine(GolemBossAI.UnlockPositionAfterDelay(boss, 2.5f)); // Rage has longer animation

            if (boss.bossAnimator != null)
            {
                boss.bossAnimator.PlayRageAttack();
            }

            if (boss.bossAttacks != null)
            {
                boss.bossAttacks.ExecuteRageAttack();
            }

            if (boss.showDebugLogs)
            {
                Debug.Log("[GOLEM BOSS] 💥 RAGE ATTACK!");
            }

            state = NodeState.Success;
            return state;
        }

        state = NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Check for ground slam attack
/// </summary>
public class TaskBossCheckGroundSlam : Node
{
    private GolemBossAI boss;

    public TaskBossCheckGroundSlam(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        if (Time.time - boss.lastGroundSlamTime < boss.groundSlamCooldown)
        {
            state = NodeState.Failure;
            return state;
        }

        object target = GetData("target");
        if (target == null)
        {
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;
        float distance = Vector3.Distance(boss.transform.position, targetTransform.position);

        // Ground slam is mid-range attack
        if (distance > boss.meleeAttackRange && distance <= boss.rangedAttackRange)
        {
            boss.lastGroundSlamTime = Time.time;

            // Lock position during attack
            GolemBossAI.LockBossPosition(boss);
            boss.StartCoroutine(GolemBossAI.UnlockPositionAfterDelay(boss, 2.0f)); // Ground slam animation

            if (boss.bossAnimator != null)
            {
                boss.bossAnimator.PlayGroundSlam();
            }

            if (boss.bossAttacks != null)
            {
                boss.bossAttacks.ExecuteGroundSlam();
            }

            if (boss.showDebugLogs)
            {
                Debug.Log("[GOLEM BOSS] 🌍 GROUND SLAM!");
            }

            state = NodeState.Success;
            return state;
        }

        state = NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Check for combo attack (Phase 2+)
/// </summary>
public class TaskBossCheckComboAttack : Node
{
    private GolemBossAI boss;

    public TaskBossCheckComboAttack(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        if (boss.currentPhase == GolemBossAI.BossPhase.Phase1_Normal)
        {
            state = NodeState.Failure;
            return state;
        }

        if (Time.time - boss.lastComboAttackTime < boss.comboAttackCooldown)
        {
            state = NodeState.Failure;
            return state;
        }

        object target = GetData("target");
        if (target == null)
        {
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;
        float distance = Vector3.Distance(boss.transform.position, targetTransform.position);

        if (distance <= boss.meleeAttackRange)
        {
            boss.lastComboAttackTime = Time.time;

            // Lock position during attack
            GolemBossAI.LockBossPosition(boss);
            boss.StartCoroutine(GolemBossAI.UnlockPositionAfterDelay(boss, 1.8f)); // Combo attack duration

            if (boss.bossAnimator != null)
            {
                boss.bossAnimator.PlayComboAttack();
            }

            if (boss.bossAttacks != null)
            {
                boss.bossAttacks.ExecuteComboAttack();
            }

            if (boss.showDebugLogs)
            {
                Debug.Log("[GOLEM BOSS] 🥊 COMBO ATTACK!");
            }

            state = NodeState.Success;
            return state;
        }

        state = NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Check if in attack range
/// </summary>
public class TaskBossCheckAttackRange : Node
{
    private GolemBossAI boss;
    private float attackRange;

    public TaskBossCheckAttackRange(GolemBossAI boss, float range)
    {
        this.boss = boss;
        this.attackRange = range;
    }

    public override NodeState Evaluate()
    {
        object target = GetData("target");
        if (target == null)
        {
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;
        float distance = Vector3.Distance(boss.transform.position, targetTransform.position);

        state = distance <= attackRange ? NodeState.Success : NodeState.Failure;
        return state;
    }
}

/// <summary>
/// Task: Basic melee attack
/// </summary>
public class TaskBossBasicAttack : Node
{
    private GolemBossAI boss;

    public TaskBossBasicAttack(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        if (Time.time - boss.lastBasicAttackTime < boss.basicAttackCooldown)
        {
            state = NodeState.Running;
            return state;
        }

        object target = GetData("target");
        if (target == null)
        {
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;

        // Lock position
        if (!boss.isPositionLocked)
        {
            boss.lockedPosition = boss.transform.position;
            boss.isPositionLocked = true;

            if (boss.characterController != null)
            {
                boss.characterController.enabled = false;
            }
        }

        // Rotate to target
        Vector3 direction = (targetTransform.position - boss.transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            boss.transform.rotation = Quaternion.Slerp(
                boss.transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 10f
            );
        }

        boss.lastBasicAttackTime = Time.time;

        // Lock position during attack
        GolemBossAI.LockBossPosition(boss);

        if (boss.bossAnimator != null)
        {
            boss.bossAnimator.PlayBasicAttack();
        }

        if (boss.bossAttacks != null)
        {
            boss.bossAttacks.ExecuteBasicAttack();
        }

        // Schedule unlock after attack animation duration
        boss.StartCoroutine(GolemBossAI.UnlockPositionAfterDelay(boss, 1.2f));

        if (boss.showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log("[GOLEM BOSS] ⚔️ Basic Attack!");
        }

        state = NodeState.Success;
        return state;
    }
}

/// <summary>
/// Task: Chase player
/// </summary>
public class TaskBossChaseTarget : Node
{
    private GolemBossAI boss;

    public TaskBossChaseTarget(GolemBossAI boss)
    {
        this.boss = boss;
    }

    public override NodeState Evaluate()
    {
        object target = GetData("target");
        if (target == null)
        {
            state = NodeState.Failure;
            return state;
        }

        Transform targetTransform = (Transform)target;
        float distance = Vector3.Distance(boss.transform.position, targetTransform.position);

        // Lost target
        if (distance > boss.detectionRange)
        {
            ClearData("target");
            boss.isInCombat = false;
            boss.hasRoared = false;

            if (boss.bossAnimator != null)
            {
                boss.bossAnimator.SetSpeed(0f);
            }

            state = NodeState.Failure;
            return state;
        }

        // Unlock position if locked
        if (boss.isPositionLocked)
        {
            boss.isPositionLocked = false;
            if (boss.characterController != null)
            {
                boss.characterController.enabled = true;
            }
        }

        // Move towards target
        Vector3 direction = (targetTransform.position - boss.transform.position).normalized;
        direction.y = 0;

        float moveSpeed = boss.GetCurrentMoveSpeed();

        if (boss.characterController != null)
        {
            boss.characterController.Move(direction * moveSpeed * Time.deltaTime);
        }
        else
        {
            boss.transform.position += direction * moveSpeed * Time.deltaTime;
        }

        boss.transform.rotation = Quaternion.LookRotation(direction);

        if (boss.bossAnimator != null)
        {
            boss.bossAnimator.SetSpeed(moveSpeed);
        }

        if (boss.showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[GOLEM BOSS] 🏃 Chasing at speed {moveSpeed:F1}");
        }

        state = NodeState.Running;
        return state;
    }
}

/// <summary>
/// Task: Patrol when idle
/// </summary>
public class TaskBossPatrol : Node
{
    private GolemBossAI boss;
    private int currentPatrolIndex = 0;
    private Vector3 currentPatrolPoint;
    private bool isWaiting = false;
    private float waitCounter = 0f;
    private float waitTime = 3f;

    public TaskBossPatrol(GolemBossAI boss)
    {
        this.boss = boss;

        if (boss.patrolPoints != null && boss.patrolPoints.Length > 0)
        {
            currentPatrolPoint = boss.patrolPoints[0].position;
        }
        else
        {
            currentPatrolPoint = boss.transform.position;
        }
    }

    public override NodeState Evaluate()
    {
        if (isWaiting)
        {
            waitCounter += Time.deltaTime;

            if (boss.bossAnimator != null)
            {
                boss.bossAnimator.SetSpeed(0f);
            }

            if (waitCounter >= waitTime)
            {
                isWaiting = false;
                waitCounter = 0f;

                // Get next patrol point
                if (boss.patrolPoints != null && boss.patrolPoints.Length > 0)
                {
                    currentPatrolIndex = (currentPatrolIndex + 1) % boss.patrolPoints.Length;
                    currentPatrolPoint = boss.patrolPoints[currentPatrolIndex].position;
                }
            }

            state = NodeState.Running;
            return state;
        }

        float distance = Vector3.Distance(boss.transform.position, currentPatrolPoint);

        if (distance < 0.5f)
        {
            isWaiting = true;
            state = NodeState.Running;
            return state;
        }

        // Move to patrol point
        Vector3 direction = (currentPatrolPoint - boss.transform.position).normalized;
        direction.y = 0;

        if (boss.characterController != null && boss.characterController.enabled)
        {
            boss.characterController.Move(direction * boss.walkSpeed * 0.5f * Time.deltaTime);
        }

        boss.transform.rotation = Quaternion.LookRotation(direction);

        if (boss.bossAnimator != null)
        {
            boss.bossAnimator.SetSpeed(boss.walkSpeed * 0.5f);
        }

        state = NodeState.Running;
        return state;
    }
}

#endregion
