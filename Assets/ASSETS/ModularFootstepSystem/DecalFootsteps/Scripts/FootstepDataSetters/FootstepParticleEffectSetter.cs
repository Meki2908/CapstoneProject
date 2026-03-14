namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Data setter in <see cref="FootstepParticleEffectCreator"/>.
    /// </summary>
    public class FootstepParticleEffectSetter : BaseFootstepDataSetter<FootstepParticleEffectData>
    {
        [SerializeField]
        protected FootstepParticleEffectCreator footstepParticleEffectCreator = default;

        [SerializeField, Min(1)]
        protected int effectsPoolDefaultCapacity = 10;

        [SerializeField, Min(10)]
        protected int effectsPoolMaxCapacity = 20;

        protected Dictionary<ParticleSystem, ExtendedSharpPool> effectsMap = new Dictionary<ParticleSystem, ExtendedSharpPool>();

        protected ParticleSystem currentEffect = default;

        public override void UpdateData()
        {
            if (currentData != null)
            {
                currentEffect = currentData.StepEffect[Random.Range(0, currentData.StepEffect.Count)];

                if (!effectsMap.ContainsKey(currentEffect))
                {
                    effectsMap[currentEffect] = new ExtendedSharpPool(currentEffect.gameObject, effectsPoolDefaultCapacity, effectsPoolMaxCapacity, true);
                }

                footstepParticleEffectCreator.FootstepEffectPool = effectsMap[currentEffect];
            }
            else
            {
                footstepParticleEffectCreator.FootstepEffectPool = null;
            }
        }
    }
}