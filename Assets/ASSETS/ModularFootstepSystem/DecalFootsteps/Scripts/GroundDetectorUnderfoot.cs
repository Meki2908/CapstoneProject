using System;

namespace ModularFootstepSystem
{
    using UnityEngine;

    /// <summary>
    /// Surface detector under the foot. 
    /// Provides surface type and point of contact between foot and ground.
    /// </summary>
    public class GroundDetectorUnderfoot : MonoBehaviour
    {
        protected const float MIN_DETECTING_DISTANCE_TO_GROUND = 0.01f;
        
        /// <summary>
        /// Type of surface underfoot.
        /// </summary>
        public virtual string SurfaceType => surfaceType;

        /// <summary>
        /// Component "Transform" of the foot.
        /// </summary>
        public virtual Transform FootTranform => footTranform;

        /// <summary>
        /// A point on the surface of the earth under the foot.
        /// </summary>
        public virtual RaycastHit GroundHit => hit;

        /// <summary>
        /// Foot on the ground?
        /// </summary>
        public virtual bool IsGrounded => isGrounded;

        [SerializeField]
        protected Transform footTranform = default;

        [SerializeField, Min(MIN_DETECTING_DISTANCE_TO_GROUND)]
        protected float detectingDistanceToGround = 0.4f;
        
        [SerializeField]
        protected Vector3 footPositionShift = Vector3.zero;

        [SerializeField] protected LayerMask detectingLayers = 1 << 0;
        
        protected Vector3 positionOfGroundUnderfoot = Vector3.zero;
        protected Vector3 stepDirection = Vector3.zero;
        protected Vector3 shiftedFootPosition = Vector3.zero;

        protected RaycastHit hit = default;

        protected string surfaceType = string.Empty;

        protected AbstractSurface surface = default;

        protected bool isGrounded = false;

        /// <summary>
        /// Find the ground under your foot.
        /// </summary>
        /// <remarks>
        /// Performs a raycast perpendicular to the ground. 
        /// If the surface contains the "AbstractSurface" component, 
        /// then the foot is considered to be on the surface.
        /// A point on the surface from the raycast is copied, 
        /// the surface type and a flag that the step was successful
        /// </remarks>
        public virtual void DetectGround()
        {
            if (Physics.Raycast(footTranform.TransformPoint(footPositionShift), Vector3.down, out hit, detectingDistanceToGround, detectingLayers))
            {
                if(hit.transform.TryGetComponent(out surface))
                {
                    positionOfGroundUnderfoot = hit.point;
                    surfaceType = surface.GetSurfaceType(positionOfGroundUnderfoot);
                    isGrounded = surfaceType != null;
                }
                else
                {
                    isGrounded = false;
                }
            }
            else
            {
                isGrounded = false;
            }
        }

#region Editor_Logic
#if UNITY_EDITOR
        [SerializeField] protected bool drawGizmos = true;

        [SerializeField] protected float footMarkSize = 0.05f;
        
        protected virtual void OnDrawGizmosSelected()
        {
            if (!footTranform || !drawGizmos)
            {
                return;
            }

            shiftedFootPosition = footTranform.TransformPoint(footPositionShift);
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(shiftedFootPosition, footMarkSize);
            
            Gizmos.color = Color.blue;
            Vector3 drawTo = shiftedFootPosition;
            drawTo.y -= detectingDistanceToGround;
            Gizmos.DrawLine(shiftedFootPosition, drawTo);
        }
#endif
#endregion
    }
}