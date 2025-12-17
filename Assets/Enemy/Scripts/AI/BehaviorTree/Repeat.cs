using System.Collections.Generic;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Repeat Decorator - Lặp lại child node N lần
    /// Success sau khi lặp đủ số lần
    /// </summary>
    public class Repeat : Node
    {
        private int repeatCount;
        private int currentCount;

        public Repeat(int count) : base()
        {
            repeatCount = count;
            currentCount = 0;
        }

        public Repeat(int count, Node child) : base()
        {
            repeatCount = count;
            currentCount = 0;
            Attach(child);
        }

        public override NodeState Evaluate()
        {
            if (children.Count == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            while (currentCount < repeatCount)
            {
                NodeState childState = children[0].Evaluate();

                if (childState == NodeState.Running)
                {
                    state = NodeState.Running;
                    return state;
                }
                else if (childState == NodeState.Failure)
                {
                    currentCount = 0; // Reset on failure
                    state = NodeState.Failure;
                    return state;
                }

                currentCount++;
            }

            currentCount = 0; // Reset for next evaluation
            state = NodeState.Success;
            return state;
        }

        public override void Reset()
        {
            base.Reset();
            currentCount = 0;
        }
    }
}





