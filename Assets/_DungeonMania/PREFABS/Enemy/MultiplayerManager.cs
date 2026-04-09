using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using Artsystack.ArtsystackGui;

/// <summary>
/// Gắn lên Canvas menu (Pre Start / Canvas_Menu). Nút Create Host / Join Host gọi
/// <see cref="CreateRoom"/> / <see cref="StartClientAndLoadGame"/>.
/// Sử dụng Photon Fusion 2 thay cho Unity Netcode + Relay.
///
/// --- FLOW KẾT NỐI QUA INTERNET ---
/// 
/// HOST:
///   1. Bấm "Create Room" → tạo Fusion session trên Photon Cloud
///   2. Hiện room code trên UI (vd. "A1B2C3")
///   3. Chờ client join → hiện player list realtime trên menu
///   4. Bấm "Enter Game" → load Map_Chinh cho tất cả player
///
/// CLIENT:
///   1. Nhập room code → bấm "Join"
///   2. Kết nối Photon Cloud → tìm session "A1B2C3"
///   3. Nếu host CHƯA load scene → chờ tại "Connected! Waiting for host..."
///   4. Khi host load scene → Fusion tự đồng bộ → tất cả vào Map_Chinh
///   5. NetworkLobbyManager hiện trong gameplay → host bấm START
///
/// Photon Cloud xử lý NAT traversal, relay, matchmaking tự động.
/// Không cần Unity Relay, không cần quản lý IP/port.
/// </summary>
public class MultiplayerManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static MultiplayerManager Instance { get; private set; }

    /// <summary>True khi đang là host.</summary>
    public static bool IsHost { get; private set; }

    /// <summary>Session name (room code) hiện tại.</summary>
    public static string CurrentSessionName { get; private set; }

    /// <summary>Tên player local — set trước khi join/host.</summary>
    public static string LocalPlayerName { get; set; } = "Player";

    [Tooltip("Tên scene trong Build Settings (không cần đường dẫn).")]
    [SerializeField] private string gameplaySceneName = "Map_Chinh";

    [Header("=== ROOM SETTINGS ===")]
    [Tooltip("Prefab có NetworkRunner + NetworkSceneManagerDefault. Để null = tự tạo runtime.")]
    [SerializeField] private NetworkRunner runnerPrefab;

    [Tooltip("Scene reference prefab — NetworkObject được dùng làm StartGameObject khi load scene. Phải nằm trong gameplay scene (Map_Chinh).")]
    [SerializeField] private NetworkObject sceneReferencePrefab;

    [Tooltip("Player prefab (phải có NetworkObject Fusion). Gán trong Inspector.")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("=== SCENE LOADING ===")]
    [Tooltip("Thời gian chờ tối đa (giây) trước khi scene load bị coi là timeout.")]
    [SerializeField] private float sceneLoadTimeout = 30f;

    [Header("=== UI REFERENCES ===")]
    [Tooltip("Input field để nhập Room Code (Join panel).")]
    [SerializeField] private TMP_InputField joinCodeInput;


    [Tooltip("Text hiển thị trạng thái (connecting, error...) — Host panel.")]
    [SerializeField] private TextMeshProUGUI statusText;
    [Tooltip("Text hiển thị trạng thái — Join panel.")]
    [SerializeField] private TextMeshProUGUI statusTextJoin;

    [Header("=== PLAYER NAME ===")]
    [Tooltip("Input field to enter player name (Host panel).")]
    [SerializeField] private TMP_InputField playerNameInput;
    [Tooltip("Input field to enter player name (Join panel).")]
    [SerializeField] private TMP_InputField playerNameInputJoin;

    [Header("=== HOST BUTTONS ===")]
    [Tooltip("CREATE ROOM button (hidden after room created).")]
    [SerializeField] private GameObject createRoomButton;
    [Tooltip("ENTER GAME button (shown after room created).")]
    [SerializeField] private GameObject enterGameButton;
    [Tooltip("Text thông báo yêu cầu người chơi trước khi vào game.")]
    [SerializeField] private TextMeshProUGUI waitingForPlayersText;

    [Header("=== PLAYER LIST (Panel_CreateRoom) ===")]
    [Tooltip("Container with VerticalLayoutGroup for player entries.")]
    [SerializeField] private Transform playerListContainer;
    [Tooltip("Text showing 'Players: X/4'.")]
    [SerializeField] private TextMeshProUGUI playerCountText;

    [Header("=== PANEL REFERENCES ===")]
    [Tooltip("Panel_CreateRoom — hiện khi host chưa tạo phòng.")]
    [SerializeField] private GameObject panelCreateRoom;
    [Tooltip("Panel_JoinRoom — ẩn sau khi client kết nối thành công.")]
    [SerializeField] private GameObject panelJoinRoom;
    [Tooltip("Panel_ConnectedRoom — panel chung khi cả host và client đã vào room.")]
    [SerializeField] private GameObject panelConnectedRoom;

    [Header("=== CONNECTED ROOM UI (Shared) ===")]
    [Tooltip("Text hiện room code trên Panel_ConnectedRoom.")]
    [SerializeField] private TextMeshProUGUI connectedRoomCodeText;
    [Tooltip("Text hiện trạng thái trên Panel_ConnectedRoom.")]
    [SerializeField] private TextMeshProUGUI connectedStatusText;
    [Tooltip("Text hiện player count trên Panel_ConnectedRoom.")]
    [SerializeField] private TextMeshProUGUI connectedPlayerCountText;
    [Tooltip("Container player list trên Panel_ConnectedRoom.")]
    [SerializeField] private Transform connectedPlayerListContainer;
    [Tooltip("Button START GAME — chỉ host thấy.")]
    [SerializeField] private GameObject startGameButton;
    [Tooltip("Button LEAVE ROOM — cả host và client.")]
    [SerializeField] private GameObject leaveRoomButton;
    [Tooltip("Button COPY room code.")]
    [SerializeField] private GameObject copyCodeButton;

    /// <summary>Runner hiện tại (tồn tại xuyên scene).</summary>
    public static NetworkRunner Runner { get; private set; }

    /// <summary>Tracking spawned player objects for cleanup.</summary>
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    /// <summary>Tracking connected player UI entries on the ConnectedPanel by PlayerRef.</summary>
    private Dictionary<PlayerRef, GameObject> _connectedPlayerEntries = new();

    /// <summary>Cache received player names from ReliableData (host side, trước scene load).</summary>
    private Dictionary<PlayerRef, string> _receivedPlayerNames = new();

    /// <summary>ReliableKey for player name sharing before scene load.</summary>
    private static readonly ReliableKey PLAYER_NAME_KEY = ReliableKey.FromInts(1, 0, 0, 0);

    /// <summary>ReliableKey cho host relay tên player cho các client khác.
    /// Data format: [4 bytes PlayerId][UTF8 name bytes]</summary>
    private static readonly ReliableKey PLAYER_NAME_RELAY_KEY = ReliableKey.FromInts(1, 0, 0, 1);

    [Header("=== SECURITY ===")]
    [Tooltip("Mật khẩu phòng (để trống = không cần mật khẩu).")]
    [SerializeField] private string roomPassword = "";
    [Tooltip("Cho phép client yêu cầu mật khẩu khi join.")]
    [SerializeField] private bool requirePasswordOnJoin = false;

    /// <summary>Connection timeout in milliseconds.</summary>
    private const int CONNECTION_TIMEOUT_MS = 15000;

    /// <summary>Tracking connected player count (updated in menu before scene load).</summary>
    private int _connectedPlayerCount = 0;

    // ══════════════ HOST MIGRATION STATE ══════════════
    [Header("=== HOST MIGRATION ===")]
    [Tooltip("Tự động chơi tiếp khi host migration xảy ra (client trở thành host mới).")]
    [SerializeField] private bool autoMigrateOnHostLeft = true;

    /// <summary>Lưu trạng thái trước khi host rời — dùng khi migrate.</summary>
    private HostMigrationToken _pendingMigrationToken;

    /// <summary>PlayerRef cũ của host trước migration (để skip despawn object cũ).</summary>
    private PlayerRef _previousHostPlayerRef;

    /// <summary>Đang trong quá trình host migration.</summary>
    private bool _isMigratingHost = false;

    /// <summary>Cờ tránh OnShutdown ghi đè panel khi LeaveRoom đã xử lý.</summary>
    private bool _isLeavingRoom = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Auto-attach debug overlay nếu chưa có (F3 để toggle)
        if (!TryGetComponent<MultiplayerDebugOverlay>(out _))
            gameObject.AddComponent<MultiplayerDebugOverlay>();

        // Load player name from PlayerPrefs
        string saved = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(saved))
        {
            if (playerNameInput != null)
                playerNameInput.text = saved;
            if (playerNameInputJoin != null)
                playerNameInputJoin.text = saved;
        }

        // Wire ENTER GAME button onClick (legacy, Panel_CreateRoom)
        if (enterGameButton != null)
        {
            var btn = enterGameButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(LoadGameAsHost);
            }
        }

        // Wire START GAME button onClick (Panel_ConnectedRoom, chỉ host thấy)
        if (startGameButton != null)
        {
            var btn = startGameButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(LoadGameAsHost);
            }
        }

        // Wire COPY button onClick
        if (copyCodeButton != null)
        {
            var btn = copyCodeButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(CopyRoomCode);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ───────────────────── HELPER ─────────────────────

    /// <summary>Tạo random room code (6 ký tự uppercase).</summary>
    static string GenerateRoomCode(int length = 6)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sb = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);
        return sb.ToString();
    }

    NetworkRunner CreateRunner()
    {
        NetworkRunner runner;
        if (runnerPrefab != null)
        {
            runner = Instantiate(runnerPrefab);
        }
        else
        {
            var go = new GameObject("NetworkRunner");
            runner = go.AddComponent<NetworkRunner>();
            go.AddComponent<NetworkSceneManagerDefault>();
        }

        runner.AddCallbacks(this);
        DontDestroyOnLoad(runner.gameObject);
        return runner;
    }

    /// <summary>Tìm build index của scene bằng tên (không cần path đầy đủ).</summary>
    int FindSceneBuildIndex(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (!string.IsNullOrEmpty(path) && path.Contains(sceneName))
                return i;
        }
        Debug.LogError($"[MultiplayerManager] Scene '{sceneName}' not found in Build Settings!");
        return -1;
    }

    // ═══════════════════════════════════════════════════════
    //  HOST FLOW
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Step 1: Create Fusion session (room) on Photon Cloud.
    /// Hiện room code + player list trên menu. CHƯA load gameplay scene.
    /// Client có thể join ngay — tên hiện trên player list.
    /// Host bấm "Enter Game" khi sẵn sàng → load scene cho tất cả.
    /// </summary>
    public async void CreateRoom()
    {
        IsHost = true;
        SavePlayerName();
        _connectedPlayerCount = 0;

        try
        {
            SetStatus("Creating room...");

            if (Runner != null && Runner.IsRunning)
            {
                await Runner.Shutdown();
                Runner = null;
            }

            Runner = CreateRunner();
            CurrentSessionName = GenerateRoomCode();

            // Không load scene ngay — giữ ở menu để chờ client
            var startTask = Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = CurrentSessionName,
                PlayerCount = 4,
                SceneManager = Runner.GetComponent<INetworkSceneManager>(),
                // Password support via CustomPhotonAppSettings (Advanced)
            });

            // Timeout 15s — tránh await vô hạn
            var completed = await Task.WhenAny(startTask, Task.Delay(CONNECTION_TIMEOUT_MS));
            if (completed != startTask)
            {
                SetStatus("Connection timed out! Check your internet.");
                if (Runner != null) { await Runner.Shutdown(); Runner = null; }
                return;
            }

            var result = await startTask;

            if (result.Ok)
            {
                // Room code sẽ hiện trên Panel_ConnectedRoom (connectedRoomCodeText)

                SetStatus($"Room: {CurrentSessionName} — Waiting for players...");
                Debug.Log($"[MultiplayerManager] Fusion Host created. Code: {CurrentSessionName}");

                // Copy to clipboard
                try { GUIUtility.systemCopyBuffer = CurrentSessionName; }
                catch (Exception) { /* ignore clipboard errors */ }
            }
            else
            {
                SetStatus($"Failed: {result.ShutdownReason}");
                Debug.LogError($"[MultiplayerManager] StartGame failed: {result.ShutdownReason}");
                return;
            }
        }
        catch (Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MultiplayerManager] CreateRoom exception: {e}");
            return;
        }

        // Host th\u00e0nh c\u00f4ng \u2192 chuy\u1ec3n sang Panel_ConnectedRoom (panel chung v\u1edbi client)
        _connectedPlayerCount = 1;
        ShowConnectedPanel(CurrentSessionName, isHost: true);
        AddConnectedPlayerToList(LocalPlayerName, true, Runner.LocalPlayer);
        UpdateConnectedPlayerCount(_connectedPlayerCount);
    }

    /// <summary>
    /// Copy room code hiện tại vào clipboard.
    /// </summary>
    public void CopyRoomCode()
    {
        if (string.IsNullOrEmpty(CurrentSessionName)) return;

        try
        {
            GUIUtility.systemCopyBuffer = CurrentSessionName;
            Debug.Log($"[MultiplayerManager] Copied room code: {CurrentSessionName}");

            // Visual feedback: thay đổi text tạm thời
            if (connectedRoomCodeText != null)
            {
                // Dừng coroutine cũ nếu đang chạy (tránh spam click)
                StopCoroutine(nameof(ResetCopyFeedback));

                connectedRoomCodeText.text = "✅ Copied!";
                connectedRoomCodeText.color = new Color(0.4f, 1f, 0.5f);

                // Reset sau 1.5s → luôn dùng CurrentSessionName (không dùng text cũ)
                StartCoroutine(ResetCopyFeedback(1.5f));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MultiplayerManager] Copy failed: {e.Message}");
        }
    }

    private System.Collections.IEnumerator ResetCopyFeedback(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (connectedRoomCodeText != null)
        {
            connectedRoomCodeText.text = CurrentSessionName;
            connectedRoomCodeText.color = new Color(1f, 0.84f, 0f);
        }
    }

    /// <summary>
    /// Step 2: Host bấm "ENTER GAME" → load gameplay scene cho TẤT CẢ players đã kết nối.
    /// Fusion sẽ tự đồng bộ scene — client tự load theo.
    /// </summary>
    /// <summary>Chặn LoadGameAsHost gọi 2 lần.</summary>
    private bool _isLoadingScene = false;

    public void LoadGameAsHost()
    {
        if (_isLoadingScene)
        {
            Debug.LogWarning("[MultiplayerManager] Already loading scene! Ignoring duplicate call.");
            return;
        }

        if (Runner == null || !Runner.IsRunning)
        {
            Debug.LogError("[MultiplayerManager] Runner not running! Call CreateRoom first.");
            return;
        }

        if (!Runner.IsServer)
        {
            Debug.LogError("[MultiplayerManager] Only the host can load scenes!");
            return;
        }

        int sceneIndex = FindSceneBuildIndex(gameplaySceneName);
        if (sceneIndex < 0)
        {
            Debug.LogError($"[MultiplayerManager] Scene '{gameplaySceneName}' NOT in Build Settings! Add it via File → Build Settings.");
            SetStatus($"Error: Scene '{gameplaySceneName}' not in Build Settings!");
            return;
        }

        _isLoadingScene = true; // Chặn gọi lần 2

        // Bắt đầu coroutine: hiện loading → chờ render → load scene
        StartCoroutine(LoadGameSequence(sceneIndex));
    }

    /// <summary>
    /// Coroutine: hiện loading panel → chờ render → load scene.
    ///
    /// StartGameObject = NetworkObject của NetworkLobbyManager trong gameplay scene.
    /// Fusion dùng StartGameObject để biết scene đích và đồng bộ cho client.
    /// Nếu StartGameObject không được set, dùng SceneRef.FromIndex đơn giản.
    /// </summary>
    private System.Collections.IEnumerator LoadGameSequence(int sceneIndex)
    {
        SetStatus("Loading game...");

        // 1. Ẩn panel phòng + hiện loading panel
        if (panelConnectedRoom != null) panelConnectedRoom.SetActive(false);
        ShowLoadingPanel();

        // 2. Chờ 2 frame để Unity render loading panel
        yield return null;
        yield return null;

        // 3. Log tất cả players
        int playerCount = 0;
        foreach (var p in Runner.ActivePlayers)
        {
            playerCount++;
            Debug.Log($"[MultiplayerManager] Player before scene load: {p} (PlayerId={p.PlayerId})");
        }
        Debug.Log($"[MultiplayerManager] Host loading '{gameplaySceneName}' (idx={sceneIndex}) for {playerCount} players...");

        // 4. Tìm StartGameObject — ƯU TIÊN: sceneReferencePrefab (prefab) > NetworkLobbyManager trong scene
        NetworkObject startObj = null;

        if (sceneReferencePrefab != null)
        {
            startObj = sceneReferencePrefab;
            Debug.Log($"[MultiplayerManager] Using sceneReferencePrefab: {startObj.name}");
        }
        else
        {
            // Fallback: tìm NetworkLobbyManager trong scene đích
            // NOTE: Khi còn ở menu, scene chưa load nên sẽ null → dùng simple LoadScene
            var lobbyMgr = FindFirstObjectByType<NetworkLobbyManager>();
            if (lobbyMgr != null)
                startObj = lobbyMgr.GetComponent<NetworkObject>();

            if (startObj != null)
                Debug.Log($"[MultiplayerManager] Using NetworkLobbyManager StartGameObject: {startObj.name}");
        }

        // 5. Load scene
        if (startObj != null)
        {
            Debug.Log($"[MultiplayerManager] Loading with StartGameObject: {startObj.name}");
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex),
                new LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single),
                startObj);
        }
        else
        {
            Debug.LogWarning($"[MultiplayerManager] No StartGameObject! Using simple scene load. Client timeout có thể xảy ra!");
            Runner.LoadScene(SceneRef.FromIndex(sceneIndex));
        }
    }

    /// <summary>
    /// Legacy: Create room + immediately load scene (backwards compatible).
    /// </summary>
    public async void StartHostAndLoadGame()
    {
        IsHost = true;
        SavePlayerName();

        SetStatus("Creating room...");

        if (Runner != null && Runner.IsRunning)
        {
            await Runner.Shutdown();
            Runner = null;
        }

        Runner = CreateRunner();
        CurrentSessionName = GenerateRoomCode();

        int sceneIndex = FindSceneBuildIndex(gameplaySceneName);
        var sceneInfo = new NetworkSceneInfo();
        if (sceneIndex >= 0)
            sceneInfo.AddSceneRef(SceneRef.FromIndex(sceneIndex), LoadSceneMode.Single);

        var result = await Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = CurrentSessionName,
            PlayerCount = 4,
            Scene = sceneInfo,
            SceneManager = Runner.GetComponent<INetworkSceneManager>()
        });

        if (result.Ok)
        {
            SetStatus($"Room Code: {CurrentSessionName}");
            Debug.Log($"[MultiplayerManager] Host created + loading. Code: {CurrentSessionName}");
        }
        else
        {
            SetStatus($"Failed: {result.ShutdownReason}");
            Debug.LogError($"[MultiplayerManager] StartGame failed: {result.ShutdownReason}");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  CLIENT FLOW
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Client nhập room code → kết nối Photon Cloud → join session.
    /// Fusion tự đồng bộ scene khi host load.
    /// </summary>
    public async void StartClientAndLoadGame()
    {
        IsHost = false;
        SavePlayerName();

        string code = joinCodeInput != null ? joinCodeInput.text : "";
        if (string.IsNullOrWhiteSpace(code))
        {
            SetStatus("Enter room code first!");
            return;
        }

        code = code.Trim().ToUpper();
        SetStatus("Connecting to Photon Cloud...");

        try
        {
            if (Runner != null && Runner.IsRunning)
            {
                await Runner.Shutdown();
                Runner = null;
            }

            Runner = CreateRunner();
            CurrentSessionName = code;

            var startTask = Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = code,
                SceneManager = Runner.GetComponent<INetworkSceneManager>()
            });

            // Timeout 15s
            var completed = await Task.WhenAny(startTask, Task.Delay(CONNECTION_TIMEOUT_MS));
            if (completed != startTask)
            {
                SetStatus("Connection timed out! Check room code and internet.");
                if (Runner != null) { await Runner.Shutdown(); Runner = null; }
                return;
            }

            var result = await startTask;

            if (result.Ok)
            {
                SetStatus("Connected! Waiting for host to start game...");
                Debug.Log($"[MultiplayerManager] Client joined session: {code}");

                // Chuyển panel: ẩn JoinRoom → hiện ConnectedRoom (client mode)
                ShowConnectedPanel(code, isHost: false);

                // Thêm TẤT CẢ player (bao gồm mình) theo thứ tự PlayerId
                // Đảm bảo thứ tự giống nhau giữa Host và Client:
                //   Host (PlayerId=1) luôn đứng đầu → rồi các player theo thứ tự join
                var sortedPlayers = new List<PlayerRef>();
                foreach (var p in Runner.ActivePlayers)
                    sortedPlayers.Add(p);
                sortedPlayers.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

                foreach (var p in sortedPlayers)
                {
                    bool isHostPlayer = (p.PlayerId == 1);
                    bool isSelf = (p == Runner.LocalPlayer);
                    string displayName;

                    if (isSelf)
                        displayName = LocalPlayerName;
                    else if (_receivedPlayerNames.TryGetValue(p, out var cachedName))
                        displayName = cachedName;
                    else
                        displayName = $"Player {p.PlayerId}";

                    AddConnectedPlayerToList(displayName, isHostPlayer, p);
                }

                _connectedPlayerCount = CountActivePlayers(Runner);
                UpdateConnectedPlayerCount(_connectedPlayerCount);

                // Gửi tên tới host qua ReliableData
                try
                {
                    byte[] nameBytes = Encoding.UTF8.GetBytes(LocalPlayerName);
                    Runner.SendReliableDataToServer(PLAYER_NAME_KEY, nameBytes);
                    Debug.Log($"[MultiplayerManager] Sent player name '{LocalPlayerName}' to host.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MultiplayerManager] Failed to send name: {ex.Message}");
                }
            }
            else
            {
                SetStatus($"Room not found or connection failed!");
                Debug.LogError($"[MultiplayerManager] Client join failed: {result.ShutdownReason}");
            }
        }
        catch (Exception e)
        {
            SetStatus($"Error: {e.Message}");
            Debug.LogError($"[MultiplayerManager] Client exception: {e}");
        }
    }

    // ═══════════════════════════════════════════════════════
    //  UI HELPERS
    // ═══════════════════════════════════════════════════════

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        if (statusTextJoin != null)
            statusTextJoin.text = msg;
        if (connectedStatusText != null)
            connectedStatusText.text = msg;
        Debug.Log($"[MultiplayerManager] {msg}");
    }

    /// <summary>Hiện panel loading từ GameMenuManager khi chuyển scene.</summary>
    private void ShowLoadingPanel()
    {
        var menuMgr = FindFirstObjectByType<GameMenuManager>();
        if (menuMgr != null && menuMgr.Panel_Loading != null)
        {
            menuMgr.Panel_Loading.SetActive(true);
            Debug.Log("[MultiplayerManager] 📺 Loading panel shown.");
        }
    }

    /// <summary>Ẩn panel loading.</summary>
    private void HideLoadingPanel()
    {
        var menuMgr = FindFirstObjectByType<GameMenuManager>();
        if (menuMgr != null && menuMgr.Panel_Loading != null)
        {
            menuMgr.Panel_Loading.SetActive(false);
            Debug.Log("[MultiplayerManager] 📺 Loading panel hidden.");
        }
    }

    /// <summary>
    /// Chuyển sang Panel_ConnectedRoom — panel chung cho cả host và client.
    /// Host thấy nút START GAME, client thấy nút LEAVE.
    /// </summary>
    private void ShowConnectedPanel(string roomCode, bool isHost)
    {
        // Ẩn các panel cũ
        if (panelCreateRoom != null)
            panelCreateRoom.SetActive(false);
        if (panelJoinRoom != null)
            panelJoinRoom.SetActive(false);

        // Hiện panel chung
        if (panelConnectedRoom != null)
        {
            panelConnectedRoom.SetActive(true);

            if (connectedRoomCodeText != null)
                connectedRoomCodeText.text = roomCode;

            if (connectedStatusText != null)
                connectedStatusText.text = isHost
                    ? "Room created! Waiting for players..."
                    : "Connected! Waiting for host to start game...";

            if (connectedPlayerCountText != null)
                connectedPlayerCountText.text = "";

            // Chỉ host thấy START GAME
            if (startGameButton != null)
                startGameButton.SetActive(isHost);

            // LEAVE hiện cho cả hai
            if (leaveRoomButton != null)
                leaveRoomButton.SetActive(true);
        }

        // Clear connected player list UI (nhưng GIỮ _receivedPlayerNames cache)
        ClearConnectedPlayerList();
        _connectedPlayerEntries.Clear();
        // KHÔNG clear _receivedPlayerNames — data nhận từ ReliableData trước ShowConnectedPanel
        // sẽ được dùng lại khi rebuild list sau đó
    }

    /// <summary>Host hoặc Client bấm Leave → disconnect và quay về panel ban đầu.</summary>
    public async void LeaveRoom()
    {
        Debug.Log("[MultiplayerManager] Leaving room...");
        bool wasHost = IsHost;
        _isLeavingRoom = true; // Ngăn OnShutdown ghi đè panel

        // Lưu game trước khi disconnect
        try { GameController.PlayerSave(); }
        catch (System.Exception) { /* GameController chưa load */ }

        if (Runner != null && Runner.IsRunning)
        {
            await Runner.Shutdown();
            Runner = null;
        }

        // Ẩn panel connected
        if (panelConnectedRoom != null)
            panelConnectedRoom.SetActive(false);

        // Quay về panel ban đầu: host → CreateRoom, client → JoinRoom
        if (wasHost && panelCreateRoom != null)
            panelCreateRoom.SetActive(true);
        else if (panelJoinRoom != null)
            panelJoinRoom.SetActive(true);

        SetStatus("");
        _connectedPlayerCount = 0;
        _connectedPlayerEntries.Clear();
        _receivedPlayerNames.Clear();
        _isLeavingRoom = false;

        // Reset CREATE ROOM button state
        if (createRoomButton != null) createRoomButton.SetActive(true);
        if (enterGameButton != null) enterGameButton.SetActive(false);
    }

    /// <summary>Từ Panel_JoinRoom, quay lại Panel_CreateRoom (nút BACK).</summary>
    public void BackToCreateRoom()
    {
        if (panelJoinRoom != null)
            panelJoinRoom.SetActive(false);
        if (panelCreateRoom != null)
            panelCreateRoom.SetActive(true);
    }

    /// <summary>Từ Panel_CreateRoom, chuyển sang Panel_JoinRoom (nút JOIN ROOM).</summary>
    public void GoToJoinRoom()
    {
        if (panelCreateRoom != null)
            panelCreateRoom.SetActive(false);
        if (panelJoinRoom != null)
            panelJoinRoom.SetActive(true);

        // Sync tên player sang join panel
        if (playerNameInput != null && playerNameInputJoin != null
            && !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            playerNameInputJoin.text = playerNameInput.text;
        }
    }

    // ───────── Connected Panel Player List Helpers ─────────

    private void ClearConnectedPlayerList()
    {
        if (connectedPlayerListContainer == null) return;
        for (int i = connectedPlayerListContainer.childCount - 1; i >= 0; i--)
            Destroy(connectedPlayerListContainer.GetChild(i).gameObject);
        _connectedPlayerEntries.Clear();
    }

    private void AddConnectedPlayerToList(string playerName, bool isHostPlayer, PlayerRef playerRef)
    {
        if (connectedPlayerListContainer == null) return;

        // Nếu player đã có entry → cập nhật tên thay vì tạo mới
        if (_connectedPlayerEntries.TryGetValue(playerRef, out var existingEntry))
        {
            if (existingEntry != null)
            {
                var existingText = existingEntry.GetComponent<TextMeshProUGUI>();
                if (existingText != null)
                {
                    string icon2 = isHostPlayer ? "\ud83d\udc51" : "\ud83c\udfae";
                    string role2 = isHostPlayer ? " (Host)" : "";
                    existingText.text = $"  {icon2}  {playerName}{role2}";
                }
            }
            return;
        }

        var entryGO = new GameObject($"Player_{connectedPlayerListContainer.childCount}",
            typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        entryGO.transform.SetParent(connectedPlayerListContainer, false);
        entryGO.layer = 5;

        var rt = entryGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 80);

        var text = entryGO.GetComponent<TextMeshProUGUI>();
        string icon = isHostPlayer ? "\ud83d\udc51" : "\ud83c\udfae";
        string role = isHostPlayer ? " (Host)" : "";
        text.text = $"  {icon}  {playerName}{role}";
        text.fontSize = 35;
        text.alignment = TextAlignmentOptions.Left;
        text.color = isHostPlayer ? new Color(1f, 0.84f, 0f) : Color.white;

        // Track entry by PlayerRef
        _connectedPlayerEntries[playerRef] = entryGO;
    }

    private void UpdateConnectedPlayerCount(int count)
    {
        if (connectedPlayerCountText != null)
            connectedPlayerCountText.text = $"Players: {count}/4";
    }

    /// <summary>
    /// Rebuild toàn bộ connected player list theo thứ tự PlayerId.
    /// Host (PlayerId=1) luôn đứng đầu, đảm bảo thứ tự giống nhau giữa Host và tất cả Client.
    /// </summary>
    private void RebuildConnectedPlayerList(NetworkRunner runner)
    {
        if (connectedPlayerListContainer == null || runner == null) return;

        // Clear UI nhưng giữ cached names
        ClearConnectedPlayerList();
        _connectedPlayerEntries.Clear();

        // Sort tất cả player theo PlayerId
        var sortedPlayers = new List<PlayerRef>();
        foreach (var p in runner.ActivePlayers)
            sortedPlayers.Add(p);
        sortedPlayers.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

        foreach (var p in sortedPlayers)
        {
            bool isHostPlayer = (p.PlayerId == 1);
            bool isSelf = (p == runner.LocalPlayer);
            string displayName;

            if (isSelf)
                displayName = LocalPlayerName;
            else if (_receivedPlayerNames.TryGetValue(p, out var cachedName))
                displayName = cachedName;
            else
                displayName = $"Player {p.PlayerId}";

            AddConnectedPlayerToList(displayName, isHostPlayer, p);
        }
    }

    private void SavePlayerName()
    {
        string name = "";

        // Đọc tên từ đúng input field tùy theo panel đang active
        // Host (CreateRoom) → playerNameInput, Client (JoinRoom) → playerNameInputJoin
        if (IsHost)
        {
            // Host: ưu tiên input trên Panel_CreateRoom
            if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
                name = playerNameInput.text.Trim();
            else if (playerNameInputJoin != null && !string.IsNullOrWhiteSpace(playerNameInputJoin.text))
                name = playerNameInputJoin.text.Trim();
        }
        else
        {
            // Client: ưu tiên input trên Panel_JoinRoom
            if (playerNameInputJoin != null && !string.IsNullOrWhiteSpace(playerNameInputJoin.text))
                name = playerNameInputJoin.text.Trim();
            else if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
                name = playerNameInput.text.Trim();
        }

        if (string.IsNullOrEmpty(name))
            name = $"Player_{UnityEngine.Random.Range(100, 999)}";

        LocalPlayerName = name;
        NetworkPlayerName.LocalPlayerName = name;
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();

        // Đồng bộ tên sang cả 2 input field
        if (playerNameInput != null) playerNameInput.text = name;
        if (playerNameInputJoin != null) playerNameInputJoin.text = name;

        Debug.Log($"[MultiplayerManager] Player name saved: '{name}' (IsHost={IsHost})");
    }

    private void ClearPlayerList()
    {
        if (playerListContainer == null) return;
        for (int i = playerListContainer.childCount - 1; i >= 0; i--)
            Destroy(playerListContainer.GetChild(i).gameObject);
    }

    private void AddPlayerToList(string playerName, bool isHost)
    {
        if (playerListContainer == null) return;

        var entryGO = new GameObject($"Player_{playerListContainer.childCount}",
            typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        entryGO.transform.SetParent(playerListContainer, false);
        entryGO.layer = 5;

        var rt = entryGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);

        var text = entryGO.GetComponent<TextMeshProUGUI>();
        string icon = isHost ? "\ud83d\udc51" : "\ud83c\udfae";
        string role = isHost ? " (Host)" : "";
        text.text = $"  {icon}  {playerName}{role}";
        text.fontSize = 26;
        text.alignment = TextAlignmentOptions.Left;
        text.color = isHost ? new Color(1f, 0.84f, 0f) : Color.white;
    }

    private void UpdatePlayerCount(int count)
    {
        if (playerCountText != null)
            playerCountText.text = $"Players: {count}/4";
    }

    // ═══════════════════════════════════════════════════════
    //  INetworkRunnerCallbacks — Photon Cloud events
    // ═══════════════════════════════════════════════════════

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[MultiplayerManager] Player {player} joined session.");

        if (runner.IsServer)
        {
            // Host: chỉ tăng count cho remote players (host đã count = 1 từ CreateRoom)
            if (player != runner.LocalPlayer)
            {
                _connectedPlayerCount++;
            }

            // Hiện player join trên connected panel (menu hoặc lobby)
            if (Instance != null && player != runner.LocalPlayer)
            {
                string displayName = _receivedPlayerNames.TryGetValue(player, out var pName)
                    ? pName
                    : $"Player {player.PlayerId} (connecting...)";
                AddConnectedPlayerToList(displayName, false, player);
                UpdateConnectedPlayerCount(_connectedPlayerCount);
                SetStatus($"Room: {CurrentSessionName} — {_connectedPlayerCount} players connected");
            }

            // Spawn player prefab nếu đã ở trong gameplay scene
            if (playerPrefab != null && IsInGameplayScene() && !_spawnedPlayers.ContainsKey(player))
            {
                int playerIndex = player.PlayerId - 1;
                Vector3 pos;
                Quaternion rot;

                if (PlayerSpawnConfig.SpawnPointCount > 0)
                {
                    var spawnPoint = PlayerSpawnConfig.GetSpawnPoint(playerIndex);
                    if (spawnPoint != null)
                    {
                        pos = spawnPoint.position;
                        rot = spawnPoint.rotation;
                    }
                    else
                    {
                        pos = PlayerSpawnConfig.GetSpawnPosition(playerIndex);
                        rot = Quaternion.identity;
                    }
                }
                else
                {
                    pos = GetFallbackSpawnPosition(player.PlayerId);
                    rot = Quaternion.identity;
                    Debug.LogWarning($"[MultiplayerManager] No spawn points for late joiner! Fallback pos={pos}");
                }

                var obj = runner.Spawn(playerPrefab, pos, rot, player);
                _spawnedPlayers[player] = obj;
                runner.SetPlayerObject(player, obj);
                Debug.Log($"[MultiplayerManager] Spawned player prefab for {player} at {pos} (OnPlayerJoined)");
            }

            // Host gửi tên của mình cho client mới join
            try
            {
                byte[] nameBytes = Encoding.UTF8.GetBytes(LocalPlayerName);
                runner.SendReliableDataToPlayer(player, PLAYER_NAME_KEY, nameBytes);
                Debug.Log($"[MultiplayerManager] Sent host name '{LocalPlayerName}' to {player}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MultiplayerManager] Failed to send host name: {ex.Message}");
            }

            // Host gửi TẤT CẢ tên player đã cached cho client mới join
            // Để client mới thấy tên các player đã có trong phòng
            if (player != runner.LocalPlayer)
            {
                foreach (var kvp in _receivedPlayerNames)
                {
                    if (kvp.Key == player) continue; // Không gửi tên của chính họ lại
                    try
                    {
                        byte[] relayData = BuildRelayNameData(kvp.Key, kvp.Value);
                        runner.SendReliableDataToPlayer(player, PLAYER_NAME_RELAY_KEY, relayData);
                        Debug.Log($"[MultiplayerManager] Sent cached name '{kvp.Value}' (player {kvp.Key}) to new joiner {player}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MultiplayerManager] Failed to relay cached name: {ex.Message}");
                    }
                }
            }
        }
        else
        {
            // Client side: rebuild toàn bộ list theo thứ tự PlayerId
            // Đảm bảo thứ tự đồng bộ với Host (Host luôn đứng đầu)
            if (player != runner.LocalPlayer)
            {
                _connectedPlayerCount = CountActivePlayers(runner);
                UpdateConnectedPlayerCount(_connectedPlayerCount);

                // Rebuild toàn bộ list theo thứ tự PlayerId
                RebuildConnectedPlayerList(runner);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[MultiplayerManager] Player {player} left.");

        // ─── Kiểm tra host rời ───
        if (_previousHostPlayerRef != PlayerRef.None && player == _previousHostPlayerRef)
        {
            Debug.Log("[MultiplayerManager] ⚠️ Host player rời phòng!");
            // Host migration sẽ được xử lý bởi Fusion callback OnHostMigration
            // Tại đây chỉ cập nhật UI
        }

        if (runner.IsServer)
        {
            _connectedPlayerCount = Mathf.Max(1, _connectedPlayerCount - 1);

            // Despawn player object nếu có
            if (_spawnedPlayers.TryGetValue(player, out var obj))
            {
                if (obj != null)
                    runner.Despawn(obj);
                _spawnedPlayers.Remove(player);
            }

            // Xóa entry UI khỏi connected panel player list
            if (_connectedPlayerEntries.TryGetValue(player, out var entryGO))
            {
                if (entryGO != null)
                    Destroy(entryGO);
                _connectedPlayerEntries.Remove(player);
            }
            _receivedPlayerNames.Remove(player);

            // Update connected panel UI
            UpdateConnectedPlayerCount(_connectedPlayerCount);
            SetStatus($"Room: {CurrentSessionName} — {_connectedPlayerCount} players connected");
        }
        else
        {
            // Client side: cập nhật UI khi player khác rời
            if (player != runner.LocalPlayer)
            {
                // Xóa entry UI
                if (_connectedPlayerEntries.TryGetValue(player, out var entryGO))
                {
                    if (entryGO != null)
                        Destroy(entryGO);
                    _connectedPlayerEntries.Remove(player);
                }
                _receivedPlayerNames.Remove(player);

                _connectedPlayerCount = CountActivePlayers(runner);
                UpdateConnectedPlayerCount(_connectedPlayerCount);
            }
        }

        // ─── Client: thông báo nếu host rời ───
        if (!runner.IsServer && _previousHostPlayerRef != PlayerRef.None && player == _previousHostPlayerRef)
        {
            Debug.Log("[MultiplayerManager] ⚠️ Host rời! Đang chờ Host Migration...");
            SetStatus("Host left. Reconnecting...");
            TryReconnectAfterHostMigration();
        }
    }

    /// <summary>Kiểm tra đang ở gameplay scene hay menu.</summary>
    private bool IsInGameplayScene()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        return activeScene.Contains(gameplaySceneName) || activeScene == gameplaySceneName;
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        string role = runner.IsServer ? "HOST" : "CLIENT";
        Debug.Log($"[MultiplayerManager] ✅ Scene load done! Scene='{sceneName}', Role={role}");

        // Log tất cả players
        int count = 0;
        foreach (var p in runner.ActivePlayers)
        {
            count++;
            Debug.Log($"[MultiplayerManager] → Active player: {p} (PlayerId={p.PlayerId})");
        }
        Debug.Log($"[MultiplayerManager] Total active players: {count}");

        // ── Chờ scene thực sự sẵn sàng trước khi spawn ──
        StartCoroutine(PostSceneLoadSequence(runner));
    }

    private System.Collections.IEnumerator PostSceneLoadSequence(NetworkRunner runner)
    {
        // Đợi scene active
        while (SceneManager.sceneCount < 1)
            yield return null;

        // Đợi thêm 1 frame để scene fully instantiated
        yield return null;

        // Đăng ký DungeonWaveManager callbacks
        RegisterDungeonWaveManager(runner);

        // Ẩn loading panel
        HideLoadingPanel();

        // Reset flag
        _isLoadingScene = false;

        // Spawn player cho server
        if (runner.IsServer && playerPrefab != null && IsInGameplayScene())
        {
            Debug.Log($"[MultiplayerManager] [HOST] Spawning player prefabs...");

            // Clear danh sách cũ
            _spawnedPlayers.Clear();

            int spawnCount = PlayerSpawnConfig.SpawnPointCount;
            if (spawnCount == 0)
                Debug.LogWarning("[MultiplayerManager] No spawn points! Using fallback.");

            foreach (var player in runner.ActivePlayers)
            {
                // Kiểm tra đã spawn chưa
                if (_spawnedPlayers.ContainsKey(player))
                {
                    Debug.Log($"[MultiplayerManager] Player {player} already spawned, skipping.");
                    continue;
                }

                int playerIndex = player.PlayerId - 1;
                Vector3 pos;
                Quaternion rot;

                if (spawnCount > 0)
                {
                    var spawnPoint = PlayerSpawnConfig.GetSpawnPoint(playerIndex);
                    if (spawnPoint != null)
                    {
                        pos = spawnPoint.position;
                        rot = spawnPoint.rotation;
                    }
                    else
                    {
                        pos = PlayerSpawnConfig.GetSpawnPosition(playerIndex);
                        rot = Quaternion.identity;
                        Debug.LogWarning($"[MultiplayerManager] SpawnPoint[{playerIndex}] null! Fallback: {pos}");
                    }
                }
                else
                {
                    pos = GetFallbackSpawnPosition(player.PlayerId);
                    rot = Quaternion.identity;
                }

                var obj = runner.Spawn(playerPrefab, pos, rot, player);
                _spawnedPlayers[player] = obj;
                runner.SetPlayerObject(player, obj);

                Debug.Log($"[MultiplayerManager] ✅ Spawned {player} at {pos}");
            }
        }
        else if (!runner.IsServer)
        {
            Debug.Log($"[MultiplayerManager] [CLIENT] Scene loaded — waiting for host spawn.");
        }
        else if (playerPrefab == null)
        {
            Debug.LogError($"[MultiplayerManager] ❌ playerPrefab is NULL! Assign in Inspector!");
        }
    }

    private void RegisterDungeonWaveManager(NetworkRunner runner)
    {
        if (runner == null)
            runner = Runner;

        if (runner == null) return;

        var waveMgr = FindFirstObjectByType<DungeonWaveManager>();
        if (waveMgr != null)
        {
            runner.RemoveCallbacks(waveMgr);
            runner.AddCallbacks(waveMgr);
            Debug.Log("[MultiplayerManager] DungeonWaveManager registered");
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        string role = runner.IsServer ? "HOST" : "CLIENT";
        Debug.Log($"[MultiplayerManager] 🔄 Scene load START — Role={role}");

        // Client: hiện loading panel khi bắt đầu load
        // Host: đã hiện trong LoadGameSequence rồi, không cần gọi lại
        if (!runner.IsServer)
        {
            SetStatus("Loading game world...");
            ShowLoadingPanel();
        }
    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[MultiplayerManager] Runner shutdown: {shutdownReason}");
        Runner = null;
        _spawnedPlayers.Clear();
        _connectedPlayerEntries.Clear();
        _receivedPlayerNames.Clear();
        _connectedPlayerCount = 0;

        // ─── HOST MIGRATION: Client được promote thành host mới ───
        if (_isMigratingHost && _pendingMigrationToken != null)
        {
            _isMigratingHost = false;
            Debug.Log("[MultiplayerManager] Host migration: Runner bị shutdown sau migration — sẽ ResumeAsHost()");
            // Runner đã shutdown → đợi Fusion tự ResumeAsHost() hoặc gọi lại CreateRoom
            // Đặt cờ để luồng tiếp theo tự động xử lý
            return;
        }

        // ─── Mất kết nối thông thường ───
        // Lưu game khi shutdown
        try { GameController.PlayerSave(); }
        catch (System.Exception) { /* GameController chưa load */ }

        // Nếu đang ở gameplay scene → quay về menu
        if (IsInGameplayScene())
        {
            Debug.Log("[MultiplayerManager] Returning to menu scene...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
        else if (!_isLeavingRoom)
        {
            // Chỉ xử lý panel nếu KHÔNG phải do LeaveRoom gọi
            // (LeaveRoom đã tự xử lý panel rồi)
            if (panelConnectedRoom != null) panelConnectedRoom.SetActive(false);
            if (IsHost && panelCreateRoom != null)
                panelCreateRoom.SetActive(true);
            else if (!IsHost && panelJoinRoom != null)
                panelJoinRoom.SetActive(true);
        }
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[MultiplayerManager] Connected to Photon Cloud server.");
        if (!runner.IsServer)
            SetStatus("Connected! Waiting for host to start game...");

        // ─── Lưu PlayerRef của host (để phát hiện host rời) ───
        // Host là PlayerRef with PlayerId == 1 trong hầu hết trường hợp
        // Hoặc dùng IsHost property
        foreach (var p in runner.ActivePlayers)
        {
            if (runner.IsServer && p == runner.LocalPlayer)
            {
                // Tôi là host
                _previousHostPlayerRef = runner.LocalPlayer;
                Debug.Log($"[MultiplayerManager] I am the host: {runner.LocalPlayer}");
            }
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[MultiplayerManager] Disconnected: {reason}");
        SetStatus($"Disconnected: {reason}");

        // Lưu game
        try { GameController.PlayerSave(); }
        catch (System.Exception) { /* ignore */ }

        // Nếu đang ở gameplay scene → quay về menu
        if (IsInGameplayScene())
        {
            Debug.Log("[MultiplayerManager] Returning to menu scene...");
            SceneManager.LoadScene(0); // Scene index 0 = menu
        }
        else
        {
            // Còn ở menu → ẩn connected panel, hiện join panel
            if (panelConnectedRoom != null) panelConnectedRoom.SetActive(false);
            if (panelJoinRoom != null) panelJoinRoom.SetActive(true);
        }
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[MultiplayerManager] Connect failed: {reason}");
        SetStatus($"Connection failed! Check room code.");
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log($"[MultiplayerManager] ⚡ OnHostMigration — token={hostMigrationToken}, IsHost={IsHost}, runner.State={runner.State}");

        _isMigratingHost = true;
        _pendingMigrationToken = hostMigrationToken;

        if (runner.IsServer)
        {
            // Server cũ rời → một trong những client sẽ nhận token này
            Debug.Log("[MultiplayerManager] Host migration: Tôi là server cũ — chuẩn bị handover");
        }
        else
        {
            // Client: server cũ rời → sẽ nhận server role
            Debug.Log("[MultiplayerManager] Host migration: Tôi là client — chờ được promote thành host mới");
        }
    }

    /// <summary>
    /// Gọi sau OnHostMigration để client mới trở thành host.
    /// Fusion sẽ tự động gọi khi runner nhận được token.
    /// </summary>
    public async void ResumeAsHost(NetworkRunner runner, HostMigrationToken token)
    {
        Debug.Log("[MultiplayerManager] 🔄 ResumeAsHost — Client promoted to new Host!");

        try
        {
            // Tạo runner mới và resume với role host
            if (Runner != null && Runner.IsRunning)
                await Runner.Shutdown();

            Runner = runner; // Dùng runner đã được Fusion setup sẵn

            var result = await Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = CurrentSessionName,
                PlayerCount = 4,
                SceneManager = Runner.GetComponent<INetworkSceneManager>(),
                HostMigrationToken = token
            });

            if (result.Ok)
            {
                IsHost = true;
                Debug.Log($"[MultiplayerManager] ✅ Host migration complete! New host: {Runner.LocalPlayer}");
                SetStatus("Host migration complete! You are now the host.");
            }
            else
            {
                Debug.LogError($"[MultiplayerManager] ❌ Host migration failed: {result.ShutdownReason}");
                SetStatus($"Migration failed: {result.ShutdownReason}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MultiplayerManager] Host migration exception: {e}");
            SetStatus($"Migration error: {e.Message}");
        }
    }

    /// <summary>
    /// Thử tự động khôi phục kết nối sau khi bị disconnect.
    /// Gọi khi OnDisconnectedFromServer thấy lý do liên quan host migration.
    /// </summary>
    public async void TryReconnectAfterHostMigration()
    {
        if (string.IsNullOrEmpty(CurrentSessionName))
        {
            Debug.LogWarning("[MultiplayerManager] No session name to reconnect to!");
            return;
        }

        Debug.Log($"[MultiplayerManager] 🔄 Trying to reconnect to session '{CurrentSessionName}'...");

        if (Runner != null && Runner.IsRunning)
            await Runner.Shutdown();

        Runner = CreateRunner();

        var startTask = Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = CurrentSessionName,
            SceneManager = Runner.GetComponent<INetworkSceneManager>()
        });

        var completed = await Task.WhenAny(startTask, Task.Delay(CONNECTION_TIMEOUT_MS));
        if (completed != startTask)
        {
            SetStatus("Reconnection timed out!");
            return;
        }

        var result = await startTask;
        if (result.Ok)
        {
            SetStatus("Reconnected! Waiting for host...");
            Debug.Log("[MultiplayerManager] ✅ Reconnected to migrated session");
        }
        else
        {
            SetStatus($"Reconnection failed: {result.ShutdownReason}");
            Debug.LogError($"[MultiplayerManager] ❌ Reconnect failed: {result.ShutdownReason}");
        }
    }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        if (data.Count == 0) return;

        // ─── PLAYER_NAME_KEY: tên trực tiếp (client→host hoặc host→client) ───
        if (key.Equals(PLAYER_NAME_KEY))
        {
            string playerName = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);

            if (runner.IsServer)
            {
                // Host nhận tên player từ client qua ReliableData
                Debug.Log($"[MultiplayerManager] Received player name '{playerName}' from {player}");

                // Lưu cache + cập nhật tên trên connected panel
                _receivedPlayerNames[player] = playerName;
                AddConnectedPlayerToList(playerName, false, player);

                // ★ Relay tên client này cho TẤT CẢ client khác
                foreach (var p in runner.ActivePlayers)
                {
                    if (p == runner.LocalPlayer) continue; // Không gửi cho chính host
                    if (p == player) continue;              // Không gửi lại cho người gửi
                    try
                    {
                        byte[] relayData = BuildRelayNameData(player, playerName);
                        runner.SendReliableDataToPlayer(p, PLAYER_NAME_RELAY_KEY, relayData);
                        Debug.Log($"[MultiplayerManager] Relayed name '{playerName}' (player {player}) to {p}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MultiplayerManager] Failed to relay name to {p}: {ex.Message}");
                    }
                }
            }
            else
            {
                // Client nhận tên host từ server
                Debug.Log($"[MultiplayerManager] Received host name '{playerName}' from server");
                _receivedPlayerNames[player] = playerName;
                AddConnectedPlayerToList(playerName, true, player);
                _connectedPlayerCount = CountActivePlayers(runner);
                UpdateConnectedPlayerCount(_connectedPlayerCount);
            }
            return;
        }

        // ─── PLAYER_NAME_RELAY_KEY: host relay tên của player khác cho client ───
        if (key.Equals(PLAYER_NAME_RELAY_KEY))
        {
            if (runner.IsServer) return; // Host không cần xử lý relay

            var (sourcePlayerId, playerName) = ParseRelayNameData(data);
            if (string.IsNullOrEmpty(playerName)) return;

            // Tìm PlayerRef từ PlayerId trong ActivePlayers
            PlayerRef sourcePlayer = default;
            bool found = false;
            foreach (var p in runner.ActivePlayers)
            {
                if (p.PlayerId == sourcePlayerId)
                {
                    sourcePlayer = p;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"[MultiplayerManager] Relay: Player with ID {sourcePlayerId} not found in ActivePlayers");
                return;
            }

            Debug.Log($"[MultiplayerManager] Received relayed name '{playerName}' for player {sourcePlayer}");

            _receivedPlayerNames[sourcePlayer] = playerName;
            bool isHostPlayer = (sourcePlayerId == 1);
            AddConnectedPlayerToList(playerName, isHostPlayer, sourcePlayer);
            _connectedPlayerCount = CountActivePlayers(runner);
            UpdateConnectedPlayerCount(_connectedPlayerCount);
        }
    }

    /// <summary>Encode [4 bytes PlayerId][UTF8 name] cho relay.</summary>
    private byte[] BuildRelayNameData(PlayerRef playerRef, string name)
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);
        byte[] result = new byte[4 + nameBytes.Length];
        BitConverter.GetBytes(playerRef.PlayerId).CopyTo(result, 0);
        nameBytes.CopyTo(result, 4);
        return result;
    }

    /// <summary>Decode [4 bytes PlayerId][UTF8 name] từ relay data.</summary>
    private (int playerId, string name) ParseRelayNameData(ArraySegment<byte> data)
    {
        if (data.Count < 5) return (0, null); // Ít nhất 4 bytes ID + 1 byte tên
        int playerId = BitConverter.ToInt32(data.Array, data.Offset);
        string name = Encoding.UTF8.GetString(data.Array, data.Offset + 4, data.Count - 4);
        return (playerId, name);
    }

    /// <summary>Đếm số player active trong session.</summary>
    private int CountActivePlayers(NetworkRunner runner)
    {
        int count = 0;
        if (runner != null)
        {
            foreach (var p in runner.ActivePlayers)
                count++;
        }
        return Mathf.Max(1, count);
    }

    /// <summary>Fallback spawn position: phân bổ theo vòng tròn quanh (0,0,0).</summary>
    private Vector3 GetFallbackSpawnPosition(int playerId)
    {
        float angle = (playerId - 1) * (360f / 4f) * Mathf.Deg2Rad;
        float radius = 3f;
        return new Vector3(Mathf.Cos(angle) * radius, 1f, Mathf.Sin(angle) * radius);
    }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
