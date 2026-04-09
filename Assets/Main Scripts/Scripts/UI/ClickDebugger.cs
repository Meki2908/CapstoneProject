using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ClickDebugger : MonoBehaviour
{
    void Update()
    {
        if (EventSystem.current == null) return;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        
        // Hoạt động với cả Input System cũ và mới (tương đồng toạ độ màn hình)
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Mouse.current != null)
            eventData.position = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        else
            eventData.position = Input.mousePosition;
        #else
        eventData.position = Input.mousePosition;
        #endif

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            // Tên của object ĐẦU TIÊN (nằm trên cùng) chặn click chuột
            Debug.Log($"[ClickDebugger] Raycast hit: {results[0].gameObject.name} (Layer: {LayerMask.LayerToName(results[0].gameObject.layer)}) - Parent: {results[0].gameObject.transform.parent?.name}");
        }
    }
}
