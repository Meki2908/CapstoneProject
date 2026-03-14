using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// NPC General Maria – stationed at the City Gate.
/// Quest 2, Step 3: Player talks to Maria → step advances to 4 (Enter Dungeon Gate).
/// </summary>
public class MariaDialogue : MonoBehaviour
{
    // ─── Quest Settings ───────────────────────────────────────────────────
    [Header("── Quest Settings ──")]
    public int questID   = 2;
    public int stepIndex = 3;   // This NPC handles step 3

    // ─── UI References ────────────────────────────────────────────────────
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
    [Header("── Dialogue Lines ──")]
    [TextArea(2, 4)]
    public string[] dialogueLines = {
        "Halt! ...Oh. You must be the one Leona sent. I am General Maria.",
        "The situation is dire. The dungeon gates have been breached — dark creatures pour out without end.",
        "My soldiers have held the line, but we are stretched thin. We need every blade we can get.",
        "Beyond that gate lies the source of this chaos. Someone has to go in and cut it off.",
        "Are you willing to face what lurks inside? ...I can see it in your eyes. You are.",
        "Then go, adventurer. Push through the dungeon gate. We are counting on you.",
        "For the city. For everyone behind these walls. Good luck — you will need it."
    };

    [TextArea(2, 4)]
    public string[] reminderLines = {
        "The dungeon gate is right ahead. Don't hesitate — we are counting on you!"
    };

    // ─── Settings ─────────────────────────────────────────────────────────
    [Header("── Settings ──")]
    public bool  typewriterEffect = true;
    public float typewriterSpeed  = 0.03f;
    public string playerTag       = "Player";

    // ─── Runtime ──────────────────────────────────────────────────────────
    string[] _activeLines;
    int      _lineIndex  = 0;
    bool     _isOpen     = false;
    bool     _isTyping   = false;
    bool     _playerNear = false;

    // ──────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (dialogueCanvas != null) { dialogueCanvas.overrideSorting = true; dialogueCanvas.sortingOrder = 200; }
        if (promptPanel)   promptPanel.SetActive(false);
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (npcPortrait && mariaSprite) npcPortrait.sprite = mariaSprite;
        SetText(npcNameTMP, npcNameLegacy, "General Maria");
        if (nextButton) nextButton.onClick.AddListener(OnNextClicked);
    }

    void Update()
    {
        if (!_playerNear) return;

        if (_isOpen)
        {
            if (IsFPressed()) OnNextClicked();
            return;
        }

        // Only allow interaction at the correct quest step
        if (!IsCorrectStep()) return;

        if (IsFPressed()) OpenDialogue();
    }

    // ─── Trigger ──────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = true;
        if (IsCorrectStep() && promptPanel != null)
            promptPanel.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = false;
        if (promptPanel) promptPanel.SetActive(false);
    }

    // ─── Dialogue Logic ───────────────────────────────────────────────────

    bool IsCorrectStep()
    {
        if (QuestManager.Instance == null) return false;
        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);

        if (state != QuestManager.QuestState.Active) return false;
        // Show reminder if past this step (step > stepIndex)
        return step >= stepIndex;
    }

    void OpenDialogue()
    {
        int currentStep = QuestManager.Instance != null
            ? QuestManager.Instance.GetStepIndex(questID) : stepIndex;

        // First time (step == stepIndex) → full dialogue; afterwards → reminder
        _activeLines = (currentStep == stepIndex) ? dialogueLines : reminderLines;
        _isOpen      = true;
        _lineIndex   = 0;

        if (promptPanel)   promptPanel.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(true);
        if (dialoguePanel) dialoguePanel.SetActive(true);

        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        SetText(nextButtonLabelTMP, nextButtonLabelLegacy, "Continue →");
        ShowLine(0);
    }

    void ShowLine(int index)
    {
        index = Mathf.Clamp(index, 0, _activeLines.Length - 1);
        bool isLast = index == _activeLines.Length - 1;
        SetText(nextButtonLabelTMP, nextButtonLabelLegacy, isLast ? "Understood →" : "Continue →");

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
        // Advance only on first full dialogue (step == stepIndex)
        if (QuestManager.Instance == null) return;
        int step = QuestManager.Instance.GetStepIndex(questID);
        if (step == stepIndex)
        {
            QuestManager.Instance.AdvanceStep(questID);   // step 3 → 4
            Debug.Log("[MariaDialogue] Quest 2 advanced to step 4: Enter the Dungeon Gate.");
        }
    }

    void CloseDialogue()
    {
        _isOpen = false;
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

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
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 3f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "Maria – Interact Zone");
    }
#endif
}
