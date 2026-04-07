using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using MovementSystem;

/// <summary>
/// Chỉ client sở hữu (owner) chạy input, CharacterController, Character và CinemachineCamera.
/// Bản remote trên cùng máy không tranh Camera.main / Brain và không nhận cùng bộ phím chuột.
/// </summary>
[DefaultExecutionOrder(-150)]
public class NetworkPlayerLocalOwnership : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        ApplyLocalOwnership(IsOwner);
    }

    void Start()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            return;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return;
        ApplyLocalOwnership(true);
    }

    void ApplyLocalOwnership(bool local)
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
    }
}
