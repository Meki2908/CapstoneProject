namespace ModularFootstepSystem.Extensions
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Extended pool interface.
    /// </summary>
    public interface IExtendedPool 
    {
        /// <summary>
        /// Returns a pool object.
        /// </summary>
        GameObject Get { get; }

        /// <summary>
        /// Returns the active pool objects.
        /// </summary>
        IReadOnlyList<GameObject> ActiveObjects { get; }

        /// <summary>
        /// Returns a pool object as a component.
        /// </summary>
        /// <typeparam name="T">Component type.</typeparam>
        /// <param name="pooledObject">Pool object.</param>
        /// <returns>Component has been received.</returns>
        bool TryGet<T>(out T pooledObject) where T : Behaviour;

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        /// <param name="releasedObject">Released object.</param>
        void Release(GameObject releasedObject);
    }
}