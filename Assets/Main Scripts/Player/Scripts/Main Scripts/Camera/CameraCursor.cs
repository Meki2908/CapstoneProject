using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

namespace MovementSystem
{
    /// <summary>
    /// Phát hiện input phím Alt và báo cáo lên <see cref="MouseLockManager"/>.
    /// Quản lý enable/disable Cinemachine Look + Zoom theo trạng thái cursor.
    ///
    /// KHÔNG tự set Cursor.visible / Cursor.lockState — toàn bộ quyết định đó thuộc về MouseLockManager.
    /// Khi cursor hiện → Cinemachine tắt. Khi cursor ẩn → Cinemachine bật.
    /// </summary>
    public class CameraCursor : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference cameraToggleInputAction;
        [SerializeField]
        private bool startHidden;

#pragma warning disable CS0618 // Type is obsolete
        [SerializeField]
        private CinemachineInputProvider inputProvider;
#pragma warning restore CS0618

        [SerializeField]
        private CinemachineInputAxisController[] inputAxisControllers;

        [SerializeField]
        private bool disableCameraLookOnCursorVisible = true;
        [SerializeField]
        private bool disableCameraZoomOnCursorVisible = true;

        [Tooltip("If you're using Cinemachine 2.8.4 or earlier, untick this option.")]
        [SerializeField]
        private bool fixedCinemachineVersion;

        [Header("Input Actions (New Cinemachine/Input System)")]
        [SerializeField]
        private InputActionReference lookInputAction;
        [SerializeField]
        private InputActionReference zoomInputAction;

        // ── Runtime ──────────────────────────────────────────────────────

        // True khi Alt đang được giữ theo track nội bộ
        private bool _isAltHeld = false;

        // Track xem InputAction callback đã đăng ký chưa (tránh double)
        private bool _actionsRegistered = false;

        private PlayerInput _cachedPlayerInput;
        private InputAction _resolvedLookAction;
        private InputAction _resolvedZoomAction;

        // ── Lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            string sName = SceneManager.GetActiveScene().name;
            if (sName != "UI_Game" && sName != "Menu_Game" && sName != "DemoSceneSettings")
            {
                // MouseLockManager xử lý cursor — chúng ta chỉ sync Cinemachine
                SetCinemachineInput(true);
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            GameSettings.OnSettingsChanged += ApplyCameraSpeedSettings;
            ResolveCinemachineInputs();
            // Tắt SuppressInputWhileBlending ngay từ đầu (cả entries từ Inspector serialization)
            DisableSuppressInputWhileBlendingOnControllers();
            RegisterAltCallbacks();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSettings.OnSettingsChanged -= ApplyCameraSpeedSettings;
            UnregisterAltCallbacks();
        }

        private void RegisterAltCallbacks()
        {
            if (_actionsRegistered) return;
            if (cameraToggleInputAction != null && cameraToggleInputAction.action != null)
            {
                cameraToggleInputAction.action.started  += OnAltPressed;
                cameraToggleInputAction.action.canceled += OnAltReleased;
                _actionsRegistered = true;
            }
        }

        private void UnregisterAltCallbacks()
        {
            if (!_actionsRegistered) return;
            if (cameraToggleInputAction != null && cameraToggleInputAction.action != null)
            {
                cameraToggleInputAction.action.started  -= OnAltPressed;
                cameraToggleInputAction.action.canceled -= OnAltReleased;
            }
            _actionsRegistered = false;
        }

        private void OnEnable()
        {
            if (cameraToggleInputAction != null && cameraToggleInputAction.asset != null)
                cameraToggleInputAction.asset.Enable();
        }

        private void OnDisable()
        {
            if (cameraToggleInputAction != null && cameraToggleInputAction.asset != null)
                cameraToggleInputAction.asset.Disable();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ResolveCinemachineInputs();
            _isAltHeld = false; // Reset hold state khi chuyển scene

            string sName = scene.name;
            if (sName == "UI_Game" || sName == "Menu_Game" || sName == "DemoSceneSettings")
                return;

            // Sync Cinemachine với trạng thái MouseLockManager (apply sau 1 frame)
            StartCoroutine(DelayedSyncCinemachine());
        }

        private System.Collections.IEnumerator DelayedSyncCinemachine()
        {
            yield return null;
            // MouseLockManager đã apply cursor state — sync Cinemachine
            bool locked = MouseLockManager.Instance == null || MouseLockManager.Instance.IsGameplayCursorLocked;
            SetCinemachineInput(locked);
        }

        private int _debugLogFrame = 0;

        private void Update()
        {
            // Luôn sync Cinemachine với trạng thái MouseLockManager hiện tại
            // (để không bị lệch nếu MouseLockManager thay đổi state từ nguồn khác)
            if (MouseLockManager.Instance != null)
            {
                bool shouldCameraBeActive = MouseLockManager.Instance.IsGameplayCursorLocked;
                SyncCinemachineIfNeeded(shouldCameraBeActive);
            }

            // === DEBUG: Log trạng thái mỗi 90 frame ===
            #if UNITY_EDITOR
            _debugLogFrame++;
            if (_debugLogFrame >= 90)
            {
                _debugLogFrame = 0;
                var mlm = MouseLockManager.Instance;
                string ctrlStatus = "[]";
                if (inputAxisControllers != null && inputAxisControllers.Length > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    for (int i = 0; i < inputAxisControllers.Length; i++)
                    {
                        var c = inputAxisControllers[i];
                        sb.Append(c == null ? "[NULL]" : $"[{c.gameObject.name}:{c.enabled}]");
                    }
                    ctrlStatus = sb.ToString();
                }
                Debug.Log($"[CameraCursor] IsGameplayCursorLocked={mlm?.IsGameplayCursorLocked} | " +
                    $"Cursor.visible={Cursor.visible} | _lastCinemachineEnabled={_lastCinemachineEnabled} | " +
                    $"Controllers={ctrlStatus}");
            }
            #endif

            // Fallback legacy Alt input (khi InputAction không fire)
            // Chỉ dùng fallback nếu InputAction chưa được setup
            if (cameraToggleInputAction == null || cameraToggleInputAction.action == null)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.LeftAlt)) OnAltPressedLegacy();
                    else if (Input.GetKeyUp(KeyCode.LeftAlt)) OnAltReleasedLegacy();
                }
                catch { }
            }
        }


        // ── Alt Input Callbacks ──────────────────────────────────────────

        private void OnAltPressed(InputAction.CallbackContext context)
        {
            // UI overlay đang mở → không xử lý
            if (CursorUIPriority.IsUiOverlayActive) return;
            ShowCursorForAlt();
        }

        private void OnAltReleased(InputAction.CallbackContext context)
        {
            HideCursorAfterAlt();
        }

        private void OnAltPressedLegacy()
        {
            if (_isAltHeld) return;
            if (CursorUIPriority.IsUiOverlayActive) return;
            ShowCursorForAlt();
        }

        private void OnAltReleasedLegacy()
        {
            if (!_isAltHeld) return;
            HideCursorAfterAlt();
        }

        // ── Alt Show / Hide ───────────────────────────────────────────────

        private void ShowCursorForAlt()
        {
            _isAltHeld = true;
            if (MouseLockManager.Instance != null)
                MouseLockManager.Instance.NotifyAltHeld(true);
            // Cinemachine sẽ được sync tự động trong Update()
        }

        private void HideCursorAfterAlt()
        {
            _isAltHeld = false;
            if (MouseLockManager.Instance != null)
                MouseLockManager.Instance.NotifyAltHeld(false);
        }

        // ── Cinemachine Control ──────────────────────────────────────────

        private bool _lastCinemachineEnabled = true;

        /// <summary>
        /// Sync Cinemachine với trạng thái cursor mới nhất.
        /// Hướng disable: chỉ gọi khi state thay đổi (tránh spam).
        /// Hướng enable: luôn apply để đảm bảo controller không bị tắt bởi nguồn ngoài (force-active).
        /// </summary>
        private void SyncCinemachineIfNeeded(bool enabled)
        {
            if (!enabled)
            {
                // Chỉ disable khi state thay đổi
                if (_lastCinemachineEnabled)
                    SetCinemachineInput(false);
            }
            else
            {
                // Luôn bảo đảm enabled — phát hiện controller bị tắt bởi nguồn khác và force-enable lại
                if (!_lastCinemachineEnabled)
                    SetCinemachineInput(true);
                else
                    ForceEnsureAxisControllersEnabled();
            }
        }

        /// <summary>
        /// Enable/Disable Cinemachine input (Look + Zoom).
        /// Gọi khi cursor state thay đổi.
        /// </summary>
        public void SetCinemachineInput(bool enableInput)
        {
            _lastCinemachineEnabled = enableInput;
            ResolveCinemachineInputs();

            bool allowLook = enableInput || !disableCameraLookOnCursorVisible;
            bool allowZoom = enableInput || !disableCameraZoomOnCursorVisible;

            // New Cinemachine path
            SetInputActionEnabled(_resolvedLookAction, allowLook);
            SetInputActionEnabled(_resolvedZoomAction, allowZoom);

            if (inputAxisControllers != null && inputAxisControllers.Length > 0)
            {
                bool shouldEnableControllers = allowLook || allowZoom;
                for (int i = 0; i < inputAxisControllers.Length; i++)
                    if (inputAxisControllers[i] != null)
                        inputAxisControllers[i].enabled = shouldEnableControllers;
            }

            // Legacy Cinemachine fallback
            if (inputProvider == null)
                inputProvider = FindFirstObjectByType<CinemachineInputProvider>();

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
                if (!allowLook) inputProvider.XYAxis.action?.Disable();
                if (!allowZoom) inputProvider.ZAxis.action?.Disable();
            }
        }

        // ── Static API (gọi từ CursorUIPriority + HUD scripts) ───────────

        /// <summary>
        /// Sau khi đóng UI cuối cùng — re-sync Cinemachine với gameplay state.
        /// </summary>
        public static void ApplyGameplayCursorAfterUiClosed()
        {
            var cc = FindFirstObjectByType<CameraCursor>();
            if (cc != null)
                cc.SetCinemachineInput(true); // Gameplay: camera active
        }

        /// <summary>
        /// HUD canvas vừa được bật — thông báo MouseLockManager (HUD).
        /// MouseLockManager sẽ tự hiện cursor và lock Cinemachine.
        /// </summary>
        public static void ApplyFreeCursorForHudCanvasActivated()
        {
            // Delegate hoàn toàn cho MouseLockManager
            if (MouseLockManager.Instance != null)
                MouseLockManager.Instance.NotifyHudVisibilityChanged(true);
        }

        // ── Settings & Helpers ────────────────────────────────────────────

        private void ApplyCameraSpeedSettings()
        {
            if (inputProvider == null)
                inputProvider = FindFirstObjectByType<CinemachineInputProvider>();
            if (inputProvider == null) return;

            var gs = GameSettings.Instance;
            if (gs == null) return;

            Debug.Log($"[CameraCursor] Camera speed setting: MouseSpeed={gs.cameraMouseSpeed:F2} (not yet applied)");
        }

        /// <summary>
        /// Force-enable tất cả CinemachineInputAxisController hiện tại nếu bị tắt bởi nguồn ngoài.
        /// Được gọi mỗi frame trong gameplay để chống lại bất kỳ code nào vô tình tắt controller.
        /// </summary>
        private void ForceEnsureAxisControllersEnabled()
        {
            if (inputAxisControllers == null) return;
            bool needRefresh = false;
            for (int i = 0; i < inputAxisControllers.Length; i++)
            {
                if (inputAxisControllers[i] == null) { needRefresh = true; continue; }
                if (!inputAxisControllers[i].enabled)
                    inputAxisControllers[i].enabled = true;
            }
            // Nếu phát hiện null entry (scene mới load) → refresh array và enable ngay
            if (needRefresh)
            {
                inputAxisControllers = FindObjectsByType<CinemachineInputAxisController>(FindObjectsSortMode.None);
                DisableSuppressInputWhileBlendingOnControllers();
                for (int i = 0; i < inputAxisControllers.Length; i++)
                    if (inputAxisControllers[i] != null)
                        inputAxisControllers[i].enabled = true;
            }
        }

        /// <summary>
        /// Tắt SuppressInputWhileBlending trên tất cả CinemachineInputAxisController.
        ///
        /// ROOT CAUSE FIX:
        /// MouseLockManager dùng "soft cursor phase": IsGameplayCursorLocked=true
        /// nhưng Cursor.visible=true (transparent cursor chờ click đầu tiên).
        /// CinemachineInputAxisController.SuppressInputWhileBlending đọc Cursor.visible
        /// TRỰC TIẾP (không qua MouseLockManager) → suppress input dù gameplay đang chạy.
        ///
        /// Bằng cách tắt SuppressInputWhileBlending, CinemachineInputAxisController
        /// không tự quản lý enabled state nữa — CameraCursor là nguồn duy nhất
        /// bật/tắt controller qua SetCinemachineInput() dựa trên IsGameplayCursorLocked.
        /// </summary>
        private void DisableSuppressInputWhileBlendingOnControllers()
        {
            if (inputAxisControllers == null) return;
            for (int i = 0; i < inputAxisControllers.Length; i++)
            {
                if (inputAxisControllers[i] == null) continue;
                if (inputAxisControllers[i].SuppressInputWhileBlending)
                {
                    inputAxisControllers[i].SuppressInputWhileBlending = false;
                    Debug.Log($"[CameraCursor] SuppressInputWhileBlending disabled on '{inputAxisControllers[i].gameObject.name}' (soft-cursor compat fix).");
                }
            }
        }


        private void ResolveCinemachineInputs()
        {
            // Refresh nếu array null, rỗng, HOẶC có entry bị destroy (scene cũ unload)
            bool needRefresh = inputAxisControllers == null || inputAxisControllers.Length == 0;
            if (!needRefresh)
            {
                for (int i = 0; i < inputAxisControllers.Length; i++)
                {
                    if (inputAxisControllers[i] == null) { needRefresh = true; break; }
                }
            }
            if (needRefresh)
            {
                inputAxisControllers = FindObjectsByType<CinemachineInputAxisController>(FindObjectsSortMode.None);
                DisableSuppressInputWhileBlendingOnControllers();
            }

            if (_cachedPlayerInput == null)
                _cachedPlayerInput = FindFirstObjectByType<PlayerInput>();

            _resolvedLookAction = ResolveAction(lookInputAction, "Look");
            _resolvedZoomAction = ResolveAction(zoomInputAction, "CameraZoom");
        }

        private InputAction ResolveAction(InputActionReference directReference, string fallbackActionName)
        {
            if (directReference != null && directReference.action != null)
                return directReference.action;
            if (_cachedPlayerInput != null && _cachedPlayerInput.actions != null)
                return _cachedPlayerInput.actions.FindAction(fallbackActionName, false);
            return null;
        }

        private void SetInputActionEnabled(InputAction action, bool enable)
        {
            if (action == null) return;
            if (enable) action.Enable(); else action.Disable();
        }
    }
}