using UnityEngine;
using UnityEngine.InputSystem;

public class State
{
    public Character character;
    public StateMachine stateMachine;
    protected Vector3 velocity;
    protected Vector2 input;
    
    // Fusion-safe time base for rollback/resimulation.
    // Use SimulationTime instead of Unity Time.time to avoid desync under lag/rollback.
    protected float startTime;
    protected float TimeInState => (character != null && character.Runner != null)
        ? (character.Runner.SimulationTime - startTime)
        : 0f;
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
        // Measure time in state using Fusion simulation clock (rollback-safe)
        if (character != null && character.Runner != null)
            startTime = character.Runner.SimulationTime;
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
    /// When Fusion is running, uses <see cref="Character.currentInput"/>.<c>cameraYaw</c> from the owning client so host resimulation matches client view.
    /// Otherwise uses <see cref="Character.cameraTransform"/> (offline / before first input).
    /// </summary>
    protected void GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight)
    {
        if (character != null && character.Runner != null && character.Runner.IsRunning)
        {
            Quaternion yawOnly = Quaternion.Euler(0f, character.currentInput.cameraYaw, 0f);
            camForward = yawOnly * Vector3.forward;
            camRight = yawOnly * Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            if (camForward.sqrMagnitude >= 0.0001f)
            {
                camForward.Normalize();
                character.cachedPlanarForward = camForward;
            }
            else
            {
                camForward = character.cachedPlanarForward.sqrMagnitude >= 0.0001f ? character.cachedPlanarForward : Vector3.forward;
                camForward.y = 0f;
                camForward.Normalize();
                character.cachedPlanarForward = camForward;
            }

            if (camRight.sqrMagnitude >= 0.0001f)
                camRight.Normalize();
            else
            {
                camRight = Vector3.Cross(Vector3.up, camForward);
                if (camRight.sqrMagnitude < 0.0001f) camRight = Vector3.right;
                camRight.Normalize();
            }

            character.cachedPlanarRight = camRight;
            return;
        }

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




