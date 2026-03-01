using UnityEngine;

/// <summary>
/// Độ hiếm của item
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Dữ liệu của item hiển thị trên popup
/// </summary>
[System.Serializable]
public class ItemData
{
    [Header("Item Info")]
    public string itemName;
    public Sprite icon;
    public ItemRarity rarity = ItemRarity.Common;
    public int quantity = 1;
    
    // Constructor mặc định
    public ItemData()
    {
        itemName = "Unknown Item";
        quantity = 1;
    }
    
    // Constructor với thông tin cơ bản
    public ItemData(string name, Sprite itemIcon, ItemRarity itemRarity, int itemQuantity = 1)
    {
        itemName = name;
        icon = itemIcon;
        rarity = itemRarity;
        quantity = itemQuantity;
    }
    
    // Constructor đơn giản (sử dụng tên và rarity)
    public ItemData(string name, ItemRarity itemRarity, int itemQuantity = 1)
    {
        itemName = name;
        rarity = itemRarity;
        quantity = itemQuantity;
    }
}
