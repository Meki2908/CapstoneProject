using UnityEngine;

/// <summary>
/// Đặt script này lên NPC Leona (hoặc bất kỳ NPC nào phát quest).
/// Khi player tương tác xong hội thoại → gọi AcceptQuest.
/// Tự động chuyển sang bước tiếp theo nếu quest đang Active tại bước "Gặp NPC này".
/// </summary>
public class QuestNPCInteract : MonoBehaviour
{
    [Header("Quest sẽ được nhận khi tương tác NPC này")]
    public int questID = 1;

    [Header("Bước nào trong quest tương ứng với NPC này (0-based)")]
    [Tooltip("Bước 0 = bước đầu tiên 'Gặp Leona'. Khi player tương tác xong, quest sẽ được accept và tiến sang bước 1.")]
    public int triggerAtStep = -1; // -1 = accept quest (Available → Active), ≥ 0 = advance step

    [Header("Callback sau khi quest accepted / step advanced")]
    [Tooltip("Gắn GameObjects muốn bật/tắt sau khi quest nhận (VD: ẩn dialog, bật marker cổng)")]
    public GameObject[] activateOnAccept;
    public GameObject[] deactivateOnAccept;

    // ─── Public method – gọi từ button UI dialog hoặc từ event ───────────

    /// <summary>
    /// Gọi hàm này sau khi player xem xong đoạn hội thoại với NPC.
    /// Có thể gọi từ Unity Event trên Button "Đóng hội thoại".
    /// </summary>
    public void OnDialogueFinished()
    {
        if (QuestManager.Instance == null) return;

        bool success = false;

        if (triggerAtStep < 0)
        {
            // Chế độ ACCEPT: Available → Active
            success = QuestManager.Instance.AcceptQuest(questID);
        }
        else
        {
            // Chế độ ADVANCE STEP: chỉ tiến nếu đang đúng bước
            var state = QuestManager.Instance.GetState(questID);
            int  cur   = QuestManager.Instance.GetStepIndex(questID);

            if (state == QuestManager.QuestState.Active && cur == triggerAtStep)
            {
                QuestManager.Instance.AdvanceStep(questID);
                success = true;
            }
        }

        if (success) ApplyCallbacks();
    }

    // ─── Private ─────────────────────────────────────────────────────────

    void ApplyCallbacks()
    {
        foreach (var go in activateOnAccept)   if (go) go.SetActive(true);
        foreach (var go in deactivateOnAccept) if (go) go.SetActive(false);
    }

    // ─── Tự gọi khi trigger nếu không dùng Button ───────────────────────

    [Header("Tự động gọi OnDialogueFinished khi trigger (không cần button)")]
    public bool autoTrigger = false;
    public string playerTag = "Player";
    bool _triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!autoTrigger || _triggered) return;
        if (!other.CompareTag(playerTag)) return;
        _triggered = true;
        OnDialogueFinished();
    }
}
