using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên TeleportPoint_CityGate (hoặc bất kỳ trigger nào).
/// Khi QuestManager.AdvanceStep() đưa quest đến đúng step → tự động chuyển scene.
///
/// Setup cho Quest 2:
///   questID       = 2
///   triggerAtStep = 2      ← chuyển scene sau khi step lên 2
///   targetScene   = "Setup Scene"  (hoặc tên scene City Gate của bạn)
/// </summary>
public class TeleportOnQuestStep : MonoBehaviour
{
    [Header("Quest Setting")]
    public int questID       = 2;
    public int triggerAtStep = 2;

    [Header("Scene để chuyển đến")]
    [Tooltip("Tên scene đúng trong Build Settings")]
    public string targetScene = "Setup Scene";

    [Header("Delay trước khi chuyển (giây)")]
    public float delay = 1.5f;

    [Header("Loading message")]
    public string loadingMessage = "Đang di chuyển đến Cổng Thành...";

    bool _done = false;

    void OnEnable()
    {
        QuestManager.OnQuestStepAdvanced += OnStepAdvanced;
    }

    void OnDisable()
    {
        QuestManager.OnQuestStepAdvanced -= OnStepAdvanced;
    }

    void OnStepAdvanced(int id)
    {
        if (_done) return;
        if (id != questID) return;
        if (QuestManager.Instance == null) return;

        int currentStep = QuestManager.Instance.GetStepIndex(questID);
        if (currentStep != triggerAtStep) return;

        _done = true;
        Debug.Log($"[TeleportOnQuestStep] Quest {questID} Step {triggerAtStep} → Teleporting to '{targetScene}'");
        StartCoroutine(DoTeleport());
    }

    System.Collections.IEnumerator DoTeleport()
    {
        // Lưu game trước khi chuyển cảnh
        try { GameController.PlayerSave(); }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TeleportOnQuestStep] PlayerSave failed: {e.Message}");
        }

        yield return new WaitForSeconds(delay);

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.GoToScene(targetScene, loadingMessage);
        else
            SceneManager.LoadScene(targetScene);
    }
}
