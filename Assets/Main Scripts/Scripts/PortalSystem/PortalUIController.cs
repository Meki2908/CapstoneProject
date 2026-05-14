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

    [Header("Teleport Quest Highlight")]
    [Tooltip("Kéo Sprite màu vàng vào đây. Sprite gốc của các nút sẽ tự động được phục hồi khi xong quest.")]
    public Sprite highlightedPortalSprite;

    private Image _img1, _img2, _img3, _img4;
    private Sprite _norm1, _norm2, _norm3, _norm4;

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

    [Header("Debug — Teleport (Crit-style)")]
    [Tooltip("Bật log [CritFSM][Teleport][PortalUI] trước/sau bước 3 và sau vài frame (bắt rollback). Bật thêm debugTeleportCritLogs trên Character prefab để thấy ApplyTeleportLocal.")]
    [SerializeField] bool debugPortalTeleportCritLogs = false;

    private Transform currentPlayer;
    private Character currentCharacter;

    void LogPortalTeleportCrit(string message)
    {
        if (!debugPortalTeleportCritLogs)
            return;
        Debug.Log($"[CritFSM][Teleport][PortalUI] t={Time.unscaledTime:F3} | {message}", this);
    }

    System.Collections.IEnumerator CoTeleportPostStepWatch(Character ch, Vector3 appliedDest, Transform playerTf)
    {
        if (!debugPortalTeleportCritLogs)
            yield break;

        string cmInfo = "cm=null";
        if (cinemachineCamera != null)
        {
            var f = cinemachineCamera.Follow;
            cmInfo = $"cm.follow={(f != null ? f.name : "null")} cm.pos={cinemachineCamera.transform.position}";
        }

        yield return null;
        if (ch != null)
            LogPortalTeleportCrit($"after1Update ch.pos={ch.transform.position} want={appliedDest} delta={(ch.transform.position - appliedDest).magnitude:F4} | {cmInfo}");
        if (playerTf != null)
            LogPortalTeleportCrit($"after1Update playerTf.pos={playerTf.position} | {cmInfo}");

        yield return new WaitForFixedUpdate();
        if (ch != null)
            LogPortalTeleportCrit($"afterFixed ch.pos={ch.transform.position} want={appliedDest} delta={(ch.transform.position - appliedDest).magnitude:F4} | {cmInfo}");
        if (playerTf != null)
            LogPortalTeleportCrit($"afterFixed playerTf.pos={playerTf.position} | {cmInfo}");

        for (int i = 0; i < 18; i++)
        {
            yield return null;
            if (i == 2 || i == 8 || i == 17)
            {
                if (cinemachineCamera != null)
                {
                    var f = cinemachineCamera.Follow;
                    cmInfo = $"cm.follow={(f != null ? f.name : "null")} f.pos={(f != null ? f.position.ToString() : "-")} cmCam.pos={cinemachineCamera.transform.position}";
                }
                if (ch != null)
                    LogPortalTeleportCrit($"late+{i + 2}Updates ch.delta={(ch.transform.position - appliedDest).magnitude:F3} ch.pos={ch.transform.position} | {cmInfo}");
            }
        }
    }

    private void Awake()
    {
        if (btnPortal1 != null) { btnPortal1.onClick.AddListener(() => TeleportTo(portal1_Dest, 1)); _img1 = btnPortal1.GetComponent<Image>(); if (_img1) _norm1 = _img1.sprite; }
        if (btnPortal2 != null) { btnPortal2.onClick.AddListener(() => TeleportTo(portal2_Dest, 2)); _img2 = btnPortal2.GetComponent<Image>(); if (_img2) _norm2 = _img2.sprite; }
        if (btnPortal3 != null) { btnPortal3.onClick.AddListener(() => TeleportTo(portal3_Dest, 3)); _img3 = btnPortal3.GetComponent<Image>(); if (_img3) _norm3 = _img3.sprite; }
        if (btnPortal4 != null) { btnPortal4.onClick.AddListener(() => TeleportTo(portal4_Dest, 4)); _img4 = btnPortal4.GetComponent<Image>(); if (_img4) _norm4 = _img4.sprite; }
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
        // Prefer the trigger source from local interact probe, then local character, then inspector override.
        currentPlayer = triggeredPlayer != null ? triggeredPlayer : playerOverride;
        currentCharacter = currentPlayer != null ? currentPlayer.GetComponentInParent<Character>() : null;
        if (currentCharacter == null && Character.Local != null)
        {
            currentCharacter = Character.Local;
            currentPlayer = currentCharacter.transform;
        }

        // Host/Client safety: this menu must operate on a character controlled by this peer.
        if (currentCharacter != null &&
            currentCharacter.Runner != null &&
            currentCharacter.Runner.IsRunning &&
            !currentCharacter.HasInputAuthority &&
            !currentCharacter.HasStateAuthority)
        {
            return;
        }

        // Nếu rootPanel có CursorUiOverlayWhenActive (vd. PortalRoundCanvas trên Canvas_MapChinh), OnEnable sẽ BeginUiOverlay.
        bool overlayOnPanel = rootPanel != null && rootPanel.GetComponent<CursorUiOverlayWhenActive>() != null;
        if (!overlayOnPanel)
        {
            CursorUIPriority.BeginUiOverlay();
            MouseLockManager.Instance?.ClearGameplayLockRetries();
            GameCursorManager.TryApplyNormalCursorTextureFromScene();
        }
        if (rootPanel != null) rootPanel.SetActive(true);
        SoundManager.PlayUIOpenMenu();
        
        HighlightQuestPortal();
    }

    private void HighlightQuestPortal()
    {
        // Phục hồi sprite gốc
        if (_img1 && _norm1) _img1.sprite = _norm1;
        if (_img2 && _norm2) _img2.sprite = _norm2;
        if (_img3 && _norm3) _img3.sprite = _norm3;
        if (_img4 && _norm4) _img4.sprite = _norm4;

        if (highlightedPortalSprite == null || QuestManager.Instance == null) return;

        var activeStep = QuestManager.Instance.GetActiveStep();
        if (activeStep != null && activeStep.suggestedPortalIndex > 0)
        {
            if (activeStep.suggestedPortalIndex == 1 && _img1) _img1.sprite = highlightedPortalSprite;
            else if (activeStep.suggestedPortalIndex == 2 && _img2) _img2.sprite = highlightedPortalSprite;
            else if (activeStep.suggestedPortalIndex == 3 && _img3) _img3.sprite = highlightedPortalSprite;
            else if (activeStep.suggestedPortalIndex == 4 && _img4) _img4.sprite = highlightedPortalSprite;
        }
    }

    public void ClosePortalMenu()
    {
        bool overlayOnPanel = rootPanel != null && rootPanel.GetComponent<CursorUiOverlayWhenActive>() != null;
        if (rootPanel != null) rootPanel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        if (!overlayOnPanel)
            CursorUIPriority.EndUiOverlay();
        SoundManager.PlayUICloseMenu();
    }

    /// <summary>Prefer the local input Character when Fusion is running (avoids stale inspector references).</summary>
    static bool TryResolveLocalPlayableCharacter(out Character ch)
    {
        if (Character.Local != null)
        {
            ch = Character.Local;
            return true;
        }

        ch = null;
        var players = FindObjectsByType<Character>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            var c = players[i];
            if (c == null) continue;
            if (c.Runner != null && c.Runner.IsRunning)
            {
                if (!c.HasInputAuthority && !c.HasStateAuthority) continue;
                ch = c;
                return true;
            }
        }

        for (int i = 0; i < players.Length; i++)
        {
            var c = players[i];
            if (c == null) continue;
            if (c.Runner == null || !c.Runner.IsRunning)
            {
                ch = c;
                return true;
            }
        }

        return false;
    }

    private void TeleportTo(Transform destination, int portalIndex = 0)
    {
        _lastPortalIndex = portalIndex;

        // Fallback: resolve local playable Character first (online), then optional override, then any Character offline.
        if (currentPlayer == null)
        {
            if (TryResolveLocalPlayableCharacter(out var localCh))
            {
                currentCharacter = localCh;
                currentPlayer = localCh.transform;
            }
            else if (playerOverride != null)
            {
                currentPlayer = playerOverride;
            }
            else
            {
                var players = FindObjectsByType<Character>(FindObjectsSortMode.None);
                for (int i = 0; i < players.Length; i++)
                {
                    var ch = players[i];
                    if (ch == null) continue;
                    if (ch.Runner != null && ch.Runner.IsRunning)
                    {
                        if (!ch.HasInputAuthority && !ch.HasStateAuthority) continue;
                    }
                    currentCharacter = ch;
                    currentPlayer = ch.transform;
                    break;
                }
            }
        }
        else if (currentCharacter == null)
        {
            currentCharacter = currentPlayer.GetComponentInParent<Character>();
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

        // Tìm CharacterController (host thường là SA-only; sync currentCharacter sau khi gắn đúng transform)
        CharacterController cc = currentPlayer.GetComponent<CharacterController>();
        if (cc == null) cc = currentPlayer.GetComponentInChildren<CharacterController>();
        if (cc == null) cc = currentPlayer.GetComponentInParent<CharacterController>();

        if (cc != null && cc.transform != currentPlayer)
            currentPlayer = cc.transform;

        currentCharacter = currentPlayer != null ? currentPlayer.GetComponentInParent<Character>() : null;

        if (currentCharacter != null &&
            currentCharacter.Runner != null &&
            currentCharacter.Runner.IsRunning &&
            !currentCharacter.HasInputAuthority &&
            !currentCharacter.HasStateAuthority)
        {
            Debug.LogWarning("[Portal] Từ chối teleport vì player hiện tại không có quyền điều khiển trên peer này.");
            return;
        }

        Debug.Log($"[Portal] Dịch chuyển {currentPlayer.name} → {destination.name} tại {destination.position}");

        ClosePortalMenu();
        StartCoroutine(TeleportSequence(destination));
    }

    /// <summary>
    /// Trình tự teleport:
    /// 1. Spawn VFX tại vị trí player (điểm xuất phát)
    /// 2. Camera pan mượt đến điểm đích (dùng dummy Follow target)
    /// 3. Dịch chuyển player
    /// 4. Spawn VFX tại điểm đích
    /// 5. Advance quest
    /// </summary>
    private IEnumerator TeleportSequence(Transform destination)
    {
        // ── Bước 1: Spawn VFX + SFX tại điểm xuất phát ──
        SoundManager.PlayTeleportStart();
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
                    elapsed += Time.unscaledDeltaTime;
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
        if (currentCharacter != null)
        {
            LogPortalTeleportCrit(
                $"step3 BEFORE RequestTeleport currentPlayer={currentPlayer?.name} pos={currentPlayer?.position} ch={currentCharacter.name} ch.pos={currentCharacter.transform.position} dest={destination.position}");
            currentCharacter.LogTeleportCrit(
                $"Portal calling RequestTeleport dest={destination.position} (portal UI step3)");
            currentCharacter.RequestTeleportToWorldPosition(destination.position);
            currentPlayer = currentCharacter.transform;
            currentPlayer.rotation = destination.rotation;
            LogPortalTeleportCrit(
                $"step3 AFTER RequestTeleport currentPlayer={currentPlayer.name} pos={currentPlayer.position}");
            currentCharacter.LogTeleportCrit(
                $"Portal after RequestTeleport ch.pos={currentCharacter.transform.position}");
            if (debugPortalTeleportCritLogs)
                StartCoroutine(CoTeleportPostStepWatch(currentCharacter, destination.position, currentPlayer));
        }
        else if (currentPlayer != null)
        {
            LogPortalTeleportCrit(
                $"step3 FALLBACK no Character; playerTf={currentPlayer.name} pos={currentPlayer.position} dest={destination.position}");
            // Fallback for non-networked objects without Character.
            var cc = currentPlayer.GetComponent<CharacterController>();
            if (cc == null) cc = currentPlayer.GetComponentInChildren<CharacterController>();
            if (cc == null) cc = currentPlayer.GetComponentInParent<CharacterController>();
            if (cc != null) cc.enabled = false;
            currentPlayer.position = destination.position;
            currentPlayer.rotation = destination.rotation;
            Physics.SyncTransforms();
            if (cc != null) cc.enabled = true;
            LogPortalTeleportCrit($"step3 FALLBACK done playerTf.pos={currentPlayer.position}");
        }

        // ── Bước 4: Spawn VFX + SFX tại điểm đích ──
        SoundManager.PlayTeleportArrive();
        SpawnEffect(destination.position, destination.rotation);

        // ── Bước 5: Reset tương tác cổng CHỈ cho local player hiện tại ──────────────────────────────
        // Tránh ảnh hưởng peer khác đang đứng gần cổng.
        foreach (var node in FindObjectsByType<PortalNode>(FindObjectsSortMode.None))
            node.ExternalClearLocalCharacter(currentCharacter);
        foreach (var gate in FindObjectsByType<GateTeleporter>(FindObjectsSortMode.None))
            gate.ExternalClearLocalCharacter(currentCharacter);

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

            // Bảo vệ tuyểt đối: Quest 4 KHÔNG CÓ bước Use Portal (bước 1 của chúng đều là Talk to Maria).
            // Nếu UI Portal cố tình advance thì sẽ vô tình skip qua việc nói chuyện với Maria!
            if (qa.questID == 4) 
            {
                continue;
            }

            var state = QuestManager.Instance.GetState(qa.questID);
            int step  = QuestManager.Instance.GetStepIndex(qa.questID);

            // Kiểm tra xem quest có đang ở đúng bước cần kích hoạt bởi cổng không
            if (state == QuestManager.QuestState.Active && step == qa.triggerAtStep)
            {
                QuestManager.Instance.AdvanceStep(qa.questID);
                Debug.Log($"[Portal] Quest {qa.questID}: advanced 1 step from step {step} (portal {_lastPortalIndex}).");
            }
        }
    }
}
