using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên Canvas ở scene menu. Nút Start host gọi <see cref="StartHostAndLoadGame"/>.
/// NetworkManager chỉ ở scene gameplay (Testboss); <see cref="NetworkHostBootstrap"/> gọi StartHost sau khi load.
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    /// <summary>Đặt true trước khi LoadScene; bootstrap gameplay đọc và gọi StartHost một lần.</summary>
    public static bool PendingStartHostAfterSceneLoad { get; private set; }

    [Tooltip("Tên scene trong Build Settings (không cần đường dẫn).")]
    [SerializeField] private string gameplaySceneName = "Testboss";

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>Gọi từ nút UI: load scene gameplay rồi host (spawn player theo NetworkManager).</summary>
    public void StartHostAndLoadGame()
    {
        PendingStartHostAfterSceneLoad = true;
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public static void ClearPendingHostFlag()
    {
        PendingStartHostAfterSceneLoad = false;
    }
}
