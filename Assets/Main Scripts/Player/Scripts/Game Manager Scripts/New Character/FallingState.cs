using UnityEngine;

public class FallingState : State
{
    const float MaxGroundRayDistance = 50f;

    float playerSpeed;
    Vector3 airVelocity;

    private float maxDistanceToGroundRecord;

    public FallingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        playerSpeed = character.playerSpeed;

        character.animator.applyRootMotion = false;
        character.animator.SetBool("isGrounded", false);

        maxDistanceToGroundRecord = 0f;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        input = MoveInput;

        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            input = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        LayerMask mask = character.GetGroundRaycastMask();
        if (Physics.Raycast(character.transform.position, Vector3.down, out RaycastHit hit, MaxGroundRayDistance, mask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance > maxDistanceToGroundRecord)
                maxDistanceToGroundRecord = hit.distance;
        }

        if (character.CachedGroundedFeet || character.controller.isGrounded)
        {
            character.animator.SetBool("isGrounded", true);

            if (character.requireLanding || maxDistanceToGroundRecord > character.minFallDistanceForLanding)
            {
                character.NetworkedIsLanding = true;
                character.animator.SetTrigger("isLanding");
            }

            character.requireLanding = false;

            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!character.controller.isGrounded)
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
    }
}
