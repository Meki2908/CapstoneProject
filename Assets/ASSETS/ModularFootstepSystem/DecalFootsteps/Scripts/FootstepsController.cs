namespace ModularFootstepSystem
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// A controller that processes the process of executing steps. 
    /// Calls the handler of the foot on which the step occurred.
    /// </summary>
    public class FootstepsController : MonoBehaviour
    {
        [SerializeField]
        protected FootstepsConductor footstepsConductor = default;

        [SerializeField]
        protected List<FootHandler> footstepStartHandlers = new List<FootHandler>();
        [SerializeField]
        protected List<FootHandler> footstepMiddleHandlers = new List<FootHandler>();
        [SerializeField]
        protected List<FootHandler> footstepEndHandlers = new List<FootHandler>();

        protected virtual void OnEnable() 
        {
            footstepsConductor.onFootstepStart += OnFootstepStart;
            footstepsConductor.onFootstepMiddle += OnFootstepMiddle;
            footstepsConductor.onFootstepEnd += OnFootstepEnd;
        }

        protected virtual void OnDisable()
        {
            footstepsConductor.onFootstepStart -= OnFootstepStart;
            footstepsConductor.onFootstepMiddle -= OnFootstepMiddle;
            footstepsConductor.onFootstepEnd -= OnFootstepEnd;
        }

        /// <summary>
        /// Causes a step to be executed at the beginning of the step execution process.
        /// </summary>
        /// <param name="footType"></param>
        protected virtual void OnFootstepStart(string footType) => TakeFootstep(footType, footstepStartHandlers);

        /// <summary>
        /// Causes a step to be executed in the middle of a step's execution.
        /// </summary>
        /// <param name="footType"></param>
        protected virtual void OnFootstepMiddle(string footType) => TakeFootstep(footType, footstepMiddleHandlers);

        /// <summary>
        /// Causes a step to be executed at the end of the step execution process.
        /// </summary>
        /// <param name="footType"></param>
        protected virtual void OnFootstepEnd(string footType) => TakeFootstep(footType, footstepEndHandlers);

        /// <summary>
        /// Calls the handler of the foot on which the step occurred
        /// </summary>
        /// <param name="footType">The type of foot that took the step.</param>
        /// <param name="handlers">List of foot handlers, depending on the step execution process.</param>
        protected virtual void TakeFootstep(string footType, List<FootHandler> handlers)
        {
            foreach (FootHandler footHandler in handlers)
            {
                if(footHandler.FootType == footType)
                {
                    footHandler.CreateFootstepEffects();
                }
            }
        }
    }
}