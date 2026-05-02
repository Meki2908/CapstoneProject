using UnityEngine;

public class AttackState : State
{
    float timePassed;
    float clipLength;
    float clipSpeed;
    bool attack;
    bool jump;
    private bool dash;
    Vector2 movementInput;
    bool allowNewAttack;

    // release-to-cancel support
    bool pressedSinceLastCheck;
    bool nextAttackBuffered;
    const float commitPoint = 0.5f; // Buffer from mid-clip
    const float minChainPoint = 0.70f;
    const float maxChainPoint = 0.80f;

    // Integration
    private EquipmentSystem equipment;
    private WeaponSO currentWeapon;
    private int hitIndex;
    private WeaponHitRunner hitHandler;
    private WeaponController weaponController;

    public AttackState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
        // WeaponHitRunner removed - effects handled by separate scripts
        equipment = character.GetComponent<EquipmentSystem>();
        weaponController = character.GetComponent<WeaponController>();
    }

    public override void Enter()
    {
        base.Enter();

        // GUARD: Không cho đánh nếu chưa rút vũ khí
        if (!character.isWeaponDrawn)
        {
            Debug.LogWarning("[AttackState] Blocked — weapon not drawn! Returning to standing.");
            character.animator.ResetTrigger("attack");
            stateMachine.ChangeState(character.standing);
            return;
        }

        movementInput = Vector2.zero;
        attack = false;
        jump = false;
        dash = false;
        allowNewAttack = true;
        pressedSinceLastCheck = false;
        nextAttackBuffered = false;

        // Track attack input time for resuming after hit
        character.lastAttackInputTime = character.Runner.SimulationTime;

        character.animator.applyRootMotion = false;
        timePassed = 0f;

        // Get current weapon data
        currentWeapon = equipment != null ? equipment.GetCurrentWeapon() : null;
        hitIndex = 0;

        // Setup hit handler
        if (hitHandler == null)
        {
            hitHandler = character.GetComponent<WeaponHitRunner>();
            if (hitHandler == null)
            {
                hitHandler = character.gameObject.AddComponent<WeaponHitRunner>();
            }
        }

        if (hitHandler != null && currentWeapon != null)
        {
            // Bind with proper parameters: weapon, equipment, vfxSpawn, handRef, characterRoot
            Transform vfxSpawn = character.transform; // Use character as default spawn point
            Transform handRef = null; // No hand reference for now
            hitHandler.Bind(currentWeapon, equipment, vfxSpawn, handRef, character.transform);
        }

        // Apply attack speed multiplier from equipment
        float attackSpeedMultiplier = 1f;
        if (EquipmentManager.Instance != null)
        {
            float attackSpeedBonus = EquipmentManager.Instance.GetTotalAttackSpeedBonus();
            attackSpeedMultiplier = 1f + attackSpeedBonus; // e.g., 0.15 bonus = 1.15 multiplier
        }

        // Ensure correct weapon layer is active (not hardcoded to layer 1)
        EnsureCorrectWeaponLayer();
        
        // Apply attack speed directly to animator for smoother transitions
        ApplyAttackSpeedToAnimator();

        // Start first hit
        character.animator.SetTrigger("attack");
        
        // --- ĐOẠN NÀY LÀ CỨU TINH ĐÂY ---
        character.playerVelocity = Vector3.zero;
        character.CalculatedVelocity = Vector3.zero; // THÊM DÒNG NÀY ĐỂ GIẾT CHẾT BÓNG MA QUÁN TÍNH!
        // --------------------------------
        
        character.animator.SetFloat("speed", 0f);
        TutorialTextDisplay.NotifyNormalAttackStartedFromGameplay();

        if (hitHandler != null && currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0)
        {
            hitHandler.StartHit(hitIndex);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();

        movementInput = MoveInput;
        if (allowNewAttack && AttackTriggered)
        {
            attack = true;
            pressedSinceLastCheck = true;
        }
        if (character.TryConsumeJumpBuffered(character.canStartJump))
        {
            jump = true;
        }
        if (DashTriggered)
        {
            dash = true;
        }

        if (TutorialInputGate.IsActive)
        {
            var m = TutorialInputGate.EffectiveMask;
            if ((m & TutorialInputMask.Move) == 0) movementInput = Vector2.zero;
            if ((m & TutorialInputMask.Attack) == 0) attack = false;
            if ((m & TutorialInputMask.Jump) == 0) jump = false;
            if ((m & TutorialInputMask.Dash) == 0) dash = false;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        timePassed += character.Runner.DeltaTime;
        
        // Get the correct weapon layer index based on current weapon
        int weaponLayerIndex = GetWeaponLayerIndex();
        if (weaponLayerIndex >= 0 && character.animator.GetCurrentAnimatorClipInfoCount(weaponLayerIndex) > 0)
        {
            clipLength = character.animator.GetCurrentAnimatorClipInfo(weaponLayerIndex)[0].clip.length;
            clipSpeed = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex).speed;
        }
        else
        {
            // Fallback to layer 1 if weapon layer not found
            if (character.animator.GetCurrentAnimatorClipInfoCount(1) > 0)
            {
                clipLength = character.animator.GetCurrentAnimatorClipInfo(1)[0].clip.length;
                clipSpeed = character.animator.GetCurrentAnimatorStateInfo(1).speed;
            }
            else
            {
                // Default fallback values if no clips are playing
                clipLength = 1.0f;
                clipSpeed = 1.0f;
            }
        }

        // Apply attack speed multiplier from equipment
        float attackSpeedMultiplier = 1f;
        if (EquipmentManager.Instance != null)
        {
            float attackSpeedBonus = EquipmentManager.Instance.GetTotalAttackSpeedBonus();
            attackSpeedMultiplier = 1f + attackSpeedBonus; // e.g., 0.15 bonus = 1.15 multiplier
        }

        // Apply attack speed to animator for smoother animation
        ApplyAttackSpeedToAnimator();

        // Calculate clip duration with attack speed multiplier
        float baseClipDuration = clipLength / Mathf.Max(clipSpeed, 0.0001f);
        float clipDuration = baseClipDuration / attackSpeedMultiplier; // Faster attack = shorter duration
        
        // Use normalized time from animator state instead of timePassed for more accurate timing
        float normalizedTime = 0f;
        bool isInTransition = false;
        if (weaponLayerIndex >= 0)
        {
            var stateInfo = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
            normalizedTime = stateInfo.normalizedTime;
            isInTransition = character.animator.IsInTransition(weaponLayerIndex);
        }
        else
        {
            normalizedTime = timePassed / clipDuration;
        }

        // Allow buffering next attack from commit point
        if (!nextAttackBuffered && normalizedTime >= commitPoint)
        {
            nextAttackBuffered = pressedSinceLastCheck;
            pressedSinceLastCheck = false;
        }

        // Adaptive chain window (70-80%): shorter clips need later chaining, longer clips can chain earlier.
        float chainPoint = GetAdaptiveChainPoint(clipDuration);
        // Thay vì dùng chung 1 mốc thời gian, ta tách làm 2 mốc riêng biệt:
        bool canChain = !isInTransition && normalizedTime >= chainPoint;

        // 1. Trường hợp có nhồi Input: Cho phép combo sớm (ở mốc chainPoint)
        if (canChain && (nextAttackBuffered || attack))
        {
            timePassed = 0f;

            if (currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0)
                hitIndex = (hitIndex + 1) % currentWeapon.hitTimings.Length;
            else
                hitIndex = 0;

            character.animator.ResetTrigger("attack");
            character.animator.SetTrigger("attack");
            
            attack = false;
            allowNewAttack = true;
            nextAttackBuffered = false;
            pressedSinceLastCheck = false;

            if (hitHandler != null && currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0)
            {
                hitHandler.StartHit(hitIndex);
            }
        }
        // 2. Giai đoạn thu chiêu (Recovery Phase): Khi đã qua chainPoint và KHÔNG đánh tiếp
        else if (canChain && !nextAttackBuffered && !attack)
        {
            // TRƯỜNG HỢP A: Bấm phím di chuyển -> "Move Cancel" (Ngắt thu chiêu để chạy ngay lập tức)
            if (movementInput.sqrMagnitude > 0.01f)
            {
                timePassed = 0f;
                if (hitHandler != null) hitHandler.CancelCurrentHit();
                
                // Lệnh tối thượng: Ép Animator Weapon Layer cắt ngang hoạt ảnh, chui tọt về "None" (0.1s làm mượt)
                if (weaponLayerIndex >= 0) 
                {
                    character.animator.CrossFade("None", 0.1f, weaponLayerIndex);
                }
                
                // Trả quyền ngay cho CombatMoveState để lấy tốc độ chạy!
                stateMachine.ChangeState(character.combatMove); 
                return;
            }
            
            // TRƯỜNG HỢP B: Buông tay khỏi bàn phím -> Chờ hết hoạt ảnh tự nhiên
            // Dùng shortNameHash để bắt tên "None" chuẩn xác 100% không bị Unity chơi khăm
            bool isActuallyNone = false;
            if (weaponLayerIndex >= 0) 
            {
                var currState = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
                isActuallyNone = (currState.shortNameHash == Animator.StringToHash("None"));
            }
            
            if (isActuallyNone || (!isInTransition && normalizedTime >= 0.95f))
            {
                timePassed = 0f;
                if (hitHandler != null) hitHandler.CancelCurrentHit();
                stateMachine.ChangeState(character.combatMove);
                return;
            }
        }

        // -- BẮT SỰ KIỆN NGẮT CHIÊU (ANIMATION CANCELING) --
        if (jump)
        {
            if (hitHandler != null) hitHandler.CancelCurrentHit();
            // Bắn trigger để kích hoạt AnyState -> None trong Animator
            character.animator.SetTrigger("jump"); 
            stateMachine.ChangeState(character.jumping);
            return;
        }

        if (dash)
        {
            // Bỏ qua cái safeDashStart/safeDashEnd phức tạp cũ đi. 
            // Cứ bấm Dash là ngắt ngay lập tức!
            if (hitHandler != null) hitHandler.CancelCurrentHit();
            // Bắn trigger để kích hoạt AnyState -> None trong Animator
            character.animator.SetTrigger("dash"); 
            stateMachine.ChangeState(character.dashing);
            return;
        }

        // Xoay người theo Camera khi chém
        if (movementInput.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = character.cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = character.cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 targetDirection = (camForward * movementInput.y + camRight * movementInput.x).normalized;
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationSpeed * character.Runner.DeltaTime);
            }
        }

        character.animator.SetFloat("speed", movementInput.magnitude);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!character.controller.isGrounded)
        {
            character.playerVelocity.y += character.gravityValue * Time.fixedDeltaTime;
        }
        else
        {
            character.playerVelocity.y = 0f;
        }

        character.controller.Move(character.playerVelocity * Time.fixedDeltaTime);
    }

    public override void Exit()
    {
        base.Exit();
        if (hitHandler != null) hitHandler.CancelCurrentHit();
        character.animator.SetFloat("speed", 0f);
        character.animator.ResetTrigger("attack");
    }

    /// <summary>
    /// Ensure the correct weapon layer is active based on current weapon type
    /// </summary>
    private void EnsureCorrectWeaponLayer()
    {
        if (currentWeapon == null || character.animator == null) return;

        int swordLayer = 1;
        int axeLayer = 2;
        int mageLayer = 3;

        // Set all weapon layers to 0 first
        SetLayerWeightSafe(swordLayer, 0f);
        SetLayerWeightSafe(axeLayer, 0f);
        SetLayerWeightSafe(mageLayer, 0f);

        // Activate the correct layer based on weapon type
        switch (currentWeapon.weaponType)
        {
            case WeaponType.Sword:
                SetLayerWeightSafe(swordLayer, 1f);
                break;
            case WeaponType.Axe:
                SetLayerWeightSafe(axeLayer, 1f);
                break;
            case WeaponType.Mage:
                SetLayerWeightSafe(mageLayer, 1f);
                break;
        }
    }

    /// <summary>
    /// Get the correct weapon layer index based on current weapon type
    /// </summary>
    private int GetWeaponLayerIndex()
    {
        if (currentWeapon == null) return 1; // Default to sword layer

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Sword:
                return 1;
            case WeaponType.Axe:
                return 2;
            case WeaponType.Mage:
                return 3;
            default:
                return 1; // Default to sword layer
        }
    }

    private void SetLayerWeightSafe(int layer, float weight)
    {
        if (character.animator != null && layer >= 0 && layer < character.animator.layerCount)
        {
            character.animator.SetLayerWeight(layer, weight);
        }
    }

    /// <summary>
    /// Apply attack speed multiplier directly to animator for smoother animation
    /// Formula: animationSpeed = baseSpeed * (1 + attackSpeedBonus)
    /// Similar to damage calculation: damage = baseDamage + (baseDamage × %)
    /// </summary>
    private void ApplyAttackSpeedToAnimator()
    {
        if (character.animator == null) return;

        float attackSpeedMultiplier = 1f;
        if (EquipmentManager.Instance != null)
        {
            float attackSpeedBonus = EquipmentManager.Instance.GetTotalAttackSpeedBonus();
            // Formula: multiplier = 1 + bonus (e.g., 0.15 bonus = 1.15 multiplier)
            attackSpeedMultiplier = 1f + attackSpeedBonus;
        }

        // Apply speed to the weapon layer
        int weaponLayerIndex = GetWeaponLayerIndex();
        if (weaponLayerIndex >= 0)
        {
            // Get current state info
            var stateInfo = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
            
            // Set animator speed directly using speed multiplier
            // This directly affects animation playback speed
            character.animator.speed = attackSpeedMultiplier;
            
            // Also try to set via parameter if available (for per-state control)
            if (character.animator.parameters != null)
            {
                foreach (var param in character.animator.parameters)
                {
                    if (param.name == "attackSpeed" && param.type == AnimatorControllerParameterType.Float)
                    {
                        character.animator.SetFloat("attackSpeed", attackSpeedMultiplier);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Dynamic chain threshold in [0.70..0.80] based on real clip duration.
    /// Short clips (snappier) chain later; long clips chain earlier to avoid feeling stuck.
    /// </summary>
    private float GetAdaptiveChainPoint(float durationSeconds)
    {
        // duration <= 0.45s -> 0.80 ; duration >= 1.00s -> 0.70
        float t = Mathf.InverseLerp(0.45f, 1.00f, Mathf.Max(0.01f, durationSeconds));
        return Mathf.Lerp(maxChainPoint, minChainPoint, t);
    }
}


