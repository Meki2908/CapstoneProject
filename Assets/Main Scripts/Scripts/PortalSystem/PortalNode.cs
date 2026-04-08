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

    private bool _playerInRange = false;
    private Transform _playerTransform;

    private void Update()
    {
        if (!_playerInRange) return;
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
        if (other.CompareTag("Player") && IsLocalPlayerCollider(other))
        {
            _playerInRange   = true;
            _playerTransform = other.transform;
            ShowHint(true);

            if (portalUI == null)
                Debug.LogWarning("[PortalNode] Chưa gán UI Canvas vào PortalNode!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && IsLocalPlayerCollider(other))
        {
            ForceExit();
        }
    }

    bool IsLocalPlayerCollider(Collider other)
    {
        return other != null;
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
        ShowHint(false);

        if (portalUI != null)
            portalUI.ClosePortalMenu();
    }

    private void ShowHint(bool show)
    {
        if (pressFHintCanvas != null)
            pressFHintCanvas.SetActive(show);
    }
}
