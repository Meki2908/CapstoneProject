using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menu multiplayer: Photon Fusion (<see cref="FusionSessionLauncher"/>) hoặc chơi đơn (load scene trực tiếp).
/// NGO / Unity Relay đã gỡ — không còn NetworkManager.
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    /// <summary>Luôn false (giữ API cũ cho script khác).</summary>
    public static bool UsingRelay => false;

    static string _fusionSessionName;

    [Header("=== PHOTON FUSION ===")]
    [Tooltip("True: host/join qua Fusion StartGame. False: chỉ LoadScene (single-player / test).")]
    [SerializeField] private bool usePhotonFusion = true;

    [Tooltip("Để trống sẽ tìm FusionSessionLauncher.Instance hoặc FindFirstObjectByType.")]
    [SerializeField] private FusionSessionLauncher fusionLauncher;

    [Tooltip("Tên scene trong Build Settings.")]
    [SerializeField] private string gameplaySceneName = "Map_Chinh";

    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TextMeshProUGUI joinCodeDisplay;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI statusTextJoin;

    [Header("=== PLAYER NAME ===")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField playerNameInputJoin;

    [Header("=== HOST BUTTONS ===")]
    [SerializeField] private GameObject createRoomButton;
    [SerializeField] private GameObject enterGameButton;
    [SerializeField] private TextMeshProUGUI waitingForPlayersText;

    [Header("=== PLAYER LIST ===")]
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private TextMeshProUGUI playerCountText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        string saved = PlayerPrefs.GetString("PlayerName", "");
        if (!string.IsNullOrEmpty(saved))
        {
            if (playerNameInput != null)
                playerNameInput.text = saved;
            if (playerNameInputJoin != null)
                playerNameInputJoin.text = saved;
        }

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

    public async void CreateRoom()
    {
        SavePlayerName();

        if (usePhotonFusion)
        {
            _fusionSessionName = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            if (joinCodeDisplay != null)
                joinCodeDisplay.text = _fusionSessionName;
            SetStatus($"Session: {_fusionSessionName}");
            try { GUIUtility.systemCopyBuffer = _fusionSessionName; }
            catch (System.Exception) { /* ignore */ }
            if (createRoomButton != null) createRoomButton.SetActive(false);
            if (enterGameButton != null) enterGameButton.SetActive(true);
            AddPlayerToList(NetworkPlayerName.LocalPlayerName, true);
            UpdatePlayerCount(1);
            return;
        }

        SetStatus("Room ready (offline).");
        if (createRoomButton != null) createRoomButton.SetActive(false);
        if (enterGameButton != null) enterGameButton.SetActive(true);
        AddPlayerToList(NetworkPlayerName.LocalPlayerName, true);
        UpdatePlayerCount(1);
    }

    public void LoadGameAsHost()
    {
        if (usePhotonFusion)
        {
            if (string.IsNullOrEmpty(_fusionSessionName))
            {
                SetStatus("Create room first (Fusion).");
                return;
            }
            LoadFusionHostAsync(_fusionSessionName);
            return;
        }

        Debug.Log("[MultiplayerManager] Loading gameplay scene (offline)...");
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public async void StartHostAndLoadGame()
    {
        SavePlayerName();

        if (usePhotonFusion)
        {
            _fusionSessionName = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            if (joinCodeDisplay != null)
                joinCodeDisplay.text = _fusionSessionName;
            LoadFusionHostAsync(_fusionSessionName);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public async void StartClientAndLoadGame()
    {
        SavePlayerName();

        if (usePhotonFusion)
        {
            string code = joinCodeInput != null ? joinCodeInput.text : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("Enter session name / room code!");
                return;
            }
            LoadFusionClientAsync(code.Trim());
            return;
        }

        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    /// <summary>Không còn NGO — no-op.</summary>
    public void CommitPendingJoinFromMenu() { }

    public static void SetPendingStartHostAfterSceneLoad(bool value)
    {
        // NGO bootstrap removed; không cần cờ pending.
    }

    public static void ClearPendingHostFlag() { }

    public static void ClearPendingClientFlag() { }

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
    }

    private void AddPlayerToList(string playerName, bool isHost)
    {
        if (playerListContainer == null) return;

        for (int i = playerListContainer.childCount - 1; i >= 0; i--)
        {
            var child = playerListContainer.GetChild(i);
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null && (tmp.fontStyle & FontStyles.Italic) != 0)
                Destroy(child.gameObject);
        }

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

    FusionSessionLauncher ResolveFusionLauncher()
    {
        if (fusionLauncher != null)
            return fusionLauncher;
        if (FusionSessionLauncher.Instance != null)
            return FusionSessionLauncher.Instance;
        return Object.FindFirstObjectByType<FusionSessionLauncher>();
    }

    async void LoadFusionHostAsync(string sessionName)
    {
        var launcher = ResolveFusionLauncher();
        if (launcher == null)
        {
            SetStatus("ERROR: Add FusionSessionLauncher + NetworkRunner prefab.");
            Debug.LogError("[MultiplayerManager] FusionSessionLauncher missing.");
            return;
        }

        SetStatus("Fusion: starting host...");
        bool ok = await launcher.StartHostAsync(sessionName);
        if (!ok)
            SetStatus("Fusion host failed. Check App Id / NetworkRunner.");
    }

    async void LoadFusionClientAsync(string sessionName)
    {
        var launcher = ResolveFusionLauncher();
        if (launcher == null)
        {
            SetStatus("ERROR: Add FusionSessionLauncher + NetworkRunner prefab.");
            Debug.LogError("[MultiplayerManager] FusionSessionLauncher missing.");
            return;
        }

        SetStatus("Fusion: connecting...");
        bool ok = await launcher.StartClientAsync(sessionName);
        if (!ok)
            SetStatus("Fusion client failed. Check session name / App Id.");
    }
}
