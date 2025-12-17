using System;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Condition Decorator - Chỉ thực thi child nếu condition đúng
    /// Trả về Failure nếu condition false
    /// Trả về kết quả của child nếu condition true
    /// </summary>
    public class Condition : Node
    {
        private Func<bool> condition;

        public Condition(Func<bool> condition) : base()
        {
            this.condition = condition;
        }

        public Condition(Func<bool> condition, Node child) : base()
        {
            this.condition = condition;
            Attach(child);
        }

        public override NodeState Evaluate()
        {
            if (children.Count == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            // Kiểm tra condition
            if (condition == null || !condition())
            {
                state = NodeState.Failure;
                return state;
            }

            // Condition đúng, thực thi child
            state = children[0].Evaluate();
            return state;
        }
    }
}





