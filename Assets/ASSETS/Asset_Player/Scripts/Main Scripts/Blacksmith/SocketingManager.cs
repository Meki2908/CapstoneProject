using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Kết quả khảm gem
/// </summary>
public enum SocketResult
{
    Success,        // Khảm thành công — gem gắn, crystal tiêu thụ
    Fail,           // Thất bại — chỉ mất crystal, gem trả lại
    NoCrystal,      // Không có crystal stone
    NoGem,          // Không có gem
    NoTarget,       // Không có target (weapon/equipment)
    InvalidSlot     // Slot không hợp lệ
}

/// <summary>
/// Loại target khảm
/// </summary>
public enum SocketTargetType
{
    Weapon,
    Equipment
}

/// <summary>
/// Manages gem socketing logic for both weapons and equipment.
/// Requires Crystal Stone — success rate depends on equipment rarity vs crystal rarity.
/// On failure: only crystal is consumed, gem is returned.
/// </summary>
public class SocketingManager : MonoBehaviour
{
    public static SocketingManager Instance { get; private set; }

    // Events
    public event Action<SocketResult, Item, Item> OnSocketAttempt; // result, gem, crystal

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ================================================================
    // BẢNG TỈ LỆ THÀNH CÔNG
    // Hàng = rarity của target (weapon/equipment), Cột = rarity crystal
    // ================================================================
    // Rarity indices: 0=Common, 1=Uncommon, 2=Rare, 3=Epic, 4=Legendary, 5=Mythic
    private static readonly float[,] SuccessRateTable = new float[6, 6]
    {
        // Crystal:      Common  Uncommon  Rare   Epic   Legend  Mythic
        /* Common    */ { 1.00f,  1.00f,  1.00f,  1.00f,  1.00f,  1.00f },
        /* Uncommon  */ { 0.70f,  0.90f,  1.00f,  1.00f,  1.00f,  1.00f },
        /* Rare      */ { 0.40f,  0.60f,  0.80f,  0.95f,  1.00f,  1.00f },
        /* Epic      */ { 0.20f,  0.35f,  0.55f,  0.75f,  0.90f,  1.00f },
        /* Legendary */ { 0.05f,  0.15f,  0.30f,  0.50f,  0.70f,  0.90f },
        /* Mythic    */ { 0.00f,  0.05f,  0.15f,  0.30f,  0.50f,  0.75f },
    };

    /// <summary>
    /// Tính tỉ lệ thành công dựa trên rarity target và rarity crystal
    /// </summary>
    public float CalculateSuccessRate(Rarity targetRarity, Rarity crystalRarity)
    {
        int row = Mathf.Clamp((int)targetRarity, 0, 5);
        int col = Mathf.Clamp((int)crystalRarity, 0, 5);
        return SuccessRateTable[row, col];
    }

    /// <summary>
    /// Thử khảm gem vào weapon
    /// </summary>
    public SocketResult TrySocketWeapon(WeaponType weaponType, int slotIndex, Item gem, Item crystal)
    {
        // Validate
        if (gem == null || gem.itemType != ItemType.Gems)
            return SocketResult.NoGem;
        if (crystal == null || crystal.itemType != ItemType.CrystalStone)
            return SocketResult.NoCrystal;
        if (weaponType == WeaponType.None)
            return SocketResult.NoTarget;
        if (slotIndex < 0 || slotIndex >= 3)
            return SocketResult.InvalidSlot;
        if (WeaponGemManager.Instance == null || InventoryManager.Instance == null)
            return SocketResult.NoTarget;

        // Check inventory has both items
        if (InventoryManager.Instance.GetItemAmount(gem.id) <= 0)
            return SocketResult.NoGem;
        if (InventoryManager.Instance.GetItemAmount(crystal.id) <= 0)
            return SocketResult.NoCrystal;

        // Lấy rarity của weapon (dùng SO rarity vì weapon không có runtime rarity riêng)
        // Dùng rarity từ gem slot target — ở đây dùng crystal rarity vs weapon rarity
        // Weapon rarity: dùng mặc định từ WeaponSO nếu có, nếu không dùng Common
        WeaponController wc = UnityEngine.Object.FindFirstObjectByType<WeaponController>();
        Rarity targetRarity = Rarity.Common;
        if (wc != null && wc.GetCurrentWeapon() != null)
        {
            // Nếu WeaponSO có rarity field → dùng nó, nếu không → Common
            targetRarity = Rarity.Common; // Weapon mặc định Common (có thể mở rộng sau)
        }

        // Tính tỉ lệ
        float successRate = CalculateSuccessRate(targetRarity, crystal.rarity);

        // Luôn tiêu thụ crystal
        InventoryManager.Instance.RemoveItem(crystal.id, 1);

        // Roll
        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll <= successRate)
        {
            // THÀNH CÔNG — gắn gem
            bool equipped = WeaponGemManager.Instance.EquipGem(weaponType, slotIndex, gem);
            if (equipped)
            {
                Debug.Log($"[SocketingManager] SUCCESS! Socketed {gem.itemName} into {weaponType} slot {slotIndex} (rate={successRate:P0})");
                OnSocketAttempt?.Invoke(SocketResult.Success, gem, crystal);
                return SocketResult.Success;
            }
            else
            {
                // Equip failed (shouldn't happen) — refund crystal
                InventoryManager.Instance.AddItem(crystal.id, 1, crystal.rarity);
                return SocketResult.InvalidSlot;
            }
        }
        else
        {
            // THẤT BẠI — chỉ mất crystal, gem KHÔNG mất
            Debug.Log($"[SocketingManager] FAIL! Crystal consumed, gem returned. (rate={successRate:P0}, roll={roll:F2})");
            OnSocketAttempt?.Invoke(SocketResult.Fail, gem, crystal);
            return SocketResult.Fail;
        }
    }

    /// <summary>
    /// Thử khảm gem vào equipment
    /// </summary>
    public SocketResult TrySocketEquipment(int equipSlotIndex, int gemSlotIndex, Item gem, Item crystal)
    {
        // Validate
        if (gem == null || gem.itemType != ItemType.Gems)
            return SocketResult.NoGem;
        if (crystal == null || crystal.itemType != ItemType.CrystalStone)
            return SocketResult.NoCrystal;
        if (EquipmentManager.Instance == null || InventoryManager.Instance == null)
            return SocketResult.NoTarget;
        if (equipSlotIndex < 0 || equipSlotIndex >= 4)
            return SocketResult.InvalidSlot;
        if (gemSlotIndex < 0 || gemSlotIndex >= 4)
            return SocketResult.InvalidSlot;

        // Check target equipment exists
        Item equippedItem = EquipmentManager.Instance.GetEquippedItemByIndex(equipSlotIndex);
        if (equippedItem == null)
            return SocketResult.NoTarget;

        // Check inventory has both items
        if (InventoryManager.Instance.GetItemAmount(gem.id) <= 0)
            return SocketResult.NoGem;
        if (InventoryManager.Instance.GetItemAmount(crystal.id) <= 0)
            return SocketResult.NoCrystal;

        // Lấy runtime rarity của equipment
        Rarity targetRarity = EquipmentManager.Instance.GetEquippedRarity(equipSlotIndex);

        // Tính tỉ lệ
        float successRate = CalculateSuccessRate(targetRarity, crystal.rarity);

        // Luôn tiêu thụ crystal
        InventoryManager.Instance.RemoveItem(crystal.id, 1);

        // Roll
        float roll = UnityEngine.Random.Range(0f, 1f);
        if (roll <= successRate)
        {
            // THÀNH CÔNG — gắn gem vào equipment
            bool equipped = EquipmentManager.Instance.EquipGemToSlot(equipSlotIndex, gemSlotIndex, gem);
            if (equipped)
            {
                Debug.Log($"[SocketingManager] SUCCESS! Socketed {gem.itemName} into equipment slot {equipSlotIndex} gem {gemSlotIndex} (rate={successRate:P0})");
                OnSocketAttempt?.Invoke(SocketResult.Success, gem, crystal);
                return SocketResult.Success;
            }
            else
            {
                // Equip failed — refund crystal
                InventoryManager.Instance.AddItem(crystal.id, 1, crystal.rarity);
                return SocketResult.InvalidSlot;
            }
        }
        else
        {
            // THẤT BẠI — chỉ mất crystal, gem KHÔNG mất
            Debug.Log($"[SocketingManager] FAIL! Crystal consumed, gem returned. (rate={successRate:P0}, roll={roll:F2})");
            OnSocketAttempt?.Invoke(SocketResult.Fail, gem, crystal);
            return SocketResult.Fail;
        }
    }

    /// <summary>
    /// Lấy text mô tả tỉ lệ thành công
    /// </summary>
    public string GetSuccessRateText(Rarity targetRarity, Rarity crystalRarity)
    {
        float rate = CalculateSuccessRate(targetRarity, crystalRarity);
        return $"{rate * 100f:F0}%";
    }

    /// <summary>
    /// Lấy màu cho success rate bar
    /// </summary>
    public Color GetSuccessRateColor(float rate)
    {
        if (rate >= 0.9f) return new Color(0.2f, 0.9f, 0.3f);    // Xanh lá (rất cao)
        if (rate >= 0.7f) return new Color(0.5f, 0.9f, 0.2f);    // Xanh lá nhạt
        if (rate >= 0.5f) return new Color(0.9f, 0.9f, 0.2f);    // Vàng
        if (rate >= 0.3f) return new Color(0.9f, 0.6f, 0.2f);    // Cam
        if (rate >= 0.1f) return new Color(0.9f, 0.3f, 0.2f);    // Đỏ cam
        return new Color(0.9f, 0.1f, 0.1f);                       // Đỏ đậm (rất thấp)
    }
}
