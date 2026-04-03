using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to automatically set gem values based on rarity
/// </summary>
public class GemValueEditor : EditorWindow
{
    [MenuItem("Tools/Gem Value Editor")]
    public static void ShowWindow()
    {
        GetWindow<GemValueEditor>("Gem Value Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Gem Value Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "This tool will automatically set gemValuePercent for all gem items based on their rarity.\n" +
            "Values:\n" +
            "Common: Speed 1-2%, CD 5-8%, Dmg 8-12%\n" +
            "Uncommon: Speed 2-5%, CD 8-12%, Dmg 12-18%\n" +
            "Rare: Speed 3-7%, CD 10-15%, Dmg 15-22%\n" +
            "Epic: Speed 5-10%, CD 15-20%, Dmg 25-30%\n" +
            "Legendary: Speed 15-25%, CD 30-40%, Dmg 40-50%\n" +
            "Mythic: Speed 25-35%, CD 45-55%, Dmg 55-70%",
            MessageType.Info
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Set All Gem Values (Random within range)"))
        {
            SetAllGemValues(true);
        }

        if (GUILayout.Button("Auto-Set All Gem Values (Max values)"))
        {
            SetAllGemValues(false);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Random: Sets a random value within the range for each gem.\n" +
            "Max: Sets the maximum value for each gem (for testing).",
            MessageType.Info
        );
    }

    private void SetAllGemValues(bool useRandom)
    {
        string[] guids = AssetDatabase.FindAssets("t:Item");

        int gemsUpdated = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Item item = AssetDatabase.LoadAssetAtPath<Item>(path);

            if (item != null && item.itemType == ItemType.Gems)
            {
                float value = 0f;
                
                if (useRandom)
                {
                    value = Item.GetGemValueByRarity(item.rarity, item.gemType);
                }
                else
                {
                    // Set to max value
                    switch (item.rarity)
                    {
                        case Rarity.Common:
                            switch (item.gemType)
                            {
                                case GemType.MovementSpeed: value = 0.02f; break;
                                case GemType.CooldownReduction: value = 0.08f; break;
                                case GemType.Damage: value = 0.12f; break;
                            }
                            break;
                        case Rarity.Uncommon:
                            switch (item.gemType)
                            {
                                case GemType.MovementSpeed: value = 0.05f; break;
                                case GemType.CooldownReduction: value = 0.12f; break;
                                case GemType.Damage: value = 0.18f; break;
                            }
                            break;
                        case Rarity.Rare:
                            switch (item.gemType)
                            {
                                case GemType.MovementSpeed: value = 0.07f; break;
                                case GemType.CooldownReduction: value = 0.15f; break;
                                case GemType.Damage: value = 0.22f; break;
                            }
                            break;
                        case Rarity.Epic:
                            switch (item.gemType)
                            {
                                case GemType.MovementSpeed: value = 0.10f; break;
                                case GemType.CooldownReduction: value = 0.20f; break;
                                case GemType.Damage: value = 0.30f; break;
                            }
                            break;
                        case Rarity.Legendary:
                            switch (item.gemType)
                            {
                                case GemType.MovementSpeed: value = 0.25f; break;
                                case GemType.CooldownReduction: value = 0.40f; break;
                                case GemType.Damage: value = 0.50f; break;
                            }
                            break;
                        case Rarity.Mythic:
                            switch (item.gemType)
                            {
                                case GemType.MovementSpeed: value = 0.35f; break;
                                case GemType.CooldownReduction: value = 0.55f; break;
                                case GemType.Damage: value = 0.70f; break;
                            }
                            break;
                    }
                }

                item.gemValuePercent = value;
                EditorUtility.SetDirty(item);
                gemsUpdated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", $"Updated {gemsUpdated} gem items!", "OK");
        Debug.Log($"[GemValueEditor] Updated {gemsUpdated} gem items with {(useRandom ? "random" : "max")} values");
    }
}

