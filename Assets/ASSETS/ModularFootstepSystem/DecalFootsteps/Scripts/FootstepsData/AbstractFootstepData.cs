namespace ModularFootstepSystem
{
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Abstraction of steps data attached to a surface type.
    /// </summary>
    public abstract class AbstractFootstepData : ScriptableObject 
    {
        /// <summary>
        /// Type of surface to which data is assigned.
        /// </summary>
        public SurfaceType SurfaceType => surfaceType;

        [SerializeField]
        protected SurfaceType surfaceType = default;
    }
}