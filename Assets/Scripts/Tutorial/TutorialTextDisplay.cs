using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controls the full tutorial step sequence:
/// delayed text display, enemy spawning, kill tracking, level-setting, and final teleport canvas.
/// </summary>
public class TutorialTextDisplay : MonoBehaviour
{
    // ─── UI ──────────────────────────────────────────────────────────────────
    [Header("── UI ──")]
    public TMP_Text tutorialText;
    [Tooltip("Panel shown when tutorial is complete")]
    public GameObject completionCanvas;        // Canvas to show at the very end
    [Tooltip("Message shown on completion (auto-hides after 5s)")]
    public TMP_Text completionMessageText;

    // ─── Quest ───────────────────────────────────────────────────────────────
    [Header("── Quest ──")]
    public TutorialQuestFinisher questFinisher;

    // ─── Timing ──────────────────────────────────────────────────────────────
    [Header("── Timing ──")]
    public float textDelay = 2f;   // Delay before each step text appears

    // ─── Enemy Spawning ───────────────────────────────────────────────────────
    [Header("── Enemy Spawning ──")]
    [Tooltip("Prefab for the first solo enemy (step after R skill)")]
    public GameObject enemyPrefab1;
    [Tooltip("Spawn point for enemy 1")]
    public Transform spawnPoint1;

    [Tooltip("Prefab for the second solo enemy (step after T skill)")]
    public GameObject enemyPrefab2;
    [Tooltip("Spawn point for enemy 2")]
    public Transform spawnPoint2;

    [Tooltip("Prefab for the final wave (10 enemies)")]
    public GameObject wavePrefab;
    [Tooltip("Spawn points for the final 10 enemies (at least 10 transforms)")]
    public Transform[] waveSpawnPoints;

    // ─── Runtime ─────────────────────────────────────────────────────────────
    private int  _step        = 0;
    private bool _waiting     = false;   // Waiting for a specific input/event
    private bool _completed   = false;
    private int  _waveKills   = 0;
    private const int WAVE_COUNT = 10;

    // Step constants for readability
    private const int STEP_SPACE     = 0;
    private const int STEP_TAB_DRAW  = 1;
    private const int STEP_ROLL      = 2;
    private const int STEP_ATTACK    = 3;
    private const int STEP_SKILL_E   = 4;
    private const int STEP_SKILL_R   = 5;
    private const int STEP_KILL1     = 6;   // Waiting for enemy1 death
    private const int STEP_SKILL_T   = 7;
    private const int STEP_KILL2     = 8;   // Waiting for enemy2 death
    private const int STEP_ULT       = 9;
    private const int STEP_TAB_SHEATH= 10;
    private const int STEP_OPEN_INV  = 11;
    private const int STEP_CHANGE_WP = 12;  // Waiting for weapon swap event
    private const int STEP_CLOSE_INV = 13;
    private const int STEP_WAVE      = 14;  // Waiting for 10 kills
    // End

    private readonly string[] _texts =
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
        /* 10 */ "Press Tab to sheathe your weapon",
        /* 11 */ "Press I to open your inventory",
        /* 12 */ "Change your weapon",
        /* 13 */ "Press I to close your inventory",
        /* 14 */ "Defeat all 10 enemies!",
    };

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (completionCanvas != null) completionCanvas.SetActive(false);

        // Set all weapons to level 1 at tutorial start
        SetAllMasteryLevels(1);

        StartCoroutine(ShowStepDelayed(0));
    }

    void Update()
    {
        if (_completed || _waiting) return;

        switch (_step)
        {
            case STEP_SPACE:
                if (Input.GetKeyDown(KeyCode.Space))        Advance(); break;
            case STEP_TAB_DRAW:
                if (Input.GetKeyDown(KeyCode.Tab))          Advance(); break;
            case STEP_ROLL:
                if (Input.GetMouseButtonDown(1))            Advance(); break;
            case STEP_ATTACK:
                if (Input.GetMouseButtonDown(0))            Advance(); break;
            case STEP_SKILL_E:
                if (Input.GetKeyDown(KeyCode.E))            Advance(); break;
            case STEP_SKILL_R:
                if (Input.GetKeyDown(KeyCode.R))            Advance(); break;
            case STEP_SKILL_T:
                if (Input.GetKeyDown(KeyCode.T))            Advance(); break;
            case STEP_ULT:
                if (Input.GetKeyDown(KeyCode.Q))            Advance(); break;
            case STEP_TAB_SHEATH:
                if (Input.GetKeyDown(KeyCode.Tab))          Advance(); break;
            case STEP_OPEN_INV:
                if (Input.GetKeyDown(KeyCode.I))            Advance(); break;
            case STEP_CLOSE_INV:
                if (Input.GetKeyDown(KeyCode.I))            Advance(); break;
            // STEP_KILL1, STEP_KILL2, STEP_CHANGE_WP, STEP_WAVE → driven by events
        }
    }

    // ─── Advance ─────────────────────────────────────────────────────────────

    void Advance()
    {
        _step++;
        HandleStepSideEffects(_step);
        if (_step < _texts.Length)
        {
            // Kill/wave steps: show text immediately when enemy spawns
            bool isKillStep = (_step == STEP_KILL1 || _step == STEP_KILL2 || _step == STEP_WAVE);
            if (isKillStep) ShowStepNow(_step);
            else            StartCoroutine(ShowStepDelayed(_step));
        }
    }

    /// <summary>Hook special events that happen WHEN entering a step.</summary>
    void HandleStepSideEffects(int step)
    {
        switch (step)
        {
            case STEP_KILL1:
                SpawnEnemy1();
                break;

            case STEP_SKILL_T:
                // Set mastery to level 30 after kill 1
                SetAllMasteryLevels(30);
                break;

            case STEP_KILL2:
                SpawnEnemy2();
                break;

            case STEP_ULT:
                // Set mastery to level 60 after kill 2
                SetAllMasteryLevels(60);
                break;

            case STEP_WAVE:
                SpawnWave();
                break;
        }
    }

    // ─── Delayed Text ─────────────────────────────────────────────────────────

    IEnumerator ShowStepDelayed(int step)
    {
        _waiting = true;
        if (tutorialText != null) tutorialText.text = "";
        yield return new WaitForSeconds(textDelay);
        if (tutorialText != null && step < _texts.Length)
            tutorialText.text = _texts[step];
        _waiting = false;
    }

    void ShowStepNow(int step)
    {
        if (tutorialText != null && step < _texts.Length)
            tutorialText.text = _texts[step];
    }

    // ─── Enemy Spawning ───────────────────────────────────────────────────────

    void SpawnEnemy1()
    {
        if (enemyPrefab1 == null || spawnPoint1 == null) { DebugSkipKill1(); return; }
        var go = Instantiate(enemyPrefab1, spawnPoint1.position, spawnPoint1.rotation);
        if (!HookDeathEvent(go, OnEnemy1Died))
        { Debug.LogWarning("[Tutorial] No death script on enemy1, skipping kill wait."); Advance(); }
    }

    void SpawnEnemy2()
    {
        if (enemyPrefab2 == null || spawnPoint2 == null) { DebugSkipKill2(); return; }
        var go = Instantiate(enemyPrefab2, spawnPoint2.position, spawnPoint2.rotation);
        if (!HookDeathEvent(go, OnEnemy2Died))
        { Debug.LogWarning("[Tutorial] No death script on enemy2, skipping kill wait."); Advance(); }
    }

    void SpawnWave()
    {
        if (wavePrefab == null || waveSpawnPoints == null || waveSpawnPoints.Length == 0)
        { Debug.LogWarning("[Tutorial] Wave prefab or spawn points not set!"); TriggerCompletion(); return; }

        _waveKills = 0;
        int count = Mathf.Min(WAVE_COUNT, waveSpawnPoints.Length);
        for (int i = 0; i < count; i++)
        {
            if (waveSpawnPoints[i] == null) continue;
            var go = Instantiate(wavePrefab, waveSpawnPoints[i].position, waveSpawnPoints[i].rotation);
            HookDeathEvent(go, OnWaveEnemyDied);
        }
    }

    /// <summary>
    /// Hooks into the enemy death event using EnemyDeathBridge (DungeonMania skeletons)
    /// or TakeDamageTest (player-side enemies) — whichever is found.
    /// Returns true if successfully hooked.
    /// </summary>
    bool HookDeathEvent(GameObject go, System.Action callback)
    {
        var bridge = go.GetComponentInChildren<EnemyDeathBridge>();
        if (bridge != null) { bridge.OnEnemyDied += callback; return true; }

        var td = go.GetComponentInChildren<TakeDamageTest>();
        if (td != null) { td.OnEnemyDied += callback; return true; }

        return false;
    }

    // ─── Kill Callbacks ───────────────────────────────────────────────────────

    void OnEnemy1Died()   => Advance();   // → STEP_SKILL_T
    void OnEnemy2Died()   => Advance();   // → STEP_ULT

    void OnWaveEnemyDied()
    {
        _waveKills++;
        if (_waveKills >= WAVE_COUNT)
            TriggerCompletion();
    }

    // ─── Completion ───────────────────────────────────────────────────────────

    void TriggerCompletion()
    {
        _completed = true;
        if (tutorialText != null) tutorialText.text = "";

        if (completionCanvas != null)
        {
            completionCanvas.SetActive(true);
            if (completionMessageText != null)
                completionMessageText.text = "Tutorial Complete! Well done, adventurer!";
            StartCoroutine(HideCompletionMessage());
        }
    }

    IEnumerator HideCompletionMessage()
    {
        yield return new WaitForSeconds(5f);
        if (completionMessageText != null) completionMessageText.text = "";
        // Do NOT hide completionCanvas — player still needs to press the button to return
    }

    // ─── Public API (called from events) ──────────────────────────────────────

    /// <summary>Call from WeaponSwapper.OnWeaponSwapped event when player changes weapon.</summary>
    public void OnWeaponChanged()
    {
        if (_step == STEP_CHANGE_WP && !_completed)
            Advance();   // → STEP_CLOSE_INV
    }

    /// <summary>Skip current step (debug / skip button).</summary>
    public void ShowNextTutorialText() => Advance();

    // ─── Level Setting ────────────────────────────────────────────────────────

    void SetAllMasteryLevels(int level)
    {
        if (WeaponMasteryManager.Instance == null)
        {
            Debug.LogWarning("[Tutorial] WeaponMasteryManager.Instance is null, cannot set mastery level.");
            return;
        }
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Sword, level);
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Axe,   level);
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Mage,  level);
        Debug.Log($"[Tutorial] All mastery levels set to {level}");
    }

    // ─── Debug Helpers ────────────────────────────────────────────────────────
    void DebugSkipKill1() { Debug.LogWarning("[Tutorial] Enemy1 prefab/spawn missing, skipping."); Advance(); }
    void DebugSkipKill2() { Debug.LogWarning("[Tutorial] Enemy2 prefab/spawn missing, skipping."); Advance(); }
}