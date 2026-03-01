using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Tự động thêm hiệu ứng hover cho button:
/// - Đổi màu text thành vàng khi di chuột vào
/// - Hiện Selected_line (nếu có) khi di chuột vào
/// - Khôi phục khi di chuột ra
/// </summary>
[RequireComponent(typeof(EventTrigger))]
public class ButtonHoverEffect : MonoBehaviour
{
    [Header("Hover Settings")]
    [SerializeField] private Color hoverColor = Color.yellow;
    
    private TextMeshProUGUI textComp;
    private Color originalColor;
    private GameObject selectedLine;
    private EventTrigger eventTrigger;

    private void Awake()
    {
        // Lấy text component
        textComp = GetComponentInChildren<TextMeshProUGUI>(true);
        if (textComp != null)
        {
            originalColor = textComp.color;
        }

        // Tìm Selected_line là con
        selectedLine = transform.Find("Selected_line")?.gameObject;
        if (selectedLine == null)
        {
            // Tìm đệ quy
            selectedLine = FindChildRecursive(transform, "Selected_line");
        }

        // Ẩn selected line ban đầu
        if (selectedLine != null)
        {
            selectedLine.SetActive(false);
        }

        // Setup EventTrigger
        eventTrigger = GetComponent<EventTrigger>();
        SetupTrigger();
    }

    private void SetupTrigger()
    {
        // Pointer Enter
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener(OnPointerEnter);
        eventTrigger.triggers.Add(enterEntry);

        // Pointer Exit
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener(OnPointerExit);
        eventTrigger.triggers.Add(exitEntry);
    }

    public void OnPointerEnter(BaseEventData data)
    {
        if (textComp != null)
        {
            textComp.color = hoverColor;
        }
        if (selectedLine != null)
        {
            selectedLine.SetActive(true);
        }
    }

    public void OnPointerExit(BaseEventData data)
    {
        if (textComp != null)
        {
            textComp.color = originalColor;
        }
        if (selectedLine != null)
        {
            selectedLine.SetActive(false);
        }
    }

    private GameObject FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
            {
                return child.gameObject;
            }
            GameObject result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
