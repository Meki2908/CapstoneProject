using UnityEngine;

public class CombatMoveState : BaseMoveState
{
    private float toggleCooldown = 0.5f;
    private float lastToggleTime = 0;
    bool attack;

    // NEW: trÃ¡nh CombatMoveState can thiá»‡p trong lÃºc Ä‘ang dÃ¹ng skill
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

        // NEW: khi Ä‘ang skill, khÃ´ng Ä‘á»c/tiÃªu thá»¥ input Ä‘á»ƒ trÃ¡nh can thiá»‡p
        if (skillLock != null && skillLock.isPerformingSkill)
            return;

        attack = attackAction.triggered;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // NEW: khi Ä‘ang skill, khÃ´ng xÃ©t sheath/attack/Ä‘á»•i state
        if (skillLock != null && skillLock.isPerformingSkill)
            return;

        // Kiá»ƒm tra cooldown trÆ°á»›c khi xá»­ lÃ½ toggle vÅ© khÃ­
        if (Time.time - lastToggleTime < toggleCooldown)
        {
            return;
        }

        if (sheathWeapon && character.isWeaponDrawn)
        {
            lastToggleTime = Time.time;

            // reset cá» Ä‘á»ƒ khÃ´ng loop
            sheathWeapon = false;
            character.isWeaponDrawn = false;
            character.currentLocomotionState = character.standing;

            // Gá»i Ä‘Ãºng trigger vÃ  trÃ¡nh double ChangeState
            character.animator.ResetTrigger("drawWeapon");
            character.animator.SetTriggerNetworked("sheathWeapon");

            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        // Náº¿u nháº¥n nÃºt táº¥n cÃ´ng, chuyá»ƒn sang AttackState
        if (attack && stateMachine.currentState != character.attacking)
        {
            character.animator.SetTriggerNetworked("attack");
            stateMachine.ChangeState(character.attacking);
        }
    }

    public override void PhysicsUpdate()
    {
        // NEW: Ä‘á»ƒ BaseMoveState xá»­ lÃ½ lock (Ä‘á»©ng yÃªn khi skill)
        base.PhysicsUpdate();
    }

    public override void Exit()
    {
        base.Exit();
        // Don't reset speed here - let BaseMoveState handle it for smooth blending
        character.animator.ResetTrigger("attack"); // Äáº·t láº¡i trigger Ä‘á»ƒ trÃ¡nh dÆ° thá»«a
    }
}

