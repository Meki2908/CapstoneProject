using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

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
    [Tooltip("Nếu true: host 1 mình vẫn bấm START được (tránh prefab cũ minPlayers=2 kẹt lobby).")]
    [SerializeField] private bool allowHostStartAlone = true;
    [Tooltip("Số player tối đa (kể cả host).")]
    [SerializeField] private int maxPlayers = 4;

    [Header("Game State")]
    [Tooltip("GameObject chứa gameplay controllers. Để null = không auto-activate.")]
    [SerializeField] private GameObject gameplayRoot;

    // ── UI (tự tạo runtime) ──
    private GameObject _lobbyCanvasRoot;
    private GameObject lobbyPanel;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI joinCodeText;
    private TextMeshProUGUI playerCountText;
    private Transform playerListContainer;
    private Button startGameButton;
    private TextMeshProUGUI startButtonText;
    private TextMeshProUGUI statusText;

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
    /// <summary>Host bấm START trên UI — gán GameStarted trong FixedUpdateNetwork (Fusion simulation), không gán [Networked] từ OnClick.</summary>
    private bool _pendingStartGame;

    void Awake()
    {
        // BUG-13 fix: Singleton guard
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Lobby] Duplicate NetworkLobbyManager detected — destroying '{gameObject.name}'");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Host/server điều khiển START và [Networked] lobby — ưu tiên IsServer vì HasStateAuthority đôi khi sai trên scene object.</summary>
    private bool IsLobbyServer => Runner != null && Runner.IsServer;

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
    /// Bật lại để fix lỗi click Start bị chặn bởi Canvas_Menu từ ngoài sảnh
    /// </summary>
    private GraphicRaycaster _menuRaycaster;

    /// <summary>
    /// Hiện lobby sau khi runner & scene đã sẵn sàng.
    /// Tránh gọi SetupLobbyUI quá sớm khi scene vừa load.
    /// </summary>
    private void ShowLobbyWhenReady()
    {
        if (_lobbyInitialized) return;

        // Tắt Canvas_Menu tạm thời để không đè lên Lobby
        var menuCanvasGO = GameObject.Find("Canvas_Menu");
        if (menuCanvasGO != null)
        {
            _menuRaycaster = menuCanvasGO.GetComponent<GraphicRaycaster>();
            if (_menuRaycaster != null) _menuRaycaster.enabled = false;
        }

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

        // ── Verify lobby input system ──
        VerifyLobbyInputSystem();

        // Đăng ký tên player local — KHÔNG dùng HasInputAuthority trên lobby object
        // (NetworkLobbyManager thường không có InputAuthority → trước đây không ai gọi RPC → 0 player).
        if (Runner != null)
        {
            string name = NetworkPlayerName.LocalPlayerName;
            if (string.IsNullOrEmpty(name))
                name = $"Player_{Runner.LocalPlayer.PlayerId}";
            // RPC từ object không có InputAuthority → RpcInfo.Source = None → slot sai.
            // Truyền PlayerRef rõ ràng.
            var lp = Runner.LocalPlayer;
            if (lp != PlayerRef.None)
                RPC_RegisterPlayerName(lp, name);
        }
    }

    // ───────────────── RENDER + UPDATE ─────────────────

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
    void RPC_RegisterPlayerName(PlayerRef forPlayer, string playerName, RpcInfo info = default)
    {
        if (forPlayer == PlayerRef.None)
        {
            Debug.LogWarning($"[Lobby] RPC_RegisterPlayerName: PlayerRef.None (from={info.Source})");
            return;
        }

        Debug.Log($"[Lobby] Player registered: {playerName} (forPlayer={forPlayer}, rpcFrom={info.Source})");

        int slot = forPlayer.PlayerId - 1;
        if (slot < 0 || slot >= maxPlayers)
        {
            Debug.LogWarning($"[Lobby] Invalid name slot {slot} for {forPlayer}");
            return;
        }

        PlayerNames.Set(slot, playerName);
        SyncPlayerCountFromRunner();
        Debug.Log($"[Lobby] Name stored at slot {slot}, PlayerCount={PlayerCount}");
    }

    /// <summary>True khi panel lobby đang mở và game chưa bắt đầu — dùng để không khóa chuột gameplay.</summary>
    public bool IsLobbyBlockingCursor =>
        lobbyPanel != null && lobbyPanel.activeInHierarchy && !GameStarted;

    void SetLobbyRootVisible(bool visible)
    {
        if (_lobbyCanvasRoot != null)
            _lobbyCanvasRoot.SetActive(visible);
        else if (lobbyPanel != null)
            lobbyPanel.SetActive(visible);
    }

    /// <summary>Đồng bộ số người chơi từ Fusion (nguồn đúng), không chỉ dựa vào RPC.</summary>
    private void SyncPlayerCountFromRunner()
    {
        if (Runner == null || !Runner.IsServer) return;

        int active = 0;
        foreach (var _ in Runner.ActivePlayers)
            active++;

        if (PlayerCount != active)
            PlayerCount = active;
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner == null || !Runner.IsServer) return;

        // START từ nút UI → chờ tick simulation mới ghi GameStarted (tránh host không nhận ChangeDetector / lobby không tắt).
        if (_pendingStartGame && !GameStarted)
        {
            _pendingStartGame = false;
            GameStarted = true;
            _wasGameStarted = true;
            Debug.Log("[Lobby] GameStarted = true (simulation) — hiding lobby + gameplay.");
            HideLobby();
            ActivateGameplay();
            OnGameStarted?.Invoke();
            return;
        }

        if (GameStarted) return;
        SyncPlayerCountFromRunner();
    }

    // ───────────────── UI ─────────────────

    void SetupLobbyUI()
    {
        if (lobbyPanel == null)
        {
            CreateDefaultLobbyUI();
        }

        if (lobbyPanel == null) return;

        SetLobbyRootVisible(true);

        var lobbyCanvas = _lobbyCanvasRoot != null ? _lobbyCanvasRoot : lobbyPanel.transform.root.gameObject;
        Debug.Log($"[Lobby] UI setup complete. LobbyCanvas active={lobbyCanvas.activeInHierarchy}, sortingOrder={lobbyCanvas.GetComponent<Canvas>()?.sortingOrder}");

        // Debug: liệt kê Canvas đang active trong scene
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (c.gameObject.activeInHierarchy)
                Debug.Log($"  ACTIVE Canvas: '{c.gameObject.name}', order={c.sortingOrder}");
        }

        // Join code
        if (joinCodeText != null)
        {
            string code = MultiplayerManager.CurrentSessionName;
            joinCodeText.text = !string.IsNullOrEmpty(code) ? $"Room Code: {code}" : "";
        }

        // Title
        if (titleText != null)
            titleText.text = IsLobbyServer ? "YOUR ROOM" : "JOINED ROOM";

        // Start button (chỉ host thấy)
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(IsLobbyServer);
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
            Debug.Log($"[Lobby] ✅ START button wired. interactable={startGameButton.interactable}, " +
                      $"active={startGameButton.gameObject.activeSelf}, IsLobbyServer={IsLobbyServer}");
        }
        else
        {
            Debug.LogError("[Lobby] ❌ startGameButton is NULL after CreateDefaultLobbyUI!");
        }

        // Status
        if (statusText != null)
            statusText.text = IsLobbyServer ? "Press START (or Enter) when ready!" : "Waiting for host to start...";

        UpdatePlayerCountUI();
        RefreshPlayerList();

        Debug.Log("[Lobby] UI setup complete.");
    }

    void RefreshPlayerList()
    {
        if (lobbyPanel == null || !lobbyPanel.activeInHierarchy) return;

        // Xóa items cũ
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item);
        }
        _spawnedItems.Clear();

        if (playerListContainer == null) return;

        // Liệt kê theo ActivePlayers + PlayerId (khớp spawn index), không theo PlayerCount tuần tự
        var sortedPlayers = new System.Collections.Generic.List<PlayerRef>();
        if (Runner != null)
        {
            foreach (var p in Runner.ActivePlayers)
                sortedPlayers.Add(p);
            sortedPlayers.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
        }

        if (sortedPlayers.Count == 0)
        {
            for (int i = 0; i < maxPlayers; i++)
            {
                string name = PlayerNames[i].ToString();
                if (string.IsNullOrEmpty(name)) continue;
                bool isHost = (i == 0);
                AddOnePlayerListRow(i, name, isHost);
            }
            return;
        }

        int row = 0;
        foreach (var p in sortedPlayers)
        {
            int slot = p.PlayerId - 1;
            string name = (slot >= 0 && slot < maxPlayers) ? PlayerNames[slot].ToString() : "";
            if (string.IsNullOrEmpty(name))
                name = $"Player {p.PlayerId}";
            bool isHost = (p.PlayerId == 1);
            AddOnePlayerListRow(row, name, isHost);
            row++;
        }
    }

    void AddOnePlayerListRow(int rowIndex, string name, bool isHost)
    {
        var item = new GameObject($"PlayerItem_{rowIndex}", typeof(RectTransform), typeof(TextMeshProUGUI));
        item.transform.SetParent(playerListContainer, false);
        item.layer = 5;

        var rt = item.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 45);

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

    void UpdatePlayerCountUI()
    {
        int count = PlayerCount;
        if (Runner != null)
        {
            int active = 0;
            foreach (var _ in Runner.ActivePlayers)
                active++;
            count = Mathf.Max(count, active);
        }

        if (playerCountText != null)
            playerCountText.text = $"Players: {count}/{maxPlayers}";

        if (startGameButton != null && IsLobbyServer)
        {
            bool canStart = CanHostStartNow(count);
            startGameButton.interactable = canStart;

            if (startButtonText != null)
            {
                if (canStart)
                    startButtonText.text = "▶  START";
                else
                {
                    int need = minPlayers - count;
                    startButtonText.text = need > 0 ? $"Need {need} more" : "▶  START";
                }
            }
        }

        if (statusText != null)
        {
            if (IsLobbyServer)
            {
                bool canStart = CanHostStartNow(count);
                statusText.text = canStart ? "Ready! Press START or Enter." : $"Waiting for {Mathf.Max(0, minPlayers - count)} more...";
            }
            else
                statusText.text = "Waiting for host to start...";
        }
    }

    /// <summary>Host có được phép START với số người hiện tại không.</summary>
    private bool CanHostStartNow(int displayedCount)
    {
        if (displayedCount >= minPlayers)
            return true;
        if (allowHostStartAlone && Runner != null)
        {
            int active = 0;
            foreach (var _ in Runner.ActivePlayers)
                active++;
            if (active <= 1 && displayedCount >= 1)
                return true;
        }
        return false;
    }

    // ───────────────── START GAME ─────────────────

    void OnStartGameClicked()
    {
        // ★ LOG ĐẦU TIÊN — nếu dòng này không xuất hiện trong Console, click CHƯA đến được handler
        Debug.Log($"[Lobby] ★★★ OnStartGameClicked FIRED! " +
                  $"IsLobbyServer={IsLobbyServer}, GameStarted={GameStarted}, " +
                  $"_pendingStartGame={_pendingStartGame}, " +
                  $"Runner={(Runner != null ? "OK" : "NULL")}, " +
                  $"Runner.IsServer={(Runner != null ? Runner.IsServer.ToString() : "N/A")}");

        if (!IsLobbyServer)
        {
            Debug.LogWarning("[Lobby] START ignored — chỉ server/host mới bắt đầu game.");
            return;
        }

        ExecuteStartGame();
    }

    /// <summary>
    /// Logic start game chung — gọi từ nút onClick HOẶC từ phím Enter (fallback).
    /// </summary>
    private void ExecuteStartGame()
    {
        if (GameStarted || _pendingStartGame)
        {
            Debug.Log("[Lobby] ExecuteStartGame skipped — already started or pending.");
            return;
        }

        int count = PlayerCount;
        if (Runner != null)
        {
            int active = 0;
            foreach (var _ in Runner.ActivePlayers)
                active++;
            count = Mathf.Max(count, active);
        }

        if (!CanHostStartNow(count))
        {
            Debug.LogWarning($"[Lobby] Not enough players! count={count}, minPlayers={minPlayers}");
            return;
        }

        Debug.Log($"[Lobby] ✅ START GAME executing! PlayerCount={count}");
        _pendingStartGame = true;

        // Ẩn lobby NGAY (host nhìn thấy tắt ngay, không đợi tick simulation).
        HideLobbyImmediate();
        ActivateGameplay();
    }

    /// <summary>
    /// Update chạy mỗi frame — dùng để:
    /// 1) Đảm bảo cursor visible khi lobby đang mở
    /// 2) Phím Enter = fallback khi nút START không click được
    /// 3) Verify EventSystem hoạt động
    /// </summary>
    void Update()
    {
        // Chỉ chạy khi lobby đang hiện
        if (!_lobbyInitialized || lobbyPanel == null || !lobbyPanel.activeInHierarchy || GameStarted)
            return;

        // ── Fallback: phím Enter/Return để bắt đầu game ──
        if (IsLobbyServer && !_pendingStartGame)
        {
            bool enterPressed = false;

            // New Input System
            if (Keyboard.current != null)
            {
                enterPressed = Keyboard.current.enterKey.wasPressedThisFrame
                            || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
            }

            // Legacy fallback
            if (!enterPressed)
            {
                try { enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); }
                catch { }
            }

            if (enterPressed)
            {
                Debug.Log("[Lobby] Enter key pressed — starting game (keyboard fallback).");
                ExecuteStartGame();
                return;
            }
        }

        // ── Fallback: Direct mouse click trên START button (bypass EventSystem) ──
        if (IsLobbyServer && !_pendingStartGame && startGameButton != null && startGameButton.gameObject.activeSelf)
        {
            bool leftClick = false;

            // New Input System
            if (Mouse.current != null)
                leftClick = Mouse.current.leftButton.wasPressedThisFrame;

            // Legacy fallback
            if (!leftClick)
            {
                try { leftClick = Input.GetMouseButtonDown(0); }
                catch { }
            }

            if (leftClick)
            {
                // Kiểm tra chuột có nằm trên nút START không
                var btnRT = startGameButton.GetComponent<RectTransform>();
                if (btnRT != null)
                {
                    Vector2 mousePos;
                    if (Mouse.current != null)
                        mousePos = Mouse.current.position.ReadValue();
                    else
                        mousePos = (Vector2)Input.mousePosition;

                    // Null camera = ScreenSpaceOverlay canvas
                    if (RectTransformUtility.RectangleContainsScreenPoint(btnRT, mousePos, null))
                    {
                        Debug.Log("[Lobby] ★ Direct mouse click on START — bypassing EventSystem!");
                        ExecuteStartGame();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra EventSystem, InputModule, GraphicRaycaster sau khi lobby UI được tạo.
    /// </summary>
    private void VerifyLobbyInputSystem()
    {
        // EventSystem
        var es = EventSystem.current;
        if (es == null)
        {
            Debug.LogError("[Lobby] ❌ EventSystem.current is NULL — UI sẽ không nhận click!");
        }
        else
        {
            Debug.Log($"[Lobby] EventSystem: '{es.gameObject.name}', enabled={es.enabled}");

            // InputModule
            var uiModule = es.GetComponent<InputSystemUIInputModule>();
            var standalone = es.GetComponent<StandaloneInputModule>();
            if (uiModule != null)
                Debug.Log($"[Lobby] ✅ InputSystemUIInputModule active on EventSystem.");
            else if (standalone != null)
                Debug.LogWarning($"[Lobby] ⚠️ StandaloneInputModule on EventSystem — có thể không nhận click!");
            else
                Debug.LogError($"[Lobby] ❌ No InputModule on EventSystem!");
        }

        // Start button
        if (startGameButton != null)
        {
            Debug.Log($"[Lobby] START button: active={startGameButton.gameObject.activeSelf}, " +
                      $"interactable={startGameButton.interactable}, " +
                      $"listeners={startGameButton.onClick.GetPersistentEventCount()}");
        }
    }

    void HideLobby()
    {
        Debug.Log("[Lobby] Hiding lobby...");

        SetLobbyRootVisible(false);
        ApplyGameplayCursorAndRestoreMenu();
    }

    /// <summary>
    /// Ẩn lobby NGAY LẬP TỨC (không qua coroutine).
    /// Dùng khi vào scene mà game đã started.
    /// </summary>
    private void HideLobbyImmediate()
    {
        SetLobbyRootVisible(false);
        ApplyGameplayCursorAndRestoreMenu();
    }

    private void ApplyGameplayCursorAndRestoreMenu()
    {
        // Xóa logic bật lại _menuRaycaster vì _menuRaycaster (Canvas_Menu) có thể chặn click của Pause Menu trong game.
        // Chỉ lưu lại việc EndAllUiOverlays và Lock Cursor.
        var menuCanvasGO = GameObject.Find("Canvas_Menu");
        if (menuCanvasGO != null)
        {
            var gr = menuCanvasGO.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr != null) gr.enabled = false;
            Debug.Log("[Lobby] Forced Canvas_Menu GraphicRaycaster OFF during gameplay to prevent blocking.");
        }

        // Clear mọi chặn UI từ scene trước để Alt hoạt động đúng
        CursorUIPriority.EndAllUiOverlays();

        // Tự động ghim chuột khi đóng lobby và vào game
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
    }

    /// <summary>
    /// Kích hoạt gameplay sau khi lobby ẩn.
    /// </summary>
    private void ActivateGameplay()
    {
        Debug.Log("[Lobby] Activating gameplay...");

        // ── Disable scene player (Player đặt sẵn trong scene) ──
        // Khi lobby CÙNG scene với gameplay, LocalPlayerHandler.OnSceneLoaded KHÔNG fire lại.
        // Phải disable thủ công ở đây.
        DisableScenePlayer();

        // Có thể spawn thêm gameplay objects nếu cần
        if (gameplayRoot != null && !gameplayRoot.activeSelf)
        {
            gameplayRoot.SetActive(true);
        }

        // Player có thể đã spawn khi scene load (trước START) — snap lại đúng spawn point sau khi đóng lobby.
        foreach (var snap in FindObjectsByType<NetworkPlayerSpawnSnap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (snap == null || snap.Object == null || !snap.Object.IsValid) continue;
            if (!snap.Object.HasStateAuthority) continue;
            snap.ForceSnapToSpawn();
        }
    }

    /// <summary>
    /// Disable scene player (Player_3.0, Player, v.v.) khi online.
    /// Chỉ giữ lại Fusion-spawned player prefab (có NetworkObject component).
    /// </summary>
    private void DisableScenePlayer()
    {
        // Nếu LocalPlayerHandler tồn tại → gọi trực tiếp
        if (LocalPlayerHandler.Instance != null)
        {
            LocalPlayerHandler.Instance.TryDisableScenePlayer();
        }

        // Fallback: tìm và disable theo tag "Player"
        int disabledCount = 0;
        try
        {
            var taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var go in taggedPlayers)
            {
                // Bỏ qua nếu đã inactive
                if (!go.activeInHierarchy) continue;

                // ✅ KEY CHECK: Bỏ qua nếu thuộc Fusion-spawned hierarchy
                // Fusion-spawned objects có NetworkObject trên root hoặc parent
                if (IsFusionSpawned(go)) continue;

                // Bỏ qua objects không phải player thực sự
                string lowerName = go.name.ToLower();
                if (lowerName.Contains("spawn") || lowerName.Contains("handler") ||
                    lowerName.Contains("manager") || lowerName.Contains("canvas") ||
                    lowerName.Contains("nametag"))
                    continue;

                // Disable root object (để tất cả children cũng bị tắt)
                GameObject root = go.transform.root.gameObject;

                if (root.activeInHierarchy && !IsFusionSpawned(root))
                {
                    root.SetActive(false);
                    disabledCount++;
                    Debug.Log($"[Lobby] ✅ Disabled scene player: '{root.name}' (found via child '{go.name}')");
                }
            }
        }
        catch { /* Tag 'Player' chưa tồn tại */ }

        // Fallback 2: tìm theo tên trong root objects (nếu không có tag)
        if (disabledCount == 0)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in roots)
            {
                if (!go.activeInHierarchy) continue;
                if (IsFusionSpawned(go)) continue;

                string lowerName = go.name.ToLower();
                if (!lowerName.Contains("player")) continue;
                if (lowerName.Contains("spawn") || lowerName.Contains("handler") ||
                    lowerName.Contains("manager") || lowerName.Contains("canvas") ||
                    lowerName.Contains("nametag") || lowerName.Contains("camera") ||
                    lowerName.Contains("event") || lowerName.Contains("ui"))
                    continue;

                // Kiểm tra có CharacterController hoặc Character → chắc chắn là player
                if (go.GetComponentInChildren<CharacterController>(true) != null ||
                    go.GetComponentInChildren<Character>(true) != null)
                {
                    go.SetActive(false);
                    disabledCount++;
                    Debug.Log($"[Lobby] ✅ Disabled scene player (by name): '{go.name}'");
                }
            }
        }

        if (disabledCount > 0)
            Debug.Log($"[Lobby] Disabled {disabledCount} scene player(s) — only Fusion-spawned prefabs remain.");
        else
            Debug.Log("[Lobby] No scene players found to disable (expected if gameplay scene auto-spawns).");

        // (Đã gỡ bỏ EnsureEventSystemExists theo yêu cầu)
    }

    /// <summary>
    /// Kiểm tra object có thuộc Fusion-spawned hierarchy không.
    /// Fusion-spawned = tên ROOT chứa "(Clone)" (do Unity Instantiate thêm vào).
    /// Scene-placed player dùng cùng prefab nhưng KHÔNG có "(Clone)" vì được đặt trực tiếp trong scene.
    /// </summary>
    private bool IsFusionSpawned(GameObject go)
    {
        // Chỉ dùng "(Clone)" trên ROOT — đây là cách DUY NHẤT phân biệt
        // vì cả scene player và Fusion player đều có NetworkObject (dùng chung prefab)
        GameObject root = go.transform.root.gameObject;
        return root.name.Contains("(Clone)");
    }

    // ───────────────── AUTO-CREATE LOBBY UI ─────────────────

    void CreateDefaultLobbyUI()
    {
        var canvasGO = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Cao nhất có thể — đảm bảo lobby luôn trên mọi canvas (kể cả Canvas_Menu cũ)
        canvas.sortingOrder = 32767;

        _lobbyCanvasRoot = canvasGO;

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
        tmp.raycastTarget = false;
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
