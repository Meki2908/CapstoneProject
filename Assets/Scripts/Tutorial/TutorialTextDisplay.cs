using UnityEngine;
using TMPro;
using System.Collections;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Tutorial step sequence:
/// 0  Space  - Jump
/// 1  Tab    - Equip weapon
/// 2  RMB    - Roll
/// 3  LMB    - Attack
/// 4  E      - Skill 1
/// 5  R      - Skill 2
/// 6  [Spawn Enemy 1] Kill the enemy
/// 7  T      - Skill 3  (lv30 set)
/// 8  [Spawn Enemy 2] Kill the enemy
/// 9  Q      - Ultimate (lv60 set)
/// -- 5s pause --
/// 10 [Spawn 10 enemies] Defeat all enemies (kill counter shown)
/// 11 Tab    - Sheathe
/// 12 I      - Open inventory
/// 13 Change weapon  (OnWeaponChanged event)
/// 14 I      - Close inventory
/// 15 [Complete] show completion canvas → TutorialQuestFinisher
/// </summary>
public class TutorialTextDisplay : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text tutorialText;
    public GameObject completionCanvas;
    public TMP_Text  completionMessageText;

    [Header("Quest")]
    public TutorialQuestFinisher questFinisher;

    [Header("Timing")]
    public float textDelay    = 1f;    // 1s between steps
    public float postUltDelay = 5f;    // Pause after Q before wave spawns

    [Header("Enemy Spawning")]
    public GameObject  enemyPrefab1;
    public Transform   spawnPoint1;
    public GameObject  enemyPrefab2;
    public Transform   spawnPoint2;
    public GameObject  wavePrefab;
    public Transform[] waveSpawnPoints;

    // ─────────────────────────────────────────────────────────────────────────
    //  Step Constants
    // ─────────────────────────────────────────────────────────────────────────
    const int STEP_SPACE      = 0;
    const int STEP_TAB_DRAW   = 1;
    const int STEP_ROLL       = 2;
    const int STEP_ATTACK     = 3;
    const int STEP_SKILL_E    = 4;
    const int STEP_SKILL_R    = 5;
    const int STEP_KILL1      = 6;
    const int STEP_SKILL_T    = 7;
    const int STEP_KILL2      = 8;
    const int STEP_ULT        = 9;
    const int STEP_WAVE       = 10;   // After 5s pause
    const int STEP_TAB_SHEATH = 11;
    const int STEP_OPEN_INV   = 12;
    const int STEP_CHANGE_WP  = 13;
    const int STEP_CLOSE_INV  = 14;
    const int WAVE_COUNT      = 10;

    readonly string[] _texts =
    {
        /* 0  */ "Press Space to jump",
        /* 1  */ "Press Tab to equip your weapon",
        /* 2  */ "Press Right Mouse Button to roll",
        /* 3  */ "Press Left Mouse Button to attack",
        /* 4  */ "Press E to use Skill 1",
        /* 5  */ "Press R to use Skill 2",
        /* 6  */ "Kill the enemy!",
        /* 7  */ "Press T to use Skill 3",
        /* 8  */ "Kill the enemy!",
        /* 9  */ "Press Q to use your Ultimate",
        /* 10 */ "Defeat all 10 enemies!",
        /* 11 */ "Press Tab to sheathe your weapon",
        /* 12 */ "Press I to open your inventory",
        /* 13 */ "Change your weapon",
        /* 14 */ "Press I to close your inventory",
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────
    int  _step      = 0;
    bool _waiting   = false;
    bool _completed = false;
    int  _waveKills = 0;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (completionCanvas != null) completionCanvas.SetActive(false);
        SetAllMasteryLevels(1);
        StartCoroutine(ShowDelayed(0));
    }

    void Update()
    {
        if (_completed || _waiting) return;

        switch (_step)
        {
            case STEP_SPACE:      if (IsKeyDown(KeyCode.Space, "space")) Advance(); break;
            case STEP_TAB_DRAW:   if (IsKeyDown(KeyCode.Tab,   "tab"))   Advance(); break;
            case STEP_ROLL:       if (IsMouseDown(1))                    Advance(); break;
            case STEP_ATTACK:     if (IsMouseDown(0))                    Advance(); break;
            case STEP_SKILL_E:    if (IsKeyDown(KeyCode.E, "e"))         Advance(); break;
            case STEP_SKILL_R:    if (IsKeyDown(KeyCode.R, "r"))         Advance(); break;
            case STEP_SKILL_T:    if (IsKeyDown(KeyCode.T, "t"))         Advance(); break;
            case STEP_ULT:        if (IsKeyDown(KeyCode.Q, "q"))         Advance(); break;
            case STEP_TAB_SHEATH: if (IsKeyDown(KeyCode.Tab, "tab"))     Advance(); break;
            case STEP_OPEN_INV:   if (IsKeyDown(KeyCode.I, "i"))         Advance(); break;
            case STEP_CLOSE_INV:  if (IsKeyDown(KeyCode.I, "i"))         Advance(); break;
            // STEP_KILL1, STEP_KILL2, STEP_CHANGE_WP, STEP_WAVE → event-driven
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Input Helpers – support both old & new Input System
    // ─────────────────────────────────────────────────────────────────────────

    bool IsKeyDown(KeyCode legacy, string newKey)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            var key = kb.FindKeyOnCurrentKeyboardLayout(newKey);
            if (key != null && key.wasPressedThisFrame) return true;
        }
#endif
        return Input.GetKeyDown(legacy);
    }

    bool IsMouseDown(int button)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (button == 0 && mouse.leftButton.wasPressedThisFrame)  return true;
            if (button == 1 && mouse.rightButton.wasPressedThisFrame) return true;
        }
#endif
        return Input.GetMouseButtonDown(button);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Step Progression
    // ─────────────────────────────────────────────────────────────────────────

    void Advance()
    {
        _step++;
        OnEnterStep(_step);

        if (_step >= _texts.Length) return;

        bool instant = (_step == STEP_KILL1 || _step == STEP_KILL2 || _step == STEP_WAVE);
        if (instant) ShowNow(_step);
        else         StartCoroutine(ShowDelayed(_step));
    }

    void OnEnterStep(int step)
    {
        switch (step)
        {
            case STEP_KILL1:      SpawnEnemy1();              break;
            case STEP_SKILL_T:    SetAllMasteryLevels(30);    break;
            case STEP_KILL2:      SpawnEnemy2();              break;
            case STEP_ULT:        SetAllMasteryLevels(60);    break;
            case STEP_WAVE:       StartCoroutine(DelayedWaveSpawn()); break;
        }
    }

    IEnumerator DelayedWaveSpawn()
    {
        _waiting = true;
        if (tutorialText != null) tutorialText.text = "Excellent! Prepare yourself...";
        yield return new WaitForSeconds(postUltDelay);
        SpawnWave();
        ShowNow(STEP_WAVE);
        _waiting = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Display
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator ShowDelayed(int step)
    {
        _waiting = true;
        if (tutorialText != null) tutorialText.text = "";
        yield return new WaitForSeconds(textDelay);
        if (tutorialText != null && step < _texts.Length)
            tutorialText.text = _texts[step];
        _waiting = false;
    }

    void ShowNow(int step)
    {
        if (tutorialText != null && step < _texts.Length)
            tutorialText.text = _texts[step];
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Enemy Spawning
    // ─────────────────────────────────────────────────────────────────────────

    void SpawnEnemy1()
    {
        if (enemyPrefab1 == null || spawnPoint1 == null) { Advance(); return; }
        var go = Instantiate(enemyPrefab1, spawnPoint1.position, spawnPoint1.rotation);
        if (!HookDeath(go, OnEnemy1Died)) Advance();
    }

    void SpawnEnemy2()
    {
        if (enemyPrefab2 == null || spawnPoint2 == null) { Advance(); return; }
        var go = Instantiate(enemyPrefab2, spawnPoint2.position, spawnPoint2.rotation);
        if (!HookDeath(go, OnEnemy2Died)) Advance();
    }

    void SpawnWave()
    {
        if (wavePrefab == null || waveSpawnPoints == null || waveSpawnPoints.Length == 0)
        { TriggerCompletion(); return; }

        _waveKills = 0;
        int count = Mathf.Min(WAVE_COUNT, waveSpawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            if (waveSpawnPoints[i] == null) continue;
            var go = Instantiate(wavePrefab, waveSpawnPoints[i].position, waveSpawnPoints[i].rotation);
            HookDeath(go, OnWaveKill);
        }
    }

    bool HookDeath(GameObject go, System.Action cb)
    {
        var bridge = go.GetComponentInChildren<EnemyDeathBridge>();
        if (bridge != null) { bridge.OnEnemyDied += cb; return true; }

        var td = go.GetComponentInChildren<TakeDamageTest>();
        if (td != null) { td.OnEnemyDied += cb; return true; }

        Debug.LogWarning("[Tutorial] No death script found on enemy prefab.");
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Kill Callbacks
    // ─────────────────────────────────────────────────────────────────────────

    void OnEnemy1Died() => Advance(); // → STEP_SKILL_T (lv30 + T)
    void OnEnemy2Died() => Advance(); // → STEP_ULT (lv60 + Q)

    void OnWaveKill()
    {
        _waveKills++;
        // Show live counter
        if (tutorialText != null)
            tutorialText.text = $"Defeat all 10 enemies! ({_waveKills}/{WAVE_COUNT})";

        if (_waveKills >= WAVE_COUNT)
            Advance(); // → STEP_TAB_SHEATH
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Completion
    // ─────────────────────────────────────────────────────────────────────────

    void TriggerCompletion()
    {
        _completed = true;
        if (tutorialText != null) tutorialText.text = "";

        if (completionCanvas != null)
        {
            completionCanvas.SetActive(true);
            if (completionMessageText != null)
                completionMessageText.text = "Tutorial Complete! Well done, adventurer!";
            StartCoroutine(FinishAndReturn());
        }
        else
        {
            if (questFinisher != null) questFinisher.FinishTutorial();
        }
    }

    IEnumerator FinishAndReturn()
    {
        yield return new WaitForSeconds(3f);
        if (completionMessageText != null) completionMessageText.text = "";
        if (questFinisher != null) questFinisher.FinishTutorial();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Mastery / Levels
    // ─────────────────────────────────────────────────────────────────────────

    void SetAllMasteryLevels(int lv)
    {
        if (WeaponMasteryManager.Instance == null) return;
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Sword, lv);
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Axe,   lv);
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Mage,  lv);
        Debug.Log($"[Tutorial] Mastery level set to {lv}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called by WeaponController.OnWeaponChanged Unity Event.</summary>
    public void OnWeaponChanged()
    {
        if (_step == STEP_CHANGE_WP && !_completed) Advance();
    }

    /// <summary>Skip button — advance one step.</summary>
    public void ShowNextTutorialText() => Advance();
}