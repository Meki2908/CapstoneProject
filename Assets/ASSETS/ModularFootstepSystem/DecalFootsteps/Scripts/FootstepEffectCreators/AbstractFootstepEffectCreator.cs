namespace ModularFootstepSystem
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Abstraction of footstep effect creator.
    /// </summary>
    public abstract class AbstractFootstepEffectCreator : MonoBehaviour
    {
        /// <summary>
        /// The leaving effects state has changed.
        /// </summary>
        public event Action onLeaveEffectStateChanged = delegate { };

        /// <summary>
        /// Need to create effects.
        /// </summary>
        public bool IsLeaveEffect
        {
            get => isLeaveEffect;

            set
            {
                if(isLeaveEffect != value)
                {
                    isLeaveEffect = value;
                    onLeaveEffectStateChanged();
                }
            }
        }

        protected FootHandler footHandler = default;

        protected bool isLeaveEffect = true;

        /// <summary>
        /// Initialize footstep effect creator.
        /// </summary>
        /// <param name="_footHandler">Footstep handler for the current leg.</param>
        public virtual void Initialize(FootHandler _footHandler) => footHandler = _footHandler;

        /// <summary>
        /// Creates an effect when stepping with foot.
        /// </summary>
        public abstract void CreateEffect();
    }
}