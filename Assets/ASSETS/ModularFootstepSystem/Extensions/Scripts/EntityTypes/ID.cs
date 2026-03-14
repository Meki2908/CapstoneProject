namespace ModularFootstepSystem.Extensions
{
    using UnityEngine;

    /// <summary>
    /// SO text ID.
    /// </summary>
    [CreateAssetMenu(fileName = nameof(ID), menuName = "ModularFootstepSystem/Extensions/" + nameof(ID))]
    public class ID : ScriptableObject
    {
        /// <summary>
        /// Text ID.
        /// </summary>
        public virtual string Id => id;

        [SerializeField]
        protected string id = string.Empty;
    }
}