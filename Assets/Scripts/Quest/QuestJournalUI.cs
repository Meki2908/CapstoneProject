using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// Quest Journal UI – hiển thị quest đang active và danh sách steps kèm trạng thái.
/// Nhấn J để mở/đóng.
/// </summary>
public class QuestJournalUI : MonoBehaviour
{
    [Header("── Journal Panel ──")]
    public GameObject rootPanel;
    public KeyCode toggleKey = KeyCode.J;

    [Header("── Text Fields ──")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI descriptionText;

    [Header("── HUD (góc màn hình) ──")]
    public TextMeshProUGUI hudTitleText;
    public TextMeshProUGUI hudStepText;

    [Header("── Empty State ──")]
    public string emptyQuestTitle       = "No Active Quest";
    public string emptyQuestDesc        = "Visit NPC Leona or check the Tavern to find new adventures.";
    public string emptyQuestInstruction = "Explore the city and talk to NPCs.";

    bool _isOpen = false;

    // ──────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (rootPanel) rootPanel.SetActive(false);
        else Debug.LogError("[QuestJournalUI] rootPanel is NOT assigned!");
    }

    void Update()
    {
        if (IsTogglePressedThisFrame()) ToggleJournal();
    }

    bool IsTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame) return true;
#endif
        return Input.GetKeyDown(toggleKey);
    }

    void OnEnable()
    {
        QuestManager.OnQuestAccepted     += RefreshUI;
        QuestManager.OnQuestStepAdvanced += RefreshUI;
        QuestManager.OnQuestCompleted    += RefreshUI;
        RefreshUI(0);
    }

    void OnDisable()
    {
        QuestManager.OnQuestAccepted     -= RefreshUI;
        QuestManager.OnQuestStepAdvanced -= RefreshUI;
        QuestManager.OnQuestCompleted    -= RefreshUI;
    }

    public void ToggleJournal()
    {
        if (rootPanel == null) return;
        _isOpen = !rootPanel.activeSelf;
        rootPanel.SetActive(_isOpen);
        Cursor.visible   = _isOpen;
        Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        if (_isOpen) RefreshUI(0);
    }

    // ── Core ──────────────────────────────────────────────────────────────

    public void RefreshUI(int questID)
    {
        var mgr = QuestManager.Instance ?? FindFirstObjectByType<QuestManager>();
        if (mgr == null) { Debug.LogWarning("[QuestJournal] QuestManager not found!"); return; }

        QuestData quest = mgr.GetActiveQuest();
        QuestStep step  = mgr.GetActiveStep();

        if (quest == null) { SetEmpty(); return; }

        Debug.Log($"[QuestJournal] Displaying Active Quest: {quest.questTitle} (ID: {quest.questID})");

        // 1. Quest title
        if (titleText != null)    titleText.text    = quest.questTitle;
        if (hudTitleText != null) hudTitleText.text = quest.questTitle;

        // 2. Current step activity (stepTitle)
        string activity = step != null ? step.stepTitle : "Quest Complete!";
        // Debug.Log($"[QuestJournal] Current Step: {activity}");
        if (instructionText != null) instructionText.text = activity;
        if (hudStepText != null)     hudStepText.text     = $"► {activity}";

        // 3. Quest description
        if (descriptionText) descriptionText.text = quest.questDescription;


    }

    void SetEmpty()
    {
        if (titleText != null)       titleText.text       = emptyQuestTitle;
        if (descriptionText != null) descriptionText.text = emptyQuestDesc;
        if (instructionText != null) instructionText.text = emptyQuestInstruction;

        if (hudTitleText != null)    hudTitleText.text     = "";
        if (hudStepText != null)     hudStepText.text      = "";
    }
}
