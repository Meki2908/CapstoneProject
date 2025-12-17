using System.Collections.Generic;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Parallel Node - Thực thi tất cả children đồng thời
    /// Có 2 modes:
    /// - AllSuccess: Trả về Success khi TẤT CẢ children Success
    /// - AnySuccess: Trả về Success khi BẤT KỲ child nào Success
    /// </summary>
    public class Parallel : Node
    {
        public enum ParallelMode
        {
            AllSuccess,  // Tất cả phải Success
            AnySuccess   // Chỉ cần 1 Success
        }

        private ParallelMode mode;

        public Parallel(ParallelMode mode) : base()
        {
            this.mode = mode;
        }

        public Parallel(ParallelMode mode, List<Node> children) : base(children)
        {
            this.mode = mode;
        }

        public override NodeState Evaluate()
        {
            if (children.Count == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            int successCount = 0;
            int failureCount = 0;

            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Success:
                        successCount++;
                        break;
                    case NodeState.Failure:
                        failureCount++;
                        break;
                    case NodeState.Running:
                        // Nếu có child đang Running, sẽ xử lý ở logic dưới
                        break;
                }
            }

            // Determine result based on mode
            if (mode == ParallelMode.AllSuccess)
            {
                if (successCount == children.Count)
                {
                    state = NodeState.Success;
                }
                else if (failureCount > 0)
                {
                    state = NodeState.Failure;
                }
                else
                {
                    state = NodeState.Running;
                }
            }
            else // AnySuccess
            {
                if (successCount > 0)
                {
                    state = NodeState.Success;
                }
                else if (failureCount == children.Count)
                {
                    state = NodeState.Failure;
                }
                else
                {
                    state = NodeState.Running;
                }
            }

            return state;
        }
    }
}

