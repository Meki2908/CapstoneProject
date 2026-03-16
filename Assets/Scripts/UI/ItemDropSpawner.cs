using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào enemy prefab — tự động spawn item orb khi enemy chết
/// Drop table cấu hình trong Inspector hoặc dùng default theo enemy type
/// </summary>
public class ItemDropSpawner : MonoBehaviour
{
    [Header("=== Drop Settings ===")]
    // Drops are now unlimited — each item rolls independently based on rarity

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
    /// Inject drop table từ bên ngoài (DungeonWaveManager) — thay thế default drops
    /// </summary>
    public void SetDropTable(List<ItemDropEntry> drops, bool enableExp, int maxDrops = 0, int customExp = 0)
    {
        itemDropTable = drops ?? new List<ItemDropEntry>();
        dropExp = enableExp;
        maxDropCount = maxDrops;
        customExpAmount = customExp;
        Debug.Log($"[ItemDropSpawner] Drop table injected: {itemDropTable.Count} items, exp={enableExp}, maxDrops={maxDrops}, customExp={customExp}");
    }

    /// <summary>
    /// Số item tối đa rơi ra (0 = không giới hạn)
    /// </summary>
    private int maxDropCount = 0;

    /// <summary>
    /// Set orb prefab từ bên ngoài (khi tạo bằng AddComponent)
    /// </summary>
    public void SetOrbPrefab(GameObject prefab, float scale = 0.3f)
    {
        orbPrefab = prefab;
        orbScale = scale;
    }

    /// <summary>
    /// Gọi khi enemy chết — spawn item orb bay ra
    /// </summary>
    public void SpawnDrops(Vector3 deathPosition)
    {
        int dropCount = 0;

        // 1. Item ScriptableObject drops
        // Mỗi item roll độc lập theo dropChance, giới hạn bởi maxDropCount
        foreach (var drop in itemDropTable)
        {
            if (maxDropCount > 0 && dropCount >= maxDropCount) break;
            if (drop.item == null) continue;
            if (Random.value > drop.dropChance) continue;

            int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
            Rarity rtRarity = drop.item.useRandomRarity 
                ? RandomRarityFromEnemy() 
                : drop.item.rarity;
            SpawnOrb(deathPosition, drop.item, qty, rtRarity);
            dropCount++;
        }

        // 2. Custom drops (tên — không kết nối inventory)
        foreach (var drop in customDropTable)
        {
            if (Random.value > drop.dropChance) continue;

            int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
            SpawnOrb(deathPosition, drop.itemName, drop.icon, drop.rarity, qty);
            dropCount++;
        }

        // 3. EXP orb (màu vàng nổi bật)
        if (dropExp)
        {
            int exp = customExpAmount > 0 ? customExpAmount : GetExpFromEnemyType();
            if (exp > 0)
            {
                SpawnOrb(deathPosition, $"EXP +{exp}", null, ItemRarity.Legendary, exp);
            }
        }

        // 4. Nếu không có drop table nào → dùng default theo enemy type
        if (itemDropTable.Count == 0 && customDropTable.Count == 0)
        {
            SpawnDefaultDrops(deathPosition);
        }

        Debug.Log($"[ItemDropSpawner] Dropped {dropCount} items from {itemDropTable.Count} entries");
    }

    /// <summary>
    /// Spawn orb từ Item ScriptableObject với runtime rarity
    /// </summary>
    private void SpawnOrb(Vector3 position, Item item, int quantity, Rarity rtRarity)
    {
        var orbGO = CreateOrbGameObject(position, item.itemName);
        var orb = orbGO.GetComponent<ItemDropOrb>();
        if (orb == null) orb = orbGO.AddComponent<ItemDropOrb>();
        orb.Setup(item, rtRarity, quantity);
    }

    /// <summary>
    /// Spawn orb từ Item ScriptableObject (dùng SO rarity)
    /// </summary>
    private void SpawnOrb(Vector3 position, Item item, int quantity)
    {
        SpawnOrb(position, item, quantity, item.rarity);
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
    /// Drop mặc định nếu không cấu hình drop table — dùng Item ScriptableObject thật
    /// Player nhặt → thêm vào inventory
    /// </summary>
    private void SpawnDefaultDrops(Vector3 position)
    {
        // Load tất cả Item ScriptableObject từ Resources hoặc cache
        if (cachedItems == null)
        {
            cachedItems = Resources.FindObjectsOfTypeAll<Item>();
            Debug.Log($"[ItemDropSpawner] Cached {cachedItems.Length} Item ScriptableObjects");
        }

        // Tìm enemy type
        var enemyScript = GetComponent<EnemyScript>();
        if (enemyScript == null) enemyScript = GetComponentInParent<EnemyScript>();
        int type = enemyScript != null ? (int)enemyScript.enemyType : 0;

        // === DROP HEALTH POTION (quái nào cũng có chance) ===
        Item healthPotion = FindItem("Health potion");
        if (healthPotion != null)
        {
            float potionChance = type >= 4 ? 0.8f : (type >= 2 ? 0.5f : 0.3f);
            if (Random.value < potionChance)
            {
                int qty = type >= 4 ? Random.Range(2, 4) : 1;
                SpawnOrb(position, healthPotion, qty);
            }
        }

        // === DROP GEM THEO ENEMY TYPE ===
        switch (type)
        {
            case 0: // Skeleton — Common gems
            case 1: // Archer
                if (Random.value < 0.2f)
                    SpawnRandomGem(position, Rarity.Common);
                break;

            case 2: // Monster — Common/Epic gems
                if (Random.value < 0.3f)
                    SpawnRandomGem(position, Random.value < 0.7f ? Rarity.Common : Rarity.Epic);
                break;

            case 3: // Lich — Epic gems
                if (Random.value < 0.35f)
                    SpawnRandomGem(position, Rarity.Epic);
                if (Random.value < 0.15f)
                    SpawnRandomGem(position, Rarity.Common);
                break;

            case 4: // Boss — Epic/Legendary gems + Equipment
                SpawnRandomGem(position, Rarity.Epic); // Guaranteed Epic
                if (Random.value < 0.3f)
                    SpawnRandomGem(position, Rarity.Legendary);
                SpawnRandomEquipment(position); // Guaranteed equipment
                break;

            case 5: // Demon — Legendary gems + Equipment
                SpawnRandomGem(position, Rarity.Legendary); // Guaranteed Legendary
                SpawnRandomGem(position, Rarity.Epic);
                SpawnRandomEquipment(position);
                break;
        }
    }

    // Cache items để không load lại mỗi lần
    private static Item[] cachedItems;

    private Item FindItem(string name)
    {
        if (cachedItems == null) return null;
        foreach (var item in cachedItems)
        {
            if (item != null && item.itemName == name) return item;
        }
        return null;
    }

    private void SpawnRandomGem(Vector3 position, Rarity targetRarity)
    {
        if (cachedItems == null) return;

        // Tìm tất cả gem đúng rarity
        var gems = new List<Item>();
        foreach (var item in cachedItems)
        {
            if (item != null && item.itemType == ItemType.Gems && item.rarity == targetRarity)
                gems.Add(item);
        }

        if (gems.Count > 0)
        {
            Item gem = gems[Random.Range(0, gems.Count)];
            SpawnOrb(position, gem, 1);
        }
    }

    private void SpawnRandomEquipment(Vector3 position)
    {
        if (cachedItems == null) return;

        // Tìm tất cả equipment
        var equips = new List<Item>();
        foreach (var item in cachedItems)
        {
            if (item != null && item.itemType == ItemType.Equipment)
                equips.Add(item);
        }

        if (equips.Count > 0)
        {
            Item equip = equips[Random.Range(0, equips.Count)];
            SpawnOrb(position, equip, 1);
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
        return 100;
    }

    /// <summary>
    /// Random rarity dựa trên enemy type (weighted random)
    /// </summary>
    private Rarity RandomRarityFromEnemy()
    {
        var enemyScript = GetComponent<EnemyScript>();
        if (enemyScript == null) enemyScript = GetComponentInParent<EnemyScript>();

        int type = enemyScript != null ? (int)enemyScript.enemyType : 0;

        // Weights: Common, Uncommon, Rare, Epic, Legendary, Mythic
        float[] weights;
        switch (type)
        {
            case 0: // Skeleton
            case 1: // Archer
                weights = new float[] { 55, 25, 12, 6, 2, 0 };
                break;
            case 2: // Monster
                weights = new float[] { 35, 25, 18, 14, 5, 3 };
                break;
            case 3: // Lich
                weights = new float[] { 15, 20, 25, 22, 13, 5 };
                break;
            case 4: // Boss (chung)
            case 6: // Stoneogre
            case 7: // Golem
            case 8: // Minotaur
            case 9: // Ifrit
                weights = new float[] { 5, 10, 15, 30, 25, 15 };
                break;
            case 5: // Demon
                weights = new float[] { 0, 5, 10, 25, 35, 25 };
                break;
            default:
                weights = new float[] { 45, 25, 15, 10, 4, 1 };
                break;
        }

        float total = 0;
        foreach (float w in weights) total += w;
        float roll = Random.Range(0f, total);

        float cumulative = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) return (Rarity)(i + 1); // +1 vì None=0, Common=1
        }

        return Rarity.Common;
    }
}
