using UnityEngine;

public class JumpingState : State
{
    bool grounded;
    bool landTriggered;

    float gravityValue;
    float jumpHeight;
    float playerSpeed;

    Vector3 airVelocity;

    public JumpingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        grounded = false;
        landTriggered = false;
        character.canStartJump = false;
        gravityValue = character.gravityValue;
        jumpHeight = character.jumpHeight;
        playerSpeed = character.playerSpeed;

        character.animator.SetFloat("speed", 0);
        character.animator.SetTrigger("jump");
        character.NotifyJumpImpulseStarted();
        Jump();
        TutorialTextDisplay.NotifyJumpStartedFromGameplay();
    }
    public override void HandleInput()
    {
        base.HandleInput();

        input = MoveInput;
        // Trong state này không xét jump; Space không đổi Y — impulse đã được gán trong Enter().
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            input = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (grounded)
        {
            if (!landTriggered)
            {
                // Base Layer Jumping Up -> LightLanding uses the "land" trigger.
                character.animator.SetTrigger("land");
                landTriggered = true;
            }
            // Return to the active locomotion context (combat/standing) instead of forcing standing.
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (!grounded)
        {

            velocity = character.playerVelocity;
            airVelocity = new Vector3(input.x, 0, input.y);

            GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

            velocity = velocity.x * camRight + velocity.z * camForward;
            velocity.y = 0f;
            airVelocity = airVelocity.x * camRight + airVelocity.z * camForward;
            if (airVelocity.sqrMagnitude > 1f) airVelocity.Normalize();
            airVelocity.y = 0f;
            Vector3 movement = (airVelocity * character.airControl + velocity * (1 - character.airControl)) * playerSpeed;
            character.CalculatedVelocity.x = movement.x;
            character.CalculatedVelocity.z = movement.z;

            if (movement.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(movement.x, 0, movement.z));
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
            }
        }
        grounded = character.controller.isGrounded;
    }

    void Jump()
    {
        character.playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
    }

}




