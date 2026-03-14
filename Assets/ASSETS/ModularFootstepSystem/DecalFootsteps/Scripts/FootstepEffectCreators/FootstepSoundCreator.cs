namespace ModularFootstepSystem
{
    using UnityEngine;

    /// <summary>
    /// Creator of sounds when stepping with a foot.
    /// </summary>
    /// <remarks>
    /// Starts playing a step sound.
    /// </remarks>
    public class FootstepSoundCreator : AbstractFootstepEffectCreator
    {
        /// <summary>
        /// Sound when step the surface. Changes with each step and the surface changes.
        /// </summary>
        public virtual AudioClip FootstepAudio
        {
            get => footstepAudio;

            set
            {
                if (footstepAudio != value)
                {
                    footstepAudio = value;
                }
            }
        }

        [SerializeField]
        protected AudioSource audioSource = default;

        protected AudioClip footstepAudio = default;

        public override void CreateEffect()
        {
            if (isLeaveEffect && footstepAudio != null)
            {
                audioSource.PlayOneShot(footstepAudio);
            }
        }
    }
}