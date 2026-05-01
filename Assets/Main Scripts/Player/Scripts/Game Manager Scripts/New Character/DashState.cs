using UnityEngine;

public class DashState : State
{
    private float elapsedTime;
    private Vector3 dashDirection;

    // Dash cooldown and chain tracking
    private int currentDashCount = 0; // Current number of consecutive dashes

    // Static variables to persist across state instances
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

        // Get dash settings from Character
        float dashCooldown = character.dashCooldown;
        float dashChainCooldown = character.dashChainCooldown;
        int maxConsecutiveDashes = character.maxConsecutiveDashes;

        // Check if dash is on cooldown
        float currentTime = character.Runner.SimulationTime;

        if (dashChainEndTime > currentTime + 100f || lastDashTime > currentTime + 100f)
        {
            ResetDashCooldown();
        }

        if (currentTime < dashChainEndTime)
        {
            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        if (lastDashTime <= 0 || currentTime - lastDashTime > dashCooldown)
        {
            dashCount = 0;
        }

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

        // Reset the timer
        elapsedTime = 0f;

        // Calculate the dash direction based on input
        Vector2 input = MoveInput;
        GetPlanarCameraBasis(out Vector3 forward, out Vector3 right);
        dashDirection = (forward * input.y + right * input.x).normalized;

        if (dashDirection == Vector3.zero)
        {
            dashDirection = forward;
            dashDirection.Normalize();
        }

        // Rotate the character to face the dash direction
        character.transform.rotation = Quaternion.LookRotation(dashDirection);

        // B?t I-frame và d?i Layer tàng hình
        character.IsDashing = true;
        character.AE_EnableDashInvincibility();

        // Phát animation l?n vòng
        if (character.animator)
        {
            character.animator.SetTrigger("dash");
        }

        if(character.TryGetComponent(out StuckDetection stuck)){
            stuck.enabled = false;
        }

        TutorialTextDisplay.NotifyDashStartedFromGameplay();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Tích luy th?i gian
        elapsedTime += character.Runner.DeltaTime;

        // N?u th?i gian lu?t dã vu?t qua dashDuration
        if (elapsedTime >= character.dashDuration)
        {
            // Chuy?n v? tr?ng thái d?ng yên
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Lao di v?i t?c d? dashSpeed (Không c?n di?u ki?n if(isDashMoving) t? AE n?a)
        character.CalculatedVelocity = character.transform.forward * character.dashSpeed;
    }

    public override void Exit()
    {
        base.Exit();

        // T?t I-frame, tr? l?i Layer bình thu?ng
        character.IsDashing = false;
        if (character != null)
        {
            character.AE_DisableDashInvincibility();
        }

        dashDirection = Vector3.zero;

        // Snap blend "speed" theo input hi?n t?i
        if (character != null)
        {
            Vector2 m = MoveInput;
            character.SetAnimatorLocomotionSpeed(m.magnitude);
        }

        if(character.TryGetComponent(out StuckDetection stuck)){
            stuck.enabled = true;
        }
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
        {
            return false;
        }

        if (currentTime - lastDashTime > character.dashCooldown)
        {
            dashCount = 0;
        }

        if (dashCount >= character.maxConsecutiveDashes)
        {
            return false;
        }

        return true;
    }

    public float GetRemainingCooldown()
    {
        float currentTime = character.Runner.SimulationTime;
        float chainCooldownRemaining = dashChainEndTime - currentTime;

        if (chainCooldownRemaining > 0)
        {
            return chainCooldownRemaining;
        }

        return 0f;
    }

    #endregion
}
