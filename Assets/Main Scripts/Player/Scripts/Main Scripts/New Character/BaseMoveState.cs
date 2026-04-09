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

    private SkillLock skillLock; // NEW

    public BaseMoveState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
        skillLock = character.GetComponent<SkillLock>(); // NEW
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

        character.canStartJump = true;
    }

    public override void HandleInput()
    {
        base.HandleInput();

        // Read input first before checking sprint condition
        // Read input and calculate movement direction relative to the camera
        input = moveAction.ReadValue<Vector2>();
        velocity = new Vector3(input.x, 0, input.y);

        // Align movement direction with stable planar camera basis (ignore camera pitch)
        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

        velocity = velocity.x * camRight + velocity.z * camForward;
        if (velocity.sqrMagnitude > 1f) velocity.Normalize();
        velocity.y = 0f; // Ensure no vertical movement

        if (skillLock != null && skillLock.isPerformingSkill)
        {
            input = Vector2.zero;
            velocity = Vector3.zero;
            return;
        }

        // Ray hai chân + jump buffer (Character.Update) — không phụ thuộc CC.isGrounded một frame.
        if (character.TryConsumeJumpBuffered(character.canStartJump))
        {
            jump = true;
        }
        if (crouchAction.triggered)
        {
            crouch = true;
        }
        // Check if sprint button is being held (IsPressed) AND there is movement input
        // This ensures sprint activates immediately when Shift is held while moving
        if (sprintAction.IsPressed() && input.sqrMagnitude > 0f)
        {
            sprint = true;
        }
        else
        {
            sprint = false; // Reset sprint flag when conditions aren't met
        }

        // Safety: nếu vì lý do nào đó dashLockUntil bị set quá xa trong tương lai
        // (ví dụ sau khi load scene / editor play-stop-play), thì reset về 0 để tránh khóa dash vĩnh viễn
        if (character.dashLockUntil > Time.time + 5f)
        {
            character.dashLockUntil = 0f;
        }

        // Dash chỉ được phép khi không bị khóa (vd: ngay sau khi thoát GetHitState)
        if (Time.time >= character.dashLockUntil && dashAction.triggered)
        {
            dash = true;
            Debug.Log("[BaseMoveState] Dash input TRIGGERED, dashLockUntil: " + character.dashLockUntil + ", Time= " + Time.time);
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

        ApplyTutorialInputGate();
    }

    void ApplyTutorialInputGate()
    {
        if (!TutorialInputGate.IsActive) return;
        var m = TutorialInputGate.EffectiveMask;
        if ((m & TutorialInputMask.Move) == 0)
        {
            input = Vector2.zero;
            velocity = Vector3.zero;
        }
        if ((m & TutorialInputMask.Jump) == 0) jump = false;
        if ((m & TutorialInputMask.Crouch) == 0) crouch = false;
        if ((m & TutorialInputMask.Sprint) == 0) sprint = false;
        if ((m & TutorialInputMask.Dash) == 0) dash = false;
        if ((m & TutorialInputMask.ToggleWeapon) == 0)
        {
            drawWeapon = false;
            sheathWeapon = false;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        gravityVelocity.y += gravityValue * Time.deltaTime;
        grounded = character.controller.isGrounded;
        if (grounded && gravityVelocity.y < 0) gravityVelocity.y = 0f;

        // NEW: đang skill -> chỉ gravity Y
        if (skillLock != null && skillLock.isPerformingSkill)
        {
            character.controller.Move(new Vector3(0f, gravityVelocity.y, 0f) * Time.deltaTime);
            return;
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
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        character.SetAnimatorLocomotionSpeed(input.magnitude);

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