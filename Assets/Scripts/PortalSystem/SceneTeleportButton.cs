using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Script đơn giản gắn trực tiếp vào Button để chuyển Scene.
/// Có thể tự động advance/complete quest khi button được ấn.
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneTeleportButton : MonoBehaviour
{
    [Header("── Scene Settings ──")]
    [Tooltip("Tên chính xác của Scene muốn chuyển đến (phải có trong Build Settings)")]
    public string targetSceneName;

    [Tooltip("Delay trước khi chuyển (để kịp nghe tiếng click hoặc chạy hiệu ứng)")]
    public float delay = 0.2f;

    [Header("── Quest Advance (tuỳ chọn) ──")]
    [Tooltip("Set questID > 0 để advance quest khi ấn button này.")]
    public int questID       = 0;
    [Tooltip("Bước hiện tại phải đang ở đây thì mới advance.")]
    public int triggerAtStep = 4;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick()
    {
        TryAdvanceQuest();   // advance/complete quest trước khi đổi scene

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[SceneTeleportButton] Nút {gameObject.name} chưa nhập tên Scene!");
            return;
        }

        Debug.Log($"[SceneTeleportButton] Chuẩn bị chuyển đến: {targetSceneName}");
        Invoke(nameof(ExecuteTeleport), delay);
    }

    void TryAdvanceQuest()
    {
        if (questID <= 0 || QuestManager.Instance == null) return;
        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);
        if (state == QuestManager.QuestState.Active && step == triggerAtStep)
        {
            QuestManager.Instance.AdvanceStep(questID);
            Debug.Log($"[SceneTeleportButton] Quest {questID} step {triggerAtStep} advanced → quest may complete.");
        }
    }

    private void ExecuteTeleport()
    {
        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.GoToScene(targetSceneName, "Đang chuyển vùng...");
            else
                SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[SceneTeleportButton] LỖI: Scene '{targetSceneName}' không tồn tại hoặc chưa thêm vào Build Settings!");
        }
    }
}

