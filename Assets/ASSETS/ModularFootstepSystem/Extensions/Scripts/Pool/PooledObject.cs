namespace ModularFootstepSystem.Extensions
{
    using UnityEngine;

    /// <summary>
    /// Pooled object.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        /// <summary>
        /// Pool reference.
        /// </summary>
        public virtual IExtendedPool Pool => pool;

        protected IExtendedPool pool = default;

        /// <summary>
        /// Initializes a pool object.
        /// </summary>
        /// <param name="_pool">Pool reference.</param>
        public virtual void Initialize(IExtendedPool _pool)
            => pool = _pool;

        /// <summary>
        /// Release an object to the pool.
        /// </summary>
        public virtual void Release() => pool.Release(gameObject);
    }
}