using System.Collections.Generic;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Selector Node - Thử từng child cho đến khi có Success
    /// Trả về Success nếu BẤT KỲ child nào Success
    /// Trả về Failure nếu TẤT CẢ children Failure
    /// </summary>
    public class Selector : Node
    {
        public Selector() : base() { }
        public Selector(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Failure:
                        continue;
                    case NodeState.Success:
                        state = NodeState.Success;
                        return state;
                    case NodeState.Running:
                        state = NodeState.Running;
                        return state;
                    default:
                        continue;
                }
            }

            state = NodeState.Failure;
            return state;
        }
    }
}
