using UnityEngine;

/// <summary>
/// ScriptableObject chứa config font cho FloatingCombatText.
/// Tạo 1 lần trong project, tự động load ở MỌI scene.
/// 
/// CÁCH TẠO:
/// 1. Click chuột phải trong Project window
/// 2. Create → DungeonMania → Floating Text Settings
/// 3. Kéo Font bạn muốn vào
/// 4. XONG! Không cần gắn vào scene nào.
/// 
/// File PHẢI nằm trong thư mục Resources để auto-load.
/// Đường dẫn mặc định: Assets/_DungeonMania/Resources/FloatingTextSettings.asset
/// </summary>
[CreateAssetMenu(fileName = "FloatingTextSettings", menuName = "DungeonMania/Floating Text Settings")]
public class FloatingTextSettings : ScriptableObject
{
    [Header("=== FONT SETTINGS ===")]
    [Tooltip("Font tùy chỉnh — kéo font từ project vào đây. Để trống = dùng Arial mặc định")]
    public Font customFont;
    
    [Tooltip("Kích thước chữ (character size trong world space)")]
    public float characterSize = 0.4f;
    
    [Tooltip("Font size (resolution — cao hơn = sắc nét hơn)")]
    public int fontSize = 48;
    
    [Header("=== COLOR SETTINGS ===")]
    [Tooltip("Màu chữ BLOCKED khi shield chặn damage")]
    public Color blockedColor = new Color(0.3f, 0.7f, 1f); // Light blue
    
    [Tooltip("Thời gian hiển thị (giây)")]
    public float displayDuration = 1.5f;
}
