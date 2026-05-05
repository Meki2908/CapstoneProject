using UnityEngine;

public class FallingState : State
{
    const float MaxGroundRayDistance = 50f;

    float playerSpeed;
    Vector3 airVelocity;
    
    private Vector3 inheritedInertia; 
    private float maxDistanceToGroundRecord;

    // --- BIẾN KIỂM SOÁT ROOT MOTION LÚC LANDING ---
    private bool isRecoveringFromLanding;
    private float landingTimer;

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
        
        inheritedInertia = character.momentumToInherit;
        character.momentumToInherit = Vector3.zero; 

        isRecoveringFromLanding = false;
        landingTimer = 0f;
    }

    public override void HandleInput()
    {
        base.HandleInput();
        
        if (isRecoveringFromLanding)
        {
            input = Vector2.zero; // Khóa phím lúc Landing
        }
        else
        {
            input = MoveInput;
            if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
                input = Vector2.zero;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 1. ĐANG KHỤY GỐI LANDING -> CHỜ ANIMATOR XONG MỚI NHẢ
        if (isRecoveringFromLanding)
        {
            landingTimer += character.Runner.DeltaTime;
            
            int lowerLayer = character.animator.GetLayerIndex("Lower body");
            bool isPlayingLanding = false;
            if (lowerLayer >= 0)
                isPlayingLanding = character.animator.GetCurrentAnimatorStateInfo(lowerLayer).IsName("Landing");

            if (landingTimer > 0.15f && (!isPlayingLanding || landingTimer > 1.5f))
            {
                character.animator.applyRootMotion = false; 
                State next = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
                stateMachine.ChangeState(next);
            }
            return; 
        }

        // 2. TÌM KHOẢNG CÁCH MAX ĐỂ KÍCH HOẠT FALLING IDLE
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

        // 3. XỬ LÝ CHẠM ĐẤT
        if (character.CachedGroundedFeet || character.controller.isGrounded)
        {
            character.animator.SetBool("isGrounded", true);
            character.animator.SetBool("isHighFalling", false);

            // A. Rơi cực cao -> Parkour Roll
            if (maxDistanceToGroundRecord >= character.farFallDistanceForRoll)
            {
                character.isRollLanding = true;
                stateMachine.ChangeState(character.dashing);
                return;
            }
            // B. Rơi vừa -> Khụy gối Landing (Khóa Root Motion)
            else if (character.requireLanding || maxDistanceToGroundRecord > character.minFallDistanceForLanding)
            {
                character.NetworkedIsLanding = true;
                character.animator.SetTrigger("isLanding");
                
                character.animator.applyRootMotion = true; // Bật Root Motion
                isRecoveringFromLanding = true;
                landingTimer = 0f;
                return; 
            }

            // C. Rơi thấp -> Về lại chạy/đứng ngay lập tức
            character.requireLanding = false;
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // NẾU ĐANG LANDING: Khóa ngang, ép dính đất
        if (isRecoveringFromLanding)
        {
            character.CalculatedVelocity.x = 0f;
            character.CalculatedVelocity.z = 0f;
            character.playerVelocity.y = -2f; 
            return;
        }

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

            inheritedInertia = Vector3.Lerp(inheritedInertia, Vector3.zero, character.fallInertiaDecayRate * character.Runner.DeltaTime);

            Vector3 playerControlMovement = (airVelocity * character.airControl + velocity * (1 - character.airControl)) * playerSpeed;
            Vector3 finalMovement = playerControlMovement + inheritedInertia;

            character.CalculatedVelocity.x = finalMovement.x;
            character.CalculatedVelocity.z = finalMovement.z;

            // === ÉP GIA TỐC RƠI (Trọng trường nhân tạo) ===
            character.playerVelocity.y -= character.extraFallAcceleration * character.Runner.DeltaTime;
            // ===============================================

            if (finalMovement.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(finalMovement.x, 0, finalMovement.z));
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
            }
        }
    }
}