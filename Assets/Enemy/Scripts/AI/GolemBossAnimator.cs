using UnityEngine;

/// <summary>
/// GOLEM BOSS ANIMATOR CONTROLLER
/// Quản lý tất cả animation states và transitions cho boss
/// Tương thích với GolemAnimator.controller parameters
/// </summary>
[RequireComponent(typeof(Animator))]
public class GolemBossAnimator : MonoBehaviour
{
    [Header("=== ANIMATOR REFERENCE ===")]
    public Animator animator;
    
    [Header("=== ANIMATION SETTINGS ===")]
    [Tooltip("Tốc độ animation walk/run multiplier")]
    public float animationSpeedMultiplier = 1f;
    
    [Tooltip("Smooth time cho speed transitions")]
    public float speedSmoothTime = 0.1f;
    
    [Header("=== ATTACK VARIATIONS ===")]
    [Tooltip("Random sử dụng Hit hoặc Hit2")]
    public bool randomizeBasicAttacks = true;
    
    [Header("=== DEBUG ===")]
    public bool showDebugLogs = false;
    
    // Animation parameter names (matching GolemAnimator.controller)
    private static readonly string PARAM_WALK = "Walk";
    private static readonly string PARAM_IDLE_ACTION = "IdleAction";
    private static readonly string PARAM_HIT = "Hit";
    private static readonly string PARAM_HIT2 = "Hit2";
    private static readonly string PARAM_DAMAGE = "Damage";
    private static readonly string PARAM_DIE = "Die";
    private static readonly string PARAM_RAGE = "Rage";
    private static readonly string PARAM_JUMP = "Jump";
    private static readonly string PARAM_LAND = "Land";
    private static readonly string PARAM_SLEEP_START = "SleepStart";
    private static readonly string PARAM_SLEEP_END = "SleepEnd";
    
    // State names
    private static readonly string STATE_IDLE = "Idle";
    // private static readonly string STATE_WALK = "Walk"; // Unused
    // private static readonly string STATE_DIE = "Die";   // Unused

    // Internal state
    private float currentSpeed = 0f;
    private float speedVelocity = 0f;
    private int lastAttackVariation = 0;
    
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogError("[GolemBossAnimator] ❌ No Animator found!");
        }
    }
    
    private void Start()
    {
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] ✅ Animator initialized");
            LogAvailableParameters();
        }
    }
    
    #region MOVEMENT ANIMATIONS
    
    /// <summary>
    /// Set movement speed (controls Walk animation)
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (animator == null) return;
        
        // Smooth damp to target speed
        currentSpeed = Mathf.SmoothDamp(
            currentSpeed,
            speed * animationSpeedMultiplier,
            ref speedVelocity,
            speedSmoothTime
        );
        
        animator.SetFloat(PARAM_WALK, currentSpeed);
        
        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[GolemBossAnimator] Speed: {currentSpeed:F2} (target: {speed:F2})");
        }
    }
    
    /// <summary>
    /// Force idle animation
    /// </summary>
    public void ForceIdle()
    {
        if (animator == null) return;
        
        SetSpeed(0f);
        animator.Play(STATE_IDLE, 0, 0f);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] → IDLE");
        }
    }
    
    /// <summary>
    /// Play idle action animation
    /// </summary>
    public void PlayIdleAction()
    {
        if (animator == null) return;
        
        animator.SetTrigger(PARAM_IDLE_ACTION);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 🎭 Idle Action");
        }
    }
    
    #endregion
    
    #region COMBAT ANIMATIONS
    
    /// <summary>
    /// Play basic attack (Hit or Hit2)
    /// </summary>
    public void PlayBasicAttack()
    {
        if (animator == null) return;
        
        // Reset other attack triggers
        ResetAllTriggers();
        
        if (randomizeBasicAttacks)
        {
            // Alternate or random between Hit and Hit2
            if (Random.value > 0.5f)
            {
                animator.SetTrigger(PARAM_HIT);
                lastAttackVariation = 1;
                
                if (showDebugLogs)
                {
                    Debug.Log("[GolemBossAnimator] ⚔️ Basic Attack (Hit)");
                }
            }
            else
            {
                animator.SetTrigger(PARAM_HIT2);
                lastAttackVariation = 2;
                
                if (showDebugLogs)
                {
                    Debug.Log("[GolemBossAnimator] ⚔️ Basic Attack (Hit2)");
                }
            }
        }
        else
        {
            animator.SetTrigger(PARAM_HIT);
            
            if (showDebugLogs)
            {
                Debug.Log("[GolemBossAnimator] ⚔️ Basic Attack");
            }
        }
    }
    
    /// <summary>
    /// Play combo attack (Hit → Hit2 sequence)
    /// </summary>
    public void PlayComboAttack()
    {
        if (animator == null) return;
        
        ResetAllTriggers();
        
        // Trigger both for combo
        animator.SetTrigger(PARAM_HIT);
        
        // Delay second hit
        Invoke(nameof(TriggerSecondHit), 0.5f);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 🥊 COMBO ATTACK!");
        }
    }
    
    private void TriggerSecondHit()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_HIT2);
        }
    }
    
    /// <summary>
    /// Play ground slam (using Jump → Land sequence)
    /// </summary>
    public void PlayGroundSlam()
    {
        if (animator == null) return;
        
        ResetAllTriggers();
        animator.SetTrigger(PARAM_JUMP);
        
        // Auto-trigger land after jump
        Invoke(nameof(TriggerLand), 1f);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 🌍 GROUND SLAM!");
        }
    }
    
    private void TriggerLand()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_LAND);
        }
    }
    
    /// <summary>
    /// Play rage attack
    /// </summary>
    public void PlayRageAttack()
    {
        if (animator == null) return;
        
        ResetAllTriggers();
        animator.SetTrigger(PARAM_RAGE);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 💥 RAGE ATTACK!");
        }
    }
    
    #endregion
    
    #region SPECIAL ANIMATIONS
    
    /// <summary>
    /// Play roar animation (using Rage trigger)
    /// </summary>
    public void PlayRoar()
    {
        if (animator == null) return;
        
        ResetAllTriggers();
        animator.SetTrigger(PARAM_RAGE);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 🦁 ROAR!");
        }
    }
    
    /// <summary>
    /// Play heal animation (using SleepStart as meditation/heal pose)
    /// </summary>
    public void PlayHeal()
    {
        if (animator == null) return;
        
        ResetAllTriggers();
        animator.SetTrigger(PARAM_SLEEP_START);
        
        // Auto end heal after some time
        Invoke(nameof(EndHeal), 3f);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 💚 HEALING!");
        }
    }
    
    private void EndHeal()
    {
        if (animator != null)
        {
            animator.SetTrigger(PARAM_SLEEP_END);
        }
    }
    
    /// <summary>
    /// Play phase transition animation (Rage for dramatic effect)
    /// </summary>
    public void PlayPhaseTransition(GolemBossAI.BossPhase newPhase)
    {
        if (animator == null) return;
        
        ResetAllTriggers();
        
        switch (newPhase)
        {
            case GolemBossAI.BossPhase.Phase2_Aggressive:
                animator.SetTrigger(PARAM_RAGE);
                break;
                
            case GolemBossAI.BossPhase.Phase3_Enraged:
                animator.SetTrigger(PARAM_RAGE);
                // Could trigger multiple rage animations for more intensity
                Invoke(nameof(PlayRageAttack), 1.5f);
                break;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[GolemBossAnimator] 🔥 PHASE TRANSITION → {newPhase}");
        }
    }
    
    /// <summary>
    /// Play damage reaction
    /// </summary>
    public void PlayDamage()
    {
        if (animator == null) return;
        
        animator.SetTrigger(PARAM_DAMAGE);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 💔 Damage Taken");
        }
    }
    
    /// <summary>
    /// Play death animation
    /// </summary>
    public void PlayDeath()
    {
        if (animator == null) return;
        
        ResetAllTriggers();
        animator.SetTrigger(PARAM_DIE);
        
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 💀 DEATH!");
        }
    }
    
    #endregion
    
    #region UTILITY
    
    /// <summary>
    /// Reset all trigger parameters
    /// </summary>
    private void ResetAllTriggers()
    {
        if (animator == null) return;
        
        foreach (var param in animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                animator.ResetTrigger(param.name);
            }
        }
    }
    
    /// <summary>
    /// Check if currently playing specific animation
    /// </summary>
    public bool IsPlayingAnimation(string stateName)
    {
        if (animator == null) return false;
        
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }
    
    /// <summary>
    /// Get current animation normalized time (0-1)
    /// </summary>
    public float GetCurrentAnimationTime()
    {
        if (animator == null) return 0f;
        
        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
    }
    
    /// <summary>
    /// Check if animation has finished
    /// </summary>
    public bool HasAnimationFinished(string stateName)
    {
        if (animator == null) return true;
        
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stateName) && stateInfo.normalizedTime >= 1f;
    }
    
    /// <summary>
    /// Log available animator parameters (debug)
    /// </summary>
    private void LogAvailableParameters()
    {
        if (animator == null) return;
        
        Debug.Log("[GolemBossAnimator] Available Parameters:");
        foreach (var param in animator.parameters)
        {
            Debug.Log($"   - {param.name} ({param.type})");
        }
    }
    
    #endregion
    
    #region ANIMATION EVENTS
    
    // These methods can be called from Animation Events in Unity
    
    /// <summary>
    /// Animation Event: Attack hit frame
    /// </summary>
    public void OnAttackHit()
    {
        // This is called at the exact frame where attack should deal damage
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 💥 Attack Hit Frame!");
        }
        
        // Notify GolemBossAttacks to deal damage
        var bossAttacks = GetComponent<GolemBossAttacks>();
        if (bossAttacks != null)
        {
            bossAttacks.OnAttackHitFrame();
        }
    }
    
    /// <summary>
    /// Animation Event: Ground slam impact
    /// </summary>
    public void OnGroundSlamImpact()
    {
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 💥 Ground Slam Impact!");
        }
        
        var bossAttacks = GetComponent<GolemBossAttacks>();
        if (bossAttacks != null)
        {
            bossAttacks.OnGroundSlamImpact();
        }
    }
    
    /// <summary>
    /// Animation Event: Rage wave release
    /// </summary>
    public void OnRageWaveRelease()
    {
        if (showDebugLogs)
        {
            Debug.Log("[GolemBossAnimator] 💥 Rage Wave!");
        }
        
        var bossAttacks = GetComponent<GolemBossAttacks>();
        if (bossAttacks != null)
        {
            bossAttacks.OnRageWaveRelease();
        }
    }
    
    /// <summary>
    /// Animation Event: Footstep sound
    /// </summary>
    public void OnFootstep()
    {
        // Play footstep sound effect here
        if (showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log("[GolemBossAnimator] 👣 Footstep");
        }
    }
    
    #endregion
}
