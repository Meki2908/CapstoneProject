using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages equipped items per slot and provides stat bonuses.
/// Persists to JSON. Hỗ trợ Runtime Rarity.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "equipment.json";

    private const int NUM_SLOTS = 4; // Head, Body, Legs, Accessory
    private const int NUM_GEM_SLOTS = 4; // 4 gem slots per equipment piece

    [Serializable]
    private class EquipmentSlots
    {
        public int[] slotItemIds = new int[NUM_SLOTS] { -1, -1, -1, -1 };
        public int[] slotRarities = new int[NUM_SLOTS] { 0, 0, 0, 0 }; // Rarity enum as int
        public float[] equipStatRolls = new float[NUM_SLOTS] { 1f, 1f, 1f, 1f }; // NEW: rolled stat multiplier per equipment
        // Gem slots: [equipSlot][gemSlot] — flat array for JSON serialization
        public int[] gemSlotIds = new int[NUM_SLOTS * NUM_GEM_SLOTS]; // -1 = empty
        public float[] gemRolledValues = new float[NUM_SLOTS * NUM_GEM_SLOTS]; // NEW: rolled gem values
    }

    [Serializable]
    private class EquipmentSave
    {
        public EquipmentSlots slots;
    }

    private EquipmentSlots equipmentSlots;

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            InitializeRuntime();
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeRuntime()
    {
        equipmentSlots = new EquipmentSlots();
        // Initialize gem slots to -1 (empty)
        for (int i = 0; i < equipmentSlots.gemSlotIds.Length; i++)
        {
            equipmentSlots.gemSlotIds[i] = -1;
            equipmentSlots.gemRolledValues[i] = 0f;
        }
        // Initialize stat rolls to 1.0 (100%)
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            equipmentSlots.equipStatRolls[i] = 1f;
        }
    }

    private string GetSavePath() => Path.Combine(Application.persistentDataPath, saveFileName);

    public void Save()
    {
        try
        {
            var data = new EquipmentSave { slots = equipmentSlots };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSavePath(), json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EquipmentManager] Failed to save: {e.Message}");
        }
    }

    public void Load()
    {
        string path = GetSavePath();
        if (!File.Exists(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<EquipmentSave>(json);
            if (data != null && data.slots != null)
            {
                equipmentSlots = data.slots;
                // Ensure slotRarities array exists (backward compat with old saves)
                if (equipmentSlots.slotRarities == null || equipmentSlots.slotRarities.Length != NUM_SLOTS)
                {
                    equipmentSlots.slotRarities = new int[NUM_SLOTS];
                }
                // Ensure equipStatRolls array exists (backward compat)
                if (equipmentSlots.equipStatRolls == null || equipmentSlots.equipStatRolls.Length != NUM_SLOTS)
                {
                    equipmentSlots.equipStatRolls = new float[NUM_SLOTS] { 1f, 1f, 1f, 1f };
                }
                // Ensure gemRolledValues array exists (backward compat)
                if (equipmentSlots.gemRolledValues == null || equipmentSlots.gemRolledValues.Length != NUM_SLOTS * NUM_GEM_SLOTS)
                {
                    equipmentSlots.gemRolledValues = new float[NUM_SLOTS * NUM_GEM_SLOTS];
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EquipmentManager] Failed to load: {e.Message}");
        }
    }

    // === GET EQUIPPED ITEM ===

    public Item GetEquippedItem(EquipmentSlotType slotType)
    {
        return GetEquippedItemByIndex((int)slotType);
    }

    public Item GetEquippedItemByIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return null;
        int itemId = equipmentSlots.slotItemIds[slotIndex];
        if (itemId < 0) return null;
        return InventoryManager.Instance?.GetItemById(itemId);
    }

    /// <summary>
    /// Get runtime rarity of equipped item at slot
    /// </summary>
    public Rarity GetEquippedRarity(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return Rarity.Common;
        return (Rarity)equipmentSlots.slotRarities[slotIndex];
    }

    public Rarity GetEquippedRarity(EquipmentSlotType slotType)
    {
        return GetEquippedRarity((int)slotType);
    }

    // === EQUIP / UNEQUIP ===

    public bool EquipItem(EquipmentSlotType slotType, Item equipmentItem)
    {
        return EquipItemByIndex((int)slotType, equipmentItem);
    }

    /// <summary>
    /// Equip item với runtime rarity
    /// </summary>
    public bool EquipItemByIndex(int slotIndex, Item equipmentItem, Rarity rarity)
    {
        if (equipmentItem == null || equipmentItem.itemType != ItemType.Equipment) return false;
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return false;
        if (InventoryManager.Instance == null) return false;

        if (InventoryManager.Instance.GetItemAmount(equipmentItem.id, rarity) <= 0)
        {
            Debug.LogWarning($"[EquipmentManager] No {equipmentItem.itemName} [{rarity}] in inventory");
            return false;
        }

        // Return existing item to inventory (PRESERVE its rolled value)
        int currentId = equipmentSlots.slotItemIds[slotIndex];
        if (currentId >= 0)
        {
            Rarity currentRarity = (Rarity)equipmentSlots.slotRarities[slotIndex];
            float currentRoll = equipmentSlots.equipStatRolls[slotIndex];
            InventoryManager.Instance.AddItemWithRoll(currentId, 1, currentRarity, currentRoll);
        }

        // Consume and equip
        bool removed = InventoryManager.Instance.RemoveItem(equipmentItem.id, 1, rarity);
        if (!removed) return false;

        // Get the rolled stat multiplier from inventory queue
        float rolledMultiplier = InventoryManager.Instance.LastRemovedRoll;
        if (rolledMultiplier < 0f) rolledMultiplier = 1f; // fallback to 100%

        equipmentSlots.slotItemIds[slotIndex] = equipmentItem.id;
        equipmentSlots.slotRarities[slotIndex] = (int)rarity;
        equipmentSlots.equipStatRolls[slotIndex] = rolledMultiplier;

        Debug.Log($"[EquipmentManager] Equipped {equipmentItem.itemName} [{rarity}] with stat roll {rolledMultiplier:F2} ({rolledMultiplier*100f:F0}%)");

        Save();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Equip item — backward compat (dùng SO rarity)
    /// </summary>
    public bool EquipItemByIndex(int slotIndex, Item equipmentItem)
    {
        Rarity r = equipmentItem != null ? equipmentItem.rarity : Rarity.Common;
        return EquipItemByIndex(slotIndex, equipmentItem, r);
    }

    public bool RemoveItem(EquipmentSlotType slotType)
    {
        return RemoveItemByIndex((int)slotType);
    }

    public bool RemoveItemByIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return false;
        if (InventoryManager.Instance == null) return false;

        int currentId = equipmentSlots.slotItemIds[slotIndex];
        if (currentId < 0) return false;

        // PRESERVE rolled value when returning to inventory
        Rarity currentRarity = (Rarity)equipmentSlots.slotRarities[slotIndex];
        float currentRoll = equipmentSlots.equipStatRolls[slotIndex];
        InventoryManager.Instance.AddItemWithRoll(currentId, 1, currentRarity, currentRoll);

        equipmentSlots.slotItemIds[slotIndex] = -1;
        equipmentSlots.slotRarities[slotIndex] = 0;
        equipmentSlots.equipStatRolls[slotIndex] = 1f;
        Save();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public int[] GetEquippedItemIds()
    {
        return equipmentSlots.slotItemIds;
    }

    public void RemoveAll()
    {
        if (InventoryManager.Instance == null) return;

        for (int i = 0; i < NUM_SLOTS; i++)
        {
            int id = equipmentSlots.slotItemIds[i];
            if (id >= 0)
            {
                Rarity r = (Rarity)equipmentSlots.slotRarities[i];
                float roll = equipmentSlots.equipStatRolls[i];
                InventoryManager.Instance.AddItemWithRoll(id, 1, r, roll);
                equipmentSlots.slotItemIds[i] = -1;
                equipmentSlots.slotRarities[i] = 0;
                equipmentSlots.equipStatRolls[i] = 1f;
            }
        }
        Save();
        OnEquipmentChanged?.Invoke();
    }

    public void EquipBest()
    {
        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.GetAllItemsWithRarity();
        if (items == null || items.Count == 0) return;

        // Get all equipment items with their rarity
        var equipList = new List<(Item item, Rarity rarity)>();
        foreach (var (item, amount, rarity) in items)
        {
            if (item == null || amount <= 0) continue;
            if (item.itemType != ItemType.Equipment) continue;
            equipList.Add((item, rarity));
        }

        if (equipList.Count == 0) return;

        // Sort by rarity (descending), then by scaled stat value
        equipList.Sort((a, b) =>
        {
            int rarityCompare = b.rarity.CompareTo(a.rarity);
            if (rarityCompare != 0) return rarityCompare;

            float aValue = a.item.ScaledHPBonus(a.rarity) + a.item.ScaledDefenseBonus(a.rarity);
            float bValue = b.item.ScaledHPBonus(b.rarity) + b.item.ScaledDefenseBonus(b.rarity);
            return bValue.CompareTo(aValue);
        });

        for (int i = 0; i < NUM_SLOTS && i < equipList.Count; i++)
        {
            EquipItemByIndex(i, equipList[i].item, equipList[i].rarity);
        }
    }

    // === STAT GETTERS — dùng runtime rarity × rolled multiplier ===

    /// <summary>
    /// Get the rolled stat multiplier for a specific equipment slot
    /// </summary>
    public float GetEquipStatRoll(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return 1f;
        return equipmentSlots.equipStatRolls[slotIndex];
    }

    public float GetTotalHPBonus()
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var item = GetEquippedItemByIndex(i);
            if (item != null)
            {
                Rarity r = GetEquippedRarity(i);
                total += item.ScaledHPBonus(r) * equipmentSlots.equipStatRolls[i];
            }
        }
        return total;
    }

    public float GetTotalCritRateBonus()
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var item = GetEquippedItemByIndex(i);
            if (item != null)
            {
                Rarity r = GetEquippedRarity(i);
                total += item.ScaledCritRateBonus(r) * equipmentSlots.equipStatRolls[i];
            }
        }
        return Mathf.Clamp01(total);
    }

    public float GetTotalCritDamageMultiplier()
    {
        float total = 1f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var item = GetEquippedItemByIndex(i);
            if (item != null)
            {
                Rarity r = GetEquippedRarity(i);
                total += (item.ScaledCritDamageMultiplier(r) - 1f) * equipmentSlots.equipStatRolls[i];
            }
        }
        return Mathf.Max(1f, total);
    }

    public float GetTotalMovementSpeedBonus()
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var item = GetEquippedItemByIndex(i);
            if (item != null)
            {
                Rarity r = GetEquippedRarity(i);
                total += item.ScaledMovementSpeedBonus(r) * equipmentSlots.equipStatRolls[i];
            }
        }
        return total;
    }

    public float GetTotalAttackSpeedBonus()
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var item = GetEquippedItemByIndex(i);
            if (item != null)
            {
                Rarity r = GetEquippedRarity(i);
                total += item.ScaledAttackSpeedBonus(r) * equipmentSlots.equipStatRolls[i];
            }
        }
        return total;
    }

    public float GetTotalDefenseBonus()
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var item = GetEquippedItemByIndex(i);
            if (item != null)
            {
                Rarity r = GetEquippedRarity(i);
                total += item.ScaledDefenseBonus(r) * equipmentSlots.equipStatRolls[i];
            }
        }
        return total;
    }

    // ================================================================
    // EQUIPMENT GEM SLOTS (4 gem slots per equipment piece)
    // ================================================================

    private int GemIndex(int equipSlot, int gemSlot) => equipSlot * NUM_GEM_SLOTS + gemSlot;

    /// <summary>
    /// Get equipped gem at a specific slot
    /// </summary>
    public Item GetEquippedGem(int equipSlotIndex, int gemSlotIndex)
    {
        if (equipSlotIndex < 0 || equipSlotIndex >= NUM_SLOTS) return null;
        if (gemSlotIndex < 0 || gemSlotIndex >= NUM_GEM_SLOTS) return null;
        int idx = GemIndex(equipSlotIndex, gemSlotIndex);
        if (idx >= equipmentSlots.gemSlotIds.Length) return null;
        int itemId = equipmentSlots.gemSlotIds[idx];
        if (itemId < 0) return null;
        return InventoryManager.Instance?.GetItemById(itemId);
    }

    /// <summary>
    /// Equip gem into equipment slot — called by SocketingManager
    /// </summary>
    public bool EquipGemToSlot(int equipSlotIndex, int gemSlotIndex, Item gemItem)
    {
        if (gemItem == null || gemItem.itemType != ItemType.Gems) return false;
        if (equipSlotIndex < 0 || equipSlotIndex >= NUM_SLOTS) return false;
        if (gemSlotIndex < 0 || gemSlotIndex >= NUM_GEM_SLOTS) return false;
        if (InventoryManager.Instance == null) return false;

        // Check equipment is equipped
        if (equipmentSlots.slotItemIds[equipSlotIndex] < 0)
        {
            Debug.LogWarning($"[EquipmentManager] Cannot socket gem: no equipment in slot {equipSlotIndex}");
            return false;
        }

        // Check inventory has the gem
        if (InventoryManager.Instance.GetItemAmount(gemItem.id) <= 0)
        {
            Debug.LogWarning($"[EquipmentManager] No {gemItem.itemName} in inventory");
            return false;
        }

        int idx = GemIndex(equipSlotIndex, gemSlotIndex);

        // Return existing gem to inventory (PRESERVE its rolled value)
        int currentGemId = equipmentSlots.gemSlotIds[idx];
        if (currentGemId >= 0)
        {
            float existingRoll = equipmentSlots.gemRolledValues[idx];
            Item existingGem = InventoryManager.Instance.GetItemById(currentGemId);
            Rarity gemRarity = (existingGem != null) ? existingGem.rarity : Rarity.Common;
            InventoryManager.Instance.AddItemWithRoll(currentGemId, 1, gemRarity, existingRoll);
        }

        // Consume gem from inventory and equip
        bool removed = InventoryManager.Instance.RemoveItem(gemItem.id, 1);
        if (!removed) return false;

        // Get the rolled value from inventory queue
        float rolledValue = InventoryManager.Instance.LastRemovedRoll;
        if (rolledValue < 0f) rolledValue = gemItem.gemValuePercent; // fallback

        equipmentSlots.gemSlotIds[idx] = gemItem.id;
        equipmentSlots.gemRolledValues[idx] = rolledValue;
        Save();
        OnEquipmentChanged?.Invoke();
        Debug.Log($"[EquipmentManager] Socketed {gemItem.itemName} (rolled: {rolledValue*100f:F1}%) into equipment slot {equipSlotIndex} gem {gemSlotIndex}");
        return true;
    }

    /// <summary>
    /// Remove gem from equipment slot
    /// </summary>
    public bool RemoveGemFromSlot(int equipSlotIndex, int gemSlotIndex)
    {
        if (equipSlotIndex < 0 || equipSlotIndex >= NUM_SLOTS) return false;
        if (gemSlotIndex < 0 || gemSlotIndex >= NUM_GEM_SLOTS) return false;
        if (InventoryManager.Instance == null) return false;

        int idx = GemIndex(equipSlotIndex, gemSlotIndex);
        int currentGemId = equipmentSlots.gemSlotIds[idx];
        if (currentGemId < 0) return false;

        // Gem is DESTROYED when removed from equipment (not returned to inventory)
        Debug.Log($"[EquipmentManager] Gem id={currentGemId} destroyed on removal from equipment slot {equipSlotIndex} gem {gemSlotIndex}");

        equipmentSlots.gemSlotIds[idx] = -1;
        equipmentSlots.gemRolledValues[idx] = 0f;
        Save();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Remove all gems from an equipment slot
    /// </summary>
    public void RemoveAllGemsFromSlot(int equipSlotIndex)
    {
        if (equipSlotIndex < 0 || equipSlotIndex >= NUM_SLOTS) return;
        for (int g = 0; g < NUM_GEM_SLOTS; g++)
        {
            RemoveGemFromSlot(equipSlotIndex, g);
        }
    }

    /// <summary>
    /// Get gem stat multipliers from all equipment gems
    /// </summary>
    public float GetTotalGemMovementSpeedBonus()
    {
        float total = 0f;
        for (int e = 0; e < NUM_SLOTS; e++)
        {
            for (int g = 0; g < NUM_GEM_SLOTS; g++)
            {
                var gem = GetEquippedGem(e, g);
                if (gem != null && gem.gemType == GemType.MovementSpeed)
                    total += equipmentSlots.gemRolledValues[GemIndex(e, g)];
            }
        }
        return total;
    }

    public float GetTotalGemCooldownReduction()
    {
        float total = 0f;
        for (int e = 0; e < NUM_SLOTS; e++)
        {
            for (int g = 0; g < NUM_GEM_SLOTS; g++)
            {
                var gem = GetEquippedGem(e, g);
                if (gem != null && gem.gemType == GemType.CooldownReduction)
                    total += equipmentSlots.gemRolledValues[GemIndex(e, g)];
            }
        }
        return total;
    }

    public float GetTotalGemDamageBonus()
    {
        float total = 0f;
        for (int e = 0; e < NUM_SLOTS; e++)
        {
            for (int g = 0; g < NUM_GEM_SLOTS; g++)
            {
                var gem = GetEquippedGem(e, g);
                if (gem != null && gem.gemType == GemType.Damage)
                    total += equipmentSlots.gemRolledValues[GemIndex(e, g)];
            }
        }
        return total;
    }

    /// <summary>
    /// Get rolled gem value for a specific equipment gem slot
    /// </summary>
    public float GetRolledGemValue(int equipSlotIndex, int gemSlotIndex)
    {
        if (equipSlotIndex < 0 || equipSlotIndex >= NUM_SLOTS) return 0f;
        if (gemSlotIndex < 0 || gemSlotIndex >= NUM_GEM_SLOTS) return 0f;
        return equipmentSlots.gemRolledValues[GemIndex(equipSlotIndex, gemSlotIndex)];
    }

    /// <summary>
    /// Number of gem slots per equipment piece
    /// </summary>
    public int GetNumGemSlots() => NUM_GEM_SLOTS;
}
