using System.Collections.Generic;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Inverter (Decorator) - Đảo ngược kết quả của child
    /// Success -> Failure
    /// Failure -> Success
    /// Running -> Running
    /// </summary>
    public class Inverter : Node
    {
        public Inverter() : base() { }
        public Inverter(Node child)
        {
            Attach(child);
        }

        public override NodeState Evaluate()
        {
            if (children.Count == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            switch (children[0].Evaluate())
            {
                case NodeState.Failure:
                    state = NodeState.Success;
                    return state;
                case NodeState.Success:
                    state = NodeState.Failure;
                    return state;
                case NodeState.Running:
                    state = NodeState.Running;
                    return state;
            }

            state = NodeState.Success;
            return state;
        }
    }
}
