using UnityEngine;
using UnityEngine.UI;

public class MapPortalMarker : MonoBehaviour
{
    public RectTransform Rect { get; private set; }
    public Transform Target { get; private set; }

    Button _button;
    BigMapController _map;

    void Awake()
    {
        Rect = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
    }

    // Chỉ dùng 2 tham số để khỏi mismatch
    public void Init(BigMapController map, Transform target)
    {
        _map = map;
        Target = target;

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() =>
            {
                if (_map != null && Target != null)
                    _map.TeleportTo(Target.position);
            });
        }
    }
}
