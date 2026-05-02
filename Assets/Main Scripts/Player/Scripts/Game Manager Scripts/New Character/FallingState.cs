using UnityEngine;

public class FallingState : State
{
    float gravityValue;
    float playerSpeed;
    Vector3 airVelocity;
    
    // THÊM: Biến lưu thời điểm bắt đầu rơi
    float fallStartTime; 

    public FallingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        gravityValue = character.gravityValue;
        playerSpeed = character.playerSpeed;

        character.animator.applyRootMotion = false;
        character.animator.SetBool("isGrounded", false);
        
        // THÊM: Ghi lại thời điểm bắt đầu rơi vào State này (Dùng Runner.SimulationTime cho chuẩn Fusion mạng)
        fallStartTime = character.Runner.SimulationTime; 
    }

    public override void HandleInput()
    {
        base.HandleInput();
        input = MoveInput;
        
        if (TutorialInputGate.IsActive && (TutorialInputGate.EffectiveMask & TutorialInputMask.Move) == 0)
            input = Vector2.zero;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (character.CachedGroundedFeet || character.controller.isGrounded)
        {
            character.animator.SetBool("isGrounded", true); 

            // THÊM LOGIC CỦA BẠN: Kiểm tra xem đã lơ lửng đủ lâu chưa (ví dụ 0.15 giây)
            float timeInAir = character.Runner.SimulationTime - fallStartTime;
            
            // Nếu rơi lâu hơn 0.15s (Chắc chắn là do Jump hoặc rớt từ vách cao) mới cho nhún
            if (timeInAir > 0.15f) 
            {
                character.NetworkedIsLanding = true; 
                character.animator.SetTrigger("isLanding");
            }
            // Nếu nhỏ hơn 0.15s, hệ thống âm thầm bỏ qua cái Trigger Landing và chỉ chuyển State thôi!
            
            State nextState = character.currentLocomotionState != null ? character.currentLocomotionState : character.standing;
            stateMachine.ChangeState(nextState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

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
            
            Vector3 movement = (airVelocity * character.airControl + velocity * (1 - character.airControl)) * playerSpeed;
            character.CalculatedVelocity.x = movement.x;
            character.CalculatedVelocity.z = movement.z;

            if (movement.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(new Vector3(movement.x, 0, movement.z));
                character.transform.rotation = Quaternion.Slerp(character.transform.rotation, targetRotation, character.rotationDampTime);
            }
        }
    }
}
