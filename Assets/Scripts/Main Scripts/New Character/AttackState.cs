using UnityEditor.Timeline.Actions;
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
    const float commitPoint = 0.5f; // 50% of clip - allow earlier transition
    const float earlyTransitionPoint = 0.7f; // 70% of clip - can transition early if buffered

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
        if (jumpAction.triggered)
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
            clipLength = character.animator.GetCurrentAnimatorClipInfo(1)[0].clip.length;
            clipSpeed = character.animator.GetCurrentAnimatorStateInfo(1).speed;
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
        if (weaponLayerIndex >= 0)
        {
            var stateInfo = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
            normalizedTime = stateInfo.normalizedTime;
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

        // Allow early transition if attack is buffered (smoother combo)
        bool canTransitionEarly = nextAttackBuffered && normalizedTime >= earlyTransitionPoint;
        bool canTransitionNormal = normalizedTime >= 1f || timePassed >= clipDuration;

        if (canTransitionEarly || canTransitionNormal)
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
            if (hitHandler != null) hitHandler.CancelCurrentHit();
            stateMachine.ChangeState(character.dashing);
            return;
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
}