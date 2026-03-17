using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// NPC Paladin – stationed in town after Quest 2 is complete.
/// Quest 3, Step 0: Tells player that Leona went to a new battlefield.
/// Advances Quest 3 step 0 → 1 after dialogue.
/// </summary>
public class PaladinDialogue : MonoBehaviour
{
    [Header("── Quest Settings ──")]
    public int questID   = 3;
    public int stepIndex = 0;

    [Header("── Prompt UI ──")]
    public GameObject promptPanel;

    [Header("── Dialogue Canvas ──")]
    public Canvas     dialogueCanvas;
    public GameObject dialoguePanel;
    public Image      npcPortrait;
    public Sprite     paladinSprite;

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

    [Header("── Dialogue Lines (Step 0) ──")]
    [TextArea(2, 4)]
    public string[] openingLines = {
        "Thank you for your help pushing back those monsters at the gate.",
        "You're asking about Maria? She's moved on to a more dangerous area.",
        "After we secured this position, new reports came in — another gate appeared. Larger. Darker.",
        "Maria didn't wait for orders. She took her unit and marched straight toward it.",
        "That's Maria for you. She never waits.",
        "Do you want to help her? Good. I knew you would.",
        "Use the teleport portal nearby and select the Battlefield destination.",
        "She's out there. She needs backup. Go now."
    };

    [Header("── Reminder (if player returns) ──")]
    [TextArea(2, 4)]
    public string[] reminderLines = {
        "The teleport portal is nearby. Select Battlefield — Maria is waiting for you!"
    };

    [Header("── Settings ──")]
    public bool   typewriterEffect = true;
    public float  typewriterSpeed  = 0.03f;
    public string playerTag        = "Player";

    // ─── Runtime ──────────────────────────────────────────────────────────
    string[] _activeLines;
    int      _lineIndex  = 0;
    bool     _isOpen     = false;
    bool     _isTyping   = false;
    bool     _playerNear = false;

    // ──────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (dialogueCanvas) { dialogueCanvas.overrideSorting = true; dialogueCanvas.sortingOrder = 200; }
        if (promptPanel)    promptPanel.SetActive(false);
        if (dialoguePanel)  dialoguePanel.SetActive(false);
        if (npcPortrait && paladinSprite) npcPortrait.sprite = paladinSprite;
        SetText(npcNameTMP, npcNameLegacy, "Paladin");
        if (nextButton) nextButton.onClick.AddListener(OnNextClicked);
    }

    void Update()
    {
        if (!_playerNear) return;
        if (_isOpen) { if (IsFPressed()) OnNextClicked(); return; }
        if (ShouldShowPrompt() && IsFPressed()) OpenDialogue();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = true;
        if (ShouldShowPrompt() && promptPanel) promptPanel.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = false;
        if (promptPanel) promptPanel.SetActive(false);
    }

    bool ShouldShowPrompt()
    {
        if (QuestManager.Instance == null) return false;
        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);
        return state == QuestManager.QuestState.Active && step <= stepIndex + 1;
    }

    void OpenDialogue()
    {
        int step = QuestManager.Instance != null ? QuestManager.Instance.GetStepIndex(questID) : stepIndex;
        _activeLines = (step == stepIndex) ? openingLines : reminderLines;
        _isOpen      = true;
        _lineIndex   = 0;

        if (promptPanel)   promptPanel.SetActive(false);
        if (dialogueCanvas) dialogueCanvas.gameObject.SetActive(true);
        if (dialoguePanel)  dialoguePanel.SetActive(true);

        // Re-set tên NPC khi mở, tránh bị script khác ghi đè
        SetText(npcNameTMP, npcNameLegacy, "Paladin");

        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
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
        if (_isTyping)
        {
            StopAllCoroutines(); _isTyping = false;
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
        // Accept Quest 3 + advance step 0 → 1
        QuestManager.Instance.AcceptQuest(questID);
        if (QuestManager.Instance.GetStepIndex(questID) == stepIndex)
        {
            QuestManager.Instance.AdvanceStep(questID);
            Debug.Log("[PaladinDialogue] Quest 3 step 0 → 1: find teleport to battlefield.");
        }
    }

    void CloseDialogue()
    {
        _isOpen = false;
        if (dialoguePanel)  dialoguePanel.SetActive(false);
        if (dialogueCanvas) dialogueCanvas.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
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
        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 3f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            "Paladin – Quest 3 Giver\n(Step 0)");
    }
#endif
}
