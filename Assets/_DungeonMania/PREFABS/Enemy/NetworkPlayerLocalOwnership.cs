using System;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using MovementSystem;

/// <summary>
/// Chỉ client sở hữu (input authority) chạy input, CharacterController, Character và CinemachineCamera.
/// Bản remote trên cùng máy không tranh Camera.main / Brain và không nhận cùng bộ phím chuột.
///
/// Khi player chết (NetworkPlayerDeathManager), tắt input nhưng GIỮ BODY VISIBLE để spectator thấy.
/// </summary>
[DefaultExecutionOrder(-150)]
public class NetworkPlayerLocalOwnership : NetworkBehaviour
{
    private NetworkPlayerDeathManager _deathManager;
    private bool _lastSpectating;

    private void Start()
    {
        // Chạy fallback nếu chạy trực tiếp Scene không qua NetworkRunner (chế độ chơi Offline/Single-player)
        var runner = FindAnyObjectByType<NetworkRunner>();
        if (runner == null)
        {
            Debug.Log("[LocalOwnership] Không tìm thấy NetworkRunner (Offline Mode). Tự động kích hoạt Local Ownership.");
            ApplyLocalOwnership(true, false);
        }
    }

    private bool IsOfflineOrSinglePlayer()
    {
        var runner = FindAnyObjectByType<NetworkRunner>();
        return runner == null || runner.GameMode == GameMode.Single;
    }

    public override void Spawned()
    {
        _deathManager = GetComponent<NetworkPlayerDeathManager>();

        // Kiểm tra NetworkObject hợp lệ trước khi dùng HasInputAuthority
        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning($"[LocalOwnership] Object not valid on Spawned! name='{gameObject.name}'");
            return;
        }

        Debug.Log($"[LocalOwnership] Spawned — HasInputAuthority={HasInputAuthority}, HasStateAuthority={HasStateAuthority}, name='{gameObject.name}'");

        ApplyLocalOwnership(HasInputAuthority, false);
        _lastSpectating = NetIsSpectating_GetSafe();
    }

    /// <summary>Lấy NetIsSpectating an toàn, không throw exception.</summary>
    private bool NetIsSpectating_GetSafe()
    {
        if (_deathManager == null || _deathManager.Object == null || !_deathManager.Object.IsValid)
            return false;
        try { return _deathManager.NetIsSpectating; }
        catch (InvalidOperationException) { return false; }
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        if (_deathManager == null || _deathManager.Object == null || !_deathManager.Object.IsValid) return;

        bool spectating = NetIsSpectating_GetSafe();
        if (spectating != _lastSpectating)
        {
            _lastSpectating = spectating;
            ApplyLocalOwnership(!spectating, true);
        }
    }

    void ApplyLocalOwnership(bool local, bool isDead)
    {
        foreach (var pi in GetComponentsInChildren<PlayerInput>(true))
        {
            pi.enabled = local;
        }

        foreach (var cc in GetComponentsInChildren<CharacterController>(true))
            cc.enabled = local;

        foreach (var ch in GetComponentsInChildren<Character>(true))
            ch.enabled = local;

        foreach (var cm in GetComponentsInChildren<CinemachineCamera>(true))
        {
            if (local) cm.gameObject.SetActive(true);
            cm.enabled = local;
        }

        if (local)
        {
            // ═══════════════════════════════════════════════════════════════
            // NGUYÊN NHÂN GỐC: Fusion Player Prefab KHÔNG chứa Main Camera 
            // (chỉ có CinemachineCamera ảo như PlayerCamera, HandFollowCamera).
            // Scene Map_Chinh có Main Camera nhưng KHÔNG gắn CinemachineBrain.
            // → CinemachineCamera trên Player không có Brain nào lắng nghe
            // → Camera đứng yên 1 chỗ mãi mãi.
            //
            // Giải pháp: Tìm Camera.main của SCENE, gắn CinemachineBrain
            // vào nó nếu thiếu → Cinemachine điều khiển được Camera ngay.
            // ═══════════════════════════════════════════════════════════════
            
            // 1. Tìm Main Camera — ưu tiên Camera.main (scene camera)
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                // Fallback: tìm trong children của Player (trường hợp prefab gốc có sẵn Main Camera)
                var playerCams = GetComponentsInChildren<Camera>(true);
                foreach (var c in playerCams)
                {
                    if (c.CompareTag("MainCamera"))
                    {
                        mainCam = c;
                        break;
                    }
                }
                if (mainCam == null && playerCams.Length > 0) mainCam = playerCams[0];
            }

            if (mainCam != null)
            {
                // 2. ★ GẮN CinemachineBrain nếu Main Camera chưa có! ★
                //    Đây là nguyên nhân chính khiến Camera đứng yên 1 chỗ.
                var brain = mainCam.GetComponent<Unity.Cinemachine.CinemachineBrain>();
                if (brain == null)
                {
                    brain = mainCam.gameObject.AddComponent<Unity.Cinemachine.CinemachineBrain>();
                    Debug.Log($"[LocalOwnership] ★ Đã gắn CinemachineBrain vào '{mainCam.name}' — Camera sẽ follow Player!");
                }
                brain.enabled = true;
                mainCam.enabled = true;
                mainCam.gameObject.SetActive(true);

                // 3. Multiplayer: tháo Camera khỏi Player để làm Spectator khi chết
                //    Offline/Single: giữ nguyên
                if (!IsOfflineOrSinglePlayer() && mainCam.transform.IsChildOf(this.transform))
                {
                    mainCam.transform.SetParent(null);
                    mainCam.gameObject.name = "Global_MainCamera_Observer";
                }
            }
            else
            {
                Debug.LogError("[LocalOwnership] KHÔNG tìm thấy Main Camera nào trong Scene lẫn Player Prefab!");
            }

            // Lobby đang mở: không gọi EndAllUiOverlays (sẽ xóa BeginUiOverlay của NetworkLobbyManager)
            // và không khóa chuột — để bấm START / Alt hoạt động.
            // NetworkLobbyManager Spawned (order 0) chạy sau script này (-150): depth có thể vẫn 0
            // trong cùng frame — kiểm tra Instance + GameStarted để khớp với EnsureLobbyUiOverlayStack.
            var lobby = NetworkLobbyManager.Instance;
            if (CursorUIPriority.IsUiOverlayActive ||
                (lobby != null && lobby.IsLobbyBlockingCursor))
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                CursorUIPriority.EndAllUiOverlays();
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                CameraCursor.ApplyGameplayCursorAfterUiClosed();
            }
        }
        else
        {
            // Là Clone máy của khách -> Phá hủy XÓA SẠCH cụm Camera (và AudioListener) trên xác nó
            var theirCams = GetComponentsInChildren<Camera>(true);
            foreach (var c in theirCams)
            {
                // Xóa Component thay vì GameObject để an toàn chống sập Prefab
                Destroy(c);
            }

            var theirAudios = GetComponentsInChildren<AudioListener>(true);
            foreach (var a in theirAudios)
            {
                Destroy(a);
            }

            if (isDead)
            {
                // Player đã chết → spectate mode
                foreach (var pi in GetComponentsInChildren<PlayerInput>(true))
                    pi.enabled = false;

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                Debug.Log("[LocalOwnership] Player dead — spectate mode, input disabled");
            }
        }

        Debug.Log($"[LocalOwnership] ApplyLocalOwnership: local={local}, isDead={isDead}, name='{gameObject.name}'");
    }
}
