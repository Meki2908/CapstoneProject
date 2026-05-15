using UnityEngine;
using UnityEngine.InputSystem;

public class PortalNode : MonoBehaviour
{
    [Tooltip("Kéo Canvas Menu chọn cổng vào đây")]
    public PortalUIController portalUI;

    [Tooltip("Vị trí sẽ dịch chuyển người chơi tới khi chọn cổng này.")]
    public Transform spawnPoint;

    [Header("Press F Hint")]
    [Tooltip("Kéo GameObject/Canvas 'Press F' vào đây (sẽ bật/tắt tự động)")]
    public GameObject pressFHintCanvas;

    [Header("Debug")]
    [Tooltip("Log khi vào vùng Beacon (menu F, KHÔNG phải dungeon canvas). Lọc Console: PortalNode")]
    public bool enableDebugLogs = true;

    private bool _playerInRange = false;
    private Transform _playerTransform;
    private Character _playerCharacter;
    Collider _trigger;

    private void Awake()
    {
        _trigger = GetComponent<Collider>();
    }

    private void Update()
    {
        if (!_playerInRange) return;
        if (_playerCharacter != null && !IsCharacterInsideTrigger(_playerCharacter))
        {
            ForceExit();
            return;
        }
        if (Keyboard.current == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            ShowHint(false);
            if (portalUI != null)
                portalUI.OpenPortalMenu(_playerTransform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (enableDebugLogs)
            Debug.Log($"[PortalNode] OnTriggerEnter Beacon '{name}' | other='{other.name}' tag='{other.tag}' (đây là menu bấm F, không phải dungeon canvas)", this);

        if (!other.CompareTag("Player"))
        {
            if (enableDebugLogs)
                Debug.Log($"[PortalNode] REJECTED tag '{other.tag}' (cần Player)", this);
            return;
        }

        var ch = other.GetComponentInParent<Character>();
        if (ch == null)
        {
            if (enableDebugLogs)
                Debug.Log("[PortalNode] REJECTED: không có Character trên parent", this);
            return;
        }

        if (!CanBeUsedBy(ch))
        {
            if (enableDebugLogs)
                Debug.Log($"[PortalNode] REJECTED: không phải local player (Fusion) char={ch.name}", this);
            return;
        }

        SetLocalCharacter(ch);

        if (portalUI == null)
            Debug.LogWarning("[PortalNode] Chưa gán UI Canvas vào PortalNode!");
        else if (enableDebugLogs)
            Debug.Log("[PortalNode] OK — hiện gợi ý Press F; bấm F để mở menu 4 cổng", this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var ch = other.GetComponentInParent<Character>();
        if (ch != null && _playerCharacter != null && ch != _playerCharacter)
            return;

        ForceExit();
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
        ForceExit();
    }

    private void SetLocalCharacter(Character ch)
    {
        _playerCharacter = ch;
        _playerTransform = ch.transform;
        if (!_playerInRange)
            ShowHint(true);
        _playerInRange = true;
    }

    /// <summary>
    /// Reset state ngay lập tức.
    /// Gọi từ OnTriggerExit (thoát bình thường)
    /// hoặc từ PortalUIController sau khi teleport (vì warp làm mất OnTriggerExit).
    /// </summary>
    public void ForceExit()
    {
        _playerInRange   = false;
        _playerTransform = null;
        _playerCharacter = null;
        ShowHint(false);

        if (portalUI != null)
            portalUI.ClosePortalMenu();
    }

    private void ShowHint(bool show)
    {
        SharedPressFHintRegistry.SetVisible(pressFHintCanvas, GetInstanceID(), show);
    }

    bool IsCharacterInsideTrigger(Character ch)
    {
        if (_trigger == null || ch == null)
            return false;
        Vector3 samplePoint = ch.transform.position + Vector3.up * 0.8f;
        Vector3 closest = _trigger.ClosestPoint(samplePoint);
        return (closest - samplePoint).sqrMagnitude <= 0.0001f;
    }

    private void OnDisable()
    {
        ShowHint(false);
        _playerInRange = false;
        _playerTransform = null;
        _playerCharacter = null;
    }
}
