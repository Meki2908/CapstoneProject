namespace ModularFootstepSystem
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using Extensions;

    /// <summary>
    /// Foot Handler. 
    /// Responsible for calling the logic for creating effects and storing the current surface type
    /// </summary>
    [RequireComponent(typeof(GroundDetectorUnderfoot))]
    public class FootHandler : MonoBehaviour
    {
        /// <summary>
        /// Surface type has changed.
        /// </summary>
        public event Action onSurfaceTypeChanged = delegate { };

        /// <summary>
        /// The leaving effects state has changed.
        /// </summary>
        public event Action onLeaveEffectsStateChanged = delegate { };

        /// <summary>
        /// Unique identifier for each foot.
        /// </summary>
        public virtual string FootType => footType.Id;

        /// <summary>
        /// Current type of surface underfoot.
        /// </summary>
        public virtual string CurrentSurfaceType
        {
            get => currentSurfaceType;

            set
            {
                if(currentSurfaceType != value)
                {
                    currentSurfaceType = value;
                    onSurfaceTypeChanged();
                }
            }
        }

        /// <summary>
        /// Is it necessary to leave an effect?
        /// </summary>
        public virtual bool IsLeavesEffects
        {
            get => isLeavesEffects;

            set
            {
                if (isLeavesEffects != value)
                {
                    isLeavesEffects = value;
                    onLeaveEffectsStateChanged();
                }
            }
        }

        /// <summary>
        /// Surface detector under the foot. 
        /// </summary>
        public virtual GroundDetectorUnderfoot GroundDetectorUnderfoot => groundDetectorUnderfoot;

        [SerializeField]
        protected FootType footType = default;

        [SerializeField]
        protected List<AbstractFootstepEffectCreator> footstepCreators = new List<AbstractFootstepEffectCreator>();
        [SerializeField]
        protected List<AbstractFootstepDataSetter> footstepDataSetters = new List<AbstractFootstepDataSetter>();

        protected GroundDetectorUnderfoot groundDetectorUnderfoot = default;

        protected string currentSurfaceType = string.Empty;

        protected bool isLeavesEffects = false;

        protected virtual void Awake()
        {
            groundDetectorUnderfoot = GetComponent<GroundDetectorUnderfoot>();

            foreach (AbstractFootstepEffectCreator creator in footstepCreators)
            {
                creator.Initialize(this);
            }
        }

        /// <summary>
        /// Starts creating effects if necessary.
        /// </summary>
        /// <remarks>
        /// Triggers a ground check and determines whether to leave effects 
        /// if the distance from the foot to the ground is within the acceptable range.
        /// Next, the surface type is checked. 
        /// If it has changed, then the data for the current surface is updated, 
        /// otherwise the data for the new surface is installed.
        /// After which the creation of effects of leaving footsteps on the surface begins.
        /// </remarks>
        public virtual void CreateFootstepEffects()
        {
            groundDetectorUnderfoot.DetectGround();

            isLeavesEffects = groundDetectorUnderfoot.IsGrounded;

            if (isLeavesEffects)
            {
                if (currentSurfaceType != groundDetectorUnderfoot.SurfaceType)
                {
                    currentSurfaceType = groundDetectorUnderfoot.SurfaceType;
                    footstepDataSetters.ForEach(setter => setter.SetSurfaceData(currentSurfaceType));
                }
                else
                {
                    footstepDataSetters.ForEach(setter => setter.UpdateData());
                }

                footstepCreators.ForEach(creator => creator.CreateEffect());
            }
        }
    }
}