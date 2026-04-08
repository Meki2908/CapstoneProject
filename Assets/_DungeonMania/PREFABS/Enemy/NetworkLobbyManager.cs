using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Lobby overlay trong gameplay scene.
/// Flow:
///   1. Host/Client load vào Map_Chinh → NetworkManager.Start
///   2. Mỗi player connected → gửi tên qua ServerRpc → server thêm vào NetworkList
///   3. UI hiện danh sách player
///   4. Host bấm "Bắt đầu" (khi >= minPlayers) → ServerRpc → tất cả ẩn lobby
///   5. Gameplay bắt đầu
///
/// Gắn trên GameObject trong gameplay scene (cùng NetworkManager hoặc riêng).
/// </summary>
public class NetworkLobbyManager : NetworkBehaviour
{
    public static NetworkLobbyManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Số player tối thiểu để bắt đầu (kể cả host).")]
    [SerializeField] private int minPlayers = 2;
    [Tooltip("Số player tối đa (kể cả host).")]
    [SerializeField] private int maxPlayers = 4;

    [Header("UI References (gán trong Inspector)")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI startButtonText;
    [SerializeField] private TextMeshProUGUI statusText;

    /// <summary>Danh sách tên player (sync qua network).</summary>
    private NetworkList<FixedString64Bytes> _playerNames;

    /// <summary>True khi game đã bắt đầu (lobby ẩn).</summary>
    public NetworkVariable<bool> GameStarted { get; private set; } = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Event khi game bắt đầu (lobby ẩn, gameplay chạy).</summary>
    public event Action OnGameStarted;

    private List<GameObject> _spawnedItems = new List<GameObject>();
    // Map clientId → index in _playerNames for removal
    private Dictionary<ulong, int> _clientIdToNameIndex = new Dictionary<ulong, int>();
    private int _nextNameIndex = 0;

    void Awake()
    {
        Instance = this;
        _playerNames = new NetworkList<FixedString64Bytes>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        // Listen for player list changes
        _playerNames.OnListChanged += OnPlayerListChanged;
        GameStarted.OnValueChanged += OnGameStartedChanged;

        if (IsServer)
        {
            // Server: listen for connects/disconnects
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Add host as first player
            string hostName = NetworkPlayerName.LocalPlayerName;
            if (string.IsNullOrEmpty(hostName)) hostName = "Host";
            _playerNames.Add(new FixedString64Bytes(hostName));
            _clientIdToNameIndex[NetworkManager.LocalClientId] = _nextNameIndex++;
        }

        if (IsClient && !IsServer)
        {
            // Client: gửi tên tới server
            string myName = NetworkPlayerName.LocalPlayerName;
            if (string.IsNullOrEmpty(myName)) myName = $"Player_{NetworkManager.LocalClientId}";
            RegisterPlayerNameServerRpc(new FixedString64Bytes(myName));
        }

        // Setup UI
        SetupLobbyUI();

        // Nếu game đã bắt đầu (reconnect) → ẩn lobby
        if (GameStarted.Value)
        {
            HideLobby();
        }
    }

    public override void OnNetworkDespawn()
    {
        _playerNames.OnListChanged -= OnPlayerListChanged;
        GameStarted.OnValueChanged -= OnGameStartedChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ───────────────── SERVER EVENTS ─────────────────

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        Debug.Log($"[Lobby] Client {clientId} connected. Waiting for name...");
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"[Lobby] Client {clientId} disconnected.");

        // Remove player name from list
        if (_clientIdToNameIndex.TryGetValue(clientId, out int nameIdx))
        {
            // Find current position (may have shifted due to earlier removals)
            // Rebuild: search by tracking. Since indices shift, find the correct current index.
            int currentIdx = -1;
            int count = 0;
            foreach (var kv in _clientIdToNameIndex)
            {
                if (kv.Value < nameIdx) count++;
            }
            // No exact way without more bookkeeping, so just remove by counting order
            // Simpler approach: remove the entry by scanning
            _clientIdToNameIndex.Remove(clientId);

            // Rebuild the list: clear and re-add from connected clients
            RebuildPlayerNames();
        }

        UpdatePlayerCount();
    }

    void RebuildPlayerNames()
    {
        _playerNames.Clear();
        _clientIdToNameIndex.Clear();
        _nextNameIndex = 0;

        // Re-add host
        string hostName = NetworkPlayerName.LocalPlayerName;
        if (string.IsNullOrEmpty(hostName)) hostName = "Host";
        _playerNames.Add(new FixedString64Bytes(hostName));
        _clientIdToNameIndex[NetworkManager.LocalClientId] = _nextNameIndex++;

        // Re-add connected clients (except host)
        foreach (var kv in NetworkManager.ConnectedClients)
        {
            if (kv.Key == NetworkManager.LocalClientId) continue;
            var playerObj = kv.Value.PlayerObject;
            if (playerObj != null)
            {
                var nameComp = playerObj.GetComponent<NetworkPlayerName>();
                string name = nameComp != null ? nameComp.NetName.Value.ToString() : $"Player_{kv.Key}";
                if (string.IsNullOrEmpty(name)) name = $"Player_{kv.Key}";
                _playerNames.Add(new FixedString64Bytes(name));
                _clientIdToNameIndex[kv.Key] = _nextNameIndex++;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void RegisterPlayerNameServerRpc(FixedString64Bytes playerName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[Lobby] Player registered: {playerName} (clientId={clientId})");
        _playerNames.Add(playerName);
        _clientIdToNameIndex[clientId] = _nextNameIndex++;
        UpdatePlayerCount();
    }

    // ───────────────── UI ─────────────────

    void SetupLobbyUI()
    {
        if (lobbyPanel == null)
        {
            // Auto-create lobby UI nếu chưa có
            CreateDefaultLobbyUI();
        }

        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);

        // Join code
        if (joinCodeText != null)
        {
            string code = RelayManager.CurrentJoinCode;
            joinCodeText.text = !string.IsNullOrEmpty(code) ? $"Room Code: {code}" : "";
        }

        // Title
        if (titleText != null)
            titleText.text = IsServer ? "YOUR ROOM" : "JOINED ROOM";

        // Start button (chỉ host thấy)
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(IsServer);
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        // Status
        if (statusText != null)
            statusText.text = IsServer ? "Waiting for players..." : "Waiting for host to start...";

        UpdatePlayerCount();
        RefreshPlayerList();

        // Lock cursor during lobby
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void OnPlayerListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        RefreshPlayerList();
        UpdatePlayerCount();
    }

    void RefreshPlayerList()
    {
        // Xóa items cũ
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        _spawnedItems.Clear();

        if (playerListContainer == null) return;

        for (int i = 0; i < _playerNames.Count; i++)
        {
            string name = _playerNames[i].ToString();
            bool isHost = (i == 0);

            GameObject item;
            if (playerListItemPrefab != null)
            {
                item = Instantiate(playerListItemPrefab, playerListContainer);
            }
            else
            {
                // Auto-create simple text item
                item = new GameObject($"PlayerItem_{i}", typeof(RectTransform), typeof(TextMeshProUGUI));
                item.transform.SetParent(playerListContainer, false);
                item.layer = 5;

                var rt = item.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 45);
            }

            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                string icon = isHost ? "👑" : "🎮";
                string role = isHost ? " (Host)" : "";
                text.text = $"  {icon}  {name}{role}";
                text.fontSize = 28;
                text.color = isHost ? new Color(1f, 0.84f, 0f) : Color.white;
                text.alignment = TextAlignmentOptions.Left;
            }

            _spawnedItems.Add(item);
        }
    }

    void UpdatePlayerCount()
    {
        int count = _playerNames.Count;

        if (playerCountText != null)
            playerCountText.text = $"Players: {count}/{maxPlayers}";

        // Start button: enable khi đủ người
        if (startGameButton != null && IsServer)
        {
            bool canStart = count >= minPlayers;
            startGameButton.interactable = canStart;

            if (startButtonText != null)
                startButtonText.text = canStart ? "▶  START" : $"Need {minPlayers - count} more";
        }

        if (statusText != null)
        {
            if (IsServer)
                statusText.text = count >= minPlayers ? "Ready! Press START." : $"Waiting for {minPlayers - count} more...";
            else
                statusText.text = "Waiting for host to start...";
        }
    }

    // ───────────────── START GAME ─────────────────

    void OnStartGameClicked()
    {
        if (!IsServer) return;
        if (_playerNames.Count < minPlayers)
        {
            Debug.LogWarning("[Lobby] Not enough players!");
            return;
        }

        GameStarted.Value = true;
    }

    void OnGameStartedChanged(bool prev, bool current)
    {
        if (current)
        {
            HideLobby();
            OnGameStarted?.Invoke();
        }
    }

    void HideLobby()
    {
        Debug.Log("[Lobby] Game started! Hiding lobby.");

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        // Unlock cursor cho gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // ───────────────── AUTO-CREATE LOBBY UI ─────────────────

    void CreateDefaultLobbyUI()
    {
        // Tạo fullscreen canvas
        var canvasGO = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Dark background
        lobbyPanel = new GameObject("LobbyPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lobbyPanel.transform.SetParent(canvasGO.transform, false);
        lobbyPanel.layer = 5;
        var panelRT = lobbyPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        lobbyPanel.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.06f, 0.95f);

        // Center container
        var center = new GameObject("Center", typeof(RectTransform));
        center.transform.SetParent(lobbyPanel.transform, false);
        center.layer = 5;
        var cRT = center.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.25f, 0.1f);
        cRT.anchorMax = new Vector2(0.75f, 0.9f);
        cRT.sizeDelta = Vector2.zero;

        // Title
        titleText = CreateUIText(center.transform, "Title", "LOBBY", 42, TextAlignmentOptions.Center);
        SetRectAnchors(titleText.gameObject, 0, 1, 1, 1, 0, -20, 0, 60);

        // Join Code
        joinCodeText = CreateUIText(center.transform, "JoinCode", "", 30, TextAlignmentOptions.Center);
        SetRectAnchors(joinCodeText.gameObject, 0, 1, 1, 1, 0, -75, 0, 40);
        joinCodeText.color = new Color(1f, 0.84f, 0f);

        // Player count
        playerCountText = CreateUIText(center.transform, "PlayerCount", "Players: 0/4", 26, TextAlignmentOptions.Center);
        SetRectAnchors(playerCountText.gameObject, 0, 1, 1, 1, 0, -120, 0, 35);

        // Player list container (vertical layout)
        var listPanel = new GameObject("PlayerListBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
        listPanel.transform.SetParent(center.transform, false);
        listPanel.layer = 5;
        SetRectAnchors(listPanel, 0.05f, 0.25f, 0.95f, 0.82f, 0, 0, 0, 0);
        listPanel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.1f, 0.8f);
        var vlg = listPanel.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 5;
        vlg.padding = new RectOffset(15, 15, 10, 10);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        playerListContainer = listPanel.transform;

        // Start button
        var btnGO = new GameObject("Button_StartGame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(center.transform, false);
        btnGO.layer = 5;
        SetRectAnchors(btnGO, 0.2f, 0, 0.8f, 0, 0, 80, 0, 55);
        btnGO.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.25f, 1f);
        startGameButton = btnGO.GetComponent<Button>();

        startButtonText = CreateUIText(btnGO.transform, "Text", "▶  START", 28, TextAlignmentOptions.Center);
        var bTextRT = startButtonText.GetComponent<RectTransform>();
        bTextRT.anchorMin = Vector2.zero;
        bTextRT.anchorMax = Vector2.one;
        bTextRT.sizeDelta = Vector2.zero;

        // Status
        statusText = CreateUIText(center.transform, "Status", "", 22, TextAlignmentOptions.Center);
        SetRectAnchors(statusText.gameObject, 0, 0, 1, 0, 0, 20, 0, 35);
        statusText.color = new Color(0.7f, 0.9f, 1f);
    }

    static TextMeshProUGUI CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = 5;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static void SetRectAnchors(GameObject go, float aMinX, float aMinY, float aMaxX, float aMaxY, float posX, float posY, float sizeX, float sizeY)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY);
        rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.anchoredPosition = new Vector2(posX, posY);
        rt.sizeDelta = new Vector2(sizeX, sizeY);
    }
}
