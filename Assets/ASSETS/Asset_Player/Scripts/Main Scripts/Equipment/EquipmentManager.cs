using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages equipped items per slot and provides stat bonuses.
/// Persists to JSON.
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
        public int[] slotItemIds = new int[NUM_SLOTS] { -1, -1, -1, -1 }; // -1 = empty
    }

    [Serializable]
    private class EquipmentSave
    {
        public EquipmentSlots slots;
    }

    // Runtime storage
    private EquipmentSlots equipmentSlots;

    // Events
    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
            Debug.Log($"[EquipmentManager] Saved equipment to {GetSavePath()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[EquipmentManager] Failed to save: {e.Message}");
        }
    }

    public void Load()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.Log($"[EquipmentManager] No save found at {path} (starting fresh)");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<EquipmentSave>(json);
            if (data != null && data.slots != null)
            {
                equipmentSlots = data.slots;
                Debug.Log($"[EquipmentManager] Loaded equipment from {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[EquipmentManager] Failed to load: {e.Message}");
        }
    }

    /// <summary>
    /// Get equipped item at a specific slot (by slot type - legacy)
    /// </summary>
    public Item GetEquippedItem(EquipmentSlotType slotType)
    {
        int slotIndex = (int)slotType;
        return GetEquippedItemByIndex(slotIndex);
    }

    /// <summary>
    /// Get equipped item at a specific slot (by index)
    /// </summary>
    public Item GetEquippedItemByIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return null;

        int itemId = equipmentSlots.slotItemIds[slotIndex];
        if (itemId < 0) return null;

        return InventoryManager.Instance?.GetItemById(itemId);
    }

    /// <summary>
    /// Equip an item into a specific slot (by slot type - legacy)
    /// </summary>
    public bool EquipItem(EquipmentSlotType slotType, Item equipmentItem)
    {
        int slotIndex = (int)slotType;
        return EquipItemByIndex(slotIndex, equipmentItem);
    }

    /// <summary>
    /// Equip an item into a specific slot (by index) - any equipment can go into any slot
    /// </summary>
    public bool EquipItemByIndex(int slotIndex, Item equipmentItem)
    {
        if (equipmentItem == null || equipmentItem.itemType != ItemType.Equipment)
        {
            Debug.LogWarning("[EquipmentManager] EquipItemByIndex invalid args");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= NUM_SLOTS)
        {
            Debug.LogWarning($"[EquipmentManager] EquipItemByIndex invalid slot index: {slotIndex}");
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[EquipmentManager] EquipItem failed: InventoryManager.Instance is null");
            return false;
        }

        // Must own the item in inventory
        if (InventoryManager.Instance.GetItemAmount(equipmentItem.id) <= 0)
        {
            Debug.LogWarning($"[EquipmentManager] EquipItem failed: No item id {equipmentItem.id} in inventory");
            return false;
        }

        int currentId = equipmentSlots.slotItemIds[slotIndex];

        // Return existing item to inventory
        if (currentId >= 0)
        {
            InventoryManager.Instance.AddItem(currentId, 1, 1f);
        }

        // Consume one from inventory and equip
        bool removed = InventoryManager.Instance.RemoveItem(equipmentItem.id, 1);
        if (!removed)
        {
            Debug.LogWarning("[EquipmentManager] EquipItem failed: could not remove from inventory");
            return false;
        }

        equipmentSlots.slotItemIds[slotIndex] = equipmentItem.id;
        Save();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Remove item from a specific slot (by slot type - legacy)
    /// </summary>
    public bool RemoveItem(EquipmentSlotType slotType)
    {
        int slotIndex = (int)slotType;
        return RemoveItemByIndex(slotIndex);
    }

    /// <summary>
    /// Remove item from a specific slot (by index)
    /// </summary>
    public bool RemoveItemByIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return false;
        if (InventoryManager.Instance == null) return false;

        int currentId = equipmentSlots.slotItemIds[slotIndex];
        if (currentId < 0) return false;

        InventoryManager.Instance.AddItem(currentId, 1, 1f);
        equipmentSlots.slotItemIds[slotIndex] = -1;
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
                InventoryManager.Instance.AddItem(id, 1, 1f);
                equipmentSlots.slotItemIds[i] = -1;
            }
        }
        Save();
        OnEquipmentChanged?.Invoke();
    }

    public void EquipBest()
    {
        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.GetAllItems(); // (Item, amount)
        if (items == null || items.Count == 0) return;

        // Get all equipment items
        List<Item> equipmentItems = new List<Item>();
        foreach (var (item, amount) in items)
        {
            if (item == null || amount <= 0) continue;
            if (item.itemType != ItemType.Equipment) continue;
            equipmentItems.Add(item);
        }

        if (equipmentItems.Count == 0) return;

        // Sort by rarity (descending), then by total stat value
        equipmentItems.Sort((a, b) =>
        {
            int rarityCompare = b.rarity.CompareTo(a.rarity);
            if (rarityCompare != 0) return rarityCompare;

            // Calculate total stat value
            float aValue = a.hpBonus + a.defenseBonus + (a.critRateBonus * 100f) + ((a.critDamageMultiplier - 1f) * 100f) + (a.movementSpeedBonus * 100f) + (a.attackSpeedBonus * 100f);
            float bValue = b.hpBonus + b.defenseBonus + (b.critRateBonus * 100f) + ((b.critDamageMultiplier - 1f) * 100f) + (b.movementSpeedBonus * 100f) + (b.attackSpeedBonus * 100f);
            return bValue.CompareTo(aValue);
        });

        // Equip best items to each slot (any equipment can go into any slot)
        for (int i = 0; i < NUM_SLOTS && i < equipmentItems.Count; i++)
        {
            EquipItemByIndex(i, equipmentItems[i]);
        }
    }

    // Stat getters - sum all equipped items
    public float GetTotalHPBonus()
    {
        float total = 0f;
        foreach (var slotType in Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>())
        {
            var item = GetEquippedItem(slotType);
            if (item != null)
            {
                total += item.hpBonus;
            }
        }
        return total;
    }

    public float GetTotalCritRateBonus()
    {
        float total = 0f;
        foreach (var slotType in Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>())
        {
            var item = GetEquippedItem(slotType);
            if (item != null)
            {
                total += item.critRateBonus;
            }
        }
        return Mathf.Clamp01(total); // Clamp to 0-1 (0-100%)
    }

    public float GetTotalCritDamageMultiplier()
    {
        float total = 1f; // Start at 1.0 (100%)
        foreach (var slotType in Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>())
        {
            var item = GetEquippedItem(slotType);
            if (item != null)
            {
                total += (item.critDamageMultiplier - 1f); // Add bonus (e.g., 1.5 -> +0.5)
            }
        }
        return Mathf.Max(1f, total); // Minimum 1.0 (100%)
    }

    public float GetTotalMovementSpeedBonus()
    {
        float total = 0f;
        foreach (var slotType in Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>())
        {
            var item = GetEquippedItem(slotType);
            if (item != null)
            {
                total += item.movementSpeedBonus;
            }
        }
        return total;
    }

    public float GetTotalAttackSpeedBonus()
    {
        float total = 0f;
        foreach (var slotType in Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>())
        {
            var item = GetEquippedItem(slotType);
            if (item != null)
            {
                total += item.attackSpeedBonus;
            }
        }
        return total;
    }

    public float GetTotalDefenseBonus()
    {
        float total = 0f;
        foreach (var slotType in Enum.GetValues(typeof(EquipmentSlotType)).Cast<EquipmentSlotType>())
        {
            var item = GetEquippedItem(slotType);
            if (item != null)
            {
                total += item.defenseBonus;
            }
        }
        return total;
    }
}

