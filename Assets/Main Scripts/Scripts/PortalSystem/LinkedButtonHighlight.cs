using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Script giúp đồng bộ trạng thái Highlight (rê chuột vào) giữa 2 Button hoặc Selectable.
/// Gán script này vào Button chính, và kéo Button phụ (trên bản đồ) vào ô Linked Selectable.
/// </summary>
public class LinkedButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Button hoặc Marker trên MiniMap mà bạn muốn nó highlight theo")]
    public Selectable linkedSelectable;

    // Khi chuột rê vào Button này
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (linkedSelectable != null)
        {
            // Giả lập sự kiện rê chuột vào button phụ
            linkedSelectable.OnPointerEnter(eventData);
        }
    }

    // Khi chuột rời khỏi Button này
    public void OnPointerExit(PointerEventData eventData)
    {
        if (linkedSelectable != null)
        {
            // Giả lập sự kiện rời chuột khỏi button phụ
            linkedSelectable.OnPointerExit(eventData);
        }
    }

    // Đồng bộ cả khi Click (nếu cần)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (linkedSelectable != null)
        {
            linkedSelectable.OnPointerDown(eventData);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (linkedSelectable != null)
        {
            linkedSelectable.OnPointerUp(eventData);
        }
    }
}
