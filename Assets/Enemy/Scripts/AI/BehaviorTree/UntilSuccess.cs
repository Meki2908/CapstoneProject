namespace AI.BehaviorTree
{
    /// <summary>
    /// UntilSuccess Decorator - Lặp lại child cho đến khi Success
    /// Trả về Success khi child Success
    /// Trả về Running nếu child đang Running
    /// </summary>
    public class UntilSuccess : Node
    {
        public UntilSuccess() : base() { }
        
        public UntilSuccess(Node child) : base()
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
                case NodeState.Success:
                    state = NodeState.Success;
                    return state;
                case NodeState.Running:
                    state = NodeState.Running;
                    return state;
                case NodeState.Failure:
                    // Tiếp tục lặp lại
                    state = NodeState.Running;
                    return state;
                default:
                    state = NodeState.Running;
                    return state;
            }
        }
    }
}





