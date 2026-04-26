using UnityEngine;

/// <summary>
/// Debug Cheat Panel — Toggle với F1.
/// Tự tạo UI bằng IMGUI, không cần setup Inspector.
/// Gắn lên bất kì GameObject DontDestroyOnLoad (VD: GameSettings).
/// </summary>
public class CheatPanel : MonoBehaviour
{
    public static CheatPanel Instance { get; private set; }

    // ── Cheat Flags ──
    public static bool NoCooldown { get; private set; }
    public static float DamageMultiplier { get; private set; } = 1f;

    private bool _show;
    private Rect _windowRect;
    private bool _rectInitialized;

    // Cache
    private AbilityIconManager _aimCache;
    // (Không cần backup/restore — MouseLockManager quản lý trạng thái)

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // Kiểm tra phân quyền: Chỉ Server (Host/SinglePlayer) mới được bật Cheat
            if (Artsystack.ArtsystackGui.FusionConnectionManager.Instance != null && 
                Artsystack.ArtsystackGui.FusionConnectionManager.Instance.Runner != null)
            {
                if (!Artsystack.ArtsystackGui.FusionConnectionManager.Instance.Runner.IsServer)
                {
                    Debug.LogWarning("[CheatPanel] Bạn không phải là Host. Lệnh Cheat bị từ chối!");
                    return;
                }
            }

            _show = !_show;

            if (_show)
            {
                // Show cursor via MouseLockManager
                MouseLockManager.Instance?.SetGameplayCursorLocked(false);
            }
            else
            {
                // Hide cursor via MouseLockManager
                MouseLockManager.Instance?.SetGameplayCursorLocked(true);
            }
        }

        // Continuously clear cooldowns when cheat is on
        if (NoCooldown)
        {
            if (_aimCache == null)
                _aimCache = FindFirstObjectByType<AbilityIconManager>();

            if (_aimCache != null)
            {
                _aimCache.TutorialClearCooldown(AbilityInput.E);
                _aimCache.TutorialClearCooldown(AbilityInput.R);
                _aimCache.TutorialClearCooldown(AbilityInput.T);
                _aimCache.TutorialClearCooldown(AbilityInput.Q_Ultimate);
            }
        }
    }

    void OnGUI()
    {
        if (!_show) return;

        // Phóng to toàn bộ UI gấp 1.6 lần
        float uiScale = 1.6f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

        // Initialize rect on first use (Screen.width will be affected by scale)
        if (!_rectInitialized)
        {
            float scaledWidth = Screen.width / uiScale;
            _windowRect = new Rect(scaledWidth - 320, 20, 300, 400);
            _rectInitialized = true;
        }

        _windowRect = GUI.Window(999, _windowRect, DrawWindow, "⚡ CHEAT PANEL (F1)");
    }

    void DrawWindow(int id)
    {
        GUILayout.Space(5);

        // ── No Cooldown ──
        bool newNoCd = GUILayout.Toggle(NoCooldown, "  No Cooldown Skill", GUILayout.Height(30));
        if (newNoCd != NoCooldown)
        {
            NoCooldown = newNoCd;
            Debug.Log($"[CheatPanel] No Cooldown = {NoCooldown}");
        }

        GUILayout.Space(5);

        // ── x10 Damage ──
        bool dmgOn = DamageMultiplier > 1f;
        bool newDmg = GUILayout.Toggle(dmgOn, "  x10 Damage", GUILayout.Height(30));
        if (newDmg != dmgOn)
        {
            DamageMultiplier = newDmg ? 10f : 1f;
            Debug.Log($"[CheatPanel] Damage Multiplier = {DamageMultiplier}x");
        }

        GUILayout.Space(10);

        // ── Skip Quest ──
        GUILayout.Label("── Quest ──");
        if (GUILayout.Button("Skip Current Quest", GUILayout.Height(35)))
            SkipQuest();

        GUILayout.Space(10);

        // ── Level ──
        GUILayout.Label("── Weapon Mastery Level ──");
        if (GUILayout.Button("Set All Weapons → Level 30", GUILayout.Height(35)))
            SetAllWeaponLevel(30);

        GUILayout.Space(3);
        if (GUILayout.Button("Set All Weapons → Level 60", GUILayout.Height(35)))
            SetAllWeaponLevel(60);

        GUILayout.Space(10);

        // ── Info ──
        GUI.color = Color.gray;
        GUILayout.Label($"Cooldown: {(NoCooldown ? "OFF" : "Normal")}");
        GUILayout.Label($"Damage: {DamageMultiplier}x");
        if (QuestManager.Instance != null)
        {
            var active = QuestManager.Instance.GetActiveQuest();
            GUILayout.Label($"Active Quest: {(active != null ? active.questTitle : "None")}");
        }
        GUI.color = Color.white;

        GUI.DragWindow();
    }

    // ── Implementations ──

    void SkipQuest()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[CheatPanel] QuestManager not found!");
            return;
        }

        var activeQuest = QuestManager.Instance.GetActiveQuest();
        if (activeQuest == null)
        {
            Debug.LogWarning("[CheatPanel] No active quest to skip!");
            return;
        }

        int questID = activeQuest.questID;
        // Advance until complete
        for (int i = 0; i < 50; i++) // safety limit
        {
            if (QuestManager.Instance.GetState(questID) != QuestManager.QuestState.Active)
                break;
            QuestManager.Instance.AdvanceStep(questID);
        }

        Debug.Log($"[CheatPanel] Skipped quest {questID}: {activeQuest.questTitle}");
    }

    void SetAllWeaponLevel(int level)
    {
        if (WeaponMasteryManager.Instance == null)
        {
            Debug.LogWarning("[CheatPanel] WeaponMasteryManager not found!");
            return;
        }

        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Sword, level);
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Axe, level);
        WeaponMasteryManager.Instance.SetMasteryLevel(WeaponType.Mage, level);

        Debug.Log($"[CheatPanel] All weapons set to level {level}");
    }
}
