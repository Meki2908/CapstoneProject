using UnityEngine;
using Fusion;

/// <summary>
/// Gắn trong gameplay scene (Map_Chinh, Dungeon, Battle...).
/// Khi ONLINE: disable toàn bộ scene player (Player_3.0...) → Fusion KHÔNG spawn/điều khiển được.
/// Khi SINGLEPLAYER: không làm gì → scene player hoạt động bình thường.
///
/// FIX v4: Disable GameObject (thay vì chỉ disable NetworkObject).
///         Áp dụng cho TẤT CẢ gameplay scenes khi multiplayer.
/// </summary>
[DefaultExecutionOrder(-250)]
public class LocalPlayerHandler : MonoBehaviour
{
    [Tooltip("Tag để tìm player local trong scene. Mặc định: 'Player'")]
    [SerializeField] private string playerTag = "Player";

    /// <summary>Scene player đã bị disable (lưu để enable lại khi disconnect về singleplayer).</summary>
    private GameObject _disabledScenePlayer;

    /// <summary>Đã xử lý scene load chưa (tránh gọi 2 lần khi cùng scene).</summary>
    private bool _hasProcessedScene;

    public static LocalPlayerHandler Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Fire khi scene ĐÃ load xong hoàn toàn.
    /// Tìm scene player và disable toàn bộ GameObject → Fusion bỏ qua hoàn toàn.
    /// </summary>
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (!IsGameplayScene(scene.name)) return;
        StartCoroutine(ProcessGameplaySceneOnLoad(scene.name));
    }

    System.Collections.IEnumerator ProcessGameplaySceneOnLoad(string sceneName)
    {
        yield return null; // Đợi 1 frame để scene ổn định

        if (_hasProcessedScene) yield break;
        _hasProcessedScene = true;

        TryDisableScenePlayer();
    }

    /// <summary>
    /// Tìm scene player (Player_3.0...) trong scene hiện tại và disable toàn bộ GameObject.
    /// Khi online: Fusion không thể spawn/đăng ký object này.
    /// Public để NetworkLobbyManager có thể gọi khi lobby cùng scene với gameplay.
    /// </summary>
    public void TryDisableScenePlayer()
    {
        // Kiểm tra multiplayer
        bool isOnline = IsOnlineSession();

        if (!isOnline)
        {
            Debug.Log("[LocalPlayerHandler] Singleplayer mode — giữ nguyên scene player.");
            return;
        }

        Debug.Log("[LocalPlayerHandler] ONLINE MODE — searching for scene player to disable...");

        // Tìm scene player bằng tag
        GameObject scenePlayer = null;

        try
        {
            var tagged = GameObject.FindGameObjectWithTag(playerTag);
            if (tagged != null && !tagged.name.Contains("(Clone)"))
            {
                scenePlayer = tagged;
            }
        }
        catch { /* tag chưa tồn tại */ }

        // Fallback: tìm trong root objects
        if (scenePlayer == null)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in roots)
            {
                if (IsLikelyPlayer(go) && !go.name.Contains("(Clone)"))
                {
                    scenePlayer = go;
                    break;
                }
            }
        }

        if (scenePlayer != null)
        {
            // Disable toàn bộ GameObject → Fusion KHÔNG thể spawn hay điều khiển
            scenePlayer.SetActive(false);
            _disabledScenePlayer = scenePlayer;

            Debug.Log($"[LocalPlayerHandler] ✅ DISABLED scene player: '{scenePlayer.name}' " +
                       $"— Fusion will use spawned prefab instead. GameObject.SetActive(false) = " +
                       $"GameObject.activeInHierarchy:{scenePlayer.activeInHierarchy}");
        }
        else
        {
            Debug.Log("[LocalPlayerHandler] ⚠️ No scene player found — Fusion will spawn from prefab (expected in some setups).");
        }

        VerifySetup();
    }

    void VerifySetup()
    {
        if (!IsOnlineSession()) return;

        int spawnedCount = 0;
        int scenePlayerCount = 0;

        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
        {
            if (!IsLikelyPlayer(go)) continue;

            if (go.name.Contains("(Clone)"))
                spawnedCount++;
            else
                scenePlayerCount++;
        }

        Debug.Log($"[LocalPlayerHandler] ✅ Final: {spawnedCount} spawned (Fusion prefab), " +
                  $"{scenePlayerCount} scene player(s) (should be disabled)");
    }

    bool IsOnlineSession()
    {
        var runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (var runner in runners)
        {
            if (runner.IsRunning) return true;
        }
        return false;
    }

    bool IsLikelyPlayer(GameObject go)
    {
        try { if (go.CompareTag(playerTag)) return true; }
        catch { }

        string name = go.name.ToLower();
        return name.Contains("player") &&
               !name.Contains("spawn") &&
               !name.Contains("canvas") &&
               !name.Contains("handler") &&
               !name.Contains("manager") &&
               !name.Contains("nametag") &&
               !name.Contains("camera") &&
               !name.Contains("event") &&
               !name.Contains("ui");
    }

    bool IsGameplayScene(string sceneName)
    {
        return sceneName.Contains("Map") ||
               sceneName.Contains("Dungeon") ||
               sceneName.Contains("Battle") ||
               sceneName.Contains("Chinh") ||
               sceneName.Contains("Gameplay");
    }

    /// <summary>
    /// Enable lại scene player khi disconnect về singleplayer.
    /// Gọi từ MultiplayerManager khi thoát room.
    /// </summary>
    public void RestoreScenePlayer()
    {
        if (_disabledScenePlayer != null)
        {
            _disabledScenePlayer.SetActive(true);
            Debug.Log($"[LocalPlayerHandler] ✅ Restored scene player: '{_disabledScenePlayer.name}'");
            _disabledScenePlayer = null;
            _hasProcessedScene = false;
        }
    }
}
