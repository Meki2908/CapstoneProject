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

    [Tooltip("Text hiển thị trạng thái (connecting, error...).")]
    [SerializeField] private TextMeshProUGUI statusText;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ───────────────────── HOST ─────────────────────

    /// <summary>Gọi từ nút UI: tạo phòng (relay hoặc LAN) rồi load scene gameplay.</summary>
    public async void StartHostAndLoadGame()
    {
        UsingRelay = useRelay;

        if (useRelay)
        {
            SetStatus("Đang tạo phòng...");
            string joinCode = await RelayManager.Instance.CreateRelayAsync();
            if (joinCode == null)
            {
                SetStatus("Lỗi tạo phòng! Kiểm tra kết nối internet.");
                return;
            }

            // Hiển thị join code cho host
            if (joinCodeDisplay != null)
                joinCodeDisplay.text = joinCode;

            SetStatus($"Mã phòng: {joinCode}");
            Debug.Log($"[MultiplayerManager] Relay Host created. Code: {joinCode}");
        }

        _pendingClient = false;
        _pendingConnectPort = 0;
        _pendingHost = true;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    // ───────────────────── CLIENT ─────────────────────

    /// <summary>Gọi từ nút UI: nhập join code → join relay → load scene gameplay.</summary>
    public async void StartClientAndLoadGame()
    {
        UsingRelay = useRelay;

        if (useRelay)
        {
            string code = joinCodeInput != null ? joinCodeInput.text : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("Nhập mã phòng trước!");
                return;
            }

            SetStatus("Đang kết nối...");
            bool ok = await RelayManager.Instance.JoinRelayAsync(code);
            if (!ok)
            {
                SetStatus("Không thể kết nối! Kiểm tra mã phòng.");
                return;
            }

            SetStatus("Đã kết nối. Đang tải...");
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
        Debug.Log($"[MultiplayerManager] {msg}");
    }
}
