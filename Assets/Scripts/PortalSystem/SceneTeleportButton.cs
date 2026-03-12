using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Script đơn giản gắn trực tiếp vào Button để chuyển Scene.
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneTeleportButton : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Tên chính xác của Scene muốn chuyển đến (phải có trong Build Settings)")]
    public string targetSceneName;

    [Tooltip("Delay trước khi chuyển (để kịp nghe tiếng click hoặc chạy hiệu ứng)")]
    public float delay = 0.2f;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[SceneTeleportButton] Nút {gameObject.name} chưa nhập tên Scene!");
            return;
        }

        Debug.Log($"[SceneTeleportButton] Chuẩn bị chuyển đến: {targetSceneName}");
        Invoke(nameof(ExecuteTeleport), delay);
    }

    private void ExecuteTeleport()
    {
        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            // Đảm bảo game chạy bình thường, không bị dừng
            Time.timeScale = 1f;
            
            // Ẩn chuột trước khi sang scene mới (tùy chọn)
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[SceneTeleportButton] LỖI: Scene '{targetSceneName}' không tồn tại hoặc chưa thêm vào Build Settings!");
        }
    }
}
