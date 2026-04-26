using UnityEngine;
using UnityEngine.InputSystem;

public class State
{
    public Character character;
    public StateMachine stateMachine;
    protected Vector3 velocity;
    protected Vector2 input;
    protected bool JumpTriggered => character.currentInput.buttons.IsSet(NetworkInputButtons.Jump) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Jump);
    protected bool DashTriggered => character.currentInput.buttons.IsSet(NetworkInputButtons.Dash) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Dash);
    protected bool ToggleWeaponTriggered => character.currentInput.buttons.IsSet(NetworkInputButtons.ToggleWeapon) && !character.previousInput.buttons.IsSet(NetworkInputButtons.ToggleWeapon);
    protected bool AttackTriggered => character.currentInput.buttons.IsSet(NetworkInputButtons.Attack) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Attack);
    protected bool CrouchTriggered => character.currentInput.buttons.IsSet(NetworkInputButtons.Crouch) && !character.previousInput.buttons.IsSet(NetworkInputButtons.Crouch);
    
    protected bool SprintPressed => character.currentInput.buttons.IsSet(NetworkInputButtons.Sprint);
    protected bool AttackPressed => character.currentInput.buttons.IsSet(NetworkInputButtons.Attack);
    protected Vector2 MoveInput => character.currentInput.movementInput;


    public State(Character _character, StateMachine _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;

    }

    public virtual void Enter()
    {
    }

    public virtual void HandleInput()
    {
    }

    public virtual void LogicUpdate()
    {
    }

    public virtual void PhysicsUpdate()
    {
    }

    public virtual void Exit()
    {
    }

    /// <summary>
    /// Returns a stable planar camera basis for movement.
    /// Keeps last valid basis when camera pitch is near vertical.
    /// </summary>
    protected void GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight)
    {
        camForward = character.cameraTransform != null ? character.cameraTransform.forward : character.cachedPlanarForward;
        camForward.y = 0f;

        if (camForward.sqrMagnitude >= 0.0001f)
        {
            camForward.Normalize();
            character.cachedPlanarForward = camForward;
        }
        else
        {
            camForward = character.cachedPlanarForward;
            if (camForward.sqrMagnitude < 0.0001f) camForward = Vector3.forward;
            camForward.Normalize();
        }

        camRight = character.cameraTransform != null ? character.cameraTransform.right : character.cachedPlanarRight;
        camRight.y = 0f;

        if (camRight.sqrMagnitude >= 0.0001f)
        {
            camRight.Normalize();
        }
        else
        {
            camRight = Vector3.Cross(Vector3.up, camForward);
            if (camRight.sqrMagnitude < 0.0001f) camRight = character.cachedPlanarRight;
            if (camRight.sqrMagnitude < 0.0001f) camRight = Vector3.right;
            camRight.Normalize();
        }

        character.cachedPlanarRight = camRight;
    }
}




