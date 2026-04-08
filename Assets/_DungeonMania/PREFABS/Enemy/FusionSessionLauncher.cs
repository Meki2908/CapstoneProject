#if PHOTON_FUSION
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Khởi chạy Photon Fusion (Host/Client) và load scene gameplay qua <see cref="NetworkSceneInfo"/>.
/// Gán prefab <see cref="NetworkRunner"/> từ Fusion (sample) và build index scene trong Inspector.
/// Cần cấu hình Photon App Id (Fusion Hub / Resources) trước khi chạy.
/// </summary>
public class FusionSessionLauncher : MonoBehaviour
{
    public static FusionSessionLauncher Instance { get; private set; }

    /// <summary>True sau khi StartGame host/client thành công.</summary>
    public static bool IsFusionSessionActive { get; private set; }

    /// <summary>
    /// True ngay trước khi gọi StartGame — tránh race: NGO bootstrap trong scene mới có thể chạy trước khi await hoàn tất.
    /// </summary>
    public static bool SkipNgoBootstrap { get; private set; }

    public static string CurrentSessionName { get; private set; }

    [Header("Fusion")]
    [Tooltip("Prefab NetworkRunner từ Fusion sample (có thể kèm NetworkSceneManagerDefault).")]
    [SerializeField] private NetworkRunner runnerPrefab;

    [Tooltip("Scene gameplay trong Build Settings. Để trống sẽ thử Assets/Scenes/<tên>.unity")]
    [SerializeField] private string gameplaySceneName = "Map_Chinh";

    [Tooltip("Nếu >= 0, bỏ qua resolve tên scene và dùng index trực tiếp.")]
    [SerializeField] private int gameplaySceneBuildIndexOverride = -1;

    [SerializeField] private int maxPlayers = 8;

    private NetworkRunner _runner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public async Task<bool> StartHostAsync(string sessionName)
    {
        return await StartGameInternal(GameMode.Host, sessionName);
    }

    public async Task<bool> StartClientAsync(string sessionName)
    {
        return await StartGameInternal(GameMode.Client, sessionName);
    }

    private async Task<bool> StartGameInternal(GameMode mode, string sessionName)
    {
        if (runnerPrefab == null)
        {
            Debug.LogError("[FusionSessionLauncher] Chưa gán NetworkRunner prefab (Fusion sample).");
            return false;
        }

        if (string.IsNullOrWhiteSpace(sessionName))
        {
            Debug.LogError("[FusionSessionLauncher] Session name trống.");
            return false;
        }

        int sceneIdx = ResolveGameplayBuildIndex();
        if (sceneIdx < 0)
        {
            Debug.LogError("[FusionSessionLauncher] Không resolve được build index cho scene gameplay. Kiểm tra Build Settings và gameplaySceneBuildIndexOverride.");
            return false;
        }

        CurrentSessionName = sessionName.Trim();
        IsFusionSessionActive = false;
        SkipNgoBootstrap = true;

        if (_runner != null)
        {
            Destroy(_runner.gameObject);
            _runner = null;
        }

        _runner = Instantiate(runnerPrefab);
        DontDestroyOnLoad(_runner.gameObject);

        INetworkSceneManager sceneManager = _runner.GetComponent<INetworkSceneManager>();
        if (sceneManager == null)
            sceneManager = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(sceneIdx), LoadSceneMode.Single);

        var args = new StartGameArgs
        {
            GameMode = mode,
            SessionName = CurrentSessionName,
            Scene = sceneInfo,
            SceneManager = sceneManager,
            PlayerCount = maxPlayers,
        };

        StartGameResult result;
        try
        {
            result = await _runner.StartGame(args);
        }
        catch (System.Exception e)
        {
            SkipNgoBootstrap = false;
            Debug.LogError($"[FusionSessionLauncher] StartGame exception: {e.Message}");
            return false;
        }

        bool ok = result.Ok;
        if (!ok)
        {
            SkipNgoBootstrap = false;
            Debug.LogError($"[FusionSessionLauncher] StartGame thất bại: {DescribeResult(result)}");
        }

        IsFusionSessionActive = ok;
        return ok;
    }

    private static string DescribeResult(StartGameResult result)
    {
        return $"{result.ShutdownReason} Ok={result.Ok} {result.ErrorMessage}";
    }

    private int ResolveGameplayBuildIndex()
    {
        if (gameplaySceneBuildIndexOverride >= 0)
            return gameplaySceneBuildIndexOverride;

        string[] candidates = { $"Assets/Scenes/{gameplaySceneName}.unity" };

        foreach (var path in candidates)
        {
            int idx = SceneUtility.GetBuildIndexByScenePath(path);
            if (idx >= 0)
                return idx;
        }

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (!string.IsNullOrEmpty(path) && path.EndsWith(gameplaySceneName + ".unity"))
                return i;
        }

        return -1;
    }
}
#else
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Stub khi chưa import Photon Fusion. Sau khi import SDK: Player → Scripting Define Symbols → thêm <b>PHOTON_FUSION</b> (vd. Standalone), rồi gán NetworkRunner trên bản đầy đủ.
/// </summary>
public class FusionSessionLauncher : MonoBehaviour
{
    public static FusionSessionLauncher Instance { get; private set; }

    public static bool IsFusionSessionActive { get; private set; }

    public static bool SkipNgoBootstrap { get; private set; }

    public static string CurrentSessionName { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public Task<bool> StartHostAsync(string sessionName)
    {
        Debug.LogWarning(
            "[FusionSessionLauncher] Photon Fusion chưa được bật. Import Photon Fusion 2 SDK, rồi thêm scripting define PHOTON_FUSION (Edit → Project Settings → Player).");
        return Task.FromResult(false);
    }

    public Task<bool> StartClientAsync(string sessionName)
    {
        Debug.LogWarning(
            "[FusionSessionLauncher] Photon Fusion chưa được bật. Import SDK và thêm PHOTON_FUSION vào Scripting Define Symbols.");
        return Task.FromResult(false);
    }
}
#endif
