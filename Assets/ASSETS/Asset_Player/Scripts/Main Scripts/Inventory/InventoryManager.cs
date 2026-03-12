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

    public InventoryItemData(int id, int amt, int rar = 0)
    {
        itemId = id;
        amount = amt;
        rarity = rar;
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
        Rarity r = itemLookup.ContainsKey(itemId) ? itemLookup[itemId].rarity : Rarity.Common;
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

    /// <summary>
    /// Get all items in inventory as list of (Item, amount, rarity)
    /// </summary>
    public List<(Item item, int amount, Rarity rarity)> GetAllItemsWithRarity()
    {
        var result = new List<(Item, int, Rarity)>();

        foreach (var kvp in inventoryItems)
        {
            int id; Rarity r;
            SplitKey(kvp.Key, out id, out r);
            if (itemLookup.ContainsKey(id))
            {
                result.Add((itemLookup[id], kvp.Value, r));
            }
        }

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

    private void RefreshInventoryUI()
    {
        InventoryController inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController != null)
        {
            inventoryController.RefreshInventoryUI();
        }
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

        // Random rarity cho test
        Rarity randomRarity = (Rarity)UnityEngine.Random.Range(0, 5);

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
            saveData.items.Add(new InventoryItemData(id, kvp.Value, (int)r));
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
                foreach (var itemData in saveData.items)
                {
                    int key = MakeKey(itemData.itemId, itemData.rarity);
                    inventoryItems[key] = itemData.amount;
                }
                Debug.Log($"[InventoryManager] Loaded {inventoryItems.Count} item slots from save");
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
