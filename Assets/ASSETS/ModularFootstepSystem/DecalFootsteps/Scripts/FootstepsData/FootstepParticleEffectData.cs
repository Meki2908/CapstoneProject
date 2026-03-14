namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Footstep particle effects attached to the surface type.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(FootstepParticleEffectData), menuName = "ModularFootstepSystem/" + nameof(FootstepParticleEffectData))]
    public class FootstepParticleEffectData : AbstractFootstepData
    {
        /// <summary>
        /// List of prefabs with <see cref="ParticleSystem"/>.
        /// </summary>
        public virtual IReadOnlyList<ParticleSystem> StepEffect => stepEffect;

        [SerializeField]
        protected List<ParticleSystem> stepEffect = new List<ParticleSystem>();
    }
}