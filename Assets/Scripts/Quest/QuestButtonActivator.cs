using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enables/disables a UI Button based on quest state & step.
/// Attach to the same GameObject as the Button, or assign buttonTarget.
/// 
/// Use case: Battlefield teleport button – disabled until Quest 3 step >= 1.
/// </summary>
[RequireComponent(typeof(Button))]
public class QuestButtonActivator : MonoBehaviour
{
    [Header("── Quest Condition ──")]
    public int questID      = 3;
    public int enableAtStep = 1;   // Enable when step >= this value

    [Header("── Optional ──")]
    [Tooltip("Leave empty to use Button on this GameObject")]
    public Button buttonTarget;

    Button _btn;

    void Awake()
    {
        _btn = buttonTarget != null ? buttonTarget : GetComponent<Button>();
    }

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

    void Start()
    {
        StartCoroutine(DelayedRefresh());
    }

    System.Collections.IEnumerator DelayedRefresh()
    {
        yield return null;
        Refresh(0);
    }

    void Refresh(int id)
    {
        if (_btn == null || QuestManager.Instance == null) return;

        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);

        bool shouldEnable = state == QuestManager.QuestState.Active && step >= enableAtStep
                         || state == QuestManager.QuestState.Completed;

        _btn.interactable = shouldEnable;
        Debug.Log($"[QuestButtonActivator] Quest {questID} step {step} → button {(shouldEnable ? "ENABLED" : "DISABLED")}");
    }
}
