using UnityEngine;

public class SprintJumpState : State
{
    private float animLength;
    private float timePassed;

    public SprintJumpState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine) { }

    public override void Enter()
    {
        base.Enter();

        // Bật root motion để animation điều khiển hoàn toàn movement
        character.animator.applyRootMotion = true;

        // Play sprintJump animation
        character.animator.SetTrigger("sprintJump");

        // Lấy độ dài clip jump để biết khi nào kết thúc
        AnimatorStateInfo info = character.animator.GetCurrentAnimatorStateInfo(0);
        animLength = info.length;

        timePassed = 0f;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        timePassed += Time.deltaTime;

        // Khi animation gần kết thúc thì quay lại Sprinting
        if (timePassed >= animLength * 0.95f) // 95% để tránh lệch frame
        {
            stateMachine.ChangeState(character.sprinting);
        }
    }

    public override void Exit()
    {
        base.Exit();
        // Tắt root motion để trả quyền lại cho code điều khiển
        character.animator.applyRootMotion = false;
    }
}
