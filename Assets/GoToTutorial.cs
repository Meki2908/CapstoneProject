using UnityEngine;
using UnityEngine.SceneManagement;  // Để sử dụng SceneManagement

public class TriggerCanvas : MonoBehaviour
{
    public GameObject canvas;  // Canvas cần hiển thị
    public string sceneName;   // Tên Scene cần chuyển đến
    private bool isInTriggerZone = false;  // Kiểm tra xem người chơi có trong Trigger không

    void Start()
    {
        // Ẩn Canvas khi bắt đầu
        canvas.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        // Kiểm tra nếu đối tượng va chạm là nhân vật (hoặc tag của nhân vật)
        if (other.CompareTag("Player"))
        {
            isInTriggerZone = true;
            canvas.SetActive(true);  // Hiển thị Canvas khi va chạm
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Kiểm tra nếu đối tượng rời khỏi Trigger Zone
        if (other.CompareTag("Player"))
        {
            isInTriggerZone = false;
            canvas.SetActive(false);  // Ẩn Canvas khi người chơi rời khỏi Trigger
        }
    }

    void Update()
    {
        // Nếu người chơi đang trong Trigger và nhấn F
        if (isInTriggerZone && Input.GetKeyDown(KeyCode.F))
        {
            // Chuyển sang Scene mới
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.GoToScene(sceneName, "Đang chuyển cảnh...");
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}