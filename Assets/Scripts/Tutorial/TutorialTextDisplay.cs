using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialTextDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text tutorialText;

    [Header("Quest Integration")]
    [Tooltip("Kéo GameObject chứa TutorialQuestFinisher vào đây")]
    public TutorialQuestFinisher questFinisher;

    [Header("Return Prompt UI")]
    [Tooltip("Panel hiện dòng 'Press F to return' sau Congratulations, ẩn lúc đầu")]
    public GameObject returnPromptPanel;

    // ─── Tutorial Steps ────────────────────────────────────────────────────
    // Thứ tự bước:
    // 0  – Press Space to jump
    // 1  – Press E to equip your weapon
    // 2  – Press right mouse button to roll
    // 3  – Press left mouse button to attack
    // 4  – Press E on the dummy to use Special Skill 1
    // 5  – Press R on the dummy to use Special Skill 2
    // 6  – Press T on the dummy to use Special Skill 3
    // 7  – Press Q to use your Ultimate Skill
    // 8  – Press E to sheathe your weapon
    // 9  – Press I to open your inventory
    // 10 – Choose a different weapon (press 1 or 2)
    // 11 – Congratulations! → hiện prompt 'Press F to return'

    private readonly string[] tutorialSteps =
    {
        "Press Space to jump",
        "Press Tab to equip your weapon",
        "Press Right Mouse Button to roll",
        "Press Left Mouse Button to attack",
        "Press E on the dummy to use Special Skill 1",
        "Press R on the dummy to use Special Skill 2",
        "Press T on the dummy to use Special Skill 3",
        "Press Q to use your Ultimate Skill",
        "Press Tab to sheathe your weapon",
        "Press I to open your inventory",
        "Choose a different weapon — press 1 or 2",
        "Congratulations! You have completed the tutorial!"
    };

    private int  _currentStep    = 0;
    private bool _completed      = false;
    private bool _waitingReturn  = false;   // Đang chờ F để thoát

    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (returnPromptPanel) returnPromptPanel.SetActive(false);
        ShowStep(0);
    }

    void Update()
    {
        // Khi đến màn hình Congratulations, chờ F để thoát
        if (_waitingReturn)
        {
            if (Input.GetKeyDown(KeyCode.F)) TriggerReturn();
            return;
        }

        if (_completed) return;

        switch (_currentStep)
        {
            // Bước 0: Space để nhảy
            case 0:
                if (Input.GetKeyDown(KeyCode.Space)) Advance();
                break;

            // Bước 1: Tab để rút vũ khí
            case 1:
                if (Input.GetKeyDown(KeyCode.Tab)) Advance();
                break;

            // Bước 2: Chuột phải để lăn
            case 2:
                if (Input.GetMouseButtonDown(1)) Advance();
                break;

            // Bước 3: Chuột trái để tấn công
            case 3:
                if (Input.GetMouseButtonDown(0)) Advance();
                break;

            // Bước 4: E - skill 1
            case 4:
                if (Input.GetKeyDown(KeyCode.E)) Advance();
                break;

            // Bước 5: R - skill 2
            case 5:
                if (Input.GetKeyDown(KeyCode.R)) Advance();
                break;

            // Bước 6: T - skill 3
            case 6:
                if (Input.GetKeyDown(KeyCode.T)) Advance();
                break;

            // Bước 7: Q - Ultimate
            case 7:
                if (Input.GetKeyDown(KeyCode.Q)) Advance();
                break;

            // Bước 8: Tab để cất vũ khí
            case 8:
                if (Input.GetKeyDown(KeyCode.Tab)) Advance();
                break;

            // Bước 9: I để mở inventory
            case 9:
                if (Input.GetKeyDown(KeyCode.I)) Advance();
                break;

            // Bước 10: Đợi event từ WeaponSwapper.OnWeaponSwapped
            // (Không detect phím – sử dụng chuột trong Inventory UI)
            // Bước 11: Congratulations – tự hoàn thành sau delay
            // (Không cần input, được xử lý trong Advance())
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    void Advance()
    {
        _currentStep++;

        if (_currentStep >= tutorialSteps.Length)
        {
            // Đã qua tất cả bước → hiện bước cuối và wrap up
            _currentStep = tutorialSteps.Length - 1;
        }

        ShowStep(_currentStep);

        // Đến bước "Congratulations" → hiện prompt và chờ F
        if (_currentStep == tutorialSteps.Length - 1)
        {
            _completed     = true;
            _waitingReturn = true;

            // Hiện dòng "Press F to return"
            if (returnPromptPanel) returnPromptPanel.SetActive(true);
        }
    }

    void ShowStep(int index)
    {
        if (tutorialText != null)
            tutorialText.text = tutorialSteps[index];
    }

    void TriggerReturn()
    {
        _waitingReturn = false;
        if (returnPromptPanel) returnPromptPanel.SetActive(false);

        if (questFinisher != null)
            questFinisher.FinishTutorial();
        else
            Debug.LogWarning("[TutorialTextDisplay] QuestFinisher chưa được gán!");
    }

    // ─── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ WeaponSwapper.OnWeaponSwapped event khi player đổi vũ khí bằng chuột.
    /// Chỉ hoạt động nếu đang ở bước 10 ("Choose a different weapon").
    /// </summary>
    public void OnWeaponChanged()
    {
        if (_currentStep == 10 && !_completed)
            Advance();
    }

    /// <summary>Bỏ qua bước hiện tại – có thể gọi từ button "Skip" trong UI</summary>
    public void ShowNextTutorialText() => Advance();
}