using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Một nút bấm → chuyển sang map tiếp theo (vòng tròn).
/// Mỗi entry có: ảnh map + tên map hiển thị lên Text.
/// </summary>
public class MapImageSwitcher : MonoBehaviour
{
    [Serializable]
    public class MapEntry
    {
        public string mapName = "Map";        // Tên hiển thị
        public GameObject mapImageObject;     // GameObject chứa ảnh map
    }

    [Header("Danh sách map (theo thứ tự)")]
    public MapEntry[] maps;

    [Header("Nút chuyển map (1 nút duy nhất)")]
    public Button cycleButton;

    [Header("Text hiển thị tên map")]
    public TMP_Text mapNameText;             // kéo TMP_Text vào đây
    // Nếu dùng Text thường thay TMP thì đổi dòng trên thành: public Text mapNameText;

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
        if (maps == null || maps.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % maps.Length;
        ShowCurrent();
    }

    void ShowCurrent()
    {
        if (maps == null || maps.Length == 0) return;

        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i].mapImageObject != null)
                maps[i].mapImageObject.SetActive(i == _currentIndex);
        }

        // Cập nhật tên map
        if (mapNameText != null)
            mapNameText.text = maps[_currentIndex].mapName;

        Debug.Log($"[MapImageSwitcher] Map: {maps[_currentIndex].mapName} ({_currentIndex + 1}/{maps.Length})");
    }
}
