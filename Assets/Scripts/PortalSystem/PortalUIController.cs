using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PortalUIController : MonoBehaviour
{
    [Header("Portal Destinations (Kéo SpawnPoint của các cổng vào đây)")]
    // Bạn nên tạo một Empty GameObject đặt nhích ra phía trước mặt của chiếc cổng
    // và dùng GameObject đó làm Transform đích để người chơi không bị teleport đè vào lưới cổng 
    public Transform portal1_Dest;
    public Transform portal2_Dest;
    public Transform portal3_Dest;
    public Transform portal4_Dest;

    [Header("UI Buttons")]
    public Button btnPortal1;
    public Button btnPortal2;
    public Button btnPortal3;
    public Button btnPortal4;
    public Button btnClose;

    [Header("── Quest Advance (mỗi button có thể advance một quest riêng) ──")]
    public PortalQuestAdvance[] questAdvances;

    [System.Serializable]
    public class PortalQuestAdvance
    {
        [Tooltip("Portal button nào trigger: 1=btn1, 2=btn2, 3=btn3, 4=btn4")]
        public int portalButton  = 1;
        public int questID       = 0;
        [Tooltip("Chỉ advance nếu step hiện tại đúng bằng giá trị này")]
        public int triggerAtStep = 0;
        [Tooltip("Số bước advance (mặc định 1)")]
        public int advanceSteps  = 1;
    }

    int _lastPortalIndex = 0;

    [Header("Player Reference")]
    [Tooltip("Kéo thả Player của bạn vào đây nếu bạn muốn gán cứng, nếu không script sẽ tự tìm qua Tag 'Player'")]
    public Transform playerOverride;

    [Header("Root Panel (panel con bên trong Canvas)")]
    [Tooltip("Kéo Panel con vào đây. Canvas luôn bật, chỉ panel con được ẩn/hiện.")]
    public GameObject rootPanel;

    [Header("Teleport VFX")]
    [Tooltip("Kéo prefab hiệu ứng Teleport vào đây (ví dụ: Teleport.prefab)")]
    public GameObject teleportEffectPrefab;
    [Tooltip("Thời gian (giây) trước khi tự Destroy VFX sau khi spawn")]
    public float effectDuration = 2f;

    [Header("Camera Pan")]
    [Tooltip("Kéo Cinemachine Virtual Camera (đang follow player) vào đây")]
    public CinemachineCamera cinemachineCamera;
    [Tooltip("Thời gian (giây) camera di chuyển từ điểm nguồn đến điểm đích")]
    public float cameraPanDuration = 1.2f;

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

        // Fallback: Nếu vẫn chưa có Player, thử tìm bằng Tag
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
            Debug.LogError("[Portal] KHÔNG TÌM THẤY PLAYER!");
            return;
        }

        if (destination == null)
        {
            Debug.LogError("[Portal] KHÔNG CÓ ĐÍCH ĐẾN!");
            return;
        }

        Debug.Log($"[Portal] Dịch chuyển {currentPlayer.name} → {destination.name} tại {destination.position}");

        // Tìm CharacterController
        CharacterController cc = currentPlayer.GetComponent<CharacterController>();
        if (cc == null) cc = currentPlayer.GetComponentInChildren<CharacterController>();
        if (cc == null) cc = currentPlayer.GetComponentInParent<CharacterController>();

        if (cc != null && cc.transform != currentPlayer)
            currentPlayer = cc.transform;

        ClosePortalMenu();
        StartCoroutine(TeleportSequence(destination, cc));
    }

    /// <summary>
    /// Trình tự teleport:
    /// 1. Spawn VFX tại vị trí player (điểm xuất phát)
    /// 2. Camera pan mượt đến điểm đích (dùng dummy Follow target)
    /// 3. Dịch chuyển player
    /// 4. Spawn VFX tại điểm đích
    /// 5. Advance quest
    /// </summary>
    private IEnumerator TeleportSequence(Transform destination, CharacterController cc)
    {
        // ── Bước 1: Spawn VFX tại điểm xuất phát ──
        SpawnEffect(currentPlayer.position, currentPlayer.rotation);

        // ── Bước 2: Camera pan đến điểm đích ──
        if (cinemachineCamera != null && cameraPanDuration > 0f)
        {
            Transform originalFollow = cinemachineCamera.Follow;

            if (originalFollow != null)
            {
                // Tạo dummy GameObject làm Follow target tạm để camera pan mà không kéo player
                GameObject dummy = new GameObject("_CameraPanDummy");
                dummy.transform.position = originalFollow.position;

                cinemachineCamera.Follow = dummy.transform;

                Vector3 startPos = dummy.transform.position;
                Vector3 endPos   = destination.position;
                float elapsed = 0f;

                while (elapsed < cameraPanDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / cameraPanDuration));
                    dummy.transform.position = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }

                // Trả camera về follow player, xóa dummy
                cinemachineCamera.Follow = originalFollow;
                Destroy(dummy);
            }
            else
            {
                yield return null;
            }
        }
        else
        {
            yield return null;
        }

        // ── Bước 3: Dịch chuyển player ──
        if (cc != null) cc.enabled = false;
        currentPlayer.position = destination.position;
        currentPlayer.rotation = destination.rotation;
        Physics.SyncTransforms();
        if (cc != null) cc.enabled = true;

        // ── Bước 4: Spawn VFX tại điểm đích ──
        SpawnEffect(destination.position, destination.rotation);

        // ── Bước 5: Reset tất cả PortalNode ──────────────────────────────
        // CharacterController.enabled = false/true + warp làm Unity không gửi
        // OnTriggerExit cho portal cũ → _playerInRange bị giữ true → nhấn F
        // vẫn mở portal UI. ForceExit() trên tất cả nodes giải quyết triệt để.
        foreach (var node in FindObjectsByType<PortalNode>(FindObjectsSortMode.None))
            node.ForceExit();

        // ── Bước 6: Advance quest ──
        TryAdvanceQuest();
        Debug.Log($"[Portal] Dịch chuyển hoàn tất. Vị trí: {currentPlayer.position}");
    }

    /// <summary>Spawn hiệu ứng teleport và tự Destroy sau effectDuration giây.</summary>
    private void SpawnEffect(Vector3 position, Quaternion rotation)
    {
        if (teleportEffectPrefab == null) return;
        GameObject vfx = Instantiate(teleportEffectPrefab, position, rotation);
        Destroy(vfx, effectDuration);
    }

    void TryAdvanceQuest()
    {
        if (QuestManager.Instance == null) return;
        if (questAdvances == null) return;

        foreach (var qa in questAdvances)
        {
            if (qa.questID <= 0) continue;
            if (qa.portalButton != 0 && _lastPortalIndex != qa.portalButton) continue;

            var state = QuestManager.Instance.GetState(qa.questID);
            int step  = QuestManager.Instance.GetStepIndex(qa.questID);
            int maxStep = qa.triggerAtStep + qa.advanceSteps - 1;

            if (state != QuestManager.QuestState.Active || step < qa.triggerAtStep || step > maxStep) continue;

            int stepsLeft = maxStep - step + 1;
            for (int i = 0; i < stepsLeft; i++)
            {
                if (QuestManager.Instance.GetState(qa.questID) != QuestManager.QuestState.Active) break;
                QuestManager.Instance.AdvanceStep(qa.questID);
            }
            Debug.Log($"[Portal] Quest {qa.questID}: advanced {stepsLeft} step(s) from step {step} (portal {_lastPortalIndex}).");
        }
    }
}
