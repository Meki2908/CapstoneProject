#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: Tự động tạo 6 Crystal Stone ScriptableObjects.
/// Chạy từ menu: Tools > Create Crystal Stones
/// Sau khi chạy xong có thể xóa script này.
/// </summary>
public class CrystalStoneCreator
{
    [MenuItem("Tools/Create Crystal Stones")]
    public static void CreateCrystalStones()
    {
        string folder = "Assets/ASSETS/Asset_Player/ScriptableObjects/Items";

        // Đảm bảo folder tồn tại
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Debug.LogError($"[CrystalStoneCreator] Folder not found: {folder}");
            return;
        }

        // 6 rarity levels
        var rarities = new (string name, Rarity rarity, int id)[]
        {
            ("Crystal Stone Common",    Rarity.Common,    100),
            ("Crystal Stone Uncommon",  Rarity.Uncommon,  101),
            ("Crystal Stone Rare",      Rarity.Rare,      102),
            ("Crystal Stone Epic",      Rarity.Epic,      103),
            ("Crystal Stone Legendary", Rarity.Legendary,  104),
            ("Crystal Stone Mythic",    Rarity.Mythic,    105),
        };

        int created = 0;

        foreach (var (name, rarity, id) in rarities)
        {
            string path = $"{folder}/{name}.asset";

            // Nếu đã tồn tại → bỏ qua
            if (AssetDatabase.LoadAssetAtPath<Item>(path) != null)
            {
                Debug.Log($"[CrystalStoneCreator] Already exists: {path}");
                continue;
            }

            Item crystal = ScriptableObject.CreateInstance<Item>();
            crystal.id = id;
            crystal.itemName = name;
            crystal.rarity = rarity;
            crystal.itemType = ItemType.CrystalStone;
            crystal.isStackable = true;
            crystal.maxStackSize = 999;
            crystal.description = $"Đá Crystal {rarity}. Dùng để khảm gem vào vũ khí và trang bị tại NPC Thợ Rèn. Rarity càng cao, tỉ lệ khảm thành công càng lớn.";

            // icon sẽ set thủ công sau (vì cần Sprite asset)
            crystal.icon = null;

            AssetDatabase.CreateAsset(crystal, path);
            created++;
            Debug.Log($"[CrystalStoneCreator] Created: {path} (ID={id}, Rarity={rarity})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Crystal Stone Creator",
            $"Hoàn thành! Đã tạo {created} Crystal Stone ScriptableObjects.\n\n" +
            "⚠️ Nhớ gán Icon sprite cho mỗi crystal trong Inspector.\n" +
            $"Vị trí: {folder}",
            "OK"
        );
    }
}
#endif
