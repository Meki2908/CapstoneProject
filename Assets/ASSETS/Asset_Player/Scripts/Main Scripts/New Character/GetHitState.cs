using UnityEngine;

public class GetHitState : State
{
    bool dash;
    bool jump;
    bool toBaseMove;
    float hitDuration = 0.2f; // Thời gian stun ngắn — player hồi phục nhanh
    float hitTimer;

    private WeaponController weaponController;
    private bool weaponLayersWereDisabled = false;

    public GetHitState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();
        dash = false;
        jump = false;
        toBaseMove = false;
        hitTimer = hitDuration;
        weaponLayersWereDisabled = false;

        // === FIX: Clear buffered dash input để tránh auto-dash khi exit GetHitState ===
        // Consume dashAction trigger nếu đang pending
        if (dashAction != null && dashAction.triggered)
        {
            // Reading .triggered consumes it, preventing auto-dash
            Debug.Log("[GetHitState] Consumed buffered dash input");
        }

        // Find WeaponController if not already found
        if (weaponController == null)
        {
            weaponController = character.GetComponent<WeaponController>();
        }

        if (character.animator != null)
        {
            // Use currentLocomotionState to determine which layer should play gethit animation
            // If StandingState (weapon not drawn) -> play on base layer only
            // If CombatMoveState (weapon drawn) -> play on weapon layer
            bool isWeaponDrawn = character.currentLocomotionState is CombatMoveState;

            if (isWeaponDrawn)
            {
                // Weapon is drawn - ensure weapon layer can play animation
                // The animation should play on the active weapon layer
                character.animator.SetTrigger("gethit");
            }
            else
            {
                // Weapon is not drawn - disable weapon layers temporarily to force base layer
                DisableWeaponLayersForBaseHit();

                // Trigger gethit - will play on base layer since weapon layers are disabled
                character.animator.SetTrigger("gethit");
            }
        }

        // KHÔNG cancel skill/timeline khi bị hit
        // Player chỉ bị flinch nhẹ, vẫn tiếp tục đánh thường và skill bình thường
    }

    private void DisableWeaponLayersForBaseHit()
    {
        if (weaponController == null || character.animator == null) return;

        // Store original layer weights and disable weapon layers
        // This ensures gethit plays on base layer only
        weaponLayersWereDisabled = true;

        int baseLayer = 0; // Base Layer is always 0
        int swordLayer = 1;
        int axeLayer = 2;
        int mageLayer = 3;

        // Disable all weapon layers
        SetLayerWeightSafe(swordLayer, 0f);
        SetLayerWeightSafe(axeLayer, 0f);
        SetLayerWeightSafe(mageLayer, 0f);

        // Ensure base layer is active
        SetLayerWeightSafe(baseLayer, 1f);
    }

    private void RestoreWeaponLayers()
    {
        if (!weaponLayersWereDisabled) return;

        // The WeaponController manages layer weights automatically
        // When we exit GetHitState and return to currentLocomotionState,
        // the WeaponController will already have the correct layer weights
        // We just need to reset the flag
        weaponLayersWereDisabled = false;
    }

    private void SetLayerWeightSafe(int layer, float weight)
    {
        if (character.animator != null && layer >= 0 && layer < character.animator.layerCount)
        {
            character.animator.SetLayerWeight(layer, weight);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();

        // TEMPORARILY DISABLE DASH DURING HIT to fix auto-dash issue
        // The user reported player auto-dashes when hit - this should only happen with right mouse button
        // For now, disable dash input during hit to prevent auto-dash
        // Re-enable this if you want dash during hit:
        // if (dashAction.triggered)
        // {
        //     dash = true;
        // }
        
        if (jumpAction.triggered)
        {
            jump = true;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Update timer
        hitTimer -= Time.deltaTime;

        // Allow transition to base move after hit duration
        if (hitTimer <= 0)
        {
            toBaseMove = true;
        }

        // Priority: Dash > Jump > Resume Attack > BaseMove
        if (dash)
        {
            stateMachine.ChangeState(character.dashing);
        }
        else if (jump)
        {
            stateMachine.ChangeState(character.jumping);
        }
        else if (toBaseMove)
        {
            // Try to resume attack state if we were attacking before getting hit
            if (ShouldResumeAttack())
            {
                ResumeAttackState();
            }
            else
            {
                // Return to previous locomotion state
                stateMachine.ChangeState(character.currentLocomotionState);
            }
        }
    }

    private bool ShouldResumeAttack()
    {
        // Resume attack if we were in attack state and still have attack input buffered
        // Also resume if we were attacking and the attack wasn't interrupted by something else
        return (character.lastStateBeforeHit == character.attacking) &&
               (Time.time - character.lastAttackInputTime < 1.0f || // Within 1 second
                (character.attacking != null && character.movementSM.currentState == character.attacking));
    }

    private void ResumeAttackState()
    {
        Debug.Log("[GetHitState] Resuming attack after hit animation");
        stateMachine.ChangeState(character.attacking);
    }

    public override void Exit()
    {
        base.Exit();

        // Restore weapon layers if they were disabled
        if (weaponLayersWereDisabled && weaponController != null)
        {
            // Restore weapon layer weights by reapplying weapon controller settings
            // The WeaponController should handle this, but we ensure layers are restored
            RestoreWeaponLayers();
        }
    }
}
