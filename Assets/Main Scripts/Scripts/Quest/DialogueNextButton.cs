using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên nút Next trong Dialogue Canvas dùng chung.
/// Khi click → gọi OnNextClicked() của NPC đang mở dialogue.
/// </summary>
public class DialogueNextButton : MonoBehaviour
{
    private Button _btn;

    // NPC dialogue đang active (static để bất kỳ NPC nào cũng set được)
    private static System.Action _currentNextAction;

    void Awake()
    {
        _btn = GetComponent<Button>();
        if (_btn != null)
            _btn.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        if (_currentNextAction != null)
            _currentNextAction.Invoke();
    }

    // ── Gọi từ NPC scripts ──

    /// <summary>
    /// NPC gọi khi MỞ dialogue — đăng ký callback
    /// VD: DialogueNextButton.Register(() => OnNextClicked());
    /// </summary>
    public static void Register(System.Action nextAction)
    {
        _currentNextAction = nextAction;
    }

    /// <summary>
    /// NPC gọi khi ĐÓNG dialogue — hủy callback
    /// </summary>
    public static void Unregister()
    {
        _currentNextAction = null;
    }
}
