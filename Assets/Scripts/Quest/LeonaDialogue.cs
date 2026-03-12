using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// NPC Leona – Hội thoại đa trạng thái theo quest:
///   Quest 1 Available  → Nhận nhiệm vụ tutorial, teleport vào Tutorial scene
///   Quest 1 Completed  → Nói chuyện ngắn, advance Quest 2 step 0 → dẫn đến cổng dungeon
///   Quest 2 Active+    → Chỉ nhắc nhở đường đến cổng
/// </summary>
public class LeonaDialogue : MonoBehaviour
{
    // ─── UI References ────────────────────────────────────────────────────

    [Header("── Prompt UI ──")]
    public GameObject promptPanel;

    [Header("── Dialogue Canvas ──")]
    public Canvas     dialogueCanvas;
    public GameObject dialoguePanel;
    public Image      npcPortrait;
    public Sprite     leonaSprite;

    [Header("── Text ──")]
    public TextMeshProUGUI npcNameTMP;
    public Text            npcNameLegacy;
    public TextMeshProUGUI dialogueBodyTMP;
    public Text            dialogueBodyLegacy;

    [Header("── Button ──")]
    public Button          nextButton;
    public TextMeshProUGUI nextButtonLabelTMP;
    public Text            nextButtonLabelLegacy;

    // ─── Hội thoại theo Quest State ───────────────────────────────────────

    [Header("── Dialogue: Quest 1 (Available → Accept) ──")]
    [TextArea(2, 4)]
    public string[] quest1AcceptLines = {
        "Oh, a new adventurer! Welcome to this land.",
        "I'm Leona — a guide for warriors who wish to challenge the Dungeon.",
        "Would you like me to guide you through the basics first?",
        "Great! Complete the training and then head to the Dungeon Gate to the North. Good luck!"
    };

    [Header("── Dialogue: Quest 2 (Chỉ đường đến cổng dungeon) ──")]
    [TextArea(2, 4)]
    public string[] quest2GuideLines = {
        "You've completed the training — well done!",
        "Now it's time for the real challenge.",
        "The Dungeon Gate is to the North. Follow the marker and step through when you're ready.",
        "I'll be cheering for you! Let's go!"
    };

    [Header("── Dialogue: Quest 2 đã Active (nhắc đường) ──")]
    [TextArea(2, 4)]
    public string[] quest2ReminderLines = {
        "The Dungeon Gate is to the North. Don't keep it waiting!"
    };

    // ─── Settings ─────────────────────────────────────────────────────────

    [Header("── Settings ──")]
    public bool  typewriterEffect = true;
    public float typewriterSpeed  = 0.03f;
    public string playerTag       = "Player";

    // ─── Runtime ─────────────────────────────────────────────────────────

    string[] _activeLines;
    int      _lineIndex  = 0;
    bool     _isOpen     = false;
    bool     _isTyping   = false;
    bool     _playerNear = false;

    enum DialogueMode { Quest1Accept, Quest2Guide, Quest2Reminder, None }
    DialogueMode _mode = DialogueMode.None;

    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (dialogueCanvas != null) { dialogueCanvas.overrideSorting = true; dialogueCanvas.sortingOrder = 200; }
        if (promptPanel != null)
        {
            var c = promptPanel.GetComponentInParent<Canvas>();
            if (c != null) { c.overrideSorting = true; c.sortingOrder = 199; }
        }

        if (promptPanel)   promptPanel.SetActive(false);
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (npcPortrait && leonaSprite) npcPortrait.sprite = leonaSprite;
        SetText(npcNameTMP, npcNameLegacy, "Leona");
        if (nextButton) nextButton.onClick.AddListener(OnNextClicked);

        // Debug
        Debug.Log($"[LeonaDialogue] Start – promptPanel={(promptPanel != null ? promptPanel.name : "NULL")}, dialoguePanel={(dialoguePanel != null ? "OK" : "NULL")}");
    }

    void Update()
    {
        if (!_playerNear) return;

        if (_isOpen)
        {
            if (IsFPressed()) 
            {
                Debug.Log("[LeonaDialogue] F pressed while dialogue open → OnNextClicked()");
                OnNextClicked();
            }
            return;
        }

        if (IsFPressed())
        {
            Debug.Log("[LeonaDialogue] F pressed → calling OpenDialogue()");
            OpenDialogue();
        }
    }

    // ─── Trigger ─────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[LeonaDialogue] OnTriggerEnter: {other.name} tag={other.tag}");
        if (!other.CompareTag(playerTag)) return;
        
        _playerNear = true;
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
            var parentCanvas = promptPanel.GetComponentInParent<Canvas>();
            if (parentCanvas != null && !parentCanvas.enabled)
            {
                Debug.LogWarning($"[LeonaDialogue] Cảnh báo: '{parentCanvas.name}' (Canvas cha của promptPanel) đang bị TẮT (enabled=false)! Giao diện sẽ không hiện.");
            }
            Debug.Log($"[LeonaDialogue] Đã hiện promptPanel ({promptPanel.name})");
        }
        else
        {
            Debug.LogWarning("[LeonaDialogue] promptPanel is NULL – Hãy kéo thả object gợi ý 'Nhấn F' vào Inspector!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = false;
        if (promptPanel) promptPanel.SetActive(false);
    }

    // ─── Dialogue Logic ───────────────────────────────────────────────────

    void OpenDialogue()
    {
        _mode = PickMode();
        _activeLines = GetLines(_mode);

        Debug.Log($"[LeonaDialogue] OpenDialogue → mode={_mode}, linesCount={(_activeLines?.Length ?? 0)}");

        if (_activeLines == null || _activeLines.Length == 0)
        {
            Debug.LogWarning($"[LeonaDialogue] KHÔNG CÓ HỘI THOẠI cho mode {_mode}! Kiểm tra lại mảng string trong Inspector hoặc logic QuestManager.");
            return;
        }

        _isOpen    = true;
        _lineIndex = 0;

        if (promptPanel)   promptPanel.SetActive(false);
        
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            if (dialogueCanvas != null) dialogueCanvas.enabled = true;
        }
        else
        {
            Debug.LogError("[LeonaDialogue] dialoguePanel is NULL! Không thể hiện hội thoại.");
            return;
        }

        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        ShowLine(0);
    }

    DialogueMode PickMode()
    {
        if (QuestManager.Instance == null)
        {
            Debug.Log("[LeonaDialogue] PickMode → QuestManager null → Quest1Accept");
            return DialogueMode.Quest1Accept;
        }

        var q1 = QuestManager.Instance.GetState(1);
        var q2 = QuestManager.Instance.GetState(2);
        Debug.Log($"[LeonaDialogue] PickMode → q1={q1}, q2={q2}");

        // Quest 1 chưa hoàn thành → Leona giới thiệu và gửi sang tutorial
        if (q1 == QuestManager.QuestState.Available || q1 == QuestManager.QuestState.Active)
            return DialogueMode.Quest1Accept;

        // Quest 1 đã xong - kiểm tra Quest 2
        if (q1 == QuestManager.QuestState.Completed)
        {
            // Quest 2 chưa được accept → thử accept và nhắc đường
            if (q2 != QuestManager.QuestState.Completed)
                return DialogueMode.Quest2Reminder;
        }

        return DialogueMode.None;
    }

    string[] GetLines(DialogueMode mode)
    {
        switch (mode)
        {
            case DialogueMode.Quest1Accept:   return quest1AcceptLines;
            case DialogueMode.Quest2Guide:    return quest2GuideLines;
            case DialogueMode.Quest2Reminder: return quest2ReminderLines;
            default: return null;
        }
    }

    void ShowLine(int index)
    {
        index = Mathf.Clamp(index, 0, _activeLines.Length - 1);
        bool isLast = index == _activeLines.Length - 1;

        string btnLabel = isLast ? GetLastButtonLabel() : "Continue →";
        SetText(nextButtonLabelTMP, nextButtonLabelLegacy, btnLabel);

        if (typewriterEffect) StartCoroutine(TypeLine(_activeLines[index]));
        else SetText(dialogueBodyTMP, dialogueBodyLegacy, _activeLines[index]);
    }

    string GetLastButtonLabel()
    {
        switch (_mode)
        {
            case DialogueMode.Quest1Accept: return "Accept ✓";
            case DialogueMode.Quest2Guide:  return "Let's go! →";
            default:                        return "OK";
        }
    }

    IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        SetText(dialogueBodyTMP, dialogueBodyLegacy, "");
        foreach (char c in line)
        {
            if (dialogueBodyTMP)    dialogueBodyTMP.text    += c;
            if (dialogueBodyLegacy) dialogueBodyLegacy.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        _isTyping = false;
    }

    public void OnNextClicked()
    {
        if (_isTyping)
        {
            StopAllCoroutines();
            _isTyping = false;
            SetText(dialogueBodyTMP, dialogueBodyLegacy, _activeLines[_lineIndex]);
            return;
        }

        _lineIndex++;

        if (_lineIndex >= _activeLines.Length)
        {
            CloseDialogue();
            OnDialogueFinished();
            return;
        }

        ShowLine(_lineIndex);
    }

    void OnDialogueFinished()
    {
        switch (_mode)
        {
            case DialogueMode.Quest1Accept:
                // Hoàn thành Quest 1 + nhận Quest 2 ngay lập tức
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.CompleteQuest(1);
                    QuestManager.Instance.AcceptQuest(2);
                    Debug.Log("[LeonaDialogue] Quest 1 completed, Quest 2 accepted. Teleporting to Tutorial...");
                }
                // Teleport sang Tutorial để học kỹ năng
                var teleporter = GetComponent<QuestSceneTeleporter>();
                if (teleporter != null) teleporter.TeleportToScene();
                else Debug.LogWarning("[LeonaDialogue] Không tìm thấy QuestSceneTeleporter!");
                break;

            case DialogueMode.Quest2Guide:
                // Nhắc đường đến dungeon (Quest 2 đã active rồi)
                break;

            case DialogueMode.Quest2Reminder:
                // Thử accept Quest 2 (phòng trường hợp còn Locked/Available)
                if (QuestManager.Instance != null)
                    QuestManager.Instance.AcceptQuest(2);
                break;
        }
    }

    void CloseDialogue()
    {
        _isOpen = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────

    bool IsFPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) return true;
#endif
        return Input.GetKeyDown(KeyCode.F);
    }

    void SetText(TextMeshProUGUI tmp, Text legacy, string value)
    {
        if (tmp)    tmp.text    = value;
        if (legacy) legacy.text = value;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 2.5f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "Leona – Interact Zone");
    }
#endif
}
