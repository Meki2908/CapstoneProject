using UnityEditor.Timeline.Actions;
using UnityEngine;
public class CombatMoveState : BaseMoveState
{
    private float toggleCooldown = 0.5f; // Cooldown duration
    private float lastToggleTime = 0;   // Tracks the last toggle time
    bool attack;

    Vector3 cVelocity;

    public CombatMoveState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        drawWeapon = true;
        sheathWeapon = false;
        attack = false;
        character.isWeaponDrawn = true;
        //Debug.Log("Combat Move State");
    }

    public override void HandleInput()
    {
        base.HandleInput();

        if (toggleWeaponAction.triggered)
        {
            sheathWeapon = true;
        }

        if (attackAction.triggered)
        {
            attack = true;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Ensure cooldown before processing input
        if (Time.time - lastToggleTime < toggleCooldown)
        {
            return; // Wait for cooldown to finish
        }
        Debug.Log($"[CombatMoveState] sheathWeapon: {sheathWeapon}, isWeaponDrawn: {character.isWeaponDrawn}");
        if (sheathWeapon && character.isWeaponDrawn) // Transition to StandingState
        {
            lastToggleTime = Time.time; // Update the last toggle time

            character.isWeaponDrawn = false;
            character.currentLocomotionState = character.standing;

            // Set the sheathWeapon trigger and reset drawWeapon
            character.animator.SetTrigger("sheathWeapon");
            character.animator.ResetTrigger("drawWeapon");

            // Change to StandingState
            stateMachine.ChangeState(character.currentLocomotionState);
        }

        if (attack) // Transition to AttackingState
        {
            // Set the attack trigger
            character.animator.SetTrigger("attack");

            // Change to AttackingState
            stateMachine.ChangeState(character.attacking);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void Exit()
    {
        base.Exit();

    }

}