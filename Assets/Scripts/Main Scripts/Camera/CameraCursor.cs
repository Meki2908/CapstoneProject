using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

#pragma warning disable CS0618 // CinemachineInputProvider is obsolete

namespace MovementSystem
{
    public class CameraCursor : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference cameraToggleInputAction;
        [SerializeField]
        private bool startHidden;
        [SerializeField]
        private CinemachineInputProvider inputProvider; // Corrected type reference
        [SerializeField]
        private bool disableCameraLookOnCursorVisible;
        [SerializeField]
        private bool disableCameraZoomOnCursorVisible;
        [Tooltip("If you're using Cinemachine 2.8.4 or earlier, untick this option.\\nIf unticked, both Look and Zoom will be disabled.")]
        [SerializeField]
        private bool fixedCinemachineVersion;
        private void Awake()
        {
            if (cameraToggleInputAction != null)
            {
                cameraToggleInputAction.action.started += OnCameraCursorToggled;
            }

            if (startHidden)
            {
                ToggleCursor();
            }
        }

        private void OnEnable()
        {
            if (cameraToggleInputAction != null && cameraToggleInputAction.asset != null)
            {
                cameraToggleInputAction.asset.Enable();
            }
        }

        private void OnDisable()
        {
            if (cameraToggleInputAction != null && cameraToggleInputAction.asset != null)
            {
                cameraToggleInputAction.asset.Disable();
            }
        }

        private void OnCameraCursorToggled(InputAction.CallbackContext context)
        {
            ToggleCursor();
        }

        private void ToggleCursor()
        {
            Cursor.visible = !Cursor.visible;
            if (!Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                if (!fixedCinemachineVersion)
                {
                    if (inputProvider != null)
                        inputProvider.enabled = true;
                    return;
                }

                if (inputProvider != null)
                {
                    inputProvider.XYAxis.action?.Enable();
                    inputProvider.ZAxis.action?.Enable();
                }

                return;
            }

            Cursor.lockState = CursorLockMode.None;
            if (!fixedCinemachineVersion)
            {
                if (inputProvider != null)
                    inputProvider.enabled = false;
                return;
            }

            if (inputProvider != null)
            {
                if (disableCameraLookOnCursorVisible)
                {
                    inputProvider.XYAxis.action?.Disable();
                }

                if (disableCameraZoomOnCursorVisible)
                {
                    inputProvider.ZAxis.action?.Disable();
                }
            }
        }
    }
}
#pragma warning restore CS0618