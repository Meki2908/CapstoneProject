using UnityEngine;

public sealed class SheathWeaponState : State
{
    private const int UpperBodyLayerIndex = 4; // Player.controller: Upper body is layer 4 (not Base Layer 0)
    private const string SheathTag = "Sheath";
    private const string WeaponActionTag = "WeaponAction";
    private const bool TOGGLE_DEBUG = true;
    private bool hasSwapped;

    private WeaponController weaponController;

    public SheathWeaponState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        if (TOGGLE_DEBUG)
        {
            bool combatMove = character.animator != null && character.animator.GetBool("combatMove");
            Debug.Log($"[ToggleWeapon][SheathState.Enter] frame={Time.frameCount} time={Time.time:F3} sim={character.Runner.SimulationTime:F3} isWeaponDrawn={character.isWeaponDrawn} combatMove={combatMove}");
        }

        hasSwapped = false;

        if (weaponController == null)
            weaponController = character.GetComponentInChildren<WeaponController>(true);

        if (weaponController != null)
        {
            // Start sheath animation, but do NOT swap visuals/scripts yet.
            // Swap will happen later when Upper body normalizedTime reaches WeaponSO.sheathUnequipNormalizedTime.
            weaponController.TriggerSheathAnimationOnly();
        }
        else if (TOGGLE_DEBUG)
        {
            Debug.LogWarning($"[ToggleWeapon][SheathState] No WeaponController on enter. frame={Time.frameCount} sim={character.Runner.SimulationTime:F3}");
        }

        // Consider weapon still drawn until we reach the swap point.
        character.isWeaponDrawn = true;
        if (character.animator != null)
            character.animator.SetBool("combatMove", false);

        // Lock locomotion truth: khi đang sheath/hit, lối thoát khỏi GetHit phải quay về Standing.
        character.currentLocomotionState = character.standing;
    }

    public override void HandleInput()
    {
        // Intentionally do nothing: uninterruptible by input.
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        Vector2 input = MoveInput;

        if (character == null || character.animator == null)
        {
            character.currentLocomotionState = character.standing;
            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        bool isSprinting = SprintPressed && input.sqrMagnitude > 0f;
        float animSpeedMultiplier = isSprinting ? 1f : 0.5f;
        character.SetAnimatorLocomotionSpeed(input.magnitude * animSpeedMultiplier);

        TrySwapAtTiming();

        int layerIndex = UpperBodyLayerIndex;
        float timeInState = TimeInState;

        if (character.animator.layerCount <= layerIndex)
        {
            if (timeInState > 1.5f)
            {
                Debug.LogWarning("[FSM] Failsafe: Cất vũ khí quá lâu (missing layer), ép buộc vào Standing!");
                if (!hasSwapped && weaponController != null)
                    weaponController.EnsureSheathed(requestAnimation: false);
                character.isWeaponDrawn = false;
                if (character.animator != null)
                    character.animator.SetBool("combatMove", false);
                character.currentLocomotionState = character.standing;
                stateMachine.ChangeState(character.standing);
                return;
            }
            return;
        }

        if (character.animator.IsInTransition(layerIndex))
            return;

        AnimatorStateInfo stateInfo = character.animator.GetCurrentAnimatorStateInfo(layerIndex);

        if (stateInfo.IsTag(WeaponActionTag) && timeInState > 0.1f && stateInfo.normalizedTime >= 0.95f)
        {
            if (!hasSwapped && weaponController != null)
                weaponController.EnsureSheathed(requestAnimation: false);
            character.isWeaponDrawn = false;
            if (character.animator != null)
                character.animator.SetBool("combatMove", false);
            character.currentLocomotionState = character.standing;
            stateMachine.ChangeState(character.standing);
            return;
        }

        if (timeInState > 1.5f)
        {
            Debug.LogWarning("[FSM] Failsafe: Cất vũ khí quá lâu, ép buộc vào Standing!");
            if (!hasSwapped && weaponController != null)
                weaponController.EnsureSheathed(requestAnimation: false);
            character.isWeaponDrawn = false;
            if (character.animator != null)
                character.animator.SetBool("combatMove", false);
            character.currentLocomotionState = character.standing;
            stateMachine.ChangeState(character.standing);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector2 input = MoveInput;
        GetPlanarCameraBasis(out Vector3 camForward, out Vector3 camRight);

        Vector3 targetDirection = (camForward * input.y + camRight * input.x).normalized;

        // 3. TỐC ĐỘ VẬT LÝ DỰA THEO SHIFT
        bool isSprinting = SprintPressed && input.sqrMagnitude > 0f;
        float baseTargetSpeed = isSprinting ? character.sprintSpeed : character.playerSpeed;

        // 4. CỘNG DỒN CHỈ SỐ CỦA NGỌC (GEM) CHO CHUẨN XÁC NHƯ BASE MOVESTATE
        float gemMultiplier = 1f;
        var wc = character.GetComponent<WeaponController>();
        if (wc != null && wc.GetCurrentWeapon() != null && WeaponGemManager.Instance != null)
        {
            gemMultiplier = WeaponGemManager.Instance.GetMovementSpeedMultiplier(wc.GetCurrentWeapon().weaponType);
        }

        // Tốc độ cuối cùng
        float targetSpeed = input.sqrMagnitude > 0.01f ? (baseTargetSpeed * gemMultiplier) : 0f;

        // Bơm vận tốc vào CharacterController
        character.CalculatedVelocity.x = targetDirection.x * targetSpeed;
        character.CalculatedVelocity.z = targetDirection.z * targetSpeed;

        // Xoay người
        if (targetDirection.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);
            targetRot.x = 0;
            targetRot.z = 0; // Khóa trục để không bị ngửa người lên trời
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRot, character.rotationSpeed * character.Runner.DeltaTime);
        }
    }

    private void TrySwapAtTiming()
    {
        if (hasSwapped) return;
        if (weaponController == null) return;
        var weapon = weaponController.GetCurrentWeapon();
        if (weapon == null) return;
        if (character == null || character.animator == null) return;
        if (character.animator.layerCount <= UpperBodyLayerIndex) return;
        if (character.animator.IsInTransition(UpperBodyLayerIndex)) return;

        var st = character.animator.GetCurrentAnimatorStateInfo(UpperBodyLayerIndex);
        if (!(st.IsTag(SheathTag) || st.IsName("Sheath Weapon"))) return;

        float t = Mathf.Clamp01(weapon.sheathUnequipNormalizedTime);
        if (st.normalizedTime < t) return;

        weaponController.EnsureSheathed(requestAnimation: false);
        character.isWeaponDrawn = false;
        if (character.animator != null)
            character.animator.SetBool("combatMove", false);
        character.currentLocomotionState = character.standing;
        hasSwapped = true;
    }
}

