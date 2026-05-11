using UnityEngine;
public class SprintState : State
{
    float gravityValue;
    Vector3 currentVelocity;

    bool grounded;
    bool sprint;
    bool dash;
    bool sprintJump;
    Vector3 cVelocity;
    private const float FallTimeout = 0.15f;

    public SprintState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        sprint = false;
        sprintJump = false;
        dash = false;
        input = Vector2.zero;
        velocity = Vector3.zero;
        currentVelocity = Vector3.zero;

        grounded = character.controller.isGrounded;
        gravityValue = character.gravityValue;
        character.NetFallTimer = 0f;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        input = MoveInput;
        velocity = new Vector3(input.x, 0, input.y);

        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

        velocity = velocity.x * camRight + velocity.z * camForward;
        if (velocity.sqrMagnitude > 1f) velocity.Normalize();
        velocity.y = 0f;

        bool sprintButtonHeld = SprintPressed;

        // Sprint is active if: sprint button is held AND there is movement input
        if (sprintButtonHeld && input.sqrMagnitude > 0f)
        {
            sprint = true;
        }
        else
        {
            sprint = false;
        }

        if (character.TryConsumeJumpBuffered(character.canStartJump))
        {
            sprintJump = true;
        }
        if (DashTriggered)
        {
            dash = true;
        }

        if (TutorialInputGate.IsActive)
        {
            var m = TutorialInputGate.EffectiveMask;
            if ((m & TutorialInputMask.Move) == 0)
            {
                input = Vector2.zero;
                velocity = Vector3.zero;
            }
            if ((m & TutorialInputMask.Jump) == 0) sprintJump = false;
            if ((m & TutorialInputMask.Dash) == 0) dash = false;
            if ((m & TutorialInputMask.Sprint) == 0) sprint = false;
        }
    }

    public override void LogicUpdate()
    {
        // Fall Logic Timeout
        if (!grounded && character.PlayerVelocity.y < 0f)
        {
            character.NetFallTimer += character.Runner.DeltaTime;
            if (character.NetFallTimer >= FallTimeout)
            {
                character.NetFallTimer = 0f;
                stateMachine.ChangeState(character.falling);
                return;
            }
        }
        else
        {
            character.NetFallTimer = 0f;
        }

        if (sprintJump)
        {
            stateMachine.ChangeState(character.sprintjumping);
            return;
        }

        if (sprint)
        {
            character.SetAnimatorLocomotionSpeed(input.magnitude);
        }
        else if (input.sqrMagnitude == 0f) // chỉ khi buông hết phím di chuyển mới HardStop
        {
            stateMachine.ChangeState(character.hardStop);
        }
        else
        {
            stateMachine.ChangeState(character.currentLocomotionState);
        }
        if (dash)
        {
            stateMachine.ChangeState(character.dashing);
        }

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Guard: skip nếu CharacterController bị disable (vd: đang teleport)
        if (character.controller == null || !character.controller.enabled) return;
        // Use stable grounded (feet cast + small grace) to avoid flicker on edges/uneven meshes.
        grounded = character.IsGroundedStable();
        currentVelocity = Vector3.SmoothDamp(currentVelocity, velocity, ref cVelocity, character.velocityDampTime);

        // Sprint speed already includes gem/equipment via UpdateSpeedWithGems on character.sprintSpeed (do not multiply gems again).
        character.CalculatedVelocity = currentVelocity * character.sprintSpeed;


        if (velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
        }
    }
    public override void Exit()
    {
        base.Exit();
    }
}




