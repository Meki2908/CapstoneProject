using UnityEngine;

public class SprintJumpState : State
{
    private Vector3 horizontalDirection;
    private float verticalVelocity;
    private bool hasLeftGround;
    private bool landingTriggered;

    public SprintJumpState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        // Drive jump by physics so this state actually moves on Y.
        character.animator.applyRootMotion = false;

        // Play sprintJump animation
        character.animator.SetTrigger("sprintJump");

        hasLeftGround = false;
        landingTriggered = false;

        // Initialize jump impulse (use dedicated sprint-jump height, fallback to normal jump height)
        float sprintJumpHeight = character.sprintJumpHeight > 0f ? character.sprintJumpHeight : character.jumpHeight;
        verticalVelocity = Mathf.Sqrt(sprintJumpHeight * -2.0f * character.gravityValue);

        // Initialize horizontal movement based on current move input, fallback to facing direction
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);
        horizontalDirection = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        if (horizontalDirection.sqrMagnitude < 0.0001f)
        {
            horizontalDirection = character.transform.forward;
            horizontalDirection.y = 0f;
            horizontalDirection.Normalize();
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        bool grounded = character.controller.isGrounded;
        if (!grounded)
        {
            hasLeftGround = true;
        }

        if (grounded && hasLeftGround && verticalVelocity <= 0f)
        {
            if (!landingTriggered)
            {
                character.animator.SetTrigger("land");
                landingTriggered = true;
            }

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            bool keepSprinting = sprintAction.IsPressed() && moveInput.sqrMagnitude > 0f;
            stateMachine.ChangeState(keepSprinting ? character.sprinting : character.currentLocomotionState);
            character.animator.SetTrigger("move");
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);
        Vector3 desiredDirection = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            // Light air steering while preserving sprint-jump feel
            horizontalDirection = Vector3.Slerp(horizontalDirection, desiredDirection, character.airControl * Time.fixedDeltaTime * 6f);
            horizontalDirection.y = 0f;
            horizontalDirection.Normalize();
        }

        if (character.controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = 0f;
        }

        verticalVelocity += character.gravityValue * Time.fixedDeltaTime;

        Vector3 horizontalVelocity = horizontalDirection * character.sprintSpeed;
        Vector3 movement = horizontalVelocity * Time.fixedDeltaTime + Vector3.up * (verticalVelocity * Time.fixedDeltaTime);
        character.controller.Move(movement);

        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            character.transform.rotation = Quaternion.Slerp(
                character.transform.rotation,
                Quaternion.LookRotation(horizontalVelocity),
                character.rotationDampTime
            );
        }
    }

    public override void Exit()
    {
        base.Exit();
        character.animator.applyRootMotion = false;

        character.playerVelocity = horizontalDirection * character.sprintSpeed;
        character.playerVelocity.y = 0f;
        character.animator.SetFloat("speed", 1f);
    }
}