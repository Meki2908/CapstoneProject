/// <summary>
/// Độ khó dungeon: Easy (Dễ), Normal (Trung bình), Hard (Khó)
/// </summary>
public enum DungeonDifficulty
{
    Easy   = 0,  // Dễ — không có boss
    Normal = 1,  // Trung bình — boss phụ wave 5
    Hard   = 2   // Khó — boss phụ wave 4, boss chính wave 5
}

/// <summary>
/// Static config — lưu lựa chọn độ khó + map giữa các scene.
/// Đồng đội làm UI chọn độ khó chỉ cần set 2 giá trị này trước khi LoadScene:
///   DungeonConfig.SelectedDifficulty = DungeonDifficulty.Hard;
///   DungeonConfig.SelectedMapType = 2; // 0=Desert, 1=Swamp, 2=Hell
/// </summary>
public static class DungeonConfig
{
    /// <summary>Độ khó đã chọn (default: Normal)</summary>
    public static DungeonDifficulty SelectedDifficulty = DungeonDifficulty.Normal;
    
    /// <summary>Map type: 0=Desert (Sa Mạc), 1=Swamp (Đầm Lầy), 2=Hell</summary>
    public static int SelectedMapType = 0;
}
