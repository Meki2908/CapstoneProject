using UnityEngine;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Base class cho Behavior Tree
    /// Cải tiến: Thêm tick rate control, pause/resume, và manual evaluation
    /// </summary>
    public abstract class BehaviorTree : MonoBehaviour
    {
        [Header("Behavior Tree Settings")]
        [Tooltip("Tick rate (0 = every frame, 1 = 1 per second, etc.)")]
        public float tickRate = 0f; // 0 = every frame
        
        [Tooltip("Pause tree execution")]
        public bool isPaused = false;

        [Tooltip("Enable debug logging")]
        public bool enableDebugLogs = false;

        private Node root = null;
        private float tickTimer = 0f;

        protected void Start()
        {
            root = SetupTree();
            if (enableDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Behavior Tree initialized");
            }
        }

        private void Update()
        {
            if (root == null || isPaused)
                return;

            // Tick rate control
            if (tickRate > 0f)
            {
                tickTimer += Time.deltaTime;
                if (tickTimer < 1f / tickRate)
                    return;
                tickTimer = 0f;
            }

            root.Evaluate();
        }

        /// <summary>
        /// Override method này để tạo cấu trúc tree
        /// </summary>
        protected abstract Node SetupTree();

        /// <summary>
        /// Manually trigger tree evaluation (useful for event-driven updates)
        /// </summary>
        public void EvaluateTree()
        {
            if (root != null && !isPaused)
            {
                root.Evaluate();
            }
        }

        /// <summary>
        /// Pause tree execution
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// Resume tree execution
        /// </summary>
        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>
        /// Reset tree to initial state
        /// </summary>
        public void ResetTree()
        {
            if (root != null)
            {
                root.Reset();
            }
        }

        /// <summary>
        /// Get current root node (for debugging)
        /// </summary>
        public Node GetRoot()
        {
            return root;
        }
    }
}
