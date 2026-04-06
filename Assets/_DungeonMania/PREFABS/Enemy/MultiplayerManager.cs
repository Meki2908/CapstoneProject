using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên Canvas menu (Pre Start / Canvas_Menu). Nút Create Host / Join Host gọi
/// <see cref="StartHostAndLoadGame"/> / <see cref="StartClientAndLoadGame"/> hoặc
/// <see cref="CommitPendingJoinFromMenu"/> trước <see cref="SceneTransitionManager.GoToScene"/>.
/// NetworkManager chỉ ở scene gameplay; <see cref="NetworkHostBootstrap"/> / <see cref="NetworkClientBootstrap"/> gọi Start sau khi load.
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    static bool _pendingHost;
    static bool _pendingClient;
    static string _pendingConnectAddress = "127.0.0.1";
    /// <summary>0 = cổng từ scene (thường 7777; không offset ParrelSync cho client). &gt;0 = ép cổng (vd. 7778 khi join host clone cùng máy).</summary>
    static ushort _pendingConnectPort;

    /// <summary>Đặt true trước khi LoadScene; bootstrap gameplay đọc và gọi StartHost một lần.</summary>
    public static bool PendingStartHostAfterSceneLoad => _pendingHost;

    /// <summary>Đặt true trước khi LoadScene; bootstrap gameplay đọc và gọi StartClient một lần.</summary>
    public static bool PendingStartClientAfterSceneLoad => _pendingClient;

    /// <summary>Địa chỉ dùng khi Join (đã set trước khi load scene).</summary>
    public static string PendingConnectAddress => _pendingConnectAddress;

    /// <summary>0 = auto; &gt;0 = cổng kết nối tới host (đọc trong NetworkClientBootstrap).</summary>
    public static ushort PendingConnectPort => _pendingConnectPort;

    [Tooltip("Tên scene trong Build Settings (không cần đường dẫn).")]
    [SerializeField] private string gameplaySceneName = "Map_Chinh";

    [Tooltip("IP host khi bấm Join (LAN hoặc 127.0.0.1).")]
    [SerializeField] private string connectAddress = "127.0.0.1";

    [Tooltip("Cổng host. 0 = dùng cổng scene (7777). Không dùng offset ParrelSync cho client — join Editor từ clone: để 0. Join host clone cùng máy (vd. 7778): nhập cổng đó.")]
    [SerializeField] private ushort connectPort = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Gọi từ nút UI: load scene gameplay rồi host.</summary>
    public void StartHostAndLoadGame()
    {
        _pendingClient = false;
        _pendingConnectPort = 0;
        _pendingHost = true;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    /// <summary>Gọi từ nút UI: load scene gameplay rồi kết nối client tới host (không dùng SceneTransitionManager).</summary>
    public void StartClientAndLoadGame()
    {
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
}
