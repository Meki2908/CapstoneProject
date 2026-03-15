using UnityEngine;
using UnityEngine.InputSystem;

public class State
{
    public Character character;
    public StateMachine stateMachine;

    protected Vector3 gravityVelocity;
    protected Vector3 velocity;
    protected Vector2 input;

    public InputAction moveAction;
    public InputAction lookAction;
    public InputAction jumpAction;
    public InputAction crouchAction;
    public InputAction sprintAction;
    public InputAction dashAction;
    public InputAction toggleWeaponAction;
    public InputAction attackAction;

    public State(Character _character, StateMachine _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;

        moveAction = character.playerInput.actions["Move"];
        lookAction = character.playerInput.actions["Look"];
        jumpAction = character.playerInput.actions["Jump"];
        crouchAction = character.playerInput.actions["Crouch"];
        sprintAction = character.playerInput.actions["Sprint"];
        dashAction = character.playerInput.actions["Dash"];
        toggleWeaponAction = character.playerInput.actions["ToggleWeapon"];
        attackAction = character.playerInput.actions["Attack"];
    }

    public virtual void Enter()
    {
        Debug.Log("enter state: " + this.ToString());
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