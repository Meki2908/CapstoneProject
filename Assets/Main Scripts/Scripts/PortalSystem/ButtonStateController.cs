using UnityEngine;
using UnityEngine.UI;
using TMPro; // Nếu bạn dùng TextMeshPro, nếu không thì dùng UnityEngine.UI.Text

[RequireComponent(typeof(Button))]
public class ButtonStateController : MonoBehaviour
{
    private Button targetButton;
    private TextMeshProUGUI buttonText;

    [Header("Settings")]
    public string activeText = "Available";
    public string disabledText = "Locked";
    public bool hideTextWhenDisabled = true; // Thêm option này
    
    [SerializeField] private bool disableOnStart = false;

    private void Awake()
    {
        targetButton = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (disableOnStart)
        {
            SetDisable();
        }
    }

    /// <summary>
    /// Vô hiệu hóa nút bấm
    /// </summary>
    public void SetDisable()
    {
        if (targetButton != null)
        {
            targetButton.interactable = false;
        }

        if (buttonText != null)
        {
            if (hideTextWhenDisabled) 
                buttonText.gameObject.SetActive(false); // Ẩn hoàn toàn object chữ
            else 
                buttonText.text = disabledText;
        }
        
        Debug.Log($"[ButtonState] {gameObject.name} đã bị Disable.");
    }

    /// <summary>
    /// Kích hoạt lại nút bấm
    /// </summary>
    public void SetEnable()
    {
        if (targetButton != null)
        {
            targetButton.interactable = true;
        }

        if (buttonText != null)
        {
            buttonText.gameObject.SetActive(true); // Hiện lại object chữ
            buttonText.text = activeText;
        }

        Debug.Log($"[ButtonState] {gameObject.name} đã được Enable.");
    }

    /// <summary>
    /// Chuyển đổi trạng thái (Toggle)
    /// </summary>
    public void ToggleState(bool isOn)
    {
        if (isOn) SetEnable();
        else SetDisable();
    }
}
