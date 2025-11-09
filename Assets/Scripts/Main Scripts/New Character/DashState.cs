using UnityEngine;

public class DashState : State
{
    private float dashTimer;
    private Vector3 dashDirection;

    // Dash cooldown and chain tracking
    private int currentDashCount = 0; // Current number of consecutive dashes
    private bool isDashMovementActive = false; // Whether dash movement is currently active (set by Animation Event)

    // Static variables to persist across state instances
    private static int staticDashCount = 0;
    private static float staticLastDashTime = 0f;
    private static float staticDashChainEndTime = 0f;

    public DashState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // Get dash settings from Character
        float dashCooldown = character.dashCooldown;
        float dashChainCooldown = character.dashChainCooldown;
        int maxConsecutiveDashes = character.maxConsecutiveDashes;

        // Check if dash is on cooldown
        float currentTime = Time.time;

        // Safety check: If staticDashChainEndTime is way too large (likely from previous play session),
        // or if staticLastDashTime is way too large, reset everything
        // This handles the case when Unity Editor stops and plays again
        if (staticDashChainEndTime > currentTime + 100f || staticLastDashTime > currentTime + 100f)
        {
            ResetDashCooldown();
        }

        // Check if we're in dash chain cooldown (after max consecutive dashes)
        if (currentTime < staticDashChainEndTime)
        {
            // Still in cooldown, return to previous state
            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        // Check if we need to reset dash count (enough time has passed since last dash)
        // Also reset if staticLastDashTime is 0 (initialized) or negative (invalid)
        if (staticLastDashTime <= 0 || currentTime - staticLastDashTime > dashCooldown)
        {
            staticDashCount = 0; // Reset dash count if cooldown period has passed
        }

        // Check if we've reached max consecutive dashes
        if (staticDashCount >= maxConsecutiveDashes)
        {
            // Start chain cooldown
            staticDashChainEndTime = currentTime + dashChainCooldown;
            staticDashCount = 0; // Reset count
            // Return to previous state
            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        // Increment dash count and update last dash time
        staticDashCount++;
        staticLastDashTime = currentTime;
        currentDashCount = staticDashCount;

        // Set the dash timer
        dashTimer = character.dashDuration;

        // Calculate the dash direction based on input
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 forward = character.cameraTransform.forward;
        Vector3 right = character.cameraTransform.right;

        // Normalize forward and right vectors to avoid diagonal speed boost
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        dashDirection = (forward * input.y + right * input.x).normalized;

        // If no input, dash forward
        if (dashDirection == Vector3.zero)
        {
            dashDirection = character.cameraTransform.forward;
            dashDirection.y = 0;
            dashDirection.Normalize();
        }

        // Rotate the character to face the dash direction
        character.transform.rotation = Quaternion.LookRotation(dashDirection);

        // Reset dash movement flag (will be set by Animation Event)
        isDashMovementActive = false;

        // Trigger dash animation if available
        if (character.animator)
        {
            character.animator.SetTrigger("dash");
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Decrease the dash timer
        dashTimer -= Time.deltaTime;

        // Transition back to StandingState when dash ends
        if (dashTimer <= 0)
        {
            isDashMovementActive = false; // Stop dash movement
            stateMachine.ChangeState(character.currentLocomotionState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Only apply dash movement if Animation Event has activated it
        if (isDashMovementActive)
        {
            character.controller.Move(dashDirection * character.dashSpeed * Time.deltaTime);
        }
    }

    public override void Exit()
    {
        base.Exit();

        // Stop dash movement
        isDashMovementActive = false;

        // Reset dash-related variables if needed
        dashDirection = Vector3.zero;
    }

    #region Animation Events

    /// <summary>
    /// Animation Event: Start dash movement
    /// Call this from dash animation at the frame where dash movement should begin
    /// </summary>
    public void AE_StartDashMovement()
    {
        isDashMovementActive = true;
        Debug.Log($"[DashState] AE_StartDashMovement - Dash movement started (Dash {currentDashCount}/{character.maxConsecutiveDashes})");
    }

    /// <summary>
    /// Animation Event: Stop dash movement
    /// Call this from dash animation at the frame where dash movement should end
    /// </summary>
    public void AE_StopDashMovement()
    {
        isDashMovementActive = false;
        Debug.Log("[DashState] AE_StopDashMovement - Dash movement stopped");
    }

    #endregion

    #region Public Methods for Cooldown Checking

    /// <summary>
    /// Reset all dash cooldown and count variables
    /// Call this when game starts or restarts
    /// </summary>
    public static void ResetDashCooldown()
    {
        staticDashCount = 0;
        staticLastDashTime = 0f;
        staticDashChainEndTime = 0f;
        Debug.Log("[DashState] ResetDashCooldown - All dash cooldown variables reset");
    }

    /// <summary>
    /// Check if dash can be performed (not on cooldown)
    /// </summary>
    public static bool CanDash(float dashCooldown, float dashChainCooldown, int maxConsecutiveDashes)
    {
        float currentTime = Time.time;

        // Check if in chain cooldown
        if (currentTime < staticDashChainEndTime)
        {
            return false;
        }

        // Check if we need to reset dash count
        if (currentTime - staticLastDashTime > dashCooldown)
        {
            staticDashCount = 0;
        }

        // Check if we've reached max consecutive dashes
        if (staticDashCount >= maxConsecutiveDashes)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get remaining cooldown time
    /// </summary>
    public static float GetRemainingCooldown(float dashCooldown, float dashChainCooldown)
    {
        float currentTime = Time.time;
        float chainCooldownRemaining = staticDashChainEndTime - currentTime;

        if (chainCooldownRemaining > 0)
        {
            return chainCooldownRemaining;
        }

        return 0f;
    }

    #endregion
}