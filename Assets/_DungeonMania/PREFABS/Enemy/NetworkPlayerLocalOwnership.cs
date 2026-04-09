using System;
using Fusion;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
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
            pi.enabled = local;

        foreach (var cc in GetComponentsInChildren<CharacterController>(true))
            cc.enabled = local;

        foreach (var ch in GetComponentsInChildren<Character>(true))
            ch.enabled = local;

        foreach (var cm in GetComponentsInChildren<CinemachineCamera>(true))
            cm.enabled = local;

        if (local)
        {
            CursorUIPriority.EndAllUiOverlays();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            CameraCursor.ApplyGameplayCursorAfterUiClosed();
        }
        else if (isDead)
        {
            // Player đã chết → spectate mode
            foreach (var pi in GetComponentsInChildren<PlayerInput>(true))
                pi.enabled = false;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Debug.Log("[LocalOwnership] Player dead — spectate mode, input disabled");
        }

        Debug.Log($"[LocalOwnership] ApplyLocalOwnership: local={local}, isDead={isDead}, name='{gameObject.name}'");
    }
}
