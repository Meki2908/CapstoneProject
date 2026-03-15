using UnityEngine;
public class SprintState : State
{
    float gravityValue;
    Vector3 currentVelocity;

    bool grounded;
    bool sprint;
    bool dash;
    float playerSpeed;
    bool sprintJump;
    Vector3 cVelocity;

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
        gravityVelocity.y = 0;

        playerSpeed = character.sprintSpeed;
        grounded = character.controller.isGrounded;
        gravityValue = character.gravityValue;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        input = moveAction.ReadValue<Vector2>();
        velocity = new Vector3(input.x, 0, input.y);

        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

        velocity = velocity.x * camRight + velocity.z * camForward;
        if (velocity.sqrMagnitude > 1f) velocity.Normalize();
        velocity.y = 0f;

        bool sprintButtonHeld = sprintAction.IsPressed();

        // Sprint is active if: sprint button is held AND there is movement input
        if (sprintButtonHeld && input.sqrMagnitude > 0f)
        {
            sprint = true;
        }
        else
        {
            sprint = false;
        }

        if (jumpAction.triggered)
        {
            sprintJump = true;

        }
        if (dashAction.triggered)
        {
            dash = true;
        }
    }

    public override void LogicUpdate()
    {
        if (sprint)
        {
            character.animator.SetFloat("speed", input.magnitude + 0.5f, character.speedDampTime, Time.deltaTime);
        }
        else if (input.sqrMagnitude == 0f) // chỉ khi buông hết phím di chuyển mới HardStop
        {
            stateMachine.ChangeState(character.hardStop);
        }
        else
        {
            stateMachine.ChangeState(character.currentLocomotionState);
        }

        if (sprintJump)
        {
            stateMachine.ChangeState(character.sprintjumping);
        }
        if (dash)
        {
            stateMachine.ChangeState(character.dashing);
        }

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        gravityVelocity.y += gravityValue * Time.deltaTime;
        grounded = character.controller.isGrounded;
        if (grounded && gravityVelocity.y < 0)
        {
            gravityVelocity.y = 0f;
        }
        currentVelocity = Vector3.SmoothDamp(currentVelocity, velocity, ref cVelocity, character.velocityDampTime);

        // Apply movement speed multiplier from equipped gems
        float speedMultiplier = 1f;
        var wc = character.GetComponent<WeaponController>();
        if (wc != null && wc.GetCurrentWeapon() != null && WeaponGemManager.Instance != null)
        {
            speedMultiplier = WeaponGemManager.Instance.GetMovementSpeedMultiplier(wc.GetCurrentWeapon().weaponType);
        }
        character.controller.Move(currentVelocity * Time.deltaTime * (playerSpeed * speedMultiplier) + gravityVelocity * Time.deltaTime);


        if (velocity.sqrMagnitude > 0)
        {
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.LookRotation(velocity), character.rotationDampTime);
        }
    }
    public override void Exit()
    {
        base.Exit();
    }
}