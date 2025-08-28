using UnityEngine;

public class BaseMoveState : State
{
    float gravityValue;
    bool jump;
    bool crouch;
    bool dash;
    Vector3 currentVelocity;
    bool grounded;
    bool sprint;
    float playerSpeed;
    public bool sheathWeapon;
    public bool drawWeapon;

    Vector3 cVelocity;

    public BaseMoveState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        jump = false;
        crouch = false;
        sprint = false;
        dash = false; // Initialize dash to false
        input = Vector2.zero;
        velocity = Vector3.zero;
        currentVelocity = Vector3.zero;
        gravityVelocity.y = 0;

        playerSpeed = character.playerSpeed;
        grounded = character.controller.isGrounded;
        gravityValue = character.gravityValue;
    }

    public override void HandleInput()
    {
        base.HandleInput();

        if (jumpAction.triggered)
        {
            jump = true;
        }
        if (crouchAction.triggered)
        {
            crouch = true;
        }
        if (sprintAction.triggered)
        {
            sprint = true;
        }
        if (dashAction.triggered)
        {
            dash = true;
        }
        if (toggleWeaponAction.triggered)
        {
            if (character.isWeaponDrawn)
            {
                sheathWeapon = true;
                drawWeapon = false; // Prevent triggering both
            }
            else
            {
                drawWeapon = true;
                sheathWeapon = false; // Prevent triggering both
            }
        }
        // Read input and calculate movement direction relative to the camera
        input = moveAction.ReadValue<Vector2>();
        velocity = new Vector3(input.x, 0, input.y);

        // Align movement direction with the camera's forward and right vectors
        velocity = velocity.x * character.cameraTransform.right.normalized + velocity.z * character.cameraTransform.forward.normalized;
        velocity.y = 0f; // Ensure no vertical movement
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
        character.controller.Move(currentVelocity * Time.deltaTime * playerSpeed + gravityVelocity * Time.deltaTime);

        // Smoothly rotate the character to face the movement direction
        if (velocity.sqrMagnitude > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        character.animator.SetFloat("speed", input.magnitude, character.speedDampTime, Time.deltaTime);

        if (dash) // Transition to DashState if dash is triggered
        {
            stateMachine.ChangeState(character.dashing);
        }
        else if (sprint) // Transition to SprintState
        {
            stateMachine.ChangeState(character.sprinting);
        }
        else if (jump) // Transition to JumpingState
        {
            stateMachine.ChangeState(character.jumping);
        }
        else if (crouch) // Transition to CrouchingState
        {
            stateMachine.ChangeState(character.crouching);
        }
    }

    public override void Exit()
    {
        base.Exit();

        gravityVelocity.y = 0f;
        character.playerVelocity = new Vector3(input.x, 0, input.y);

        if (velocity.sqrMagnitude > 0)
        {
            character.transform.rotation = Quaternion.LookRotation(velocity);
        }
    }
}