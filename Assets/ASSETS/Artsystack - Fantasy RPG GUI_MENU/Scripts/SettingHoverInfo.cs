using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace Artsystack.ArtsystackGui
{
    /// <summary>
    /// Tự động tìm tất cả setting items có child "Bg" trong các panel settings.
    /// Khi hover: hiện Bg + cập nhật Right Side info panel.
    /// Không cần thêm script thủ công vào từng nút.
    /// </summary>
    public class SettingHoverInfo : MonoBehaviour
    {
        // Static references to the right panel
        private static TextMeshProUGUI infoTitle;
        private static TextMeshProUGUI infoDescription;
        private static GameObject infoPanel;

        // ========== DỮ LIỆU TÊN VÀ MÔ TẢ ==========
        private static readonly Dictionary<string, (string name, string description)> settingData
            = new Dictionary<string, (string, string)>
        {
            // ===== GRAPHICS =====
            { "Tab_ScreenResolution", (
                "Screen Resolution",
                "Thay đổi độ phân giải màn hình. Độ phân giải cao hơn cho hình ảnh sắc nét hơn nhưng yêu cầu phần cứng mạnh hơn."
            )},
            { "Tab_FrameRate", (
                "Frame Rate",
                "Giới hạn tốc độ khung hình (FPS). FPS cao hơn cho trải nghiệm mượt mà hơn. Chọn 60 FPS cho cân bằng, 120+ cho gaming."
            )},
            { "Tab_DisplayMode", (
                "Display Mode",
                "Chế độ hiển thị: Fullscreen (toàn màn hình), Windowed (cửa sổ), Borderless (toàn màn hình không viền)."
            )},
            { "Tab_ChromaticAberration", (
                "Chromatic Aberration",
                "Hiệu ứng quang sai màu sắc ở rìa màn hình, tạo cảm giác điện ảnh. Tắt để có hình ảnh rõ ràng hơn."
            )},
            { "Tab_Sharpening", (
                "Sharpening",
                "Tăng độ sắc nét cho hình ảnh. Bật để chi tiết rõ hơn, tắt nếu thấy hình ảnh bị nhiễu."
            )},
            { "Tab_Brightness", (
                "Brightness",
                "Điều chỉnh độ sáng tổng thể của game. Tăng nếu hình ảnh quá tối, giảm nếu quá sáng gây chói mắt."
            )},
            { "Tab_Contrast", (
                "Contrast",
                "Điều chỉnh độ tương phản giữa vùng sáng và tối. Tăng để hình ảnh rõ nét, giảm để mềm mại hơn."
            )},
            { "Tab_Saturation", (
                "Saturation",
                "Bật/tắt độ bão hòa màu sắc. Bật để màu sắc rực rỡ sinh động, tắt để hình ảnh tự nhiên hơn."
            )},

            // ===== AUDIO =====
            { "Tab_MasterVolume", (
                "Master Volume",
                "Âm lượng tổng của toàn bộ game. Điều chỉnh này ảnh hưởng đến tất cả âm thanh bao gồm nhạc, hiệu ứng và giọng nói."
            )},
            { "Tab_MusicVolume", (
                "Music Volume",
                "Âm lượng nhạc nền. Điều chỉnh riêng nhạc nền mà không ảnh hưởng đến hiệu ứng âm thanh."
            )},
            { "Tab_SFXVolume", (
                "SFX Volume",
                "Âm lượng hiệu ứng âm thanh: tiếng bước chân, tiếng kiếm, va chạm, và các âm thanh hành động trong game."
            )},
            { "Tab_VoiceLanguage", (
                "Voice Language",
                "Ngôn ngữ lồng tiếng cho nhân vật trong game. Có thể chọn: English, Vietnamese, Japanese, Korean, Chinese."
            )},
            { "Tab_BackgroundSound", (
                "Background Sound",
                "Âm thanh nền môi trường: tiếng gió, tiếng chim, tiếng nước chảy. Bật để tăng sự sống động cho thế giới game."
            )},

            // ===== CONTROLLER - KEY BINDINGS =====
            { "Tab_KeyBind_Dodge", (
                "Dodge",
                "Phím né tránh đòn tấn công. Sử dụng đúng thời điểm để tránh sát thương và tạo cơ hội phản công."
            )},
            { "Tab_KeyBind_Sprint", (
                "Sprint",
                "Phím chạy nhanh. Giữ phím để di chuyển với tốc độ cao hơn, tiêu hao stamina khi sử dụng."
            )},
            { "Tab_KeyBind_SneakCrouch", (
                "Sneak / Crouch",
                "Phím lẻn/ngồi xuống. Giảm tiếng ồn khi di chuyển, giúp tránh bị kẻ địch phát hiện."
            )},
            { "Tab_KeyBind_Jump", (
                "Jump",
                "Phím nhảy. Dùng để vượt qua chướng ngại vật, nhảy lên cao hoặc tránh đòn tấn công tầm thấp."
            )},
            { "Tab_KeyBind_MoveForward", (
                "Move Forward",
                "Phím di chuyển tiến về phía trước theo hướng nhìn của camera."
            )},
            { "Tab_KeyBind_MoveBackward", (
                "Move Backward",
                "Phím di chuyển lùi về phía sau. Hữu ích khi cần giữ khoảng cách với kẻ địch."
            )},
            { "Tab_KeyBind_MoveLeft", (
                "Move Left",
                "Phím di chuyển sang trái. Kết hợp với các phím khác để di chuyển chéo."
            )},
            { "Tab_KeyBind_MoveRight", (
                "Move Right",
                "Phím di chuyển sang phải. Kết hợp với các phím khác để di chuyển chéo."
            )},
            { "Tab_KeyBind_Heal", (
                "Heal",
                "Phím sử dụng vật phẩm hồi máu. Hồi phục HP của nhân vật, có thời gian hồi giữa các lần sử dụng."
            )},
            { "Tab_KeyBind_Menu", (
                "Menu",
                "Phím mở menu game. Truy cập túi đồ, bản đồ, nhiệm vụ, và các tùy chọn khác."
            )},
            { "Tab_KeyBind_Attack", (
                "Attack",
                "Phím tấn công cơ bản. Nhấn để tấn công kẻ địch, giữ hoặc nhấn liên tục để thực hiện combo."
            )},
            { "Tab_KeyBind_Interact", (
                "Interact",
                "Phím tương tác với NPC, vật phẩm, cửa, rương kho báu và các đối tượng trong thế giới game."
            )},
            { "Tab_KeyBind_WeaponItemWheel", (
                "Weapon / Item Wheel",
                "Phím mở vòng tròn chọn vũ khí và vật phẩm nhanh. Giữ phím và di chuột để chọn."
            )},

            // ===== GAMEPLAY =====
            { "Tab_MiniMap", (
                "Mini Map",
                "Bật/tắt bản đồ nhỏ góc màn hình. Hiển thị vị trí nhân vật, kẻ địch gần và điểm đánh dấu nhiệm vụ."
            )},
            { "Tab_CameraMouseSpeed", (
                "Camera Move Speed",
                "Tốc độ di chuyển camera bằng chuột. Tăng để camera phản hồi nhanh hơn, giảm nếu cảm thấy quá nhạy."
            )},
            { "Tab_CameraZoomSpeedGameplay", (
                "Camera Zoom Speed",
                "Tốc độ zoom camera khi cuộn chuột. Tăng để zoom nhanh hơn, giảm để zoom mượt mà và chính xác hơn."
            )},
            { "Tab_HDRMode", (
                "HDR Mode",
                "Bật/tắt chế độ HDR (High Dynamic Range). HDR cho dải màu rộng hơn, hình ảnh sống động và chân thực hơn. Yêu cầu màn hình hỗ trợ HDR."
            )},
        };

        /// <summary>
        /// Gọi từ SettingsManager.Start() để set reference panel bên phải
        /// </summary>
        public static void SetInfoPanel(GameObject panel, TextMeshProUGUI title, TextMeshProUGUI description)
        {
            infoPanel = panel;
            infoTitle = title;
            infoDescription = description;
        }

        /// <summary>
        /// Tự động quét tất cả setting items trong panels và gắn hover event.
        /// Gọi từ SettingsManager.Start()
        /// </summary>
        public static void AutoSetupAllPanels(params GameObject[] panels)
        {
            foreach (var panel in panels)
            {
                if (panel == null) continue;
                ScanAndSetup(panel.transform);
            }
        }

        private static void ScanAndSetup(Transform parent)
        {
            foreach (Transform child in parent)
            {
                // Tìm child có tên bắt đầu bằng "Tab_"
                if (child.name.StartsWith("Tab_"))
                {
                    // Tìm Bg child
                    Transform bg = child.Find("Bg");

                    // Thêm EventTrigger
                    SetupHoverEvent(child.gameObject, bg, child.name);
                }

                // Đệ quy tìm sâu hơn (ScrollRect > ViewPort > Content)
                if (child.childCount > 0 && !child.name.StartsWith("Tab_"))
                {
                    ScanAndSetup(child);
                }
            }
        }

        private static void SetupHoverEvent(GameObject settingItem, Transform bg, string itemName)
        {
            // Đảm bảo có EventTrigger
            EventTrigger trigger = settingItem.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = settingItem.AddComponent<EventTrigger>();

            // Ẩn Bg mặc định
            if (bg != null)
                bg.gameObject.SetActive(false);

            // PointerEnter - Hiện Bg + cập nhật info
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) =>
            {
                if (bg != null) bg.gameObject.SetActive(true);

                if (settingData.TryGetValue(itemName, out var info))
                {
                    if (infoTitle != null) infoTitle.text = info.name;
                    if (infoDescription != null) infoDescription.text = info.description;
                }
                else
                {
                    // Fallback: dùng tên GameObject
                    if (infoTitle != null) infoTitle.text = itemName.Replace("Tab_", "").Replace("_", " ");
                    if (infoDescription != null) infoDescription.text = "";
                }

                if (infoPanel != null) infoPanel.SetActive(true);
            });
            trigger.triggers.Add(enterEntry);

            // PointerExit - Ẩn Bg + ẩn info
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) =>
            {
                if (bg != null) bg.gameObject.SetActive(false);
                if (infoPanel != null) infoPanel.SetActive(false);
            });
            trigger.triggers.Add(exitEntry);
        }
    }
}
