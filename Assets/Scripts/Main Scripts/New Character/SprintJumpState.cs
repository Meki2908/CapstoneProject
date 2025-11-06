using UnityEngine;

public class SprintJumpState : State
{
    private float animLength;
    private float timePassed;
    private Vector3 jumpVelocity;

    public SprintJumpState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        // Enable root motion to allow animation-driven movement
        character.animator.applyRootMotion = true;

        // Play sprintJump animation
        character.animator.SetTrigger("sprintJump");

        // Get jump animation length
        AnimatorStateInfo info = character.animator.GetCurrentAnimatorStateInfo(0);
        animLength = info.length;

        timePassed = 0f;

        // Initialize forward velocity for the jump (in the direction the character is facing)
        Vector3 forwardDirection = character.transform.forward;
        jumpVelocity = forwardDirection * character.sprintSpeed; // Use sprintSpeed for consistent forward push
        jumpVelocity.y = Mathf.Sqrt(-2.0f * character.gravityValue * character.jumpHeight); // Apply jump height
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        timePassed += Time.deltaTime;

        // Transition back to Sprinting state near animation end
        if (timePassed >= animLength * 0.95f) // 95% to avoid frame mismatch
        {
            stateMachine.ChangeState(character.sprinting);
            character.animator.SetTrigger("move"); // Trigger Move Blend Tree for smooth transition
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Apply gravity
        if (!character.controller.isGrounded)
        {
            jumpVelocity.y += character.gravityValue * Time.fixedDeltaTime;
        }
        else
        {
            jumpVelocity.y = 0f; // Reset vertical velocity when grounded
        }

        // Apply forward movement (combined with root motion)
        character.controller.Move(jumpVelocity * Time.fixedDeltaTime);
    }

    public override void Exit()
    {
        base.Exit();
        // Disable root motion to return control to code
        character.animator.applyRootMotion = false;

        // Ensure smooth transition by preserving some velocity for sprinting
        character.playerVelocity = character.transform.forward * character.sprintSpeed;
        character.playerVelocity.y = 0f; // Ensure no residual vertical velocity
        character.animator.SetFloat("speed", 1f); // Set speed for Move Blend Tree to match sprinting
    }
}