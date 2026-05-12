using UnityEngine;

public class HardStopState : State
{
    private float stopDuration = 0.5f; // Duration of the hard stop
    private float stopTimer;
    private Vector3 decelerationVelocity; // Velocity to decelerate the player
    private Quaternion initialFacingDirection; // The direction the character was facing during the dash

    public HardStopState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // Set the stop timer
        stopTimer = stopDuration;

        // Initialize deceleration velocity with the player's current velocity
        decelerationVelocity = character.PlayerVelocity;

        // Store the character's current facing direction
        initialFacingDirection = character.transform.rotation;

        // Trigger the hard stop animation
        if (character.animator)
        {
            character.SetTriggerSafe("hardStop");
            character.SetAnimatorLocomotionSpeed(0f);
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Decrease the stop timer
        stopTimer -= character.Runner.DeltaTime;

        // Transition back to StandingState when the stop animation is complete
        if (stopTimer <= 0)
        {
            stateMachine.ChangeState(character.currentLocomotionState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Gradually reduce the player's velocity to simulate momentum
        decelerationVelocity = Vector3.Lerp(decelerationVelocity, Vector3.zero, character.Runner.DeltaTime / stopDuration);

        // Apply the deceleration to the player's movement
        character.controller.Move(decelerationVelocity * character.Runner.DeltaTime);

        // Keep the character facing the initial direction during the hard stop
        character.transform.rotation = initialFacingDirection;
    }

    public override void Exit()
    {
        base.Exit();

        // Reset the player's velocity when exiting the state
        character.PlayerVelocity = Vector3.zero;

        // Preserve the character's facing direction
        character.transform.rotation = initialFacingDirection;
    }
}

