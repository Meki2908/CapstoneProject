using UnityEngine;

public sealed class DrawWeaponState : State
{
    private const float TimeoutSeconds = 1.6f;
    private const int UpperBodyLayerIndex = 4; // Player.controller: Upper body is layer 4
    private const string DrawTag = "Draw";
    private const bool TOGGLE_DEBUG = true;
    private float enteredAt;
    private bool hasEnteredDrawClip;
    private bool hasSwapped;

    private WeaponController weaponController;

    public DrawWeaponState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
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
            Debug.Log($"[ToggleWeapon][DrawState.Enter] frame={Time.frameCount} time={Time.time:F3} sim={character.Runner.SimulationTime:F3} isWeaponDrawn={character.isWeaponDrawn} combatMove={combatMove}");
        }

        enteredAt = Time.time;
        hasEnteredDrawClip = false;
        hasSwapped = false;

        if (weaponController == null)
            weaponController = character.GetComponentInChildren<WeaponController>(true);

        if (weaponController == null || weaponController.GetCurrentWeapon() == null)
        {
            if (TOGGLE_DEBUG) Debug.LogWarning($"[ToggleWeapon][DrawState] No WeaponController/weapon. Abort draw. frame={Time.frameCount} sim={character.Runner.SimulationTime:F3}");
            // No weapon to draw -> stay in unarmed locomotion.
            character.isWeaponDrawn = false;
            if (character.animator != null)
                character.animator.SetBool("combatMove", false);
            character.currentLocomotionState = character.standing;
            stateMachine.ChangeState(character.currentLocomotionState);
            return;
        }

        // Start draw animation, but do NOT swap weapon visuals/scripts yet.
        // Swap will happen later when Upper body normalizedTime reaches WeaponSO.drawEquipNormalizedTime.
        weaponController.TriggerDrawAnimationOnly();

        // Consider weapon still sheathed until we reach the swap point.
        character.isWeaponDrawn = false;
        if (character.animator != null)
            character.animator.SetBool("combatMove", true);

        // Lock locomotion truth: khi đang draw/hit, lối thoát khỏi GetHit phải quay về CombatMove.
        character.currentLocomotionState = character.combatMove;
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
            stateMachine.ChangeState(character.combatMove);
            return;
        }

        // 1. KIỂM TRA INPUT SPRINT (Shift)
        bool isSprinting = SprintPressed && input.sqrMagnitude > 0f;

        // 2. ÉP TỐC ĐỘ ANIMATOR (Sprint = 1.0, Chạy bộ = 0.5)
        float animSpeedMultiplier = isSprinting ? 1f : 0.5f;
        character.SetAnimatorLocomotionSpeed(input.magnitude * animSpeedMultiplier);

        // Swap at the configured timing, then wait for clip end.
        TrySwapAtTiming();

        // Prefer checking the UpperBody layer since Draw/Sheath is authored there.
        bool finished = IsUpperBodyClipFinished();
        bool timedOut = (Time.time - enteredAt) >= TimeoutSeconds;

        if (finished || timedOut)
        {
            // Failsafe: if something prevented swap, force drawn before exiting.
            if (!hasSwapped && weaponController != null)
            {
                weaponController.EnsureDrawn(requestAnimation: false);
                character.isWeaponDrawn = true;
                if (character.animator != null)
                    character.animator.SetBool("combatMove", true);
                character.currentLocomotionState = character.combatMove;
            }
            character.currentLocomotionState = character.combatMove;
            stateMachine.ChangeState(character.currentLocomotionState);
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
        if (!(st.IsTag(DrawTag) || st.IsName("Draw Weapon"))) return;

        float t = Mathf.Clamp01(weapon.drawEquipNormalizedTime);
        if (st.normalizedTime < t) return;

        weaponController.EnsureDrawn(requestAnimation: false);
        character.isWeaponDrawn = true;
        if (character.animator != null)
            character.animator.SetBool("combatMove", true);
        character.currentLocomotionState = character.combatMove;
        hasSwapped = true;
    }

    private bool IsUpperBodyClipFinished()
    {
        if (character.animator.layerCount <= UpperBodyLayerIndex) return false;

        if (character.animator.IsInTransition(UpperBodyLayerIndex)) return false;

        var st = character.animator.GetCurrentAnimatorStateInfo(UpperBodyLayerIndex);
        if (!hasEnteredDrawClip)
        {
            if (st.IsTag(DrawTag) || st.IsName("Draw Weapon"))
                hasEnteredDrawClip = true;
            else
                return false;
        }

        if (TOGGLE_DEBUG && hasEnteredDrawClip)
        {
            Debug.Log($"[ToggleWeapon][DrawState.Anim] frame={Time.frameCount} norm={st.normalizedTime:F3} tagDraw={st.IsTag(DrawTag)} nameDraw={st.IsName("Draw Weapon")} inTrans={character.animator.IsInTransition(UpperBodyLayerIndex)}");
        }

        return st.normalizedTime >= 1f;
    }
}

