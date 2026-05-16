using UnityEngine;

public class CombatMoveState : BaseMoveState
{

    // NEW: tránh CombatMoveState can thiệp trong lúc đang dùng skill
    private SkillLock skillLock;

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
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (character.currentInput.buttons.IsSet(NetworkInputButtons.Attack) &&
            !character.previousInput.buttons.IsSet(NetworkInputButtons.Attack))
        {
            // Prevent rapid re-enter AttackState (client resim / spam click) before previous swing finishes.
            if (character.Runner != null && character.Runner.IsRunning)
            {
                const float minReattackInterval = 0.28f;
                if (character.Runner.SimulationTime - character.lastAttackStateExitSimTime < minReattackInterval)
                    return;
            }

            // Failsafe (idempotent): ensure visuals/scripts are in drawn state without spamming animator triggers.
            var wc = character.GetComponent<WeaponController>();
            if (wc != null)
                wc.EnsureDrawn(requestAnimation: false);

            character.SetTriggerSafe("attack");
            stateMachine.ChangeState(character.attacking);
            return;
        }

        bool canIssueSkillCommands = character.Runner == null
            || !character.Runner.IsRunning
            || character.HasInputAuthority;

        if (character.isWeaponDrawn && canIssueSkillCommands)
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
    }

    public override void Exit()
    {
        base.Exit();
        // Don't reset speed here - let BaseMoveState handle it for smooth blending
        character.ResetTriggerSafe("attack"); // Chỉ reset Animator thuần khi không có NetworkMecanimAnimator
    }
}
