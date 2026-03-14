using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        "Oh, a new adventurer! Welcome. I am Leona.",
        "I will help you start your journey in this world.",
        "First, let's learn the basics of combat in the training area.",
        "Look for me near the large red tree in the city if you need further guidance! Good luck!"
    };

    [Header("── Dialogue: Quest 2 (City Gate) ──")]
    [TextArea(2, 4)]
    public string[] quest2GuideLines = {
        "You've returned! The training grounds have clearly done you good.",
        "But there is no time to rest — a crisis is unfolding at the City Gate.",
        "General Maria is stationed there, holding the line against creatures spilling out of the dungeon.",
        "She needs every able fighter she can get. I am asking you to go and support her.",
        "There is a teleport point just nearby — it will take you straight to the City Gate.",
        "Find Maria when you arrive. She will brief you on the situation. Go now, and be careful!"
    };


    [Header("── Dialogue: Quest 2 đã Active (nhắc đường) ──")]
    [TextArea(2, 4)]
    public string[] quest2ReminderLines = {
        "The Dungeon Gate is to the North. Don't keep it waiting!"
    };

    [Header("── Dialogue: Mặc định (Khi lỗi hoặc xong hết) ──")]
    [TextArea(2, 4)]
    public string[] defaultLines = {
        "Stay safe out there, adventurer!",
        "The world is full of mysteries yet to be found."
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

    enum DialogueMode { Quest1Accept, Quest2Guide, Quest2Reminder, Default, None }
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

        // Both Locked → force Quest 1 to start
        if (q1 == QuestManager.QuestState.Locked && q2 == QuestManager.QuestState.Locked)
            return DialogueMode.Quest1Accept;

        // Quest 1 not done → give/remind Quest 1
        if (q1 == QuestManager.QuestState.Available || q1 == QuestManager.QuestState.Active)
            return DialogueMode.Quest1Accept;

        // Quest 1 done + Quest 2 not yet active → give Quest 2 briefing
        if (q1 == QuestManager.QuestState.Completed &&
            (q2 == QuestManager.QuestState.Locked || q2 == QuestManager.QuestState.Available))
            return DialogueMode.Quest2Guide;

        // Quest 2 already active → reminder
        if (q2 == QuestManager.QuestState.Active)
            return DialogueMode.Quest2Reminder;

        return DialogueMode.Default;
    }

    string[] GetLines(DialogueMode mode)
    {
        switch (mode)
        {
            case DialogueMode.Quest1Accept:   return quest1AcceptLines;
            case DialogueMode.Quest2Guide:    return quest2GuideLines;
            case DialogueMode.Quest2Reminder: return quest2ReminderLines;
            case DialogueMode.Default:        return defaultLines;
            default:                          return defaultLines; // Luôn trả về fallback thay vì null
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
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.AdvanceStep(1);
                    if (QuestManager.Instance.GetState(1) == QuestManager.QuestState.Completed)
                        QuestManager.Instance.AcceptQuest(2);
                    Debug.Log("[LeonaDialogue] Quest 1 advanced.");
                }
                var teleporter = GetComponent<QuestSceneTeleporter>();
                if (teleporter != null) teleporter.TeleportToScene();
                else Debug.LogWarning("[LeonaDialogue] QuestSceneTeleporter not found!");
                break;

            case DialogueMode.Quest2Guide:
                // Accept Quest 2 + advance step 0 → 1 (Talk to Leona done)
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.AcceptQuest(2);
                    if (QuestManager.Instance.GetStepIndex(2) == 0)
                        QuestManager.Instance.AdvanceStep(2);   // step 0 → 1 (heading to teleport)
                    Debug.Log("[LeonaDialogue] Quest 2 accepted, step 0 → 1.");
                }
                break;

            case DialogueMode.Quest2Reminder:
                // Already active – just remind, no state change
                break;
        }
    }

    void CloseDialogue()
    {
        _isOpen = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        // Xoá focus khỏi UI để lần trigger tiếp theo không bị chặn
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
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
