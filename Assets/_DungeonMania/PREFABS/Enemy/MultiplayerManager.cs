using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Gắn lên Canvas menu (Pre Start / Canvas_Menu). Nút Create Host / Join Host gọi
/// <see cref="StartHostAndLoadGame"/> / <see cref="StartClientAndLoadGame"/> hoặc
/// <see cref="CommitPendingJoinFromMenu"/> trước <see cref="SceneTransitionManager.GoToScene"/>.
/// NetworkManager chỉ ở scene gameplay; <see cref="NetworkHostBootstrap"/> / <see cref="NetworkClientBootstrap"/> gọi Start sau khi load.
///
/// --- RELAY ---
/// Khi useRelay = true:
///   Host: CreateRelayAsync → lưu relay data static → LoadScene → bootstrap đọc relay data → StartHost
///   Client: JoinRelayAsync(joinCode) → lưu relay data → LoadScene → bootstrap đọc → StartClient
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    static bool _pendingHost;
    static bool _pendingClient;
    static string _pendingConnectAddress = "127.0.0.1";
    /// <summary>0 = cổng từ scene (thường 7777; không offset ParrelSync cho client). >0 = ép cổng (vd. 7778 khi join host clone cùng máy).</summary>
    static ushort _pendingConnectPort;

    /// <summary>Đặt true trước khi LoadScene; bootstrap gameplay đọc và gọi StartHost một lần.</summary>
    public static bool PendingStartHostAfterSceneLoad => _pendingHost;

    /// <summary>Đặt true trước khi LoadScene; bootstrap gameplay đọc và gọi StartClient một lần.</summary>
    public static bool PendingStartClientAfterSceneLoad => _pendingClient;

    /// <summary>Địa chỉ dùng khi Join (đã set trước khi load scene).</summary>
    public static string PendingConnectAddress => _pendingConnectAddress;

    /// <summary>0 = auto; >0 = cổng kết nối tới host (đọc trong NetworkClientBootstrap).</summary>
    public static ushort PendingConnectPort => _pendingConnectPort;

    /// <summary>True nếu dùng Unity Relay (internet). False = LAN trực tiếp.</summary>
    public static bool UsingRelay { get; private set; }

    [Tooltip("Tên scene trong Build Settings (không cần đường dẫn).")]
    [SerializeField] private string gameplaySceneName = "Map_Chinh";

    [Tooltip("IP host khi bấm Join (LAN hoặc 127.0.0.1).")]
    [SerializeField] private string connectAddress = "127.0.0.1";

    [Tooltip("Cổng host. 0 = dùng cổng scene (7777). Không dùng offset ParrelSync cho client — join Editor từ clone: để 0. Join host clone cùng máy (vd. 7778): nhập cổng đó.")]
    [SerializeField] private ushort connectPort = 0;

    [Header("=== RELAY SETTINGS ===")]
    [Tooltip("Bật Unity Relay (online qua internet). Tắt = chơi LAN.")]
    [SerializeField] private bool useRelay = true;

    [Tooltip("Input field để nhập Join Code (gán từ tab_HostOptions).")]
    [SerializeField] private TMP_InputField joinCodeInput;

    [Tooltip("Text hiển thị Join Code sau khi host tạo phòng.")]
    [SerializeField] private TextMeshProUGUI joinCodeDisplay;

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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Load player name from PlayerPrefs
        string saved = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(saved))
        {
            if (playerNameInput != null)
                playerNameInput.text = saved;
            if (playerNameInputJoin != null)
                playerNameInputJoin.text = saved;
        }

        // Wire ENTER GAME button onClick
        if (enterGameButton != null)
        {
            var btn = enterGameButton.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(LoadGameAsHost);
            }
        }


    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ───────────────────── HOST ─────────────────────

    /// <summary>
    /// Step 1: Create Relay room, show join code. Does NOT load scene yet.
    /// Call LoadGameAsHost() to actually load the gameplay scene.
    /// </summary>
    public async void CreateRoom()
    {
        SavePlayerName();
        UsingRelay = useRelay;

        if (useRelay)
        {
            // Check RelayManager exists
            if (RelayManager.Instance == null)
            {
                SetStatus("ERROR: RelayManager not found! Add RelayManager to scene.");
                Debug.LogError("[MultiplayerManager] RelayManager.Instance is null! Create a GameObject with RelayManager script.");
                return;
            }

            try
            {
                SetStatus("Creating room...");
                string joinCode = await RelayManager.Instance.CreateRelayAsync();
                if (joinCode == null)
                {
                    SetStatus("Failed to create room! Check internet.");
                    return;
                }

                // Show join code
                if (joinCodeDisplay != null)
                    joinCodeDisplay.text = joinCode;

                SetStatus($"Room Code: {joinCode}");
                Debug.Log($"[MultiplayerManager] Relay Host created. Code: {joinCode}");

                // Copy to clipboard (safe)
                try { GUIUtility.systemCopyBuffer = joinCode; }
                catch (System.Exception) { /* ignore clipboard errors */ }
            }
            catch (System.Exception e)
            {
                SetStatus($"Error: {e.Message}");
                Debug.LogError($"[MultiplayerManager] CreateRoom exception: {e}");
                return;
            }
        }
        else
        {
            SetStatus("Room created (LAN).");
        }

        // Swap buttons: hide CREATE ROOM, show ENTER GAME (enabled immediately)
        if (createRoomButton != null) createRoomButton.SetActive(false);
        if (enterGameButton != null)
            enterGameButton.SetActive(true);

        // Show host in player list
        AddPlayerToList(NetworkPlayerName.LocalPlayerName, true);
        UpdatePlayerCount(1);
    }

    /// <summary>
    /// Step 2: Load gameplay scene as Host. Call after CreateRoom().
    /// Can also be called directly for backwards compatibility.
    /// </summary>
    public void LoadGameAsHost()
    {
        // Host loads scene immediately. NetworkLobbyManager in gameplay scene
        // will handle waiting for players before starting the actual game.
        _pendingClient = false;
        _pendingConnectPort = 0;
        _pendingHost = true;
        Debug.Log("[MultiplayerManager] Host loading gameplay scene...");
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Legacy: Create room + immediately load scene (backwards compatible).
    /// </summary>
    public async void StartHostAndLoadGame()
    {
        SavePlayerName();
        UsingRelay = useRelay;

        if (useRelay)
        {
            SetStatus("Creating room...");
            string joinCode = await RelayManager.Instance.CreateRelayAsync();
            if (joinCode == null)
            {
                SetStatus("Failed to create room! Check internet.");
                return;
            }

            if (joinCodeDisplay != null)
                joinCodeDisplay.text = joinCode;

            SetStatus($"Room Code: {joinCode}");
            Debug.Log($"[MultiplayerManager] Relay Host created. Code: {joinCode}");
        }

        _pendingClient = false;
        _pendingConnectPort = 0;
        _pendingHost = true;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    // ───────────────────── CLIENT ─────────────────────

    /// <summary>Join relay room and load gameplay scene.</summary>
    public async void StartClientAndLoadGame()
    {
        SavePlayerName();
        UsingRelay = useRelay;

        if (useRelay)
        {
            string code = joinCodeInput != null ? joinCodeInput.text : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("Enter room code first!");
                return;
            }

            SetStatus("Connecting...");
            bool ok = await RelayManager.Instance.JoinRelayAsync(code);
            if (!ok)
            {
                SetStatus("Connection failed! Check room code.");
                return;
            }

            SetStatus("Connected. Loading...");
        }

        CommitPendingJoinFromMenu();
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    /// <summary>
    /// Gọi khi còn Instance (trước SceneTransitionManager.GoToScene): copy IP/cổng vào static để sau khi load Map_Chinh vẫn biết cấu hình join.
    /// </summary>
    public void CommitPendingJoinFromMenu()
    {
        _pendingHost = false;
        _pendingConnectAddress = string.IsNullOrWhiteSpace(connectAddress) ? "127.0.0.1" : connectAddress.Trim();
        _pendingConnectPort = connectPort;
        _pendingClient = true;
    }

    // ───────────────────── STATIC HELPERS ─────────────────────

    /// <summary>Gọi trước khi load Map_Chinh từ menu (SceneTransitionManager / async).</summary>
    public static void SetPendingStartHostAfterSceneLoad(bool value)
    {
        _pendingHost = value;
        if (value)
        {
            _pendingClient = false;
            _pendingConnectPort = 0;
        }
    }

    public static void ClearPendingHostFlag()
    {
        _pendingHost = false;
    }

    public static void ClearPendingClientFlag()
    {
        _pendingClient = false;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        if (statusTextJoin != null)
            statusTextJoin.text = msg;
        Debug.Log($"[MultiplayerManager] {msg}");
    }

    private void SavePlayerName()
    {
        // Try Join panel input first (for client), then Host panel input
        string name = "";
        if (playerNameInputJoin != null && !string.IsNullOrWhiteSpace(playerNameInputJoin.text))
            name = playerNameInputJoin.text.Trim();
        else if (playerNameInput != null)
            name = playerNameInput.text.Trim();

        if (string.IsNullOrEmpty(name))
            name = $"Player_{Random.Range(100, 999)}";

        NetworkPlayerName.LocalPlayerName = name;
        PlayerPrefs.SetString("PlayerName", name);
        PlayerPrefs.Save();
        Debug.Log($"[MultiplayerManager] Player name: {name}");
    }

    private void AddPlayerToList(string playerName, bool isHost)
    {
        if (playerListContainer == null) return;

        // Clear placeholder entries (italic "Waiting...")
        for (int i = playerListContainer.childCount - 1; i >= 0; i--)
        {
            var child = playerListContainer.GetChild(i);
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null && (tmp.fontStyle & FontStyles.Italic) != 0)
                Destroy(child.gameObject);
        }

        // Create entry
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
}
