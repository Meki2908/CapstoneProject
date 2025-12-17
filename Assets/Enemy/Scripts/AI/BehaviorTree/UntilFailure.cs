namespace AI.BehaviorTree
{
    /// <summary>
    /// UntilFailure Decorator - Lặp lại child cho đến khi Failure
    /// Trả về Success khi child Failure
    /// Trả về Running nếu child đang Running
    /// </summary>
    public class UntilFailure : Node
    {
        public UntilFailure() : base() { }
        
        public UntilFailure(Node child) : base()
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
                case NodeState.Running:
                    state = NodeState.Running;
                    return state;
                case NodeState.Success:
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





