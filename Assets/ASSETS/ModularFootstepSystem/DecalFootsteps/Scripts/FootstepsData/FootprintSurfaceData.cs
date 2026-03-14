namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Footprints attached to the surface type.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(FootprintSurfaceData), menuName = "ModularFootstepSystem/" + nameof(FootprintSurfaceData))]
    public class FootprintSurfaceData : AbstractFootstepData
    {
        /// <summary>
        /// List of footprint decal settings.
        /// </summary>
        public virtual IReadOnlyList<FootprintDecalSettings> FootprintSettings => footprintSettings;

        [SerializeField]
        protected List<FootprintDecalSettings> footprintSettings = new List<FootprintDecalSettings>();
    }
}