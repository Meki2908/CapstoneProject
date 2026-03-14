namespace ModularFootstepSystem.Extensions
{
    using UnityEngine;

    /// <summary>
    /// SO <see cref="Terrain"/> surface text ID.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(TerrainSurfaceType), menuName = "ModularFootstepSystem/Extensions/" + nameof(TerrainSurfaceType))]
    public class TerrainSurfaceType : ScriptableObject
    {
        /// <summary>
        /// Type of surface to which terrain layer is assigned.
        /// </summary>
        public SurfaceType SurfaceType => surfaceType;

        /// <summary>
        /// The terrain layer to which the surface type is assigned.
        /// </summary>
        public virtual TerrainLayer TerrainLayer => terrainLayer;

        [SerializeField]
        protected SurfaceType surfaceType = default;

        [SerializeField]
        protected TerrainLayer terrainLayer = default;
    }
}