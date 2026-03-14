namespace ModularFootstepSystem
{
    using UnityEngine;

    /// <summary>
    /// Abstraction of the surface on which the effects of steps are reproduced.
    /// </summary>
    public abstract class AbstractSurface : MonoBehaviour
    {
        /// <summary>
        /// Return the surface type.
        /// </summary>
        /// <param name="worldPosition">World position at which contact with the surface occurred.</param>
        /// <returns>Surface type as a text string.</returns>
        public abstract string GetSurfaceType(Vector3 worldPosition);
    }
}