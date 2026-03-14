namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Serialized data assigned to footstep state type. Required to install data through the inspector.
    /// </summary>
    /// <typeparam name="T">Type of data assigned to footstep state type.</typeparam>
    [System.Serializable]
    public class SerializableFootstepStateData<T> where T : AbstractFootstepData
    {
        /// <summary>
        /// Footstep state type.
        /// </summary>
        public virtual FootstepStateType FootstepStateType => footstepStateType;

        /// <summary>
        /// List of data assigned to footstep state type.
        /// </summary>
        public virtual IReadOnlyList<T> Data => data;

        [SerializeField]
        protected FootstepStateType footstepStateType = default;

        [SerializeField]
        protected List<T> data = new List<T>();
    }
}