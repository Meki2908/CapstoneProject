using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn vào Button để chuyển Scene + set độ khó dungeon.
/// 
/// === HƯỚNG DẪN SETUP ===
/// 1. Gắn script này vào mỗi Button (Easy / Normal / Hard)
/// 2. Điền "Target Scene Name" (VD: "MapDamLay", "MapSaMac", "MapHell")
/// 3. Chọn "Dungeon Difficulty" từ dropdown (Easy / Normal / Hard)
/// 4. Chọn "Map Type" (0=Sa Mạc, 1=Đầm Lầy, 2=Địa Ngục)
/// </summary>
[RequireComponent(typeof(Button))]
public class SceneTeleportButton : MonoBehaviour
{
    public enum MultiplayerActionMode
    {
        Auto,
        DirectTeleport,
        InviteOnly,
        StartWhenReady,
        SelectDifficultyForInvite
    }

    [Header("── Scene Settings ──")]
    [Tooltip("Tên chính xác của Scene muốn chuyển đến (phải có trong Build Settings)")]
    public string targetSceneName;

    [Tooltip("Delay trước khi chuyển (để kịp nghe tiếng click hoặc chạy hiệu ứng)")]
    public float delay = 0.2f;

    [Header("── Dungeon Difficulty ──")]
    [Tooltip("Độ khó dungeon: Easy (không boss), Normal (1 boss), Hard (2 boss + boss chính)")]
    public DungeonDifficulty dungeonDifficulty = DungeonDifficulty.Normal;

    [Tooltip("Map type: 0=Sa Mạc (Desert), 1=Đầm Lầy (Swamp), 2=Địa Ngục (Hell)")]
    [Range(0, 2)]
    public int mapType = 0;

    [Header("── Quest Advance (tuỳ chọn) ──")]
    [Tooltip("Set questID > 0 để advance quest khi ấn button này.")]
    public int questID       = 0;
    [Tooltip("Bước hiện tại phải đang ở đây thì mới advance.")]
    public int triggerAtStep = 4;

    [Header("── Multiplayer Invite Flow ──")]
    [Tooltip("Auto: dựa trên tên nút (Invite/Start).")]
    public MultiplayerActionMode multiplayerActionMode = MultiplayerActionMode.Auto;
    [Tooltip("Thời gian chờ client phản hồi lời mời (giây).")]
    public float inviteDurationSeconds = 20f;

    [Header("── Debug ──")]
    public bool enableDebugLogs = true;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (IsPassiveCloseButton())
        {
            Dbg($"Skip runtime bind for close button '{name}'");
            return;
        }
        btn.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick()
    {
        MultiplayerActionMode actionMode = ResolveActionMode();
        var localChar = Character.LocalCharacter;
        bool hasRunner = localChar != null && localChar.Runner != null && localChar.Runner.IsRunning;
        bool inviteFlowEnabled = hasRunner && IsMultiplayerSession(localChar.Runner);
        bool isHost = localChar != null && localChar.IsHostAuthorityForParty();

        if (inviteFlowEnabled && actionMode == MultiplayerActionMode.DirectTeleport && !isHost)
        {
            Debug.Log("[SceneTeleportButton] Chỉ Host được phép bắt đầu flow dungeon trong multiplayer.");
            return;
        }

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[SceneTeleportButton] Nút {gameObject.name} chưa nhập tên Scene!");
            return;
        }

        if (inviteFlowEnabled)
        {
            if (actionMode == MultiplayerActionMode.SelectDifficultyForInvite)
            {
                var flow = GetComponentInParent<DungeonGateUiFlow>();
                if (flow == null)
                {
                    var canvasRoot = GetComponentInParent<Canvas>();
                    if (canvasRoot != null)
                        flow = DungeonGateUiFlow.FindForCanvas(canvasRoot.gameObject);
                }

                if (flow != null)
                {
                    if (!string.IsNullOrEmpty(targetSceneName))
                        flow.targetSceneName = targetSceneName;
                    flow.mapType = mapType;
                    flow.ShowPreStartPanel(dungeonDifficulty);
                    Dbg($"SelectDifficultyForInvite → Pre-Start | {dungeonDifficulty}");
                }
                else
                    Debug.LogWarning($"[SceneTeleportButton] Không tìm thấy DungeonGateUiFlow cho nút {name}.", this);

                return;
            }

            if (actionMode == MultiplayerActionMode.InviteOnly)
            {
                string scene = !string.IsNullOrEmpty(targetSceneName)
                    ? targetSceneName
                    : DungeonPartyRuntime.PendingInviteSceneName;
                var diff = !string.IsNullOrEmpty(targetSceneName)
                    ? dungeonDifficulty
                    : DungeonPartyRuntime.PendingInviteDifficulty;
                int map = !string.IsNullOrEmpty(targetSceneName)
                    ? mapType
                    : DungeonPartyRuntime.PendingInviteMapType;

                Dbg(
                    $"InviteOnly click | isHost={isHost} hasRunner={hasRunner} scene='{scene}' diff={diff} map={map} " +
                    $"buttonTarget='{targetSceneName}' pendingScene='{DungeonPartyRuntime.PendingInviteSceneName}' pendingDiff={DungeonPartyRuntime.PendingInviteDifficulty}");

                if (isHost && localChar.TryHostRequestDungeonInvite(scene, diff, map, inviteDurationSeconds))
                {
                    Dbg($"Invite sent scene={scene} diff={diff} map={map}");
                    TryAdvanceQuest();
                    return;
                }
                Debug.LogWarning("[SceneTeleportButton] Invite thất bại. Chỉ host mới được gửi invite.");
                return;
            }

            if (actionMode == MultiplayerActionMode.StartWhenReady)
            {
                if (isHost && localChar.TryHostStartDungeonFromInvite())
                {
                    TryAdvanceQuest();
                    return;
                }
                Debug.LogWarning("[SceneTeleportButton] Chưa đủ điều kiện Start (chưa all-accept hoặc không phải host).");
                return;
            }

            // Multiplayer + DirectTeleport: ép theo flow all-accept.
            if (isHost && localChar.TryHostStartDungeonFromInvite())
            {
                TryAdvanceQuest();
                return;
            }
            Debug.LogWarning("[SceneTeleportButton] Multiplayer yêu cầu flow invite/start đồng thuận.");
            return;
        }
        else if (hasRunner && enableDebugLogs)
        {
            Dbg("Runner đang chạy nhưng chỉ có 1 player -> dùng flow single-player (không hiện Panel_Pre-Start).");
        }

        TryAdvanceQuest();

        // === OFFLINE: SET DUNGEON CONFIG TRƯỚC KHI CHUYỂN SCENE ===
        DungeonConfig.SelectedDifficulty = dungeonDifficulty;
        DungeonConfig.SelectedMapType = mapType;
        Debug.Log($"[SceneTeleportButton] DungeonConfig set: Difficulty={dungeonDifficulty}, MapType={mapType}");
        Debug.Log($"[SceneTeleportButton] Chuẩn bị chuyển đến: {targetSceneName}");
        Invoke(nameof(ExecuteTeleport), delay);
    }

    MultiplayerActionMode ResolveActionMode()
    {
        if (multiplayerActionMode != MultiplayerActionMode.Auto)
            return multiplayerActionMode;

        string n = gameObject.name.ToLowerInvariant();
        if (n.Contains("invite"))
            return MultiplayerActionMode.InviteOnly;
        if (n.Contains("start"))
            return MultiplayerActionMode.StartWhenReady;
        if (n.Contains("easy") || n.Contains("normal") || n.Contains("hard"))
            return MultiplayerActionMode.SelectDifficultyForInvite;
        return MultiplayerActionMode.DirectTeleport;
    }

    bool IsPassiveCloseButton()
    {
        string n = gameObject.name.ToLowerInvariant().Trim();
        return n.Contains("cancel") || n.Contains("cancle") || n.Contains("close") || n == "x";
    }

    static bool IsMultiplayerSession(Fusion.NetworkRunner runner)
    {
        if (runner == null || !runner.IsRunning)
            return false;

        int total = 0;
        foreach (var _ in runner.ActivePlayers)
            total++;
        return total > 1;
    }

    void Dbg(string msg)
    {
        if (!enableDebugLogs)
            return;
        Debug.Log($"[SceneTeleportButton] {msg}", this);
    }

    void TryAdvanceQuest()
    {
        if (questID <= 0 || QuestManager.Instance == null) return;
        var state = QuestManager.Instance.GetState(questID);
        int step  = QuestManager.Instance.GetStepIndex(questID);
        if (state == QuestManager.QuestState.Active && step == triggerAtStep)
        {
            QuestManager.Instance.AdvanceStep(questID);
            Debug.Log($"[SceneTeleportButton] Quest {questID} step {triggerAtStep} advanced.");
        }
    }

    private void ExecuteTeleport()
    {
        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Time.timeScale = 1f;
            CursorUIPriority.EndAllUiOverlays();

            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.GoToScene(targetSceneName, "Đang chuyển vùng...");
            else
                SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[SceneTeleportButton] LỖI: Scene '{targetSceneName}' không tồn tại hoặc chưa thêm vào Build Settings!");
        }
    }
}


