using System.Collections.Generic;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Selector with Memory - Giống Selector nhưng nhớ child đang Running
    /// Khi một child trả về Running, lần sau sẽ tiếp tục từ child đó
    /// Thay vì bắt đầu lại từ đầu
    /// </summary>
    public class SelectorWithMemory : Node
    {
        private int currentChildIndex = 0;

        public SelectorWithMemory() : base() { }
        public SelectorWithMemory(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            // Bắt đầu từ child đang chạy (nếu có)
            for (int i = currentChildIndex; i < children.Count; i++)
            {
                switch (children[i].Evaluate())
                {
                    case NodeState.Failure:
                        continue;
                    case NodeState.Success:
                        currentChildIndex = 0; // Reset về đầu
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        currentChildIndex = i; // Nhớ child đang chạy
                        state = NodeState.Running;
                        return state;
                    default:
                        continue;
                }
            }

            // Tất cả children đều Failure
            currentChildIndex = 0; // Reset về đầu
            state = NodeState.Failure;
            return state;
        }

        public override void Reset()
        {
            base.Reset();
            currentChildIndex = 0;
        }
    }
}





