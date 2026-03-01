using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Artsystack.ArtsystackGui
{
    /// <summary>
    /// Quản lý menu chính của game
    /// </summary>
    public class GameMenuManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject panel_GUIGame;
        [SerializeField] private GameObject panel_GUISettings;
        [SerializeField] private GameObject panel_Loading;
        [SerializeField] private GameObject panel_PopUpPause;
        [SerializeField] private GameObject panel_Exit;

        [Header("Main Menu Buttons")]
        [SerializeField] private UnityEngine.UI.Button btn_Play;
        [SerializeField] private UnityEngine.UI.Button btn_Help;
        [SerializeField] private UnityEngine.UI.Button btn_Settings;
        [SerializeField] private UnityEngine.UI.Button btn_Exit;

        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "Game";
        [SerializeField] private bool showCursorOnPlay = false;

        private bool isGameRunning = false;

        private void Start()
        {
            InitializeMenu();
        }

        private void InitializeMenu()
        {
            // Đảm bảo chỉ hiển thị panel chính
            HideAllPanels();
            
            if (panel_GUIGame != null)
                panel_GUIGame.SetActive(true);

            // Thiết lập event listeners cho main menu
            if (btn_Play != null)
                btn_Play.onClick.AddListener(OnPlayClicked);
            
            if (btn_Settings != null)
                btn_Settings.onClick.AddListener(OnSettingsClicked);
            
            if (btn_Exit != null)
                btn_Exit.onClick.AddListener(OnExitClicked);

            // Cursor settings
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void HideAllPanels()
        {
            if (panel_GUIGame != null) panel_GUIGame.SetActive(false);
            if (panel_GUISettings != null) panel_GUISettings.SetActive(false);
            if (panel_Loading != null) panel_Loading.SetActive(false);
            if (panel_PopUpPause != null) panel_PopUpPause.SetActive(false);
            if (panel_Exit != null) panel_Exit.SetActive(false);
        }

        #region Main Menu Events

        /// <summary>
        /// Khi bấm nút Play - Bắt đầu game mới hoặc tiếp tục
        /// </summary>
        public void OnPlayClicked()
        {
            StartCoroutine(LoadGameScene());
        }

        /// <summary>
        /// Khi bấm nút Settings - Mở panel cài đặt
        /// </summary>
        public void OnSettingsClicked()
        {
            HideAllPanels();
            
            if (panel_GUISettings != null)
                panel_GUISettings.SetActive(true);
        }

        /// <summary>
        /// Khi bấm nút Exit - Hiển thị hộp thoại xác nhận thoát
        /// </summary>
        public void OnExitClicked()
        {
            // Hiện panel exit đè lên menu chính (không ẩn menu nền)
            if (panel_Exit != null)
                panel_Exit.SetActive(true);
        }

        /// <summary>
        /// Khi bấm nút Continue - Tiếp tục game (từ pause)
        /// </summary>
        public void OnContinueClicked()
        {
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(false);

            ResumeGame();
        }

        /// <summary>
        /// Đóng panel Settings và quay lại menu chính
        /// </summary>
        public void OnCloseSettings()
        {
            HideAllPanels();
            
            if (panel_GUIGame != null)
                panel_GUIGame.SetActive(true);
        }

        #endregion

        #region Game Flow

        private IEnumerator LoadGameScene()
        {
            // Hiển thị loading
            if (panel_Loading != null)
                panel_Loading.SetActive(true);

            // Load scene bất đồng bộ
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);

            while (!asyncLoad.isDone)
            {
                // Cập nhật loading progress nếu cần
                yield return null;
            }

            isGameRunning = true;
            
            // Ẩn cursor nếu cần
            if (!showCursorOnPlay)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        /// <summary>
        /// Bắt đầu game (gọi từ nút Play)
        /// </summary>
        public void StartGame()
        {
            OnPlayClicked();
        }

        /// <summary>
        /// Tạm dừng game và hiển thị menu pause
        /// </summary>
        public void PauseGame()
        {
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(true);

            Time.timeScale = 0f;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        /// <summary>
        /// Tiếp tục game từ trạng thái pause
        /// </summary>
        public void ResumeGame()
        {
            if (panel_PopUpPause != null)
                panel_PopUpPause.SetActive(false);

            Time.timeScale = 1f;
            
            if (!showCursorOnPlay)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        /// <summary>
        /// Thoát game (gọi từ hộp thoại xác nhận)
        /// </summary>
        public void ConfirmExitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        /// <summary>
        /// Hủy thoát và quay lại menu
        /// </summary>
        public void CancelExit()
        {
            if (panel_Exit != null)
                panel_Exit.SetActive(false);
                
            if (panel_GUIGame != null)
                panel_GUIGame.SetActive(true);
        }

        #endregion

        #region Input Handling

        private void Update()
        {
            // Pause game với phím Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isGameRunning)
                {
                    if (panel_PopUpPause != null && panel_PopUpPause.activeSelf)
                    {
                        ResumeGame();
                    }
                    else
                    {
                        PauseGame();
                    }
                }
            }
        }

        #endregion

        #region Public Properties

        public bool IsGameRunning => isGameRunning;
        
        public GameObject Panel_GUIGame => panel_GUIGame;
        public GameObject Panel_GUISettings => panel_GUISettings;
        public GameObject Panel_Loading => panel_Loading;
        public GameObject Panel_PopUpPause => panel_PopUpPause;
        public GameObject Panel_Exit => panel_Exit;

        #endregion
    }
}
