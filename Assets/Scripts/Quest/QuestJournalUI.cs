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

    [Header("Toggle Settings")]
    public KeyCode toggleKey = KeyCode.J;
    public GameObject rootPanel; // Object chính cần bật/tắt

    [Header("Settings")]
    [Tooltip("Nội dung hiển thị khi không có nhiệm vụ nào đang thực hiện")]
    public string emptyQuestTitle = "No Active Quest";
    public string emptyQuestDesc = "You have no core objectives right now. Explore the world or talk to NPCs.";
    public string emptyQuestInstruction = "---";

    private bool _isOpen = false;

    private void Start()
    {
        if (rootPanel == null) rootPanel = gameObject;
        rootPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleJournal();
        }
    }

    public void ToggleJournal()
    {
        _isOpen = !_isOpen;
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
        if (QuestManager.Instance == null) return;

        // Lấy dữ liệu nhiệm vụ đang Active
        QuestData activeQuest = QuestManager.Instance.GetActiveQuest();
        QuestStep activeStep = QuestManager.Instance.GetActiveStep();

        if (activeQuest != null)
        {
            // Hiển thị thông tin nhiệm vụ
            if (titleText) titleText.text = activeQuest.questTitle;
            if (descriptionText) descriptionText.text = activeQuest.questDescription;
            
            // Hiển thị hướng dẫn bước hiện tại
            if (instructionText)
            {
                if (activeStep != null)
                {
                    instructionText.text = $"<b>Objective:</b>\n{activeStep.instruction}";
                }
                else
                {
                    instructionText.text = "Goal reached! Check your rewards.";
                }
            }
        }
        else
        {
            // Hiển thị trạng thái trống
            if (titleText) titleText.text = emptyQuestTitle;
            if (descriptionText) descriptionText.text = emptyQuestDesc;
            if (instructionText) instructionText.text = emptyQuestInstruction;
        }

        Debug.Log("[QuestJournal] UI đã được cập nhật.");
    }
}
