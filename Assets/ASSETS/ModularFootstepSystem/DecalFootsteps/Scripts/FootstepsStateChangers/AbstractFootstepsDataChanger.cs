namespace ModularFootstepSystem
{
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Abstraction of footstep data changer depending on footstep state type.
    /// </summary>
    public abstract class AbstractFootstepsDataChanger : MonoBehaviour
    {
        /// <summary>
        /// Changes footstep data by state type.
        /// </summary>
        /// <param name="stateType">Footsteps state type.</param>
        public abstract void ChangeData(FootstepStateType stateType);
    }
}