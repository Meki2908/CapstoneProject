using UnityEngine;

public class CombatMoveState : BaseMoveState
{
    bool attack;

    // NEW: tránh CombatMoveState can thiệp trong lúc đang dùng skill
    private SkillLock skillLock;
    private float currentSpeed;

    private SwordSkills swordSkills;
    private AxeSkill axeSkill;
    private MageSkills mageSkills;

    public CombatMoveState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
        skillLock = character.GetComponent<SkillLock>(); // NEW
    }

    public override void Enter()
    {
        base.Enter();
        attack = false;
        // Flags one-shot; tránh spam draw/sheath khi đổi state.
        drawWeapon = false;
        sheathWeapon = false;
        // Do not force isWeaponDrawn here; Draw/Sheath states own that truth.
        character.animator.SetBool("combatMove", true);

        if (swordSkills == null) swordSkills = character.GetComponentInChildren<SwordSkills>(true);
        if (axeSkill == null) axeSkill = character.GetComponentInChildren<AxeSkill>(true);
        if (mageSkills == null) mageSkills = character.GetComponentInChildren<MageSkills>(true);

        velocity = character.CalculatedVelocity;
    }
    public override void HandleInput()
    {
        base.HandleInput();

        // NEW: khi đang skill, không đọc/tiêu thụ input để tránh can thiệp
        if (skillLock != null && skillLock.isPerformingSkill)
            return;

        attack = AttackTriggered;
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Attack) == 0)
            attack = false;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (character.currentInput.buttons.IsSet(NetworkInputButtons.Attack) &&
            !character.previousInput.buttons.IsSet(NetworkInputButtons.Attack))
        {
            // Failsafe (idempotent): ensure visuals/scripts are in drawn state without spamming animator triggers.
            var wc = character.GetComponent<WeaponController>();
            if (wc != null)
                wc.EnsureDrawn(requestAnimation: false);

            character.animator.SetTrigger("attack");
            stateMachine.ChangeState(character.attacking);
            return;
        }

        if (character.isWeaponDrawn)
        {
            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_E) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_E))
            {
                if (swordSkills != null && swordSkills.isActiveAndEnabled) swordSkills.TryUse(AbilityInput.E);
                if (axeSkill != null && axeSkill.isActiveAndEnabled) axeSkill.TryUse(AbilityInput.E);
                if (mageSkills != null && mageSkills.isActiveAndEnabled) mageSkills.TryUse(AbilityInput.E);
            }

            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_R) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_R))
            {
                if (swordSkills != null && swordSkills.isActiveAndEnabled) swordSkills.TryUse(AbilityInput.R);
                if (axeSkill != null && axeSkill.isActiveAndEnabled) axeSkill.TryUse(AbilityInput.R);
                if (mageSkills != null && mageSkills.isActiveAndEnabled) mageSkills.TryUse(AbilityInput.R);
            }

            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_T) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_T))
            {
                if (swordSkills != null && swordSkills.isActiveAndEnabled) swordSkills.TryUse(AbilityInput.T);
                if (axeSkill != null && axeSkill.isActiveAndEnabled) axeSkill.TryUse(AbilityInput.T);
                if (mageSkills != null && mageSkills.isActiveAndEnabled) mageSkills.TryUse(AbilityInput.T);
            }

            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_Q) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_Q))
            {
                if (swordSkills != null && swordSkills.isActiveAndEnabled) swordSkills.TryUse(AbilityInput.Q_Ultimate);
                if (axeSkill != null && axeSkill.isActiveAndEnabled) axeSkill.TryUse(AbilityInput.Q_Ultimate);
                if (mageSkills != null && mageSkills.isActiveAndEnabled) mageSkills.TryUse(AbilityInput.Q_Ultimate);
            }
        }

        if (skillLock != null && skillLock.isPerformingSkill)
            return;

        if (attack && stateMachine.currentState != character.attacking)
        {
            // Failsafe (idempotent): ensure visuals/scripts are in drawn state without spamming animator triggers.
            var wc = character.GetComponent<WeaponController>();
            if (wc != null)
                wc.EnsureDrawn(requestAnimation: false);

            character.animator.SetTrigger("attack");
            stateMachine.ChangeState(character.attacking);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector3 camForward = character.cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = character.cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        // 3. HU?NG T?I: C?p nh?t t?c th� 100% theo ph�m b?m (KH�NG LERP HU?NG)
        Vector3 targetDirection = (camForward * input.y + camRight * input.x).normalized;

        // 4. T?C �?: L�m mu?t t?c d?.
        float targetSpeed = input.sqrMagnitude > 0.01f ? character.playerSpeed : 0f;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 15f * character.Runner.DeltaTime);

        // 5. V?N T?C = HU?NG T?C TH?I * T?C �? MU?T
        velocity = targetDirection * currentSpeed;

        character.CalculatedVelocity.x = velocity.x;
        character.CalculatedVelocity.z = velocity.z;

        // Xoay model
        if (velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(velocity.x, 0, velocity.z));
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationSpeed * character.Runner.DeltaTime);
        }
    }

    public override void Exit()
    {
        base.Exit();
        // Don't reset speed here - let BaseMoveState handle it for smooth blending
        character.animator.ResetTrigger("attack"); // Đặt lại trigger để tránh dư thừa
    }
}







