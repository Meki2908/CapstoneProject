namespace ModularFootstepSystem
{
    using UnityEngine;

    /// <summary>
    /// Data setter in <see cref="DecalFootprintsCreator"/>.
    /// </summary>
    public class FootprintDataSetter : BaseFootstepDataSetter<FootprintSurfaceData>
    {
        [SerializeField]
        protected DecalFootprintsCreator footprintsCreator = default;

        public override void UpdateData()
        {
            footprintsCreator.FootprintSettings = currentData == null ?
                null :
                currentData.FootprintSettings[Random.Range(0, currentData.FootprintSettings.Count)];
        }
    }
}