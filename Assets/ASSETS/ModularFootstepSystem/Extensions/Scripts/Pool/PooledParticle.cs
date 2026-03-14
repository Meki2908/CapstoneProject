namespace ModularFootstepSystem.Extensions
{
    using UnityEngine;

    /// <summary>
    /// Pooled object with <see cref="ParticleSystem"/>.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledParticle : PooledObject
    {
        protected virtual void OnParticleSystemStopped()
            => Release();
    }
}