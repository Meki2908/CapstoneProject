using System.Collections.Generic;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Sequence with Memory - Giống Sequence nhưng nhớ child đang Running
    /// Khi một child trả về Running, lần sau sẽ tiếp tục từ child đó
    /// Thay vì bắt đầu lại từ đầu
    /// </summary>
    public class SequenceWithMemory : Node
    {
        private int currentChildIndex = 0;

        public SequenceWithMemory() : base() { }
        public SequenceWithMemory(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            // Bắt đầu từ child đang chạy (nếu có)
            for (int i = currentChildIndex; i < children.Count; i++)
            {
                switch (children[i].Evaluate())
                {
                    case NodeState.Failure:
                        currentChildIndex = 0; // Reset về đầu
                        state = NodeState.Failure;
                        return state;
                    case NodeState.Success:
                        continue;
                    case NodeState.Running:
                        currentChildIndex = i; // Nhớ child đang chạy
                        state = NodeState.Running;
                        return state;
                    default:
                        currentChildIndex = 0;
                        state = NodeState.Success;
                        return state;
                }
            }

            // Tất cả children đều Success
            currentChildIndex = 0; // Reset về đầu
            state = NodeState.Success;
            return state;
        }

        public override void Reset()
        {
            base.Reset();
            currentChildIndex = 0;
        }
    }
}





