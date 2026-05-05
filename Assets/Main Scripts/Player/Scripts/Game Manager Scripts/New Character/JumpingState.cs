using UnityEngine;

public class JumpingState : State
{
    bool grounded;
    bool landTriggered;

    float gravityValue;
    float jumpHeight;
    float playerSpeed;

    Vector3 airVelocity;

    public JumpingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        // L?U L?I T?A �? Y L�C B?T �?U NH?Y
        character.jumpStartY = character.transform.position.y;

        grounded = false;
        landTriggered = false;

        // TH?M D?NG N?Y (C?c k? quan tr?ng d? b?t d?u t?nh Cooldown):
        character.lastJumpTime = character.Runner.SimulationTime; 

        gravityValue = character.gravityValue;
        jumpHeight = character.jumpHeight;
        playerSpeed = character.playerSpeed;

        character.animator.SetFloat("speed", 0);
        character.animator.SetTrigger("jump");
        character.NotifyJumpImpulseStarted();
        Jump();
        TutorialTextDisplay.NotifyJumpStartedFromGameplay();

        character.requireLanding = true;
    }
    public override void HandleInput()
    {
        base.HandleInput();

        input = MoveInput;
        // Trong state n?y kh?ng x?t jump; Space kh?ng ??i Y ? impulse ?? ???c g?n trong Enter().
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            input = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // N?u v?n t?c tr?c Y b?t d?u ?m (nghia l? dang roi xu?ng)
        if (character.playerVelocity.y <= 0f)
        {
            // Nhu?ng s?n kh?u l?i cho FallingState
            stateMachine.ChangeState(character.falling);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (!grounded)
        {

            velocity = character.playerVelocity;
            airVelocity = new Vector3(input.x, 0, input.y);

            GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

            velocity = velocity.x * camRight + velocity.z * camForward;
            velocity.y = 0f;
            airVelocity = airVelocity.x * camRight + airVelocity.z * camForward;
            if (airVelocity.sqrMagnitude > 1f) airVelocity.Normalize();
            airVelocity.y = 0f;
            Vector3 movement = (airVelocity * character.airControl + velocity * (1 - character.airControl)) * playerSpeed;
            character.CalculatedVelocity.x = movement.x;
            character.CalculatedVelocity.z = movement.z;

            if (movement.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(movement.x, 0, movement.z));
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
            }
        }
        grounded = character.controller.isGrounded;
    }

    void Jump()
    {
        character.playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravityValue);
    }

    public override void Exit()
    {
        base.Exit();
        
        // Snap blend "speed" theo input hi?n t?i gi?ng nhu Dash d? ti?p d?t mu?t m?
        if (character != null)
        {
            Vector2 m = MoveInput;
            float targetSpeed = m.magnitude;
            if (character.currentLocomotionState == character.combatMove) targetSpeed *= 0.5f;
            character.SetAnimatorLocomotionSpeed(targetSpeed);
        }
    }

}




