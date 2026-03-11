using UnityEngine;
using UnityEngine.UI;

public class PortalUIController : MonoBehaviour
{
    [Header("Portal Destinations (Kéo SpawnPoint của 3 cổng vào đây)")]
    // Bạn nên tạo một Empty GameObject đặt nhích ra phía trước mặt của chiếc cổng
    // và dùng GameObject đó làm Transform đích để người chơi không bị teleport đè vào lưới cổng 
    public Transform portal1_Dest;
    public Transform portal2_Dest;
    public Transform portal3_Dest;

    [Header("UI Buttons (Kéo 4 nút vào đây)")]
    public Button btnPortal1;
    public Button btnPortal2;
    public Button btnPortal3;
    public Button btnClose;

    [Header("Player Reference")]
    [Tooltip("Kéo thả Player của bạn vào đây nếu bạn muốn gán cứng, nếu không script sẽ tự tìm qua Tag 'Player'")]
    public Transform playerOverride;

    // Lưu lại vị trí người chơi lúc nhận trigger cổng
    private Transform currentPlayer;

    private void Start()
    {
        Debug.Log("[PortalUI] PortalUIController Start() đang chạy...");

        // Kiểm tra Canvas và GraphicRaycaster
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            Debug.LogError($"[PortalUI] KHÔNG TÌM THẤY 'Graphic Raycaster' trên Canvas '{canvas.name}'! Các nút bấm sẽ không nhận chuột.");
        }

        // Kiểm tra EventSystem
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogError("[PortalUI] KHÔNG TÌM THẤY 'Event System' trong Scene! Hãy chuột phải vào Hierarchy -> UI -> Event System.");
        }

        // Gán chức năng khi nhấn nút với Debug Log để theo dõi
        if (btnPortal1 != null) {
            btnPortal1.onClick.AddListener(() => { Debug.Log("[PortalUI] Bấm nút Cổng 1"); TeleportTo(portal1_Dest); });
            Debug.Log("[PortalUI] Đã gán thành công listener cho nút Cổng 1");
        } else Debug.LogWarning("[PortalUI] Ô 'Btn Portal 1' đang bị trống (Null). Hãy kéo nút vào Inspector!");
        
        if (btnPortal2 != null) {
            btnPortal2.onClick.AddListener(() => { Debug.Log("[PortalUI] Bấm nút Cổng 2"); TeleportTo(portal2_Dest); });
            Debug.Log("[PortalUI] Đã gán thành công listener cho nút Cổng 2");
        } else Debug.LogWarning("[PortalUI] Ô 'Btn Portal 2' đang bị trống (Null). Hãy kéo nút vào Inspector!");
        
        if (btnPortal3 != null) {
            btnPortal3.onClick.AddListener(() => { Debug.Log("[PortalUI] Bấm nút Cổng 3"); TeleportTo(portal3_Dest); });
            Debug.Log("[PortalUI] Đã gán thành công listener cho nút Cổng 3");
        } else Debug.LogWarning("[PortalUI] Ô 'Btn Portal 3' đang bị trống (Null). Hãy kéo nút vào Inspector!");
        
        if (btnClose != null) {
            btnClose.onClick.AddListener(() => { Debug.Log("[PortalUI] Bấm nút Đóng"); ClosePortalMenu(); });
            Debug.Log("[PortalUI] Đã gán thành công listener cho nút Đóng");
        } else Debug.LogWarning("[PortalUI] Ô 'Btn Close' đang bị trống (Null). Hãy kéo nút vào Inspector!");
        
        // Ẩn Menu lúc mới bắt đầu game
        gameObject.SetActive(false);
    }

    public void OpenPortalMenu(Transform triggeredPlayer)
    {
        // Luôn ưu tiên dùng playerOverride nếu được gán trong Inspector
        currentPlayer = (playerOverride != null) ? playerOverride : triggeredPlayer;
        
        // Hiện UI
        gameObject.SetActive(true);
        
        // Ép chuột hiển thị và mở khóa
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("[PortalUI] Menu đã mở. Hãy kiểm tra nếu nút bấm vẫn không phản hồi.");
    }

    public void ClosePortalMenu()
    {
        // Ẩn UI
        gameObject.SetActive(false);
        
        // Khóa chuột lại để tiếp tục điều khiển camera
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void TeleportTo(Transform destination)
    {
        // Fallback cuối cùng: Nếu vẫn chưa có Player, thử tìm bằng Tag
        if (currentPlayer == null)
        {
            if (playerOverride != null) currentPlayer = playerOverride;
            else
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) currentPlayer = p.transform;
            }
        }

        if (currentPlayer == null)
        {
            Debug.LogError("[Portal] KHÔNG TÌM THẤY PLAYER! Hãy kéo Player vào ô 'Player Override' trong Inspector của PortalUIController.");
            return;
        }

        if (destination == null)
        {
            Debug.LogError("[Portal] KHÔNG CÓ ĐÍCH ĐẾN! Kiểm tra xem bạn đã kéo đích đến vào script ở Inspector chưa.");
            return;
        }

        Debug.Log($"[Portal] Đang dịch chuyển Player {currentPlayer.name} đến {destination.name}...");

        // Vô hiệu hóa CharacterController trước khi thay đổi Position
        CharacterController cc = currentPlayer.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Dịch chuyển
        currentPlayer.position = destination.position;
        currentPlayer.rotation = destination.rotation;

        // Bật lại CharacterController
        if (cc != null) cc.enabled = true;

        ClosePortalMenu();
        Debug.Log("[Portal] Dịch chuyển hoàn tất.");
    }
}
