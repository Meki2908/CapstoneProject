using System;
using System.IO;
using UnityEngine;

[Serializable]
public class WeaponSelectionSaveData
{
    public int weaponType;
}

/// <summary>
/// Lưu <see cref="WeaponType"/> đang trang bị vào JSON dưới persistentDataPath
/// (giữ qua các lần Play trong Editor và bản build).
/// </summary>
public static class WeaponSelectionPersistence
{
    const string FileName = "weapon_selection.json";

    static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void Save(WeaponType type)
    {
        if (type == WeaponType.None) return;
        try
        {
            var data = new WeaponSelectionSaveData { weaponType = (int)type };
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WeaponSelectionPersistence] Save failed: {e.Message}");
        }
    }

    public static bool TryLoad(out WeaponType type)
    {
        type = WeaponType.None;
        if (!File.Exists(FilePath)) return false;
        try
        {
            var data = JsonUtility.FromJson<WeaponSelectionSaveData>(File.ReadAllText(FilePath));
            if (data == null) return false;
            type = (WeaponType)data.weaponType;
            if (type != WeaponType.Sword && type != WeaponType.Axe && type != WeaponType.Mage)
            {
                type = WeaponType.None;
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WeaponSelectionPersistence] Load failed: {e.Message}");
            return false;
        }
    }

    public static WeaponSO ResolveWeaponSO(WeaponType t)
    {
        var swapper = UnityEngine.Object.FindFirstObjectByType<WeaponSwapper>(FindObjectsInactive.Include);
        if (swapper == null)
        {
            Debug.LogWarning("[WeaponSelectionPersistence] No WeaponSwapper in scene — cannot resolve WeaponSO.");
            return null;
        }
        return swapper.GetWeaponSO(t);
    }
}
