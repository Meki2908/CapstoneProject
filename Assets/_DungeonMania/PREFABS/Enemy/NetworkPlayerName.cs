using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

/// <summary>
/// Gắn trên player prefab (root, cùng NetworkObject).
/// Sync tên player qua network + hiện tên trên đầu player cho remote.
///
/// Flow:
/// 1. Menu: player nhập tên → lưu vào static <see cref="LocalPlayerName"/>
/// 2. OnNetworkSpawn (owner): ghi tên vào NetworkVariable
/// 3. Remote: đọc NetworkVariable → hiện WorldSpace Canvas trên đầu player
/// 4. Owner: ẩn tên trên đầu mình (camera nhìn xuống sẽ block)
/// </summary>
[DefaultExecutionOrder(220)]
public class NetworkPlayerName : NetworkBehaviour
{
    /// <summary>Tên player local — set trước khi Start Host/Client.</summary>
    public static string LocalPlayerName { get; set; } = "Player";

    public readonly NetworkVariable<FixedString64Bytes> NetName = new(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Name Tag Settings")]
    [Tooltip("Offset trên đầu player (Y).")]
    [SerializeField] private float nameTagHeight = 2.2f;
    [Tooltip("Font size tên.")]
    [SerializeField] private float fontSize = 4f;

    private GameObject _nameTagGO;
    private TextMeshProUGUI _nameText;
    private Canvas _nameCanvas;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Ghi tên local vào network
            string name = string.IsNullOrWhiteSpace(LocalPlayerName) ? $"Player_{OwnerClientId}" : LocalPlayerName;
            NetName.Value = new FixedString64Bytes(name);
        }

        // Listen for name changes
        NetName.OnValueChanged += OnNameChanged;

        // Tạo name tag cho remote players
        CreateNameTag();
        UpdateNameTag(NetName.Value.ToString());

        // Owner ẩn tên trên đầu mình
        if (IsOwner && _nameTagGO != null)
            _nameTagGO.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        NetName.OnValueChanged -= OnNameChanged;

        if (_nameTagGO != null)
            Destroy(_nameTagGO);
    }

    void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes current)
    {
        UpdateNameTag(current.ToString());
    }

    void LateUpdate()
    {
        if (_nameTagGO == null || !_nameTagGO.activeSelf) return;

        // Giữ name tag trên đầu player
        _nameTagGO.transform.position = transform.position + Vector3.up * nameTagHeight;

        // Billboard: luôn quay về camera
        if (Camera.main != null)
        {
            _nameTagGO.transform.rotation = Quaternion.LookRotation(
                _nameTagGO.transform.position - Camera.main.transform.position);
        }
    }

    // ──────────── Name Tag Creation ────────────

    void CreateNameTag()
    {
        _nameTagGO = new GameObject($"NameTag_{OwnerClientId}");
        _nameTagGO.transform.SetParent(null); // World space, không follow rotation

        // Canvas (World Space)
        _nameCanvas = _nameTagGO.AddComponent<Canvas>();
        _nameCanvas.renderMode = RenderMode.WorldSpace;
        _nameCanvas.sortingOrder = 100;

        var rt = _nameTagGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 50);
        rt.localScale = Vector3.one * 0.01f; // Scale down cho world space

        // Text
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
        _nameText.enableAutoSizing = false;
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
