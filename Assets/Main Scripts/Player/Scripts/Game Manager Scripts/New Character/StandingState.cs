using UnityEngine;

public class StandingState : BaseMoveState
{
    Vector3 cVelocity;

    public StandingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();
        // Flags one-shot; không giữ trạng thái "sheathWeapon=true" giữa các state.
        drawWeapon = false;
        sheathWeapon = false;
        // Do not force isWeaponDrawn here; Draw/Sheath states own that truth.
        //Debug.Log("Standing State");
    }

    public override void HandleInput()
    {
        base.HandleInput();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
    }

    public override void Exit()
    {
        base.Exit();
    }
}

