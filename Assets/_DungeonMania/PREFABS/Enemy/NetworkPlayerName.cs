using UnityEngine;
using TMPro;

/// <summary>
/// Tên player (menu → <see cref="LocalPlayerName"/>) + name tag world-space.
/// </summary>
[DefaultExecutionOrder(220)]
public class NetworkPlayerName : MonoBehaviour
{
    public static string LocalPlayerName { get; set; } = "Player";

    [SerializeField] private float nameTagHeight = 2.2f;
    [SerializeField] private float fontSize = 4f;

    private GameObject _nameTagGO;
    private TextMeshProUGUI _nameText;

    void Start()
    {
        string name = string.IsNullOrWhiteSpace(LocalPlayerName) ? "Player" : LocalPlayerName;
        CreateNameTag();
        UpdateNameTag(name);
        if (_nameTagGO != null)
            _nameTagGO.SetActive(false);
    }

    void LateUpdate()
    {
        if (_nameTagGO == null || !_nameTagGO.activeSelf) return;
        _nameTagGO.transform.position = transform.position + Vector3.up * nameTagHeight;
        if (Camera.main != null)
        {
            _nameTagGO.transform.rotation = Quaternion.LookRotation(
                _nameTagGO.transform.position - Camera.main.transform.position);
        }
    }

    void CreateNameTag()
    {
        _nameTagGO = new GameObject("NameTag");
        _nameTagGO.transform.SetParent(null);

        var canvas = _nameTagGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        var rt = _nameTagGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 50);
        rt.localScale = Vector3.one * 0.01f;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(_nameTagGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        _nameText = textGO.GetComponent<TextMeshProUGUI>();
        _nameText.fontSize = fontSize;
        _nameText.alignment = TextAlignmentOptions.Center;
        _nameText.color = Color.white;
        _nameText.outlineWidth = 0.2f;
        _nameText.outlineColor = Color.black;
    }

    void UpdateNameTag(string name)
    {
        if (_nameText != null)
            _nameText.text = name;
    }

    void OnDestroy()
    {
        if (_nameTagGO != null)
            Destroy(_nameTagGO);
    }
}
