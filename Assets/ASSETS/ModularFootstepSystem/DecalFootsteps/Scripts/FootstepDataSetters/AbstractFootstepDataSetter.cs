namespace ModularFootstepSystem
{
    using UnityEngine;

    /// <summary>
    /// Abstraction of the data setter into the step effect creator.
    /// </summary>
    public abstract class AbstractFootstepDataSetter : MonoBehaviour
    {
        /// <summary>
        /// Sets data by surface type.
        /// </summary>
        /// <param name="surfaceId">Surface ID.</param>
        public abstract void SetSurfaceData(string surfaceId);

        /// <summary>
        /// Updates the data used by the creator.
        /// </summary>
        public abstract void UpdateData();
    }
}