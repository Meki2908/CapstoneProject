namespace ModularFootstepSystem
{
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Creator of particle effects when stepping with a foot.
    /// </summary>
    /// <remarks>
    /// Leaves particle effects at the point of contact between the foot and the surface.
    /// </remarks>
    public class FootstepParticleEffectCreator : AbstractFootstepEffectCreator
    {
        protected const string FOOTSTEP_EFFECTS_PARENT_OBJECT_NAME = "FootstepObjects";

        /// <summary>
        /// Pool with particle effects. Varies depending on surface.
        /// </summary>
        public virtual ExtendedSharpPool FootstepEffectPool
        {
            get => footstepEffectPool;

            set
            {
                if (footstepEffectPool != value)
                {
                    footstepEffectPool = value;
                }
            }
        }

        protected ExtendedSharpPool footstepEffectPool = default;

        protected ParticleSystem footstepEffect = default;

        protected Quaternion footstepEffectRotation = Quaternion.identity;

        protected Transform footstepEffectsParent = default;

        public override void Initialize(FootHandler _footHandler)
        {
            base.Initialize(_footHandler);

            GameObject parentObject = GameObject.Find(FOOTSTEP_EFFECTS_PARENT_OBJECT_NAME);
            if (parentObject != null)
            {
                footstepEffectsParent = parentObject.transform;
            }
            else
            {
                footstepEffectsParent = new GameObject(FOOTSTEP_EFFECTS_PARENT_OBJECT_NAME).transform;
            }
        }

        public override void CreateEffect()
        {
            if (isLeaveEffect && footstepEffectPool != null)
            {
                footstepEffect = footstepEffectPool.Get.GetComponent<ParticleSystem>();
                SetPosition(footstepEffect.transform);
                footstepEffect.time = 0f;
                footstepEffect.Play();
            }
        }

        /// <summary>
        /// Sets the position and rotation of a footstep particle effect on a surface.
        /// </summary>
        /// <param name="footstepEffectTransform">Particle effect transform.</param>
        protected virtual void SetPosition(Transform footstepEffectTransform)
        {
            footstepEffectTransform.SetParent(footstepEffectsParent, true);
            footstepEffectTransform.position = footHandler.GroundDetectorUnderfoot.GroundHit.point;

            footstepEffectRotation = Quaternion.FromToRotation(footHandler.GroundDetectorUnderfoot.FootTranform.up, footHandler.GroundDetectorUnderfoot.GroundHit.normal);
            footstepEffectTransform.rotation = footstepEffectRotation * footHandler.GroundDetectorUnderfoot.FootTranform.rotation;
        }
    }
}