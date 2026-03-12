using UnityEngine;
using TMPro;

/// <summary>
/// Hiển thị thông tin nhiệm vụ hiện tại lên giao diện Cuốn sổ (Book UI).
/// Kế thừa logic từ QuestManager để tự động cập nhật khi có thay đổi.
/// </summary>
public class QuestJournalUI : MonoBehaviour
{
    [Header("UI Text Components")]
    [Tooltip("Text hiển thị tên nhiệm vụ")]
    public TextMeshProUGUI titleText;
    
    [Tooltip("Text hiển thị mô tả tổng quan của nhiệm vụ")]
    public TextMeshProUGUI descriptionText;
    
    [Tooltip("Text hiển thị hướng dẫn bước hiện tại (Mục tiêu)")]
    public TextMeshProUGUI instructionText;

    [Header("HUD (Corner Screen)")]
    public TextMeshProUGUI hudTitleText;

    [Header("Toggle Settings")]
    public KeyCode toggleKey = KeyCode.J;
    public GameObject rootPanel; // Object chính cần bật/tắt

    [Header("Placeholder Strings")]
    public string emptyQuestTitle = "No Active Quest";
    public string emptyQuestDesc = "Visit NPC Leona or check the Tavern to find new adventures.";
    public string emptyQuestInstruction = "Explore the city and talk to NPCs.";

    private bool _isOpen = false;

    private void Start()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[QuestJournalUI] rootPanel is NOT assigned! Please drag the Journal Panel into the inspector slot.");
        }
        _isOpen = false;
    }

    private void Update()
    {
        if (IsTogglePressedThisFrame())
        {
            ToggleJournal();
        }
    }

    private bool IsTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.jKey.wasPressedThisFrame)
            return true;
#endif
        return Input.GetKeyDown(toggleKey);
    }

    public void ToggleJournal()
    {
        if (rootPanel == null) return;

        _isOpen = !rootPanel.activeSelf;
        rootPanel.SetActive(_isOpen);

        if (_isOpen)
        {
            RefreshUI(0);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnEnable()
    {
        // Đăng ký sự kiện để tự động cập nhật UI khi quest thay đổi
        QuestManager.OnQuestAccepted += RefreshUI;
        QuestManager.OnQuestStepAdvanced += RefreshUI;
        QuestManager.OnQuestCompleted += RefreshUI;
        
        // Cập nhật ngay khi mở bảng
        RefreshUI(0);
    }

    private void OnDisable()
    {
        QuestManager.OnQuestAccepted -= RefreshUI;
        QuestManager.OnQuestStepAdvanced -= RefreshUI;
        QuestManager.OnQuestCompleted -= RefreshUI;
    }

    /// <summary>
    /// Làm mới toàn bộ thông tin trên trang sách
    /// </summary>
    public void RefreshUI(int questID)
    {
        var manager = QuestManager.Instance;
        
        // Cố định lỗi NULL nếu manager chưa kịp Awake
        if (manager == null)
        {
            manager = FindFirstObjectByType<QuestManager>();
            if (manager == null)
            {
                Debug.LogWarning("[QuestJournal] QuestManager not found in scene! UI cannot be updated.");
                return;
            }
        }

        // Fetch the first Active quest
        QuestData activeQuest = manager.GetActiveQuest();
        QuestStep activeStep = manager.GetActiveStep();

        if (activeQuest != null)
        {
            Debug.Log($"[QuestJournal] Displaying Active Quest: {activeQuest.questTitle} (ID: {activeQuest.questID})");
            
            if (titleText) titleText.text = activeQuest.questTitle;
            if (descriptionText) descriptionText.text = activeQuest.questDescription;
            if (hudTitleText) hudTitleText.text = activeQuest.questTitle;
            
            if (instructionText)
            {
                if (activeStep != null)
                {
                    instructionText.text = $"<b>Objective:</b> {activeStep.instruction}";
                    Debug.Log($"[QuestJournal] Current Step: {activeStep.stepTitle}");
                }
                else
                {
                    instructionText.text = "Goal reached! Check your rewards.";
                }
            }
        }
        else
        {
            if (titleText) titleText.text = emptyQuestTitle;
            if (descriptionText) descriptionText.text = emptyQuestDesc;
            if (instructionText) instructionText.text = emptyQuestInstruction;
        }
    }
}
