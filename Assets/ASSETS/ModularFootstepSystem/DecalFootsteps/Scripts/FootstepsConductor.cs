namespace ModularFootstepSystem
{
    using System;
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Conductor of the process of stepping with feet.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class FootstepsConductor : MonoBehaviour
    {
        /// <summary>
        /// Footstep process has begun.
        /// </summary>
        public event Action<string> onFootstepStart = delegate { };

        /// <summary>
        /// Footstep process in middle of progress.
        /// </summary>
        public event Action<string> onFootstepMiddle = delegate { };

        /// <summary>
        /// Footstep process has ended.
        /// </summary>
        public event Action<string> onFootstepEnd = delegate { };

        /// <summary>
        /// Notifies about the start of the footstep process.
        /// </summary>
        /// <param name="footstepId"></param>
        public virtual void FootstepStart(FootType footstepId) => onFootstepStart(footstepId.Id);

        /// <summary>
        /// Notifies about the middle of your footstep progress
        /// </summary>
        /// <param name="footstepId"></param>
        public virtual void FootstepMiddle(FootType footstepId) => onFootstepMiddle(footstepId.Id);

        /// <summary>
        /// Notifies about the end of the footstep process.
        /// </summary>
        /// <param name="footstepId"></param>
        public virtual void FootstepEnd(FootType footstepId) => onFootstepEnd(footstepId.Id);
    }
}