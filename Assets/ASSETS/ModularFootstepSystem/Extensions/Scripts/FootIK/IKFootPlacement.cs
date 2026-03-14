namespace ModularFootstepSystem.Extensions
{
    using UnityEngine;

    /// <summary>
    /// Simple implementation of inverse kinematics for foots.
    /// </summary>
    public class IKFootPlacement : MonoBehaviour
    {
        [SerializeField]
        protected Animator animator = default;

        [SerializeField, Range(0, 1f)]
        protected float distanceToGround = 0.25f;

        [SerializeField]
        protected LayerMask layerMask = default;

        [SerializeField]
        protected string leftFootWeightParameterName = "IKLeftFootWeight";
        [SerializeField]
        protected string rightFootWeightParameterName = "IKRightFootWeight";

        protected Ray ray = default;

        protected RaycastHit hit = default;

        protected Vector3 footPosition = default;

        protected void OnAnimatorIK(int layerIndex)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(leftFootWeightParameterName));
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(leftFootWeightParameterName));
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animator.GetFloat(rightFootWeightParameterName));
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat(rightFootWeightParameterName));

            SetFootIK(AvatarIKGoal.LeftFoot);
            SetFootIK(AvatarIKGoal.RightFoot);
        }

        /// <summary>
        /// Sets the position of the foot.
        /// </summary>
        /// <param name="foot">Changeable foot.</param>
        protected virtual void SetFootIK(AvatarIKGoal foot)
        {
            ray = new Ray(animator.GetIKPosition(foot) + Vector3.up, Vector3.down);
            if (Physics.Raycast(ray, out hit, distanceToGround + 1f, layerMask))
            {
                    footPosition = hit.point;
                    footPosition.y += distanceToGround;
                    animator.SetIKPosition(foot, footPosition);
                    animator.SetIKRotation(foot, Quaternion.LookRotation(transform.forward, hit.normal));
            }
        }
    }
}