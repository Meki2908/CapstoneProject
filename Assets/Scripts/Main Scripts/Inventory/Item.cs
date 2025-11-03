using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class Item : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;
    public float value;
    // public bool isStackable;
    // public int stackSize;
    // public int maxStackSize;
    // public int maxStackSize;
}
