using UnityEngine;

public enum ItemType { Armor, Consumable, Material, Gems }
public enum Rarity { Common, Epic, Legendary }

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public Rarity rarity;
    public bool isStackable;
    public int maxStackSize;
    public string description;
    public ItemType itemType;
}
