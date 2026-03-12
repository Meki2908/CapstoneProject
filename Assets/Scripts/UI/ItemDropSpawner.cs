using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào enemy prefab — tự động spawn item orb khi enemy chết
/// Drop table cấu hình trong Inspector hoặc dùng default theo enemy type
/// </summary>
public class ItemDropSpawner : MonoBehaviour
{
    [Header("=== Drop Settings ===")]
    [Tooltip("Số item orb tối đa spawn khi chết")]
    [SerializeField] private int maxDropCount = 3;

    [Tooltip("Prefab cho orb (null = tự tạo sphere)")]
    [SerializeField] private GameObject orbPrefab;

    [Tooltip("Scale orb")]
    [SerializeField] private float orbScale = 0.3f;

    [Header("=== Drop Table (Item ScriptableObject) ===")]
    [SerializeField] private List<ItemDropEntry> itemDropTable = new List<ItemDropEntry>();

    [Header("=== Drop Table (tùy chỉnh tên) ===")]
    [SerializeField] private List<DropEntry> customDropTable = new List<DropEntry>();

    [Header("=== EXP Orb ===")]
    [Tooltip("Có drop EXP orb không")]
    [SerializeField] private bool dropExp = true;

    [Tooltip("Lượng EXP (0 = tự tính theo enemy type)")]
    [SerializeField] private int customExpAmount = 0;

    /// <summary>
    /// Drop bằng tên (không kết nối inventory)
    /// </summary>
    [System.Serializable]
    public class DropEntry
    {
        public string itemName = "Unknown Item";
        public Sprite icon;
        public ItemRarity rarity = ItemRarity.Common;
        public int minQuantity = 1;
        public int maxQuantity = 1;
        [Range(0f, 1f)]
        [Tooltip("Xác suất drop (0-1)")]
        public float dropChance = 0.5f;
    }

    /// <summary>
    /// Drop bằng Item ScriptableObject (kết nối inventory)
    /// </summary>
    [System.Serializable]
    public class ItemDropEntry
    {
        [Tooltip("Kéo Item ScriptableObject vào đây")]
        public Item item;
        public int minQuantity = 1;
        public int maxQuantity = 1;
        [Range(0f, 1f)]
        [Tooltip("Xác suất drop (0-1)")]
        public float dropChance = 0.5f;
    }

    /// <summary>
    /// Gọi khi enemy chết — spawn item orb bay ra
    /// </summary>
    public void SpawnDrops(Vector3 deathPosition)
    {
        int dropCount = 0;

        // 1. Item ScriptableObject drops (kết nối inventory)
        foreach (var drop in itemDropTable)
        {
            if (dropCount >= maxDropCount) break;
            if (drop.item == null) continue;
            if (Random.value > drop.dropChance) continue;

            int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
            SpawnOrb(deathPosition, drop.item, qty);
            dropCount++;
        }

        // 2. Custom drops (tên — không kết nối inventory)
        foreach (var drop in customDropTable)
        {
            if (dropCount >= maxDropCount) break;
            if (Random.value > drop.dropChance) continue;

            int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
            SpawnOrb(deathPosition, drop.itemName, drop.icon, drop.rarity, qty);
            dropCount++;
        }

        // 3. EXP orb
        if (dropExp && dropCount < maxDropCount)
        {
            int exp = customExpAmount > 0 ? customExpAmount : GetExpFromEnemyType();
            if (exp > 0)
            {
                SpawnOrb(deathPosition, $"EXP +{exp}", null, ItemRarity.Common, exp);
            }
        }

        // 4. Nếu không có drop table nào → dùng default theo enemy type
        if (itemDropTable.Count == 0 && customDropTable.Count == 0)
        {
            SpawnDefaultDrops(deathPosition);
        }
    }

    /// <summary>
    /// Spawn orb từ Item ScriptableObject (thêm vào inventory khi nhặt)
    /// </summary>
    private void SpawnOrb(Vector3 position, Item item, int quantity)
    {
        var orbGO = CreateOrbGameObject(position, item.itemName);
        var orb = orbGO.GetComponent<ItemDropOrb>();
        if (orb == null) orb = orbGO.AddComponent<ItemDropOrb>();
        orb.Setup(item, quantity); // Dùng Setup(Item) → tự lấy name + icon + rarity + link inventory
    }

    /// <summary>
    /// Spawn orb từ tên (chỉ notification, không inventory)
    /// </summary>
    private void SpawnOrb(Vector3 position, string itemName, Sprite icon, ItemRarity rarity, int quantity)
    {
        var orbGO = CreateOrbGameObject(position, itemName);
        var orb = orbGO.GetComponent<ItemDropOrb>();
        if (orb == null) orb = orbGO.AddComponent<ItemDropOrb>();
        orb.Setup(itemName, icon, rarity, quantity);
    }

    /// <summary>
    /// Tạo GameObject cho orb
    /// </summary>
    private GameObject CreateOrbGameObject(Vector3 position, string name)
    {
        GameObject orbGO;

        if (orbPrefab != null)
        {
            orbGO = Instantiate(orbPrefab, position, Quaternion.identity);
        }
        else
        {
            orbGO = new GameObject($"ItemOrb_{name}");
            orbGO.transform.position = position;
        }

        orbGO.transform.localScale = Vector3.one * orbScale;
        return orbGO;
    }

    /// <summary>
    /// Drop mặc định nếu không cấu hình drop table
    /// </summary>
    private void SpawnDefaultDrops(Vector3 position)
    {
        // Drop Mora/Gold luôn
        int gold = Random.Range(10, 50);
        SpawnOrb(position, "Gold", null, ItemRarity.Common, gold);

        // Chance drop vật liệu theo loại quái
        var enemyScript = GetComponent<EnemyScript>();
        if (enemyScript == null) enemyScript = GetComponentInParent<EnemyScript>();

        if (enemyScript != null)
        {
            int type = (int)enemyScript.enemyType;
            switch (type)
            {
                case 0: // Skeleton
                    if (Random.value < 0.3f)
                        SpawnOrb(position, "Bone Fragment", null, ItemRarity.Common, Random.Range(1, 3));
                    break;
                case 1: // Archer
                    if (Random.value < 0.3f)
                        SpawnOrb(position, "Arrow Head", null, ItemRarity.Uncommon, Random.Range(1, 2));
                    break;
                case 2: // Monster
                    if (Random.value < 0.25f)
                        SpawnOrb(position, "Monster Claw", null, ItemRarity.Uncommon, 1);
                    break;
                case 3: // Lich
                    if (Random.value < 0.2f)
                        SpawnOrb(position, "Dark Essence", null, ItemRarity.Rare, 1);
                    break;
                case 4: // Boss
                    SpawnOrb(position, "Boss Core", null, ItemRarity.Epic, 1);
                    if (Random.value < 0.3f)
                        SpawnOrb(position, "Rare Gem", null, ItemRarity.Legendary, 1);
                    break;
                case 5: // Demon
                    SpawnOrb(position, "Demon Heart", null, ItemRarity.Legendary, 1);
                    SpawnOrb(position, "Dark Crystal", null, ItemRarity.Epic, Random.Range(1, 3));
                    break;
            }
        }
    }

    /// <summary>
    /// Lấy EXP theo enemy type
    /// </summary>
    private int GetExpFromEnemyType()
    {
        var enemyScript = GetComponent<EnemyScript>();
        if (enemyScript == null) enemyScript = GetComponentInParent<EnemyScript>();

        if (enemyScript != null)
        {
            switch ((int)enemyScript.enemyType)
            {
                case 0: return 100;   // Skeleton
                case 1: return 150;   // Archer
                case 2: return 300;   // Monster
                case 3: return 350;   // Lich
                case 4: return 1500;  // Boss
                case 5: return 3000;  // Demon
            }
        }
        return 50;
    }
}
