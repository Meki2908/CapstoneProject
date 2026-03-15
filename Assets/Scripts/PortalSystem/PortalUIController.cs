using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PortalUIController : MonoBehaviour
{
    [Header("Portal Destinations (Kéo SpawnPoint của 3 cổng vào đây)")]
    // Bạn nên tạo một Empty GameObject đặt nhích ra phía trước mặt của chiếc cổng
    // và dùng GameObject đó làm Transform đích để người chơi không bị teleport đè vào lưới cổng 
    public Transform portal1_Dest;
    public Transform portal2_Dest;
    public Transform portal3_Dest;
    public Transform portal4_Dest;

    [Header("UI Buttons (Kéo 4 nút vào đây)")]
    public Button btnPortal1;
    public Button btnPortal2;
    public Button btnPortal3;
    public Button btnPortal4;
    public Button btnClose;

    [System.Serializable]
    public struct PortalQuestConfig
    {
        [Tooltip("0 = không advance quest")]
        public int questID;
        public int triggerAtStep;
        [Tooltip("Số bước advance (thường là 2 để skip step intermediate)")]
        public int advanceSteps;
    }

    [Header("── Quest Advance – per portal button ──")]
    [Tooltip("Index 0=btnPortal1, 1=btnPortal2, 2=btnPortal3, 3=btnPortal4")]
    public PortalQuestConfig[] portalQuestConfigs = new PortalQuestConfig[4];

    int _lastPortalIndex = 0;

    [Header("Player Reference")]
    [Tooltip("Kéo thả Player của bạn vào đây nếu bạn muốn gán cứng, nếu không script sẽ tự tìm qua Tag 'Player'")]
    public Transform playerOverride;

    [Header("Root Panel (panel con bên trong Canvas)")]
    [Tooltip("Kéo Panel con vào đây. Canvas luôn bật, chỉ panel con được ẩn/hiện.")]
    public GameObject rootPanel;

    private Transform currentPlayer;

    private void Awake()
    {
        if (btnPortal1 != null) btnPortal1.onClick.AddListener(() => TeleportTo(portal1_Dest, 1));
        if (btnPortal2 != null) btnPortal2.onClick.AddListener(() => TeleportTo(portal2_Dest, 2));
        if (btnPortal3 != null) btnPortal3.onClick.AddListener(() => TeleportTo(portal3_Dest, 3));
        if (btnPortal4 != null) btnPortal4.onClick.AddListener(() => TeleportTo(portal4_Dest, 4));
        if (btnClose   != null) btnClose.onClick.AddListener(ClosePortalMenu);
    }

    private void Start()
    {
        // Ẩn panel con khi bắt đầu — Canvas vẫn bật để Awake/Start chạy đúng
        if (rootPanel != null) rootPanel.SetActive(false);
        else Debug.LogError("[PortalUI] rootPanel chưa được gán! Kéo Panel con vào Inspector.");

        if (UnityEngine.EventSystems.EventSystem.current == null)
            Debug.LogError("[PortalUI] KHÔNG TÌM THẤY 'Event System' trong Scene!");

        if (btnPortal1 == null) Debug.LogWarning("[PortalUI] Btn Portal 1 chưa gán!");
        if (btnPortal2 == null) Debug.LogWarning("[PortalUI] Btn Portal 2 chưa gán!");
        if (btnPortal3 == null) Debug.LogWarning("[PortalUI] Btn Portal 3 chưa gán!");
        if (btnClose   == null) Debug.LogWarning("[PortalUI] Btn Close chưa gán!");
    }

    public void OpenPortalMenu(Transform triggeredPlayer)
    {
        currentPlayer = (playerOverride != null) ? playerOverride : triggeredPlayer;
        if (rootPanel != null) rootPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ClosePortalMenu()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void TeleportTo(Transform destination, int portalIndex = 0)
    {
        _lastPortalIndex = portalIndex;
        // Fallback cuối cùng: Nếu vẫn chưa có Player, thử tìm bằng Tag
        if (currentPlayer == null)
        {
            if (playerOverride != null) currentPlayer = playerOverride;
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) currentPlayer = p.transform;
            }
        }

        if (currentPlayer == null)
        {
            Debug.LogError("[Portal] KHÔNG TÌM THẤY PLAYER! Hãy kéo Player vào ô 'Player Override' trong Inspector của PortalUIController.");
            return;
        }

        if (destination == null)
        {
            Debug.LogError("[Portal] KHÔNG CÓ ĐÍCH ĐẾN! Kiểm tra xem bạn đã kéo đích đến vào script ở Inspector chưa.");
            return;
        }

        Debug.Log($"[Portal] Đang dịch chuyển Player {currentPlayer.name} từ {currentPlayer.position} đến {destination.name} tại {destination.position}...");

        // Tìm CharacterController trên currentPlayer hoặc các con/cha của nó
        CharacterController cc = currentPlayer.GetComponent<CharacterController>();
        if (cc == null) cc = currentPlayer.GetComponentInChildren<CharacterController>();
        if (cc == null) cc = currentPlayer.GetComponentInParent<CharacterController>();

        // Nếu tìm thấy CC ở cha/con, cập nhật currentPlayer thành object đó để dịch chuyển đúng object gốc
        if (cc != null && cc.transform != currentPlayer)
        {
            Debug.Log($"[Portal] Tìm thấy CharacterController trên {cc.name}, chuyển mục tiêu dịch chuyển sang object này.");
            currentPlayer = cc.transform;
        }

        // Vô hiệu hóa CharacterController trước khi thay đổi Position
        if (cc != null) cc.enabled = false;

        // Dịch chuyển
        currentPlayer.position = destination.position;
        currentPlayer.rotation = destination.rotation;

        // Đảm bảo Unity cập nhật vị trí vật lý ngay lập tức
        Physics.SyncTransforms();

        // Bật lại CharacterController
        if (cc != null) cc.enabled = true;

        ClosePortalMenu();
        TryAdvanceQuest();
        Debug.Log($"[Portal] Dịch chuyển hoàn tất. Vị trí hiện tại: {currentPlayer.position}");
    }

    void TryAdvanceQuest()
    {
        if (QuestManager.Instance == null) return;

        // Per-portal config: index = _lastPortalIndex - 1
        int idx = _lastPortalIndex - 1;
        if (idx >= 0 && idx < portalQuestConfigs.Length)
        {
            var cfg = portalQuestConfigs[idx];
            if (cfg.questID > 0)
            {
                AdvanceQuestConfig(cfg.questID, cfg.triggerAtStep,
                    cfg.advanceSteps > 0 ? cfg.advanceSteps : 1);
                return;
            }
        }
        // No config for this portal — skip
        Debug.Log($"[Portal] No quest config for portal button {_lastPortalIndex}.");
    }

    void AdvanceQuestConfig(int qID, int triggerStep, int steps)
    {
        var state = QuestManager.Instance.GetState(qID);
        int step  = QuestManager.Instance.GetStepIndex(qID);
        int maxStep = triggerStep + steps - 1;

        if (state != QuestManager.QuestState.Active || step < triggerStep || step > maxStep) return;

        int stepsLeft = maxStep - step + 1;
        for (int i = 0; i < stepsLeft; i++)
        {
            if (QuestManager.Instance.GetState(qID) != QuestManager.QuestState.Active) break;
            QuestManager.Instance.AdvanceStep(qID);
        }
        Debug.Log($"[Portal] Quest {qID}: advanced {stepsLeft} step(s) from step {step} (portal btn {_lastPortalIndex}).");
    }
}
