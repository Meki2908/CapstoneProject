using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

namespace MovementSystem
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 20f)]
        private float smoothing = 4f;
        [SerializeField]
        [Range(0f, 20f)]
        private float zoomSensitivity = 2f;
        [SerializeField]
        private InputActionReference zoomInputAction;
        private PlayerCameraDistanceManager distanceManager;
        private void Awake()
        {
            // Prefer the local player camera distance manager if available
            distanceManager = PlayerNetworkSetup.LocalCameraDistanceManager;
            if (distanceManager == null)
                distanceManager = GetComponentInParent<PlayerCameraDistanceManager>();
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
            if (distanceManager != null && zoomInputAction != null)
            {
                // Nhân thêm GameSettings zoom speed multiplier
                float settingsZoomMultiplier = GameSettings.Instance != null ? GameSettings.Instance.cameraZoomSpeed : 1f;
                float zoomValue = zoomInputAction.action.ReadValue<float>() * zoomSensitivity * settingsZoomMultiplier;
                distanceManager.AddUserTargetDelta(zoomValue);
            }
        }
    }
}