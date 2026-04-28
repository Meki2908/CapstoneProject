using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Passes drag events from a child UI element (e.g. Button) up to the parent ScrollRect,
/// allowing click-drag scrolling to work even on clickable items.
/// </summary>
public class ScrollDragPassthrough : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ScrollRect parentScrollRect;

    void Awake()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null)
            parentScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null)
            parentScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parentScrollRect != null)
            parentScrollRect.OnEndDrag(eventData);
    }
}
