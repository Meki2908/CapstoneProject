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
        private CinemachineInputAxisController[] inputAxisControllers;
        [SerializeField]
        private bool disableCameraLookOnCursorVisible;
        [SerializeField]
        private bool disableCameraZoomOnCursorVisible;
        [Tooltip("If you're using Cinemachine 2.8.4 or earlier, untick this option.\\nIf unticked, both Look and Zoom will be disabled.")]
        [SerializeField]
        private bool fixedCinemachineVersion;
        [Header("Input Actions (New Cinemachine/Input System)")]
        [SerializeField]
        private InputActionReference lookInputAction;
        [SerializeField]
        private InputActionReference zoomInputAction;

        // Track cursor state internally to avoid conflicts
        private bool isCursorHidden = false;
        private InventoryController cachedInventory; // Cache reference
        private PlayerInput cachedPlayerInput;
        private InputAction resolvedLookAction;
        private InputAction resolvedZoomAction;

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

            ResolveCinemachineInputs();
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
            ResolveCinemachineInputs();

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
            // Không lock cursor nếu inventory đang mở
            if (IsInventoryOpen()) return;

            isCursorHidden = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SetCinemachineInput(true);
        }

        private void Update()
        {
            // KHÔNG xử lý cursor khi inventory đang mở
            if (IsInventoryOpen()) return;

            // Chỉ dùng legacy Input khi KHÔNG có InputAction gán
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
            // KHÔNG toggle cursor khi inventory đang mở
            if (IsInventoryOpen()) return;

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
            ResolveCinemachineInputs();

            bool allowLook = enableInput || !disableCameraLookOnCursorVisible;
            bool allowZoom = enableInput || !disableCameraZoomOnCursorVisible;

            // New Cinemachine path: use Input System actions to disable look/zoom independently.
            SetInputActionEnabled(resolvedLookAction, allowLook);
            SetInputActionEnabled(resolvedZoomAction, allowZoom);

            // New Cinemachine controllers: if both look and zoom should be disabled, disable controllers.
            if (inputAxisControllers != null && inputAxisControllers.Length > 0)
            {
                bool shouldEnableControllers = allowLook || allowZoom;
                for (int i = 0; i < inputAxisControllers.Length; i++)
                {
                    if (inputAxisControllers[i] != null)
                        inputAxisControllers[i].enabled = shouldEnableControllers;
                }
            }

            // Legacy Cinemachine fallback
            if (inputProvider == null)
            {
                inputProvider = FindFirstObjectByType<CinemachineInputProvider>();
            }

            if (inputProvider == null)
            {
                if (enableInput) ApplyCameraSpeedSettings();
                return;
            }

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
                if (!allowLook)
                {
                    inputProvider.XYAxis.action?.Disable();
                }
                if (!allowZoom)
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
            if (inputProvider == null)
            {
                // New Cinemachine input-axis controller path currently does not expose a single "gain" API here.
                return;
            }

            var gs = GameSettings.Instance;
            if (gs == null) return;

            // TODO: Migrate to InputAxisController to support Gain (camera sensitivity)
            // CinemachineInputProvider (deprecated) does not support Gain on InputActionReference
            Debug.Log($"[CameraCursor] Camera speed setting: MouseSpeed={gs.cameraMouseSpeed:F2} (not yet applied)");
        }

        /// <summary>
        /// Kiểm tra inventory đang mở (cache reference để tránh Find mỗi frame)
        /// </summary>
        private bool IsInventoryOpen()
        {
            if (cachedInventory == null)
                cachedInventory = FindFirstObjectByType<InventoryController>();
            return cachedInventory != null && cachedInventory.isInventoryOpen;
        }

        private void ResolveCinemachineInputs()
        {
            if (inputAxisControllers == null || inputAxisControllers.Length == 0)
            {
                inputAxisControllers = FindObjectsByType<CinemachineInputAxisController>(FindObjectsSortMode.None);
            }

            if (cachedPlayerInput == null)
                cachedPlayerInput = FindFirstObjectByType<PlayerInput>();

            resolvedLookAction = ResolveAction(lookInputAction, "Look");
            resolvedZoomAction = ResolveAction(zoomInputAction, "CameraZoom");
        }

        private InputAction ResolveAction(InputActionReference directReference, string fallbackActionName)
        {
            if (directReference != null && directReference.action != null)
                return directReference.action;

            if (cachedPlayerInput != null && cachedPlayerInput.actions != null)
                return cachedPlayerInput.actions.FindAction(fallbackActionName, false);

            return null;
        }

        private void SetInputActionEnabled(InputAction action, bool enable)
        {
            if (action == null) return;

            if (enable)
                action.Enable();
            else
                action.Disable();
        }
    }
}