using UnityEngine;

public class CombatMoveState : BaseMoveState
{
    private float toggleCooldown = 0.5f;
    private float lastToggleTime = 0;
    bool attack;

    // NEW: tránh CombatMoveState can thiệp trong lúc đang dùng skill
    private SkillLock skillLock;
    private float currentSpeed;

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
        character.isWeaponDrawn = true;
        character.animator.SetBool("combatMove", true);
        Debug.Log($"<color=green>[CombatMoveState]</color> Enter");
        
        // 1. D?N D?P BÓNG MA QUÁN TÍNH: L?y v?n t?c th?c t? hi?n t?i, không d?ng d? cu!
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

        // THÊM ĐOẠN NÀY VÀO ĐỂ BẮT SỰ KIỆN CHẠM
        if (character.currentInput.buttons.IsSet(NetworkInputButtons.Attack) && 
           !character.previousInput.buttons.IsSet(NetworkInputButtons.Attack))
        {
            stateMachine.ChangeState(character.attacking);
            return;
        }
        var swordSkills = character.GetComponentInChildren<SwordSkills>();
        var axeSkill = character.GetComponentInChildren<AxeSkill>();
        var mageSkills = character.GetComponentInChildren<MageSkills>();
        
        if (character.isWeaponDrawn)
        {
            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_E) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_E)) 
            {
                if (swordSkills != null) swordSkills.TryUse(AbilityInput.E);
                Debug.Log($"<color=green>[CombatMoveState]</color> Sword Skills E pressed");
                if (axeSkill != null) axeSkill.TryUse(AbilityInput.E);
                Debug.Log($"<color=green>[CombatMoveState]</color> Axe Skills E pressed");
                if (mageSkills != null) mageSkills.TryUse(AbilityInput.E);
                Debug.Log($"<color=green>[CombatMoveState]</color> Mage Skills E pressed");

                Debug.Log($"<color=green>[CombatMoveState]</color> Skill E pressed");
            }
                
            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_R) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_R)) 
            {
                if (swordSkills != null) swordSkills.TryUse(AbilityInput.R);
                Debug.Log($"<color=green>[CombatMoveState]</color> Sword Skills R pressed");
                if (axeSkill != null) axeSkill.TryUse(AbilityInput.R);
                Debug.Log($"<color=green>[CombatMoveState]</color> Axe Skill R pressed");
                if (mageSkills != null) mageSkills.TryUse(AbilityInput.R);
                Debug.Log($"<color=green>[CombatMoveState]</color> Mage Skills R pressed");
                Debug.Log($"<color=green>[CombatMoveState]</color> Skill R pressed");
            }
                
            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_T) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_T)) 
            {
                if (swordSkills != null) swordSkills.TryUse(AbilityInput.T);
                if (axeSkill != null) axeSkill.TryUse(AbilityInput.T);
                if (mageSkills != null) mageSkills.TryUse(AbilityInput.T);
                Debug.Log($"<color=green>[CombatMoveState]</color> Skill T pressed");
            }
                
            if (character.currentInput.buttons.IsSet(NetworkInputButtons.Skill_Q) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Skill_Q)) 
            {
                if (swordSkills != null) swordSkills.TryUse(AbilityInput.Q_Ultimate);
                if (axeSkill != null) axeSkill.TryUse(AbilityInput.Q_Ultimate);
                if (mageSkills != null) mageSkills.TryUse(AbilityInput.Q_Ultimate);
                Debug.Log($"<color=green>[CombatMoveState]</color> Skill Q pressed");
            }
        }

        // NEW: khi đang skill, không xét sheath/attack/đổi state
        if (skillLock != null && skillLock.isPerformingSkill)
            return;

        // Kiểm tra cooldown trước khi xử lý toggle vũ khí
        if (character.Runner.SimulationTime - lastToggleTime < toggleCooldown)
        {
            return;
        }

        if (sheathWeapon && character.isWeaponDrawn)
        {
            lastToggleTime = character.Runner.SimulationTime;

            // reset cờ để không loop
            sheathWeapon = false;
            character.isWeaponDrawn = false;
            character.currentLocomotionState = character.standing;
            TutorialTextDisplay.NotifyWeaponSheathedFromGameplay();

            // Gọi đúng trigger và tránh double ChangeState
            character.animator.ResetTrigger("drawWeapon");
            character.animator.SetTrigger("sheathWeapon");

            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        // Nếu nhấn nút tấn công, chuyển sang AttackState
        if (attack && stateMachine.currentState != character.attacking)
        {
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







