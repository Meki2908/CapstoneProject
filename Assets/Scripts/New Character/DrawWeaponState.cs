// 8/27/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public class DrawWeaponState : State
{
    public DrawWeaponState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // Trigger the draw weapon animation
        character.animator.SetTrigger("drawWeapon");
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Check if the drawWeapon animation has finished
        AnimatorStateInfo stateInfo = character.animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("DrawWeapon") && stateInfo.normalizedTime >= 1.0f)
        {
            // Transition to the CombatMove state after the animation finishes
            stateMachine.ChangeState(character.currentLocomotionState);
        }
    }

    public override void Exit()
    {
        base.Exit();
        // Any cleanup logic if needed
    }
}