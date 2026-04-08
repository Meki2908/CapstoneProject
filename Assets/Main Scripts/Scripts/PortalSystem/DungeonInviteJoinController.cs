using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel invite dungeon — NGO RPC đã gỡ; Accept/Refuse chỉ đóng UI.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class DungeonInviteJoinController : MonoBehaviour
{
    public static DungeonInviteJoinController Instance { get; private set; }
    static bool? _pendingVisible;

    [Tooltip("Để trống = chính GameObject này.")]
    [SerializeField] GameObject panelRoot;

    void Awake()
    {
        Instance = this;
        if (panelRoot == null)
            panelRoot = gameObject;
        if (_pendingVisible.HasValue)
        {
            SetPanelVisibleInternal(_pendingVisible.Value);
            _pendingVisible = null;
        }
    }

    void Start()
    {
        DungeonLobbyInviteNetwork.EnsureRegistered();
        WireAcceptRefuseButtons();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void WireAcceptRefuseButtons()
    {
        foreach (var st in GetComponentsInChildren<SceneTeleportButton>(true))
        {
            st.enabled = false;
            var b = st.GetComponent<Button>();
            if (b != null)
                b.onClick.RemoveAllListeners();
        }

        var accept = FindChildByName(transform, "Accept");
        if (accept != null)
        {
            var btn = accept.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(OnAcceptClicked);
        }

        var refuse = FindChildByName(transform, "Refuse");
        if (refuse != null)
        {
            var btn = refuse.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(OnRefuseClicked);
        }
    }

    static Transform FindChildByName(Transform root, string exactName)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == exactName)
                return t;
        }
        return null;
    }

    void OnAcceptClicked()
    {
        SetPanelVisibleInternal(false);
    }

    void OnRefuseClicked()
    {
        SetPanelVisibleInternal(false);
    }

    public void ShowPanel()
    {
        SetPanelVisibleInternal(true);
    }

    public void HidePanel()
    {
        SetPanelVisibleInternal(false);
    }

    void SetPanelVisibleInternal(bool visible)
    {
        if (panelRoot == null) return;
        if (visible)
            EnsureAncestorsActive(panelRoot.transform);
        panelRoot.SetActive(visible);
    }

    static void EnsureAncestorsActive(Transform leaf)
    {
        if (leaf == null) return;
        int d = 0;
        for (Transform t = leaf; t != null; t = t.parent)
            d++;
        var chain = new Transform[d];
        int idx = d - 1;
        for (Transform t = leaf; t != null; t = t.parent)
            chain[idx--] = t;
        for (int i = 0; i < d; i++)
        {
            if (!chain[i].gameObject.activeSelf)
                chain[i].gameObject.SetActive(true);
        }
    }

    public static void SetInvitePanelVisible(bool visible)
    {
        if (Instance != null)
        {
            Instance.SetPanelVisibleInternal(visible);
            return;
        }

        var found = Object.FindFirstObjectByType<DungeonInviteJoinController>(FindObjectsInactive.Include);
        if (found != null)
        {
            Instance = found;
            found.SetPanelVisibleInternal(visible);
            return;
        }

        _pendingVisible = visible;
    }
}
