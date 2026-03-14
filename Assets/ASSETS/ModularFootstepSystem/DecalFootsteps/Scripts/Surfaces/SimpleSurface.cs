namespace ModularFootstepSystem
{
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Simple(uniform) surface.
    /// </summary>
    public class SimpleSurface : AbstractSurface
    {
        [SerializeField]
        protected SurfaceType surfaceType = default;

        public override string GetSurfaceType(Vector3 worldPosition) => surfaceType.Id;
    }
}