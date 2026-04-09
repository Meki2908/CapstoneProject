using UnityEngine;

/// <summary>
/// Texture 32×32 trong suốt dùng khi không gán Player Settings &gt; Default Cursor.
/// Gán PNG trong suốt vào Project Settings &gt; Player &gt; Default Cursor là cách khuyến nghị (user).
/// </summary>
static class TransparentCursorUtility
{
    static Texture2D _runtime32;

    public static Texture2D GetRuntimeTransparent32()
    {
        if (_runtime32 != null)
            return _runtime32;

        _runtime32 = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        _runtime32.name = "RuntimeTransparentCursor32";
        var px = new Color32[32 * 32];
        for (int i = 0; i < px.Length; i++)
            px[i] = new Color32(0, 0, 0, 0);
        _runtime32.SetPixels32(px);
        _runtime32.Apply(false, false);
        return _runtime32;
    }
}
