using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gate teleporter giữa 2 điểm A ↔ B.
/// Tự động phát hiện player đến từ phía nào và tele sang phía kia.
/// </summary>
public class GateTeleporter : MonoBehaviour
{
    [Header("── Hai đầu cổng ──")]
    public Transform pointA;          // Đích khi player đứng phía B
    public Transform pointB;          // Đích khi player đứng phía A

    [Header("── UI ──")]
    public GameObject pressFCanvas;

    [Header("── Settings ──")]
    public float teleportCooldown = 0.5f;

    // ─── Runtime ──────────────────────────────────────────────────────────
    Transform _player;
    bool      _playerInRange = false;
    bool      _canTeleport   = true;

    // ──────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (pressFCanvas) pressFCanvas.SetActive(false);
    }

    void Update()
    {
        if (!_playerInRange) return;
        if (Keyboard.current == null) return;
        if (Keyboard.current.fKey.wasPressedThisFrame) TeleportPlayer();
    }

    void TeleportPlayer()
    {
        if (!_canTeleport || _player == null) return;
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("[GateTeleporter] Chưa gán đủ pointA và pointB!");
            return;
        }

        _canTeleport = false;

        // Xác định player đang gần A hay B → tele sang bên còn lại
        float distA = Vector3.Distance(_player.position, pointA.position);
        float distB = Vector3.Distance(_player.position, pointB.position);
        Transform destination = (distA <= distB) ? pointB : pointA;

        // Tắt CharacterController khi dịch chuyển để tránh collision reset
        var cc = _player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        _player.position = destination.position;
        if (_player.TryGetComponent<CharacterController>(out var cc2)) cc2.enabled = true;

        if (pressFCanvas) pressFCanvas.SetActive(false);
        _playerInRange = false;   // reset, OnTriggerEnter sẽ re-detect nếu cần

        Invoke(nameof(ResetCooldown), teleportCooldown);

        Debug.Log($"[GateTeleporter] Teleported to {destination.name}");
    }

    void ResetCooldown() => _canTeleport = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _player       = other.transform;
        _playerInRange = true;
        if (pressFCanvas) pressFCanvas.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (pressFCanvas) pressFCanvas.SetActive(false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (pointA) { Gizmos.color = Color.cyan;  Gizmos.DrawSphere(pointA.position, 0.3f); }
        if (pointB) { Gizmos.color = Color.green; Gizmos.DrawSphere(pointB.position, 0.3f); }
        if (pointA && pointB)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
#endif
}
