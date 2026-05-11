using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Fusion;

namespace Artsystack.ArtsystackGui
{
    /// <summary>
    /// Quản lý menu tạm dừng game (Pause Menu)
    /// GUI: Continue, Save Game, Setting, Exit
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        [Header("Pause Panel")]
        [SerializeField] private GameObject panel_PopUpPause;

        [Header("HUD Panel (HP, Inventory,...)")]
        [SerializeField] private GameObject panel_HUD;

        [Header("Pause Buttons - Theo ten trong Hierarchy")]
        [SerializeField] private Button btn_Continue;       // Panel_PopUp_Pause > Btn_Continue
        [SerializeField] private Button btn_SaveGame;      // Panel_PopUp_Pause > Btn_Save Game
        [SerializeField] private Button btn_Setting;       // Panel_PopUp_Pause > Btn_Setting
        [SerializeField] private Button btn_Exit;          // Panel_PopUp_Pause > Btn_Exit

        [Header("Settings Panel")]
        [SerializeField] private GameObject panel_GUISettings;

        [Header("Scene Recognition")]
        [Tooltip("Danh sách các scene được xem là Main Menu. Ở những scene này, PauseMenuManager sẽ KHÔNG bắt phím ESC.")]
        [SerializeField] private List<string> mainMenuSceneNames = new List<string> { "DemoSceneSettings", "Menu_Game", "UI_Game" };

        [Header("Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool cursorVisibleOnPause = true;

        [Tooltip("Chặn TogglePause bị gọi 2 lần cùng frame (Input System + Legacy, hoặc script khác) → Esc một lần mở pause ngay.")]
        [SerializeField] private float togglePauseDebounceSeconds = 0.2f;

        private static PauseMenuManager instance;
        private bool isPaused = false;
        private float _lastTogglePauseUnscaledTime = -999f;
        private int _lastTogglePauseFrame = -1;
        private PlayerInput _cachedPlayerInput;

        public static PauseMenuManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<PauseMenuManager>();
                return instance;
            }
        }

        private void Awake()
        {
            Debug.Log($"[PauseMenuManager] Awake() on {gameObject.name}, instance={instance}, this={this.GetInstanceID()}");
            if (instance != null && instance != this)
            {
                Debug.LogWarning($"[PauseMenuManager] Duplicate detected! Disabling on {gameObject.name}");
                this.enabled = false;
                return;
            }
            instance = this;

            Debug.Log($"[PauseMenuManager] panel_PopUpPause={(panel_PopUpPause != null ? panel_PopUpPause.name : "NULL")}");
            Debug.Log($"[PauseMenuManager] panel_HUD={(panel_HUD != null ? panel_HUD.name : "NULL")}");

            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(false);

            SceneManager.sceneLoaded += OnSceneLoaded_ClearPlayerInputCache;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded_ClearPlayerInputCache;
        }

        private void OnSceneLoaded_ClearPlayerInputCache(Scene scene, LoadSceneMode mode)
        {
            _cachedPlayerInput = null;
        }



        private void Start()
        {
            SetupButtonListeners();

            // Bật HUD panel + tất cả children (bị tắt mặc định trong Inspector)
            if (panel_HUD != null)
            {
                panel_HUD.SetActive(true);
                foreach (Transform child in panel_HUD.transform)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }

        private void Update()
        {
            // Kiểm tra tên scene hiện tại
            string currentSceneName = SceneManager.GetActiveScene().name;
            
            // Trong lobby (Main Menu scenes), ESC thuộc về GameMenuManager — skip ở đây
            if (mainMenuSceneNames.Contains(currentSceneName))
                return;

            bool escPressed = false;

            // Ưu tiên action Player/OpenMenu (cùng binding với Esc) — một nguồn, tránh lệch frame giữa Input System và Legacy.
            // Khi đang pause, SetPlayerInput tắt map Player → dùng Keyboard/Legacy bên dưới để đóng pause.
            if (_cachedPlayerInput == null)
                _cachedPlayerInput = FindFirstObjectByType<PlayerInput>();
            if (_cachedPlayerInput != null && _cachedPlayerInput.actions != null)
            {
                var openMenu = _cachedPlayerInput.actions.FindAction("OpenMenu", false);
                if (openMenu != null && openMenu.enabled && openMenu.WasPressedThisFrame())
                    escPressed = true;
            }

            if (!escPressed && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                escPressed = true;

            if (!escPressed)
            {
                try
                {
                    if (Input.GetKeyDown(KeyCode.Escape))
                        escPressed = true;
                }
                catch { }
            }

            if (escPressed)
            {
                if (Time.unscaledTime - _lastTogglePauseUnscaledTime < togglePauseDebounceSeconds)
                    return;
                _lastTogglePauseUnscaledTime = Time.unscaledTime;
                Debug.Log($"[PauseMenuManager] ESC detected! isPaused={isPaused}, enabled={enabled}");
                TogglePause();
            }
        }

        private void SetupButtonListeners()
        {
            if (btn_Continue != null)
                btn_Continue.onClick.AddListener(ResumeGame);

            if (btn_SaveGame != null)
                btn_SaveGame.onClick.AddListener(SaveGame);

            if (btn_Setting != null)
                btn_Setting.onClick.AddListener(OpenSettings);

            if (btn_Exit != null)
                btn_Exit.onClick.AddListener(ExitToMainMenu);
        }

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            Time.timeScale = 0f;

            Debug.Log($"[PauseMenuManager] PauseGame() — panel_PopUpPause={(panel_PopUpPause != null ? panel_PopUpPause.name : "NULL")}");

            if (panel_PopUpPause != null)
            {
                // CRITICAL: Kích hoạt tất cả parent trước khi bật panel
                EnsureParentsActive(panel_PopUpPause.transform);
                panel_PopUpPause.SetActive(true);

                // Fix: Đảm bảo Animator chạy được khi Time.timeScale = 0
                Animator[] anims = panel_PopUpPause.GetComponentsInChildren<Animator>(true);
                foreach (var anim in anims)
                {
                    anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                }

                // Kiểm tra Canvas parent
                Canvas parentCanvas = panel_PopUpPause.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    if (!parentCanvas.enabled) parentCanvas.enabled = true;
                    Debug.Log($"[PauseMenuManager] Parent Canvas: {parentCanvas.gameObject.name}, enabled={parentCanvas.enabled}, renderMode={parentCanvas.renderMode}, activeInHierarchy={parentCanvas.gameObject.activeInHierarchy}");
                }
                else
                {
                    Debug.LogError("[PauseMenuManager] NO PARENT CANVAS FOUND! Panel won't render!");
                }
                
                SoundManager.PlayUIOpenMenu();
                Debug.Log($"[PauseMenuManager] panel_PopUpPause SET ACTIVE = true, activeSelf={panel_PopUpPause.activeSelf}, activeInHierarchy={panel_PopUpPause.activeInHierarchy}");
            }
            else
            {
                Debug.LogError("[PauseMenuManager] panel_PopUpPause is NULL! GUI cannot show!");
            }

            // Ẩn HUD khi pause
            if (panel_HUD != null)
                panel_HUD.SetActive(false);

            // Tắt PlayerInput để UI buttons nhận được click
            SetPlayerInput(false);

            // CursorUIPriority.BeginUiOverlay() abaixo já notifica o MouseLockManager
            GameCursorManager.TryApplyNormalCursorTextureFromScene();

            // Đồng bộ với CameraCursor / UI — tránh trạng thái cursor/input lệch
            CursorUIPriority.BeginUiOverlay();

            // Giống Alt “mở chuột”: dừng retry ép lock sau load scene (MouseLockManager), tránh đè cùng frame với pause.
            if (MouseLockManager.Instance != null)
                MouseLockManager.Instance.ClearGameplayLockRetries();
        }

        public void ResumeGame()
        {
            // Cho phép resume khi panel vẫn bật nhưng isPaused bị lệch (Continue / script khác đã đụng UI).
            bool panelVisible = panel_PopUpPause != null && panel_PopUpPause.activeSelf;
            if (!isPaused && !panelVisible)
                return;

            isPaused = false;
            SoundManager.PlayUICloseMenu();
            HideAllPanels();

            // Tắt lại các parent đã bật khi pause
            RestoreParents();

            Time.timeScale = 1f;
            // Cursor: CursorUIPriority.EndUiOverlay() bên dưới đã thông báo MouseLockManager

            // Bật lại PlayerInput khi resume
            SetPlayerInput(true);

            CursorUIPriority.EndUiOverlay();

            // Hiện lại HUD khi resume
            if (panel_HUD != null)
                panel_HUD.SetActive(true);
        }

        private void SetPlayerInput(bool enabled)
        {
            var characters = FindObjectsByType<Character>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                var character = characters[i];
                if (character == null || !character.isActiveAndEnabled)
                    continue;

                var pi = character.playerInput != null ? character.playerInput : character.GetComponent<PlayerInput>();
                if (pi == null || pi.actions == null)
                    continue;

                if (enabled)
                    pi.enabled = true;

                var playerMap = pi.actions.FindActionMap("Player");
                if (playerMap != null)
                {
                    if (enabled) playerMap.Enable();
                    else playerMap.Disable();
                }

                var skillMap = pi.actions.FindActionMap("Skill");
                if (skillMap != null)
                {
                    if (enabled) skillMap.Enable();
                    else skillMap.Disable();
                }
            }
        }

        public void TogglePause()
        {
            // Chặn mọi nguồn gọi TogglePause 2 lần trong cùng frame (tránh mở rồi đóng ngay).
            if (Time.frameCount == _lastTogglePauseFrame)
                return;
            _lastTogglePauseFrame = Time.frameCount;

            if (isPaused) ResumeGame();
            else PauseGame();
        }

        private void SaveGame()
        {
            Debug.Log("Save Game clicked");
        }

        private void OpenSettings()
        {
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(false);

            // Gọi SettingsManager để khởi tạo đúng (tab, load settings)
            var settingsManager = FindFirstObjectByType<SettingsManager>();
            if (settingsManager != null)
            {
                settingsManager.OpenSettings();
                SoundManager.PlayUIOpenMenu();
            }
            else if (panel_GUISettings != null)
            {
                panel_GUISettings.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            if (panel_GUISettings != null)
                panel_GUISettings.SetActive(false);
            SoundManager.PlayUICloseMenu();
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(true);
        }

        private void ExitToMainMenu()
        {
            // Cursor: CursorUIPriority.EndAllUiOverlays() trong DungeonWaveManager.ReturnToMainMap() hoặc
            // MouseLockManager.OnSceneLoaded() sẽ xử lý khi chuyển scene
            Time.timeScale = 1f;
            GameCursorManager.TryApplyNormalCursorTextureFromScene();
            isPaused = false;
            HideAllPanels();

            try
            {
                CursorUIPriority.EndAllUiOverlays();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PauseMenuManager] EndAllUiOverlays: {ex.Message}");
            }

            // In dungeon: use DungeonWaveManager to return properly
            // (cleans up reward panel, restores player UI, goes to Map_Chinh; Fusion shutdown handled there)
            var dwm = FindFirstObjectByType<DungeonWaveManager>();
            if (dwm != null)
            {
                SetPlayerInput(true); // Re-enable player input before transition
                dwm.ReturnToMainMap();
                return;
            }

            TryShutdownFusionBeforeMainMenu();

            try
            {
                if (SceneTransitionManager.Instance != null)
                    SceneTransitionManager.Instance.GoToScene(mainMenuSceneName, "Returning to menu...");
                else
                    SceneManager.LoadScene(mainMenuSceneName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PauseMenuManager] GoToScene failed for '{mainMenuSceneName}': {ex.Message}");
                try
                {
                    SceneManager.LoadScene(mainMenuSceneName);
                }
                catch (Exception ex2)
                {
                    Debug.LogError($"[PauseMenuManager] Fallback LoadScene failed: {ex2.Message}");
                }
            }
        }

        /// <summary>
        /// Leave Fusion session before loading UI_Game so host gets <see cref="INetworkRunnerCallbacks.OnPlayerLeft"/> and can despawn the avatar.
        /// Safe: try/catch, singleton resolve, fallback <see cref="NetworkRunner"/> if manager reference is stale.
        /// </summary>
        static void TryShutdownFusionBeforeMainMenu()
        {
            try
            {
                if (FusionConnectionManager.TryResolveInstance())
                {
                    var cm = FusionConnectionManager.Instance;
                    var runner = cm != null ? cm.Runner : null;
                    if (runner != null && runner.IsRunning)
                    {
                        bool destroyChild = runner.gameObject != cm.gameObject;
                        Debug.Log($"[PauseMenuManager] Shutdown Fusion before main menu (destroyChild={destroyChild}, mode={runner.GameMode}).");
                        runner.Shutdown(destroyChild, ShutdownReason.Ok);
                        return;
                    }
                }

                var orphan = UnityEngine.Object.FindFirstObjectByType<NetworkRunner>();
                if (orphan != null && orphan.IsRunning)
                {
                    Debug.LogWarning("[PauseMenuManager] FusionConnectionManager missing/stale — shutting down orphan NetworkRunner.");
                    orphan.Shutdown(false, ShutdownReason.Ok);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PauseMenuManager] Fusion Shutdown (primary) failed: {ex.Message}");
                try
                {
                    var r = UnityEngine.Object.FindFirstObjectByType<NetworkRunner>();
                    if (r != null && r.IsRunning)
                        r.Shutdown(false, ShutdownReason.Ok);
                }
                catch (Exception ex2)
                {
                    Debug.LogWarning($"[PauseMenuManager] Fusion Shutdown (fallback) failed: {ex2.Message}");
                }
            }
        }

        private void HideAllPanels()
        {
            if (panel_PopUpPause != null) panel_PopUpPause.SetActive(false);
            if (panel_GUISettings != null) panel_GUISettings.SetActive(false);
        }

        // Lưu danh sách các parent đã bật khi pause → tắt lại khi resume
        private readonly List<GameObject> activatedParents = new List<GameObject>();

        /// <summary>
        /// Kích hoạt tất cả parent inactive từ panel lên đến Canvas
        /// (parent có thể bị ẩn bởi GameMenuManager.HideAllPanels)
        /// </summary>
        private void EnsureParentsActive(Transform child)
        {
            activatedParents.Clear();
            Transform current = child.parent;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    Debug.Log($"[PauseMenuManager] Activating inactive parent: {current.gameObject.name}");
                    current.gameObject.SetActive(true);
                    activatedParents.Add(current.gameObject);
                }
                // Dừng khi tới Canvas root
                if (current.GetComponent<Canvas>() != null)
                    break;
                current = current.parent;
            }
        }

        /// <summary>
        /// Tắt lại các parent đã bật khi pause
        /// </summary>
        private void RestoreParents()
        {
            foreach (var go in activatedParents)
            {
                if (go != null)
                {
                    go.SetActive(false);
                    Debug.Log($"[PauseMenuManager] Deactivating parent: {go.name}");
                }
            }
            activatedParents.Clear();
        }

        public bool IsPaused => isPaused;
    }
}
