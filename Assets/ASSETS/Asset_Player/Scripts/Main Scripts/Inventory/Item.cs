using UnityEngine;

public enum ItemType { Equipment, Consumable, Material, Gems, CrystalStone }
public enum Rarity { None, Common, Uncommon, Rare, Epic, Legendary, Mythic }
public enum GemType { MovementSpeed, CooldownReduction, Damage }
public enum EquipmentSlotType { Head, Body, Legs, Accessory }

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    [Tooltip("Rarity mặc định của SO (dùng làm base). Runtime rarity có thể khác.")]
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

    [Header("Equipment Settings (used when ItemType = Equipment)")]
    [Tooltip("Which equipment slot this item can be equipped to")]
    public EquipmentSlotType equipmentSlot;

    [Header("Equipment Stats (giá trị Common làm chuẩn, tự nhân theo rarity)")]
    [Tooltip("HP bonus (flat value)")]
    public float hpBonus = 0f;
    [Tooltip("Defense bonus (flat value, reduces incoming damage)")]
    public float defenseBonus = 0f;
    [Tooltip("Critical Rate bonus (0.0-1.0, e.g., 0.15 = 15%)")]
    [Range(0f, 1f)]
    public float critRateBonus = 0f;
    [Tooltip("Critical Damage multiplier (1.0 = 100%, 1.5 = 150%, etc.)")]
    [Range(1f, 5f)]
    public float critDamageMultiplier = 1f;
    [Tooltip("Movement Speed bonus (0.0-1.0, e.g., 0.1 = 10%)")]
    [Range(0f, 1f)]
    public float movementSpeedBonus = 0f;
    [Tooltip("Attack Speed bonus (0.0-1.0, e.g., 0.15 = 15%)")]
    [Range(0f, 1f)]
    public float attackSpeedBonus = 0f;

    [Header("Equipment Passive")]
    [Tooltip("Passive ability description")]
    [TextArea(2, 4)]
    public string passiveDescription = "";

    // ========================================
    // RARITY MULTIPLIER SYSTEM
    // Common=1x, Uncommon=1.5x, Epic=2x, Legendary=3x, Mythic=5x
    // ========================================

    /// <summary>
    /// Hệ số nhân stat theo rarity bất kỳ (dùng cho runtime rarity)
    /// </summary>
    public static float GetRarityMultiplier(Rarity r)
    {
        switch (r)
        {
            case Rarity.None:      return 1.0f;
            case Rarity.Common:    return 1.0f;
            case Rarity.Uncommon:  return 1.5f;
            case Rarity.Rare:      return 1.75f;
            case Rarity.Epic:      return 2.0f;
            case Rarity.Legendary: return 3.0f;
            case Rarity.Mythic:    return 5.0f;
            default:               return 1.0f;
        }
    }

    /// <summary>
    /// Hệ số nhân dựa trên rarity của SO này (backward compat)
    /// </summary>
    public float GetRarityMultiplier()
    {
        return GetRarityMultiplier(rarity);
    }

    /// <summary>
    /// Màu hex cho UI theo rarity
    /// </summary>
    public static string GetRarityColorHex(Rarity r)
    {
        switch (r)
        {
            case Rarity.None:      return "#AAAAAA"; // Xám (không có rarity)
            case Rarity.Common:    return "#FFFFFF"; // Trắng
            case Rarity.Uncommon:  return "#00FF00"; // Xanh lá
            case Rarity.Rare:      return "#3498DB"; // Xanh dương
            case Rarity.Epic:      return "#9B59B6"; // Tím
            case Rarity.Legendary: return "#FFD700"; // Vàng
            case Rarity.Mythic:    return "#FF4444"; // Đỏ
            default:               return "#FFFFFF";
        }
    }

    // --- Scaled Getters (dùng runtime rarity) ---
    public float ScaledHPBonus(Rarity r)              => hpBonus * GetRarityMultiplier(r);
    public float ScaledDefenseBonus(Rarity r)          => defenseBonus * GetRarityMultiplier(r);
    public float ScaledCritRateBonus(Rarity r)         => Mathf.Clamp01(critRateBonus * GetRarityMultiplier(r));
    public float ScaledCritDamageMultiplier(Rarity r)  => 1f + (critDamageMultiplier - 1f) * GetRarityMultiplier(r);
    public float ScaledMovementSpeedBonus(Rarity r)    => Mathf.Clamp01(movementSpeedBonus * GetRarityMultiplier(r));
    public float ScaledAttackSpeedBonus(Rarity r)      => Mathf.Clamp01(attackSpeedBonus * GetRarityMultiplier(r));

    // ========================================
    // GEM VALUE BY RARITY (5 cấp)
    // ========================================

    public static float GetGemValueByRarity(Rarity rarity, GemType gemType)
    {
        float min = 0f, max = 0f;

        switch (rarity)
        {
            case Rarity.Common:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.01f; max = 0.02f; break;
                    case GemType.CooldownReduction: min = 0.05f; max = 0.08f; break;
                    case GemType.Damage: min = 0.08f; max = 0.12f; break;
                }
                break;
            case Rarity.Uncommon:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.02f; max = 0.05f; break;
                    case GemType.CooldownReduction: min = 0.08f; max = 0.12f; break;
                    case GemType.Damage: min = 0.12f; max = 0.18f; break;
                }
                break;
            case Rarity.Rare:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.03f; max = 0.07f; break;
                    case GemType.CooldownReduction: min = 0.10f; max = 0.15f; break;
                    case GemType.Damage: min = 0.15f; max = 0.22f; break;
                }
                break;
            case Rarity.Epic:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.05f; max = 0.10f; break;
                    case GemType.CooldownReduction: min = 0.15f; max = 0.20f; break;
                    case GemType.Damage: min = 0.25f; max = 0.30f; break;
                }
                break;
            case Rarity.Legendary:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.15f; max = 0.25f; break;
                    case GemType.CooldownReduction: min = 0.30f; max = 0.40f; break;
                    case GemType.Damage: min = 0.40f; max = 0.50f; break;
                }
                break;
            case Rarity.Mythic:
                switch (gemType)
                {
                    case GemType.MovementSpeed: min = 0.25f; max = 0.35f; break;
                    case GemType.CooldownReduction: min = 0.45f; max = 0.55f; break;
                    case GemType.Damage: min = 0.55f; max = 0.70f; break;
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
