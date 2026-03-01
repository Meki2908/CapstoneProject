using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        [Header("Pause Buttons - Theo ten trong Hierarchy")]
        [SerializeField] private Button btn_Continue;       // Panel_PopUp_Pause > Btn_Continue
        [SerializeField] private Button btn_SaveGame;      // Panel_PopUp_Pause > Btn_Save Game
        [SerializeField] private Button btn_Setting;       // Panel_PopUp_Pause > Btn_Setting
        [SerializeField] private Button btn_Exit;          // Panel_PopUp_Pause > Btn_Exit

        [Header("Settings Panel")]
        [SerializeField] private GameObject panel_GUISettings;
        [SerializeField] private Button button_Close;      // Panel_GUISetting > Button_Close

        [Header("Exit Panel")]
        [SerializeField] private GameObject panel_Exit;
        [SerializeField] private Button button_ConfirmExit;
        [SerializeField] private Button button_CancelExit;

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
                    instance = FindObjectOfType<PauseMenuManager>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(false);
        }

        private void Start()
        {
            SetupButtonListeners();
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
                btn_Exit.onClick.AddListener(ShowExitConfirm);

            if (button_Close != null)
                button_Close.onClick.AddListener(CloseSettings);

            if (button_ConfirmExit != null)
                button_ConfirmExit.onClick.AddListener(ConfirmExit);

            if (button_CancelExit != null)
                button_CancelExit.onClick.AddListener(CancelExit);
        }

        public void PauseGame()
        {
            if (isPaused) return;
            isPaused = true;
            Time.timeScale = 0f;

            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(true);

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
            if (panel_GUISettings != null)
                panel_GUISettings.SetActive(true);
        }

        private void CloseSettings()
        {
            if (panel_GUISettings != null)
                panel_GUISettings.SetActive(false);
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(true);
        }

        private void ShowExitConfirm()
        {
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(false);
            if (panel_Exit != null)
                panel_Exit.SetActive(true);
        }

        private void ConfirmExit()
        {
            Time.timeScale = 1f;
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private void CancelExit()
        {
            if (panel_Exit != null)
                panel_Exit.SetActive(false);
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(true);
        }

        private void HideAllPanels()
        {
            if (panel_PopUpPause != null) panel_PopUpPause.SetActive(false);
            if (panel_GUISettings != null) panel_GUISettings.SetActive(false);
            if (panel_Exit != null) panel_Exit.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                TogglePause();
        }

        public bool IsPaused => isPaused;
    }
}
