using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Một nút bấm → chuyển sang map tiếp theo (vòng tròn).
/// Gắn script này vào bất kỳ GameObject nào trong panel map.
/// </summary>
public class MapImageSwitcher : MonoBehaviour
{
    [Header("Danh sách ảnh map (theo thứ tự)")]
    public GameObject[] mapImages;

    [Header("Nút chuyển map (1 nút duy nhất)")]
    public Button cycleButton;

    [Header("Bắt đầu từ index")]
    public int startIndex = 0;

    private int _currentIndex = 0;
    private bool _listenerAdded = false;

    void OnEnable()
    {
        if (!_listenerAdded && cycleButton != null)
        {
            cycleButton.onClick.AddListener(NextMap);
            _listenerAdded = true;
        }

        _currentIndex = startIndex;
        ShowCurrent();
    }

    public void NextMap()
    {
        if (mapImages == null || mapImages.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % mapImages.Length;
        ShowCurrent();
    }

    void ShowCurrent()
    {
        for (int i = 0; i < mapImages.Length; i++)
        {
            if (mapImages[i] != null)
                mapImages[i].SetActive(i == _currentIndex);
        }
    }
}
