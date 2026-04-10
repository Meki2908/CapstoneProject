/// <summary>
/// Cờ tùy chọn để bỏ qua ẩn HUD một lần trong <see cref="PreEnterDungeonCutsceneController"/> (nếu cần tương lai).
/// </summary>
public static class DungeonPreEnterSession
{
    /// <summary>
    /// Đặt true trước khi reload scene dungeon từ <see cref="DungeonWaveManager.RestartDungeon"/>.
    /// </summary>
    public static bool SkipHudHideOnNextPreEnterTimeline { get; set; }

    /// <summary>
    /// Trả về true một lần rồi xóa cờ — dùng trong <see cref="PreEnterDungeonCutsceneController"/>.
    /// </summary>
    public static bool ConsumeSkipHudHideNextEntry()
    {
        if (!SkipHudHideOnNextPreEnterTimeline) return false;
        SkipHudHideOnNextPreEnterTimeline = false;
        return true;
    }
}
