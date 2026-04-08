using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn lên root Dungeon Canvas (DamLay / Samac / Demon). Tự gán <see cref="SceneTeleportButton.dungeonLobbyCoordinator"/>
/// cho các nút độ khó; tắt <see cref="SceneTeleportButton"/> trên panel "Number of players" và gắn chọn 1–4 người.
/// </summary>
public class DungeonPortalLobbyCoordinator : MonoBehaviour
{
    public static DungeonPortalLobbyCoordinator Current { get; private set; }

    [Header("Panels (auto-find theo tên nếu để trống)")]
    [Tooltip("Object tên \"Button\" chứa Easy/Normal/Hard — ẩn sau khi chọn độ khó.")]
    [SerializeField] GameObject difficultyButtonsRoot;
    [SerializeField] GameObject numberOfPlayersPanel;
    [SerializeField] GameObject preparationPanel;

    [Header("TMP (auto-find trong Preparation panel)")]
    [SerializeField] TMP_Text countdownText;
    [SerializeField] TMP_Text joinedExpectedText;

    [Header("Solo / fallback")]
    [SerializeField] float localFinalCountdownSeconds = 3f;

    string _pendingSceneName;
    Coroutine _localCountdownRoutine;
    bool _playerCountButtonsWired;

    void Awake()
    {
        ResolvePanelRefs();
        RegisterSceneTeleportButtons();
        WirePlayerCountButtons();
    }

    void OnEnable()
    {
        Current = this;
    }

    void OnDisable()
    {
        if (Current == this)
            Current = null;
    }

    void ResolvePanelRefs()
    {
        if (difficultyButtonsRoot == null)
        {
            var t = FindDirectChildNamed(transform, "Button");
            if (t != null) difficultyButtonsRoot = t.gameObject;
        }
        if (numberOfPlayersPanel == null)
        {
            var t = FindChildByNamePrefix(transform, "Number of players");
            if (t != null) numberOfPlayersPanel = t.gameObject;
        }
        if (preparationPanel == null)
        {
            var t = FindChildByNamePrefix(transform, "Preparation panel");
            if (t != null) preparationPanel = t.gameObject;
        }
        if (preparationPanel != null)
        {
            if (countdownText == null)
            {
                var t = FindChildRecursive(preparationPanel.transform, "Countdown");
                if (t != null) countdownText = t.GetComponent<TMP_Text>();
            }
            if (joinedExpectedText == null)
            {
                var t = FindChildRecursive(preparationPanel.transform, "Joined/Expected");
                if (t != null) joinedExpectedText = t.GetComponent<TMP_Text>();
            }
        }
    }

    static Transform FindChildRecursive(Transform parent, string exactName)
    {
        if (parent.name == exactName) return parent;
        foreach (Transform c in parent)
        {
            var r = FindChildRecursive(c, exactName);
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>Khớp "Preparation panel", "Preparation panel (2)", ...</summary>
    static Transform FindChildByNamePrefix(Transform root, string namePrefix)
    {
        foreach (var tr in root.GetComponentsInChildren<Transform>(true))
        {
            if (tr.name.StartsWith(namePrefix))
                return tr;
        }
        return null;
    }

    /// <summary>Chỉ con trực tiếp (tránh trùng tên Button lồng sâu).</summary>
    static Transform FindDirectChildNamed(Transform parent, string childName)
    {
        foreach (Transform c in parent)
        {
            if (c.name == childName)
                return c;
        }
        return null;
    }

    /// <summary>Gọi khi mở cổng (ScenePortal) — reset về bước chọn độ khó.</summary>
    public void ResetDungeonPortalPanelsForEntry()
    {
        if (_localCountdownRoutine != null)
        {
            StopCoroutine(_localCountdownRoutine);
            _localCountdownRoutine = null;
        }
        if (difficultyButtonsRoot != null)
            difficultyButtonsRoot.SetActive(true);
        if (numberOfPlayersPanel != null)
            numberOfPlayersPanel.SetActive(false);
        if (preparationPanel != null)
            preparationPanel.SetActive(false);
        if (countdownText != null)
            countdownText.text = string.Empty;
    }

    void RegisterSceneTeleportButtons()
    {
        foreach (var st in GetComponentsInChildren<SceneTeleportButton>(true))
        {
            if (IsUnderNamedPanel(st.transform, "Number of players"))
            {
                st.enabled = false;
                continue;
            }
            st.dungeonLobbyCoordinator = this;
        }
    }

    static bool IsUnderNamedPanel(Transform t, string panelName)
    {
        while (t != null)
        {
            if (t.name == panelName || t.name.StartsWith(panelName + " ("))
                return true;
            t = t.parent;
        }
        return false;
    }

    void WirePlayerCountButtons()
    {
        if (_playerCountButtonsWired) return;
        if (numberOfPlayersPanel == null) return;
        int added = 0;
        foreach (var btn in numberOfPlayersPanel.GetComponentsInChildren<Button>(true))
        {
            int n = ParsePlayerCountFromButton(btn);
            if (n <= 0) continue;
            int captured = n;
            btn.onClick.AddListener(() => OnPlayerCountClicked(captured));
            added++;
        }
        if (added > 0)
            _playerCountButtonsWired = true;
        Debug.Log($"[DungeonPortalLobby] Wired player-count buttons: {added} on {name}");
    }

    static int ParsePlayerCount(string objectName)
    {
        if (objectName.Contains("1 player")) return 1;
        if (objectName.Contains("2 player")) return 2;
        if (objectName.Contains("3 player")) return 3;
        if (objectName.Contains("4 player")) return 4;
        return 0;
    }

    /// <summary>Một số prefab đặt Button trên child (tên "Button") — lấy số người từ parent "N player(s)".</summary>
    static int ParsePlayerCountFromButton(Button btn)
    {
        if (btn == null) return 0;
        Transform t = btn.transform;
        for (int d = 0; d < 8 && t != null; d++)
        {
            int n = ParsePlayerCount(t.gameObject.name);
            if (n > 0) return n;
            t = t.parent;
        }
        return 0;
    }

    /// <summary>Gọi từ SceneTeleportButton khi chọn Easy/Normal/Hard.</summary>
    public void OnDifficultySelected(SceneTeleportButton source)
    {
        if (source == null) return;

        Current = this;
        _pendingSceneName = source.targetSceneName;
        DungeonConfig.SelectedDifficulty = source.dungeonDifficulty;
        DungeonConfig.SelectedMapType = source.mapType;
        Debug.Log($"[DungeonPortalLobby] Difficulty selected scene={_pendingSceneName}");

        if (difficultyButtonsRoot != null)
            difficultyButtonsRoot.SetActive(false);
        if (numberOfPlayersPanel != null)
            numberOfPlayersPanel.SetActive(true);

        ResolvePanelRefs();
        WirePlayerCountButtons();
    }

    void OnPlayerCountClicked(int maxPlayers)
    {
        Debug.Log($"[DungeonPortalLobby] Player count clicked: {maxPlayers} (solo / Fusion lobby sau này)");

        if (numberOfPlayersPanel != null)
            numberOfPlayersPanel.SetActive(false);
        if (preparationPanel != null)
            preparationPanel.SetActive(true);

        SetJoinedExpected(1, maxPlayers);

        if (_localCountdownRoutine != null)
            StopCoroutine(_localCountdownRoutine);
        _localCountdownRoutine = StartCoroutine(LocalSoloCountdownRoutine(maxPlayers));
    }

    IEnumerator LocalSoloCountdownRoutine(int maxPlayers)
    {
        SetJoinedExpected(1, maxPlayers);
        yield return StartCoroutine(RunFinalCountdownSeconds(localFinalCountdownSeconds));
        ExecuteTeleport();
    }

    /// <summary>Server → client: cập nhật đã vào / dự kiến.</summary>
    public void SetJoinedExpected(int joined, int expected)
    {
        if (joinedExpectedText != null)
            joinedExpectedText.text = $"{joined}/{expected}";
    }

    /// <summary>Server → client: đếm ngược rồi load scene.</summary>
    public void StartNetworkFinalCountdown(string sceneName, float seconds)
    {
        _pendingSceneName = sceneName;
        if (_localCountdownRoutine != null)
            StopCoroutine(_localCountdownRoutine);
        _localCountdownRoutine = StartCoroutine(NetworkFinalRoutine(sceneName, seconds));
    }

    IEnumerator NetworkFinalRoutine(string sceneName, float seconds)
    {
        yield return StartCoroutine(RunFinalCountdownSeconds(seconds));
        _pendingSceneName = sceneName;
        ExecuteTeleport();
    }

    IEnumerator RunFinalCountdownSeconds(float seconds)
    {
        float t = seconds;
        while (t > 0f)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(t).ToString();
            t -= Time.deltaTime;
            yield return null;
        }
        if (countdownText != null)
            countdownText.text = "0";
    }

    public void OnLobbyCancelledFromNetwork()
    {
        if (preparationPanel != null)
            preparationPanel.SetActive(false);
        if (countdownText != null)
            countdownText.text = string.Empty;
    }

    void ExecuteTeleport()
    {
        if (string.IsNullOrEmpty(_pendingSceneName))
            return;
        LoadDungeonSceneStatic(_pendingSceneName);
    }

    public static void ApplyJoinedExpectedToAll(int joined, int expected)
    {
        foreach (var c in Object.FindObjectsByType<DungeonPortalLobbyCoordinator>(FindObjectsSortMode.None))
        {
            if (c != null)
                c.SetJoinedExpected(joined, expected);
        }
    }

    public static void ApplyCountdownTextToAllPreparation(string text)
    {
        foreach (var c in Object.FindObjectsByType<DungeonPortalLobbyCoordinator>(FindObjectsSortMode.None))
        {
            if (c != null && c.countdownText != null)
                c.countdownText.text = text;
        }
    }

    public static void ApplyLobbyCancelledToAll()
    {
        foreach (var c in Object.FindObjectsByType<DungeonPortalLobbyCoordinator>(FindObjectsSortMode.None))
        {
            if (c != null)
                c.OnLobbyCancelledFromNetwork();
        }
    }

    public static void LoadDungeonSceneStatic(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[DungeonPortalLobby] Scene '{sceneName}' không load được.");
            return;
        }

        Time.timeScale = 1f;
        CursorUIPriority.EndAllUiOverlays();

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.GoToScene(sceneName, "Đang chuyển vùng...");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
