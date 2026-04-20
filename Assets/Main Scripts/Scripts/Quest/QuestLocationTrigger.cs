using UnityEngine;

/// <summary>
/// Đặt trên GameObject cổng Dungeon Gate (hoặc bất kỳ điểm đến nào).
/// Khi player bước vào trigger, quest sẽ advance tới bước tiếp theo
/// (hoặc hoàn thành nếu hết bước).
/// </summary>
public class QuestLocationTrigger : MonoBehaviour
{
    [Header("Quest & Bước")]
    public int questID = 1;

    [Tooltip("Trigger bước nào thì advance (0-based). Phải khớp bước hiện tại của quest.")]
    public int triggerAtStep = 1;   // Bước 1 = 'Đến cổng dungeon' 

    [Header("Player")]
    public string playerTag = "Player";

    [Header("Hiệu ứng khi advance")]
    public GameObject[] activateOnAdvance;
    public GameObject[] deactivateOnAdvance;
    public ParticleSystem celebrationFX;

    bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;
        if (QuestManager.Instance == null) return;

        var qd    = QuestManager.Instance.GetQuestData(questID);
        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);

        if (state != QuestManager.QuestState.Active) return;
        if (step != triggerAtStep) return;

        // BẢO VỆ NGHIÊM NGẶT - NGĂN TRIGGER ẨN BỎ QUA HỘI THOẠI CỦA MARIA
        // Tránh trường hợp Trigger đặt ở vùng người chơi vừa teleport tới vô tình tự động hoàn thành luôn việc phải nói chuyện với Maria!
        if ((questID == 2 && triggerAtStep == 2) || 
            (questID == 3 && triggerAtStep == 2) || 
            (questID == 4 && triggerAtStep == 0))
        {
            Debug.LogWarning($"[QuestLocationTrigger] Ngăn chặn trigger ẩn tự động bỏ qua Bước {triggerAtStep} của Quest {questID} (Bước này bắt buộc phải nói chuyện với Maria).");
            return;
        }

        _triggered = true;
        QuestManager.Instance.AdvanceStep(questID);

        foreach (var go in activateOnAdvance)   if (go) go.SetActive(true);
        foreach (var go in deactivateOnAdvance) if (go) go.SetActive(false);
        if (celebrationFX) celebrationFX.Play();

        Debug.Log($"[QuestLocationTrigger] Quest {questID} Bước {triggerAtStep} → hoàn thành tại '{gameObject.name}'");
    }

    /// <summary>Reset để có thể trigger lại (dùng khi debug).</summary>
    public void Reset() => _triggered = false;
}
