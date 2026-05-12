using UnityEngine;

public class GetHitState : State
{
    bool dash;
    bool jump;
    bool toBaseMove;
    bool toggleWeapon; // allow queue Draw/Sheath while getting hit
    
    // BỎ LOGIC DELAY: Set thời gian stun = 0 theo yêu cầu để bỏ hoàn toàn delay.
    // Kết hợp với layer "Upper body", player có thể đánh/chạy ngay cả khi animation đang chạy!
    float hitDuration = 0.0f; 
    float hitTimer;

    private WeaponController weaponController;

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

        if (weaponController == null)
            weaponController = character.GetComponent<WeaponController>();

        if (weaponController != null && !character.isWeaponDrawn)
            weaponController.SetWeaponLayersForSheathedGetHit();
        else if (weaponController != null)
            weaponController.ReapplyWeaponLayers();

        if (character.animator != null)
            character.SetTriggerSafe("gethit");
    }

    public override void HandleInput()
    {
        base.HandleInput();

        // TEMPORARILY DISABLE DASH DURING HIT to fix auto-dash issue
        // The user reported player auto-dashes when hit - this should only happen with right mouse button
        // For now, disable dash input during hit to prevent auto-dash
        // Re-enable this if you want dash during hit:
        // if (DashTriggered)
        // {
        //     dash = true;
        // }
        
        if (character.TryConsumeJumpBuffered(character.canStartJump))
        {
            jump = true;
        }

        // NEW: cho phép rút/cất/đổi vũ khí khi đang bị đánh
        if (ToggleWeaponTriggered)
        {
            toggleWeapon = true;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Update timer
        hitTimer -= character.Runner.DeltaTime;

        // Allow transition to base move after hit duration
        if (hitTimer <= 0)
        {
            toBaseMove = true;
        }

        // Priority: ToggleWeapon > Dash > Jump > Resume Attack > BaseMove
        if (toggleWeapon)
        {
            // Buffer exactly once: store requested action at the moment of Tab press.
            character.QueueWeaponActionFromCurrentContext();
            toggleWeapon = false;
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
            character.dashLockUntil = character.Runner.SimulationTime + 0.1f; // 0.1s sau khi hết hit mới cho phép dash lại

            // NOTE: Không được "xả hàng đợi" ở GetHitState nữa.
            // Chỉ trả về locomotion/attack. Việc xả queued Draw/Sheath sẽ được đồng bộ theo Animator tại BaseMoveState.
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
               (character.Runner.SimulationTime - character.lastAttackInputTime < 1.0f || // Within 1 second
                (character.attacking != null && character.movementSM.currentState == character.attacking));
    }

    private void ResumeAttackState()
    {
        stateMachine.ChangeState(character.attacking);
    }

    public override void Exit()
    {
        base.Exit();
        if (weaponController != null)
            weaponController.ReapplyWeaponLayers();
    }
}



