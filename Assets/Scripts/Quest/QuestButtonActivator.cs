using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enables/disables a UI Button based on quest state & step.
/// Works with existing ButtonStateController if present (calls SetEnable/SetDisable).
/// </summary>
public class QuestButtonActivator : MonoBehaviour
{
    [Header("── Quest Condition ──")]
    public int questID      = 3;
    public int enableAtStep = 1;

    // ─── Runtime ──────────────────────────────────────────────────────────
    Button                _btn;
    ButtonStateController _bsc;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _bsc = GetComponent<ButtonStateController>();
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

    void Start() => StartCoroutine(DelayedRefresh());

    System.Collections.IEnumerator DelayedRefresh()
    {
        // Wait until QuestManager is ready (up to 2 seconds)
        float waited = 0f;
        while (QuestManager.Instance == null && waited < 2f)
        {
            yield return null;
            waited += Time.deltaTime;
        }
        Refresh(0);
    }

    void Refresh(int id)
    {
        if (QuestManager.Instance == null) return;

        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);

        bool shouldEnable = (state == QuestManager.QuestState.Active && step >= enableAtStep)
                         || state == QuestManager.QuestState.Completed;

        Debug.Log($"[QuestButtonActivator] Quest {questID} step {step} → {(shouldEnable ? "ENABLE" : "DISABLE")} {gameObject.name}");

        // Use ButtonStateController if present, otherwise direct interactable
        if (_bsc != null)
        {
            if (shouldEnable) _bsc.SetEnable();
            else              _bsc.SetDisable();
        }
        else if (_btn != null)
        {
            _btn.interactable = shouldEnable;
        }
    }
}

