/// <summary>
/// Trạng thái phiên giữa lần load scene dungeon (vd. Restart → không ẩn HUD khi chạy pre-enter timeline).
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
