using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Đảm bảo Text luôn hiển thị phía trên Image trong UI
/// Thêm script này vào các Text/TextMeshPro cần hiển thị trên cùng
/// </summary>
[ExecuteAlways]
public class TextOverImage : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Giá trị sorting order cao hơn sẽ hiển thị trên")]
    public int additionalSortingOrder = 1;
    
    private Canvas textCanvas;
    
    void Start()
    {
        EnsureTextOnTop();
    }
    
    void OnEnable()
    {
        EnsureTextOnTop();
    }
    
    /// <summary>
    /// Đảm bảo text hiển thị trên image bằng cách thêm Canvas component
    /// </summary>
    public void EnsureTextOnTop()
    {
        // Thêm Canvas component nếu chưa có
        textCanvas = GetComponent<Canvas>();
        if (textCanvas == null)
        {
            textCanvas = gameObject.AddComponent<Canvas>();
        }
        
        // Bật override sorting để text render riêng
        textCanvas.overrideSorting = true;
        textCanvas.sortingOrder = additionalSortingOrder;
        
        // Thêm GraphicRaycaster nếu cần tương tác
        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }
    }
    
    void OnValidate()
    {
        if (textCanvas != null)
        {
            textCanvas.sortingOrder = additionalSortingOrder;
        }
    }
}
