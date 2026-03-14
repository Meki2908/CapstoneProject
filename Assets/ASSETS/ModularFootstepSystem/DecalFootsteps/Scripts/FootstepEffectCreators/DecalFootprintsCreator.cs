namespace ModularFootstepSystem
{
    using UnityEngine;
    using UnityEngine.Rendering.Universal;
    using Extensions;

    /// <summary>
    /// Creator of foot decals when stepping with a foot.
    /// </summary>
    /// <remarks>
    /// Leaves footprint at the point of contact between the foot and the surface.
    /// </remarks>
    public class DecalFootprintsCreator : AbstractFootstepEffectCreator
    {
        protected const float FOOTPRINT_ANGLE_X = 90f;
        protected const float FOOTPRINT_ANGLE_Z_MODIFIER = 180f;

        protected const string FOOTPRINTS_PARENT_OBJECT_NAME = "FootstepObjects";

        /// <summary>
        /// Settings for decals of marks left on the surface. Varies depending on surface.
        /// </summary>
        public virtual FootprintDecalSettings FootprintSettings
        {
            get => footprintSettings;

            set
            {
                if (footprintSettings != value)
                {
                    footprintSettings = value;
                }
            }
        }

        [SerializeField]
        protected ExtendedBehaviourPool footprintPool = default;

        protected FootprintDecalSettings footprintSettings = default;

        protected DecalProjector footprint = default;

        protected Quaternion footprintRotation = Quaternion.identity;

        protected Transform footprintsParent = default;

        public override void Initialize(FootHandler _footHandler)
        {
            base.Initialize(_footHandler);

            GameObject parentObject = GameObject.Find(FOOTPRINTS_PARENT_OBJECT_NAME);
            if(parentObject != null)
            {
                footprintsParent = parentObject.transform;
            }
            else
            {
                footprintsParent = new GameObject(FOOTPRINTS_PARENT_OBJECT_NAME).transform;
            }
        }

        public override void CreateEffect()
        {
            if (isLeaveEffect && footprintSettings != null)
            {
                footprint = footprintPool.Get.GetComponent<DecalProjector>();
                InitializeEffect();
                SetPosition(footprint.transform);
            }
        }

        /// <summary>
        /// Initializes footprint decal settings.
        /// </summary>
        protected virtual void InitializeEffect()
        {
            footprint.pivot = footprintSettings.Pivot;
            footprint.size = footprintSettings.Size;
            footprint.material = footprintSettings.Material;
        }

        /// <summary>
        /// Sets the position and rotation of a footprint on a surface.
        /// </summary>
        /// <param name="footprintTransform">Footprint decal transform.</param>
        protected virtual void SetPosition(Transform footprintTransform)
        {
            footprintTransform.SetParent(footprintsParent, true);
            footprintTransform.position = footHandler.GroundDetectorUnderfoot.GroundHit.point;

            footprintRotation = Quaternion.FromToRotation(footHandler.GroundDetectorUnderfoot.FootTranform.up, footHandler.GroundDetectorUnderfoot.GroundHit.normal);
            footprintTransform.rotation = footprintRotation * footHandler.GroundDetectorUnderfoot.FootTranform.rotation;

            footprintTransform.eulerAngles = new Vector3(footprintTransform.eulerAngles.x + FOOTPRINT_ANGLE_X, footprintTransform.eulerAngles.y, footprintTransform.eulerAngles.z + FOOTPRINT_ANGLE_Z_MODIFIER);
        }
    }
}