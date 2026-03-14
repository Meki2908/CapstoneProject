namespace ModularFootstepSystem.Demo
{
    using UnityEngine;
    using ModularFootstepSystem.Extensions;
    using ModularFootstepSystem;

    /// <summary>
    /// Player movement type provider.
    /// </summary>
    /// <remarks>
    /// Demo implementation of setting the type of player movement. 
    /// Used to change the effects of footsteps when speeding up. 
    /// It can also be used, for example, 
    /// to change the effects of steps when a player lands on different surfaces 
    /// or for other types of movement you need.
    /// </remarks>
    public class PlayerMovementTypeProvider : MonoBehaviour
    {
        [SerializeField]
        protected Animator animator = default;

        [SerializeField]
        protected string animatorStateName = string.Empty;

        [SerializeField]
        protected FootstepsStateController footstepsStateController = default;

        [SerializeField]
        protected FootstepStateType footstepStateType = default;

        protected bool inState = false;
        protected bool tempStateValue = false;

        private void Update()
        {
            inState = animator.GetCurrentAnimatorStateInfo(0).IsName(animatorStateName);

            if (inState != tempStateValue)
            {
                if (inState)
                {
                    footstepsStateController.SetState(footstepStateType);
                }

                tempStateValue = inState;
            }
        }
    }
}