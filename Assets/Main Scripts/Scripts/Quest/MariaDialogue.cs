using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// NPC Maria — handles dialogue for multiple quests:
///   Quest 2, Step 3: Maria at City Gate → briefs player to enter dungeon
///   Quest 3, Step 2: Maria at Battlefield → briefs player to enter Swamp Dungeon
///   Quest 4, Step 0: Maria at Battlefield → tells about Final Gate at mountain top
/// </summary>
public class MariaDialogue : MonoBehaviour
{
    [Header("── Quest Settings ──")]
    public int quest2ID = 2;
    public int quest3ID = 3;
    public int quest4ID = 4;

    [Header("── Prompt UI ──")]
    public GameObject promptPanel;

    [Header("── Dialogue Canvas ──")]
    public Canvas     dialogueCanvas;
    public GameObject dialoguePanel;
    public Image      npcPortrait;
    public Sprite     mariaSprite;

    [Header("── Text ──")]
    public TextMeshProUGUI npcNameTMP;
    public Text            npcNameLegacy;
    public TextMeshProUGUI dialogueBodyTMP;
    public Text            dialogueBodyLegacy;

    [Header("── Button ──")]
    public Button          nextButton;
    public TextMeshProUGUI nextButtonLabelTMP;
    public Text            nextButtonLabelLegacy;

    // ─── Dialogue Lines ───────────────────────────────────────────────────

    [Header("── Quest 2 Step 3 – City Gate ──")]
    [TextArea(2, 4)]
    public string[] quest2Lines = {
        "You made it! Leona said she'd send someone — I'm glad it's you.",
        "I'm Maria. I've been holding this position for two days now.",
        "That portal — the one right behind me — it appeared out of nowhere.",
        "At first it was just strange lights. Then the creatures started coming through.",
        "My unit has been fighting around the clock. We can stop what comes out, but we can't close it.",
        "Something on the other side is keeping it open. Someone has to go in and destroy it.",
        "My soldiers are exhausted. I can't send them in — they've given everything already.",
        "But you... you're fresh, and you're strong. I can feel it.",
        "Please. Go through that portal and put an end to this. We'll hold the line as long as we can."
    };

    [Header("── Quest 3 Step 2 – Battlefield ──")]
    [TextArea(2, 4)]
    public string[] quest3Lines = {
        "You made it out here! I'm relieved.",
        "I was pushing toward the Demon Gate — our real objective — but something is blocking the path.",
        "Another gate appeared overnight. Smaller than the Demon Gate, but crawling with creatures.",
        "It leads to a swamp dungeon. Dark, dangerous, and full of monsters.",
        "We can't advance until it's dealt with.",
        "I'd go in myself, but I need to hold this perimeter.",
        "Please — go through that gate and close it from the inside. Then we push forward together."
    };

    [Header("── Quest 4 Step 0 – Final Gate ──")]
    [TextArea(2, 4)]
    public string[] quest4Lines = {
        "You did it! The swamp dungeon is cleared. I knew I could count on you.",
        "While you were in there, my scouts finally returned with intel.",
        "The source of all this — the final gate — it's at the top of the mountain.",
        "That's where the biggest threat is. Everything we've faced so far has been coming from up there.",
        "I need to stay here and guard this position. These creatures don't stop coming.",
        "But you... you've proven yourself. You can handle what's up there.",
        "Go to the mountain top. Close that gate. End this once and for all.",
        "And hey — be careful up there. I'll be waiting for you when you come back."
    };

    [Header("── Reminder ──")]
    [TextArea(2, 4)]
    public string[] reminderLines = {
        "The portal is right there. Go in — we'll hold the line out here!"
    };

    [Header("── Quest 4 Reminder ──")]
    [TextArea(2, 4)]
    public string[] quest4ReminderLines = {
        "The final gate is at the mountain top. Hurry — I'll guard here!"
    };

    [Header("── Settings ──")]
    public bool   typewriterEffect = true;
    public float  typewriterSpeed  = 0.03f;
    public string playerTag        = "Player";

    // ─── Runtime ──────────────────────────────────────────────────────────
    enum DialogueMode { Quest2Step2, Quest3Step1, Quest4Step0, Reminder, Quest4Reminder, None }

    string[]     _activeLines;
    DialogueMode _mode;
    int          _lineIndex  = 0;
    bool         _isOpen     = false;
    bool         _isTyping   = false;
    bool         _playerNear = false;

    // ──────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (dialogueCanvas != null) { dialogueCanvas.overrideSorting = true; dialogueCanvas.sortingOrder = 200; }
        if (promptPanel)   promptPanel.SetActive(false);
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (npcPortrait && mariaSprite) npcPortrait.sprite = mariaSprite;
        SetText(npcNameTMP, npcNameLegacy, "Maria");
        // removed double-binding legacy code
    }

    void Update()
    {
        if (!_playerNear) return;
        if (_isOpen) { if (IsFPressed()) OnNextClicked(); return; }
        if (CanTalk() && IsFPressed()) OpenDialogue();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = true;
        if (CanTalk() && promptPanel) promptPanel.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = false;
        if (promptPanel) promptPanel.SetActive(false);
        if (_isOpen) CloseDialogue();
    }

    // ─── Logic ────────────────────────────────────────────────────────────

    bool CanTalk()
    {
        return PickMode() != DialogueMode.None;
    }

    DialogueMode PickMode()
    {
        if (QuestManager.Instance == null) return DialogueMode.None;

        // Quest 3 TRƯỚC ƯU TIÊN quest đang active
        var q3 = QuestManager.Instance.GetState(quest3ID);
        if (q3 == QuestManager.QuestState.Active)
        {
            int q3step = QuestManager.Instance.GetStepIndex(quest3ID);
// Added Teleport step offset
            if (q3step <= 2) return DialogueMode.Quest3Step1;
            if (q3step > 2)  return DialogueMode.Reminder;
        }

        // Quest 4 — CHỈ khi Q3 đã Completed
        if (q3 == QuestManager.QuestState.Completed)
        {
            var q4 = QuestManager.Instance.GetState(quest4ID);
            if (q4 == QuestManager.QuestState.Active || q4 == QuestManager.QuestState.Available)
            {
                int q4step = QuestManager.Instance.GetStepIndex(quest4ID);
                if (q4step == 0) return DialogueMode.Quest4Step0;
                if (q4step > 0)  return DialogueMode.Quest4Reminder;
            }
        }

        // Quest 2 — step 2: Maria at City Gate
        var q2 = QuestManager.Instance.GetState(quest2ID);
        if (q2 == QuestManager.QuestState.Active)
        {
            int q2step = QuestManager.Instance.GetStepIndex(quest2ID);
            if (q2step <= 2) return DialogueMode.Quest2Step2;
            if (q2step > 2)  return DialogueMode.Reminder;
        }

        return DialogueMode.None;
    }

    string[] GetLines(DialogueMode mode)
    {
        switch (mode)
        {
            case DialogueMode.Quest2Step2:   return quest2Lines;
            case DialogueMode.Quest3Step1:   return quest3Lines;
            case DialogueMode.Quest4Step0:   return quest4Lines;
            case DialogueMode.Quest4Reminder:return quest4ReminderLines;
            case DialogueMode.Reminder:      return reminderLines;
            default:                         return reminderLines;
        }
    }

    void OpenDialogue()
    {
        _mode = PickMode();
        _activeLines = GetLines(_mode);

        if (_activeLines == null || _activeLines.Length == 0) return;

        _isOpen    = true;
        _lineIndex = 0;

        if (promptPanel) promptPanel.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(true);
        if (dialoguePanel)  dialoguePanel.SetActive(true);

        SetText(npcNameTMP, npcNameLegacy, "Maria");
        // Cursor được xử lý bời CursorUIPriority.BeginUiOverlay()
        CursorUIPriority.BeginUiOverlay();
        DialogueNextButton.Register(OnNextClicked);
        ShowLine(0);
    }

    void ShowLine(int index)
    {
        index = Mathf.Clamp(index, 0, _activeLines.Length - 1);
        bool isLast = index == _activeLines.Length - 1;
        SetText(nextButtonLabelTMP, nextButtonLabelLegacy, isLast ? "Understood!" : "Continue →");
        if (typewriterEffect) StartCoroutine(TypeLine(_activeLines[index]));
        else SetText(dialogueBodyTMP, dialogueBodyLegacy, _activeLines[index]);
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
        if (!_isOpen || _activeLines == null) return;
        if (_isTyping)
        {
            StopAllCoroutines();
            _isTyping = false;
            SetText(dialogueBodyTMP, dialogueBodyLegacy, _activeLines[_lineIndex]);
            return;
        }
        _lineIndex++;
        if (_lineIndex >= _activeLines.Length) { CloseDialogue(); OnDialogueFinished(); return; }
        ShowLine(_lineIndex);
    }

    void OnDialogueFinished()
    {
        if (QuestManager.Instance == null) return;

        switch (_mode)
        {
            case DialogueMode.Quest2Step2:
                // Nếu quest đang kẹt ở step 1 hoặc 2, đẩy thẳng tới step 3 (Dungeon Gate)
                int currentQ2 = QuestManager.Instance.GetStepIndex(quest2ID);
                while (currentQ2 >= 0 && currentQ2 <= 2)
                {
                    QuestManager.Instance.AdvanceStep(quest2ID);
                    currentQ2++;
                }
                Debug.Log($"[MariaDialogue] Auto-forwarded Quest {quest2ID} up to step {currentQ2} -> Enter dungeon gate.");
                break;

            case DialogueMode.Quest3Step1:
                int currentQ3 = QuestManager.Instance.GetStepIndex(quest3ID);
                while (currentQ3 >= 0 && currentQ3 <= 2)
                {
                    QuestManager.Instance.AdvanceStep(quest3ID);
                    currentQ3++;
                }
                Debug.Log($"[MariaDialogue] Auto-forwarded Quest {quest3ID} up to step {currentQ3} -> Go to Swamp Dungeon.");
                break;

            case DialogueMode.Quest4Step0:
                // Quest 4 step 0 → 1: Accept + advance → go to mountain
                QuestManager.Instance.AcceptQuest(quest4ID);
                if (QuestManager.Instance.GetStepIndex(quest4ID) == 0)
                {
                    QuestManager.Instance.AdvanceStep(quest4ID);
                    Debug.Log("[MariaDialogue] Quest 4 step 0 → 1: Go to the mountain top.");
                }
                break;
        }
    }

    void CloseDialogue()
    {
        _isOpen = false;
        DialogueNextButton.Unregister();
        if (dialoguePanel)  dialoguePanel.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);
        // Cursor được xử lý bời CursorUIPriority.EndUiOverlay()
        CursorUIPriority.EndUiOverlay();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

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
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 3f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            "Maria – Q2/Q3/Q4");
    }
#endif
}
