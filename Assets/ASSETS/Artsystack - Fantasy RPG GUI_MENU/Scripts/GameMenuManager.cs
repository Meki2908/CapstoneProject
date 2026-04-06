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
        [SerializeField] private UnityEngine.UI.Button btn_Settings;
        [SerializeField] private UnityEngine.UI.Button btn_Exit;

        [Header("GUI_btn_Play — tabs")]
        [Tooltip("Tab_Play — nút Play trên prefab mở Multiplayer mode bằng UnityEvent, không cần gán vào đây.")]
        [SerializeField] private GameObject tab_TabPlay;
        [SerializeField] private GameObject tab_MultiplayerMode;
        [SerializeField] private GameObject tab_HostOptions;

        [Header("GUI_btn_Play — actions")]
        [Tooltip("Hàng Tab_Play: Play | Help (giữa) | Exit — Help mặc định mở Settings; đổi OnHelpClicked nếu có panel Help riêng.")]
        [SerializeField] private UnityEngine.UI.Button btn_HelpMiddle;
        [SerializeField] private UnityEngine.UI.Button btn_SinglePlayer;
        [SerializeField] private UnityEngine.UI.Button btn_HostCreate;
        [SerializeField] private UnityEngine.UI.Button btn_HostJoin;

        [Header("Scene Recognition")]
        [Tooltip("Danh sách các scene được xem là Main Menu (Lobby). Nếu không ở các scene này, GameMenuManager sẽ bị vô hiệu hóa.")]
        [SerializeField] private List<string> mainMenuSceneNames = new List<string>
        {
            "DemoSceneSettings", "Menu_Game", "UI_Game", "Pre Start Scene",
        };

        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "Map_Chinh";
        [SerializeField] private bool showCursorOnPlay = false;

        [Tooltip("Giống Pre Start Scene → Testboss: bật trước khi load Map_Chinh để NetworkHostBootstrap StartHost và spawn player.")]
        [SerializeField] private bool requestHostWhenLoadingGameplay = true;

        private bool isGameRunning = false;

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Canvas_Menu có thể DontDestroyOnLoad: ẩn lobby khi vào gameplay, hiện lại panel chính khi quay về menu.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mainMenuSceneNames.Contains(scene.name))
            {
                enabled = true;
                HideAllPanels();
                if (panel_GUIGame != null)
                    panel_GUIGame.SetActive(true);
                ResetPlayModeTabs();
            }
            else
            {
                HideAllPanels();
                enabled = false;
            }
        }

        private void Start()
        {
            // Kiểm tra tên scene hiện tại có nằm trong list Main Menu không
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (!mainMenuSceneNames.Contains(currentSceneName))
            {
                HideAllPanels();
                Debug.Log($"[GameMenuManager] Current scene '{currentSceneName}' is NOT a Main Menu scene. Disabling GameMenuManager.");
                this.enabled = false;
                return;
            }

            Debug.Log($"[GameMenuManager] Active in Main Menu scene '{currentSceneName}'.");
            InitializeMenu();
        }

        /// <summary>Tab_Play hiện, Multiplayer mode và Host options ẩn (màn hình đầu trong GUI_btn_Play).</summary>
        private void ResetPlayModeTabs()
        {
            if (tab_TabPlay != null) tab_TabPlay.SetActive(true);
            if (tab_MultiplayerMode != null) tab_MultiplayerMode.SetActive(false);
            if (tab_HostOptions != null) tab_HostOptions.SetActive(false);
        }

        private void InitializeMenu()
        {
            // Đảm bảo chỉ hiển thị panel chính
            HideAllPanels();
            
            if (panel_GUIGame != null)
                panel_GUIGame.SetActive(true);

            ResetPlayModeTabs();

            // Tab_Play: nút Play → Multiplayer mode (UnityEvent trên prefab). Help (giữa). Single / Create / Join → code bên dưới.
            if (btn_HelpMiddle != null)
                btn_HelpMiddle.onClick.AddListener(OnHelpClicked);
            if (btn_SinglePlayer != null)
                btn_SinglePlayer.onClick.AddListener(OnSinglePlayerClicked);
            if (btn_HostCreate != null)
                btn_HostCreate.onClick.AddListener(OnHostCreateClicked);
            if (btn_HostJoin != null)
                btn_HostJoin.onClick.AddListener(OnJoinHostClicked);
            
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
        /// Nút Help ở giữa hàng Tab_Play. Mặc định mở Settings; override nếu bạn có UI Help riêng.
        /// </summary>
        public void OnHelpClicked()
        {
            OnSettingsClicked();
        }

        /// <summary>
        /// Single player — load scene (host/solo theo requestHostWhenLoadingGameplay).
        /// </summary>
        public void OnSinglePlayerClicked()
        {
            StartCoroutine(LoadGameScene());
        }

        /// <summary>
        /// Create host — cùng luồng load scene + pending host như single (Map_Chinh).
        /// </summary>
        public void OnHostCreateClicked()
        {
            StartCoroutine(LoadGameScene());
        }

        /// <summary>
        /// Tương thích script/UI cũ gọi OnPlayClicked — coi như Single player.
        /// </summary>
        public void OnPlayClicked()
        {
            OnSinglePlayerClicked();
        }

        /// <summary>
        /// Join host — loading screen + load Map_Chinh + client (MultiplayerManager + NetworkClientBootstrap).
        /// </summary>
        public void OnJoinHostClicked()
        {
            StartCoroutine(LoadClientGameScene());
        }

        private IEnumerator LoadClientGameScene()
        {
            HideAllPanels();

            var mp = MultiplayerManager.Instance;
            if (mp == null)
            {
                Debug.LogError("[GameMenuManager] MultiplayerManager.Instance is null — không thể Join.");
                yield break;
            }

            var stm = SceneTransitionManager.EnsureInstance();
            if (stm != null)
            {
                mp.CommitPendingJoinFromMenu();
                stm.GoToScene(gameSceneName, "Đang kết nối...");
                isGameRunning = true;
                yield break;
            }

            mp.StartClientAndLoadGame();
            isGameRunning = true;
        }

        /// <summary>
        /// Khi bấm nút Settings - Mở panel cài đặt
        /// </summary>
        public void OnSettingsClicked()
        {
            HideAllPanels();
            
            // Gọi SettingsManager để bật đúng (child panels + tab mặc định)
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
            ResetPlayModeTabs();
        }

        #endregion

        #region Game Flow

        private IEnumerator LoadGameScene()
        {
            // Ẩn menu trước khi chuyển
            HideAllPanels();

            if (requestHostWhenLoadingGameplay && gameSceneName == "Map_Chinh")
                MultiplayerManager.SetPendingStartHostAfterSceneLoad(true);

            // Dùng SceneTransitionManager (tự tạo nếu chưa có — UI_Game không có player)
            var stm = SceneTransitionManager.EnsureInstance();
            if (stm != null)
            {
                stm.GoToScene(gameSceneName, "Đang tải game...");
                isGameRunning = true;
                yield break;
            }

            // Fallback: hiển thị loading panel tự quản lý
            if (panel_Loading != null)
                panel_Loading.SetActive(true);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            isGameRunning = true;
            
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
            OnSinglePlayerClicked();
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
            ResetPlayModeTabs();
        }

        #endregion

        #region Input Handling

        private void Update()
        {
            // ESC chỉ dùng trong gameplay (pause/resume)
            // Trong lobby, dùng phím B để Back (xử lý bởi SettingsManager)
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
