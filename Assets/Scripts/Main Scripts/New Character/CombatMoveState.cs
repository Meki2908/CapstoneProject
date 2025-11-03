using UnityEditor.Timeline.Actions;
using UnityEngine;

public class CombatMoveState : BaseMoveState
{
    private float toggleCooldown = 0.5f;
    private float lastToggleTime = 0;
    bool attack;

    // NEW: tránh CombatMoveState can thiệp trong lúc đang dùng skill
    private SkillLock skillLock;

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
        // Don't set speed here - let BaseMoveState handle it for smooth blending
    }

    public override void HandleInput()
    {
        base.HandleInput();

        // NEW: khi đang skill, không đọc/tiêu thụ input để tránh can thiệp
        if (skillLock != null && skillLock.isPerformingSkill)
            return;

        attack = attackAction.triggered;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // NEW: khi đang skill, không xét sheath/attack/đổi state
        if (skillLock != null && skillLock.isPerformingSkill)
            return;

        // Kiểm tra cooldown trước khi xử lý toggle vũ khí
        if (Time.time - lastToggleTime < toggleCooldown)
        {
            return;
        }

        if (sheathWeapon && character.isWeaponDrawn)
        {
            lastToggleTime = Time.time;

            // reset cờ để không loop
            sheathWeapon = false;

            character.isWeaponDrawn = false;
            character.currentLocomotionState = character.standing;

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
        // NEW: để BaseMoveState xử lý lock (đứng yên khi skill)
        base.PhysicsUpdate();
    }

    public override void Exit()
    {
        base.Exit();
        // Don't reset speed here - let BaseMoveState handle it for smooth blending
        character.animator.ResetTrigger("attack"); // Đặt lại trigger để tránh dư thừa
    }
}