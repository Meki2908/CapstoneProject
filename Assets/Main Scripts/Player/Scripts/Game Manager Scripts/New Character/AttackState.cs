using UnityEngine;

public class AttackState : State
{
    private const bool DEBUG_ATTACK_GATE = true;
    float timePassed;
    float clipLength;
    float clipSpeed;
    bool attack;
    bool jump;
    private bool dash;
    Vector2 movementInput;
    
    // --- HỆ THỐNG INPUT BUFFERING VÀ SNAP HƯỚNG ---
    bool pressedSinceLastCheck; 
    bool nextAttackBuffered;    
    Vector2 bufferedDirection;  
    
    const float commitPoint = 0.4f; 
    
    private bool waitingForComboTransition;
    private bool mageVfxSpawnedThisHit;
    
    // THÊM BIẾN BẢO VỆ MAGE: Hẹn giờ ngắt lệnh nếu Animator quá lỳ
    private float comboTransitionTimeout;

    private EquipmentSystem equipment;
    private WeaponController weaponController;
    private WeaponSO currentWeapon;
    private WeaponSO boundWeaponForHitRunner;
    private int hitIndex;
    private WeaponHitRunner hitHandler;
    
    private Quaternion targetAttackRotation;
    private bool isSnappingRotation;
    
    // === Smart Soft Lock-On ===
    private readonly Collider[] autoAimColliders = new Collider[20];
    private EnemyDetection enemyDetection;

    public AttackState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
        equipment = character.GetComponentInChildren<EquipmentSystem>();
        weaponController = character.GetComponentInChildren<WeaponController>();
    }

    public override void Enter()
    {
        base.Enter();
        
        if (enemyDetection == null)
            enemyDetection = character.GetComponentInChildren<EnemyDetection>(true);

        if (!character.isWeaponDrawn)
        {
            if (DEBUG_ATTACK_GATE)
            {
                bool combatMove = character.animator != null && character.animator.GetBool("combatMove");
                string fsm = stateMachine != null && stateMachine.currentState != null ? stateMachine.currentState.GetType().Name : "null";
                Debug.LogWarning($"[AttackGate] Enter blocked: isWeaponDrawn=false fsm={fsm} combatMove={combatMove} frame={Time.frameCount} sim={character.Runner.SimulationTime:F3}");
            }
            character.animator.ResetTrigger("attack");
            stateMachine.ChangeState(character.standing);
            return;
        }

        movementInput = Vector2.zero;
        attack = false;
        jump = false;
        dash = false;
        
        pressedSinceLastCheck = false;
        nextAttackBuffered = false;
        bufferedDirection = Vector2.zero;
        
        // === ĐÃ FIX: Bật khiên bảo vệ ngay từ đòn 1 ===
        waitingForComboTransition = true; 
        comboTransitionTimeout = 0.4f; // Cho Animator 0.4s để thoát khỏi trạng thái Locomotion/None
        // ==============================================

        mageVfxSpawnedThisHit = false;
        isSnappingRotation = false;

        character.lastAttackInputTime = character.Runner.SimulationTime;
        character.animator.applyRootMotion = false;
        timePassed = 0f;

        boundWeaponForHitRunner = null;
        ResolveAndSyncCurrentWeapon();
        hitIndex = 0;

        if (hitHandler == null)
            hitHandler = character.gameObject.AddComponent<WeaponHitRunner>();

        TryBindHitRunner();

        EnsureCorrectWeaponLayer();
        ApplyAttackSpeedToAnimator();

        character.animator.ResetTrigger("attack");
        character.animator.SetTrigger("attack");
        if (DEBUG_ATTACK_GATE)
            Debug.Log($"[AttackGate] Enter ok weapon={(currentWeapon != null ? currentWeapon.weaponType.ToString() : "null")} hitIndex={hitIndex} frame={Time.frameCount} sim={character.Runner.SimulationTime:F3}");
        // Keep combatMove true while attacking to ensure weapon layers behave correctly.
        if (character.animator != null)
            character.animator.SetBool("combatMove", true);
        
        character.playerVelocity = Vector3.zero;
        character.CalculatedVelocity = Vector3.zero; 
        character.animator.SetFloat("speed", 0f);
        mageVfxSpawnedThisHit = false;
        
        Vector2 initialInput = MoveInput;
        DetermineAttackRotation(initialInput);

        TutorialTextDisplay.NotifyNormalAttackStartedFromGameplay();

        if (hitHandler != null && currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0)
        {
            hitHandler.StartHit(hitIndex);
        }
    }

    public override void HandleInput()
    {
        base.HandleInput();

        movementInput = MoveInput;
        
        if (movementInput.sqrMagnitude > 0.01f)
        {
            bufferedDirection = movementInput;
        }

        if (AttackTriggered)
        {
            attack = true;
            pressedSinceLastCheck = true;
        }
        if (character.TryConsumeJumpBuffered(character.canStartJump)) jump = true;
        if (DashTriggered) dash = true;

        if (TutorialInputGate.IsActive)
        {
            var m = TutorialInputGate.EffectiveMask;
            if ((m & TutorialInputMask.Move) == 0) movementInput = Vector2.zero;
            if ((m & TutorialInputMask.Attack) == 0) attack = false;
            if ((m & TutorialInputMask.Jump) == 0) jump = false;
            if ((m & TutorialInputMask.Dash) == 0) dash = false;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        ResolveAndSyncCurrentWeapon();
        if (currentWeapon != null && currentWeapon.hitTimings != null && currentWeapon.hitTimings.Length > 0
            && hitIndex >= currentWeapon.hitTimings.Length)
        {
            hitIndex = currentWeapon.hitTimings.Length - 1;
        }

        TryBindHitRunner();
        EnsureCorrectWeaponLayer();

        timePassed += character.Runner.DeltaTime;
        
        if (isSnappingRotation)
        {
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetAttackRotation, character.attackRotationSpeed * character.Runner.DeltaTime);
            if (Quaternion.Angle(character.transform.rotation, targetAttackRotation) < 2f)
            {
                isSnappingRotation = false;
            }
        }

        int weaponLayerIndex = GetWeaponLayerIndex();
        float normalizedTime = 0f;
        bool isInTransition = false;

        if (weaponLayerIndex >= 0 && character.animator != null)
        {
            var stateInfo = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
            isInTransition = character.animator.IsInTransition(weaponLayerIndex);
            
            if (isInTransition)
            {
                var nextStateInfo = character.animator.GetNextAnimatorStateInfo(weaponLayerIndex);
                normalizedTime = nextStateInfo.normalizedTime; 
            }
            else
            {
                normalizedTime = stateInfo.normalizedTime;
            }
        }

        ApplyAttackSpeedToAnimator();

        // 1. MỞ KHÓA THÔNG MINH BẰNG TAG — chỉ tin transition tới state có tag "Attack"
        if (waitingForComboTransition && character.animator != null && weaponLayerIndex >= 0)
        {
            if (isInTransition)
            {
                var nextState = character.animator.GetNextAnimatorStateInfo(weaponLayerIndex);
                if (nextState.IsTag("Attack"))
                {
                    waitingForComboTransition = false;
                    comboTransitionTimeout = 0f;
                }
            }
            else
            {
                // Fallback: nếu đã vào state tag Attack mà không báo transition, mở khóa luôn.
                var currState = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
                if (currState.IsTag("Attack"))
                {
                    waitingForComboTransition = false;
                    comboTransitionTimeout = 0f;
                }
            }
        }

        // 2. FAILSAFE độc lập — không nằm trong canChain
        if (waitingForComboTransition && !isInTransition && timePassed >= comboTransitionTimeout)
        {
            ForceExitState();
            return;
        }

        // =========================================================================
        // === CHỈ XỬ LÝ VFX/COMBO KHI ĐÃ VÀO ĐÚNG STATE ATTACK (tránh đọc normalizedTime state cũ) ===
        // =========================================================================
        if (!waitingForComboTransition)
        {
            if (currentWeapon != null && currentWeapon.weaponType == WeaponType.Mage
                && currentWeapon.hitTimings != null
                && hitIndex >= 0 && hitIndex < currentWeapon.hitTimings.Length)
            {
                if (!mageVfxSpawnedThisHit)
                {
                    float vfxTargetPercent = Mathf.Clamp01(currentWeapon.hitTimings[hitIndex].vfxTime);

                    if (normalizedTime >= vfxTargetPercent)
                    {
                        mageVfxSpawnedThisHit = true;
                        var mageAtk = character.GetComponentInChildren<MageNormalAttack>(true);
                        if (mageAtk != null)
                            mageAtk.FireProjectileFSM(hitIndex);
                    }
                }
            }

            if (!nextAttackBuffered && normalizedTime >= commitPoint)
            {
                nextAttackBuffered = pressedSinceLastCheck;
                pressedSinceLastCheck = false;
            }

            float currentChainPoint = 0.75f;
            float currentMoveCancelPoint = 0.85f;
            bool isLastHit = false;

            if (currentWeapon != null && currentWeapon.hitTimings != null && hitIndex < currentWeapon.hitTimings.Length)
            {
                currentChainPoint = currentWeapon.hitTimings[hitIndex].chainPoint;
                currentMoveCancelPoint = currentWeapon.hitTimings[hitIndex].moveCancelPoint;

                if (hitIndex >= currentWeapon.hitTimings.Length - 1)
                {
                    isLastHit = true;
                }
            }

            bool canChain = !isInTransition && normalizedTime >= currentChainPoint;
            bool canCombo = canChain && !isLastHit;

            // ===== RESTART COMBO (hit cuối) =====
            // Nếu đang ở hit cuối và người chơi bấm Attack tại điểm chain,
            // ta restart về hit 1 ngay trong AttackState để tránh kẹt state (do nhánh exit yêu cầu !attack).
            if (isLastHit && canChain && (nextAttackBuffered || attack))
            {
                timePassed = 0f;
                hitIndex = 0;
                mageVfxSpawnedThisHit = false;

                character.animator.ResetTrigger("attack");
                character.animator.SetTrigger("attack");

                DetermineAttackRotation(bufferedDirection);

                waitingForComboTransition = true;
                comboTransitionTimeout = timePassed + 0.4f;

                attack = false;
                nextAttackBuffered = false;
                pressedSinceLastCheck = false;
                bufferedDirection = Vector2.zero;

                if (hitHandler != null)
                    hitHandler.StartHit(hitIndex);

                return;
            }

            // 1. TRIỂN KHAI ĐÒN TIẾP THEO
            if (canCombo && (nextAttackBuffered || attack))
            {
                timePassed = 0f;
                hitIndex++;
                mageVfxSpawnedThisHit = false;

                character.animator.ResetTrigger("attack");
                character.animator.SetTrigger("attack");

                DetermineAttackRotation(bufferedDirection);

                waitingForComboTransition = true;
                comboTransitionTimeout = timePassed + 0.4f;

                attack = false;
                nextAttackBuffered = false;
                pressedSinceLastCheck = false;
                bufferedDirection = Vector2.zero;

                if (hitHandler != null)
                    hitHandler.StartHit(hitIndex);
            }
            // 2. CHỜ THU CHIÊU (HOẶC BỊ KHÓA ĐÒN MỚI)
            else if (canChain)
            {
                if (!nextAttackBuffered && !attack)
                {
                    bool isActuallyNone = false;
                    if (weaponLayerIndex >= 0)
                    {
                        var currState = character.animator.GetCurrentAnimatorStateInfo(weaponLayerIndex);
                        isActuallyNone = (currState.shortNameHash == Animator.StringToHash("None"));
                    }

                    bool canMoveCancel = normalizedTime >= currentMoveCancelPoint;
                    bool allowExitByNormalizedEnd = !isInTransition && normalizedTime >= 0.95f;

                    if (movementInput.sqrMagnitude > 0.01f && canMoveCancel)
                    {
                        if (weaponLayerIndex >= 0)
                            character.animator.CrossFade("None", 0.1f, weaponLayerIndex);

                        ForceExitState();
                        return;
                    }

                    if (isActuallyNone || allowExitByNormalizedEnd)
                    {
                        ForceExitState();
                        return;
                    }
                }
            }
        }
        // =========================================================================

        if (jump)
        {
            if (hitHandler != null) hitHandler.CancelCurrentHit();
            character.animator.SetTrigger("jump"); 
            stateMachine.ChangeState(character.jumping);
            return;
        }

        if (dash)
        {
            if (hitHandler != null) hitHandler.CancelCurrentHit();
            character.animator.SetTrigger("dash"); 
            stateMachine.ChangeState(character.dashing);
            return;
        }

        character.animator.SetFloat("speed", 0f);
    }

    private void ForceExitState()
    {
        if (DEBUG_ATTACK_GATE)
        {
            bool combatMove = character.animator != null && character.animator.GetBool("combatMove");
            Debug.Log($"[AttackGate] ForceExitState waitingCombo={waitingForComboTransition} buffered={nextAttackBuffered} isWeaponDrawn={character.isWeaponDrawn} combatMove={combatMove} frame={Time.frameCount} sim={character.Runner.SimulationTime:F3}");
        }
        timePassed = 0f;
        waitingForComboTransition = false;
        comboTransitionTimeout = 0f;
        if (hitHandler != null) hitHandler.CancelCurrentHit();
        stateMachine.ChangeState(character.combatMove);
    }

    private void SetTargetRotation(Vector2 inputDir)
    {
        Vector3 camForward = character.cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = character.cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 targetDirection = (camForward * inputDir.y + camRight * inputDir.x).normalized;
        if (targetDirection != Vector3.zero)
        {
            targetAttackRotation = Quaternion.LookRotation(targetDirection);
            isSnappingRotation = true; 
        }
    }

    private void DetermineAttackRotation(Vector2 inputDir)
    {
        float searchRange = 4f;
        LayerMask mask;

        if (enemyDetection != null)
        {
            mask = enemyDetection.EnemyLayerMask;

            if (currentWeapon != null && currentWeapon.weaponType == WeaponType.Mage)
                searchRange = enemyDetection.MageAttackRange;
            else
                searchRange = enemyDetection.GetCurrentWeaponAttackRangePublic() * 1.2f;
        }
        else
        {
            mask = LayerMask.GetMask("Enemy");
        }

        Vector3 charPos = character.transform.position;
        Vector3 charForward = character.transform.forward;
        charForward.y = 0f;
        if (charForward.sqrMagnitude > 1e-6f) charForward.Normalize();
        else charForward = Vector3.forward;

        int count = Physics.OverlapSphereNonAlloc(charPos, searchRange, autoAimColliders, mask);

        Transform nearest = null;
        float minSqrDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = autoAimColliders[i];
            if (col == null) continue;

            TakeDamageTest hp = col.GetComponentInParent<TakeDamageTest>();
            if (hp != null && !hp.IsAlive()) continue;

            Transform targetRoot = hp != null ? hp.transform : col.transform.root;
            if (targetRoot == null || !targetRoot.gameObject.activeInHierarchy) continue;

            Vector3 dirToTarget = targetRoot.position - charPos;
            dirToTarget.y = 0f;
            if (dirToTarget.sqrMagnitude < 0.001f) continue;

            float dot = Vector3.Dot(charForward, dirToTarget.normalized);
            if (dot < 0f) continue; // ignore targets behind the player

            float sqrDist = dirToTarget.sqrMagnitude;
            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = targetRoot;
            }
        }

        if (nearest != null)
        {
            Vector3 dir = nearest.position - charPos;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                targetAttackRotation = Quaternion.LookRotation(dir.normalized);
                isSnappingRotation = true;
                return;
            }
        }

        if (inputDir.sqrMagnitude > 0.01f)
        {
            SetTargetRotation(inputDir);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (!character.controller.isGrounded)
            character.playerVelocity.y += character.gravityValue * Time.fixedDeltaTime;
        else
            character.playerVelocity.y = 0f;
        character.controller.Move(character.playerVelocity * Time.fixedDeltaTime);
    }

    public override void Exit()
    {
        base.Exit();
        if (hitHandler != null) hitHandler.CancelCurrentHit();
        character.animator.SetFloat("speed", 0f);
        character.animator.ResetTrigger("attack");
        if (character.animator != null)
            character.animator.speed = 1f;
        isSnappingRotation = false; 
    }

    public int GetCurrentHitIndex() { return hitIndex; }

    /// <summary>Equipment có thể chưa sync với WeaponController (Fusion / thứ tự Start). Luôn ưu tiên SO thật.</summary>
    private void ResolveAndSyncCurrentWeapon()
    {
        currentWeapon = equipment != null ? equipment.GetCurrentWeapon() : null;
        if (currentWeapon == null && weaponController == null)
            weaponController = character.GetComponent<WeaponController>();
        if (currentWeapon == null && weaponController != null)
        {
            currentWeapon = weaponController.GetCurrentWeapon();
            if (currentWeapon != null && equipment != null)
                equipment.SyncWeapon(currentWeapon);
        }
    }

    private void TryBindHitRunner()
    {
        if (hitHandler == null || currentWeapon == null || currentWeapon == boundWeaponForHitRunner)
            return;
        hitHandler.Bind(currentWeapon, equipment, character.transform, null, character.transform);
        boundWeaponForHitRunner = currentWeapon;
    }

    private static int GetWeaponTypeIntFromAnimator(Animator anim)
    {
        if (anim == null) return 0;
        foreach (var p in anim.parameters)
        {
            if (p.name == "weaponType" && p.type == AnimatorControllerParameterType.Int)
                return anim.GetInteger("weaponType");
        }
        return 0;
    }

    private WeaponType ResolveWeaponTypeForLayers()
    {
        if (currentWeapon != null)
            return currentWeapon.weaponType;
        int ti = GetWeaponTypeIntFromAnimator(character.animator);
        if (ti >= (int)WeaponType.Sword && ti <= (int)WeaponType.Mage)
            return (WeaponType)ti;
        return WeaponType.None;
    }

    private void EnsureCorrectWeaponLayer()
    {
        if (character.animator == null) return;
        var wt = ResolveWeaponTypeForLayers();
        if (wt == WeaponType.None) return;
        SetLayerWeightSafe(1, 0f); SetLayerWeightSafe(2, 0f); SetLayerWeightSafe(3, 0f);
        switch (wt)
        {
            case WeaponType.Sword: SetLayerWeightSafe(1, 1f); break;
            case WeaponType.Axe: SetLayerWeightSafe(2, 1f); break;
            case WeaponType.Mage: SetLayerWeightSafe(3, 1f); break;
        }
    }

    private int GetWeaponLayerIndex()
    {
        switch (ResolveWeaponTypeForLayers())
        {
            case WeaponType.Sword: return 1;
            case WeaponType.Axe: return 2;
            case WeaponType.Mage: return 3;
            default: return 1;
        }
    }

    private void SetLayerWeightSafe(int layer, float weight)
    {
        if (character.animator != null && layer >= 0 && layer < character.animator.layerCount)
            character.animator.SetLayerWeight(layer, weight);
    }

    private void ApplyAttackSpeedToAnimator()
    {
        if (character.animator == null) return;
        float attackSpeedMultiplier = 1f;
        if (EquipmentManager.Instance != null)
            attackSpeedMultiplier = 1f + EquipmentManager.Instance.GetTotalAttackSpeedBonus();

        int weaponLayerIndex = GetWeaponLayerIndex();
        if (weaponLayerIndex >= 0)
        {
            character.animator.speed = attackSpeedMultiplier;
            if (character.animator.parameters != null)
            {
                foreach (var param in character.animator.parameters)
                {
                    if (param.name == "attackSpeed" && param.type == AnimatorControllerParameterType.Float)
                    {
                        character.animator.SetFloat("attackSpeed", attackSpeedMultiplier);
                        break;
                    }
                }
            }
        }
    }
}