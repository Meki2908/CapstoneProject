using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace MovementSystem
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 12f)]
        private float defaultDistance = 6f;
        [SerializeField]
        [Range(0f, 12f)]
        private float minimumDistance = 1f;
        [SerializeField]
        [Range(0f, 12f)]
        private float maximumDistance = 6f;
        [SerializeField]
        [Range(0f, 20f)]
        private float smoothing = 4f;
        [SerializeField]
        [Range(0f, 20f)]
        private float zoomSensitivity = 1f;
        [SerializeField]
        private InputActionReference zoomInputAction;
        private CinemachinePositionComposer positionComposer;
        private float currentTargetDistance;
        private void Awake()
        {
            var virtualCamera = GetComponent<CinemachineCamera>();
            if (virtualCamera != null)
            {
                // Fix: Use the non-generic overload and cast the result
                positionComposer = virtualCamera.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
            }

            currentTargetDistance = defaultDistance;
        }

        private void OnEnable()
        {
            if (zoomInputAction != null)
            {
                zoomInputAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (zoomInputAction != null)
            {
                zoomInputAction.action.Disable();
            }
        }

        private void Update()
        {
            Zoom();
        }

        private void Zoom()
        {
            if (positionComposer != null && zoomInputAction != null)
            {
                float zoomValue = zoomInputAction.action.ReadValue<float>() * zoomSensitivity;
                currentTargetDistance = Mathf.Clamp(currentTargetDistance + zoomValue, minimumDistance, maximumDistance);
                float currentDistance = positionComposer.CameraDistance;
                if (Mathf.Approximately(currentDistance, currentTargetDistance))
                {
                    return;
                }

                float lerpedZoomValue = Mathf.Lerp(currentDistance, currentTargetDistance, smoothing * Time.deltaTime);
                positionComposer.CameraDistance = lerpedZoomValue;
            }
        }
    }
}