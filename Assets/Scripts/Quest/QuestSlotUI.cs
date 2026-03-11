using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Một slot trong panel Quest, hiển thị thông tin của một QuestData.
/// Prefab gợi ý: Image background + TitleText + StatusText + DescText
/// </summary>
public class QuestSlotUI : MonoBehaviour
{
    [Header("Text (TMP)")]
    public TextMeshProUGUI titleTMP;
    public TextMeshProUGUI statusTMP;
    public TextMeshProUGUI descTMP;
    public TextMeshProUGUI rewardTMP;

    [Header("Text (Legacy)")]
    public Text titleLegacy;
    public Text statusLegacy;
    public Text descLegacy;
    public Text rewardLegacy;

    [Header("Màu sắc trạng thái")]
    public Color colorLocked    = new Color(0.4f, 0.4f, 0.4f);
    public Color colorAvailable = Color.white;
    public Color colorActive    = new Color(1f, 0.85f, 0.2f);
    public Color colorCompleted = new Color(0.4f, 1f, 0.5f);

    [Header("Background Image (tuỳ chọn)")]
    public Image backgroundImage;

    QuestData _data;

    // ─────────────────────────────────────────────────────────────────────

    public void Bind(QuestData data)
    {
        _data = data;
        Refresh();
    }

    public void Refresh()
    {
        if (_data == null || QuestManager.Instance == null) return;

        var state = QuestManager.Instance.GetState(_data.questID);
        int step  = QuestManager.Instance.GetStepIndex(_data.questID);

        string statusStr  = StateLabel(state);
        string titleStr   = _data.questTitle;
        string descStr    = state == QuestManager.QuestState.Locked
                                ? "???"
                                : _data.questDescription;
        string rewardStr  = state == QuestManager.QuestState.Locked
                                ? ""
                                : $"Phần thưởng: {_data.rewardGold} Gold | {_data.rewardExp} EXP";

        // Nếu đang Active → hiện bước hiện tại
        if (state == QuestManager.QuestState.Active && _data.steps != null && _data.steps.Length > 0)
        {
            int idx = Mathf.Clamp(step, 0, _data.steps.Length - 1);
            descStr += $"\n→ {_data.steps[idx].instruction}";
        }

        Color col = StateColor(state);

        // TMP
        if (titleTMP)  { titleTMP.text  = titleStr;  titleTMP.color  = col; }
        if (statusTMP) { statusTMP.text = statusStr;  statusTMP.color = col; }
        if (descTMP)   { descTMP.text   = descStr; }
        if (rewardTMP) { rewardTMP.text = rewardStr; }

        // Legacy
        if (titleLegacy)  { titleLegacy.text  = titleStr;  titleLegacy.color  = col; }
        if (statusLegacy) { statusLegacy.text = statusStr;  statusLegacy.color = col; }
        if (descLegacy)   { descLegacy.text   = descStr; }
        if (rewardLegacy) { rewardLegacy.text = rewardStr; }

        // Background tint
        if (backgroundImage) backgroundImage.color = col * 0.25f + Color.black * 0.75f;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    string StateLabel(QuestManager.QuestState s) => s switch
    {
        QuestManager.QuestState.Locked    => "🔒 Chưa mở",
        QuestManager.QuestState.Available => "❕ Có thể nhận",
        QuestManager.QuestState.Active    => "⚔ Đang thực hiện",
        QuestManager.QuestState.Completed => "✅ Hoàn thành",
        _ => ""
    };

    Color StateColor(QuestManager.QuestState s) => s switch
    {
        QuestManager.QuestState.Locked    => colorLocked,
        QuestManager.QuestState.Available => colorAvailable,
        QuestManager.QuestState.Active    => colorActive,
        QuestManager.QuestState.Completed => colorCompleted,
        _ => Color.white
    };
}
