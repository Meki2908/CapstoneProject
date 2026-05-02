using UnityEngine;

public class StandingState : BaseMoveState
{
    private float toggleCooldown = 0.5f; // Cooldown duration
    private float lastToggleTime = 0;   // Tracks the last toggle time
    Vector3 cVelocity;

    public StandingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();
        drawWeapon = false;
        sheathWeapon = true;
        character.isWeaponDrawn = false;
        //Debug.Log("Standing State");
    }

    public override void HandleInput()
    {
        base.HandleInput();

        if (ToggleWeaponTriggered
            && (!TutorialInputGate.IsActive || TutorialInputGate.Allows(TutorialInputMask.ToggleWeapon)))
        {
            drawWeapon = true;
        }
        else if (TutorialInputGate.IsActive && !TutorialInputGate.Allows(TutorialInputMask.ToggleWeapon))
        {
            drawWeapon = false;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (character.Runner.SimulationTime - lastToggleTime < toggleCooldown)
        {
            return;
        }

        if (drawWeapon && !character.isWeaponDrawn) 
        {
            lastToggleTime = character.Runner.SimulationTime; 

            // Cập nhật vũ khí hiện tại lên Animator trước khi rút
            var wc = character.GetComponent<WeaponController>();
            if (wc != null && wc.GetCurrentWeapon() != null)
            {
                int wepType = (int)wc.GetCurrentWeapon().weaponType;
                // Cập nhật cả 2 parameter như mình bàn ở trước!
                character.animator.SetInteger("weaponType", wepType);
                character.animator.SetFloat("WeaponIndex", (float)wepType);
                character.animator.SetBool("combatMove", true);
            }

            character.isWeaponDrawn = true;
            character.currentLocomotionState = character.combatMove;
            TutorialTextDisplay.NotifyWeaponDrawnFromGameplay();

            character.animator.ResetTrigger("sheathWeapon");
            character.animator.SetTrigger("drawWeapon");

            stateMachine.ChangeState(character.currentLocomotionState);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}

