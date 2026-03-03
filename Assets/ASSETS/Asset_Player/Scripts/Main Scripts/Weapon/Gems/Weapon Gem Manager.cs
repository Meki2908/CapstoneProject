using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages gem sockets per weapon and provides stat multipliers.
/// Persists to JSON.
/// </summary>
public class WeaponGemManager : MonoBehaviour
{
    public static WeaponGemManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "weapon_gems.json";

    // 3 slots by GemType order: MovementSpeed, CooldownReduction, Damage
    private const int NUM_SLOTS = 3;

    [Serializable]
    private class WeaponGemSlots
    {
        public int[] slotItemIds = new int[NUM_SLOTS] { -1, -1, -1 }; // -1 = empty
    }

    [Serializable]
    private class WeaponGemSave
    {
        public WeaponGemSlots[] weapons; // Indexed by (int)WeaponType
    }

    // Runtime storage
    private WeaponGemSlots[] weaponSlots; // Indexed by WeaponType enum value

    // Events
    public event Action<WeaponType> OnGemsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ensure we're on a root GameObject before calling DontDestroyOnLoad
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
        // Ensure array large enough for all WeaponType values
        int maxWeaponIndex = Enum.GetValues(typeof(WeaponType)).Cast<int>().Max();
        weaponSlots = new WeaponGemSlots[maxWeaponIndex + 1];
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            weaponSlots[i] = new WeaponGemSlots();
        }
    }

    private string GetSavePath() => Path.Combine(Application.persistentDataPath, saveFileName);

    public void Save()
    {
        try
        {
            var data = new WeaponGemSave { weapons = weaponSlots };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(GetSavePath(), json);
            Debug.Log($"[WeaponGemManager] Saved gems to {GetSavePath()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[WeaponGemManager] Failed to save: {e.Message}");
        }
    }

    public void Load()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.Log($"[WeaponGemManager] No save found at {path} (starting fresh)");
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<WeaponGemSave>(json);
            if (data != null && data.weapons != null && data.weapons.Length == weaponSlots.Length)
            {
                weaponSlots = data.weapons;
                Debug.Log($"[WeaponGemManager] Loaded gems from {path}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[WeaponGemManager] Failed to load: {e.Message}");
        }
    }

    /// <summary>
    /// Get equipped gem at a specific slot index (0-2)
    /// </summary>
    public Item GetEquippedGem(WeaponType weaponType, int slotIndex)
    {
        if (!IsValidWeaponType(weaponType)) return null;
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return null;
        int itemId = weaponSlots[(int)weaponType].slotItemIds[slotIndex];
        if (itemId < 0) return null;
        return InventoryManager.Instance?.GetItemById(itemId);
    }

    /// <summary>
    /// Equip a gem into a specific slot (0-2). Any gem type can go into any slot.
    /// </summary>
    public bool EquipGem(WeaponType weaponType, int slotIndex, Item gemItem)
    {
        if (!IsValidWeaponType(weaponType) || gemItem == null || gemItem.itemType != ItemType.Gems)
        {
            Debug.LogWarning("[WeaponGemManager] EquipGem invalid args");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= NUM_SLOTS)
        {
            Debug.LogWarning($"[WeaponGemManager] EquipGem invalid slot index: {slotIndex}");
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[WeaponGemManager] EquipGem failed: InventoryManager.Instance is null");
            return false;
        }

        // Must own the gem in inventory
        if (InventoryManager.Instance.GetItemAmount(gemItem.id) <= 0)
        {
            Debug.LogWarning($"[WeaponGemManager] EquipGem failed: No gem id {gemItem.id} in inventory");
            return false;
        }

        int currentId = weaponSlots[(int)weaponType].slotItemIds[slotIndex];

        // Return existing gem to inventory
        if (currentId >= 0)
        {
            InventoryManager.Instance.AddItem(currentId, 1, 1f);
        }

        // Consume one from inventory and equip
        bool removed = InventoryManager.Instance.RemoveItem(gemItem.id, 1);
        if (!removed)
        {
            Debug.LogWarning("[WeaponGemManager] EquipGem failed: could not remove from inventory");
            return false;
        }

        weaponSlots[(int)weaponType].slotItemIds[slotIndex] = gemItem.id;
        Save();
        OnGemsChanged?.Invoke(weaponType);
        return true;
    }

    /// <summary>
    /// Remove gem from a specific slot (0-2)
    /// </summary>
    public bool RemoveGem(WeaponType weaponType, int slotIndex)
    {
        if (!IsValidWeaponType(weaponType)) return false;
        if (slotIndex < 0 || slotIndex >= NUM_SLOTS) return false;
        if (InventoryManager.Instance == null) return false;
        int currentId = weaponSlots[(int)weaponType].slotItemIds[slotIndex];
        if (currentId < 0) return false;
        InventoryManager.Instance.AddItem(currentId, 1, 1f);
        weaponSlots[(int)weaponType].slotItemIds[slotIndex] = -1;
        Save();
        OnGemsChanged?.Invoke(weaponType);
        return true;
    }

    public int[] GetEquippedGemIds(WeaponType weaponType)
    {
        if (!IsValidWeaponType(weaponType)) return new int[NUM_SLOTS] { -1, -1, -1 };
        return weaponSlots[(int)weaponType].slotItemIds;
    }

    public void RemoveAll(WeaponType weaponType)
    {
        if (!IsValidWeaponType(weaponType)) return;
        if (InventoryManager.Instance == null) return;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            int id = weaponSlots[(int)weaponType].slotItemIds[i];
            if (id >= 0)
            {
                InventoryManager.Instance.AddItem(id, 1, 1f);
                weaponSlots[(int)weaponType].slotItemIds[i] = -1;
            }
        }
        Save();
        OnGemsChanged?.Invoke(weaponType);
    }

    public void EquipBest(WeaponType weaponType)
    {
        if (!IsValidWeaponType(weaponType)) return;
        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.GetAllItems(); // (Item, amount)
        if (items == null || items.Count == 0) return;

        // Find top 3 best gems (by gemValuePercent, tie-break by rarity)
        List<Item> bestGems = new List<Item>();
        foreach (var (item, amount) in items)
        {
            if (item == null || amount <= 0) continue;
            if (item.itemType != ItemType.Gems) continue;

            bestGems.Add(item);
        }

        // Sort by value (descending), then by rarity (descending)
        bestGems.Sort((a, b) =>
        {
            int valueCompare = b.gemValuePercent.CompareTo(a.gemValuePercent);
            if (valueCompare != 0) return valueCompare;
            return b.rarity.CompareTo(a.rarity);
        });

        // Equip top 3 gems (or as many as available)
        int slotsToFill = Mathf.Min(NUM_SLOTS, bestGems.Count);
        for (int i = 0; i < slotsToFill; i++)
        {
            EquipGem(weaponType, i, bestGems[i]);
        }
    }

    // Multipliers - sum all gems of each type across all slots
    public float GetMovementSpeedMultiplier(WeaponType weaponType)
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var gem = GetEquippedGem(weaponType, i);
            if (gem != null && gem.gemType == GemType.MovementSpeed)
            {
                total += gem.gemValuePercent;
            }
        }
        return 1f + total; // increase speed
    }

    public float GetCooldownMultiplier(WeaponType weaponType)
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var gem = GetEquippedGem(weaponType, i);
            if (gem != null && gem.gemType == GemType.CooldownReduction)
            {
                total += gem.gemValuePercent;
            }
        }
        // reduce cooldown: multiplier less than 1
        return Mathf.Clamp(1f - total, 0.2f, 10f);
    }

    public float GetDamageMultiplier(WeaponType weaponType)
    {
        float total = 0f;
        for (int i = 0; i < NUM_SLOTS; i++)
        {
            var gem = GetEquippedGem(weaponType, i);
            if (gem != null && gem.gemType == GemType.Damage)
            {
                total += gem.gemValuePercent;
            }
        }
        return 1f + total;
    }

    private static bool IsValidWeaponType(WeaponType wt)
    {
        return wt != WeaponType.None;
    }
}


