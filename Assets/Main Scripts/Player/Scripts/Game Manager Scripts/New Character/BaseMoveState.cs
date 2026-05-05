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

    private SkillLock skillLock;
    private float currentSpeed;
    private float fallTimer = 0f;
    private const float FallTimeout = 0.15f;

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
        
        // 2. CH? L?Y "�? L?N" T?C �? HI?N T?I �? KH�NG B? VANG SAU KHI DASH
        currentSpeed = new Vector3(character.CalculatedVelocity.x, 0, character.CalculatedVelocity.z).magnitude;
        crouch = false;
        sprint = false;
        dash = false; // Initialize dash to false
        
        // FIX: Bỏ reset velocity ở đây để giữ lại gia tốc (momentum) của nhân vật
        // khi chuyển từ GetHitState hoặc các state khác quay lại BaseMove,
        // giúp nhân vật không bị khựng (stutter) rồi phải tăng tốc lại từ đầu.

        playerSpeed = character.playerSpeed;
        grounded = character.controller.isGrounded;
        gravityValue = character.gravityValue;

    }

    public override void HandleInput()
    {
        base.HandleInput();

        // Read input first before checking sprint condition
        // Read input and calculate movement direction relative to the camera
        input = MoveInput;
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
        if (CrouchTriggered)
        {
            crouch = true;
        }
        // Check if sprint button is being held (IsPressed) AND there is movement input
        // This ensures sprint activates immediately when Shift is held while moving
        if (SprintPressed && input.sqrMagnitude > 0f)
        {
            sprint = true;
        }
        else
        {
            sprint = false; // Reset sprint flag when conditions aren't met
        }

        // Safety: nếu vì lý do nào đó dashLockUntil bị set quá xa trong tương lai
        // (ví dụ sau khi load scene / editor play-stop-play), thì reset về 0 để tránh khóa dash vĩnh viễn
        if (character.dashLockUntil > character.Runner.SimulationTime + 5f)
        {
            character.dashLockUntil = 0f;
        }

        // Dash chỉ được phép khi không bị khóa (vd: ngay sau khi thoát GetHitState)
        if (character.Runner.SimulationTime >= character.dashLockUntil && DashTriggered)
        {
            dash = true;
        }
        if (ToggleWeaponTriggered)
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

        // Guard: skip nếu CharacterController bị disable (vd: đang teleport)
        if (character.controller == null || !character.controller.enabled) return;
        // Use stable grounded (feet cast + small grace) to avoid flicker on edges/uneven meshes.
        grounded = character.IsGroundedStable();

        // NEW: đang skill -> chỉ gravity Y
        if (skillLock != null && skillLock.isPerformingSkill)
        {
            character.CalculatedVelocity = Vector3.zero;
            return;
        }

        // Apply movement speed multiplier from equipped gems
        float speedMultiplier = 1f;
        var wc = character.GetComponent<WeaponController>();
        if (wc != null && wc.GetCurrentWeapon() != null && WeaponGemManager.Instance != null)
        {
            speedMultiplier = WeaponGemManager.Instance.GetMovementSpeedMultiplier(wc.GetCurrentWeapon().weaponType);
        }

        Vector3 camForward = character.cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = character.cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        // 3. HU?NG T?I: C?p nh?t t?c th� 100% theo ph�m b?m (KH�NG LERP HU?NG)
        Vector3 targetDirection = (camForward * input.y + camRight * input.x).normalized;

        // 4. T?C �?: L�m mu?t t?c d?.
        float targetSpeed = input.sqrMagnitude > 0.01f ? (character.playerSpeed * speedMultiplier) : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 15f * character.Runner.DeltaTime);

        // 5. V?N T?C = HU?NG T?C TH?I * T?C �? MU?T
        velocity = targetDirection * currentSpeed;

        character.CalculatedVelocity.x = velocity.x;
        character.CalculatedVelocity.z = velocity.z;

        // Xoay model
        if (velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
            targetRotation.x = 0;
            targetRotation.z = 0;
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationSpeed * character.Runner.DeltaTime);
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 3. Kh?c ph?c l?i Flicker ? BaseMoveState b?ng Fall Timeout (Coyote Time)
        if (!grounded && character.playerVelocity.y < 0f)
        {
            fallTimer += character.Runner.DeltaTime;
            if (fallTimer >= FallTimeout)
            {
                fallTimer = 0f;
                stateMachine.ChangeState(character.falling);
                return;
            }
        }
        else
        {
            fallTimer = 0f;
        }
        character.SetAnimatorLocomotionSpeed(input.magnitude * 0.5f);

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
        
        // Thêm đoạn này để bắt tín hiệu cất vũ khí từ Input
        if (sheathWeapon && character.isWeaponDrawn)
        {
            // Reset cờ tránh loop
            sheathWeapon = false; 
            drawWeapon = false;
            
            character.isWeaponDrawn = false;
            character.currentLocomotionState = character.standing;
            TutorialTextDisplay.NotifyWeaponSheathedFromGameplay();

            character.animator.SetBool("combatMove", false);

            // Gửi lệnh lên Animator cho thân trên
            character.animator.ResetTrigger("drawWeapon");
            character.animator.SetTrigger("sheathWeapon");

            stateMachine.ChangeState(character.currentLocomotionState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        character.playerVelocity = new Vector3(input.x, 0, input.y);

        if (velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            targetRotation.x = 0;
            targetRotation.z = 0;
            character.transform.rotation = targetRotation;
        }
    }
}






