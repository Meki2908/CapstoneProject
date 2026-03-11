using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD góc màn hình hiển thị nhiệm vụ đang Active.
/// Gắn lên Canvas (World Space hoặc Screen Space Overlay).
/// 
/// Cách setup trong Unity:
///   1. Tạo GameObject "QuestHUD" trong Canvas
///   2. Gắn script này
///   3. Gán các Text/TMP references bên dưới
/// </summary>
public class QuestHUDController : MonoBehaviour
{
    [Header("UI References (TMP)")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI stepInstructionText;

    [Header("UI References (Legacy Text – nếu không dùng TMP)")]
    public Text questTitleLegacy;
    public Text stepInstructionLegacy;

    [Header("Hiện/ẩn toàn bộ panel HUD")]
    public GameObject hudPanel;

    // ─── Unity lifecycle ──────────────────────────────────────────────────

    void OnEnable()
    {
        QuestManager.OnQuestAccepted     += Refresh;
        QuestManager.OnQuestStepAdvanced += Refresh;
        QuestManager.OnQuestCompleted    += Refresh;
    }

    void OnDisable()
    {
        QuestManager.OnQuestAccepted     -= Refresh;
        QuestManager.OnQuestStepAdvanced -= Refresh;
        QuestManager.OnQuestCompleted    -= Refresh;
    }

    void Start() => Refresh(-1);

    // ─── Public ───────────────────────────────────────────────────────────

    public void Refresh(int _)
    {
        if (QuestManager.Instance == null) return;

        var activeQuest = QuestManager.Instance.GetActiveQuest();
        var activeStep  = QuestManager.Instance.GetActiveStep();

        bool hasActive = activeQuest != null && activeStep != null;

        if (hudPanel) hudPanel.SetActive(hasActive);

        if (!hasActive) return;

        string title       = activeQuest.questTitle;
        string instruction = activeStep.instruction;

        // TMP
        if (questTitleText)      questTitleText.text      = title;
        if (stepInstructionText) stepInstructionText.text = instruction;

        // Legacy Text
        if (questTitleLegacy)      questTitleLegacy.text      = title;
        if (stepInstructionLegacy) stepInstructionLegacy.text = instruction;
    }

    /// <summary>Gọi khi muốn ẩn HUD thủ công (VD: khi mở pause menu).</summary>
    public void Hide() { if (hudPanel) hudPanel.SetActive(false); }

    /// <summary>Gọi khi muốn hiện lại HUD.</summary>
    public void Show() => Refresh(-1);
}
