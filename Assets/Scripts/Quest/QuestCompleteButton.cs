using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên Button trong Dungeon UI Canvas.
/// Khi player nhấn button → AdvanceStep hoặc CompleteQuest trực tiếp.
///
/// Setup trong Unity:
///   1. Kéo script này vào Button GameObject
///   2. Trong Button component → OnClick() → kéo chính GameObject này vào → chọn OnButtonClicked()
///   3. Điền questID = 2, mode theo ý muốn
/// </summary>
public class QuestCompleteButton : MonoBehaviour
{
    [Header("Quest cần hoàn thành")]
    public int questID = 2;

    public enum Mode
    {
        AdvanceStep,    // Advance bước hiện tại (nếu còn bước → tự Complete khi hết)
        ForceComplete,  // Complete quest ngay lập tức, bất kể đang ở bước nào
    }

    [Tooltip("AdvanceStep: tiến bước tiếp (tự complete khi hết bước)\nForceComplete: hoàn thành ngay")]
    public Mode mode = Mode.AdvanceStep;

    [Header("Chỉ cho phép nhấn 1 lần")]
    public bool onlyOnce = true;

    [Header("Tắt Button sau khi nhấn (optional)")]
    public bool disableButtonAfter = true;

    bool _used = false;

    /// <summary>
    /// Kéo hàm này vào OnClick() của Button trong Inspector.
    /// </summary>
    public void OnButtonClicked()
    {
        if (onlyOnce && _used) return;
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestCompleteButton] QuestManager.Instance is NULL!");
            return;
        }

        var state = QuestManager.Instance.GetState(questID);
        if (state != QuestManager.QuestState.Active)
        {
            Debug.LogWarning($"[QuestCompleteButton] Quest {questID} không ở trạng thái Active (hiện là {state}). Bỏ qua.");
            return;
        }

        _used = true;

        switch (mode)
        {
            case Mode.AdvanceStep:
                QuestManager.Instance.AdvanceStep(questID);
                Debug.Log($"[QuestCompleteButton] AdvanceStep Quest {questID}");
                break;

            case Mode.ForceComplete:
                QuestManager.Instance.CompleteQuest(questID);
                Debug.Log($"[QuestCompleteButton] ForceComplete Quest {questID}");
                break;
        }

        if (disableButtonAfter)
        {
            var btn = GetComponent<Button>();
            if (btn) btn.interactable = false;
        }
    }
}
