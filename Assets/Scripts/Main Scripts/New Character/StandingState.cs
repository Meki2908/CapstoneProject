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

        if (toggleWeaponAction.triggered)
        {
            drawWeapon = true;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Ensure cooldown before processing input
        if (Time.time - lastToggleTime < toggleCooldown)
        {
            return; // Wait for cooldown to finish
        }

        if (drawWeapon && !character.isWeaponDrawn) // Transition to DrawWeaponState
        {
            lastToggleTime = Time.time; // Update the last toggle time

            character.isWeaponDrawn = true;
            character.currentLocomotionState = character.combatMove;

            // Set the drawWeapon trigger and reset sheathWeapon
            character.animator.SetTrigger("drawWeapon");
            character.animator.ResetTrigger("sheathWeapon");

            // Change to CombatMoveState
            stateMachine.ChangeState(character.currentLocomotionState);
        }
    }

    public override void Exit()
    {
        base.Exit();
    }
}