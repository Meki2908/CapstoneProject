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
<<<<<<< HEAD
    
    // ĐỒNG HỒ ĐO THỜI GIAN TRÊN KHÔNG
    private float timeInAir;
    
    // Guard: đã thật sự rời mặt đất ít nhất 1 frame
    private bool hasLeftGround;
=======
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6

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
<<<<<<< HEAD
        timeInAir = 0f; // Reset đồng hồ khi bắt đầu rơi
        // Nếu chuyển sang rơi vì vừa nhảy (requireLanding == true) thì xem như chắc chắn đã rời đất.
        hasLeftGround = character.requireLanding;
=======
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
        
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
<<<<<<< HEAD
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

        // Tăng đồng hồ đo thời gian lơ lửng
        timeInAir += character.Runner.DeltaTime;
        
        // Đánh dấu đã rời đất: chỉ set khi grounded thực sự false (không dùng stable grace)
        bool groundedNow = character.CachedGroundedFeet || (character.controller != null && character.controller.isGrounded);
        if (!groundedNow)
            hasLeftGround = true;

        // 2. LƯU ĐỘ CAO RƠI MAX (Đợi >0.05s mới đo để né lỗi Raycast xuyên khe nứt lúc chạy qua dốc)
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

        // 3. XỬ LÝ CHẠM ĐẤT
        // Dùng grounded stable để tránh nhấp nháy 1-2 frame trên MeshCollider gồ ghề
        if (character.IsGroundedStable())
=======
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
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
        {
            character.animator.SetBool("isGrounded", true);
            character.animator.SetBool("isHighFalling", false);

<<<<<<< HEAD
            // Chỉ cho phép xét duyệt Landing sau khi đã rời đất thật sự (tránh Jump vừa bấm đã Land)
            if (hasLeftGround)
            {
                bool isValidToLand = character.requireLanding || timeInAir > 0.15f;

                if (isValidToLand)
                {
                    // Ưu tiên 1: Rơi cực cao -> Parkour Roll
                    if (maxDistanceToGroundRecord >= character.farFallDistanceForRoll)
                    {
                        character.isRollLanding = true;
                        stateMachine.ChangeState(character.dashing);
                        return;
                    }
                    // Ưu tiên 2: Rơi vừa hoặc Nhảy -> Khụy gối Landing
                    else if (character.requireLanding || maxDistanceToGroundRecord > character.minFallDistanceForLanding)
                    {
                        character.NetworkedIsLanding = true;
                        character.animator.SetTrigger("isLanding");
                        
                        character.animator.applyRootMotion = true; 
                        isRecoveringFromLanding = true;
                        landingTimer = 0f;
                        return; 
                    }
                }
            }

            // C. Rơi rất thấp, nhảy lách cách qua hòn đá, hoặc chạm đất ngay frame đầu -> Bỏ qua Landing
=======
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
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
            character.requireLanding = false;
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

<<<<<<< HEAD
        // ÉP VẬN TỐC BẰNG 0 ĐỂ ROOT MOTION TỰ LÀM VIỆC LÚC LANDING
=======
        // NẾU ĐANG LANDING: Khóa ngang, ép dính đất
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
        if (isRecoveringFromLanding)
        {
            character.CalculatedVelocity.x = 0f;
            character.CalculatedVelocity.z = 0f;
            character.playerVelocity.y = -2f; 
            return;
        }

<<<<<<< HEAD
        if (!character.IsGroundedStable())
=======
        if (!character.controller.isGrounded)
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
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
<<<<<<< HEAD
            bool applyExtraGravity = true;
            
            // Tắt gia tốc tạ rơi nếu đang trong quỹ đạo của cú Nhảy (chưa rớt dưới mốc xuất phát)
            if (character.requireLanding && character.transform.position.y >= character.jumpStartY)
            {
                applyExtraGravity = false; 
            }

            if (applyExtraGravity)
            {
                character.playerVelocity.y -= character.extraFallAcceleration * character.Runner.DeltaTime;
            }
            // ===============================================

            if (finalMovement.sqrMagnitude > 0.1f)
            {
=======
            character.playerVelocity.y -= character.extraFallAcceleration * character.Runner.DeltaTime;
            // ===============================================

            if (finalMovement.sqrMagnitude > 0.1f)
            {
>>>>>>> 977256f4eef791cf0127442fa9888b43439d29a6
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(finalMovement.x, 0, finalMovement.z));
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
            }
        }
    }
}