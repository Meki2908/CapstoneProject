namespace ModularFootstepSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Footstep state controller.
    /// Stores the state type of footsteps and starts installing new data from this type.
    /// </summary>
    public class FootstepsStateController : MonoBehaviour
    {
        /// <summary>
        /// The type of footstep condition has changed.
        /// </summary>
        public event Action onFootstepStateChanged = delegate { };

        /// <summary>
        /// Current state type.
        /// </summary>
        public string StateType => currentStateType.Id;

        [SerializeField]
        protected FootstepStateType defaultStateType = default;

        [SerializeField]
        protected List<AbstractFootstepsDataChanger> changers = new List<AbstractFootstepsDataChanger>();

        [SerializeField]
        protected bool initDefaultStateOnAwake = false;

        protected FootstepStateType currentStateType = default;

        protected virtual void OnAwake()
        {
            if (initDefaultStateOnAwake && changers.Count > 0)
            {
                currentStateType = defaultStateType;
                SetState(defaultStateType);
            }
        }

        /// <summary>
        /// Sets up a new state type and causes data associated with that type to change.
        /// </summary>
        /// <param name="stateType">Footstep state type.</param>
        public virtual void SetState(FootstepStateType stateType)
        {
            if(currentStateType == null || stateType.Id != currentStateType.Id)
            {
                currentStateType = stateType;

                changers.ForEach(changer => changer?.ChangeData(currentStateType));

                onFootstepStateChanged();
            }
        }
    }
}