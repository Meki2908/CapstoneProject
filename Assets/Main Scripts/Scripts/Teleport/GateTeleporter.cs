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
    Character _playerCharacter;
    bool      _playerInRange = false;
    bool      _canTeleport   = true;
    Collider _trigger;

    // ──────────────────────────────────────────────────────────────────────

    void Awake()
    {
        _trigger = GetComponent<Collider>();
    }

    void Start()
    {
        SharedPressFHintRegistry.SetVisible(pressFCanvas, GetInstanceID(), false);
    }

    void Update()
    {
        if (!_playerInRange) return;
        if (_playerCharacter != null && !IsCharacterInsideTrigger(_playerCharacter))
        {
            _playerInRange = false;
            _playerCharacter = null;
            SharedPressFHintRegistry.SetVisible(pressFCanvas, GetInstanceID(), false);
            return;
        }
        if (Keyboard.current == null) return;
        if (Keyboard.current.fKey.wasPressedThisFrame) TeleportPlayer();
    }

    void TeleportPlayer()
    {
        if (!_canTeleport || _playerCharacter == null) return;
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("[GateTeleporter] Chưa gán đủ pointA và pointB!");
            return;
        }

        _canTeleport = false;

        // Xác định player đang gần A hay B → tele sang bên còn lại
        Vector3 playerPos = _playerCharacter.transform.position;
        float distA = Vector3.Distance(playerPos, pointA.position);
        float distB = Vector3.Distance(playerPos, pointB.position);
        Transform destination = (distA <= distB) ? pointB : pointA;

        SoundManager.PlayTeleportStart();
        _playerCharacter.RequestTeleportToWorldPosition(destination.position);
        SoundManager.PlayTeleportArrive();

        SharedPressFHintRegistry.SetVisible(pressFCanvas, GetInstanceID(), false);
        _playerInRange = false;   // reset, OnTriggerEnter sẽ re-detect nếu cần

        Invoke(nameof(ResetCooldown), teleportCooldown);

        Debug.Log($"[GateTeleporter] Teleported to {destination.name}");
    }

    void ResetCooldown() => _canTeleport = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var ch = other.GetComponentInParent<Character>();
        if (ch == null) return;

        if (!CanBeUsedBy(ch))
            return;

        SetLocalCharacter(ch);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var ch = other.GetComponentInParent<Character>();
        if (ch != null && ch != _playerCharacter) return;

        _playerInRange = false;
        _playerCharacter = null;
        SharedPressFHintRegistry.SetVisible(pressFCanvas, GetInstanceID(), false);
    }

    public bool CanBeUsedBy(Character ch)
    {
        if (ch == null)
            return false;
        if (ch.Runner != null && ch.Runner.IsRunning)
            return Character.Local == ch;
        return true;
    }

    public void ExternalSetLocalCharacter(Character ch)
    {
        if (!CanBeUsedBy(ch))
            return;
        SetLocalCharacter(ch);
    }

    public void ExternalClearLocalCharacter(Character ch)
    {
        if (ch == null || _playerCharacter == null)
            return;
        if (ch != _playerCharacter)
            return;
        _playerInRange = false;
        _playerCharacter = null;
        SharedPressFHintRegistry.SetVisible(pressFCanvas, GetInstanceID(), false);
    }

    void SetLocalCharacter(Character ch)
    {
        _playerCharacter = ch;
        if (!_playerInRange)
            SharedPressFHintRegistry.SetVisible(pressFCanvas, GetInstanceID(), true);
        _playerInRange = true;
    }

    bool IsCharacterInsideTrigger(Character ch)
    {
        if (_trigger == null || ch == null)
            return false;
        Vector3 samplePoint = ch.transform.position + Vector3.up * 0.8f;
        Vector3 closest = _trigger.ClosestPoint(samplePoint);
        return (closest - samplePoint).sqrMagnitude <= 0.0001f;
    }

    void OnDisable()
    {
        _playerInRange = false;
        _playerCharacter = null;
        SharedPressFHintRegistry.SetVisible(pressFCanvas, GetInstanceID(), false);
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
