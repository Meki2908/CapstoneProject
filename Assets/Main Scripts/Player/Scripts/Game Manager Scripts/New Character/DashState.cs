using UnityEngine;

public class DashState : State
{
    private float elapsedTime;
    private Vector3 dashDirection;

    private int currentDashCount = 0;

    private int dashCount = 0;
    private float lastDashTime = 0f;
    private float dashChainEndTime = 0f;

    public DashState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        character.playerVelocity.y = 0f;

        float dashCooldown = character.dashCooldown;
        float dashChainCooldown = character.dashChainCooldown;
        int maxConsecutiveDashes = character.maxConsecutiveDashes;

        float currentTime = character.Runner.SimulationTime;

        if (dashChainEndTime > currentTime + 100f || lastDashTime > currentTime + 100f)
            ResetDashCooldown();

        if (currentTime < dashChainEndTime)
        {
            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        if (lastDashTime <= 0 || currentTime - lastDashTime > dashCooldown)
            dashCount = 0;

        if (dashCount >= maxConsecutiveDashes)
        {
            dashChainEndTime = currentTime + dashChainCooldown;
            dashCount = 0;
            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        dashCount++;
        lastDashTime = currentTime;
        currentDashCount = dashCount;

        elapsedTime = 0f;

        Vector2 input = MoveInput;
        GetPlanarCameraBasis(out Vector3 forward, out Vector3 right);
        dashDirection = (forward * input.y + right * input.x).normalized;

        if (dashDirection == Vector3.zero)
        {
            dashDirection = forward;
            dashDirection.Normalize();
        }

        character.transform.rotation = Quaternion.LookRotation(dashDirection);

        character.IsDashing = true;
        character.AE_EnableDashInvincibility();

        if (character.animator)
            character.animator.SetTrigger("dash");

        if (character.TryGetComponent(out StuckDetection stuck))
            stuck.enabled = false;

        TutorialTextDisplay.NotifyDashStartedFromGameplay();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        elapsedTime += character.Runner.DeltaTime;

        if (elapsedTime >= character.dashDuration)
        {
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float dur = Mathf.Max(character.dashDuration, 0.0001f);
        float t = Mathf.Clamp01(elapsedTime / dur);
        float mul = character.dashSpeedMultiplierOverTime != null && character.dashSpeedMultiplierOverTime.length > 0
            ? character.dashSpeedMultiplierOverTime.Evaluate(t)
            : 1f;

        Vector3 dashMove = character.transform.forward * (character.dashSpeed * mul);
        character.CalculatedVelocity.x = dashMove.x;
        character.CalculatedVelocity.z = dashMove.z;

        if (!character.controller.isGrounded && !character.CachedGroundedFeet)
            character.playerVelocity.y += character.gravityValue * character.Runner.DeltaTime;
        else
            character.playerVelocity.y = -2f;
    }

    public override void Exit()
    {
        base.Exit();

        character.IsDashing = false;
        if (character != null)
            character.AE_DisableDashInvincibility();

        dashDirection = Vector3.zero;

        if (character != null)
        {
            Vector2 m = MoveInput;
            character.SetAnimatorLocomotionSpeed(m.magnitude);
        }

        if (character.TryGetComponent(out StuckDetection stuck))
            stuck.enabled = true;
    }

    #region Public Methods for Cooldown Checking

    public void ResetDashCooldown()
    {
        dashCount = 0;
        lastDashTime = 0f;
        dashChainEndTime = 0f;
    }

    public bool CanDash()
    {
        float currentTime = character.Runner.SimulationTime;

        if (currentTime < dashChainEndTime)
            return false;

        if (currentTime - lastDashTime > character.dashCooldown)
            dashCount = 0;

        if (dashCount >= character.maxConsecutiveDashes)
            return false;

        return true;
    }

    public float GetRemainingCooldown()
    {
        float currentTime = character.Runner.SimulationTime;
        float chainCooldownRemaining = dashChainEndTime - currentTime;

        if (chainCooldownRemaining > 0)
            return chainCooldownRemaining;

        return 0f;
    }

    #endregion
}
