using Fusion;
using UnityEngine;
using TMPro;

/// <summary>
/// Gắn trên player prefab (root, cùng NetworkObject).
/// Sync tên player qua network + hiện tên trên đầu player (World Space).
///
/// Flow:
/// 1. Menu: player nhập tên → lưu vào static <see cref="LocalPlayerName"/>
/// 2. Spawned (input authority): ghi tên vào [Networked]
/// 3. Remote: đọc [Networked] → hiện tên trên đầu player
/// 4. Owner: có thể ẩn/hiện tên trên đầu mình (mặc định: hiện)
/// </summary>
[DefaultExecutionOrder(220)]
public class NetworkPlayerName : NetworkBehaviour
{
    /// <summary>Tên player local — set trước khi Start Host/Client.</summary>
    public static string LocalPlayerName { get; set; } = "Player";

    [Networked, Capacity(64)]
    public string NetName { get; set; }

    [Header("Name Tag Settings")]
    [Tooltip("Offset trên đầu player (Y).")]
    [SerializeField] private float nameTagHeight = 2.2f;
    [Tooltip("Font size tên.")]
    [SerializeField] private float fontSize = 5f;
    [Tooltip("Hiện tên trên đầu chính mình (owner)?")]
    [SerializeField] private bool showOwnName = false;

    private GameObject _nameTagGO;
    private TextMeshPro _nameText; // TextMeshPro (World Space) thay vì TextMeshProUGUI
    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        if (HasInputAuthority)
        {
            // Ghi tên local vào network
            string name = string.IsNullOrWhiteSpace(LocalPlayerName)
                ? $"Player_{Object.InputAuthority}"
                : LocalPlayerName;

            if (HasStateAuthority)
            {
                // Host player: ghi trực tiếp
                NetName = name;
            }
            else
            {
                // BUG-1b fix: Client player gửi qua RPC
                RPC_SetPlayerName(name);
            }
            Debug.Log($"[NetworkPlayerName] Local player name set to: '{name}'");
        }

        // Tạo name tag
        CreateNameTag();
        UpdateNameTag(NetName);

        // Owner: ẩn/hiện tùy setting
        if (HasInputAuthority && _nameTagGO != null)
            _nameTagGO.SetActive(showOwnName);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string playerName)
    {
        NetName = playerName;
        Debug.Log($"[NetworkPlayerName] RPC: Name set to '{playerName}' for {Object.InputAuthority}");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_nameTagGO != null)
            Destroy(_nameTagGO);
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(NetName))
            {
                UpdateNameTag(NetName);
                Debug.Log($"[NetworkPlayerName] Name updated: '{NetName}' (player={Object.InputAuthority})");
            }
        }
    }

    void LateUpdate()
    {
        if (_nameTagGO == null || !_nameTagGO.activeSelf) return;

        // Giữ name tag trên đầu player
        _nameTagGO.transform.position = transform.position + Vector3.up * nameTagHeight;

        // Billboard: luôn quay về camera
        var cam = Camera.main;
        if (cam != null)
        {
            _nameTagGO.transform.rotation = Quaternion.LookRotation(
                _nameTagGO.transform.position - cam.transform.position);
        }
    }

    // ──────────── Name Tag Creation ────────────

    void CreateNameTag()
    {
        if (_nameTagGO != null) return; // Tránh tạo trùng

        _nameTagGO = new GameObject($"NameTag_{Object.InputAuthority}");
        _nameTagGO.transform.SetParent(null); // World space, không follow rotation

        // TextMeshPro (World Space) — không cần Canvas
        _nameText = _nameTagGO.AddComponent<TextMeshPro>();
        _nameText.fontSize = fontSize;
        _nameText.alignment = TextAlignmentOptions.Center;
        _nameText.color = Color.white;
        _nameText.outlineWidth = 0.25f;
        _nameText.outlineColor = new Color32(0, 0, 0, 200);
        _nameText.enableAutoSizing = false;
        _nameText.sortingOrder = 100;

        // Size cho text
        var rt = _nameTagGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(6f, 1.5f);

        // Vị trí ban đầu
        _nameTagGO.transform.position = transform.position + Vector3.up * nameTagHeight;

        Debug.Log($"[NetworkPlayerName] Created name tag for player {Object.InputAuthority}");
    }

    void UpdateNameTag(string name)
    {
        if (_nameText != null && !string.IsNullOrEmpty(name))
            _nameText.text = name;
    }

    void OnDestroy()
    {
        if (_nameTagGO != null)
            Destroy(_nameTagGO);
    }
}
