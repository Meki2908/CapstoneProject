using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller cho Item Drop Popup với hiệu ứng thả xuống giống Genshin Impact
/// </summary>
public class ItemPopupController : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private VisualTreeAsset _popupTemplate;
    [SerializeField] private StyleSheet _popupStyleSheet;
    
    [Header("Animation Settings")]
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _maxPopupX = 5; // Số popup tối đa cùng lúc
    
    [Header("Position Settings")]
    [SerializeField] private float _popupSpacing = 10f;
    
    // Queue để xử lý nhiều item
    private Queue<ItemData> _itemQueue = new Queue<ItemData>();
    private List<VisualElement> _activePopups = new List<VisualElement>();
    private bool _isProcessingQueue = false;
    
    // Singleton instance
    private static ItemPopupController _instance;
    public static ItemPopupController Instance => _instance;
    
    private void Awake()
    {
        // Singleton pattern
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        if (_uiDocument == null)
        {
            _uiDocument = GetComponent<UIDocument>();
        }
        
        if (_uiDocument == null)
        {
            // Tạo mới UIDocument nếu không có
            _uiDocument = gameObject.AddComponent<UIDocument>();
        }
        
        // Load template nếu chưa có
        if (_popupTemplate == null)
        {
            _popupTemplate = Resources.Load<VisualTreeAsset>("UI Toolkit/ItemPopup/ItemDropPopup");
        }
        
        // Load stylesheet nếu chưa có
        if (_popupStyleSheet == null)
        {
            _popupStyleSheet = Resources.Load<StyleSheet>("UI Toolkit/ItemPopup/ItemDropPopup");
        }
        
        // Áp dụng stylesheet cho root nếu có
        if (_popupStyleSheet != null)
        {
            _uiDocument.rootVisualElement.styleSheets.Add(_popupStyleSheet);
        }
    }
    
    /// <summary>
    /// Hiển thị item popup - Gọi phương thức này khi nhặt item
    /// </summary>
    /// <param name="itemData">Thông tin item cần hiển thị</param>
    public void ShowItemPopup(ItemData itemData)
    {
        _itemQueue.Enqueue(itemData);
        
        if (!_isProcessingQueue)
        {
            StartCoroutine(ProcessQueue());
        }
    }
    
    /// <summary>
    /// Hiển thị nhiều item cùng lúc
    /// </summary>
    /// <param name="items">Danh sách item</param>
    public void ShowMultipleItemPopups(List<ItemData> items)
    {
        foreach (var item in items)
        {
            _itemQueue.Enqueue(item);
        }
        
        if (!_isProcessingQueue)
        {
            StartCoroutine(ProcessQueue());
        }
    }
    
    private IEnumerator ProcessQueue()
    {
        _isProcessingQueue = true;
        
        while (_itemQueue.Count > 0)
        {
            // Chờ cho đến khi có chỗ trống
            while (_activePopups.Count >= _maxPopupX)
            {
                yield return null;
            }
            
            ItemData item = _itemQueue.Dequeue();
            CreateAndShowPopup(item);
            
            // Delay nhỏ giữa các popup để tạo hiệu ứng đẹp
            yield return new WaitForSeconds(0.15f);
        }
        
        _isProcessingQueue = false;
    }
    
    private void CreateAndShowPopup(ItemData itemData)
    {
        // Tạo popup mới từ template
        VisualElement popup = CreatePopupFromTemplate();
        
        // Thêm vào root
        var root = _uiDocument.rootVisualElement;
        root.Add(popup);
        
        // Thiết lập vị trí dựa trên số popup đang active
        PositionPopup(popup, _activePopups.Count);
        
        // Thiết lập nội dung
        SetupPopupContent(popup, itemData);
        
        // Thêm vào danh sách active
        _activePopups.Add(popup);
        
        // Bắt đầu animation
        StartCoroutine(AnimatePopup(popup));
    }
    
    private VisualElement CreatePopupFromTemplate()
    {
        VisualElement popup;
        
        if (_popupTemplate != null)
        {
            popup = _popupTemplate.Instantiate();
        }
        // Fallback: Tạo thủ công nếu không có template
        else
        {
            popup = CreateManualPopup();
        }
        
        return popup;
    }
    
    private VisualElement CreateManualPopup()
    {
        // Tạo popup thủ công nếu không có UXML template
        var popup = new VisualElement();
        popup.name = "item-drop-popup";
        popup.AddToClassList("item-drop-popup");
        
        // Icon container
        var iconContainer = new VisualElement();
        iconContainer.name = "item-icon-container";
        iconContainer.AddToClassList("item-icon-container");
        
        var iconBg = new VisualElement();
        iconBg.name = "item-icon-bg";
        iconBg.AddToClassList("item-icon-bg");
        iconContainer.Add(iconBg);
        
        var icon = new Image();
        icon.name = "item-icon";
        icon.AddToClassList("item-icon");
        iconContainer.Add(icon);
        
        // Item info
        var info = new VisualElement();
        info.name = "item-info";
        info.AddToClassList("item-info");
        
        var nameLabel = new Label();
        nameLabel.name = "item-name";
        nameLabel.AddToClassList("item-name");
        info.Add(nameLabel);
        
        var rarityLabel = new Label();
        rarityLabel.name = "item-rarity";
        rarityLabel.AddToClassList("item-rarity");
        info.Add(rarityLabel);
        
        // Quantity badge
        var quantityBadge = new VisualElement();
        quantityBadge.name = "quantity-badge";
        quantityBadge.AddToClassList("quantity-badge");
        
        var quantityText = new Label();
        quantityText.name = "quantity-text";
        quantityText.AddToClassList("quantity-text");
        quantityBadge.Add(quantityText);
        
        // Thêm tất cả vào popup
        popup.Add(iconContainer);
        popup.Add(info);
        popup.Add(quantityBadge);
        
        return popup;
    }
    
    private void PositionPopup(VisualElement popup, int index)
    {
        // Đặt vị trí popup dựa trên index
        // Genshin style: các popup xếp từ trên xuống, căn giữa
        float yPosition = 20 + (index * (80 + _popupSpacing));
        
        popup.style.top = yPosition;
        popup.style.left = 20; // Cách lề trái
        popup.style.position = Position.Absolute;
    }
    
    private void SetupPopupContent(VisualElement popup, ItemData itemData)
    {
        // Tìm các element con
        var icon = popup.Q<Image>("item-icon");
        var nameLabel = popup.Q<Label>("item-name");
        var rarityLabel = popup.Q<Label>("item-rarity");
        var quantityText = popup.Q<Label>("quantity-text");
        
        // Thiết lập icon
        if (itemData.icon != null)
        {
            icon.sprite = itemData.icon;
        }
        
        // Thiết lập tên
        nameLabel.text = itemData.itemName;
        
        // Thiết lập độ hiếm
        rarityLabel.text = GetRarityName(itemData.rarity);
        
        // Thiết lập số lượng
        if (itemData.quantity > 1)
        {
            quantityText.text = "x" + itemData.quantity;
            quantityText.parent.style.display = DisplayStyle.Flex;
        }
        else
        {
            quantityText.parent.style.display = DisplayStyle.None;
        }
        
        // Thiết lập class cho rarity
        popup.AddToClassList("rarity-" + itemData.rarity.ToString().ToLower());
    }
    
    private string GetRarityName(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "Common";
            case ItemRarity.Uncommon: return "Uncommon";
            case ItemRarity.Rare: return "Rare";
            case ItemRarity.Epic: return "Epic";
            case ItemRarity.Legendary: return "Legendary";
            default: return "Common";
        }
    }
    
    private IEnumerator AnimatePopup(VisualElement popup)
    {
        // Drop in animation
        popup.AddToClassList("drop-in");
        
        // Đợi animation drop hoàn thành
        yield return new WaitForSeconds(0.6f);
        
        // Chuyển sang idle state
        popup.RemoveFromClassList("drop-in");
        popup.AddToClassList("idle");
        
        // Đợi display duration
        yield return new WaitForSeconds(_displayDuration);
        
        // Fade out
        popup.RemoveFromClassList("idle");
        popup.AddToClassList("fade-out");
        
        // Đợi fade out hoàn thành
        yield return new WaitForSeconds(0.4f);
        
        // Xóa popup
        RemovePopup(popup);
    }
    
    private void RemovePopup(VisualElement popup)
    {
        if (_activePopups.Contains(popup))
        {
            _activePopups.Remove(popup);
        }
        
        // Reposition các popup còn lại
        RepositionPopups();
        
        // Hủy popup
        popup.RemoveFromHierarchy();
    }
    
    private void RepositionPopups()
    {
        for (int i = 0; i < _activePopups.Count; i++)
        {
            float yPosition = 20 + (i * (80 + _popupSpacing));
            _activePopups[i].style.top = yPosition;
        }
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
