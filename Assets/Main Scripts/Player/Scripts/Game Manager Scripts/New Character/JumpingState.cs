using UnityEngine;

public class JumpingState : State
{
    const float MinJumpToFallTime = 0.06f;
    float gravityValue;
    float jumpHeight;
    float playerSpeed;

    Vector3 airVelocity;
    Vector3 lastPlanarAirVelocity;

    public JumpingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        character.jumpStartY = character.transform.position.y;

        character.lastJumpTime = character.Runner.SimulationTime;

        gravityValue = character.gravityValue;
        jumpHeight = character.jumpHeight;
        playerSpeed = character.playerSpeed;

        // Carry horizontal momentum from locomotion into air (CalculatedVelocity is still last tick before this state's PhysicsUpdate zeros it).
        airVelocity = new Vector3(character.CalculatedVelocity.x, 0f, character.CalculatedVelocity.z);
        lastPlanarAirVelocity = airVelocity;

        if (character.animator != null)
            character.animator.SetFloat("speed", 0);
        character.TrySetAnimatorBool("isGrounded", false);
        character.SetNetworkedKneeLanding(false);
        character.SetTriggerSafe("jump");
        character.NotifyJumpImpulseStarted();
        Jump();
        TutorialTextDisplay.NotifyJumpStartedFromGameplay();

        character.requireLanding = true;
    }

    public override void HandleInput()
    {
        base.HandleInput();

        input = MoveInput;
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            input = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Rollback-safe guard:
        // - cần đã qua một khoảng ngắn sau Enter
        // - cần thực sự rời đất (grounded stable false)
        // - cần vận tốc Y âm rõ ràng
        bool airborne = !character.IsGroundedStable();
        if (TimeInState >= MinJumpToFallTime && airborne && character.PlayerVelocity.y < -0.02f)
        {
            character.momentumToInherit = new Vector3(lastPlanarAirVelocity.x, 0f, lastPlanarAirVelocity.z);
            stateMachine.ChangeState(character.falling);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector2 moveInput = MoveInput;
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            moveInput = Vector2.zero;

        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

        Vector3 desiredDirection = (camRight * moveInput.x + camForward * moveInput.y).normalized;
        Vector3 desiredVelocity = desiredDirection * playerSpeed;

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            float steer = Mathf.Clamp01(character.airControl * 5f * character.Runner.DeltaTime);
            airVelocity = Vector3.Lerp(airVelocity, desiredVelocity, steer);
        }
        airVelocity.y = 0f;

        character.CalculatedVelocity.x = airVelocity.x;
        character.CalculatedVelocity.z = airVelocity.z;

        lastPlanarAirVelocity = airVelocity;

        if (airVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(airVelocity);
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
        }
    }

    void Jump()
    {
        var pvJ = character.PlayerVelocity;
        pvJ.y = Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
        character.PlayerVelocity = pvJ;
    }

    public override void Exit()
    {
        base.Exit();

        if (character != null)
        {
            Vector2 m = MoveInput;
            float targetSpeed = m.magnitude;
            if (character.currentLocomotionState == character.combatMove) targetSpeed *= 0.5f;
            character.SetAnimatorLocomotionSpeed(targetSpeed);
        }
    }
}
