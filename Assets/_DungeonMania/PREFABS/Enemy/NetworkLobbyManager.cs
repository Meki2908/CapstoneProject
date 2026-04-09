using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Lobby overlay trong gameplay scene.
///
/// Flow 1: Menu → Start Game → scene load
///   Host load scene → GameStarted = true (trong Spawned) → lobby tự ẩn
///   Client load scene → GameStarted đã true → lobby tự ẩn
///
/// Flow 2: Join giữa chừng (late join)
///   Client join khi game đã bắt đầu → lobby ẩn → gameplay
///
/// Flow 3: Vào scene trực tiếp (ko qua menu)
///   GameStarted mặc định = false → lobby hiện
///   Host bấm START → GameStarted = true
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkLobbyManager : NetworkBehaviour
{
    public static NetworkLobbyManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Số player tối thiểu để bắt đầu (kể cả host).")]
    [SerializeField] private int minPlayers = 1;
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

    [Header("Auto-create Settings")]
    [Tooltip("Tự động tạo lobby UI nếu chưa gán references.")]
    [SerializeField] private bool autoCreateUI = true;

    [Header("Game State References")]
    [Tooltip("GameObject chứa gameplay controllers (spawn khi game started). Để null = không auto-spawn.")]
    [SerializeField] private GameObject gameplayRoot;

    /// <summary>Danh sách tên player (sync qua network).</summary>
    [Networked, Capacity(4)]
    private NetworkArray<NetworkString<_32>> PlayerNames => default;

    /// <summary>Số player hiện tại.</summary>
    [Networked] private int PlayerCount { get; set; }

    /// <summary>True khi game đã bắt đầu (lobby ẩn).</summary>
    [Networked] public NetworkBool GameStarted { get; set; }

    /// <summary>Event khi game bắt đầu (lobby ẩn, gameplay chạy).</summary>
    public event Action OnGameStarted;

    private List<GameObject> _spawnedItems = new List<GameObject>();
    private ChangeDetector _changeDetector;
    private bool _wasGameStarted;
    private bool _lobbyInitialized;
    private bool _pendingShowLobby;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        _wasGameStarted = GameStarted;

        Debug.Log($"[Lobby] Spawned — GameStarted={GameStarted}, HasStateAuthority={HasStateAuthority}, HasInputAuthority={HasInputAuthority}");

        // Tự động ẩn lobby nếu game đã started (vào giữa chừng)
        if (GameStarted)
        {
            Debug.Log("[Lobby] Game already started — hiding lobby.");
            HideLobbyImmediate();
            ActivateGameplay();
        }
        else
        {
            // Chưa started → hiện lobby
            Debug.Log("[Lobby] Game not started — showing lobby.");
            ShowLobbyWhenReady();
        }
    }

    /// <summary>
    /// Hiện lobby sau khi runner & scene đã sẵn sàng.
    /// Tránh gọi SetupLobbyUI quá sớm khi scene vừa load.
    /// </summary>
    private void ShowLobbyWhenReady()
    {
        if (_lobbyInitialized) return;

        // Đợi 1 frame để scene load hoàn tất
        StartCoroutine(ShowLobbyDelayed());
    }

    private System.Collections.IEnumerator ShowLobbyDelayed()
    {
        yield return null; // Chờ 1 frame

        // Kiểm tra scene đã load xong
        while (SceneManager.sceneCount < 1)
            yield return null;

        SetupLobbyUI();
        _lobbyInitialized = true;

        // Đăng ký tên player local
        if (HasInputAuthority)
        {
            string name = NetworkPlayerName.LocalPlayerName;
            if (string.IsNullOrEmpty(name))
                name = $"Player_{Object.InputAuthority.PlayerId}";
            RPC_RegisterPlayerName(name);
        }
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayerCount):
                case nameof(PlayerNames):
                    if (_lobbyInitialized)
                    {
                        RefreshPlayerList();
                        UpdatePlayerCountUI();
                    }
                    break;
                case nameof(GameStarted):
                    if (GameStarted && !_wasGameStarted)
                    {
                        _wasGameStarted = true;
                        Debug.Log("[Lobby] GameStarted changed to TRUE — hiding lobby!");
                        HideLobby();
                        ActivateGameplay();
                        OnGameStarted?.Invoke();
                    }
                    break;
            }
        }
    }

    // ───────────────── RPC ─────────────────

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RegisterPlayerName(string playerName, RpcInfo info = default)
    {
        Debug.Log($"[Lobby] Player registered: {playerName} (player={info.Source})");

        if (PlayerCount < maxPlayers)
        {
            // Kiểm tra tên đã tồn tại chưa (tránh duplicate khi reconnect)
            bool alreadyRegistered = false;
            for (int i = 0; i < PlayerCount; i++)
            {
                if (PlayerNames[i].ToString() == playerName)
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (!alreadyRegistered)
            {
                PlayerNames.Set(PlayerCount, playerName);
                PlayerCount++;
                Debug.Log($"[Lobby] PlayerCount now: {PlayerCount}");
            }
        }
    }

    // ───────────────── UI ─────────────────

    void SetupLobbyUI()
    {
        if (lobbyPanel == null && autoCreateUI)
        {
            CreateDefaultLobbyUI();
        }

        if (lobbyPanel == null) return;

        lobbyPanel.SetActive(true);

        // Join code
        if (joinCodeText != null)
        {
            string code = MultiplayerManager.CurrentSessionName;
            joinCodeText.text = !string.IsNullOrEmpty(code) ? $"Room Code: {code}" : "";
        }

        // Title
        if (titleText != null)
            titleText.text = HasStateAuthority ? "YOUR ROOM" : "JOINED ROOM";

        // Start button (chỉ host thấy)
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(HasStateAuthority);
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        // Status
        if (statusText != null)
            statusText.text = HasStateAuthority ? "Press START when ready!" : "Waiting for host to start...";

        UpdatePlayerCountUI();
        RefreshPlayerList();

        // Lock cursor during lobby
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("[Lobby] UI setup complete.");
    }

    void RefreshPlayerList()
    {
        if (lobbyPanel == null || !lobbyPanel.activeSelf) return;

        // Xóa items cũ
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        _spawnedItems.Clear();

        if (playerListContainer == null) return;

        for (int i = 0; i < PlayerCount; i++)
        {
            string name = PlayerNames[i].ToString();
            bool isHost = (i == 0);

            GameObject item;
            if (playerListItemPrefab != null)
            {
                item = Instantiate(playerListItemPrefab, playerListContainer);
            }
            else
            {
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

    void UpdatePlayerCountUI()
    {
        int count = PlayerCount;

        if (playerCountText != null)
            playerCountText.text = $"Players: {count}/{maxPlayers}";

        if (startGameButton != null && HasStateAuthority)
        {
            bool canStart = count >= minPlayers;
            startGameButton.interactable = canStart;

            if (startButtonText != null)
                startButtonText.text = canStart ? "▶  START" : $"Need {minPlayers - count} more";
        }

        if (statusText != null)
        {
            if (HasStateAuthority)
                statusText.text = count >= minPlayers ? "Ready! Press START." : $"Waiting for {minPlayers - count} more...";
            else
                statusText.text = "Waiting for host to start...";
        }
    }

    // ───────────────── START GAME ─────────────────

    void OnStartGameClicked()
    {
        if (!HasStateAuthority) return;
        if (PlayerCount < minPlayers)
        {
            Debug.LogWarning("[Lobby] Not enough players!");
            return;
        }

        Debug.Log("[Lobby] START GAME clicked!");
        GameStarted = true;
    }

    void HideLobby()
    {
        Debug.Log("[Lobby] Hiding lobby...");

        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Ẩn lobby NGAY LẬP TỨC (không qua coroutine).
    /// Dùng khi vào scene mà game đã started.
    /// </summary>
    private void HideLobbyImmediate()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Kích hoạt gameplay sau khi lobby ẩn.
    /// </summary>
    private void ActivateGameplay()
    {
        Debug.Log("[Lobby] Activating gameplay...");

        // Có thể spawn thêm gameplay objects nếu cần
        if (gameplayRoot != null && !gameplayRoot.activeSelf)
        {
            gameplayRoot.SetActive(true);
        }
    }

    // ───────────────── AUTO-CREATE LOBBY UI ─────────────────

    void CreateDefaultLobbyUI()
    {
        var canvasGO = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        lobbyPanel = new GameObject("LobbyPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lobbyPanel.transform.SetParent(canvasGO.transform, false);
        lobbyPanel.layer = 5;
        var panelRT = lobbyPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        lobbyPanel.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.06f, 0.95f);

        var center = new GameObject("Center", typeof(RectTransform));
        center.transform.SetParent(lobbyPanel.transform, false);
        center.layer = 5;
        var cRT = center.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.25f, 0.1f);
        cRT.anchorMax = new Vector2(0.75f, 0.9f);
        cRT.sizeDelta = Vector2.zero;

        titleText = CreateUIText(center.transform, "Title", "LOBBY", 42, TextAlignmentOptions.Center);
        SetRectAnchors(titleText.gameObject, 0, 1, 1, 1, 0, -20, 0, 60);

        joinCodeText = CreateUIText(center.transform, "JoinCode", "", 30, TextAlignmentOptions.Center);
        SetRectAnchors(joinCodeText.gameObject, 0, 1, 1, 1, 0, -75, 0, 40);
        joinCodeText.color = new Color(1f, 0.84f, 0f);

        playerCountText = CreateUIText(center.transform, "PlayerCount", "Players: 0/4", 26, TextAlignmentOptions.Center);
        SetRectAnchors(playerCountText.gameObject, 0, 1, 1, 1, 0, -120, 0, 35);

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

        statusText = CreateUIText(center.transform, "Status", "", 22, TextAlignmentOptions.Center);
        SetRectAnchors(statusText.gameObject, 0, 0, 1, 0, 0, 20, 0, 35);
        statusText.color = new Color(0.7f, 0.9f, 1f);

        Debug.Log("[Lobby] Auto-created lobby UI.");
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
