using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Serializable data structure for inventory items
/// </summary>
[System.Serializable]
public class InventoryItemData
{
    public int itemId;
    public int amount;

    public InventoryItemData(int id, int amt)
    {
        itemId = id;
        amount = amt;
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
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "inventory.json";

    [Header("Item Database")]
    [Tooltip("Folder path in Resources where Item ScriptableObjects are stored (e.g., 'Items' for Resources/Items/)")]
    [SerializeField] private string itemResourcePath = "Items";
    [Tooltip("If not using Resources, manually assign all Item ScriptableObjects here")]
    [SerializeField] private Item[] itemDatabase;

    [Header("Test Settings")]
    [Tooltip("Items available for random testing")]
    [SerializeField] private Item[] testItems;

    // Internal data
    private Dictionary<int, int> inventoryItems = new Dictionary<int, int>(); // itemId -> amount
    private Dictionary<int, Item> itemLookup = new Dictionary<int, Item>(); // itemId -> Item SO

    // Events
    public event Action<int, int> OnItemAdded; // itemId, amount
    public event Action<int, int> OnItemRemoved; // itemId, amount
    public event Action OnInventoryChanged;

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
            InitializeItemDatabase();
            LoadInventory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initialize the item database lookup
    /// </summary>
    private void InitializeItemDatabase()
    {
        itemLookup.Clear();

        // First, try to load from Resources if path is specified
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

        // Then, add manually assigned items
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

        // Also add test items to lookup
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
    /// Add item to inventory with probability check
    /// </summary>
    /// <param name="itemId">ID of the item to add</param>
    /// <param name="amount">Amount to add</param>
    /// <param name="probability">Probability (0.0 to 1.0) - if random value is greater than probability, item won't be added</param>
    /// <returns>True if item was added, false if probability check failed</returns>
    public bool AddItem(int itemId, int amount, float probability = 1.0f)
    {
        // Probability check
        if (probability < 1.0f)
        {
            float randomValue = UnityEngine.Random.Range(0f, 1f);
            if (randomValue > probability)
            {
                Debug.Log($"[InventoryManager] Item {itemId} failed probability check ({randomValue:F2} > {probability:F2})");
                return false;
            }
        }

        // Check if item exists in database
        if (!itemLookup.ContainsKey(itemId))
        {
            Debug.LogWarning($"[InventoryManager] Item with ID {itemId} not found in database!");
            return false;
        }

        Item item = itemLookup[itemId];

        // Add to inventory
        if (inventoryItems.ContainsKey(itemId))
        {
            // If stackable, add to existing stack (respecting max stack size)
            if (item.isStackable)
            {
                int currentAmount = inventoryItems[itemId];
                int newAmount = currentAmount + amount;
                
                // Check max stack size
                if (item.maxStackSize > 0 && newAmount > item.maxStackSize)
                {
                    // If exceeds max stack, create new stack or cap at max
                    // For simplicity, we'll just cap it (you can modify this logic)
                    newAmount = item.maxStackSize;
                    Debug.Log($"[InventoryManager] Item {item.itemName} reached max stack size ({item.maxStackSize})");
                }
                
                inventoryItems[itemId] = newAmount;
            }
            else
            {
                // Not stackable, just add as separate entry (or you can handle differently)
                inventoryItems[itemId] += amount;
            }
        }
        else
        {
            inventoryItems[itemId] = amount;
        }

        Debug.Log($"[InventoryManager] Added {amount}x {item.itemName} (ID: {itemId}) to inventory");
        
        // Trigger events
        OnItemAdded?.Invoke(itemId, amount);
        OnInventoryChanged?.Invoke();

        // Save inventory
        SaveInventory();

        // Refresh UI if InventoryController exists
        RefreshInventoryUI();

        return true;
    }

    /// <summary>
    /// Add item using Item ScriptableObject reference
    /// </summary>
    public bool AddItem(Item item, int amount, float probability = 1.0f)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryManager] Cannot add null item!");
            return false;
        }
        return AddItem(item.id, amount, probability);
    }

    /// <summary>
    /// Remove item from inventory
    /// </summary>
    public bool RemoveItem(int itemId, int amount)
    {
        if (!inventoryItems.ContainsKey(itemId))
        {
            Debug.LogWarning($"[InventoryManager] Item {itemId} not found in inventory!");
            return false;
        }

        int currentAmount = inventoryItems[itemId];
        int newAmount = currentAmount - amount;

        if (newAmount <= 0)
        {
            inventoryItems.Remove(itemId);
        }
        else
        {
            inventoryItems[itemId] = newAmount;
        }

        Debug.Log($"[InventoryManager] Removed {amount}x item {itemId} from inventory");

        // Trigger events
        OnItemRemoved?.Invoke(itemId, amount);
        OnInventoryChanged?.Invoke();

        // Save inventory
        SaveInventory();

        // Refresh UI
        RefreshInventoryUI();

        return true;
    }

    /// <summary>
    /// Get amount of specific item in inventory
    /// </summary>
    public int GetItemAmount(int itemId)
    {
        if (inventoryItems.ContainsKey(itemId))
        {
            return inventoryItems[itemId];
        }
        return 0;
    }

    /// <summary>
    /// Get all items in inventory as list of (Item, amount) pairs
    /// </summary>
    public List<(Item item, int amount)> GetAllItems()
    {
        List<(Item, int)> result = new List<(Item, int)>();
        
        foreach (var kvp in inventoryItems)
        {
            if (itemLookup.ContainsKey(kvp.Key))
            {
                result.Add((itemLookup[kvp.Key], kvp.Value));
            }
        }
        
        return result;
    }

    /// <summary>
    /// Get Item ScriptableObject by ID
    /// </summary>
    public Item GetItemById(int itemId)
    {
        if (itemLookup.ContainsKey(itemId))
        {
            return itemLookup[itemId];
        }
        return null;
    }

    /// <summary>
    /// Check if item exists in inventory
    /// </summary>
    public bool HasItem(int itemId)
    {
        return inventoryItems.ContainsKey(itemId) && inventoryItems[itemId] > 0;
    }

    /// <summary>
    /// Refresh inventory UI by finding InventoryController and calling its refresh method
    /// </summary>
    private void RefreshInventoryUI()
    {
        InventoryController inventoryController = FindFirstObjectByType<InventoryController>();
        if (inventoryController != null)
        {
            // Call a method to refresh UI (we'll need to add this to InventoryController)
            inventoryController.RefreshInventoryUI();
        }
    }

    #region Test Methods

    /// <summary>
    /// Add a random item from test items (for testing purposes)
    /// Can be called from UI button OnClick
    /// </summary>
    public void AddRandomItem()
    {
        Item[] itemsToUse = null;

        // First, try to use test items
        if (testItems != null && testItems.Length > 0)
        {
            itemsToUse = testItems;
        }
        // If no test items, try to use item database
        else if (itemDatabase != null && itemDatabase.Length > 0)
        {
            itemsToUse = itemDatabase;
        }
        // If still no items, try to use items from lookup (already loaded items)
        else if (itemLookup != null && itemLookup.Count > 0)
        {
            itemsToUse = itemLookup.Values.ToArray();
        }

        if (itemsToUse == null || itemsToUse.Length == 0)
        {
            Debug.LogWarning("[InventoryManager] No items available! Please assign items to 'Test Items' or 'Item Database' in Inspector, or use the Editor buttons to auto-find items.");
            return;
        }

        // Filter out null items
        Item[] validItems = itemsToUse.Where(item => item != null).ToArray();
        
        if (validItems.Length == 0)
        {
            Debug.LogWarning("[InventoryManager] No valid items found!");
            return;
        }

        // Pick random item
        Item randomItem = validItems[UnityEngine.Random.Range(0, validItems.Length)];
        int randomAmount = UnityEngine.Random.Range(1, 4); // Random amount between 1-3

        AddItem(randomItem, randomAmount, 1.0f);
        Debug.Log($"[InventoryManager] Added random item: {randomItem.itemName} x{randomAmount}");
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// Save inventory to JSON file
    /// </summary>
    private void SaveInventory()
    {
        InventorySaveData saveData = new InventorySaveData();
        
        foreach (var kvp in inventoryItems)
        {
            saveData.items.Add(new InventoryItemData(kvp.Key, kvp.Value));
        }

        string json = JsonUtility.ToJson(saveData, true);
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        try
        {
            File.WriteAllText(filePath, json);
            Debug.Log($"[InventoryManager] Saved inventory to {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventoryManager] Failed to save inventory: {e.Message}");
        }
    }

    /// <summary>
    /// Load inventory from JSON file
    /// </summary>
    private void LoadInventory()
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (!File.Exists(filePath))
        {
            Debug.Log($"[InventoryManager] No save file found at {filePath}, starting with empty inventory");
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
                    inventoryItems[itemData.itemId] = itemData.amount;
                }
                Debug.Log($"[InventoryManager] Loaded {inventoryItems.Count} item types from {filePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[InventoryManager] Failed to load inventory: {e.Message}");
        }
    }

    /// <summary>
    /// Clear all items from inventory (for testing)
    /// </summary>
    public void ClearInventory()
    {
        inventoryItems.Clear();
        SaveInventory();
        RefreshInventoryUI();
        Debug.Log("[InventoryManager] Inventory cleared");
    }

    #endregion
}

