using UnityEngine;

public class SheathWeaponState : State
{
    public SheathWeaponState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // Trigger the sheath weapon animation
        character.animator.SetTriggerNetworked("sheathWeapon");
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        AnimatorStateInfo stateInfo = character.animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("SheathWeapon") && stateInfo.normalizedTime >= 1.0f)
        {
            // Transition to the CombatMove state after the animation finishes
            character.animator.SetTriggerNetworked("move");
            stateMachine.ChangeState(character.currentLocomotionState);
        }
    }
    public override void Exit()
    {
        base.Exit();
    }
}
