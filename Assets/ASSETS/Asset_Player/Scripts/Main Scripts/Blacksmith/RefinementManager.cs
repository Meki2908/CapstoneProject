using System;
using UnityEngine;

/// <summary>
/// Kết quả tinh luyện
/// </summary>
public enum RefinementResult
{
    Success,        // Thành công — level tăng, stone tiêu thụ
    Fail,           // Thất bại — chỉ mất stone, level giữ nguyên
    MaxLevel,       // Đã +7 rồi
    NoEquipment,    // Slot trống
    NoStone,        // Không có refinement stone
    InvalidSlot     // Slot không hợp lệ
}

/// <summary>
/// Manages equipment refinement (+0 → +7) and stone fusion (4:1).
/// Success rate depends on current enhancement level × stone tier.
/// On failure: only stone consumed, level stays the same.
/// </summary>
public class RefinementManager : MonoBehaviour
{
    public static RefinementManager Instance { get; private set; }

    // Events
    public event Action<RefinementResult, int, int> OnRefineAttempt; // result, slotIndex, newLevel

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================================================================
    // SUCCESS RATE TABLE (7 levels × 7 stone tiers)
    // Row = current level (0→1 through 6→7)
    // Col = stone tier (1-7)
    // ================================================================
    private static readonly float[,] RefineRateTable = new float[7, 7]
    {
        //              T1     T2     T3     T4     T5     T6     T7
        /* +0→+1 */ { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f },
        /* +1→+2 */ { 0.90f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f },
        /* +2→+3 */ { 0.70f, 0.85f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f },
        /* +3→+4 */ { 0.40f, 0.55f, 0.70f, 0.85f, 0.95f, 1.00f, 1.00f },
        /* +4→+5 */ { 0.25f, 0.35f, 0.50f, 0.65f, 0.80f, 0.95f, 1.00f },
        /* +5→+6 */ { 0.10f, 0.20f, 0.35f, 0.50f, 0.65f, 0.80f, 0.95f },
        /* +6→+7 */ { 0.05f, 0.10f, 0.20f, 0.35f, 0.50f, 0.70f, 0.90f },
    };

    /// <summary>
    /// Tính tỉ lệ thành công dựa trên level hiện tại và tier đá
    /// </summary>
    public float CalculateRefineRate(int currentLevel, int stoneTier)
    {
        int row = Mathf.Clamp(currentLevel, 0, 6);       // 0-6 (rows)
        int col = Mathf.Clamp(stoneTier - 1, 0, 6);      // tier 1-7 → index 0-6
        return RefineRateTable[row, col];
    }

    /// <summary>
    /// Lấy tier từ Item (refinementTier field)
    /// </summary>
    public int GetStoneTier(Item stone)
    {
        if (stone == null || stone.itemType != ItemType.Material) return 0;
        return stone.refinementTier;
    }

    /// <summary>
    /// Thử tinh luyện equipment tại slot
    /// </summary>
    public RefinementResult TryRefine(int equipSlotIndex, Item stone)
    {
        // Validate
        if (EquipmentManager.Instance == null || InventoryManager.Instance == null)
            return RefinementResult.InvalidSlot;
        if (equipSlotIndex < 0 || equipSlotIndex >= 4)
            return RefinementResult.InvalidSlot;

        // Check equipment exists in slot
        Item equippedItem = EquipmentManager.Instance.GetEquippedItemByIndex(equipSlotIndex);
        if (equippedItem == null)
            return RefinementResult.NoEquipment;

        // Check stone
        int tier = GetStoneTier(stone);
        if (stone == null || tier <= 0)
            return RefinementResult.NoStone;

        // Check current level
        int currentLevel = EquipmentManager.Instance.GetEnhancementLevel(equipSlotIndex);
        if (currentLevel >= EquipmentManager.MAX_ENHANCEMENT_LEVEL)
            return RefinementResult.MaxLevel;

        // Check inventory has stone
        if (InventoryManager.Instance.GetItemAmount(stone.id) <= 0)
            return RefinementResult.NoStone;

        // Calculate success rate
        float rate = CalculateRefineRate(currentLevel, tier);

        // Consume stone
        InventoryManager.Instance.RemoveItem(stone.id, 1);

        // Roll
        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll <= rate)
        {
            // SUCCESS — level up
            int newLevel = currentLevel + 1;
            EquipmentManager.Instance.SetEnhancementLevel(equipSlotIndex, newLevel);
            Debug.Log($"[RefinementManager] SUCCESS! {equippedItem.itemName} +{currentLevel} → +{newLevel} (rate={rate:P0}, roll={roll:F2})");
            OnRefineAttempt?.Invoke(RefinementResult.Success, equipSlotIndex, newLevel);
            return RefinementResult.Success;
        }
        else
        {
            // FAIL — only stone consumed, level stays
            Debug.Log($"[RefinementManager] FAIL! {equippedItem.itemName} stays at +{currentLevel}. Stone consumed. (rate={rate:P0}, roll={roll:F2})");
            OnRefineAttempt?.Invoke(RefinementResult.Fail, equipSlotIndex, currentLevel);
            return RefinementResult.Fail;
        }
    }

    // ================================================================
    // STONE FUSION: 4 × Tier N → 1 × Tier N+1
    // ================================================================

    /// <summary>
    /// Check if fusion is possible for a given stone
    /// </summary>
    public bool CanFuse(Item stone)
    {
        if (stone == null || stone.refinementTier <= 0 || stone.refinementTier >= 7)
            return false;
        if (InventoryManager.Instance == null)
            return false;
        return InventoryManager.Instance.GetItemAmount(stone.id) >= 4;
    }

    /// <summary>
    /// Get the result stone item for fusion (tier + 1)
    /// </summary>
    public Item GetFusionResultStone(Item sourceStone)
    {
        if (sourceStone == null || sourceStone.refinementTier <= 0 || sourceStone.refinementTier >= 7)
            return null;

        int nextTier = sourceStone.refinementTier + 1;

        // Search all items in database for matching refinement tier
        var allItems = InventoryManager.Instance.GetAllItemsWithRarity();
        // Also search by ID pattern: if source is 401 (T1), result is 402 (T2)
        Item resultStone = InventoryManager.Instance.GetItemById(sourceStone.id + 1);
        if (resultStone != null && resultStone.refinementTier == nextTier)
            return resultStone;

        // Fallback: search all items for matching tier
        foreach (var (item, amount, rarity) in allItems)
        {
            if (item.refinementTier == nextTier && item.itemType == ItemType.Material)
                return item;
        }

        return null;
    }

    /// <summary>
    /// Execute fusion: 4 × sourceStone → 1 × higher tier stone
    /// </summary>
    public bool TryFuse(Item sourceStone)
    {
        if (!CanFuse(sourceStone)) return false;

        Item resultStone = GetFusionResultStone(sourceStone);
        if (resultStone == null)
        {
            Debug.LogError($"[RefinementManager] Cannot find Tier {sourceStone.refinementTier + 1} stone for fusion!");
            return false;
        }

        // Consume 4 source stones
        bool removed = InventoryManager.Instance.RemoveItem(sourceStone.id, 4);
        if (!removed) return false;

        // Add 1 result stone (use default rarity since materials don't have random rarity)
        InventoryManager.Instance.AddItem(resultStone.id, 1, resultStone.rarity);

        Debug.Log($"[RefinementManager] Fused 4× {sourceStone.itemName} → 1× {resultStone.itemName}");
        return true;
    }

    // ================================================================
    // UTILITY
    // ================================================================

    /// <summary>
    /// Lấy màu cho success rate bar
    /// </summary>
    public Color GetRefineRateColor(float rate)
    {
        if (rate >= 0.9f) return new Color(0.2f, 0.9f, 0.3f);    // Xanh lá
        if (rate >= 0.7f) return new Color(0.5f, 0.9f, 0.2f);    // Xanh nhạt
        if (rate >= 0.5f) return new Color(0.9f, 0.9f, 0.2f);    // Vàng
        if (rate >= 0.3f) return new Color(0.9f, 0.6f, 0.2f);    // Cam
        if (rate >= 0.1f) return new Color(0.9f, 0.3f, 0.2f);    // Đỏ cam
        return new Color(0.9f, 0.1f, 0.1f);                       // Đỏ đậm
    }

    /// <summary>
    /// Get star display for level: filled stars (gold) + empty stars (gray) = 7 total.
    /// Requires TMP Sprite Asset with "star" (gold) and "star_empty" (gray) sprites.
    /// Example: +3 = ★★★☆☆☆☆
    /// </summary>
    public string GetStarString(int level)
    {
        string result = "";
        for (int i = 0; i < MAX_LEVEL; i++)
        {
            if (i < level)
                result += "<sprite name=\"star\">";
            else
                result += "<sprite name=\"star_empty\">";
        }
        return result;
    }

    public const int MAX_LEVEL = 7;
}
