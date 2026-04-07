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

    [Header("── Dungeon party lobby (multiplayer) ──")]
    [Tooltip("Để trống: teleport ngay như cũ. Gán DungeonPortalLobbyCoordinator trên cùng Dungeon Canvas để mở bước chọn số người + preparation.")]
    public DungeonPortalLobbyCoordinator dungeonLobbyCoordinator;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnBtnClick);
    }

    private void OnBtnClick()
    {
        if (dungeonLobbyCoordinator == null)
            dungeonLobbyCoordinator = GetComponentInParent<DungeonPortalLobbyCoordinator>(true);

        TryAdvanceQuest();

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning($"[SceneTeleportButton] Nút {gameObject.name} chưa nhập tên Scene!");
            return;
        }

        // === SET DUNGEON CONFIG TRƯỚC KHI CHUYỂN SCENE ===
        DungeonConfig.SelectedDifficulty = dungeonDifficulty;
        DungeonConfig.SelectedMapType = mapType;
        Debug.Log($"[SceneTeleportButton] DungeonConfig set: Difficulty={dungeonDifficulty}, MapType={mapType}");

        if (dungeonLobbyCoordinator != null)
        {
            Debug.Log($"[SceneTeleportButton] Mở flow lobby dungeon → {targetSceneName}");
            dungeonLobbyCoordinator.OnDifficultySelected(this);
            return;
        }

        Debug.Log($"[SceneTeleportButton] Chuẩn bị chuyển đến: {targetSceneName}");
        Invoke(nameof(ExecuteTeleport), delay);
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


