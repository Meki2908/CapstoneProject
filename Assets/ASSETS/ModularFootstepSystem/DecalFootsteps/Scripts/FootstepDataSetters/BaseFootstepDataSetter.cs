namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    /// <summary>
    /// Basic implementation of a data installer in a step effects creator.
    /// </summary>
    /// <typeparam name="T">Footstep data type.</typeparam>
    public abstract class BaseFootstepDataSetter<T> : AbstractFootstepDataSetter where T : AbstractFootstepData
    {
        [SerializeField]
        protected List<T> footstepData = new List<T>();

        protected T currentData = default;  

        protected string currentSurfaceType = string.Empty;

        /// <summary>
        /// Sets the list of data used by the creator.
        /// </summary>
        /// <param name="newFootstepData">The set of data used.</param>
        public virtual void SetStepsData(List<T> newFootstepData)
        {
            footstepData.Clear();

            if(newFootstepData != null && newFootstepData.Count > 0) 
            {
                footstepData.AddRange(newFootstepData);
            }

            SetSurfaceData(currentSurfaceType);
        }

        public override void SetSurfaceData(string surfaceId)
        {
            currentSurfaceType = surfaceId;
            currentData = footstepData != null ? footstepData.FirstOrDefault(data => data.SurfaceType.Id == surfaceId) : null;
            UpdateData();
        }
    }
}