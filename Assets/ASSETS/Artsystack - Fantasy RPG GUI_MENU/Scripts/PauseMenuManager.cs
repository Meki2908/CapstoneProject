using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

        [Header("Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private bool cursorVisibleOnPause = true;



        private static PauseMenuManager instance;
        private bool isPaused = false;

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
        }



        private void Start()
        {
            SetupButtonListeners();

            // Kiểm tra lobby mode: GameMenuManager enabled = lobby scene
            var gmm = FindFirstObjectByType<GameMenuManager>();
            lobbyModeActive = (gmm != null && gmm.enabled);
            if (lobbyModeActive)
                Debug.Log("[PauseMenuManager] Lobby mode — ESC handled by GameMenuManager");

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

        // Cache: GameMenuManager đang xử lý ESC trong lobby?
        private bool lobbyModeActive = false;

        private void Update()
        {
            // Trong lobby, GameMenuManager.Update() xử lý ESC
            if (lobbyModeActive)
                return;

            bool escPressed = false;

            // Check New Input System
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escPressed = true;
            }

            // Fallback: Legacy Input
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
                // (parent có thể bị ẩn bởi GameMenuManager.HideAllPanels)
                EnsureParentsActive(panel_PopUpPause.transform);
                panel_PopUpPause.SetActive(true);
                Debug.Log($"[PauseMenuManager] panel_PopUpPause SET ACTIVE = true, activeSelf={panel_PopUpPause.activeSelf}, activeInHierarchy={panel_PopUpPause.activeInHierarchy}");
                
                // Kiểm tra Canvas parent
                Canvas parentCanvas = panel_PopUpPause.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                    Debug.Log($"[PauseMenuManager] Parent Canvas: {parentCanvas.gameObject.name}, enabled={parentCanvas.enabled}, renderMode={parentCanvas.renderMode}, activeInHierarchy={parentCanvas.gameObject.activeInHierarchy}");
                else
                    Debug.LogError("[PauseMenuManager] NO PARENT CANVAS FOUND! Panel won't render!");
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

            if (cursorVisibleOnPause)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        public void ResumeGame()
        {
            if (!isPaused) return;
            isPaused = false;
            HideAllPanels();

            // Tắt lại các parent đã bật khi pause
            RestoreParents();

            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // Bật lại PlayerInput khi resume
            SetPlayerInput(true);

            // Hiện lại HUD khi resume
            if (panel_HUD != null)
                panel_HUD.SetActive(true);
        }

        private void SetPlayerInput(bool enabled)
        {
            var character = FindFirstObjectByType<Character>();
            if (character != null && character.playerInput != null && character.playerInput.actions != null)
            {
                // Tắt/bật Player action map (movement, combat, attack...)
                var playerMap = character.playerInput.actions.FindActionMap("Player");
                if (playerMap != null)
                {
                    if (enabled) playerMap.Enable();
                    else playerMap.Disable();
                }

                // Tắt/bật Skill action map
                var skillMap = character.playerInput.actions.FindActionMap("Skill");
                if (skillMap != null)
                {
                    if (enabled) skillMap.Enable();
                    else skillMap.Disable();
                }
            }
        }

        public void TogglePause()
        {
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
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(true);
        }

        private void ExitToMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            HideAllPanels(); // Ẩn pause/settings trước khi chuyển
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.GoToScene(mainMenuSceneName, "Đang quay về menu...");
            else
                SceneManager.LoadScene(mainMenuSceneName);
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
