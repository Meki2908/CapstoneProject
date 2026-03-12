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

    [Serializable]
    private class EquipmentSlots
    {
        public int[] slotItemIds = new int[NUM_SLOTS] { -1, -1, -1, -1 };
        public int[] slotRarities = new int[NUM_SLOTS] { 0, 0, 0, 0 }; // Rarity enum as int
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

        // Return existing item to inventory
        int currentId = equipmentSlots.slotItemIds[slotIndex];
        if (currentId >= 0)
        {
            Rarity currentRarity = (Rarity)equipmentSlots.slotRarities[slotIndex];
            InventoryManager.Instance.AddItem(currentId, 1, currentRarity);
        }

        // Consume and equip
        bool removed = InventoryManager.Instance.RemoveItem(equipmentItem.id, 1, rarity);
        if (!removed) return false;

        equipmentSlots.slotItemIds[slotIndex] = equipmentItem.id;
        equipmentSlots.slotRarities[slotIndex] = (int)rarity;
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

        Rarity currentRarity = (Rarity)equipmentSlots.slotRarities[slotIndex];
        InventoryManager.Instance.AddItem(currentId, 1, currentRarity);
        equipmentSlots.slotItemIds[slotIndex] = -1;
        equipmentSlots.slotRarities[slotIndex] = 0;
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
                InventoryManager.Instance.AddItem(id, 1, r);
                equipmentSlots.slotItemIds[i] = -1;
                equipmentSlots.slotRarities[i] = 0;
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

    // === STAT GETTERS — dùng runtime rarity ===

    public float GetTotalHPBonus()
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var item = GetEquippedItemByIndex(i);
            if (item != null)
            {
                Rarity r = GetEquippedRarity(i);
                total += item.ScaledHPBonus(r);
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
                total += item.ScaledCritRateBonus(r);
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
                total += (item.ScaledCritDamageMultiplier(r) - 1f);
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
                total += item.ScaledMovementSpeedBonus(r);
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
                total += item.ScaledAttackSpeedBonus(r);
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
                total += item.ScaledDefenseBonus(r);
            }
        }
        return total;
    }
}
