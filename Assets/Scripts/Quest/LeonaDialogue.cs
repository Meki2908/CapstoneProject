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
        "Ah, you've finally arrived! I am Leona — I look after newcomers in this city.",
        "I've been expecting someone like you. The city is in need of capable fighters.",
        "But first, you can't just rush into battle without proper training.",
        "Head to the training grounds nearby. Learn the basics — your life will depend on it.",
        "Oh, and one more thing... Do you remember Maria? Your childhood friend?",
        "She's been in the city guard for years now. She's been promoted — General Maria, they call her.",
        "She's stationed at the City Gate right now. Once you finish training, come back and I'll tell you more.",
        "Now go — the training area is right ahead. Good luck!"
    };

    [Header("── Dialogue: Quest 2 – Leona sends player to Maria (Step 0) ──")]
    [TextArea(2, 4)]
    public string[] quest2GuideLines = {
        "You've returned! I'm relieved — there's no time to rest though.",
        "I just received word from a warrior named Maria. She's stationed outside the City Gate.",
        "Apparently, a mysterious portal appeared near the dungeon entrance two days ago.",
        "Creatures have been pouring out ever since. Maria's unit is holding the line, but barely.",
        "She's asking for reinforcements — someone strong enough to go through that portal.",
        "There's a teleport point just nearby. It'll take you straight to the City Gate.",
        "Find Maria when you get there. She'll explain everything. Please hurry — they're counting on you!"
    };


    [Header("── Dialogue: Quest 2 – Leona at City Gate (Step 3) ──")]
    [TextArea(2, 4)]
    public string[] quest2CityGateLines = {
        "You... you're here! I'm so glad. Honestly, I wasn't sure you'd make it.",
        "Maria told me she'd send someone. I hoped it would be you.",
        "Do you see that gate behind me? That's the dungeon entrance.",
        "Creatures have been pouring out of there since yesterday. My guards can barely hold them back.",
        "The source of this has to be deeper inside. Someone needs to go in and stop it.",
        "I'd go myself, but I need to hold the line out here. That's why I need you.",
        "I know it's a lot to ask. But I trust you more than anyone.",
        "Please — go through that gate and deal with whatever is causing this. We're counting on you."
    };

    [TextArea(2, 4)]
    public string[] quest2ReminderLines = {
        "The Dungeon Gate is to the North. Don't keep it waiting!"
    };

    [Header("── Dialogue: Quest 3 – Leona at New Battlefield (Step 3) ──")]
    [TextArea(2, 4)]
    public string[] quest3BattlefieldLines = {
        "You tracked me down! I knew you would.",
        "I won't lie — I was hoping to handle this myself. Old habit.",
        "But look at that gate. It's bigger than the last one. Whatever is inside... it is not small.",
        "My scouts haven't come back. That tells me everything I need to know.",
        "I've been holding this position, but I can't push forward alone.",
        "With you here, we have a real chance.",
        "Go in. Clear the way. I'll be right behind you once the perimeter is secure.",
        "And hey — try not to get yourself killed in there. I'd have to feel bad about it."
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

    enum DialogueMode { Quest1Accept, Quest2Guide, Quest2CityGate, Quest2Reminder, Quest3Battlefield, Default, None }
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

        bool q2NotStarted = q2 == QuestManager.QuestState.Locked || q2 == QuestManager.QuestState.Available;
        bool q2AtStep0    = q2 == QuestManager.QuestState.Active
                            && QuestManager.Instance.GetStepIndex(2) == 0;

        // Quest 1 done + Quest 2 active → check which step
        if (q1 == QuestManager.QuestState.Completed && q2 == QuestManager.QuestState.Active)
        {
            int q2step = QuestManager.Instance.GetStepIndex(2);
            if (q2step == 3) return DialogueMode.Quest2CityGate;   // At city gate → full dialogue
            if (q2step > 3)  return DialogueMode.Quest2Reminder;   // Past it → reminder
            if (q2step > 0)  return DialogueMode.Quest2Reminder;   // Still en route → reminder
        }

        // Quest 1 done + Quest 2 not started or at step 0 → Quest2Guide (Leona in town)
        if (q1 == QuestManager.QuestState.Completed && (q2NotStarted || q2AtStep0))
            return DialogueMode.Quest2Guide;

        // Quest 2 active (fallback) → reminder
        if (q2 == QuestManager.QuestState.Active)
            return DialogueMode.Quest2Reminder;

        // Quest 2 completed + Quest 3 active
        var q3 = QuestManager.Instance.GetState(3);
        if (q2 == QuestManager.QuestState.Completed && q3 == QuestManager.QuestState.Active)
        {
            int q3step = QuestManager.Instance.GetStepIndex(3);
            if (q3step == 3) return DialogueMode.Quest3Battlefield;
            if (q3step > 3)  return DialogueMode.Default;   // past it, done
        }

        return DialogueMode.Default;
    }

    string[] GetLines(DialogueMode mode)
    {
        switch (mode)
        {
            case DialogueMode.Quest1Accept:      return quest1AcceptLines;
            case DialogueMode.Quest2Guide:       return quest2GuideLines;
            case DialogueMode.Quest2CityGate:    return quest2CityGateLines;
            case DialogueMode.Quest2Reminder:    return quest2ReminderLines;
            case DialogueMode.Quest3Battlefield: return quest3BattlefieldLines;
            case DialogueMode.Default:           return defaultLines;
            default:                             return defaultLines;
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
                // Leona in town: Accept Quest 2 + advance step 0 → 1 (heading to teleport)
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.AcceptQuest(2);
                    if (QuestManager.Instance.GetStepIndex(2) == 0)
                    {
                        QuestManager.Instance.AdvanceStep(2);
                        Debug.Log("[LeonaDialogue] Quest 2 step 0 → 1: heading to teleport.");
                    }
                }
                break;

            case DialogueMode.Quest2CityGate:
                // Leona at city gate: advance step 3 → 4 (Enter dungeon gate)
                if (QuestManager.Instance != null)
                {
                    int step = QuestManager.Instance.GetStepIndex(2);
                    if (step == 3)
                    {
                        QuestManager.Instance.AdvanceStep(2);
                        Debug.Log("[LeonaDialogue] Quest 2 step 3 → 4: Enter the Dungeon Gate.");
                    }
                }
                break;

            case DialogueMode.Quest2Reminder:
                break;

            case DialogueMode.Quest3Battlefield:
                // Leona at new battlefield: advance step 3 → 4 (Enter dungeon gate 2)
                if (QuestManager.Instance != null)
                {
                    int step3 = QuestManager.Instance.GetStepIndex(3);
                    if (step3 == 3)
                    {
                        QuestManager.Instance.AdvanceStep(3);
                        Debug.Log("[LeonaDialogue] Quest 3 step 3 → 4: Enter dungeon gate 2.");
                    }
                }
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
