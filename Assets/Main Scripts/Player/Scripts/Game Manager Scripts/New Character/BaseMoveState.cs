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
    
    // ToggleWeapon buffering (helps Fusion tick vs Update mismatch + avoids cooldown eating presses)
    protected const float TOGGLE_BUFFER_DURATION = 0.3f; // Lưu phím trong 0.3 giây
    private bool _toggleBufferHasExplicitAction = false;
    private bool _toggleBufferDraw = false;

    Vector3 cVelocity;

    private SkillLock skillLock;
    private const float FallTimeout = 0.15f;

    // Centralized toggle cooldown (prevents race between derived states)
    protected float toggleCooldown = 0.5f;

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
        // Reset toggle buffer when entering locomotion.
        character.NetToggleBuffer = 0f;
        _toggleBufferHasExplicitAction = false;
        _toggleBufferDraw = false;

        // Queued Tab during GetHit is now consumed/handled by dedicated Draw/Sheath states.
        
        // 2. CH? L?Y "�? L?N" T?C �? HI?N T?I �? KH�NG B? VANG SAU KHI DASH
        character.NetLocomotionSpeed = new Vector3(character.CalculatedVelocity.x, 0, character.CalculatedVelocity.z).magnitude;
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
            // Buffer the press; actual draw/sheath decision happens in LogicUpdate when cooldown is ready.
            character.NetToggleBuffer = TOGGLE_BUFFER_DURATION;
            _toggleBufferHasExplicitAction = false;
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
            character.NetToggleBuffer = 0f;
            _toggleBufferHasExplicitAction = false;
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

        // Speed: gem + equipment already baked into character.playerSpeed via UpdateSpeedWithGems() — do not multiply WeaponGemManager again (would square gem bonus).

        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

        // 3. HU?NG T?I: C?p nh?t t?c th� 100% theo ph�m b?m (KH�NG LERP HU?NG)
        Vector3 targetDirection = (camForward * input.y + camRight * input.x).normalized;

        // 4. T?C �?: L�m mu?t t?c d?.
        float targetSpeed = input.sqrMagnitude > 0.01f ? character.playerSpeed : 0f;
        character.NetLocomotionSpeed = Mathf.Lerp(character.NetLocomotionSpeed, targetSpeed, 15f * character.Runner.DeltaTime);

        // 5. V?N T?C = HU?NG T?C TH?I * T?C �? MU?T
        velocity = targetDirection * character.NetLocomotionSpeed;

        character.CalculatedVelocity.x = velocity.x;
        character.CalculatedVelocity.z = velocity.z;

        // Xoay model — RotateTowards (deg/step) is more stable under Fusion resimulation than Slerp with a 0–1 blend factor.
        if (velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
            targetRotation.x = 0;
            targetRotation.z = 0;
            float maxDeg = character.LocomotionBodyTurnDegreesPerSecond * character.Runner.DeltaTime;
            character.transform.rotation = Quaternion.RotateTowards(character.transform.rotation, targetRotation, maxDeg);
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 3. Kh?c ph?c l?i Flicker ? BaseMoveState b?ng Fall Timeout (Coyote Time)
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
        
        // Toggle weapon transitions are handled via explicit Draw/Sheath states
        // (uninterruptible, and applies gameplay truth via EnsureDrawn/EnsureSheathed).

        // === ĐỒNG BỘ C# VỚI ANIMATOR: CHỜ GET HIT CHẠY XONG MỚI XẢ HÀNG ĐỢI ===
        if (character.animator != null && character.queuedWeaponAction != Character.QueuedWeaponAction.None)
        {
            int upperLayer = character.animator.GetLayerIndex("Upper body");
            if (upperLayer < 0)
                upperLayer = character.animator.GetLayerIndex("UpperBody_Hit");
            if (upperLayer >= 0)
            {
                AnimatorStateInfo stateInfo = character.animator.GetCurrentAnimatorStateInfo(upperLayer);

                if (stateInfo.IsName("Default State") || stateInfo.IsName("None"))
                {
                    if (character.TryConsumeQueuedWeaponAction(out var action))
                    {
                        var uinf = character.animator.GetCurrentAnimatorStateInfo(upperLayer);
                        string upperName = uinf.IsName("None") ? "None"
                            : uinf.IsName("Default State") ? "Default State"
                            : $"other(sh={uinf.shortNameHash})";
                        character.LogCritFsm("Queue", $"Dequeued {action} upperIx={upperLayer} ({upperName}) → NetToggleBuffer");
                        // Buffer this explicit requested action so it survives cooldown.
                        character.NetToggleBuffer = TOGGLE_BUFFER_DURATION;
                        _toggleBufferHasExplicitAction = true;
                        _toggleBufferDraw = (action == Character.QueuedWeaponAction.Draw);
                    }
                }
            }
        }
        // ====================================================================

        // === TAB BUFFERING: giữ lệnh đến khi cooldown sẵn sàng (không clear khi chưa hết cooldown) ===
        character.NetToggleBuffer = Mathf.Max(0f, character.NetToggleBuffer - character.Runner.DeltaTime);
        if (character.NetToggleBuffer > 0f)
        {
            if (character.Runner.SimulationTime - character.NetLastToggleSimTime >= toggleCooldown)
            {
                character.NetLastToggleSimTime = character.Runner.SimulationTime;
                character.NetToggleBuffer = 0f;

                bool doDraw;
                if (_toggleBufferHasExplicitAction)
                {
                    doDraw = _toggleBufferDraw;
                    _toggleBufferHasExplicitAction = false;
                }
                else
                {
                    // Single source of truth at the moment we execute.
                    doDraw = !character.NetIsWeaponDrawn;
                }

                character.LogCritFsm("TabBuffer",
                    $"EXEC toggle doDraw={doDraw} NetDrawn={(bool)character.NetIsWeaponDrawn} bufWas>0 sim={character.Runner.SimulationTime:F3}");

                if (doDraw)
                {
                    stateMachine.ChangeState(character.drawingWeapon);
                    return;
                }
                else
                {
                    stateMachine.ChangeState(character.sheathingWeapon);
                    return;
                }
            }
        }
        // ====================================================================
    }

    public override void Exit()
    {
        base.Exit();
        // Do not overwrite PlayerVelocity from raw input (camera-relative movement lives in CalculatedVelocity / NetLocomotionSpeed).
        if (velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            targetRotation.x = 0;
            targetRotation.z = 0;
            character.transform.rotation = targetRotation;
        }
    }
}






