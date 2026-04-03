using UnityEngine;
using TMPro;

/// <summary>
/// Displays persistent Quest information at the corner of the screen (HUD).
/// Always visible to let players know their current objective.
/// </summary>
public class QuestHUD : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;
    public GameObject rootPanel; // The HUD container

    private void Start()
    {
        RefreshHUD(0);
    }

    private void OnEnable()
    {
        QuestManager.OnQuestAccepted += RefreshHUD;
        QuestManager.OnQuestStepAdvanced += RefreshHUD;
        QuestManager.OnQuestCompleted += RefreshHUD;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestAccepted -= RefreshHUD;
        QuestManager.OnQuestStepAdvanced -= RefreshHUD;
        QuestManager.OnQuestCompleted -= RefreshHUD;
    }

    public void RefreshHUD(int questID)
    {
        var manager = QuestManager.Instance;
        if (manager == null) return;

        QuestData activeQuest = manager.GetActiveQuest();
        QuestStep activeStep = manager.GetActiveStep();

        if (activeQuest != null)
        {
            // Chỉ update text, không tự toggle panel
            // Panel được bật/tắt bởi QuestJournalUI (phím J)
            if (titleText) titleText.text = activeQuest.questTitle;
            if (instructionText) 
            {
                if (activeStep != null) instructionText.text = activeStep.instruction;
                else instructionText.text = "Goal reached!";
            }
        }
    }
}
