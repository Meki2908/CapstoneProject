namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Footstep sounds assigned to the surface type.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(FootstepSoundData), menuName = "ModularFootstepSystem/" + nameof(FootstepSoundData))]
    public class FootstepSoundData : AbstractFootstepData
    {
        /// <summary>
        /// List of footstep sounds.
        /// </summary>
        public virtual IReadOnlyList<AudioClip> StepSounds => stepSounds;

        [SerializeField]
        protected List<AudioClip> stepSounds = new List<AudioClip>();
    }
}