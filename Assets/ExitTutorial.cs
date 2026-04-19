using UnityEngine;
using UnityEngine.SceneManagement;  // Để sử dụng SceneManagement

public class SceneSwitcher : MonoBehaviour
{
    public string sceneName;  // Tên scene cần chuyển

    void Update()
    {
        // KHÔNG nhận lệnh nếu game đang Pause (tức là đang mở Menu/Settings)
        if (Time.timeScale == 0f) return;

        // Bỏ qua nếu giao diện UI đang che màn hình (để tránh đụng phím với Menu)
        if (CursorUIPriority.IsUiOverlayActive) return;

        // Kiểm tra nếu người dùng nhấn phím X lúc đang chơi bình thường
        if (Input.GetKeyDown(KeyCode.X))
        {
            SwitchScene(sceneName);  // Gọi hàm chuyển scene
        }
    }

    // Hàm này sẽ được gọi khi phím X được nhấn
    public void SwitchScene(string sceneName)
    {
        // Kiểm tra nếu sceneName không rỗng và tồn tại trong Build Settings
        if (!string.IsNullOrEmpty(sceneName))
        {
            // Chuyển đến scene có tên đã chỉ định
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.GoToScene(sceneName, "Đang chuyển cảnh...");
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}