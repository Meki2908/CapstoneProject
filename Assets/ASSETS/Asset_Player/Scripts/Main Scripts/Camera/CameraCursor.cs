using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

namespace MovementSystem
{
    public class CameraCursor : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference cameraToggleInputAction;
        [SerializeField]
        private bool startHidden;
        [SerializeField]
#pragma warning disable CS0618 // Type is obsolete - still functional, will migrate to InputAxisController later
        private CinemachineInputProvider inputProvider; // Corrected type reference
#pragma warning restore CS0618
        [SerializeField]
        private bool disableCameraLookOnCursorVisible;
        [SerializeField]
        private bool disableCameraZoomOnCursorVisible;
        [Tooltip("If you're using Cinemachine 2.8.4 or earlier, untick this option.\\nIf unticked, both Look and Zoom will be disabled.")]
        [SerializeField]
        private bool fixedCinemachineVersion;

        // Track cursor state internally to avoid conflicts
        private bool isCursorHidden = false;

        private void Awake()
        {
            if (cameraToggleInputAction != null)
            {
                cameraToggleInputAction.action.started += OnCameraCursorToggled;
            }

            if (startHidden)
            {
                ForceHideCursor();
            }

            // Đăng ký callback khi scene mới load xong → reset cursor state
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Listen GameSettings changes
            GameSettings.OnSettingsChanged += ApplyCameraSpeedSettings;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSettings.OnSettingsChanged -= ApplyCameraSpeedSettings;
            if (cameraToggleInputAction != null)
            {
                cameraToggleInputAction.action.started -= OnCameraCursorToggled;
            }
        }

        /// <summary>
        /// Khi scene mới load (bao gồm teleport vào dungeon) → reset cursor về trạng thái đúng
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (startHidden)
            {
                StartCoroutine(DelayedForceHideCursor());
            }
        }

        private System.Collections.IEnumerator DelayedForceHideCursor()
        {
            yield return null; // Đợi 1 frame
            ForceHideCursor();
        }

        /// <summary>
        /// Force ẩn cursor — dùng khi khởi tạo hoặc chuyển scene
        /// </summary>
        private void ForceHideCursor()
        {
            isCursorHidden = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SetCinemachineInput(true);
        }

        private void Update()
        {
            // Chỉ dùng legacy Input khi KHÔNG có InputAction gán
            // Nếu có InputAction (ví dụ Player/CameraToggle bound to ALT), nó đã xử lý qua OnCameraCursorToggled
            // Dùng cả hai sẽ bị double toggle → cursor không thay đổi
            if (cameraToggleInputAction == null && Input.GetKeyDown(KeyCode.LeftAlt))
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
            isCursorHidden = !isCursorHidden;

            if (isCursorHidden)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                SetCinemachineInput(true);
            }
            else
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                SetCinemachineInput(false);
            }
        }

        /// <summary>
        /// Enable/Disable Cinemachine input dựa trên trạng thái cursor
        /// Tự tìm inputProvider nếu reference bị null (sau scene transition)
        /// </summary>
        private void SetCinemachineInput(bool enableInput)
        {
            if (inputProvider == null)
            {
                inputProvider = FindFirstObjectByType<CinemachineInputProvider>();
            }

            if (inputProvider == null) return;

            if (!fixedCinemachineVersion)
            {
                inputProvider.enabled = enableInput;
                if (enableInput) ApplyCameraSpeedSettings();
                return;
            }

            if (enableInput)
            {
                inputProvider.XYAxis.action?.Enable();
                inputProvider.ZAxis.action?.Enable();
                ApplyCameraSpeedSettings();
            }
            else
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

        /// <summary>
        /// Apply GameSettings camera speed vào CinemachineInputProvider Gain
        /// </summary>
        private void ApplyCameraSpeedSettings()
        {
            if (inputProvider == null)
                inputProvider = FindFirstObjectByType<CinemachineInputProvider>();
            if (inputProvider == null) return;

            var gs = GameSettings.Instance;
            if (gs == null) return;

            // TODO: Migrate to InputAxisController to support Gain (camera sensitivity)
            // CinemachineInputProvider (deprecated) does not support Gain on InputActionReference
            Debug.Log($"[CameraCursor] Camera speed setting: MouseSpeed={gs.cameraMouseSpeed:F2} (not yet applied)");
        }
    }
}