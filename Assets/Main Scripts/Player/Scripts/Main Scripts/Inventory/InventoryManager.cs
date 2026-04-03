using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Serializable data structure for inventory items — lưu cả rarity
/// </summary>
[System.Serializable]
public class InventoryItemData
{
    public int itemId;
    public int amount;
    public int rarity; // Rarity enum as int
    public float[] rolledValues; // NEW: one rolled stat value per item in stack (null for non-stat items)

    public InventoryItemData(int id, int amt, int rar = 1, float[] rolls = null)
    {
        itemId = id;
        amount = amt;
        rarity = rar;
        rolledValues = rolls;
    }
}

/// <summary>
/// Serializable save data for inventory
/// </summary>
[System.Serializable]
public class InventorySaveData
{
    public List<InventoryItemData> items = new List<InventoryItemData>();

    public InventorySaveData()
    {
        items = new List<InventoryItemData>();
    }
}

/// <summary>
/// Singleton manager for inventory system with JSON save/load
/// Handles item addition, removal, and persistence
/// Hỗ trợ Runtime Rarity: cùng 1 item có thể tồn tại nhiều rarity khác nhau
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "inventory.json";

    [Header("Item Database")]
    [Tooltip("Folder path in Resources where Item ScriptableObjects are stored")]
    [SerializeField] private string itemResourcePath = "Items";
    [Tooltip("If not using Resources, manually assign all Item ScriptableObjects here")]
    [SerializeField] private Item[] itemDatabase;

    [Header("Test Settings")]
    [Tooltip("Items available for random testing")]
    [SerializeField] private Item[] testItems;

    // Key = (itemId * 100 + rarity) → unique key per item+rarity combo
    private Dictionary<int, int> inventoryItems = new Dictionary<int, int>();
    private Dictionary<int, Item> itemLookup = new Dictionary<int, Item>();

    // NEW: Queue of rolled stat values per inventory key (for items with random stats)
    // Gems: each float = rolled gemValuePercent
    // Equipment: each float = rolled stat multiplier (0.01-1.0)
    private Dictionary<int, List<float>> rolledStats = new Dictionary<int, List<float>>();

    /// <summary>
    /// Last removed rolled value (set by RemoveItem for callers to retrieve)
    /// </summary>
    public float LastRemovedRoll { get; private set; } = -1f;

    // Events
    public event Action<int, int, Rarity> OnItemAddedWithRarity; // itemId, amount, rarity
    public event Action<int, int> OnItemAdded; // backward compat
    public event Action<int, int> OnItemRemoved;
    public event Action OnInventoryChanged;

    /// <summary>
    /// Tạo unique key từ itemId + rarity
    /// </summary>
    public static int MakeKey(int itemId, Rarity rarity)
    {
        return itemId * 100 + (int)rarity;
    }

    public static int MakeKey(int itemId, int rarityInt)
    {
        return itemId * 100 + rarityInt;
    }

    /// <summary>
    /// Tách key thành itemId và rarity
    /// </summary>
    public static void SplitKey(int key, out int itemId, out Rarity rarity)
    {
        itemId = key / 100;
        rarity = (Rarity)(key % 100);
    }

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
            InitializeItemDatabase();
            LoadInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeItemDatabase()
    {
        itemLookup.Clear();

        if (!string.IsNullOrEmpty(itemResourcePath))
        {
            Item[] resourcesItems = Resources.LoadAll<Item>(itemResourcePath);
            foreach (Item item in resourcesItems)
            {
                if (item != null && !itemLookup.ContainsKey(item.id))
                {
                    itemLookup[item.id] = item;
                }
            }
            Debug.Log($"[InventoryManager] Loaded {resourcesItems.Length} items from Resources/{itemResourcePath}");
        }

        if (itemDatabase != null && itemDatabase.Length > 0)
        {
            foreach (Item item in itemDatabase)
            {
                if (item != null && !itemLookup.ContainsKey(item.id))
                {
                    itemLookup[item.id] = item;
                }
            }
            Debug.Log($"[InventoryManager] Added {itemDatabase.Length} items from manually assigned database");
        }

        if (testItems != null && testItems.Length > 0)
        {
            foreach (Item item in testItems)
            {
                if (item != null && !itemLookup.ContainsKey(item.id))
                {
                    itemLookup[item.id] = item;
                }
            }
        }

        // === AUTO-DISCOVER: Tìm TẤT CẢ Item SO trong project ===
        // Đảm bảo không thiếu item nào kể cả khi chưa kéo vào Inspector
        Item[] allItems = Resources.FindObjectsOfTypeAll<Item>();
        int autoCount = 0;
        foreach (Item item in allItems)
        {
            if (item != null && !itemLookup.ContainsKey(item.id))
            {
                itemLookup[item.id] = item;
                autoCount++;
            }
        }
        if (autoCount > 0)
        {
            Debug.Log($"[InventoryManager] Auto-discovered {autoCount} additional items not in manual database");
        }

        Debug.Log($"[InventoryManager] Total items in database: {itemLookup.Count}");
    }

    /// <summary>
    /// Add item với runtime rarity
    /// </summary>
    public bool AddItem(int itemId, int amount, Rarity rarity, float probability = 1.0f)
    {
        if (probability < 1.0f)
        {
            float randomValue = UnityEngine.Random.Range(0f, 1f);
            if (randomValue > probability)
            {
                return false;
            }
        }

        if (!itemLookup.ContainsKey(itemId))
        {
            Debug.LogWarning($"[InventoryManager] Item with ID {itemId} not found in database!");
            return false;
        }

        Item item = itemLookup[itemId];
        int key = MakeKey(itemId, rarity);

        if (inventoryItems.ContainsKey(key))
        {
            if (item.isStackable)
            {
                int currentAmount = inventoryItems[key];
                int newAmount = currentAmount + amount;
                if (item.maxStackSize > 0 && newAmount > item.maxStackSize)
                {
                    newAmount = item.maxStackSize;
                }
                inventoryItems[key] = newAmount;
            }
            else
            {
                inventoryItems[key] += amount;
            }
        }
        else
        {
            inventoryItems[key] = amount;
        }

        // ── Roll random stats for items that need them ──
        if (item.HasRandomStats)
        {
            if (!rolledStats.ContainsKey(key))
                rolledStats[key] = new List<float>();

            for (int i = 0; i < amount; i++)
            {
                float roll;
                if (item.itemType == ItemType.Gems)
                    roll = item.RollGemValue();
                else // Equipment
                    roll = item.RollStatMultiplier();

                rolledStats[key].Add(roll);
                Debug.Log($"[InventoryManager] Rolled stat for {item.itemName} [{rarity}]: {roll:F4}");
            }
        }

        Debug.Log($"[InventoryManager] Added {amount}x {item.itemName} [{rarity}] (ID:{itemId}) to inventory");

        OnItemAdded?.Invoke(itemId, amount);
        OnItemAddedWithRarity?.Invoke(itemId, amount, rarity);
        OnInventoryChanged?.Invoke();

        SaveInventory();
        RefreshInventoryUI();

        return true;
    }

    /// <summary>
    /// Add item bằng ID — backward compat (dùng SO rarity)
    /// </summary>
    public bool AddItem(int itemId, int amount, float probability = 1.0f)
    {
        Rarity r = itemLookup.ContainsKey(itemId) ? itemLookup[itemId].rarity : Rarity.None;
        return AddItem(itemId, amount, r, probability);
    }

    /// <summary>
    /// Add item bằng Item SO — backward compat (dùng SO rarity)
    /// </summary>
    public bool AddItem(Item item, int amount, float probability = 1.0f)
    {
        if (item == null) return false;
        return AddItem(item.id, amount, item.rarity, probability);
    }

    /// <summary>
    /// Add item bằng Item SO + runtime rarity
    /// </summary>
    public bool AddItem(Item item, int amount, Rarity rarity, float probability = 1.0f)
    {
        if (item == null) return false;
        return AddItem(item.id, amount, rarity, probability);
    }

    /// <summary>
    /// Add item with a SPECIFIC pre-rolled stat value (không random lại).
    /// Dùng khi trả gem/equipment từ slot về inventory để giữ nguyên rolled value.
    /// </summary>
    public bool AddItemWithRoll(int itemId, int amount, Rarity rarity, float rolledValue)
    {
        if (!itemLookup.ContainsKey(itemId))
        {
            Debug.LogWarning($"[InventoryManager] AddItemWithRoll: Item ID {itemId} not found!");
            return false;
        }

        Item item = itemLookup[itemId];
        int key = MakeKey(itemId, rarity);

        if (inventoryItems.ContainsKey(key))
        {
            if (item.isStackable)
            {
                int currentAmount = inventoryItems[key];
                int newAmount = currentAmount + amount;
                if (item.maxStackSize > 0 && newAmount > item.maxStackSize)
                    newAmount = item.maxStackSize;
                inventoryItems[key] = newAmount;
            }
            else
            {
                inventoryItems[key] += amount;
            }
        }
        else
        {
            inventoryItems[key] = amount;
        }

        // Insert the SPECIFIC rolled value (NO random)
        if (item.HasRandomStats && rolledValue >= 0f)
        {
            if (!rolledStats.ContainsKey(key))
                rolledStats[key] = new List<float>();

            for (int i = 0; i < amount; i++)
                rolledStats[key].Add(rolledValue);

            Debug.Log($"[InventoryManager] Re-added {item.itemName} [{rarity}] with preserved roll {rolledValue:F4}");
        }

        OnItemAdded?.Invoke(itemId, amount);
        OnItemAddedWithRarity?.Invoke(itemId, amount, rarity);
        OnInventoryChanged?.Invoke();
        SaveInventory();
        RefreshInventoryUI();
        return true;
    }

    /// <summary>
    /// Remove item với rarity cụ thể
    /// </summary>
    public bool RemoveItem(int itemId, int amount, Rarity rarity)
    {
        int key = MakeKey(itemId, rarity);
        if (!inventoryItems.ContainsKey(key))
        {
            Debug.LogWarning($"[InventoryManager] Item {itemId} [{rarity}] not found in inventory!");
            return false;
        }

        int currentAmount = inventoryItems[key];
        int newAmount = currentAmount - amount;

        // ── Pop rolled stat values (FIFO) ──
        LastRemovedRoll = -1f;
        if (rolledStats.ContainsKey(key) && rolledStats[key].Count > 0)
        {
            LastRemovedRoll = rolledStats[key][0];
            for (int i = 0; i < amount && rolledStats[key].Count > 0; i++)
                rolledStats[key].RemoveAt(0);

            if (rolledStats[key].Count == 0)
                rolledStats.Remove(key);
        }

        if (newAmount <= 0)
        {
            inventoryItems.Remove(key);
        }
        else
        {
            inventoryItems[key] = newAmount;
        }

        OnItemRemoved?.Invoke(itemId, amount);
        OnInventoryChanged?.Invoke();
        SaveInventory();
        RefreshInventoryUI();

        return true;
    }

    /// <summary>
    /// Remove item — backward compat (tìm key đầu tiên match itemId)
    /// </summary>
    public bool RemoveItem(int itemId, int amount)
    {
        foreach (var kvp in inventoryItems)
        {
            int id; Rarity r;
            SplitKey(kvp.Key, out id, out r);
            if (id == itemId)
            {
                return RemoveItem(itemId, amount, r);
            }
        }
        return false;
    }

    /// <summary>
    /// Get amount of specific item (tổng tất cả rarity)
    /// </summary>
    public int GetItemAmount(int itemId)
    {
        int total = 0;
        foreach (var kvp in inventoryItems)
        {
            int id; Rarity r;
            SplitKey(kvp.Key, out id, out r);
            if (id == itemId) total += kvp.Value;
        }
        return total;
    }

    /// <summary>
    /// Get amount of specific item + rarity
    /// </summary>
    public int GetItemAmount(int itemId, Rarity rarity)
    {
        int key = MakeKey(itemId, rarity);
        return inventoryItems.ContainsKey(key) ? inventoryItems[key] : 0;
    }

    // Custom sort order for ItemType
    private static int GetTypeSortOrder(ItemType t)
    {
        switch (t)
        {
            case ItemType.Equipment:    return 0;
            case ItemType.CrystalStone: return 1;
            case ItemType.Gems:         return 2;
            case ItemType.Consumable:   return 3;
            case ItemType.Material:     return 4;
            default:                    return 5;
        }
    }

    /// <summary>
    /// Get all items in inventory as list of (Item, amount, rarity)
    /// Sort: Equipment → Crystal → Gems → Consumable → Material
    /// Within same type: group by base item, rarity ascending (Common→Mythic)
    /// </summary>
    public List<(Item item, int amount, Rarity rarity)> GetAllItemsWithRarity()
    {
        var result = new List<(Item item, int amount, Rarity rarity)>();

        foreach (var kvp in inventoryItems)
        {
            int id; Rarity r;
            SplitKey(kvp.Key, out id, out r);
            if (itemLookup.ContainsKey(id))
            {
                result.Add((itemLookup[id], kvp.Value, r));
            }
        }

        result.Sort((a, b) =>
        {
            // 1. Custom type order: Equipment → Crystal → Gems
            int typeA = GetTypeSortOrder(a.item.itemType);
            int typeB = GetTypeSortOrder(b.item.itemType);
            if (typeA != typeB) return typeA.CompareTo(typeB);

            // 2. Group same-base items together (gemType for gems, equipmentSlot for equipment)
            if (a.item.itemType == ItemType.Gems)
            {
                int gemTypeCompare = a.item.gemType.CompareTo(b.item.gemType);
                if (gemTypeCompare != 0) return gemTypeCompare;
            }
            else if (a.item.itemType == ItemType.Equipment)
            {
                int slotCompare = a.item.equipmentSlot.CompareTo(b.item.equipmentSlot);
                if (slotCompare != 0) return slotCompare;
            }

            // 3. Same base → rarity ascending (Common → Mythic)
            int rarityCompare = a.rarity.CompareTo(b.rarity);
            if (rarityCompare != 0) return rarityCompare;

            // 4. Same rarity → name A→Z
            return string.Compare(a.item.itemName, b.item.itemName, System.StringComparison.OrdinalIgnoreCase);
        });

        return result;
    }

    // ─── Rolled Stats API ─────────────────────────────────────────

    /// <summary>
    /// Peek at the next rolled value for a specific item (first in queue).
    /// Returns -1 if no rolled value exists.
    /// </summary>
    public float PeekNextRoll(int itemId, Rarity rarity)
    {
        int key = MakeKey(itemId, rarity);
        if (rolledStats.ContainsKey(key) && rolledStats[key].Count > 0)
            return rolledStats[key][0];
        return -1f;
    }

    /// <summary>
    /// Get all rolled values for a specific item key.
    /// Returns null if no rolled values exist.
    /// </summary>
    public List<float> GetRolledValues(int itemId, Rarity rarity)
    {
        int key = MakeKey(itemId, rarity);
        if (rolledStats.ContainsKey(key))
            return rolledStats[key];
        return null;
    }

    /// <summary>
    /// Get all items with rolled stat info for UI display.
    /// Returns (item, amount, rarity, rolls[]) — rolls may be null for non-stat items.
    /// </summary>
    public List<(Item item, int amount, Rarity rarity, List<float> rolls)> GetAllItemsWithRarityAndRolls()
    {
        var result = new List<(Item item, int amount, Rarity rarity, List<float> rolls)>();

        foreach (var kvp in inventoryItems)
        {
            int id; Rarity r;
            SplitKey(kvp.Key, out id, out r);
            if (itemLookup.ContainsKey(id))
            {
                List<float> rolls = null;
                if (rolledStats.ContainsKey(kvp.Key))
                    rolls = rolledStats[kvp.Key];
                result.Add((itemLookup[id], kvp.Value, r, rolls));
            }
        }

        result.Sort((a, b) =>
        {
            int typeA = GetTypeSortOrder(a.item.itemType);
            int typeB = GetTypeSortOrder(b.item.itemType);
            if (typeA != typeB) return typeA.CompareTo(typeB);

            if (a.item.itemType == ItemType.Gems)
            {
                int gemTypeCompare = a.item.gemType.CompareTo(b.item.gemType);
                if (gemTypeCompare != 0) return gemTypeCompare;
            }
            else if (a.item.itemType == ItemType.Equipment)
            {
                int slotCompare = a.item.equipmentSlot.CompareTo(b.item.equipmentSlot);
                if (slotCompare != 0) return slotCompare;
            }

            int rarityCompare = a.rarity.CompareTo(b.rarity); // ascending
            if (rarityCompare != 0) return rarityCompare;

            return string.Compare(a.item.itemName, b.item.itemName, System.StringComparison.OrdinalIgnoreCase);
        });

        return result;
    }

    /// <summary>
    /// Get all items — backward compat (gộp tất cả rarity)
    /// </summary>
    public List<(Item item, int amount)> GetAllItems()
    {
        var merged = new Dictionary<int, int>();
        foreach (var kvp in inventoryItems)
        {
            int id; Rarity r;
            SplitKey(kvp.Key, out id, out r);
            if (merged.ContainsKey(id))
                merged[id] += kvp.Value;
            else
                merged[id] = kvp.Value;
        }

        var result = new List<(Item, int)>();
        foreach (var kvp in merged)
        {
            if (itemLookup.ContainsKey(kvp.Key))
                result.Add((itemLookup[kvp.Key], kvp.Value));
        }
        return result;
    }

    public Item GetItemById(int itemId)
    {
        return itemLookup.ContainsKey(itemId) ? itemLookup[itemId] : null;
    }

    public bool HasItem(int itemId)
    {
        return GetItemAmount(itemId) > 0;
    }

    private InventoryController cachedInventoryController;

    private void RefreshInventoryUI()
    {
        // Unity null check handles destroyed objects (scene unload)
        if (cachedInventoryController == null)
        {
            // Tìm trong tất cả objects kể cả inactive
            cachedInventoryController = FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
            
            if (cachedInventoryController != null)
            {
                Debug.Log($"[InventoryManager] Found InventoryController on '{cachedInventoryController.gameObject.name}' (scene: {cachedInventoryController.gameObject.scene.name})");
            }
            else
            {
                Debug.LogWarning("[InventoryManager] InventoryController NOT FOUND in any scene! UI sẽ không cập nhật cho đến khi mở inventory.");
            }
        }
        
        if (cachedInventoryController != null)
        {
            cachedInventoryController.RefreshInventoryUI();
            Debug.Log($"[InventoryManager] RefreshInventoryUI called successfully. Items in dict: {inventoryItems.Count}");
        }
    }

    /// <summary>
    /// Gọi khi scene thay đổi để clear cache (InventoryController có thể bị destroy)
    /// </summary>
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Clear cache khi scene mới load → tìm lại InventoryController
        cachedInventoryController = null;
        Debug.Log($"[InventoryManager] Scene loaded: {scene.name} — cleared InventoryController cache");
    }

    #region Test Methods

    public void AddRandomItem()
    {
        Item[] itemsToUse = null;

        if (testItems != null && testItems.Length > 0)
            itemsToUse = testItems;
        else if (itemDatabase != null && itemDatabase.Length > 0)
            itemsToUse = itemDatabase;
        else if (itemLookup != null && itemLookup.Count > 0)
            itemsToUse = itemLookup.Values.ToArray();

        if (itemsToUse == null || itemsToUse.Length == 0)
        {
            Debug.LogWarning("[InventoryManager] No items available!");
            return;
        }

        Item[] validItems = itemsToUse.Where(item => item != null).ToArray();
        if (validItems.Length == 0) return;

        Item randomItem = validItems[UnityEngine.Random.Range(0, validItems.Length)];
        int randomAmount = UnityEngine.Random.Range(1, 4);

        // Random rarity cho test (skip None=0, bắt đầu từ Common=1)
        Rarity randomRarity = (Rarity)UnityEngine.Random.Range(1, 7);

        AddItem(randomItem, randomAmount, randomRarity);
        Debug.Log($"[InventoryManager] Added random: {randomItem.itemName} [{randomRarity}] x{randomAmount}");
    }

    #endregion

    #region Save/Load

    private void SaveInventory()
    {
        InventorySaveData saveData = new InventorySaveData();

        foreach (var kvp in inventoryItems)
        {
            int id; Rarity r;
            SplitKey(kvp.Key, out id, out r);

            // Save rolled values if they exist
            float[] rolls = null;
            if (rolledStats.ContainsKey(kvp.Key) && rolledStats[kvp.Key].Count > 0)
                rolls = rolledStats[kvp.Key].ToArray();

            saveData.items.Add(new InventoryItemData(id, kvp.Value, (int)r, rolls));
        }

        string json = JsonUtility.ToJson(saveData, true);
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        try
        {
            File.WriteAllText(filePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventoryManager] Failed to save inventory: {e.Message}");
        }
    }

    private void LoadInventory()
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        // === AUTO-RESET: Xóa save cũ vì Rarity enum đã thêm None ở đầu ===
        string versionFile = Path.Combine(Application.persistentDataPath, "inventory_v2.flag");
        if (File.Exists(filePath) && !File.Exists(versionFile))
        {
            Debug.LogWarning("[InventoryManager] Detected old inventory save (pre-None rarity). Deleting...");
            File.Delete(filePath);
            File.WriteAllText(versionFile, "v2");
        }
        if (!File.Exists(versionFile))
        {
            File.WriteAllText(versionFile, "v2");
        }

        if (!File.Exists(filePath))
        {
            Debug.Log($"[InventoryManager] No save file found, starting empty");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

            if (saveData != null && saveData.items != null)
            {
                inventoryItems.Clear();
                rolledStats.Clear();
                foreach (var itemData in saveData.items)
                {
                    int key = MakeKey(itemData.itemId, itemData.rarity);
                    inventoryItems[key] = itemData.amount;

                    // Restore rolled values
                    if (itemData.rolledValues != null && itemData.rolledValues.Length > 0)
                    {
                        rolledStats[key] = new List<float>(itemData.rolledValues);
                    }
                }
                Debug.Log($"[InventoryManager] Loaded {inventoryItems.Count} item slots from save (with rolled stats)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventoryManager] Failed to load inventory: {e.Message}");
        }
    }

    public void ClearInventory()
    {
        inventoryItems.Clear();
        SaveInventory();
        RefreshInventoryUI();
        Debug.Log("[InventoryManager] Inventory cleared");
    }

    #endregion
}
