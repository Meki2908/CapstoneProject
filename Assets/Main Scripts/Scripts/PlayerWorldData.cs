using UnityEngine;

/// <summary>
/// Dữ liệu "xuyên scene" đơn giản cho world navigation.
/// Dùng để nhớ điểm quay về Map_Chinh khi rời dungeon (không cần serialize).
/// </summary>
public static class PlayerWorldData
{
    public static Vector3 ReturnPosition = Vector3.zero;
    public static bool HasReturnPoint = false;
}

