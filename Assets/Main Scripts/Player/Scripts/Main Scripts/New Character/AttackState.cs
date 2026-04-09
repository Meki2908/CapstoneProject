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
        character.lastAttackInputTime = Time.time;

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
        character.playerVelocity = Vector3.zero;
        character.animator.SetFloat("speed", 0f);

        if (hitHandler != null && currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0)
        {
            hitHandler.StartHit(hitIndex);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();

        movementInput = moveAction.ReadValue<Vector2>();
        if (allowNewAttack && attackAction.triggered)
        {
            attack = true;
            pressedSinceLastCheck = true;
        }
        if (character.TryConsumeJumpBuffered(character.canStartJump))
        {
            jump = true;
        }
        if (dashAction.triggered)
        {
            dash = true;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        timePassed += Time.deltaTime;
        
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
        bool canTransitionNormal = !isInTransition && normalizedTime >= chainPoint;

        if (canTransitionNormal)
        {
            timePassed = 0f;

            if (nextAttackBuffered || attack)
            {
                if (currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0)
                    hitIndex = (hitIndex + 1) % currentWeapon.hitTimings.Length;
                else
                    hitIndex = 0;

                // Reset trigger first to ensure clean transition
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
            else
            {
                if (hitHandler != null) hitHandler.CancelCurrentHit();
                stateMachine.ChangeState(character.combatMove);
                character.animator.SetTrigger("move");
                return;
            }
        }

        if (jump)
        {
            if (hitHandler != null) hitHandler.CancelCurrentHit();
            stateMachine.ChangeState(character.jumping);
            return;
        }

        if (dash)
        {
            // Chỉ cho phép dash trong một khoảng thời gian an toàn của đòn đánh
            // để tránh trường hợp animator/state bị lệch khi spam input ở cuối animation
            float safeDashStart = 0.1f; // bỏ qua vài frame đầu
            float safeDashEnd = 0.6f;   // chỉ cho dash trong ~60% đầu của attack

            if (normalizedTime >= safeDashStart && normalizedTime <= safeDashEnd)
            {
                if (hitHandler != null) hitHandler.CancelCurrentHit();
                stateMachine.ChangeState(character.dashing);
                return;
            }
            else
            {
                // Ngoài khoảng an toàn thì bỏ qua dash để không làm kẹt AttackState
                dash = false;
            }
        }

        character.animator.SetFloat("speed", movementInput.magnitude);
    }

    public override void PhysicsUpdate()
    {
        var cc = character.controller;
        if (cc == null || !cc.enabled)
        {
            base.PhysicsUpdate();
            return;
        }

        base.PhysicsUpdate();

        if (!cc.isGrounded)
        {
            character.playerVelocity.y += character.gravityValue * Time.fixedDeltaTime;
        }
        else
        {
            character.playerVelocity.y = 0f;
        }

        cc.Move(character.playerVelocity * Time.fixedDeltaTime);
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