using System.Collections;
using Unity.Cinemachine;
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

    [Header("UI Buttons (Kéo 4 nút vào đây)")]
    public Button btnPortal1;
    public Button btnPortal2;
    public Button btnPortal3;
    public Button btnClose;

    [Header("── Quest Advance (tuỳ chọn) ──")]
    [Tooltip("Set questID > 0 để tự advance step khi player dùng portal.")]
    public int questID           = 0;
    public int triggerAtStep     = 1;
    [Tooltip("Số bước cần advance khi tele (mặc định 1). Set 2 để advance 2 bước liên tiếp.")]
    public int advanceSteps      = 1;
    [Tooltip("Portal nào thì advance quest: 1=btnPortal1, 2=btnPortal2, 3=btnPortal3, 0=bất kỳ")]
    public int questPortalButton = 1;

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

        // ── Bước 5: Advance quest ──
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
        if (questID <= 0 || QuestManager.Instance == null) return;
        if (questPortalButton != 0 && _lastPortalIndex != questPortalButton) return;

        var state   = QuestManager.Instance.GetState(questID);
        int step    = QuestManager.Instance.GetStepIndex(questID);
        int maxStep = triggerAtStep + advanceSteps - 1;

        if (state != QuestManager.QuestState.Active || step < triggerAtStep || step > maxStep) return;

        int stepsLeft = maxStep - step + 1;
        for (int i = 0; i < stepsLeft; i++)
        {
            if (QuestManager.Instance.GetState(questID) != QuestManager.QuestState.Active) break;
            QuestManager.Instance.AdvanceStep(questID);
        }
        Debug.Log($"[Portal] Quest {questID}: advanced {stepsLeft} step(s) from step {step} (portal {_lastPortalIndex}).");
    }
}
