namespace ModularFootstepSystem
{
    using UnityEngine;

    /// <summary>
    /// Data setter in <see cref="FootstepSoundCreator"/>.
    /// </summary>
    public class FootstepSoundSetter : BaseFootstepDataSetter<FootstepSoundData>
    {
        [SerializeField]
        protected FootstepSoundCreator footstepSoundCreator = default;

        public override void UpdateData()
        {
            footstepSoundCreator.FootstepAudio = currentData == null ?
                null :
                currentData.StepSounds[Random.Range(0, currentData.StepSounds.Count)];
        }
    }
}