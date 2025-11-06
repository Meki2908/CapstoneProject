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
    const float commitPoint = 0.6f; // 60% of clip

    // Integration
    private EquipmentSystem equipment;
    private WeaponSO currentWeapon;
    private int hitIndex;
    private WeaponHitRunner hitHandler;

    public AttackState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
        // WeaponHitRunner removed - effects handled by separate scripts
        equipment = character.GetComponent<EquipmentSystem>();
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
        clipLength = character.animator.GetCurrentAnimatorClipInfo(1)[0].clip.length;
        clipSpeed = character.animator.GetCurrentAnimatorStateInfo(1).speed;

        float clipDuration = clipLength / Mathf.Max(clipSpeed, 0.0001f);
        float normalized = timePassed / clipDuration;

        if (!nextAttackBuffered && normalized >= commitPoint)
        {
            nextAttackBuffered = pressedSinceLastCheck;
            pressedSinceLastCheck = false;
        }

        if (timePassed >= clipDuration)
        {
            timePassed = 0f;

            if (nextAttackBuffered || attack)
            {
                if (currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0)
                    hitIndex = (hitIndex + 1) % currentWeapon.hitTimings.Length;
                else
                    hitIndex = 0;

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
}