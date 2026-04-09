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
        gravityVelocity.y = 0;

        character.animator.SetFloat("speed", 0);
        character.animator.SetTriggerNetworked("jump");
        character.NotifyJumpImpulseStarted();
        Jump();
    }
    public override void HandleInput()
    {
        base.HandleInput();

        input = moveAction.ReadValue<Vector2>();
        // Trong state này không xét jump; Space không đổi Y — impulse đã được gán trong Enter().
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (grounded)
        {
            if (!landTriggered)
            {
                // Base Layer Jumping Up -> LightLanding uses the "land" trigger.
                character.animator.SetTriggerNetworked("land");
                landTriggered = true;
            }
            // Return to the active locomotion context (combat/standing) instead of forcing standing.
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        var cc = character.controller;
        if (cc == null || !cc.enabled)
            return;

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
            cc.Move(gravityVelocity * Time.deltaTime + (airVelocity * character.airControl + velocity * (1 - character.airControl)) * playerSpeed * Time.deltaTime);
        }

        gravityVelocity.y += gravityValue * Time.deltaTime;
        grounded = character.controller.isGrounded;
    }

    void Jump()
    {
        gravityVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
    }

}
