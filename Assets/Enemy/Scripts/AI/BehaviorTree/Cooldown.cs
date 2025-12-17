using UnityEngine;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Cooldown Decorator - Chờ cooldown trước khi thực thi child
    /// Trả về Running trong thời gian cooldown
    /// Trả về kết quả của child sau khi cooldown hết
    /// </summary>
    public class Cooldown : Node
    {
        private float cooldownTime;
        private float lastExecutionTime;
        private bool useSharedCooldown;
        private string cooldownKey;

        /// <summary>
        /// Tạo Cooldown decorator
        /// </summary>
        /// <param name="cooldown">Thời gian cooldown (seconds)</param>
        /// <param name="sharedKey">Key để share cooldown giữa nhiều nodes (optional)</param>
        public Cooldown(float cooldown, string sharedKey = null) : base()
        {
            cooldownTime = cooldown;
            lastExecutionTime = -cooldown; // Cho phép chạy ngay lần đầu
            useSharedCooldown = !string.IsNullOrEmpty(sharedKey);
            cooldownKey = sharedKey ?? "cooldown_" + GetHashCode();
        }

        public Cooldown(float cooldown, Node child, string sharedKey = null) : base()
        {
            cooldownTime = cooldown;
            lastExecutionTime = -cooldown;
            useSharedCooldown = !string.IsNullOrEmpty(sharedKey);
            cooldownKey = sharedKey ?? "cooldown_" + GetHashCode();
            Attach(child);
        }

        public override NodeState Evaluate()
        {
            if (children.Count == 0)
            {
                state = NodeState.Failure;
                return state;
            }

            float currentTime = Time.time;
            float lastTime = lastExecutionTime;

            // Check shared cooldown nếu có
            if (useSharedCooldown)
            {
                object sharedTime = GetData(cooldownKey);
                if (sharedTime != null && sharedTime is float)
                {
                    lastTime = (float)sharedTime;
                }
            }

            // Kiểm tra cooldown
            if (currentTime - lastTime < cooldownTime)
            {
                state = NodeState.Running;
                return state;
            }

            // Cooldown đã hết, thực thi child
            NodeState childState = children[0].Evaluate();

            // Nếu child thành công hoặc đang chạy, update cooldown
            if (childState == NodeState.Success || childState == NodeState.Running)
            {
                lastExecutionTime = currentTime;
                
                // Update shared cooldown nếu có
                if (useSharedCooldown)
                {
                    SetData(cooldownKey, currentTime);
                }
            }

            state = childState;
            return state;
        }

        public override void Reset()
        {
            base.Reset();
            lastExecutionTime = -cooldownTime; // Reset để có thể chạy ngay
        }
    }
}





