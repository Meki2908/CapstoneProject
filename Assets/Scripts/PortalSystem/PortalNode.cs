using UnityEngine;

public class PortalNode : MonoBehaviour
{
    [Tooltip("Kéo Canvas Menu chọn cổng vào đây")]
    public PortalUIController portalUI;

    [Tooltip("Vị trí sẽ dịch chuyển người chơi tới khi chọn cổng này. Hãy tạo một Empty GameObject đặt hơi nhích ra trước cổng một chút để người chơi không bị kẹt trong trigger.")]
    public Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem có phải người chơi chạm vào portal không
        if (other.CompareTag("Player"))
        {
            if (portalUI != null)
            {
                portalUI.OpenPortalMenu(other.transform);
            }
            else
            {
                Debug.LogWarning("[PortalNode] Chưa gán UI Canvas vào PortalNode!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Khi người chơi rời khỏi phạm vi cổng, tự đóng UI
        if (other.CompareTag("Player"))
        {
            if (portalUI != null)
            {
                portalUI.ClosePortalMenu();
            }
        }
    }
}
