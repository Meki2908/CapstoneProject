using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool: "Tools → Create Refinement Stones"
/// Creates 7 Refinement Stone Item ScriptableObjects (Tier 1-7)
/// in Resources/Items/ folder with pre-configured properties.
/// </summary>
public class RefinementStoneCreator : Editor
{
    [MenuItem("Tools/Create Refinement Stones")]
    public static void CreateRefinementStones()
    {
        string folderPath = "Assets/Resources/Items";

        // Ensure folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Resources", "Items");

        string[] tierNames = { "I", "II", "III", "IV", "V", "VI", "VII" };
        Color[] tierColors = {
            new Color(0.8f, 0.8f, 0.8f),       // T1: Silver
            new Color(0.4f, 0.9f, 0.4f),        // T2: Green
            new Color(0.3f, 0.6f, 1.0f),        // T3: Blue
            new Color(0.7f, 0.3f, 0.9f),        // T4: Purple
            new Color(1.0f, 0.84f, 0.0f),       // T5: Gold
            new Color(1.0f, 0.3f, 0.3f),        // T6: Red
            new Color(1.0f, 0.5f, 0.0f),        // T7: Orange (Mythic)
        };

        int createdCount = 0;

        for (int tier = 1; tier <= 7; tier++)
        {
            string assetName = $"Refinement Stone T{tier}";
            string assetPath = $"{folderPath}/{assetName}.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<Item>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[RefinementStoneCreator] {assetName} already exists — skipping.");
                continue;
            }

            // Create new ScriptableObject
            Item stone = ScriptableObject.CreateInstance<Item>();
            stone.id = 400 + tier;                           // 401-407
            stone.itemName = $"Refinement Stone {tierNames[tier - 1]}";
            stone.rarity = (Rarity)tier;                     // Common=1 through Mythic(placeholder)
            stone.useRandomRarity = false;
            stone.isStackable = true;
            stone.maxStackSize = 99;
            stone.description = $"Tier {tier} refinement material.\nUsed to enhance equipment at the Blacksmith.\n4× Tier {tier} → 1× Tier {tier + 1}";
            stone.itemType = ItemType.Material;
            stone.refinementTier = tier;

            // Map tier to Rarity for proper color coding
            // T1=Common, T2=Uncommon, T3=Rare, T4=Epic, T5=Legendary, T6=Mythic, T7=Mythic
            Rarity[] tierRarities = {
                Rarity.Common, Rarity.Uncommon, Rarity.Rare,
                Rarity.Epic, Rarity.Legendary, Rarity.Mythic, Rarity.Mythic
            };
            stone.rarity = tierRarities[tier - 1];

            AssetDatabase.CreateAsset(stone, assetPath);
            createdCount++;
            Debug.Log($"[RefinementStoneCreator] ✅ Created {assetName} (ID={stone.id}, tier={tier})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RefinementStoneCreator] Done! Created {createdCount} new Refinement Stones. " +
                  "Note: Assign icons in Inspector for each stone.");
    }
}
