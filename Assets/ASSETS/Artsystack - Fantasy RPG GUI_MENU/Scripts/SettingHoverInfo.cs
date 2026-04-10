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
                "Change the display resolution. Higher resolution provides sharper visuals but requires more powerful hardware."
            )},
            { "Tab_FrameRate", (
                "Frame Rate",
                "Set the maximum frame rate (FPS). Higher FPS results in smoother gameplay. Choose 60 FPS for balance, 120+ for competitive gaming."
            )},
            { "Tab_RenderDistance", (
                "Render Distance",
                "Controls how far objects are rendered in the game world. Lower values significantly improve performance, higher values allow you to see further."
            )},
            { "Tab_DisplayMode", (
                "Display Mode",
                "Choose display mode: Fullscreen (full screen), Windowed (resizable window), or Borderless (fullscreen without borders)."
            )},
            { "Tab_ChromaticAberration", (
                "Chromatic Aberration",
                "Adds a cinematic color fringing effect at the edges of the screen. Disable for a cleaner, sharper image."
            )},
            { "Tab_ShadowQuality", (
                "Shadow Quality",
                "Controls the quality of shadows in the game. Higher quality produces softer, more detailed shadows but costs more performance. Set to Off for maximum FPS."
            )},
            { "Tab_GraphicQuality", (
                "Graphic Quality",
                "Overall graphics quality preset. Adjusts texture filtering, LOD, anti-aliasing (MSAA), and lighting. Ultra provides the best visuals, Low gives the best performance."
            )},
            { "Tab_Sharpening", (
                "Sharpening",
                "Enhances image sharpness and clarity. Enable for crisper details, disable if the image looks too noisy or harsh."
            )},
            { "Tab_Brightness", (
                "Brightness",
                "Adjust the overall brightness of the game. Increase if the image is too dark, decrease if it feels too bright or washed out."
            )},
            { "Tab_Contrast", (
                "Contrast",
                "Adjust the contrast between bright and dark areas. Increase for a more vivid image, decrease for a softer look."
            )},
            { "Tab_Saturation", (
                "Saturation",
                "Toggle color saturation. Enable for vibrant, colorful visuals. Disable for a more natural, muted color palette."
            )},

            // ===== AUDIO =====
            { "Tab_MasterVolume", (
                "Master Volume",
                "Controls the overall volume of all game audio, including music, sound effects, and voice."
            )},
            { "Tab_MusicVolume", (
                "Music Volume",
                "Adjust the background music volume independently without affecting sound effects."
            )},
            { "Tab_SFXVolume", (
                "SFX Volume",
                "Adjust the volume of sound effects: footsteps, sword swings, impacts, and other in-game action sounds."
            )},
            { "Tab_VoiceLanguage", (
                "Language",
                "Select the display language for the game. Available options: English, Vietnamese, Japanese, Korean, Chinese."
            )},
            { "Tab_BackgroundSound", (
                "Background Sound",
                "Toggle ambient environmental sounds: wind, birds, flowing water. Enable for a more immersive game world."
            )},

            // ===== CONTROLLER - KEY BINDINGS =====
            { "Tab_KeyBind_Dodge", (
                "Dodge",
                "Key for dodging enemy attacks. Use at the right moment to avoid damage and create counterattack opportunities."
            )},
            { "Tab_KeyBind_Sprint", (
                "Sprint",
                "Key for sprinting. Hold to move at higher speed, consumes stamina while active."
            )},
            { "Tab_KeyBind_SneakCrouch", (
                "Sneak / Crouch",
                "Key for sneaking or crouching. Reduces movement noise, helping you avoid enemy detection."
            )},
            { "Tab_KeyBind_Jump", (
                "Jump",
                "Key for jumping. Use to leap over obstacles, reach higher ground, or dodge low attacks."
            )},
            { "Tab_KeyBind_MoveForward", (
                "Move Forward",
                "Key to move forward in the direction the camera is facing."
            )},
            { "Tab_KeyBind_MoveBackward", (
                "Move Backward",
                "Key to move backward. Useful for maintaining distance from enemies."
            )},
            { "Tab_KeyBind_MoveLeft", (
                "Move Left",
                "Key to strafe left. Combine with other keys for diagonal movement."
            )},
            { "Tab_KeyBind_MoveRight", (
                "Move Right",
                "Key to strafe right. Combine with other keys for diagonal movement."
            )},
            { "Tab_KeyBind_Heal", (
                "Heal",
                "Key to use healing items. Restores your character's HP. Has a cooldown between uses."
            )},
            { "Tab_KeyBind_Menu", (
                "Menu",
                "Key to open the game menu. Access inventory, map, quests, and other options."
            )},
            { "Tab_KeyBind_Attack", (
                "Attack",
                "Key for basic attacks. Press to strike enemies, press repeatedly or hold for combo attacks."
            )},
            { "Tab_KeyBind_Interact", (
                "Interact",
                "Key to interact with NPCs, items, doors, treasure chests, and other objects in the game world."
            )},
            { "Tab_KeyBind_WeaponItemWheel", (
                "Weapon / Item Wheel",
                "Key to open the quick-select wheel for weapons and items. Hold and move the mouse to select."
            )},

            // ===== GAMEPLAY =====
            { "Tab_MiniMap", (
                "Mini Map",
                "Toggle the minimap in the corner of the screen. Displays your position, nearby enemies, and quest markers."
            )},
            { "Tab_CameraMouseSpeed", (
                "Camera Move Speed",
                "Adjust camera rotation speed with the mouse. Increase for faster response, decrease if it feels too sensitive."
            )},
            // Alias — actual Unity name
            { "Tab_CameraMoveSpeed", (
                "Camera Move Speed",
                "Adjust camera rotation speed with the mouse. Increase for faster response, decrease if it feels too sensitive."
            )},
            { "Tab_CameraZoomSpeedGameplay", (
                "Camera Zoom Speed",
                "Adjust camera zoom speed when scrolling. Increase for faster zoom, decrease for smoother, more precise control."
            )},
            // Alias — actual Unity name
            { "Tab_CameraZoomSpeed", (
                "Camera Zoom Speed",
                "Adjust camera zoom speed when scrolling. Increase for faster zoom, decrease for smoother, more precise control."
            )},
            { "Tab_HDRMode", (
                "HDR Mode",
                "Toggle HDR (High Dynamic Range) mode. HDR provides a wider color range with more vivid and lifelike visuals. Requires an HDR-capable display."
            )},
            // Alias — actual Unity name
            { "Tab_Language", (
                "Language",
                "Select the display language for the game. Available options: English, Vietnamese, Japanese, Korean, Chinese."
            )},

            // ===== ALIAS KEYS — matching actual GameObject names =====
            { "Tab_GraphicsQuality", (
                "Graphics Quality",
                "Overall graphics quality preset. Adjusts texture filtering, LOD, anti-aliasing (MSAA), and lighting. Ultra provides the best visuals, Low gives the best performance."
            )},
            { "Tab_Sharening", (
                "Sharpening",
                "Enhances image sharpness and clarity. Enable for crisper details, disable if the image looks too noisy or harsh."
            )},

            // ===== DIRECT NAME KEYS (Controller tab) =====
            { "Dodge", (
                "Dodge",
                "Key for dodging enemy attacks. Use at the right moment to avoid damage and create counterattack opportunities."
            )},
            { "Sprint", (
                "Sprint",
                "Key for sprinting. Hold to move at higher speed, consumes stamina while active."
            )},
            { "Sneak/Crouch", (
                "Sneak / Crouch",
                "Key for sneaking or crouching. Reduces movement noise, helping you avoid enemy detection."
            )},
            { "SneakCrouch", (
                "Sneak / Crouch",
                "Key for sneaking or crouching. Reduces movement noise, helping you avoid enemy detection."
            )},
            { "Jump", (
                "Jump",
                "Key for jumping. Use to leap over obstacles, reach higher ground, or dodge low attacks."
            )},
            { "Move Forward", (
                "Move Forward",
                "Key to move forward in the direction the camera is facing."
            )},
            { "MoveForward", (
                "Move Forward",
                "Key to move forward in the direction the camera is facing."
            )},
            { "Move Backward", (
                "Move Backward",
                "Key to move backward. Useful for maintaining distance from enemies."
            )},
            { "MoveBackward", (
                "Move Backward",
                "Key to move backward. Useful for maintaining distance from enemies."
            )},
            { "Move Right", (
                "Move Right",
                "Key to strafe right. Combine with other keys for diagonal movement."
            )},
            { "MoveRight", (
                "Move Right",
                "Key to strafe right. Combine with other keys for diagonal movement."
            )},
            { "Move Left", (
                "Move Left",
                "Key to strafe left. Combine with other keys for diagonal movement."
            )},
            { "MoveLeft", (
                "Move Left",
                "Key to strafe left. Combine with other keys for diagonal movement."
            )},
            { "Heal", (
                "Heal",
                "Key to use healing items. Restores your character's HP. Has a cooldown between uses."
            )},
            { "Menu", (
                "Menu",
                "Key to open the game menu. Access inventory, map, quests, and other options."
            )},
            { "Attack", (
                "Attack",
                "Key for basic attacks. Press to strike enemies, press repeatedly or hold for combo attacks."
            )},
            { "Interact", (
                "Interact",
                "Key to interact with NPCs, items, doors, treasure chests, and other objects in the game world."
            )},
            { "Weapon / Item Wheel", (
                "Weapon / Item Wheel",
                "Key to open the quick-select wheel for weapons and items. Hold and move the mouse to select."
            )},
            { "WeaponItemWheel", (
                "Weapon / Item Wheel",
                "Key to open the quick-select wheel for weapons and items. Hold and move the mouse to select."
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
                string n = child.name;
                bool isSettingItem = n.StartsWith("Tab_") || n.StartsWith("KeyBind_") || settingData.ContainsKey(n);

                if (isSettingItem)
                {
                    Transform bg = child.Find("Bg");
                    // Use direct name first, then try normalized
                    string key = settingData.ContainsKey(n) ? n : (n.StartsWith("Tab_") ? n : "Tab_" + n);
                    SetupHoverEvent(child.gameObject, bg, key);
                }

                if (child.childCount > 0 && !isSettingItem)
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

                Debug.Log($"[HoverInfo] Item='{itemName}', Found={settingData.ContainsKey(itemName)}, DescRef={infoDescription != null}");

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
