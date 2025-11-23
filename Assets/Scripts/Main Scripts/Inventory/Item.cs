using UnityEngine;

public enum ItemType { Armor, Consumable, Material, Gems }
public enum Rarity { Common, Epic, Legendary }
public enum GemType { MovementSpeed, CooldownReduction, Damage }

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public Rarity rarity;
    public bool isStackable;
    public int maxStackSize;
    public string description;
    public ItemType itemType;

    [Header("Gem Settings (used when ItemType = Gems)")]
    public GemType gemType;
    [Tooltip("Gem stat value (0.0-1.0). For MovementSpeed/Damage: percentage increase. For CooldownReduction: percentage decrease.")]
    [Range(0f, 1f)]
    public float gemValuePercent;

    /// <summary>
    /// Get gem stat value based on rarity and gem type
    /// Returns a random value within the range for the rarity
    /// </summary>
    public static float GetGemValueByRarity(Rarity rarity, GemType gemType)
    {
        float min = 0f, max = 0f;

        switch (rarity)
        {
            case Rarity.Common:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.01f; max = 0.02f; break; // 1-2%
                    case GemType.CooldownReduction: min = 0.05f; max = 0.10f; break; // 5-10%
                    case GemType.Damage: min = 0.10f; max = 0.15f; break; // 10-15%
                }
                break;
            case Rarity.Epic:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.05f; max = 0.10f; break; // 5-10%
                    case GemType.CooldownReduction: min = 0.15f; max = 0.20f; break; // 15-20%
                    case GemType.Damage: min = 0.25f; max = 0.30f; break; // 25-30%
                }
                break;
            case Rarity.Legendary:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.20f; max = 0.30f; break; // 20-30%
                    case GemType.CooldownReduction: min = 0.40f; max = 0.50f; break; // 40-50%
                    case GemType.Damage: min = 0.50f; max = 0.60f; break; // 50-60%
                }
                break;
        }

        return Random.Range(min, max);
    }

    /// <summary>
    /// Get formatted stat text for display
    /// </summary>
    public string GetGemStatText()
    {
        if (itemType != ItemType.Gems) return "";

        string statName = "";
        string sign = "";

        switch (gemType)
        {
            case GemType.MovementSpeed:
                statName = "Speed";
                sign = "+";
                break;
            case GemType.CooldownReduction:
                statName = "CD";
                sign = "-";
                break;
            case GemType.Damage:
                statName = "Dmg";
                sign = "+";
                break;
        }

        float percent = gemValuePercent * 100f;
        return $"{statName}: {sign}{percent:F1}%";
    }
}
