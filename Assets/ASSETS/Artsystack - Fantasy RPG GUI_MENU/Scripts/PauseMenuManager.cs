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

        [Header("Input System")]
        [SerializeField] private InputActionReference openMenuAction;

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
            if (instance != null && instance != this)
            {
                this.enabled = false; // Chỉ tắt component, không xóa
                return;
            }
            instance = this;

            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(false);

            // Subscribe to OpenMenu action
            if (openMenuAction != null && openMenuAction.action != null)
            {
                openMenuAction.action.Enable();
                openMenuAction.action.performed += OnOpenMenuPerformed;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (openMenuAction != null && openMenuAction.action != null)
            {
                openMenuAction.action.performed -= OnOpenMenuPerformed;
            }
        }

        private void OnOpenMenuPerformed(InputAction.CallbackContext context)
        {
            TogglePause();
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

            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(true);

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
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void HideAllPanels()
        {
            if (panel_PopUpPause != null) panel_PopUpPause.SetActive(false);
            if (panel_GUISettings != null) panel_GUISettings.SetActive(false);
        }

        public bool IsPaused => isPaused;
    }
}
