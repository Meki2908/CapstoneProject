using UnityEngine;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Trạng thái trả về của node
    /// </summary>
    public enum NodeState
    {
        Running,  // Đang thực thi
        Success,  // Thành công
        Failure   // Thất bại
    }

    /// <summary>
    /// Base class cho tất cả các node trong Behavior Tree
    /// </summary>
    public abstract class Node
    {
        protected NodeState state;

        public Node parent;
        protected System.Collections.Generic.List<Node> children = new System.Collections.Generic.List<Node>();

        protected System.Collections.Generic.Dictionary<string, object> dataContext =
            new System.Collections.Generic.Dictionary<string, object>();

        // Reference to blackboard (optional)
        protected Blackboard blackboard = null;

        public Node()
        {
            parent = null;
        }

        /// <summary>
        /// Set blackboard reference
        /// </summary>
        public void SetBlackboard(Blackboard bb)
        {
            blackboard = bb;
        }

        /// <summary>
        /// Get blackboard từ parent hoặc từ context
        /// QUAN TRỌNG: Không gọi GetData() ở đây để tránh vòng lặp vô hạn với GetData()
        /// </summary>
        protected Blackboard GetBlackboard()
        {
            if (blackboard != null)
                return blackboard;

            // Tìm trong local context trước (không dùng GetData() để tránh vòng lặp)
            if (dataContext.TryGetValue("blackboard", out object bb) && bb is Blackboard)
                return (Blackboard)bb;

            // Tìm từ parent nodes (truy cập trực tiếp dataContext, không dùng GetData())
            Node node = parent;
            while (node != null)
            {
                // Kiểm tra blackboard trực tiếp
                if (node.blackboard != null)
                    return node.blackboard;
                
                // Kiểm tra dataContext của parent (không dùng GetData())
                if (node.dataContext.TryGetValue("blackboard", out bb) && bb is Blackboard)
                    return (Blackboard)bb;
                
                node = node.parent;
            }

            return null;
        }

        public Node(System.Collections.Generic.List<Node> children)
        {
            foreach (Node child in children)
            {
                Attach(child);
            }
        }

        /// <summary>
        /// Gắn child node
        /// </summary>
        public void Attach(Node node)
        {
            node.parent = this;
            children.Add(node);
        }

        /// <summary>
        /// Đánh giá và thực thi node
        /// </summary>
        public abstract NodeState Evaluate();

        /// <summary>
        /// Set data vào context
        /// </summary>
        public void SetData(string key, object value)
        {
            dataContext[key] = value;
        }

        /// <summary>
        /// Set data vào blackboard (nếu có)
        /// </summary>
        public void SetBlackboardData(string key, object value)
        {
            Blackboard bb = GetBlackboard();
            if (bb != null)
            {
                bb.SetData(key, value);
            }
            else
            {
                // Fallback to local context
                SetData(key, value);
            }
        }

        /// <summary>
        /// Get data từ context (tìm kiếm lên tree và blackboard)
        /// Tối ưu: Tránh recursive call, truy cập trực tiếp dataContext
        /// </summary>
        public object GetData(string key)
        {
            // Kiểm tra local context trước
            if (dataContext.TryGetValue(key, out object value))
                return value;

            // Tìm kiếm lên tree (không dùng recursive để tránh stack overflow)
            Node node = parent;
            while (node != null)
            {
                // Truy cập trực tiếp dataContext thay vì gọi GetData() để tối ưu
                if (node.dataContext.TryGetValue(key, out value))
                    return value;
                node = node.parent;
            }

            // Nếu không tìm thấy trong tree, thử blackboard
            Blackboard bb = GetBlackboard();
            if (bb != null)
            {
                value = bb.GetData(key);
                if (value != null)
                    return value;
            }

            return null;
        }

        /// <summary>
        /// Clear data từ context
        /// Tối ưu: Tránh recursive call
        /// </summary>
        public bool ClearData(string key)
        {
            if (dataContext.ContainsKey(key))
            {
                dataContext.Remove(key);
                return true;
            }

            // Tìm kiếm lên tree (không dùng recursive)
            Node node = parent;
            while (node != null)
            {
                if (node.dataContext.ContainsKey(key))
                {
                    node.dataContext.Remove(key);
                    return true;
                }
                node = node.parent;
            }
            return false;
        }

        /// <summary>
        /// Reset node state về initial state
        /// </summary>
        public virtual void Reset()
        {
            state = NodeState.Running;
            foreach (var child in children)
            {
                child.Reset();
            }
        }
    }
}

