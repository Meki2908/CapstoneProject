using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút thoát kẹt — Teleport player đến vị trí đã chọn sẵn.
/// Setup:
///   1. Kéo script này vào Button GameObject
///   2. Kéo Transform đích vào "Destination"
///   3. Button OnClick() → kéo GameObject này → chọn Teleport()
/// </summary>
public class UnstuckTeleport : MonoBehaviour
{
    [Header("Vị trí teleport đến")]
    [Tooltip("Kéo một Empty GameObject (điểm an toàn) vào đây")]
    public Transform destination;

    [Header("Player (tự tìm nếu để trống)")]
    [Tooltip("Kéo Player vào đây, hoặc để trống sẽ tự tìm bằng tag 'Player'")]
    public Transform playerOverride;

    [Header("Hiệu ứng (optional)")]
    public GameObject teleportVFXPrefab;
    public float vfxDuration = 2f;

    /// <summary>
    /// Gọi từ Button OnClick() hoặc từ code bất kỳ.
    /// </summary>
    public void Teleport()
    {
        if (destination == null)
        {
            Debug.LogError("[UnstuckTeleport] Chưa gán Destination! Kéo Transform đích vào Inspector.");
            return;
        }

        // Tìm player
        Transform player = playerOverride;
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player == null)
        {
            Debug.LogError("[UnstuckTeleport] Không tìm thấy Player!");
            return;
        }

        // Tìm CharacterController (cần disable trước khi warp)
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.GetComponentInChildren<CharacterController>();
        if (cc == null) cc = player.GetComponentInParent<CharacterController>();

        // Nếu CC nằm trên object khác, dùng object đó làm target warp
        if (cc != null && cc.transform != player)
            player = cc.transform;

        // Spawn VFX tại vị trí cũ
        if (teleportVFXPrefab != null)
        {
            GameObject vfx = Instantiate(teleportVFXPrefab, player.position, player.rotation);
            Destroy(vfx, vfxDuration);
        }

        // Teleport
        if (cc != null) cc.enabled = false;
        player.position = destination.position;
        player.rotation = destination.rotation;
        Physics.SyncTransforms();
        if (cc != null) cc.enabled = true;

        // Spawn VFX tại vị trí mới
        if (teleportVFXPrefab != null)
        {
            GameObject vfx = Instantiate(teleportVFXPrefab, destination.position, destination.rotation);
            Destroy(vfx, vfxDuration);
        }

        Debug.Log($"[UnstuckTeleport] Đã teleport {player.name} → {destination.name} ({destination.position})");
    }
}
