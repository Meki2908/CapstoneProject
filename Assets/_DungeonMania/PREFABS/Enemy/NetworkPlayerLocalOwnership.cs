using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using MovementSystem;

/// <summary>
/// Bật input / Character / camera cho instance local (Fusion authority sẽ bổ sung sau).
/// </summary>
[DefaultExecutionOrder(-150)]
public class NetworkPlayerLocalOwnership : MonoBehaviour
{
    void Start()
    {
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
