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
/// 15 [Spawn 10 enemies wave 2] Kill Enemy: Defeat all 10 enemies!
/// 16 [Complete] Congratulations → 5s → teleport to Map_Chinh
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
    //  Auto-wired references (found at runtime)
    // ─────────────────────────────────────────────────────────────────────────
    WeaponSwapper     _weaponSwapper;
    EnemyDetection    _enemyDetection;
    Character         _character;
    WeaponController  _weaponController;

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
    const int STEP_ULT         = 9;
    const int STEP_WAVE        = 10;  // After 5s pause
    const int STEP_TAB_SHEATH  = 11;
    const int STEP_OPEN_INV    = 12;
    const int STEP_CHANGE_WP   = 13;  // Đổi vũ khí + Ấn I để tắt (gộp 1 step)
    const int STEP_TAB_DRAW2   = 14;  // Rút vũ khí mới
    const int STEP_KILL_WAVE2  = 15;  // Wave 2
    const int STEP_PRESS_F     = 16;  // Ấn F để về map
    const int WAVE_COUNT       = 10;

    readonly string[] _texts =
    {
        /* 0  */ "Press <color=#FFD700><b>Space</b></color> to jump",
        /* 1  */ "Press <color=#FFD700><b>Tab</b></color> to equip your weapon",
        /* 2  */ "Press <color=#FFD700><b>Right Mouse Button</b></color> to roll",
        /* 3  */ "Press <color=#FFD700><b>Left Mouse Button</b></color> to attack",
        /* 4  */ "Press <color=#FFD700><b>E</b></color> to use Skill 1",
        /* 5  */ "Press <color=#FFD700><b>R</b></color> to use Skill 2",
        /* 6  */ "Kill the enemy!",
        /* 7  */ "Press <color=#FFD700><b>T</b></color> to use Skill 3",
        /* 8  */ "Kill the enemy!",
        /* 9  */ "Press <color=#FFD700><b>Q</b></color> to use your Ultimate",
        /* 10 */ "Defeat all <color=#FFD700><b>10</b></color> enemies!",
        /* 11 */ "Press <color=#FFD700><b>Tab</b></color> to sheathe your weapon",
        /* 12 */ "Press <color=#FFD700><b>I</b></color> to open your inventory",
        /* 13 */ "Change your weapon",
        // 14 text handled dynamically after weapon change
        /* 14 */ "Press <color=#FFD700><b>Tab</b></color> to draw your new weapon",
        /* 15 */ "Kill Enemy: Defeat all <color=#FFD700><b>10</b></color> enemies!",
        /* 16 */ "Congratulations! Tutorial Complete!\nPress <color=#FFD700><b>F</b></color> to return to the main map!",
    };

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────
    int  _step          = 0;
    bool _waiting       = false;
    bool _completed     = false;
    int  _waveKills     = 0;
    bool _inWave2       = false;   // true khi đang ở wave kill lần 2
    bool _weaponChanged = false;   // true sau khi player đã đổi vũ khí ở step 13

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (completionCanvas != null) completionCanvas.SetActive(false);
        SetAllMasteryLevels(1);

        // ── Auto-wire WeaponSwapper event ─────────────────────────────────
        _weaponSwapper  = FindFirstObjectByType<WeaponSwapper>(FindObjectsInactive.Include);
        _enemyDetection = FindFirstObjectByType<EnemyDetection>(FindObjectsInactive.Include);
        _character      = FindFirstObjectByType<Character>(FindObjectsInactive.Include);
        _weaponController = FindFirstObjectByType<WeaponController>(FindObjectsInactive.Include);

        if (_weaponController != null)
        {
            _weaponController.OnWeaponChanged += OnWeaponChangedFromController;
            Debug.Log("[Tutorial] Auto-subscribed to WeaponController.OnWeaponChanged");
        }

        if (_weaponSwapper != null)
        {
            _weaponSwapper.OnWeaponSwapped.AddListener(OnWeaponChanged);
            Debug.Log("[Tutorial] Auto-subscribed to WeaponSwapper.OnWeaponSwapped");
        }
        else
        {
            Debug.LogWarning("[Tutorial] WeaponSwapper not found – OnWeaponChanged must be wired in Inspector!");
        }

        StartCoroutine(ShowDelayed(0));
    }

    void OnDestroy()
    {
        if (_weaponController != null)
        {
            _weaponController.OnWeaponChanged -= OnWeaponChangedFromController;
        }
    }

    void OnWeaponChangedFromController(WeaponSO weapon)
    {
        OnWeaponChanged();
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
            // STEP_CHANGE_WP: đợi weapon changed event → sau đó đợi I press
            case STEP_CHANGE_WP:
                if (_weaponChanged && IsKeyDown(KeyCode.I, "i"))          Advance(); break;
            case STEP_TAB_DRAW2:  if (IsKeyDown(KeyCode.Tab, "tab"))     Advance(); break;
            case STEP_PRESS_F:    if (IsKeyDown(KeyCode.F, "f"))         TriggerCompletion(); break;
            // STEP_KILL1, STEP_KILL2, STEP_WAVE, STEP_KILL_WAVE2 → event-driven
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

        // STEP_WAVE & STEP_KILL_WAVE2: text shown by spawn coroutines after delay, not here
        bool skipText = (_step == STEP_WAVE || _step == STEP_KILL_WAVE2);
        bool instant  = (_step == STEP_KILL1 || _step == STEP_KILL2 || _step == STEP_CHANGE_WP);
        if      (skipText) { /* text handled by spawner */ }
        else if (instant)  ShowNow(_step);
        else               StartCoroutine(ShowDelayed(_step));
    }

    void OnEnterStep(int step)
    {
        switch (step)
        {
            case STEP_KILL1:       SpawnEnemy1();              break;
            case STEP_SKILL_T:     SetAllMasteryLevels(30);    break;
            case STEP_KILL2:       SpawnEnemy2();              break;
            case STEP_ULT:         SetAllMasteryLevels(60);    break;
            case STEP_WAVE:        StartCoroutine(DelayedWaveSpawn()); break;
            case STEP_CHANGE_WP:   PrepareForWeaponChange();   break;
            case STEP_TAB_DRAW2:   ShowNow(STEP_TAB_DRAW2);   break;  // Show text ngay
            case STEP_KILL_WAVE2:  SpawnWave2();               break;
            case STEP_PRESS_F:     ShowNow(STEP_PRESS_F);      break;  // Show text ngay
        }
    }

    /// <summary>
    /// Reset combat state và sheath weapon để WeaponSwapper không bị block.
    /// </summary>
    void PrepareForWeaponChange()
    {
        _weaponChanged = false;

        if (_weaponSwapper != null)
        {
            _weaponSwapper.SetTutorialMode(true);
            Debug.Log("[Tutorial] WeaponSwapper tutorial mode ON.");
        }
        if (_enemyDetection != null)
            _enemyDetection.SetCombatState(false);

        // Hiện "Change your weapon" ngay khi mở inventory
        ShowNow(STEP_CHANGE_WP);
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

        _inWave2   = false;
        _waveKills = 0;
        int count = Mathf.Min(WAVE_COUNT, waveSpawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            if (waveSpawnPoints[i] == null) continue;
            var go = Instantiate(wavePrefab, waveSpawnPoints[i].position, waveSpawnPoints[i].rotation);
            HookDeath(go, OnWaveKill);
        }
    }

    void SpawnWave2()
    {
        if (wavePrefab == null || waveSpawnPoints == null || waveSpawnPoints.Length == 0)
        { TriggerCompletion(); return; }

        _inWave2   = true;
        _waveKills = 0;
        ShowNow(STEP_KILL_WAVE2);
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
        string label = _inWave2 ? "Kill Enemy: Defeat all 10 enemies!" : "Defeat all 10 enemies!";
        if (tutorialText != null)
            tutorialText.text = $"{label} ({_waveKills}/{WAVE_COUNT})";

        if (_waveKills >= WAVE_COUNT)
        {
            if (_inWave2)
                Advance();  // Wave 2 xong → STEP_PRESS_F ("Ấn F để về map")
            else
                Advance();  // Wave 1 xong → STEP_TAB_SHEATH
        }
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
                completionMessageText.text = "Congratulations! Tutorial Complete!";
            StartCoroutine(FinishAndReturn());
        }
        else
        {
            if (questFinisher != null) questFinisher.FinishTutorial();
        }
    }

    IEnumerator FinishAndReturn()
    {
        SetAllMasteryLevels(1);  // Reset về level 1 trước khi về map
        yield return null;       // No delay – teleport immediately
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

    /// <summary>Called by WeaponSwapper.OnWeaponSwapped or WeaponController Unity Event.</summary>
    public void OnWeaponChanged()
    {
        if (_step != STEP_CHANGE_WP || _completed || _weaponChanged) return;

        // Đổi xong → set flag + show combined text + tắt tutorial mode
        _weaponChanged = true;
        if (_weaponSwapper != null) _weaponSwapper.SetTutorialMode(false);
        if (tutorialText != null)
            tutorialText.text = "Weapon changed!\nPress <color=#FFD700><b>I</b></color> to close your inventory";
    }

    /// <summary>Skip button — advance one step.</summary>
    public void ShowNextTutorialText() => Advance();
}