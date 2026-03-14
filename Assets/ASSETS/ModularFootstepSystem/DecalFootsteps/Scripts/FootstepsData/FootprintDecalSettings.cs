namespace ModularFootstepSystem
{
    using UnityEngine;

    /// <summary>
    /// Footprint decal settings.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(FootprintDecalSettings), menuName = "ModularFootstepSystem/" + nameof(FootprintDecalSettings))]
    public class FootprintDecalSettings : ScriptableObject
    {
        /// <summary>
        /// Decal pivot.
        /// </summary>
        public Vector3 Pivot => pivot;

        /// <summary>
        /// Decal size.
        /// </summary>
        public Vector3 Size => size;

        /// <summary>
        /// Footprint decal material. Contains an image of the footprint and a normal if available.
        /// </summary>
        public Material Material => material;

        [SerializeField]
        protected Vector3 pivot = Vector3.zero;
        [SerializeField]
        protected Vector3 size = Vector3.one;

        [SerializeField]
        protected Material material = default;
    }
}