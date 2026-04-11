using UnityEngine;

/// <summary>
/// Quy tắc chung cho object tag HUD: một số root (vd. GUI_Dungeon) không được ẩn cùng lúc với HUD player
/// vì không có bước restore riêng — <see cref="PreEnterDungeonCutsceneController"/>, <see cref="BossCutsceneController"/>.
/// </summary>
public static class HudSceneTagUtilities
{
    public const string DungeonHudUiRootName = "GUI_Dungeon";

    public static bool IsDungeonHudUiRoot(GameObject go)
    {
        return go != null && go.name == DungeonHudUiRootName;
    }
}
