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
        [SerializeField] private UnityEngine.UI.Button btn_NewGame;
        [SerializeField] private UnityEngine.UI.Button btn_Help;
        [SerializeField] private UnityEngine.UI.Button btn_Settings;
        [SerializeField] private UnityEngine.UI.Button btn_Exit;

        [Header("GUI_btn_Play — tabs (Multiplayer)")]
        [SerializeField] private GameObject tab_TabPlay;
        [SerializeField] private GameObject tab_MultiplayerMode;
        [SerializeField] private GameObject tab_HostOptions;
        
        [Header("GUI_btn_Play — Multiplayer Panels")]
        [SerializeField] private GameObject panel_CreateRoom;
        [SerializeField] private GameObject panel_JoinRoom;
        [SerializeField] private GameObject panel_ConnectedRoom;

        [Header("Multiplayer Mode Tab Buttons")]
        [SerializeField] private UnityEngine.UI.Button btn_SinglePlayer;
        [SerializeField] private UnityEngine.UI.Button btn_Multiplayer;
        [SerializeField] private UnityEngine.UI.Button btn_BackToPlay;

        [Header("Host Options Tab Buttons")]
        [SerializeField] private UnityEngine.UI.Button btn_HostCreate;
        [SerializeField] private UnityEngine.UI.Button btn_HostJoin;
        [SerializeField] private UnityEngine.UI.Button btn_BackFromHost;        [Header("Scene Recognition")]
        [Tooltip("Danh sách các scene được xem là Main Menu (Lobby). Nếu không ở các scene này, GameMenuManager sẽ bị vô hiệu hóa.")]
        [SerializeField] private List<string> mainMenuSceneNames = new List<string> { "DemoSceneSettings", "Menu_Game", "UI_Game" };

        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "Map_Chinh";
        [SerializeField] private bool showCursorOnPlay = false;

        private bool isGameRunning = false;

        private void Start()
        {
            // Kiểm tra tên scene hiện tại có nằm trong list Main Menu không
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (!mainMenuSceneNames.Contains(currentSceneName))
            {
                Debug.Log($"[GameMenuManager] Current scene '{currentSceneName}' is NOT a Main Menu scene. Disabling GameMenuManager.");
                this.enabled = false;
                return;
            }

            Debug.Log($"[GameMenuManager] Active in Main Menu scene '{currentSceneName}'.");
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
                btn_Play.onClick.AddListener(ClickPlayMultiplayer);

            if (btn_NewGame != null)
                btn_NewGame.onClick.AddListener(OnNewGameClicked);
            
            if (btn_Settings != null)
                btn_Settings.onClick.AddListener(OnSettingsClicked);
            
            if (btn_Help != null)
                btn_Help.onClick.AddListener(OnSettingsClicked);

            if (btn_Exit != null)
                btn_Exit.onClick.AddListener(OnExitClicked);

            // Gắn event cho các Tab con (Multiplayer Mode & Host Options)
            if (btn_SinglePlayer != null)
                btn_SinglePlayer.onClick.AddListener(OnSinglePlayerClicked);
            if (btn_Multiplayer != null)
                btn_Multiplayer.onClick.AddListener(ClickHostGame);
            if (btn_BackToPlay != null)
                btn_BackToPlay.onClick.AddListener(BackToPlayTab);

            if (btn_HostCreate != null)
                btn_HostCreate.onClick.AddListener(OnHostCreateClicked);
            if (btn_HostJoin != null)
                btn_HostJoin.onClick.AddListener(OnJoinHostClicked);
            if (btn_BackFromHost != null)
                btn_BackFromHost.onClick.AddListener(BackToMultiplayerMode);

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
        /// Khi bấm nút Play - Mở tab Multiplayer Mode thay vì load game ngay rụp!
        /// </summary>
        public void OnPlayClicked()
        {
            ClickPlayMultiplayer();
        }

        /// <summary>
        /// New Game — xóa tất cả save data rồi bắt đầu mới (hiện tại chuyển sang Multiplayer Mode)
        /// </summary>
        public void OnNewGameClicked()
        {
            // DeleteAllSaveData();
            // StartCoroutine(LoadGameScene());
            ClickPlayMultiplayer();
        }

        public void OnSinglePlayerClicked()
        {
            StartCoroutine(LoadGameScene());
        }

        private void ResetPlayModeTabs()
        {
            if (tab_TabPlay != null) tab_TabPlay.SetActive(true);
            if (tab_MultiplayerMode != null) tab_MultiplayerMode.SetActive(false);
            if (tab_HostOptions != null) tab_HostOptions.SetActive(false);
            if (panel_CreateRoom != null) panel_CreateRoom.SetActive(false);
            if (panel_JoinRoom != null) panel_JoinRoom.SetActive(false);
            if (panel_ConnectedRoom != null) panel_ConnectedRoom.SetActive(false);
        }

        public void ClickPlayMultiplayer()
        {
            if (tab_TabPlay != null) tab_TabPlay.SetActive(false);
            if (tab_MultiplayerMode != null) tab_MultiplayerMode.SetActive(true);
        }
        public void BackToPlayTab()
        {
            ResetPlayModeTabs();
        }
        public void ClickHostGame()
        {
            if (tab_MultiplayerMode != null) tab_MultiplayerMode.SetActive(false);
            if (tab_HostOptions != null) tab_HostOptions.SetActive(true);
        }
        public void BackToMultiplayerMode()
        {
            if (tab_HostOptions != null) tab_HostOptions.SetActive(false);
            if (tab_MultiplayerMode != null) tab_MultiplayerMode.SetActive(true);
        }
        public void BackToHostOptions()
        {
            if (panel_CreateRoom != null) panel_CreateRoom.SetActive(false);
            if (panel_JoinRoom != null) panel_JoinRoom.SetActive(false);
            if (tab_HostOptions != null) tab_HostOptions.SetActive(true);
        }

        public void OnHostCreateClicked()
        {
            Debug.Log("[GameMenuManager] Bấm nút Create Host!");
            
            if (tab_MultiplayerMode != null) tab_MultiplayerMode.SetActive(false);
            if (tab_HostOptions != null) tab_HostOptions.SetActive(false);
            if (panel_JoinRoom != null) panel_JoinRoom.SetActive(false);
            
            if (panel_CreateRoom != null)
            {
                Debug.Log("[GameMenuManager] Panel_CreateRoom đã được gán -> Tiến hành BẬT Panel lên!");
                panel_CreateRoom.SetActive(true);
                var mp = MultiplayerManager.Instance;
                if (mp != null) mp.HideEnterGameButton();
            }
            else
            {
                Debug.LogWarning("[GameMenuManager] LỖI: Chưa kéo Panel_CreateRoom vào Inspector! Bắt đầu tạo Room ngay lập tức!");
                var mp = MultiplayerManager.Instance;
                if (mp != null) { mp.StartHostAndLoadGame(); isGameRunning = true; }
                else StartCoroutine(LoadGameScene());
            }
        }

        public void OnJoinHostClicked()
        {
            Debug.Log("[GameMenuManager] Bấm nút Join Host!");

            if (tab_MultiplayerMode != null) tab_MultiplayerMode.SetActive(false);
            if (tab_HostOptions != null) tab_HostOptions.SetActive(false);
            if (panel_CreateRoom != null) panel_CreateRoom.SetActive(false);

            if (panel_JoinRoom != null)
            {
                Debug.Log("[GameMenuManager] Panel_JoinRoom đã được gán -> Tiến hành BẬT Panel lên!");
                panel_JoinRoom.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[GameMenuManager] LỖI: Chưa kéo Panel_JoinRoom vào Inspector! Bắt đầu vào game ngay lập tức!");
                var mp = MultiplayerManager.Instance;
                if (mp != null) { mp.StartClientAndLoadGame(); isGameRunning = true; }
            }
        }

        /// <summary>
        /// Continue — load game với save hiện có
        /// </summary>
        public void OnContinueClicked_MainMenu()
        {
            StartCoroutine(LoadGameScene());
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
            // Trong scene chơi, PauseMenuManager mới khôi phục timeScale + PlayerInput đầy đủ.
            var pm = PauseMenuManager.Instance;
            if (pm != null && pm.enabled)
            {
                pm.ResumeGame();
                return;
            }

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
            // Ẩn menu trước khi chuyển
            HideAllPanels();

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

        #region Save Data

        private static readonly string[] saveFileNames = {
            "inventory.json",
            "equipment.json",
            "weapon_gems.json",
            "weapon_selection.json",
            "weapon_mastery.json",
            "map_chinh_player_position.json"
        };

        private static readonly string[] prefsKeys = {
            "Dungeon_SaMac_MaxCleared",
            "Dungeon_DamLay_MaxCleared",
            "QUEST_COUNT",
            "QUEST_1", "QUEST_1_STEP",
            "QUEST_2", "QUEST_2_STEP",
            "QUEST_3", "QUEST_3_STEP",
            "QUEST_4", "QUEST_4_STEP",
            "QUEST_5", "QUEST_5_STEP"
        };

        bool HasSaveData()
        {
            string dir = Application.persistentDataPath;
            foreach (var f in saveFileNames)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir, f)))
                    return true;
            }
            if (PlayerPrefs.HasKey("QUEST_COUNT")) return true;
            if (PlayerPrefs.HasKey("Dungeon_SaMac_MaxCleared")) return true;
            return false;
        }

        void DeleteAllSaveData()
        {
            string dir = Application.persistentDataPath;
            foreach (var f in saveFileNames)
            {
                string path = System.IO.Path.Combine(dir, f);
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    Debug.Log($"[NewGame] Deleted: {f}");
                }
            }
            foreach (var key in prefsKeys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                    Debug.Log($"[NewGame] Deleted PlayerPrefs: {key}");
                }
            }
            PlayerPrefs.Save();

            // Force reload all DontDestroyOnLoad singletons so in-memory state is cleared
            if (QuestManager.Instance != null)
                QuestManager.Instance.ForceRefreshFromPrefs();

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.ClearInventory();

            if (EquipmentManager.Instance != null)
                EquipmentManager.Instance.Load();

            if (WeaponGemManager.Instance != null)
                WeaponGemManager.Instance.Load();

            Debug.Log("[NewGame] All save data deleted and singletons refreshed!");
        }

        #endregion
    }
}
