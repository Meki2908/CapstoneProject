using UnityEngine;
using Fusion;

/// <summary>
/// Gắn trong gameplay scene (Map_Chinh). Tự động disable player CÓ SẴN trong scene khi multiplayer.
///
/// Logic đơn giản:
///   - Multiplayer: tìm player scene-placed (KHÔNG có "(Clone)" trong tên) → disable
///   - Fusion-spawned players có "(Clone)" vì được Instantiate từ prefab → giữ nguyên
///   - Singleplayer: không làm gì
/// </summary>
public class LocalPlayerHandler : MonoBehaviour
{
    [Tooltip("Tag để tìm player local trong scene. Mặc định: 'Player'")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Delay (giây) trước khi check — đợi Fusion spawn xong.")]
    [SerializeField] private float checkDelay = 1.5f;

    /// <summary>Player local bị disable (lưu để enable lại khi disconnect).</summary>
    private GameObject _disabledLocalPlayer;

    public static LocalPlayerHandler Instance { get; private set; }

    void Awake() { Instance = this; }

    void Start()
    {
        Invoke(nameof(HandleLocalPlayer), checkDelay);
    }

    void HandleLocalPlayer()
    {
        // Kiểm tra có đang chơi multiplayer không
        bool isMultiplayer = false;
        var runners = FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None);
        foreach (var runner in runners)
        {
            if (runner.IsRunning)
            {
                isMultiplayer = true;
                break;
            }
        }

        if (!isMultiplayer)
        {
            Debug.Log("[LocalPlayerHandler] Singleplayer mode — giữ nguyên player local.");
            return;
        }

        Debug.Log("[LocalPlayerHandler] Multiplayer mode — tìm và disable scene player...");

        int disabledCount = 0;

        // Tìm tất cả root objects trong scene
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in rootObjects)
        {
            // Bỏ qua objects KHÔNG phải player
            if (!IsLikelyPlayer(go)) continue;

            // Player Fusion-spawned luôn có "(Clone)" trong tên
            // Player scene-placed KHÔNG có "(Clone)"
            if (go.name.Contains("(Clone)"))
            {
                Debug.Log($"[LocalPlayerHandler] ⏩ Bỏ qua Fusion-spawned: '{go.name}'");
                continue;
            }

            // Đây là player scene-placed → disable
            go.SetActive(false);
            _disabledLocalPlayer = go;
            disabledCount++;
            Debug.Log($"[LocalPlayerHandler] ❌ Disabled scene player: '{go.name}'");
        }

        if (disabledCount == 0)
            Debug.LogWarning("[LocalPlayerHandler] Không tìm thấy scene player nào để disable. Có thể player không tag 'Player' hoặc tên khác.");
        else
            Debug.Log($"[LocalPlayerHandler] ✅ Disabled {disabledCount} scene player(s). Fusion-spawned players vẫn active.");
    }

    /// <summary>Kiểm tra GameObject có phải player không.</summary>
    bool IsLikelyPlayer(GameObject go)
    {
        // Check tag trước
        try
        {
            if (go.CompareTag(playerTag)) return true;
        }
        catch { /* tag chưa tồn tại */ }

        // Check tên
        string name = go.name.ToLower();
        if (name.Contains("player") &&
            !name.Contains("spawn") &&
            !name.Contains("canvas") &&
            !name.Contains("handler") &&
            !name.Contains("manager") &&
            !name.Contains("nametag") &&
            !name.Contains("camera") &&
            !name.Contains("event"))
        {
            return true;
        }

        return false;
    }

    /// <summary>Enable lại player local (gọi khi disconnect/quay về singleplayer).</summary>
    public void RestoreLocalPlayer()
    {
        if (_disabledLocalPlayer != null)
        {
            _disabledLocalPlayer.SetActive(true);
            Debug.Log($"[LocalPlayerHandler] ✅ Restored local player: '{_disabledLocalPlayer.name}'");
            _disabledLocalPlayer = null;
        }
    }
}
