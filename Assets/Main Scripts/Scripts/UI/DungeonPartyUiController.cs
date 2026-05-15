using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DungeonPartyUiController : MonoBehaviour
{
    static DungeonPartyUiController _instance;
    static bool _lastShowInvitePanel;

    readonly List<GameObject> _invitationPanels = new List<GameObject>();
    readonly List<GameObject> _preStartPanels = new List<GameObject>();
    readonly List<Slider> _waitThresholdSliders = new List<Slider>();
    readonly List<TMP_Text> _waitRemainingTexts = new List<TMP_Text>();
    readonly List<Button> _acceptButtons = new List<Button>();
    readonly List<Button> _declineButtons = new List<Button>();
    readonly List<Button> _startButtons = new List<Button>();
    TMP_Text _retryAcceptedText;
    float _nextRefreshUiRefsAt;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureExistsOnLoad()
    {
        if (_instance != null)
            return;

        var go = new GameObject("DungeonPartyUiController");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<DungeonPartyUiController>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        DungeonPartyRuntime.OnChanged += HandlePartyStateChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DungeonPartyRuntime.OnChanged -= HandlePartyStateChanged;
    }

    void Start()
    {
        RefreshReferences(force: true);
        ApplyUiState();
    }

    void Update()
    {
        if (Time.unscaledTime >= _nextRefreshUiRefsAt)
            RefreshReferences(force: false);
        ApplyUiState();
    }

    void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        Dbg($"OnSceneLoaded {SceneManager.GetActiveScene().name}");
        RefreshReferences(force: true);
        ApplyUiState();
    }

    void HandlePartyStateChanged()
    {
        ApplyUiState();
    }

    void RefreshReferences(bool force)
    {
        if (!force && Time.unscaledTime < _nextRefreshUiRefsAt)
            return;

        _nextRefreshUiRefsAt = Time.unscaledTime + 1.2f;

        _invitationPanels.Clear();
        _preStartPanels.Clear();
        _waitThresholdSliders.Clear();
        _waitRemainingTexts.Clear();
        _acceptButtons.Clear();
        _declineButtons.Clear();
        _startButtons.Clear();
        _retryAcceptedText = null;

        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            string n = t.name;
            if (n == "Invitation panel")
                _invitationPanels.Add(t.gameObject);
            else if (n == "Panel_Pre-Start")
                _preStartPanels.Add(t.gameObject);
            else if (n == "Wait time threshold")
            {
                var slider = t.GetComponent<Slider>();
                if (slider != null) _waitThresholdSliders.Add(slider);
            }
            else if (n == "Wait time show remaining")
            {
                var tmp = t.GetComponent<TMP_Text>();
                if (tmp != null) _waitRemainingTexts.Add(tmp);
            }
            else if (n == "Accept")
            {
                var btn = t.GetComponent<Button>();
                if (btn != null) _acceptButtons.Add(btn);
            }
            else if (n == "Decline")
            {
                var btn = t.GetComponent<Button>();
                if (btn != null) _declineButtons.Add(btn);
            }
            else if (n == "Start")
            {
                var btn = t.GetComponent<Button>();
                if (btn != null) _startButtons.Add(btn);
            }
            else if (n == "Anount of players accept")
            {
                _retryAcceptedText = t.GetComponent<TMP_Text>();
            }
        }

        if (force && enableDebugLogs)
        {
            Dbg(
                $"RefreshReferences scene={SceneManager.GetActiveScene().name} " +
                $"invitationPanels={_invitationPanels.Count} preStartPanels={_preStartPanels.Count} " +
                $"acceptBtns={_acceptButtons.Count} declineBtns={_declineButtons.Count}");
        }

        foreach (var btn in _acceptButtons)
        {
            if (btn == null) continue;
            btn.onClick.RemoveListener(OnAcceptClicked);
            btn.onClick.AddListener(OnAcceptClicked);
        }
        foreach (var btn in _declineButtons)
        {
            if (btn == null) continue;
            btn.onClick.RemoveListener(OnDeclineClicked);
            btn.onClick.AddListener(OnDeclineClicked);
        }
    }

    void ApplyUiState()
    {
        bool inviteActive = DungeonPartyRuntime.InviteActive;
        bool isHost = DungeonPartyRuntime.IsLocalHost();
        bool showInvitePanel = DungeonPartyRuntime.LocalShouldSeeInvitationPanel;
        float remaining = DungeonPartyRuntime.InviteRemainingSeconds;
        float norm = inviteActive ? Mathf.Clamp01(remaining / 20f) : 0f;
        bool showWaitUi = inviteActive && (isHost || showInvitePanel);

        if (showInvitePanel != _lastShowInvitePanel)
        {
            Dbg(
                $"Invitation visibility → {showInvitePanel} | inviteActive={inviteActive} isHost={DungeonPartyRuntime.IsLocalHost()} " +
                $"responded={DungeonPartyRuntime.LocalRespondedCurrentInvite} panelsFound={_invitationPanels.Count} " +
                $"inviteId={DungeonPartyRuntime.InviteId} scene='{DungeonPartyRuntime.TargetSceneName}' " +
                $"accepted={DungeonPartyRuntime.AcceptedCount}/{DungeonPartyRuntime.RequiredAcceptCount}");
            _lastShowInvitePanel = showInvitePanel;
        }

        foreach (var go in _invitationPanels)
        {
            if (go == null) continue;
            bool shouldActive = showInvitePanel;
            if (shouldActive)
                EnsureHierarchyActiveForPanel(go);

            if (go.activeSelf != shouldActive)
            {
                go.SetActive(shouldActive);
                if (shouldActive)
                    Dbg($"Bật Invitation panel '{go.name}' activeInHierarchy={go.activeInHierarchy} path={GetPath(go.transform)}");
                else
                    Dbg($"Tắt Invitation panel '{go.name}'");
            }
        }

        if (showInvitePanel && _invitationPanels.Count == 0)
            Dbg("CẢNH BÁO: cần hiện Invitation panel nhưng không tìm thấy object tên 'Invitation panel' trong scene. candidates=" + BuildInvitationCandidates());

        foreach (var s in _waitThresholdSliders)
        {
            if (s == null) continue;
            if (s.gameObject.activeSelf != showWaitUi)
                s.gameObject.SetActive(showWaitUi);
            if (!s.gameObject.activeInHierarchy) continue;
            if (s.maxValue <= 1.01f)
                s.value = norm;
            else
                s.value = remaining;
        }

        foreach (var txt in _waitRemainingTexts)
        {
            if (txt == null) continue;
            if (txt.gameObject.activeSelf != showWaitUi)
                txt.gameObject.SetActive(showWaitUi);
            if (!txt.gameObject.activeInHierarchy) continue;
            txt.text = inviteActive ? $"{Mathf.CeilToInt(remaining)}s" : "0s";
        }

        foreach (var btn in _startButtons)
        {
            if (btn == null) continue;
            bool shouldShow = isHost && DungeonPartyRuntime.CanHostStart;
            if (btn.gameObject.activeSelf != shouldShow)
                btn.gameObject.SetActive(shouldShow);
        }

        if (_retryAcceptedText != null && _retryAcceptedText.gameObject.scene.IsValid())
        {
            int req = Mathf.Max(1, DungeonPartyRuntime.RetryRequired);
            _retryAcceptedText.text = $"{DungeonPartyRuntime.RetryVotes}/{req}";
        }
    }

    static void EnsureHierarchyActiveForPanel(GameObject panel)
    {
        if (panel == null)
            return;

        CursorUIPriority.BeginUiOverlay();

        Transform root = panel.transform;
        while (root != null)
        {
            if (root.name.StartsWith("Canvas") && !root.gameObject.activeSelf)
                root.gameObject.SetActive(true);
            root = root.parent;
        }
    }

    void OnAcceptClicked()
    {
        Dbg("Accept clicked");
        if (Character.LocalCharacter == null)
        {
            Dbg("Accept FAILED: Character.LocalCharacter == null");
            return;
        }
        Character.LocalCharacter.TryRespondDungeonInvite(true);
    }

    void OnDeclineClicked()
    {
        Dbg("Decline clicked");
        if (Character.LocalCharacter == null)
        {
            Dbg("Decline FAILED: Character.LocalCharacter == null");
            return;
        }
        Character.LocalCharacter.TryRespondDungeonInvite(false);
    }

    void Dbg(string msg)
    {
        if (!enableDebugLogs)
            return;
        Debug.Log($"[DungeonPartyUi] {msg}");
    }

    static string BuildInvitationCandidates()
    {
        var sb = new StringBuilder();
        int count = 0;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null)
                continue;
            string n = t.name;
            if (!n.ToLowerInvariant().Contains("invitation"))
                continue;
            if (count > 0) sb.Append(" | ");
            sb.Append(n).Append(" active=").Append(t.gameObject.activeSelf);
            count++;
            if (count >= 8)
                break;
        }
        if (count == 0)
            return "(none)";
        return sb.ToString();
    }

    static string GetPath(Transform t)
    {
        if (t == null) return "";
        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }
}
