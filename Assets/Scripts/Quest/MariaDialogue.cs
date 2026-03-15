using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// NPC Maria – warrior stationed outside the City Gate, near the newly appeared portal.
/// Quest 2, Step 3: Player talks to Maria → she briefs to enter the dungeon → step 3→4.
/// </summary>
public class MariaDialogue : MonoBehaviour
{
    [Header("── Quest Settings ──")]
    public int questID   = 2;
    public int stepIndex = 3;   // Maria handles step 3

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

    [Header("── Dialogue (Step 3 – First meeting at City Gate) ──")]
    [TextArea(2, 4)]
    public string[] openingLines = {
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

    [Header("── Reminder Dialogue (if player returns) ──")]
    [TextArea(2, 4)]
    public string[] reminderLines = {
        "The portal is right there. Go in — we'll hold the line out here!"
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
        if (dialogueCanvas != null) { dialogueCanvas.overrideSorting = true; dialogueCanvas.sortingOrder = 200; }
        if (promptPanel)   promptPanel.SetActive(false);
        if (dialoguePanel) dialoguePanel.SetActive(false);
        if (npcPortrait && mariaSprite) npcPortrait.sprite = mariaSprite;
        SetText(npcNameTMP, npcNameLegacy, "Maria");
        if (nextButton) nextButton.onClick.AddListener(OnNextClicked);
    }

    void Update()
    {
        if (!_playerNear) return;
        if (_isOpen) { if (IsFPressed()) OnNextClicked(); return; }
        if (IsCorrectStep() && IsFPressed()) OpenDialogue();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        _playerNear = true;
        if (IsCorrectStep() && promptPanel) promptPanel.SetActive(true);
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
        return state == QuestManager.QuestState.Active && step >= stepIndex;
    }

    void OpenDialogue()
    {
        int step = QuestManager.Instance != null
            ? QuestManager.Instance.GetStepIndex(questID) : stepIndex;

        _activeLines = (step == stepIndex) ? openingLines : reminderLines;
        _isOpen      = true;
        _lineIndex   = 0;

        if (promptPanel) promptPanel.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(true);
        if (dialoguePanel)  dialoguePanel.SetActive(true);

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
        int step = QuestManager.Instance.GetStepIndex(questID);
        if (step == stepIndex)
        {
            QuestManager.Instance.AdvanceStep(questID);   // step 3 → 4: Enter dungeon gate
            Debug.Log("[MariaDialogue] Quest 2 step 3 → 4: Enter the Dungeon Gate.");
        }
    }

    void CloseDialogue()
    {
        _isOpen = false;
        if (dialoguePanel)  dialoguePanel.SetActive(false);
        if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
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
            "Maria – Quest 2 Step 3\n(City Gate / Portal)");
    }
#endif
}
