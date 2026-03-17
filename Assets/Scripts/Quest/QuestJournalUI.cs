using UnityEngine;
using UnityEngine.SceneManagement;
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
    public GameObject      hudPanel; // Container của HUD nhỏ — ẩn khi không có quest

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

        // Đăng ký event để force-close journal khi scene mới load
        // (xử lý trường hợp object là DontDestroyOnLoad, Start() không chạy lại)
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Luôn đóng journal khi scene mới load — tránh bị mở tự động
        if (rootPanel && rootPanel.activeSelf)
        {
            rootPanel.SetActive(false);
            _isOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // Refresh text nhưng KHÔNG mở panel
        RefreshUI(0);
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
        _isOpen = !rootPanel.activeSelf; // luôn đọc từ panel để tránh lệch state
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

        // Hiện HUD nhỏ khi có quest
        if (hudPanel) hudPanel.SetActive(true);

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
        // Ẩn HUD nhỏ khi không có quest
        if (hudPanel) hudPanel.SetActive(false);

        if (titleText != null)       titleText.text       = emptyQuestTitle;
        if (descriptionText != null) descriptionText.text = emptyQuestDesc;
        if (instructionText != null) instructionText.text = emptyQuestInstruction;

        if (hudTitleText != null)    hudTitleText.text     = "";
        if (hudStepText != null)     hudStepText.text      = "";
    }
}
