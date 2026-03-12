using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton quản lý toàn bộ trạng thái quest của game.
/// Đồng bộ 2 chiều với PlayerPrefs (QUEST_COUNT, QUEST_1..5) để không
/// phá vỡ code SetEnemyRoom, Teleporter, EnemyDamage hiện có.
/// </summary>
public class QuestManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────
    public static QuestManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────
    [Header("Dữ liệu quest (kéo QuestData assets vào đây theo thứ tự ID)")]
    public QuestData[] quests;

    // ─────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────
    public enum QuestState { Locked, Available, Active, Completed }

    // questID → (state, currentStep)
    private Dictionary<int, QuestState> _states   = new Dictionary<int, QuestState>();
    private Dictionary<int, int>        _stepIndex = new Dictionary<int, int>();

    // ─────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────
    public delegate void QuestEvent(int questID);
    public static event QuestEvent OnQuestAccepted;
    public static event QuestEvent OnQuestStepAdvanced;
    public static event QuestEvent OnQuestCompleted;

    // ─────────────────────────────────────────────────────────────────────
    //  PlayerPrefs Keys
    // ─────────────────────────────────────────────────────────────────────
    const string KEY_COUNT = "QUEST_COUNT";
    string QKey(int id) => "QUEST_" + id;

    // ─────────────────────────────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshFromPrefs();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Đọc lại tất cả trạng thái từ PlayerPrefs (gọi khi scene load xong).</summary>
    public void RefreshFromPrefs()
    {
        _states.Clear();
        _stepIndex.Clear();

        if (quests == null || quests.Length == 0)
        {
            Debug.LogError("[QuestManager] 'Quests' array is EMPTY in Inspector! Please assign QuestData.");
            return;
        }

        int count = PlayerPrefs.GetInt(KEY_COUNT, 0);
        Debug.Log($"[QuestManager] Initializing states from Prefs. QUEST_COUNT={count}");

        foreach (var qd in quests)
        {
            if (qd == null) continue;

            int id  = qd.questID;
            int raw = PlayerPrefs.GetInt(QKey(id), 0);

            QuestState state;
            if      (raw == 2) state = QuestState.Completed;
            else if (raw == 1) state = QuestState.Active;
            else if (id == 1)  state = QuestState.Active; // New Game: Quest 1 is active by default
            else if (count >= id - 1) state = QuestState.Available;
            else               state = QuestState.Locked;

            _states[id]   = state;
            _stepIndex[id] = (raw == 1) ? PlayerPrefs.GetInt(QKey(id) + "_STEP", 0) : 0;
            
            Debug.Log($"[QuestManager] Registered Quest {id} '{qd.questTitle}': {state}");
        }
        
        if (quests.Length < 2) 
        {
            Debug.LogWarning("[QuestManager] Only " + quests.Length + " quests registered. If you are on the second quest, please make sure you added the second QuestData asset to the 'Quests' array in the Inspector!");
        }
    }

    /// <summary>Trả về trạng thái của một quest theo ID.</summary>
    public QuestState GetState(int questID)
        => _states.TryGetValue(questID, out var s) ? s : QuestState.Locked;

    /// <summary>Trả về index bước hiện tại của quest (0-based).</summary>
    public int GetStepIndex(int questID)
        => _stepIndex.TryGetValue(questID, out var i) ? i : 0;

    /// <summary>Trả về QuestData theo ID, null nếu không tìm thấy.</summary>
    public QuestData GetQuestData(int questID)
    {
        foreach (var qd in quests)
            if (qd.questID == questID) return qd;
        return null;
    }

    /// <summary>Quest đang Active đầu tiên tìm được (để hiển thị HUD).</summary>
    public QuestData GetActiveQuest()
    {
        foreach (var qd in quests)
            if (GetState(qd.questID) == QuestState.Active) return qd;
        return null;
    }

    /// <summary>Bước hiện tại của quest đang Active (để hiển thị HUD).</summary>
    public QuestStep GetActiveStep()
    {
        var qd = GetActiveQuest();
        if (qd == null) return null;
        int idx = GetStepIndex(qd.questID);
        if (qd.steps == null || qd.steps.Length == 0) return null;
        idx = Mathf.Clamp(idx, 0, qd.steps.Length - 1);
        return qd.steps[idx];
    }

    // ─── Accept ──────────────────────────────────────────────────────────

    /// <summary>
    /// Nhận quest (chuyển Available → Active).
    /// Gọi khi NPC Leona hoàn thành hội thoại.
    /// </summary>
    public bool AcceptQuest(int questID)
    {
        var qd = GetQuestData(questID);
        if (qd == null)
        {
            Debug.LogError($"[QuestManager] Cannot accept Quest {questID}: QuestData NOT FOUND in 'quests' array!\n" +
                           $"GUIDE: Please create a new QuestData asset, set its Quest ID to {questID}, and drag it into the 'Quests' list on the QuestManager object in your scene.");
            return false;
        }

        var currentState = GetState(questID);
        if (currentState != QuestState.Available && currentState != QuestState.Locked) // Allow locked to be forced active in some cases
        {
            Debug.LogWarning($"[QuestManager] Quest {questID} is already {currentState}. Re-activating anyway.");
        }

        _states[questID]   = QuestState.Active;
        _stepIndex[questID] = 0;

        // Sync PlayerPrefs
        PlayerPrefs.SetInt(QKey(questID), 1);
        PlayerPrefs.SetInt(QKey(questID) + "_STEP", 0);
        
        // Update KEY_COUNT to ensure continuity
        if (PlayerPrefs.GetInt(KEY_COUNT, 0) < questID)
            PlayerPrefs.SetInt(KEY_COUNT, questID);
            
        PlayerPrefs.Save();

        Debug.Log($"[QuestManager] Successfully ACCEPTED Quest {questID}: {qd.questTitle}");
        OnQuestAccepted?.Invoke(questID);
        return true;
    }

    // ─── Advance Step ────────────────────────────────────────────────────

    /// <summary>
    /// Tiến sang bước tiếp theo của quest đang Active.
    /// Nếu hết bước → tự động hoàn thành quest.
    /// </summary>
    public void AdvanceStep(int questID)
    {
        if (GetState(questID) != QuestState.Active) return;

        var qd = GetQuestData(questID);
        if (qd == null) return;

        int next = _stepIndex[questID] + 1;

        if (next >= qd.steps.Length)
        {
            CompleteQuest(questID);
            return;
        }

        _stepIndex[questID] = next;
        PlayerPrefs.SetInt(QKey(questID) + "_STEP", next);
        PlayerPrefs.Save();

        Debug.Log($"[QuestManager] Quest {questID} → Bước {next}: {qd.steps[next].stepTitle}");
        OnQuestStepAdvanced?.Invoke(questID);
    }

    // ─── Complete ────────────────────────────────────────────────────────

    /// <summary>
    /// Hoàn thành quest: trao thưởng, cập nhật PlayerPrefs.
    /// </summary>
    public void CompleteQuest(int questID)
    {
        if (GetState(questID) == QuestState.Completed) return;

        _states[questID] = QuestState.Completed;

        // Đồng bộ PlayerPrefs (= 2 nghĩa là "đã hoàn thành")
        PlayerPrefs.SetInt(QKey(questID), 2);

        // ← FIX: đảm bảo KEY_COUNT đủ lớn để quest tiếp theo Available sau reload
        if (PlayerPrefs.GetInt(KEY_COUNT, 0) < questID)
            PlayerPrefs.SetInt(KEY_COUNT, questID);

        PlayerPrefs.Save();

        // Trao thưởng
        var qd = GetQuestData(questID);
        if (qd != null && HeroInformation.player != null)
        {
            HeroInformation.player.gold            += qd.rewardGold;
            HeroInformation.player.experiencePoint += qd.rewardExp;
            Debug.Log($"[QuestManager] Quest {questID} hoàn thành! +{qd.rewardGold} gold, +{qd.rewardExp} EXP");
        }

        // Mở khóa quest tiếp theo nếu có
        int nextID = questID + 1;
        if (_states.ContainsKey(nextID) && _states[nextID] == QuestState.Locked)
            _states[nextID] = QuestState.Available;

        OnQuestCompleted?.Invoke(questID);
    }

    // ─── Reset ───────────────────────────────────────────────────────────

    /// <summary>Reset toàn bộ quest (dùng khi tạo game mới, tương đương SetGlobalVar).</summary>
    [ContextMenu("Reset All Quests")]
    public void ResetAllQuests()
    {
        PlayerPrefs.SetInt(KEY_COUNT, 0);
        foreach (var qd in quests)
        {
            PlayerPrefs.SetInt(QKey(qd.questID), 0);
            PlayerPrefs.SetInt(QKey(qd.questID) + "_STEP", 0);
        }
        PlayerPrefs.Save();
        RefreshFromPrefs();
        Debug.Log("[QuestManager] Đã reset tất cả quest.");
    }
}
