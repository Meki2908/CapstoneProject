using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Demo script để test Item Popup - Nhấn phím để hiển thị item
/// </summary>
public class ItemPopupDemo : MonoBehaviour
{
    [Header("Test Items")]
    [SerializeField] private List<Sprite> _testIcons;
    [SerializeField] private string[] _testItemNames = 
    {
        "Health Potion",
        "Mana Crystal",
        "Gold Coin",
        "Ancient Relic",
        "Dragon Scale"
    };
    
    private void Update()
    {
        // Nhấn phím 1-5 để test từng loại item
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ShowRandomItem();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ShowMultipleItems();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ShowAllRarities();
        }
    }
    
    private void ShowRandomItem()
    {
        if (ItemPopupController.Instance == null)
        {
            Debug.LogWarning("ItemPopupController chưa được khởi tạo!");
            return;
        }
        
        int randomIndex = Random.Range(0, _testItemNames.Length);
        int randomRarity = Random.Range(0, 5);
        
        ItemData item = new ItemData
        {
            itemName = _testItemNames[randomIndex],
            rarity = (ItemRarity)randomRarity,
            quantity = Random.Range(1, 10)
        };
        
        // Gán icon nếu có
        if (_testIcons != null && _testIcons.Count > 0)
        {
            item.icon = _testIcons[Random.Range(0, _testIcons.Count)];
        }
        
        ItemPopupController.Instance.ShowItemPopup(item);
        
        Debug.Log($"Đã hiển thị: {item.itemName} x{item.quantity} - {(ItemRarity)randomRarity}");
    }
    
    private void ShowMultipleItems()
    {
        if (ItemPopupController.Instance == null)
        {
            Debug.LogWarning("ItemPopupController chưa được khởi tạo!");
            return;
        }
        
        List<ItemData> items = new List<ItemData>();
        
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, _testItemNames.Length);
            
            ItemData item = new ItemData
            {
                itemName = _testItemNames[randomIndex],
                rarity = (ItemRarity)Random.Range(0, 5),
                quantity = Random.Range(1, 5)
            };
            
            if (_testIcons != null && _testIcons.Count > 0)
            {
                item.icon = _testIcons[Random.Range(0, _testIcons.Count)];
            }
            
            items.Add(item);
        }
        
        ItemPopupController.Instance.ShowMultipleItemPopups(items);
        
        Debug.Log($"Đã hiển thị {items.Count} items");
    }
    
    private void ShowAllRarities()
    {
        if (ItemPopupController.Instance == null)
        {
            Debug.LogWarning("ItemPopupController chưa được khởi tạo!");
            return;
        }
        
        List<ItemData> items = new List<ItemData>();
        
        // Hiển thị tất cả các loại rarity
        string[] rarityNames = { "Common Sword", "Uncommon Shield", "Rare Ring", "Epic Armor", "Legendary Weapon" };
        
        for (int i = 0; i < 5; i++)
        {
            ItemData item = new ItemData
            {
                itemName = rarityNames[i],
                rarity = (ItemRarity)i,
                quantity = 1
            };
            
            if (_testIcons != null && _testIcons.Count > 0)
            {
                item.icon = _testIcons[Mathf.Min(i, _testIcons.Count - 1)];
            }
            
            items.Add(item);
        }
        
        ItemPopupController.Instance.ShowMultipleItemPopups(items);
        
        Debug.Log("Đã hiển thị tất cả các loại rarity");
    }
    
    // GUI để test trong Build
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button);
        style.fontSize = 20;
        
        GUILayout.BeginArea(new Rect(20, Screen.height - 150, 300, 130));
        
        if (GUILayout.Button("Test 1 Item (Phím 1)", style))
        {
            ShowRandomItem();
        }
        
        if (GUILayout.Button("Test Multiple Items (Phím 2)", style))
        {
            ShowMultipleItems();
        }
        
        if (GUILayout.Button("Test All Rarities (Phím 3)", style))
        {
            ShowAllRarities();
        }
        
        GUILayout.EndArea();
    }
}
