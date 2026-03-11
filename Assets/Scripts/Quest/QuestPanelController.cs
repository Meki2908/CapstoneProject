using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel đầy đủ hiển thị danh sách tất cả quest.
/// Mở/đóng bằng phím J (hoặc gọi Toggle() từ button UI).
/// 
/// Setup:
///   1. Tạo Canvas → Panel "QuestPanel" (ẩn mặc định)
///   2. Bên trong có: titleText, một ScrollView với Content để chứa các slot
///   3. Gắn script này lên QuestPanel, gán các reference
///   4. Tạo Prefab "QuestSlotUI" (xem QuestSlotUI.cs)
/// </summary>
public class QuestPanelController : MonoBehaviour
{
    [Header("Phím mở/đóng panel")]
    public KeyCode toggleKey = KeyCode.J;

    [Header("UI")]
    public GameObject panelRoot;        // Root panel (bật/tắt)
    public Transform  slotContainer;    // Nơi chứa các slot (Content của ScrollView)
    public GameObject questSlotPrefab;  // Prefab có QuestSlotUI component

    [Header("Header text (tuỳ chọn)")]
    public TextMeshProUGUI headerTMP;
    public Text             headerLegacy;

    List<QuestSlotUI> _slots = new List<QuestSlotUI>();
    bool _isOpen = false;

    // ─────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        QuestManager.OnQuestAccepted     += RefreshUI;
        QuestManager.OnQuestStepAdvanced += RefreshUI;
        QuestManager.OnQuestCompleted    += RefreshUI;
    }

    void OnDisable()
    {
        QuestManager.OnQuestAccepted     -= RefreshUI;
        QuestManager.OnQuestStepAdvanced -= RefreshUI;
        QuestManager.OnQuestCompleted    -= RefreshUI;
    }

    void Start()
    {
        if (panelRoot) panelRoot.SetActive(false);
        if (headerTMP)    headerTMP.text    = "Nhật ký nhiệm vụ";
        if (headerLegacy) headerLegacy.text = "Nhật ký nhiệm vụ";
        BuildSlots();
        RefreshUI(-1);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    // ─── Public ──────────────────────────────────────────────────────────

    public void Toggle()
    {
        _isOpen = !_isOpen;
        if (panelRoot) panelRoot.SetActive(_isOpen);
        if (_isOpen) RefreshUI(-1);
    }

    public void Close()
    {
        _isOpen = false;
        if (panelRoot) panelRoot.SetActive(false);
    }

    // ─── Private ─────────────────────────────────────────────────────────

    void BuildSlots()
    {
        if (QuestManager.Instance == null || questSlotPrefab == null || slotContainer == null) return;

        // Xóa slot cũ
        foreach (var s in _slots) if (s) Destroy(s.gameObject);
        _slots.Clear();

        foreach (var qd in QuestManager.Instance.quests)
        {
            var go   = Instantiate(questSlotPrefab, slotContainer);
            var slot = go.GetComponent<QuestSlotUI>();
            if (slot == null) slot = go.AddComponent<QuestSlotUI>();
            slot.Bind(qd);
            _slots.Add(slot);
        }
    }

    void RefreshUI(int _)
    {
        if (QuestManager.Instance == null) return;
        foreach (var slot in _slots) slot?.Refresh();
    }
}
