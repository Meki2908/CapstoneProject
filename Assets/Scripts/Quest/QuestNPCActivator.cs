using UnityEngine;

/// <summary>
/// Activates or deactivates a list of GameObjects based on quest state.
/// Use this to show/hide NPCs when a quest starts, completes, etc.
/// 
/// Example: Hide Leona (town) and show Paladin when Quest 2 is Completed.
/// </summary>
public class QuestNPCActivator : MonoBehaviour
{
    [System.Serializable]
    public class ActivationRule
    {
        [Tooltip("Quest ID to watch")]
        public int questID = 2;

        [Tooltip("Required state to show objects below")]
        public QuestManager.QuestState showWhenState = QuestManager.QuestState.Completed;

        [Tooltip("Also show when step >= this value (-1 = ignore)")]
        public int showWhenStepMin = -1;

        [Header("Objects")]
        [Tooltip("Show these when condition is met")]
        public GameObject[] showObjects;
        [Tooltip("Hide these when condition is met")]
        public GameObject[] hideObjects;
    }

    public ActivationRule[] rules;

    void OnEnable()
    {
        QuestManager.OnQuestAccepted     += OnQuestChanged;
        QuestManager.OnQuestStepAdvanced += OnQuestChanged;
        QuestManager.OnQuestCompleted    += OnQuestChanged;
    }

    void OnDisable()
    {
        QuestManager.OnQuestAccepted     -= OnQuestChanged;
        QuestManager.OnQuestStepAdvanced -= OnQuestChanged;
        QuestManager.OnQuestCompleted    -= OnQuestChanged;
    }

    void Start()
    {
        // Delay 1 frame to ensure QuestManager is ready
        StartCoroutine(DelayedRefresh());
    }

    System.Collections.IEnumerator DelayedRefresh()
    {
        yield return null;
        Refresh();
    }

    void OnQuestChanged(int id) => Refresh();

    void Refresh()
    {
        if (QuestManager.Instance == null) return;

        foreach (var rule in rules)
        {
            var state = QuestManager.Instance.GetState(rule.questID);
            int step  = QuestManager.Instance.GetStepIndex(rule.questID);

            bool stateMatch = state == rule.showWhenState;
            bool stepMatch  = rule.showWhenStepMin < 0 || step >= rule.showWhenStepMin;
            bool active     = stateMatch && stepMatch;

            foreach (var go in rule.showObjects) if (go) go.SetActive(active);
            foreach (var go in rule.hideObjects) if (go) go.SetActive(!active);
        }
    }

#if UNITY_EDITOR
    void OnValidate() { if (Application.isPlaying) Refresh(); }
#endif
}
