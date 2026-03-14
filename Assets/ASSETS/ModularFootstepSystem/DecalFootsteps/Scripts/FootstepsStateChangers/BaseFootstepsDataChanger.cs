namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Abstraction of the basic implementation of the footstep data changer.
    /// </summary>
    /// <typeparam name="SetterType">Footstep data setter type.</typeparam>
    /// <typeparam name="DataType">Setting the footstep data type.</typeparam>
    public abstract class BaseFootstepsDataChanger<SetterType, DataType> : AbstractFootstepsDataChanger
        where SetterType : BaseFootstepDataSetter<DataType>
        where DataType : AbstractFootstepData
    {
        [SerializeField]
        protected SetterType dataSetter = default;

        [SerializeField]
        protected List<SerializableFootstepStateData<DataType>> stateData = new List<SerializableFootstepStateData<DataType>>();

        protected SerializableFootstepStateData<DataType> currentStateData = default;

        /// <summary>
        /// Receives new data on the footstep state type 
        /// and replaces the settings in FootstepDataSetter with new data.
        /// </summary>
        /// <param name="stateType">Footsteps state type.</param>
        public override void ChangeData(FootstepStateType stateType)
        {
            currentStateData = stateData.FirstOrDefault(data => data.FootstepStateType.Id == stateType.Id);
            dataSetter.SetStepsData(currentStateData != null ? currentStateData.Data.ToList() : null);
        }
    }
}