using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using UnityEngine.UI;
using TMPro;
using Artsystack.ArtsystackGui;
using System.Collections;
using UDef = UnityEngine.UI.DefaultControls;
using URes = UnityEngine.UI.DefaultControls.Resources;

/// <summary>
/// Inspector-friendly settings for runtime-built lobby TMP + private-room toggle.
/// Unity serializes this as normal YAML fields — not the internal <c>serializedVersion</c> keys
/// (those are engine metadata and not meant for hand-editing per control).
/// </summary>
[System.Serializable]
public class LobbyRuntimeUiStyle
{
    [Tooltip("Optional. Assign Kurale-Regular SDF (or any TMP font) to match the Artsystack menu.")]
    public TMP_FontAsset fontOverride;

    [Header("Host screen title")]
    public float roomSetupTitleFontSize = 50f;
    public float roomSetupSubtitleFontSize = 24f;

    [Header("Private room toggle row")]
    [Tooltip("Square toggle width & height in layout units.")]
    [Range(20f, 56f)] public float privateToggleBoxSize = 28f;
    public float privateRoomTitleFontSize = 26f;
    public float privateRoomDescFontSize = 18f;
    [Tooltip("Horizontal gap between the toggle and the label column.")]
    public float toggleToLabelGap = 12f;
    [Tooltip("Vertical spacing between title and description lines.")]
    public float privateLabelLineSpacing = 2f;
    [Tooltip("Minimum row height for the private-room line.")]
    public float privateToggleRowMinHeight = 72f;

    [Header("Room name & password (host column)")]
    public float roomNameLabelFontSize = 24f;
    public float passwordLabelFontSize = 24f;
    public float roomNameInputFontSize = 22f;
    public float passwordInputFontSize = 22f;

    [Header("Join wizard inputs")]
    public float joinInputFontSize = 22f;
}

public class MultiplayerMenuUI : MonoBehaviour
{
    [Header("Legacy inputs (optional old multiplayer tab)")]
    [Tooltip("If assigned, Start() loads SavedPlayerName here (separate from room name).")]
    [SerializeField] private TMP_InputField playerDisplayNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Save Data")]
    [Tooltip("Continue button(s) tied to single-player save (e.g. Tab_Hostoptions). Not Tab_Multiplayer recent sessions.")]
    [SerializeField] private GameObject[] continueButtons;

    [Header("Recent sessions (Tab_Multiplayer)")]
    [SerializeField] private Button btnContinueTabMultiplayerRecent;

    [Header("Lobby panels (runtime)")]
    [Tooltip("When enabled, builds Panel_HostRoomSetup + Panel_JoinRoomWizard in Awake.")]
    [SerializeField] private bool buildLobbyPanelsAtRuntime = true;

    [Tooltip("Typography & private-room toggle sizing for runtime lobby UI.")]
    [SerializeField] private LobbyRuntimeUiStyle lobbyRuntimeStyle = new LobbyRuntimeUiStyle();

    [Header("Debug")]
    [Tooltip("Logs join wizard hand-off (public try → password step). Console filter: JoinWizardCrit")]
    [SerializeField] private bool joinWizardCritLogging = false;

    [Tooltip("Password-step join + Fusion TryJoin/OnConnectRequest. Console filter: JoinPassCrit")]
    [SerializeField] private bool joinPasswordCritLogging = false;

    [Header("Connection error recovery")]
    [Tooltip("Build index of UI_Game / main menu (Editor Build Settings order). This project: UI_Game = 0, Map_Chinh = 1.")]
    [SerializeField] private int menuSceneBuildIndex = 0;

    [Tooltip("If OnConnectionError fires and Fusion has no session (not running, not cloud), load the menu when the active scene is not the menu — avoids blue screen after failed join.")]
    [SerializeField] private bool loadMenuWhenSessionDeadAfterConnectionError = true;

    private GameMenuManager _gameMenuManager;
    private GameObject _hostPanelRoot;
    private GameObject _joinPanelRoot;
    private HostSessionUI _hostSessionUi;
    private JoinSessionUI _joinSessionUi;
    private GameObject _sessionCreatePanelRoot;
    private GameObject _recentSessionsPanelRoot;
    private RectTransform _recentHostListContent;
    private RectTransform _recentJoinListContent;

    /// <summary>Normalized session names from Photon after the first <see cref="OnSessionListUpdated"/> while recent panel is open.</summary>
    HashSet<string> _activeLobbyRooms;

    bool _isFetchingLobby;

    /// <summary>Until the first lobby list callback (or fetch end), join rows show "Checking…" and hide JOIN.</summary>
    bool _waitingForFirstLobbySnapshot = true;

    /// <summary>JoinLobby finished without a snapshot (runner busy, error) — show rows without checking text and allow JOIN.</summary>
    bool _lobbyUnverifiedAfterFetch;

    static string NormalizeRoomKey(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName)) return "";
        return roomName.Trim().ToLowerInvariant();
    }

    static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var c = FindChildRecursive(root.GetChild(i), name);
            if (c != null) return c;
        }
        return null;
    }

    void JoinWizardCritLog(string message)
    {
        if (!joinWizardCritLogging) return;
        Debug.Log($"[JoinWizardCrit] {message}");
    }

    void JoinPassCritLog(string message)
    {
        if (!joinPasswordCritLogging) return;
        Debug.Log($"[JoinPassCrit] {message}");
    }

    private void Awake()
    {
        _sessionCreatePanelRoot = FindChildRecursive(transform, "SessionCreatePanel")?.gameObject;
        if (!buildLobbyPanelsAtRuntime)
            return;

        // Parent under SessionCreatePanel so layout is not driven by Canvas_Menu's VerticalLayoutGroup; fallback = full canvas.
        var panelHostRt = _sessionCreatePanelRoot != null ? _sessionCreatePanelRoot.GetComponent<RectTransform>() : null;
        var parentRt = panelHostRt != null ? panelHostRt : GetComponent<RectTransform>();
        var style = lobbyRuntimeStyle ?? new LobbyRuntimeUiStyle();
        LobbyRuntimePanels.Build(parentRt, style, out _hostPanelRoot, out _hostSessionUi, out _joinPanelRoot, out _joinSessionUi);
        if (_hostPanelRoot != null) _hostPanelRoot.SetActive(false);
        if (_joinPanelRoot != null) _joinPanelRoot.SetActive(false);
        SetSessionCreatePanelVisible(false);
    }

    void SetSessionCreatePanelVisible(bool visible)
    {
        if (_sessionCreatePanelRoot != null)
            _sessionCreatePanelRoot.SetActive(visible);
    }

    private void Start()
    {
        _gameMenuManager = FindAnyObjectByType<GameMenuManager>();

        if (errorText != null)
            errorText.text = "";

        if (playerDisplayNameInput != null)
            playerDisplayNameInput.text = PlayerDisplayNamePrefs.GetSavedOrDefault();

        if (_gameMenuManager != null && continueButtons != null)
        {
            bool hasSave = _gameMenuManager.HasSaveData();
            foreach (var btn in continueButtons)
            {
                if (btn != null)
                    btn.SetActive(hasSave);
            }
        }

        RefreshRecentSessionContinueVisibility();

        if (btnContinueTabMultiplayerRecent != null)
            btnContinueTabMultiplayerRecent.onClick.AddListener(OpenRecentSessionsPanel);

        FusionConnectionManager.TryResolveInstance();

        if (FusionConnectionManager.Instance != null)
        {
            FusionConnectionManager.Instance.SetJoinPasswordCritLogging(joinPasswordCritLogging);
            FusionConnectionManager.Instance.SetFusionSingletonLifecycleCrit(joinPasswordCritLogging);
            FusionConnectionManager.Instance.OnConnectionError += HandleConnectionError;
            FusionConnectionManager.Instance.OnSessionConnected += HandleSessionConnected;
        }
        else if (joinPasswordCritLogging)
        {
            Debug.Log("[JoinPassCrit] MultiplayerMenuUI.Start: FusionConnectionManager.Instance is null (no singleton in scene yet — join will abort until manager exists).");
        }

        if (_hostSessionUi != null)
        {
            _hostSessionUi.OnCreateSessionRequested += HandleHostCreateRequested;
            _hostSessionUi.OnBackRequested += CloseHostRoomSetup;
        }

        if (_joinSessionUi != null)
        {
            JoinSessionUI.CritLogEnabled = joinWizardCritLogging;
            JoinSessionUI.JoinPassCritEnabled = joinPasswordCritLogging;
            _joinSessionUi.OnJoinPublicFirstAttempt += HandleJoinPublicFirstAsync;
            _joinSessionUi.OnJoinWithPasswordAttempt += HandleJoinWithPasswordAsync;
            _joinSessionUi.OnBackRequested += CloseJoinWizard;
        }
    }

    private void OnDestroy()
    {
        if (FusionConnectionManager.Instance != null)
        {
            FusionConnectionManager.Instance.OnConnectionError -= HandleConnectionError;
            FusionConnectionManager.Instance.OnSessionConnected -= HandleSessionConnected;
        }

        if (btnContinueTabMultiplayerRecent != null)
            btnContinueTabMultiplayerRecent.onClick.RemoveListener(OpenRecentSessionsPanel);

        if (_hostSessionUi != null)
        {
            _hostSessionUi.OnCreateSessionRequested -= HandleHostCreateRequested;
            _hostSessionUi.OnBackRequested -= CloseHostRoomSetup;
        }

        if (_joinSessionUi != null)
        {
            _joinSessionUi.OnJoinPublicFirstAttempt -= HandleJoinPublicFirstAsync;
            _joinSessionUi.OnJoinWithPasswordAttempt -= HandleJoinWithPasswordAsync;
            _joinSessionUi.OnBackRequested -= CloseJoinWizard;
        }

        if (FusionConnectionManager.Instance != null)
            FusionConnectionManager.Instance.OnSessionListUpdatedEvent -= OnLobbyUpdated;
    }

    /// <summary>Called from Create host — shows session panel and host setup.</summary>
    public void OpenHostRoomSetup()
    {
        SetSessionCreatePanelVisible(true);
        if (_recentSessionsPanelRoot != null)
            _recentSessionsPanelRoot.SetActive(false);
        if (_hostPanelRoot != null)
        {
            _hostPanelRoot.SetActive(true);
            _hostPanelRoot.transform.SetAsLastSibling();
        }
        if (_joinPanelRoot != null)
            _joinPanelRoot.SetActive(false);
        if (_hostSessionUi != null)
            _hostSessionUi.ResetUI();
    }

    void CloseHostRoomSetup()
    {
        if (_hostPanelRoot != null)
            _hostPanelRoot.SetActive(false);
        SetSessionCreatePanelVisible(false);
    }

    /// <summary>Called from Join host — shows session panel and join wizard.</summary>
    public void OpenJoinWizard()
    {
        SetSessionCreatePanelVisible(true);
        if (_recentSessionsPanelRoot != null)
            _recentSessionsPanelRoot.SetActive(false);
        if (_hostPanelRoot != null)
            _hostPanelRoot.SetActive(false);
        if (_joinPanelRoot != null)
        {
            _joinPanelRoot.SetActive(true);
            _joinPanelRoot.transform.SetAsLastSibling();
        }
        if (_joinSessionUi != null)
            _joinSessionUi.ShowWizard();
    }

    void CloseJoinWizard()
    {
        if (_joinSessionUi != null)
            _joinSessionUi.HideWizard();
        if (_joinPanelRoot != null)
            _joinPanelRoot.SetActive(false);
        SetSessionCreatePanelVisible(false);
    }

    void HandleHostCreateRequested(HostSessionUI.SessionData data)
    {
        if (_hostPanelRoot != null)
            _hostPanelRoot.SetActive(false);
        SetSessionCreatePanelVisible(false);
        ShowLoadingScreen();
        if (FusionConnectionManager.Instance != null)
        {
            FusionConnectionManager.Instance.StartHostWithLobbyOptions(
                data.sessionName,
                data.password,
                data.maxPlayers,
                data.isPrivate);
        }
        else
        {
            HideLoadingScreen();
            if (_hostSessionUi != null)
                _hostSessionUi.ResetUI();
            if (_hostPanelRoot != null)
                _hostPanelRoot.SetActive(true);
            SetSessionCreatePanelVisible(true);
            ShowError("FusionConnectionManager not found.");
        }
    }

    void CloseJoinPanelsOnSuccessfulConnect()
    {
        JoinWizardCritLog("CloseJoinPanelsOnSuccessfulConnect (join Ok)");
        if (_joinSessionUi != null)
            _joinSessionUi.HideWizard();
        if (_joinPanelRoot != null)
            _joinPanelRoot.SetActive(false);
        SetSessionCreatePanelVisible(false);
    }

    async void HandleJoinPublicFirstAsync(string roomName)
    {
        JoinWizardCritLog($"HandleJoinPublicFirstAsync ENTER room=\"{roomName}\" joinUiNull={(_joinSessionUi == null)} fusionNull={(FusionConnectionManager.Instance == null)}");
        if (string.IsNullOrWhiteSpace(roomName))
        {
            JoinWizardCritLog("HandleJoinPublicFirstAsync ABORT empty room");
            JoinPassCritLog("UI HandleJoinPublicFirst ABORT reason=empty_room");
            return;
        }

        FusionConnectionManager.TryResolveInstance();

        if (FusionConnectionManager.Instance == null)
        {
            JoinWizardCritLog("HandleJoinPublicFirstAsync ABORT FusionConnectionManager.Instance null");
            JoinPassCritLog("UI HandleJoinPublicFirst ABORT reason=FusionInstance_null (before TryJoin — manager missing or destroyed)");
            return;
        }

        if (_joinSessionUi != null)
            _joinSessionUi.ClearJoinErrors();

        // Probe public join without full-screen loading (password rooms fail fast with ConnectionRefused).
        var result = await FusionConnectionManager.Instance.TryJoinRoomAsync(roomName.Trim(), "", false, true);
        if (FusionConnectionManager.Instance == null)
            JoinPassCritLog($"UI HandleJoinPublicFirst AFTER await TryJoin: FusionInstance_null Ok={result.Ok} reason={result.ShutdownReason} (singleton destroyed during join — see [JoinPassCrit][FusionCM] OnDestroy)");
        JoinWizardCritLog($"HandleJoinPublicFirstAsync AFTER TryJoinRoom Ok={result.Ok} ShutdownReason={result.ShutdownReason}");
        if (result.Ok)
        {
            JoinWizardCritLog("HandleJoinPublicFirstAsync BRANCH success → networking loading + menu loading + close panels");
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.StartNetworkingLoadingUI();
            ShowLoadingScreen();
            CloseJoinPanelsOnSuccessfulConnect();
            return;
        }

        HideLoadingScreen();
        JoinWizardCritLog("HandleJoinPublicFirstAsync after fail (no full loading was shown for probe)");

        if (_joinSessionUi == null)
        {
            JoinWizardCritLog("HandleJoinPublicFirstAsync ABORT joinSessionUi is null after fail — cannot show password step");
            return;
        }

        bool offerPwd = FusionConnectionManager.ShouldOfferPasswordStep(result.ShutdownReason);
        JoinWizardCritLog($"HandleJoinPublicFirstAsync ShouldOfferPasswordStep={offerPwd} for reason={result.ShutdownReason}");
        if (offerPwd)
        {
            JoinWizardCritLog("HandleJoinPublicFirstAsync → ShowStepPassword + hint");
            _joinSessionUi.ShowStepPassword();
            _joinSessionUi.SetPasswordHint("This room may require a password.");
            _joinSessionUi.SetStep2Error("");
        }
        else
        {
            JoinWizardCritLog("HandleJoinPublicFirstAsync → ShowStepRoomName + step1 error");
            _joinSessionUi.ShowStepRoomName();
            _joinSessionUi.SetStep1Error($"Cannot join: {result.ShutdownReason}");
        }
    }

    async void HandleJoinWithPasswordAsync(string roomName, string password)
    {
        int pwdLen = password?.Length ?? 0;
        JoinWizardCritLog($"HandleJoinWithPasswordAsync ENTER room=\"{roomName}\" pwdLen={pwdLen}");
        if (string.IsNullOrWhiteSpace(roomName))
        {
            JoinWizardCritLog("HandleJoinWithPasswordAsync ABORT empty room");
            JoinPassCritLog("UI HandleJoinWithPassword ABORT reason=empty_room");
            return;
        }

        FusionConnectionManager.TryResolveInstance();

        if (FusionConnectionManager.Instance == null)
        {
            JoinWizardCritLog("HandleJoinWithPasswordAsync ABORT FusionConnectionManager.Instance null");
            JoinPassCritLog("UI HandleJoinWithPassword ABORT reason=FusionInstance_null (before TryJoin — see [JoinPassCrit][FusionCM] Awake duplicate / OnDestroy)");
            return;
        }

        if (_joinSessionUi != null)
        {
            _joinSessionUi.SetStep2Error("");
            _joinSessionUi.SetPasswordHint("");
        }

        bool loadingWasActive = _gameMenuManager != null && _gameMenuManager.Panel_Loading != null && _gameMenuManager.Panel_Loading.activeSelf;
        JoinPassCritLog($"UI HandleJoinWithPassword START room=\"{roomName.Trim()}\" pwdLen={pwdLen} loadingAlready={loadingWasActive} " +
                        $"joinPanelActive={(_joinPanelRoot != null && _joinPanelRoot.activeSelf)} frame={Time.frameCount}");

        // Do not show full-screen / hide menu until join succeeds — keeps join wizard visible for wrong password (SceneTransitionManager hides Panel_GUIGame).
        FusionConnectionManager.TryResolveInstance();
        if (FusionConnectionManager.Instance == null)
        {
            JoinPassCritLog("UI HandleJoinWithPassword ABORT: FusionInstance_null (TryResolve found no manager)");
            return;
        }

        var result = await FusionConnectionManager.Instance.TryJoinRoomAsync(roomName.Trim(), password ?? "", true, true);
        if (FusionConnectionManager.Instance == null)
            JoinPassCritLog($"UI HandleJoinWithPassword AFTER await TryJoin: FusionInstance_null Ok={result.Ok} reason={result.ShutdownReason}");
        JoinWizardCritLog($"HandleJoinWithPasswordAsync AFTER TryJoinRoom Ok={result.Ok} ShutdownReason={result.ShutdownReason}");
        JoinPassCritLog($"UI HandleJoinWithPassword RESULT Ok={result.Ok} ShutdownReason={result.ShutdownReason} " +
                        $"(default struct if StartGame threw — check Fusion TryJoin EXCEPTION log)");
        if (result.Ok)
        {
            JoinWizardCritLog("HandleJoinWithPasswordAsync BRANCH success → CloseJoinPanelsOnSuccessfulConnect");
            JoinPassCritLog("UI → success branch CloseJoinPanelsOnSuccessfulConnect");
            ShowLoadingScreen();
            CloseJoinPanelsOnSuccessfulConnect();
            return;
        }

        HideLoadingScreen();

        if (_joinSessionUi != null)
        {
            if (result.ShutdownReason == ShutdownReason.ConnectionRefused)
                _joinSessionUi.SetStep2Error("<color=#f0ad4e>Password incorrect!</color>");
            else
                _joinSessionUi.SetStep2Error($"Lỗi kết nối: {result.ShutdownReason}");
        }
        JoinWizardCritLog("HandleJoinWithPasswordAsync BRANCH fail → SetStep2Error");
        JoinPassCritLog($"UI → fail branch shown step2 error (reason={result.ShutdownReason})");
    }

    /// <summary>Kept for UnityEvents / legacy buttons.</summary>
    public void OnCreateRoomClicked()
    {
        OpenHostRoomSetup();
    }

    public void OnJoinRoomClicked()
    {
        OpenJoinWizard();
    }

    public void OnSinglePlayerClicked()
    {
        ShowLoadingScreen();
        FusionConnectionManager.Instance.StartSinglePlayer();
    }

    private void ShowLoadingScreen()
    {
        if (errorText != null)
            errorText.text = "";

        if (_gameMenuManager != null && _gameMenuManager.Panel_Loading != null)
            _gameMenuManager.Panel_Loading.SetActive(true);
    }

    private void HideLoadingScreen()
    {
        if (_gameMenuManager != null && _gameMenuManager.Panel_Loading != null)
            _gameMenuManager.Panel_Loading.SetActive(false);
    }

    private void HandleConnectionError(string errorMessage)
    {
        if (joinPasswordCritLogging && _joinPanelRoot != null && _joinPanelRoot.activeSelf)
            Debug.Log($"[JoinPassCrit] HandleConnectionError while join UI open: {errorMessage}");
        HideLoadingScreen();
        if (_hostSessionUi != null)
            _hostSessionUi.ResetUI();
        if (_joinSessionUi != null)
            _joinSessionUi.HideWizard();
        if (_hostPanelRoot != null)
            _hostPanelRoot.SetActive(false);
        if (_joinPanelRoot != null)
            _joinPanelRoot.SetActive(false);
        SetSessionCreatePanelVisible(false);
        CloseRecentSessionsPanel();
        ShowError(errorMessage);

        TryReturnToMenuIfSessionDeadAfterError();
    }

    /// <summary>
    /// Only loads menu when networking is fully idle (no runner, or not running and not cloud-connected).
    /// Avoids kicking players during transient errors while a session is still alive.
    /// </summary>
    void TryReturnToMenuIfSessionDeadAfterError()
    {
        if (!loadMenuWhenSessionDeadAfterConnectionError || menuSceneBuildIndex < 0)
            return;

        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (active.buildIndex == menuSceneBuildIndex)
            return;

        bool sessionDead = !FusionConnectionManager.TryResolveInstance() ||
                           FusionConnectionManager.Instance.IsNetworkingFullyIdle();

        if (!sessionDead)
            return;

        JoinPassCritLog($"HandleConnectionError: session dead → LoadScene(menu) buildIndex={menuSceneBuildIndex} (was \"{active.name}\" idx={active.buildIndex}).");
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuSceneBuildIndex);
    }

    void HandleSessionConnected(RecentFusionSessionEntry entry)
    {
        if (entry == null) return;
        RecentFusionSessionsStore.AddOrUpdate(entry);
        RefreshRecentSessionContinueVisibility();
    }

    void RefreshRecentSessionContinueVisibility()
    {
        if (btnContinueTabMultiplayerRecent != null)
            btnContinueTabMultiplayerRecent.gameObject.SetActive(RecentFusionSessionsStore.Count > 0);
    }

    void EnsureRecentSessionsPanel()
    {
        if (_recentSessionsPanelRoot != null) return;
        var panelRt = _sessionCreatePanelRoot != null ? _sessionCreatePanelRoot.GetComponent<RectTransform>() : GetComponent<RectTransform>();
        var style = lobbyRuntimeStyle ?? new LobbyRuntimeUiStyle();
        LobbyRuntimePanels.BuildRecentSessions(panelRt, style, CloseRecentSessionsPanel, out _recentSessionsPanelRoot, out _recentHostListContent, out _recentJoinListContent);
        if (_recentSessionsPanelRoot != null)
            _recentSessionsPanelRoot.SetActive(false);
    }

    /// <summary>Called from Tab_Multiplayer Continue — pick a saved session to re-host or re-join.</summary>
    public void OpenRecentSessionsPanel()
    {
        SetSessionCreatePanelVisible(true);
        if (_hostPanelRoot != null)
            _hostPanelRoot.SetActive(false);
        if (_joinPanelRoot != null)
            _joinPanelRoot.SetActive(false);

        EnsureRecentSessionsPanel();
        _activeLobbyRooms = null;
        _waitingForFirstLobbySnapshot = true;
        _lobbyUnverifiedAfterFetch = false;
        RefreshRecentSessionsList();
        if (_recentSessionsPanelRoot != null)
        {
            _recentSessionsPanelRoot.SetActive(true);
            _recentSessionsPanelRoot.transform.SetAsLastSibling();
        }

        FetchAndFilterActiveSessions();
    }

    public void CloseRecentSessionsPanel()
    {
        _isFetchingLobby = false;
        _waitingForFirstLobbySnapshot = false;
        _lobbyUnverifiedAfterFetch = false;
        if (FusionConnectionManager.Instance != null)
            FusionConnectionManager.Instance.OnSessionListUpdatedEvent -= OnLobbyUpdated;

        if (_recentSessionsPanelRoot != null)
            _recentSessionsPanelRoot.SetActive(false);
        SetSessionCreatePanelVisible(false);
    }

    async void FetchAndFilterActiveSessions()
    {
        if (FusionConnectionManager.Instance == null) return;

        _isFetchingLobby = true;
        FusionConnectionManager.Instance.OnSessionListUpdatedEvent -= OnLobbyUpdated;
        FusionConnectionManager.Instance.OnSessionListUpdatedEvent += OnLobbyUpdated;

        await FusionConnectionManager.Instance.JoinLobbyAsync();

        // Let Fusion deliver OnSessionListUpdated next frame if it is queued after JoinSessionLobby returns.
        await Task.Yield();
        await Task.Yield();

        if (_isFetchingLobby && _waitingForFirstLobbySnapshot)
        {
            // No OnSessionListUpdated (runner still in session, JoinSessionLobby failed, etc.)
            _waitingForFirstLobbySnapshot = false;
            _lobbyUnverifiedAfterFetch = true;
        }

        if (_isFetchingLobby)
            RefreshRecentSessionsList();
    }

    void OnLobbyUpdated(List<SessionInfo> sessions)
    {
        if (!_isFetchingLobby) return;

        _waitingForFirstLobbySnapshot = false;
        _lobbyUnverifiedAfterFetch = false;

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (sessions != null)
        {
            foreach (var s in sessions)
            {
                if (s && !string.IsNullOrEmpty(s.Name))
                    set.Add(NormalizeRoomKey(s.Name));
            }
        }

        _activeLobbyRooms = set;
        RefreshRecentSessionsList();
    }

    void RefreshRecentSessionsList()
    {
        if (_recentHostListContent == null || _recentJoinListContent == null) return;
        for (int i = _recentHostListContent.childCount - 1; i >= 0; i--)
            Destroy(_recentHostListContent.GetChild(i).gameObject);
        for (int i = _recentJoinListContent.childCount - 1; i >= 0; i--)
            Destroy(_recentJoinListContent.GetChild(i).gameObject);

        var style = lobbyRuntimeStyle ?? new LobbyRuntimeUiStyle();
        foreach (var e in RecentFusionSessionsStore.GetOrderedForCurrentProfile())
        {
            if (e.wasHost)
            {
                LobbyRuntimePanels.AddRecentHostSessionRow(_recentHostListContent, style, e, OnRecentJoinClicked, OnRecentClearStoredPasswordClicked, OnRecentDeleteClicked);
                continue;
            }

            string key = NormalizeRoomKey(e.roomName);

            if (_waitingForFirstLobbySnapshot)
            {
                LobbyRuntimePanels.AddRecentJoinSessionRow(
                    _recentJoinListContent, style, e, OnRecentJoinClicked, OnRecentDeleteClicked,
                    showJoinButton: false, statusSubtitle: "Checking lobby…");
                continue;
            }

            if (_lobbyUnverifiedAfterFetch)
            {
                LobbyRuntimePanels.AddRecentJoinSessionRow(
                    _recentJoinListContent, style, e, OnRecentJoinClicked, OnRecentDeleteClicked,
                    showJoinButton: true,
                    statusSubtitle: "Could not verify lobby — you can still try Join");
                continue;
            }

            if (_activeLobbyRooms == null || !_activeLobbyRooms.Contains(key))
                continue;

            LobbyRuntimePanels.AddRecentJoinSessionRow(
                _recentJoinListContent, style, e, OnRecentJoinClicked, OnRecentDeleteClicked,
                showJoinButton: true, statusSubtitle: null);
        }
    }

    void OnRecentClearStoredPasswordClicked(RecentFusionSessionEntry entry)
    {
        if (entry == null || string.IsNullOrWhiteSpace(entry.roomName)) return;
        RecentFusionSessionsStore.ClearStoredPasswordForHost(entry.roomName.Trim());
        RefreshRecentSessionsList();
    }

    void OnRecentJoinClicked(RecentFusionSessionEntry entry)
    {
        CloseRecentSessionsPanel();
        if (entry == null || FusionConnectionManager.Instance == null)
        {
            ShowError("FusionConnectionManager not found.");
            return;
        }

        ShowLoadingScreen();
        if (entry.wasHost)
        {
            FusionConnectionManager.Instance.StartHostWithLobbyOptions(
                entry.roomName,
                entry.password ?? "",
                Mathf.Clamp(entry.hostPlayerCount, 2, 32),
                entry.hostIsPrivate);
        }
        else
        {
            FusionConnectionManager.Instance.JoinRoom(entry.roomName, entry.password ?? "");
        }
    }

    void OnRecentDeleteClicked(RecentFusionSessionEntry entry)
    {
        if (entry == null) return;
        RecentFusionSessionsStore.Remove(entry.roomName, entry.wasHost);
        RefreshRecentSessionContinueVisibility();
        RefreshRecentSessionsList();
    }

    private void ShowError(string msg)
    {
        if (errorText != null)
        {
            errorText.text = msg;
            StopAllCoroutines();
            StartCoroutine(ClearErrorTextAfterDelay(5f));
        }
        else
        {
            Debug.LogWarning($"[MultiplayerMenuUI] Error: {msg} (errorText not assigned on UI)");
        }
    }

    private IEnumerator ClearErrorTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
            errorText.text = "";
    }
}

/// <summary>Join flow: try public join from room name; password step only when needed.</summary>
public class JoinSessionUI : MonoBehaviour
{
    /// <summary>Set from <see cref="MultiplayerMenuUI"/> Start; static so runtime-built wizard can log without inspector.</summary>
    public static bool CritLogEnabled { get; set; } = true;

    /// <summary>Password-step diagnostics (<c>JoinPassCrit</c>).</summary>
    public static bool JoinPassCritEnabled { get; set; } = true;

    static void JoinWizardCritLog(string message)
    {
        if (!CritLogEnabled) return;
        Debug.Log($"[JoinWizardCrit] {message}");
    }

    static void JoinPassCritLog(string message)
    {
        if (!JoinPassCritEnabled) return;
        Debug.Log($"[JoinPassCrit] {message}");
    }

    [SerializeField] private GameObject stepRoomNameRoot;
    [SerializeField] private GameObject stepPasswordRoot;
    [SerializeField] private TMP_InputField displayNameInput;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private Button continueJoinButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button backFromPasswordButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI step1ErrorText;
    [SerializeField] private TextMeshProUGUI step2ErrorText;
    [SerializeField] private TextMeshProUGUI passwordHintText;

    public event Action<string> OnJoinPublicFirstAttempt;
    public event Action<string, string> OnJoinWithPasswordAttempt;
    public event Action OnBackRequested;

    public void WireRuntimeJoin(
        GameObject step1,
        GameObject step2,
        TMP_InputField displayName,
        TMP_InputField room,
        TMP_InputField pass,
        Button step1Join,
        Button confirm,
        Button backPwd,
        Button cancel,
        TextMeshProUGUI errStep1,
        TextMeshProUGUI pwdHint,
        TextMeshProUGUI errStep2)
    {
        stepRoomNameRoot = step1;
        stepPasswordRoot = step2;
        displayNameInput = displayName;
        roomNameInput = room;
        passwordInput = pass;
        continueJoinButton = step1Join;
        confirmButton = confirm;
        backFromPasswordButton = backPwd;
        cancelButton = cancel;
        step1ErrorText = errStep1;
        passwordHintText = pwdHint;
        step2ErrorText = errStep2;

        if (continueJoinButton != null)
            continueJoinButton.onClick.AddListener(OnContinueFromRoomName);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmJoin);
        if (backFromPasswordButton != null)
            backFromPasswordButton.onClick.AddListener(ShowStepRoomName);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => OnBackRequested?.Invoke());
    }

    public void ShowWizard()
    {
        gameObject.SetActive(true);
        ClearJoinErrors();
        ShowStepRoomName();
        if (displayNameInput != null)
            displayNameInput.text = PlayerDisplayNamePrefs.GetSavedOrDefault();
        if (roomNameInput != null) roomNameInput.text = "";
        if (passwordInput != null) passwordInput.text = "";
    }

    public void HideWizard()
    {
        gameObject.SetActive(false);
    }

    public void ClearJoinErrors()
    {
        SetStep1Error("");
        SetStep2Error("");
        SetPasswordHint("");
    }

    public void SetStep1Error(string msg)
    {
        if (step1ErrorText != null)
            step1ErrorText.text = msg ?? "";
    }

    public void SetStep2Error(string msg)
    {
        if (step2ErrorText != null)
            step2ErrorText.text = msg ?? "";
    }

    public void SetPasswordHint(string msg)
    {
        if (passwordHintText != null)
            passwordHintText.text = msg ?? "";
    }

    public void ShowStepRoomName()
    {
        if (stepRoomNameRoot != null) stepRoomNameRoot.SetActive(true);
        if (stepPasswordRoot != null) stepPasswordRoot.SetActive(false);
        SetStep1Error("");
        SetStep2Error("");
        SetPasswordHint("");
        JoinWizardCritLog($"ShowStepRoomName done step1={(stepRoomNameRoot?.activeSelf ?? false)} step2={(stepPasswordRoot?.activeSelf ?? false)}");
    }

    public void ShowStepPassword()
    {
        if (stepRoomNameRoot != null) stepRoomNameRoot.SetActive(false);
        if (stepPasswordRoot != null) stepPasswordRoot.SetActive(true);
        SetStep2Error("");
        JoinWizardCritLog($"ShowStepPassword done step1={(stepRoomNameRoot?.activeSelf ?? false)} step2={(stepPasswordRoot?.activeSelf ?? false)}");
    }

    void OnContinueFromRoomName()
    {
        if (roomNameInput == null || string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            JoinWizardCritLog("OnContinueFromRoomName SKIP (empty input)");
            return;
        }
        SetStep1Error("");
        if (displayNameInput != null)
            PlayerDisplayNamePrefs.Save(displayNameInput.text);
        string r = roomNameInput.text.Trim();
        JoinWizardCritLog($"OnContinueFromRoomName → OnJoinPublicFirstAttempt room=\"{r}\"");
        OnJoinPublicFirstAttempt?.Invoke(r);
    }

    void OnConfirmJoin()
    {
        if (roomNameInput == null || string.IsNullOrWhiteSpace(roomNameInput.text))
        {
            JoinWizardCritLog("OnConfirmJoin SKIP (empty room)");
            JoinPassCritLog("OnConfirmJoin SKIP (empty room)");
            return;
        }
        if (displayNameInput != null)
            PlayerDisplayNamePrefs.Save(displayNameInput.text);
        string room = roomNameInput.text.Trim();
        string pass = passwordInput != null ? passwordInput.text : "";
        JoinWizardCritLog($"OnConfirmJoin → OnJoinWithPasswordAttempt room=\"{room}\" pwdLen={pass.Length}");
        JoinPassCritLog($"OnConfirmJoin CLICK room=\"{room}\" pwdLen={pass.Length} " +
                        $"step1Active={(stepRoomNameRoot != null && stepRoomNameRoot.activeSelf)} " +
                        $"step2Active={(stepPasswordRoot != null && stepPasswordRoot.activeSelf)} " +
                        $"confirmInteractable={(confirmButton != null && confirmButton.interactable)} " +
                        $"confirmEnabled={(confirmButton != null && confirmButton.enabled)} " +
                        $"subscribers={(OnJoinWithPasswordAttempt?.GetInvocationList()?.Length ?? 0)}");
        if (OnJoinWithPasswordAttempt == null)
            JoinPassCritLog("OnConfirmJoin WARNING OnJoinWithPasswordAttempt has no subscribers — MultiplayerMenuUI may not have wired Start()");
        OnJoinWithPasswordAttempt?.Invoke(room, pass);
    }

    private void OnDestroy()
    {
        if (continueJoinButton != null) continueJoinButton.onClick.RemoveAllListeners();
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        if (backFromPasswordButton != null) backFromPasswordButton.onClick.RemoveAllListeners();
        if (cancelButton != null) cancelButton.onClick.RemoveAllListeners();
    }
}

/// <summary>Builds host and join lobby panels at runtime (keeps prefab YAML small).</summary>
public static class LobbyRuntimePanels
{
    static Sprite s_WhiteSliced;
    static LobbyRuntimeUiStyle s_style;

    static void BeginBuild(LobbyRuntimeUiStyle style)
    {
        s_style = style ?? new LobbyRuntimeUiStyle();
    }

    static void ApplySharedTextMeshStyle(TMP_Text tmp)
    {
        if (tmp == null) return;
        var f = s_style != null && s_style.fontOverride != null
            ? s_style.fontOverride
            : TMP_Settings.defaultFontAsset;
        if (f != null)
            tmp.font = f;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
    }

    static Sprite GetWhiteSprite()
    {
        if (s_WhiteSliced != null) return s_WhiteSliced;
        var tex = Texture2D.whiteTexture;
        s_WhiteSliced = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
        return s_WhiteSliced;
    }

    static TMP_DefaultControls.Resources TmpRes()
    {
        var s = GetWhiteSprite();
        return new TMP_DefaultControls.Resources
        {
            standard = s,
            background = s,
            inputField = s,
            knob = s,
            checkmark = s,
            dropdown = s,
            mask = s
        };
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static TextMeshProUGUI AddTmp(Transform parent, string name, string text, float fontSize, FontStyles style, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        ApplySharedTextMeshStyle(tmp);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = fontSize + 16f;
        le.preferredHeight = fontSize + 16f;
        return tmp;
    }

    static TextMeshProUGUI AddJoinErrorLine(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 18f;
        tmp.richText = true;
        tmp.color = new Color(1f, 0.4f, 0.42f);
        tmp.alignment = TextAlignmentOptions.Center;
        ApplySharedTextMeshStyle(tmp);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 22f;
        le.preferredHeight = 22f;
        return tmp;
    }

    static TextMeshProUGUI AddJoinHintLine(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 17f;
        tmp.color = new Color(0.72f, 0.75f, 0.8f);
        tmp.alignment = TextAlignmentOptions.Center;
        ApplySharedTextMeshStyle(tmp);
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 20f;
        le.preferredHeight = 20f;
        return tmp;
    }

    static Button AddButton(Transform parent, string label, Color bg, Color textCol, float height = 56f)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.type = Image.Type.Sliced;
        img.color = bg;
        var btn = go.GetComponent<Button>();
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;

        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        Stretch(txtGo.GetComponent<RectTransform>());
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 26f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textCol;
        ApplySharedTextMeshStyle(tmp);
        btn.targetGraphic = img;
        return btn;
    }

    static TMP_InputField AddTmpInput(Transform parent, string placeholder, float height = 64f, float fontSize = 22f)
    {
        var go = TMP_DefaultControls.CreateInputField(TmpRes());
        go.name = "Input_" + placeholder;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = Mathf.Max(height, fontSize + 36f);
        le.preferredHeight = le.minHeight;
        le.flexibleWidth = 1f;
        var input = go.GetComponent<TMP_InputField>();
        var ph = input.placeholder as TextMeshProUGUI;
        if (ph != null) ph.text = placeholder;
        var fontAsset = s_style != null && s_style.fontOverride != null
            ? s_style.fontOverride
            : TMP_Settings.defaultFontAsset;
        if (fontAsset != null)
        {
            input.fontAsset = fontAsset;
            if (input.textComponent != null)
                input.textComponent.font = fontAsset;
            if (ph != null)
                ph.font = fontAsset;
        }
        if (input.textComponent != null)
        {
            input.textComponent.fontSize = fontSize;
            input.textComponent.color = Color.white;
            ApplySharedTextMeshStyle(input.textComponent);
        }
        if (ph != null)
        {
            ph.fontSize = fontSize;
            ph.color = Color.white;
            ApplySharedTextMeshStyle(ph);
        }
        var bg = go.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = new Color(0.18f, 0.2f, 0.24f, 1f);
            bg.type = Image.Type.Sliced;
        }
        return input;
    }

    static Slider AddSlider(Transform parent, int min, int max)
    {
        var s = GetWhiteSprite();
        var ur = new URes
        {
            standard = s,
            background = s,
            inputField = s,
            knob = s,
            checkmark = s,
            dropdown = s,
            mask = s
        };
        var root = UDef.CreateSlider(ur);
        root.name = "Slider_MaxPlayers";
        root.transform.SetParent(parent, false);
        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 56f;
        le.preferredHeight = 56f;
        le.flexibleWidth = 1f;
        var rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40f);

        var slider = root.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = true;
        slider.value = max;
        var bgImg = root.transform.Find("Background")?.GetComponent<Image>();
        if (bgImg != null) bgImg.color = new Color(0.15f, 0.16f, 0.2f, 1f);
        var fill = root.transform.Find("Fill Area/Fill")?.GetComponent<Image>();
        if (fill != null) fill.color = new Color(0.75f, 0.55f, 0.15f, 1f);
        var knob = root.transform.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
        if (knob != null) knob.color = new Color(0.95f, 0.75f, 0.2f, 1f);
        return slider;
    }

    static Toggle AddToggle(Transform parent, string title, string description)
    {
        float box = Mathf.Clamp(s_style != null ? s_style.privateToggleBoxSize : 28f, 20f, 56f);
        float gap = s_style != null ? s_style.toggleToLabelGap : 12f;
        float titleSz = s_style != null ? s_style.privateRoomTitleFontSize : 26f;
        float descSz = s_style != null ? s_style.privateRoomDescFontSize : 18f;
        float lineGap = s_style != null ? s_style.privateLabelLineSpacing : 2f;
        float rowMin = s_style != null ? s_style.privateToggleRowMinHeight : 72f;
        float labelBlockHeight = titleSz + descSz + lineGap + 12f;
        float rowH = Mathf.Max(rowMin, Mathf.Max(box + 8f, labelBlockHeight));

        var row = new GameObject("ToggleRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var h = row.GetComponent<HorizontalLayoutGroup>();
        h.spacing = gap;
        h.padding = new RectOffset(0, 0, 0, 0);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childForceExpandHeight = false;
        h.childForceExpandWidth = false;
        h.childControlHeight = true;
        h.childControlWidth = true;
        var leRow = row.AddComponent<LayoutElement>();
        leRow.minHeight = rowH;
        leRow.preferredHeight = rowH;

        var tGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        tGo.transform.SetParent(row.transform, false);
        var tRt = tGo.GetComponent<RectTransform>();
        tRt.sizeDelta = new Vector2(box, box);
        var tLe = tGo.AddComponent<LayoutElement>();
        tLe.minWidth = tLe.preferredWidth = box;
        tLe.minHeight = tLe.preferredHeight = box;

        var bg = tGo.AddComponent<Image>();
        bg.sprite = GetWhiteSprite();
        bg.type = Image.Type.Simple;
        bg.color = new Color(0.25f, 0.26f, 0.3f, 1f);
        var chGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        chGo.transform.SetParent(tGo.transform, false);
        var chRt = chGo.GetComponent<RectTransform>();
        chRt.anchorMin = Vector2.zero;
        chRt.anchorMax = Vector2.one;
        chRt.offsetMin = new Vector2(4f, 4f);
        chRt.offsetMax = new Vector2(-4f, -4f);
        var ch = chGo.GetComponent<Image>();
        ch.sprite = GetWhiteSprite();
        ch.type = Image.Type.Simple;
        ch.color = new Color(0.9f, 0.75f, 0.15f, 1f);
        var toggle = tGo.GetComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.graphic = ch;
        toggle.isOn = false;

        var textCol = new GameObject("Labels", typeof(RectTransform), typeof(VerticalLayoutGroup));
        textCol.transform.SetParent(row.transform, false);
        var v = textCol.GetComponent<VerticalLayoutGroup>();
        v.spacing = lineGap;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;
        var textLe = textCol.AddComponent<LayoutElement>();
        textLe.flexibleWidth = 1f;
        textLe.minWidth = 120f;

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(textCol.transform, false);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = titleSz;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.TopLeft;
        ApplySharedTextMeshStyle(titleTmp);
        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.minHeight = titleLe.preferredHeight = titleSz + 6f;

        var descGo = new GameObject("Desc", typeof(RectTransform));
        descGo.transform.SetParent(textCol.transform, false);
        var descTmp = descGo.AddComponent<TextMeshProUGUI>();
        descTmp.text = description;
        descTmp.fontSize = descSz;
        descTmp.fontStyle = FontStyles.Normal;
        descTmp.color = new Color(0.75f, 0.78f, 0.82f);
        descTmp.alignment = TextAlignmentOptions.TopLeft;
        ApplySharedTextMeshStyle(descTmp);
        var descLe = descGo.AddComponent<LayoutElement>();
        descLe.minHeight = descLe.preferredHeight = descSz + 6f;

        return toggle;
    }

    /// <param name="parentContainer">Usually <c>SessionCreatePanel</c> (full rect) or the menu canvas root.</param>
    public static void Build(RectTransform parentContainer, LobbyRuntimeUiStyle style, out GameObject hostPanel, out HostSessionUI hostUi, out GameObject joinPanel, out JoinSessionUI joinUi)
    {
        BeginBuild(style);
        int layer = parentContainer.gameObject.layer;

        // --- Host ---
        hostPanel = new GameObject("Panel_HostRoomSetup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hostPanel.layer = layer;
        hostPanel.transform.SetParent(parentContainer, false);
        Stretch(hostPanel.GetComponent<RectTransform>());
        hostPanel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        hostPanel.GetComponent<Image>().raycastTarget = true;

        var hostContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        hostContent.transform.SetParent(hostPanel.transform, false);
        Stretch(hostContent.GetComponent<RectTransform>());
        var hostV = hostContent.GetComponent<VerticalLayoutGroup>();
        hostV.padding = new RectOffset(48, 48, 40, 40);
        hostV.spacing = 16f;
        hostV.childAlignment = TextAnchor.UpperCenter;
        hostV.childControlHeight = true;
        hostV.childControlWidth = true;
        hostV.childForceExpandWidth = true;

        AddTmp(hostContent.transform, "Title", "ROOM SETUP", s_style.roomSetupTitleFontSize, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f), TextAlignmentOptions.Left);
        AddTmp(hostContent.transform, "Subtitle", "Configure your room before starting the session.", s_style.roomSetupSubtitleFontSize, FontStyles.Normal, new Color(0.75f, 0.78f, 0.82f), TextAlignmentOptions.Left);

        var row = new GameObject("TwoCol", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(hostContent.transform, false);
        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 580f;
        rowLe.preferredHeight = 580f;
        rowLe.flexibleWidth = 1f;
        var hRow = row.GetComponent<HorizontalLayoutGroup>();
        hRow.spacing = 32f;
        hRow.childForceExpandHeight = true;
        hRow.childForceExpandWidth = true;

        var left = new GameObject("Col_Left", typeof(RectTransform), typeof(VerticalLayoutGroup));
        left.transform.SetParent(row.transform, false);
        var leftV = left.GetComponent<VerticalLayoutGroup>();
        leftV.spacing = 12f;
        leftV.childAlignment = TextAnchor.UpperLeft;
        leftV.childControlHeight = true;
        leftV.childControlWidth = true;
        leftV.childForceExpandWidth = true;
        var leftLe = left.AddComponent<LayoutElement>();
        leftLe.flexibleWidth = 1f;

        var right = new GameObject("Col_Right", typeof(RectTransform), typeof(VerticalLayoutGroup));
        right.transform.SetParent(row.transform, false);
        var rightV = right.GetComponent<VerticalLayoutGroup>();
        rightV.spacing = 12f;
        rightV.childAlignment = TextAnchor.UpperLeft;
        rightV.childControlHeight = true;
        rightV.childControlWidth = true;
        rightV.childForceExpandWidth = true;
        var rightLe = right.AddComponent<LayoutElement>();
        rightLe.flexibleWidth = 1f;

        AddTmp(left.transform, "H1", "BASIC INFO", 28f, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f));
        AddTmp(left.transform, "L0", "YOUR DISPLAY NAME", s_style.roomNameLabelFontSize, FontStyles.Bold, Color.white);
        var inPlayerDisplayName = AddTmpInput(left.transform, "How others see you in-game", 64f, s_style.roomNameInputFontSize);
        AddTmp(left.transform, "L1", "ROOM NAME *", s_style.roomNameLabelFontSize, FontStyles.Bold, Color.white);
        var inName = AddTmpInput(left.transform, "e.g. My Adventure Room...", 64f, s_style.roomNameInputFontSize);
        AddTmp(left.transform, "L2", "PASSWORD (OPTIONAL)", s_style.passwordLabelFontSize, FontStyles.Bold, Color.white);
        var inPass = AddTmpInput(left.transform, "Leave empty for a public room", 64f, s_style.passwordInputFontSize);
        var priv = AddToggle(left.transform, "PRIVATE ROOM", "Hidden from public room list / matchmaking.");

        AddTmp(right.transform, "H2", "SESSION & PLAYERS", 28f, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f));
        AddTmp(right.transform, "L3", "MAX PLAYERS", 22f, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f));
        var maxLabel = AddTmp(right.transform, "MaxLabel", "4 / 4", 26f, FontStyles.Normal, Color.white);
        var slider = AddSlider(right.transform, 2, 4);
        AddTmp(right.transform, "Hint", "2 — 4 squads (max)", 18f, FontStyles.Normal, new Color(0.65f, 0.68f, 0.72f));

        var footer = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footer.transform.SetParent(hostContent.transform, false);
        var fH = footer.GetComponent<HorizontalLayoutGroup>();
        fH.childAlignment = TextAnchor.MiddleRight;
        fH.spacing = 24f;
        fH.padding = new RectOffset(0, 0, 24, 0);
        var fLe = footer.AddComponent<LayoutElement>();
        fLe.minHeight = 72f;
        fLe.preferredHeight = 72f;
        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(footer.transform, false);
        spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var btnBack = AddButton(footer.transform, "CANCEL / BACK", new Color(0.22f, 0.24f, 0.28f, 1f), Color.white);
        btnBack.GetComponent<LayoutElement>().preferredWidth = 320f;
        var btnCreate = AddButton(footer.transform, "CREATE ROOM", new Color(0.85f, 0.45f, 0.12f, 1f), Color.white);
        btnCreate.GetComponent<LayoutElement>().preferredWidth = 360f;

        hostUi = hostPanel.AddComponent<HostSessionUI>();
        hostUi.WireRuntimeControls(inPlayerDisplayName, inName, inPass, priv, slider, maxLabel, btnCreate, btnBack);

        // --- Join ---
        joinPanel = new GameObject("Panel_JoinRoomWizard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        joinPanel.layer = layer;
        joinPanel.transform.SetParent(parentContainer, false);
        Stretch(joinPanel.GetComponent<RectTransform>());
        joinPanel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        joinPanel.GetComponent<Image>().raycastTarget = true;

        var joinOuter = new GameObject("JoinContent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        joinOuter.transform.SetParent(joinPanel.transform, false);
        Stretch(joinOuter.GetComponent<RectTransform>());
        var joinV = joinOuter.GetComponent<VerticalLayoutGroup>();
        joinV.padding = new RectOffset(64, 64, 80, 80);
        joinV.spacing = 20f;
        joinV.childAlignment = TextAnchor.MiddleCenter;
        joinV.childControlWidth = true;
        joinV.childForceExpandWidth = true;

        AddTmp(joinOuter.transform, "JTitle", "JOIN SESSION", 48f, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f), TextAlignmentOptions.Center);

        var step1 = new GameObject("Step_RoomName", typeof(RectTransform), typeof(VerticalLayoutGroup));
        step1.transform.SetParent(joinOuter.transform, false);
        var s1v = step1.GetComponent<VerticalLayoutGroup>();
        s1v.spacing = 16f;
        s1v.childAlignment = TextAnchor.MiddleCenter;
        s1v.childControlWidth = true;
        s1v.childForceExpandWidth = true;
        var s1le = step1.AddComponent<LayoutElement>();
        s1le.minHeight = 400f;
        AddTmp(step1.transform, "jd0", "YOUR DISPLAY NAME", 22f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        var joinDisplayName = AddTmpInput(step1.transform, "Player name...", 60f, s_style.joinInputFontSize);
        AddTmp(step1.transform, "jr1", "ROOM NAME", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        var joinRoom = AddTmpInput(step1.transform, "Enter room name...", 64f, s_style.joinInputFontSize);
        var joinErr1 = AddJoinErrorLine(step1.transform, "JoinErr1");
        var btnJoin1 = AddButton(step1.transform, "JOIN", new Color(0.22f, 0.45f, 0.75f, 1f), Color.white);

        var step2 = new GameObject("Step_Password", typeof(RectTransform), typeof(VerticalLayoutGroup));
        step2.transform.SetParent(joinOuter.transform, false);
        var s2v = step2.GetComponent<VerticalLayoutGroup>();
        s2v.spacing = 16f;
        s2v.childAlignment = TextAnchor.MiddleCenter;
        s2v.childControlWidth = true;
        s2v.childForceExpandWidth = true;
        var s2le = step2.AddComponent<LayoutElement>();
        s2le.minHeight = 320f;
        AddTmp(step2.transform, "jp1", "PASSWORD (IF ANY)", 24f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        var pwdHint = AddJoinHintLine(step2.transform, "PwdHint");
        var joinPass = AddTmpInput(step2.transform, "Enter password if required", 64f, s_style.joinInputFontSize);
        joinPass.contentType = TMP_InputField.ContentType.Password;
        var joinErr2 = AddJoinErrorLine(step2.transform, "JoinErr2");
        var rowJ = new GameObject("RowPwd", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        rowJ.transform.SetParent(step2.transform, false);
        var rowJH = rowJ.GetComponent<HorizontalLayoutGroup>();
        rowJH.spacing = 16f;
        rowJH.childAlignment = TextAnchor.MiddleCenter;
        rowJ.AddComponent<LayoutElement>().minHeight = 64f;
        var btnBackPwd = AddButton(rowJ.transform, "Back", new Color(0.22f, 0.24f, 0.28f, 1f), Color.white, 52f);
        btnBackPwd.GetComponent<LayoutElement>().preferredWidth = 220f;
        var btnConfirm = AddButton(rowJ.transform, "CONFIRM", new Color(0.2f, 0.65f, 0.35f, 1f), Color.white, 52f);
        btnConfirm.GetComponent<LayoutElement>().preferredWidth = 260f;
        var btnCancelJoin = AddButton(joinOuter.transform, "Close", new Color(0.35f, 0.2f, 0.2f, 1f), Color.white, 48f);

        joinUi = joinPanel.AddComponent<JoinSessionUI>();
        joinUi.WireRuntimeJoin(step1, step2, joinDisplayName, joinRoom, joinPass, btnJoin1, btnConfirm, btnBackPwd, btnCancelJoin, joinErr1, pwdHint, joinErr2);
    }

    static void BuildRecentScrollArea(Transform column, out RectTransform listContent)
    {
        listContent = null;
        var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(column, false);
        var scrollLe = scrollGo.AddComponent<LayoutElement>();
        scrollLe.flexibleHeight = 1f;
        scrollLe.minHeight = 160f;
        Stretch(scrollGo.GetComponent<RectTransform>());
        var scrollBg = scrollGo.GetComponent<Image>();
        scrollBg.color = new Color(0.15f, 0.16f, 0.2f, 1f);
        scrollBg.raycastTarget = true;
        var scroll = scrollGo.GetComponent<ScrollRect>();

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        Stretch(vpRt);
        viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.02f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        listContent = content.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.sizeDelta = new Vector2(0, 0);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(12, 12, 12, 12);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scroll.content = listContent;
        scroll.viewport = vpRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    /// <summary>Same container and chrome as <see cref="Build"/> host/join panels (SessionCreatePanel).</summary>
    public static void BuildRecentSessions(RectTransform parentContainer, LobbyRuntimeUiStyle style, Action onClose, out GameObject root, out RectTransform hostListContent, out RectTransform joinListContent)
    {
        root = null;
        hostListContent = null;
        joinListContent = null;
        if (parentContainer == null) return;

        BeginBuild(style);
        int layer = parentContainer.gameObject.layer;

        root = new GameObject("Panel_RecentSessions", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.layer = layer;
        root.transform.SetParent(parentContainer, false);
        Stretch(root.GetComponent<RectTransform>());
        var rootImg = root.GetComponent<Image>();
        rootImg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        rootImg.raycastTarget = true;

        var outer = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        outer.transform.SetParent(root.transform, false);
        Stretch(outer.GetComponent<RectTransform>());
        var outerV = outer.GetComponent<VerticalLayoutGroup>();
        outerV.padding = new RectOffset(48, 48, 40, 40);
        outerV.spacing = 16f;
        outerV.childAlignment = TextAnchor.UpperCenter;
        outerV.childControlHeight = true;
        outerV.childControlWidth = true;
        outerV.childForceExpandWidth = true;

        AddTmp(outer.transform, "Title", "RECENT SESSIONS", s_style.roomSetupTitleFontSize, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f), TextAlignmentOptions.Left);
        AddTmp(outer.transform, "Subtitle", "Re-host your rooms or join sessions you played before.", s_style.roomSetupSubtitleFontSize, FontStyles.Normal, new Color(0.75f, 0.78f, 0.82f), TextAlignmentOptions.Left);

        var listsRow = new GameObject("RecentTwoCol", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        listsRow.transform.SetParent(outer.transform, false);
        var listsRowLe = listsRow.AddComponent<LayoutElement>();
        listsRowLe.flexibleHeight = 1f;
        listsRowLe.minHeight = 200f;
        Stretch(listsRow.GetComponent<RectTransform>());
        var listsH = listsRow.GetComponent<HorizontalLayoutGroup>();
        listsH.spacing = 24f;
        listsH.childForceExpandHeight = true;
        listsH.childForceExpandWidth = true;
        listsH.childControlHeight = true;
        listsH.childControlWidth = true;

        var colHost = new GameObject("Col_HostRecent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        colHost.transform.SetParent(listsRow.transform, false);
        var colHostV = colHost.GetComponent<VerticalLayoutGroup>();
        colHostV.spacing = 8f;
        colHostV.childAlignment = TextAnchor.UpperCenter;
        colHostV.childControlWidth = true;
        colHostV.childForceExpandWidth = true;
        colHost.AddComponent<LayoutElement>().flexibleWidth = 1f;
        AddTmp(colHost.transform, "HHost", "YOUR ROOMS (HOST)", 24f, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f), TextAlignmentOptions.Left);
        BuildRecentScrollArea(colHost.transform, out hostListContent);

        var colJoin = new GameObject("Col_JoinRecent", typeof(RectTransform), typeof(VerticalLayoutGroup));
        colJoin.transform.SetParent(listsRow.transform, false);
        var colJoinV = colJoin.GetComponent<VerticalLayoutGroup>();
        colJoinV.spacing = 8f;
        colJoinV.childAlignment = TextAnchor.UpperCenter;
        colJoinV.childControlWidth = true;
        colJoinV.childForceExpandWidth = true;
        colJoin.AddComponent<LayoutElement>().flexibleWidth = 1f;
        AddTmp(colJoin.transform, "HJoin", "JOIN AGAIN", 24f, FontStyles.Bold, new Color(0.95f, 0.82f, 0.35f), TextAlignmentOptions.Left);
        BuildRecentScrollArea(colJoin.transform, out joinListContent);

        var footer = new GameObject("Footer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        footer.transform.SetParent(outer.transform, false);
        var fH = footer.GetComponent<HorizontalLayoutGroup>();
        fH.childAlignment = TextAnchor.MiddleRight;
        fH.spacing = 24f;
        fH.padding = new RectOffset(0, 0, 8, 0);
        var fLe = footer.AddComponent<LayoutElement>();
        fLe.minHeight = 72f;
        fLe.preferredHeight = 72f;
        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(footer.transform, false);
        spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var btnClose = AddButton(footer.transform, "CLOSE", new Color(0.22f, 0.24f, 0.28f, 1f), Color.white);
        btnClose.GetComponent<LayoutElement>().preferredWidth = 320f;
        btnClose.onClick.AddListener(() => onClose?.Invoke());
    }

    public static void AddRecentHostSessionRow(RectTransform listContent, LobbyRuntimeUiStyle style, RecentFusionSessionEntry entry, Action<RecentFusionSessionEntry> onRehost, Action<RecentFusionSessionEntry> onClearStoredPassword, Action<RecentFusionSessionEntry> onDelete)
    {
        if (listContent == null || entry == null) return;
        BeginBuild(style);

        var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(listContent, false);
        var rowH = row.GetComponent<HorizontalLayoutGroup>();
        rowH.spacing = 8f;
        rowH.childAlignment = TextAnchor.MiddleLeft;
        rowH.childForceExpandWidth = false;
        rowH.childControlWidth = true;
        rowH.childControlHeight = true;
        var rowLe = row.GetComponent<LayoutElement>();
        rowLe.minHeight = 56f;
        rowLe.preferredHeight = 56f;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.text = entry.roomName ?? "";
        labelTmp.fontSize = s_style.joinInputFontSize;
        labelTmp.fontStyle = FontStyles.Normal;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ApplySharedTextMeshStyle(labelTmp);
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        labelLe.minWidth = 80f;

        var rehostBtn = AddButton(row.transform, "RE-HOST", new Color(0.85f, 0.45f, 0.12f, 1f), Color.white, 48f);
        rehostBtn.GetComponent<LayoutElement>().preferredWidth = 118f;
        var entryRe = CloneRecentEntry(entry);
        rehostBtn.onClick.AddListener(() => onRehost?.Invoke(entryRe));

        var clearBtn = AddButton(row.transform, "CLEAR PW", new Color(0.22f, 0.5f, 0.55f, 1f), Color.white, 48f);
        clearBtn.GetComponent<LayoutElement>().preferredWidth = 118f;
        var entryClr = CloneRecentEntry(entry);
        clearBtn.onClick.AddListener(() => onClearStoredPassword?.Invoke(entryClr));

        var delBtn = AddButton(row.transform, "DELETE", new Color(0.35f, 0.2f, 0.2f, 1f), Color.white, 48f);
        delBtn.GetComponent<LayoutElement>().preferredWidth = 108f;
        var entryDel = CloneRecentEntry(entry);
        delBtn.onClick.AddListener(() => onDelete?.Invoke(entryDel));
    }

    public static void AddRecentJoinSessionRow(RectTransform listContent, LobbyRuntimeUiStyle style, RecentFusionSessionEntry entry, Action<RecentFusionSessionEntry> onJoin, Action<RecentFusionSessionEntry> onDelete, bool showJoinButton, string statusSubtitle)
    {
        if (listContent == null || entry == null) return;
        BeginBuild(style);

        var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(listContent, false);
        var rowH = row.GetComponent<HorizontalLayoutGroup>();
        rowH.spacing = 12f;
        rowH.childAlignment = TextAnchor.MiddleLeft;
        rowH.childForceExpandWidth = false;
        rowH.childControlWidth = true;
        rowH.childControlHeight = true;
        var rowLe = row.GetComponent<LayoutElement>();
        bool hasSubtitle = !string.IsNullOrEmpty(statusSubtitle);
        rowLe.minHeight = hasSubtitle ? 64f : 56f;
        rowLe.preferredHeight = hasSubtitle ? 64f : 56f;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(row.transform, false);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.richText = true;
        string name = string.IsNullOrEmpty(entry.roomName) ? "—" : entry.roomName;
        if (hasSubtitle)
        {
            labelTmp.text = $"{name}\n<size=60%><color=#8899aa>{statusSubtitle}</color></size>";
            labelTmp.color = new Color(1f, 1f, 1f, 0.92f);
        }
        else
        {
            labelTmp.text = entry.roomName ?? "";
            labelTmp.color = Color.white;
        }
        labelTmp.fontSize = s_style.joinInputFontSize;
        labelTmp.fontStyle = FontStyles.Normal;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ApplySharedTextMeshStyle(labelTmp);
        var labelLe = labelGo.AddComponent<LayoutElement>();
        labelLe.flexibleWidth = 1f;
        labelLe.minWidth = 120f;

        if (showJoinButton)
        {
            var joinBtn = AddButton(row.transform, "JOIN", new Color(0.22f, 0.45f, 0.75f, 1f), Color.white, 52f);
            joinBtn.GetComponent<LayoutElement>().preferredWidth = 140f;
            var entryJoin = CloneRecentEntry(entry);
            joinBtn.onClick.AddListener(() => onJoin?.Invoke(entryJoin));
        }
        else
        {
            var spacer = new GameObject("JoinSpacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(row.transform, false);
            spacer.GetComponent<LayoutElement>().preferredWidth = 140f;
        }

        var delBtn = AddButton(row.transform, "DELETE", new Color(0.35f, 0.2f, 0.2f, 1f), Color.white, 52f);
        delBtn.GetComponent<LayoutElement>().preferredWidth = 120f;
        var entryDel = CloneRecentEntry(entry);
        delBtn.onClick.AddListener(() => onDelete?.Invoke(entryDel));
    }

    static RecentFusionSessionEntry CloneRecentEntry(RecentFusionSessionEntry e)
    {
        return new RecentFusionSessionEntry
        {
            roomName = e.roomName,
            password = e.password,
            wasHost = e.wasHost,
            profileKey = e.profileKey,
            lastUsedUtcTicks = e.lastUsedUtcTicks,
            hostPlayerCount = e.hostPlayerCount,
            hostIsPrivate = e.hostIsPrivate
        };
    }
}
