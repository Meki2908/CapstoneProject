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
        elapsedTime = 0f;

<<<<<<< HEAD
        // Drive animator param so transitions can distinguish RollLanding vs CombatDash.
        if (character.animator)
            character.animator.SetBool("isRollLanding", character.isRollLanding);

=======
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
        // BẢO HIỂM FULL-BODY (Tắt Layer Chân)
        int lowerBodyLayerIndex = character.animator.GetLayerIndex("Lower body");
        if (lowerBodyLayerIndex >= 0)
            character.animator.SetLayerWeight(lowerBodyLayerIndex, 0f);

        // === PHÂN NHÁNH 1: PARKOUR ROLL (DÙNG ROOT MOTION) ===
        if (character.isRollLanding)
        {
            character.animator.applyRootMotion = true; // Ép Root Motion

            Vector2 input = MoveInput;
            GetPlanarCameraBasis(out Vector3 forward, out Vector3 right);
            dashDirection = (forward * input.y + right * input.x).normalized;
            if (dashDirection == Vector3.zero) dashDirection = character.transform.forward;

            character.transform.rotation = Quaternion.LookRotation(dashDirection);

            if (character.animator)
                character.animator.SetTrigger("dash");

            if (character.TryGetComponent(out StuckDetection stuck))
                stuck.enabled = false;
                
            return; // Ngắt luôn, KHÔNG bật I-Frame, KHÔNG tính Cooldown
        }

        // === PHÂN NHÁNH 2: DASH CHIẾN ĐẤU (DÙNG CODE CURVE) ===
        character.animator.applyRootMotion = false; // Tắt Root Motion

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

        if (lastDashTime <= 0 || currentTime - lastDashTime > dashCooldown) dashCount = 0;

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

        Vector2 normalInput = MoveInput;
        GetPlanarCameraBasis(out Vector3 fwd, out Vector3 rgt);
        dashDirection = (fwd * normalInput.y + rgt * normalInput.x).normalized;

        if (dashDirection == Vector3.zero) dashDirection = fwd.normalized;

        character.transform.rotation = Quaternion.LookRotation(dashDirection);

        character.IsDashing = true;
        character.AE_EnableDashInvincibility(); // Bật I-Frame

        if (character.animator)
            character.animator.SetTrigger("dash");

        if (character.TryGetComponent(out StuckDetection stuckDet))
            stuckDet.enabled = false;

        TutorialTextDisplay.NotifyDashStartedFromGameplay();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        elapsedTime += character.Runner.DeltaTime;

        // NẾU ĐANG DASH (KHÔNG PHẢI ROLL) MÀ HẾT ĐẤT -> RƠI
<<<<<<< HEAD
        if (!character.isRollLanding && !character.IsGroundedStable())
=======
        if (!character.isRollLanding && !character.controller.isGrounded && !character.CachedGroundedFeet)
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
        {
            LayerMask mask = character.GetGroundRaycastMask();
            bool hitGround = Physics.Raycast(character.transform.position, Vector3.down, out RaycastHit hit, 10f, mask, QueryTriggerInteraction.Ignore);
            
            if (!hitGround || hit.distance > character.enoughDistanceToFall)
            {
                character.momentumToInherit = new Vector3(character.CalculatedVelocity.x, 0, character.CalculatedVelocity.z);
                stateMachine.ChangeState(character.falling);
                return;
            }
        }

        if (elapsedTime >= character.dashDuration)
        {
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (character.isRollLanding)
        {
            // ROOT MOTION TỰ LO -> CODE TRẢ VỀ 0
            character.CalculatedVelocity.x = 0f;
            character.CalculatedVelocity.z = 0f;
        }
        else
        {
            // DASH THƯỜNG -> DÙNG CURVE NHƯ CŨ
            float dur = Mathf.Max(character.dashDuration, 0.0001f);
            float t = Mathf.Clamp01(elapsedTime / dur);
            float mul = character.dashSpeedMultiplierOverTime != null && character.dashSpeedMultiplierOverTime.length > 0
                ? character.dashSpeedMultiplierOverTime.Evaluate(t) : 1f;

            Vector3 dashMove = character.transform.forward * (character.dashSpeed * mul);
            character.CalculatedVelocity.x = dashMove.x;
            character.CalculatedVelocity.z = dashMove.z;
        }

        if (!character.IsGroundedStable())
            character.playerVelocity.y += character.gravityValue * character.Runner.DeltaTime;
        else
            character.playerVelocity.y = -8f;
    }

    public override void Exit()
    {
        base.Exit();

        character.animator.applyRootMotion = false; // Reset

        int lowerBodyLayerIndex = character.animator.GetLayerIndex("Lower body");
        if (lowerBodyLayerIndex >= 0)
            character.animator.SetLayerWeight(lowerBodyLayerIndex, 1f); 

<<<<<<< HEAD
        if (character.animator)
            character.animator.SetBool("isRollLanding", false);

=======
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
        if (character.isRollLanding) character.isRollLanding = false;
        else
        {
            character.IsDashing = false;
            if (character != null) character.AE_DisableDashInvincibility();
        }

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
    public void ResetDashCooldown() { dashCount = 0; lastDashTime = 0f; dashChainEndTime = 0f; }
    public bool CanDash() { if (character.Runner.SimulationTime < dashChainEndTime) return false; if (character.Runner.SimulationTime - lastDashTime > character.dashCooldown) dashCount = 0; return dashCount < character.maxConsecutiveDashes; }
    public float GetRemainingCooldown() { float r = dashChainEndTime - character.Runner.SimulationTime; return r > 0 ? r : 0f; }
    #endregion
}