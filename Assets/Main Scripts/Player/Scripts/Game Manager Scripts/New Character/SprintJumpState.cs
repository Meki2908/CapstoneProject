using UnityEngine;

public class SprintJumpState : State
{
    private Vector3 horizontalDirection;

    public SprintJumpState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        // LƯU LẠI TỌA ĐỘ Y LÚC BẮT ĐẦU NHẢY
        character.jumpStartY = character.transform.position.y;

        character.lastJumpTime = character.Runner.SimulationTime; 
        character.NotifyJumpImpulseStarted();

        // Drive jump by physics so this state actually moves on Y.
        character.animator.applyRootMotion = false;

        // Play sprintJump animation
        character.animator.SetTrigger("sprintJump");

        // Initialize jump impulse (use dedicated sprint-jump height, fallback to normal jump height)
        float sprintJumpHeight = character.sprintJumpHeight > 0f ? character.sprintJumpHeight : character.jumpHeight;
        character.playerVelocity.y = Mathf.Sqrt(sprintJumpHeight * -2.0f * character.gravityValue);

        // Initialize horizontal movement based on current move input, fallback to facing direction
        Vector2 moveInput = MoveInput;
        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);
        horizontalDirection = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        if (horizontalDirection.sqrMagnitude < 0.0001f)
        {
            horizontalDirection = character.transform.forward;
            horizontalDirection.y = 0f;
            horizontalDirection.Normalize();
        }

        character.requireLanding = true;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // Khi bắt đầu rơi xuống (vận tốc Y <= 0), nhường xử lý tiếp đất cho FallingState
        // (đảm bảo chỉ Landing khi đủ cao hoặc requireLanding = true).
        if (character.playerVelocity.y <= 0f)
            stateMachine.ChangeState(character.falling);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector2 moveInput = MoveInput;
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            moveInput = Vector2.zero;
        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);
        Vector3 desiredDirection = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        if (desiredDirection.sqrMagnitude > 0.0001f)
        {
            // Light air steering while preserving sprint-jump feel
            horizontalDirection = Vector3.Slerp(horizontalDirection, desiredDirection, character.airControl * Time.fixedDeltaTime * 6f);
            horizontalDirection.y = 0f;
            horizontalDirection.Normalize();
        }

        Vector3 horizontalVelocity = horizontalDirection * character.sprintSpeed;
        Vector3 movement = horizontalVelocity;
        character.CalculatedVelocity = movement;

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
        
        // Snap blend "speed" theo input hiện tại để tiếp đất/đổi state mượt (giống JumpingState)
        if (character != null)
        {
            Vector2 m = MoveInput;
            float targetSpeed = m.magnitude;
            if (character.currentLocomotionState == character.combatMove) targetSpeed *= 0.5f;
            character.SetAnimatorLocomotionSpeed(targetSpeed);
        }
    }
}




