using Fusion;
using UnityEngine;

public class FallingState : State
{
    const float MaxGroundRayDistance = 50f;

    float playerSpeed;
    Vector3 airVelocity;

    private Vector3 inheritedInertia;
    private float maxDistanceToGroundRecord;

    private bool isRecoveringFromLanding;

    private float timeInAir;

    private bool hasLeftGround;

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
        character.animator.SetBool("isHighFalling", false);

        maxDistanceToGroundRecord = 0f;
        timeInAir = 0f;
        hasLeftGround = character.requireLanding
            || character.PlayerVelocity.y <= 0.05f
            || !character.IsGroundedStable();

        inheritedInertia = character.momentumToInherit;
        character.momentumToInherit = Vector3.zero;

        isRecoveringFromLanding = false;

        if (character.Runner != null && character.Object != null && character.Object.IsValid
            && (character.HasStateAuthority || character.HasInputAuthority))
            character.NetLandingRecoveryTimer = TickTimer.None;
    }

    public override void HandleInput()
    {
        base.HandleInput();

        if (isRecoveringFromLanding && character.NetLandingRecoveryTimer.IsRunning)
        {
            // Dash cancel: cho phép thoát recovery sớm (action game), không chặn DashTriggered.
            if (DashTriggered && character.dashing != null && character.dashing.CanDash()
                && character.Runner != null && character.Runner.SimulationTime > character.dashLockUntil)
            {
                ClearLandingRecoveryForInterrupt();
                stateMachine.ChangeState(character.dashing);
                return;
            }

            input = Vector2.zero;
            return;
        }

        input = MoveInput;
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            input = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (isRecoveringFromLanding)
        {
            if (character.Runner != null && character.NetLandingRecoveryTimer.Expired(character.Runner))
            {
                character.LogCritFsm("Landing",
                    $"EXIT recovery (TickTimer Expired) simTick={character.Runner.Tick} | {character.GetCritFsmAnimatorSnapshot()} || {character.GetCritFsmLandingProbe()}");
                ClearLandingRecoveryForInterrupt();
                State next = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
                stateMachine.ChangeState(next);
            }

            return;
        }

        timeInAir += character.Runner.DeltaTime;

        bool groundedNow = character.CachedGroundedFeet || (character.controller != null && character.controller.isGrounded);
        if (!groundedNow)
            hasLeftGround = true;

        if (timeInAir > 0.05f)
        {
            LayerMask mask = character.GetGroundRaycastMask();
            if (Physics.Raycast(character.transform.position, Vector3.down, out RaycastHit hit, MaxGroundRayDistance, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance > maxDistanceToGroundRecord)
                {
                    maxDistanceToGroundRecord = hit.distance;
                    if (maxDistanceToGroundRecord > character.minFallDistanceForLanding)
                        character.animator.SetBool("isHighFalling", true);
                }
            }
        }

        if (character.IsGroundedStable())
        {
            character.animator.SetBool("isGrounded", true);
            character.animator.SetBool("isHighFalling", false);

            if (hasLeftGround)
            {
                bool isValidToLand = character.requireLanding || timeInAir > 0.15f;

                if (isValidToLand)
                {
                    if (maxDistanceToGroundRecord >= character.farFallDistanceForRoll)
                    {
                        character.requireLanding = false;
                        character.SetNetworkedKneeLanding(false);
                        character.IncrementLandEventCount();
                        character.SyncRollLandingState(true);
                        stateMachine.ChangeState(character.dashing);
                        return;
                    }

                    bool fromFallDistance = maxDistanceToGroundRecord > character.minFallDistanceForLanding;
                    bool fromJumpRequire = character.requireLanding && (
                        character.transform.position.y <= character.jumpStartY - character.JumpKneeLandMinDropFromApex
                        || timeInAir >= character.JumpKneeLandMinAirTime);

                    if (fromFallDistance || fromJumpRequire)
                    {
                        if (!character.HasStateAuthority && !character.HasInputAuthority)
                        {
                            character.requireLanding = false;
                            State nextLoc = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
                            stateMachine.ChangeState(nextLoc);
                            return;
                        }

                        character.SetNetworkedKneeLanding(true);
                        character.IncrementLandEventCount();
                        character.LogCritFsm("Landing",
                            $"START knee land fallDist={maxDistanceToGroundRecord:F2} min={character.minFallDistanceForLanding:F2} reqLR={character.requireLanding} tAir={timeInAir:F2} | {character.GetCritFsmAnimatorSnapshot()} || {character.GetCritFsmLandingProbe()}");

                        float recoverySeconds = ComputeKneeRecoverySeconds(fromFallDistance, fromJumpRequire, character);
                        if (character.Runner != null && character.Runner.IsRunning
                            && (character.HasStateAuthority || character.HasInputAuthority))
                            character.NetLandingRecoveryTimer = TickTimer.CreateFromSeconds(character.Runner, recoverySeconds);

                        if (!character.NetLandingRecoveryTimer.IsRunning)
                        {
                            character.LogCritFsm("Landing", "WARN TickTimer failed to start; skipping recovery lock");
                            character.SetNetworkedKneeLanding(false);
                            character.animator.applyRootMotion = false;
                            character.requireLanding = false;
                            State nextLoc = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
                            stateMachine.ChangeState(nextLoc);
                            return;
                        }

                        character.SetTriggerSafe("isLanding");
                        character.animator.applyRootMotion = true;
                        isRecoveringFromLanding = true;
                        return;
                    }
                }
            }

            character.requireLanding = false;
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    static float ComputeKneeRecoverySeconds(bool fromFallDistance, bool fromJumpRequire, Character c)
    {
        if (fromFallDistance && fromJumpRequire)
            return Mathf.Max(c.NetLandingRecoveryFallKneeSeconds, c.NetLandingRecoveryJumpKneeSeconds);
        if (fromFallDistance)
            return c.NetLandingRecoveryFallKneeSeconds;
        if (fromJumpRequire)
            return c.NetLandingRecoveryJumpKneeSeconds;
        return Mathf.Max(c.NetLandingRecoveryJumpKneeSeconds, 0.15f);
    }

    void ClearLandingRecoveryForInterrupt()
    {
        if (character.Runner != null && character.Object != null && character.Object.IsValid
            && (character.HasStateAuthority || character.HasInputAuthority))
            character.NetLandingRecoveryTimer = TickTimer.None;

        isRecoveringFromLanding = false;
        character.animator.applyRootMotion = false;
        character.SetNetworkedKneeLanding(false);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (isRecoveringFromLanding)
        {
            character.CalculatedVelocity.x = 0f;
            character.CalculatedVelocity.z = 0f;
            var pvL = character.PlayerVelocity;
            pvL.y = -2f;
            character.PlayerVelocity = pvL;
            return;
        }

        if (!character.IsGroundedStable())
        {
            velocity = character.PlayerVelocity;
            airVelocity = new Vector3(input.x, 0, input.y);

            GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

            velocity = velocity.x * camRight + velocity.z * camForward;
            velocity.y = 0f;

            airVelocity = airVelocity.x * camRight + airVelocity.z * camForward;
            if (airVelocity.sqrMagnitude > 1f) airVelocity.Normalize();
            airVelocity.y = 0f;

            inheritedInertia = Vector3.Lerp(inheritedInertia, Vector3.zero, character.fallInertiaDecayRate * character.Runner.DeltaTime);

            Vector3 playerControlMovement = (airVelocity * character.airControl + velocity * (1 - character.airControl)) * playerSpeed;
            Vector3 finalMovement = playerControlMovement + inheritedInertia;

            character.CalculatedVelocity.x = finalMovement.x;
            character.CalculatedVelocity.z = finalMovement.z;

            bool applyExtraGravity = true;

            if (character.requireLanding && character.transform.position.y >= character.jumpStartY)
                applyExtraGravity = false;

            if (applyExtraGravity)
            {
                var pvF = character.PlayerVelocity;
                pvF.y -= character.extraFallAcceleration * character.Runner.DeltaTime;
                character.PlayerVelocity = pvF;
            }

            if (finalMovement.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(finalMovement.x, 0, finalMovement.z));
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
            }
        }
    }
}
