using UnityEngine;

/// <summary>
/// Test script cho ItemPickupNotification + ItemDropOrb
/// Gắn vào bất kỳ GameObject nào trong scene
/// </summary>
public class ItemPickupNotificationTest : MonoBehaviour
{
    [Header("Nhấn phím để test:")]
    [SerializeField] private string instructions = "F1-F4=Notification | F5=1 Orb | F6=5 Orbs | F7=Giả lập quái chết";

    [Header("=== Orb Settings ===")]
    [Tooltip("Kéo Material thủ công cho orb (null = tự tạo)")]
    [SerializeField] private Material orbMaterial;

    [Header("=== Test Items (kéo Item SO vào đây) ===")]
    [Tooltip("Kéo Item ScriptableObject từ Assets/ASSETS/Asset_Player/ScriptableObjects/Items/")]
    [SerializeField] private Item[] testItems;

    [Header("--- Kích thước ---")]
    [Range(0.1f, 1f)] [SerializeField] private float orbScale = 0.3f;

    [Header("--- Bay tung (Scatter) ---")]
    [Range(1f, 10f)] [SerializeField] private float scatterForce = 3f;
    [Range(1f, 8f)] [SerializeField] private float scatterUpForce = 2.5f;

    [Header("--- Lơ lửng (Hover) ---")]
    [Range(0.05f, 2f)] [SerializeField] private float hoverHeight = 0.3f;
    [Range(0.5f, 5f)] [SerializeField] private float bobSpeed = 2f;
    [Range(0.01f, 0.5f)] [SerializeField] private float bobAmount = 0.1f;
    [Range(10f, 180f)] [SerializeField] private float rotateSpeed = 90f;

    [Header("--- Hút về Player (Magnet) ---")]
    [Range(1f, 10f)] [SerializeField] private float magnetRadius = 4f;
    [Range(2f, 20f)] [SerializeField] private float magnetSpeed = 8f;
    [Range(0.3f, 2f)] [SerializeField] private float pickupRadius = 0.8f;

    [Header("--- Spawn ---")]
    [Range(1f, 10f)] [SerializeField] private float spawnDistance = 4f;
    [Range(1f, 5f)] [SerializeField] private float glowIntensity = 2f;

    private Item[] loadedItems;

    private void Start()
    {
        // Tự load items nếu chưa kéo trong Inspector
        if (testItems == null || testItems.Length == 0)
        {
            loadedItems = Resources.LoadAll<Item>("Items");
            if (loadedItems.Length > 0)
                Debug.Log($"[Test] Auto-loaded {loadedItems.Length} items từ Resources/Items");
        }
    }

    private Item[] GetAvailableItems()
    {
        if (testItems != null && testItems.Length > 0) return testItems;
        if (loadedItems != null && loadedItems.Length > 0) return loadedItems;
        return null;
    }

    private void Update()
    {
        // Notification tests
        if (ItemPickupNotification.Instance != null)
        {
            if (Input.GetKeyDown(KeyCode.F1)) TestRandom();
            if (Input.GetKeyDown(KeyCode.F2)) TestMultiple();
            if (Input.GetKeyDown(KeyCode.F3)) TestAllRarities();
            if (Input.GetKeyDown(KeyCode.F4)) TestDuplicateMerge();
        }

        // Orb tests
        if (Input.GetKeyDown(KeyCode.F5)) TestSpawnOneOrb();
        if (Input.GetKeyDown(KeyCode.F6)) TestSpawnAllRarityOrbs();
        if (Input.GetKeyDown(KeyCode.F7)) TestSimulateEnemyDeath();
    }

    // ===== NOTIFICATION TESTS =====

    private void TestRandom()
    {
        var items = GetAvailableItems();
        if (items == null || items.Length == 0) { LogNoItems(); return; }

        var item = items[Random.Range(0, items.Length)];
        ItemPickupNotification.Instance.ShowNotification(item.itemName, item.icon, MapRarity(item.rarity), Random.Range(1, 5));
    }

    private void TestMultiple()
    {
        var items = GetAvailableItems();
        if (items == null || items.Length == 0) { LogNoItems(); return; }

        for (int i = 0; i < Mathf.Min(3, items.Length); i++)
            ItemPickupNotification.Instance.ShowNotification(items[i].itemName, items[i].icon, MapRarity(items[i].rarity), Random.Range(1, 5));
    }

    private void TestAllRarities()
    {
        var items = GetAvailableItems();
        if (items == null || items.Length == 0) { LogNoItems(); return; }

        foreach (var item in items)
            ItemPickupNotification.Instance.ShowNotification(item.itemName, item.icon, MapRarity(item.rarity), 1);
    }

    private void TestDuplicateMerge()
    {
        var items = GetAvailableItems();
        if (items == null || items.Length == 0) { LogNoItems(); return; }

        var item = items[0];
        ItemPickupNotification.Instance.ShowNotification(item.itemName, item.icon, MapRarity(item.rarity), 10);
        ItemPickupNotification.Instance.ShowNotification(item.itemName, item.icon, MapRarity(item.rarity), 10);
        ItemPickupNotification.Instance.ShowNotification(item.itemName, item.icon, MapRarity(item.rarity), 10);
        Debug.Log($"[Test] {item.itemName} phải hiện ×30 (merge 3 lần)");
    }

    // ===== ORB TESTS =====

    private void TestSpawnOneOrb()
    {
        var items = GetAvailableItems();
        if (items == null || items.Length == 0) { LogNoItems(); return; }

        Vector3 spawnPos = GetSpawnPosition(3f);
        var item = items[Random.Range(0, items.Length)];
        SpawnTestOrbWithItem(spawnPos, item, Random.Range(1, 5));
        Debug.Log($"[Test] Spawned orb: {item.itemName} ({item.rarity}) → inventory");
    }

    private void TestSpawnAllRarityOrbs()
    {
        var items = GetAvailableItems();
        if (items == null || items.Length == 0) { LogNoItems(); return; }

        int count = Mathf.Min(5, items.Length);
        for (int i = 0; i < count; i++)
            SpawnTestOrbWithItem(GetSpawnPosition(4f), items[i], 1);
        Debug.Log($"[Test] Spawned {count} orbs từ inventory items!");
    }

    private void TestSimulateEnemyDeath()
    {
        var items = GetAvailableItems();
        if (items == null || items.Length == 0) { LogNoItems(); return; }

        Vector3 deathPos = GetSpawnPosition(5f);
        int count = Random.Range(3, Mathf.Min(6, items.Length + 1));
        for (int i = 0; i < count; i++)
        {
            var item = items[Random.Range(0, items.Length)];
            SpawnTestOrbWithItem(deathPos, item, Random.Range(1, 3));
        }
        Debug.Log($"[Test] Giả lập quái chết — {count} orbs → inventory!");
    }

    private void LogNoItems()
    {
        Debug.LogWarning("[Test] Chưa có Item SO! Kéo Item ScriptableObject vào 'Test Items' trong Inspector.");
    }

    // ===== HELPER =====

    private void SpawnTestOrb(Vector3 pos, string name, ItemRarity rarity, int qty)
    {
        var orbGO = new GameObject($"TestOrb_{name}");
        orbGO.transform.position = pos;
        orbGO.transform.localScale = Vector3.one * orbScale;

        var orb = orbGO.AddComponent<ItemDropOrb>();
        orb.Setup(name, null, rarity, qty);
        ApplyOrbSettings(orb);
    }

    private void SpawnTestOrbWithItem(Vector3 pos, Item item, int qty)
    {
        var orbGO = new GameObject($"TestOrb_{item.itemName}");
        orbGO.transform.position = pos;
        orbGO.transform.localScale = Vector3.one * orbScale;

        var orb = orbGO.AddComponent<ItemDropOrb>();
        orb.Setup(item, qty); // Dùng Item SO → nhặt sẽ thêm vào inventory!
        ApplyOrbSettings(orb);
    }

    private void ApplyOrbSettings(ItemDropOrb orb)
    {
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var t = typeof(ItemDropOrb);

        void SetField(string fieldName, object value)
        {
            var f = t.GetField(fieldName, flags);
            if (f != null) f.SetValue(orb, value);
        }

        SetField("scatterForce", scatterForce);
        SetField("scatterUpForce", scatterUpForce);
        SetField("hoverHeight", hoverHeight);
        SetField("bobSpeed", bobSpeed);
        SetField("bobAmount", bobAmount);
        SetField("rotateSpeed", rotateSpeed);
        SetField("magnetRadius", magnetRadius);
        SetField("magnetSpeed", magnetSpeed);
        SetField("pickupRadius", pickupRadius);
        SetField("glowIntensity", glowIntensity);

        if (orbMaterial != null)
            SetField("customOrbMaterial", orbMaterial);
    }

    private Vector3 GetSpawnPosition(float radius)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 center = player != null ? player.transform.position : transform.position;

        Vector2 randomDir = Random.insideUnitCircle.normalized * Random.Range(radius * 0.5f, radius);
        return center + new Vector3(randomDir.x, 0.5f, randomDir.y);
    }

    /// <summary>
    /// Chuyen Rarity (Item.cs) sang ItemRarity (orb/notification)
    /// </summary>
    private ItemRarity MapRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common:    return ItemRarity.Common;
            case Rarity.Uncommon:  return ItemRarity.Uncommon;
            case Rarity.Rare:      return ItemRarity.Rare;
            case Rarity.Epic:      return ItemRarity.Epic;
            case Rarity.Legendary: return ItemRarity.Legendary;
            case Rarity.Mythic:    return ItemRarity.Mythic;
            default: return ItemRarity.Common;
        }
    }
}
