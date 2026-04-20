using UnityEngine;

public class GetHitState : State
{
    private const float SheathedLayerWeight = 1f;

    bool dash;
    bool jump;
    bool toBaseMove;
    bool toggleWeapon; // NEW: cho phép rút/cất vũ khí khi đang bị đánh
    
    // BỎ LOGIC DELAY: Set thời gian stun = 0 theo yêu cầu để bỏ hoàn toàn delay.
    // Kết hợp với layer UpperBody_Hit, player có thể đánh/chạy ngay cả khi animation đang chạy!
    float hitDuration = 0.0f; 
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
        toggleWeapon = false;
        hitTimer = hitDuration;

        // === FIX: Clear buffered dash input để tránh auto-dash khi exit GetHitState ===
        if (dashAction != null && dashAction.triggered)
        {
            Debug.Log("[GetHitState] Consumed buffered dash input");
        }

        if (weaponController == null)
        {
            weaponController = character.GetComponent<WeaponController>();
        }

        if (character.animator != null)
        {
            // BỎ HẾT logic ép Weapon Layers = 1 vì user đã gộp GetHit vào UpperBody_Hit mask
            // Chỉ cần set trigger, Layer UpperBody_Hit sẽ tự bắt (kể cả cầm hay cất vũ khí)
            character.animator.SetTrigger("gethit");
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
        
        if (character.TryConsumeJumpBuffered(character.canStartJump))
        {
            jump = true;
        }

        // NEW: cho phép rút/cất/đổi vũ khí khi đang bị đánh
        if (toggleWeaponAction.triggered)
        {
            toggleWeapon = true;
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

        // Priority: ToggleWeapon > Dash > Jump > Resume Attack > BaseMove
        if (toggleWeapon)
        {
            // Chuyển về locomotion state ngay lập tức, BaseMoveState sẽ xử lý toggle
            // Set flag trên locomotion state để nó biết cần toggle
            var locomotion = character.currentLocomotionState;
            if (locomotion is BaseMoveState baseMoveState)
            {
                if (character.isWeaponDrawn)
                {
                    baseMoveState.sheathWeapon = true;
                    baseMoveState.drawWeapon = false;
                }
                else
                {
                    baseMoveState.drawWeapon = true;
                    baseMoveState.sheathWeapon = false;
                }
            }
            stateMachine.ChangeState(locomotion);
        }
        else if (dash)
        {
            stateMachine.ChangeState(character.dashing);
        }
        else if (jump)
        {
            stateMachine.ChangeState(character.jumping);
        }
        else if (toBaseMove)
        {
            // Khi thoát khỏi GetHit, tạm thời khóa dash trong một khoảng rất ngắn
            // để tránh việc input dash bị buffer khiến player auto-dash
            character.dashLockUntil = Time.time + 0.1f; // 0.1s sau khi hết hit mới cho phép dash lại

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
        // Weapon layers are no longer manipulated by GetHitState
    }
}
