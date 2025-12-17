using System.Collections.Generic;
using UnityEngine;

namespace AI.BehaviorTree
{
    /// <summary>
    /// Blackboard - Shared data storage cho Behavior Trees
    /// Cho phép nhiều trees/nodes chia sẻ data với nhau
    /// </summary>
    public class Blackboard : MonoBehaviour
    {
        private static Dictionary<string, Blackboard> instances = new Dictionary<string, Blackboard>();
        
        [Header("Blackboard Settings")]
        [Tooltip("Unique ID cho blackboard này (để share giữa nhiều objects)")]
        public string blackboardID = "default";

        private Dictionary<string, object> data = new Dictionary<string, object>();

        private void Awake()
        {
            // Register instance
            if (!string.IsNullOrEmpty(blackboardID))
            {
                if (!instances.ContainsKey(blackboardID))
                {
                    instances[blackboardID] = this;
                }
            }
        }

        private void OnDestroy()
        {
            // Unregister instance
            if (!string.IsNullOrEmpty(blackboardID) && instances.ContainsKey(blackboardID))
            {
                if (instances[blackboardID] == this)
                {
                    instances.Remove(blackboardID);
                }
            }
        }

        /// <summary>
        /// Set data vào blackboard
        /// </summary>
        public void SetData(string key, object value)
        {
            data[key] = value;
        }

        /// <summary>
        /// Get data từ blackboard
        /// </summary>
        public object GetData(string key)
        {
            if (data.TryGetValue(key, out object value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Check if key exists
        /// </summary>
        public bool HasData(string key)
        {
            return data.ContainsKey(key);
        }

        /// <summary>
        /// Remove data từ blackboard
        /// </summary>
        public bool RemoveData(string key)
        {
            return data.Remove(key);
        }

        /// <summary>
        /// Clear all data
        /// </summary>
        public void Clear()
        {
            data.Clear();
        }

        /// <summary>
        /// Get static blackboard instance by ID
        /// </summary>
        public static Blackboard GetInstance(string id)
        {
            if (instances.TryGetValue(id, out Blackboard instance))
            {
                return instance;
            }
            return null;
        }

        /// <summary>
        /// Get all data keys (for debugging)
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return data.Keys;
        }

        /// <summary>
        /// Get data count (for debugging)
        /// </summary>
        public int GetDataCount()
        {
            return data.Count;
        }
    }
}





