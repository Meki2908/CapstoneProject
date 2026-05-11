using UnityEngine;

/// <summary>Shared display name for lobby UI and networked <see cref="Character"/> nameplates.</summary>
public static class PlayerDisplayNamePrefs
{
    public const string Key = "SavedPlayerName";
    public const int MaxLen = 24;

    public static string DefaultRandomPlayerName() => "Player_" + Random.Range(100, 999);

    public static string Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return DefaultRandomPlayerName();
        s = s.Trim();
        if (s.Length > MaxLen)
            s = s.Substring(0, MaxLen);
        return s;
    }

    public static string GetSavedOrDefault()
    {
        var raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrWhiteSpace(raw))
            return DefaultRandomPlayerName();
        return Sanitize(raw);
    }

    public static void Save(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return;
        PlayerPrefs.SetString(Key, Sanitize(displayName));
        PlayerPrefs.Save();
    }
}
