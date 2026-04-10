using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên object pre-enter dungeon (vd. "Timeline holder" cùng <see cref="PlayableDirector"/>).
/// Ẩn toàn bộ HUD player khi timeline bắt đầu phát, bật lại khi director dừng — cùng ý tưởng <see cref="BossCutsceneController"/>.
/// Dùng khi HUD là DontDestroyOnLoad / Control track khó bind đúng instance runtime.
/// </summary>
[DefaultExecutionOrder(100)]
public class PreEnterDungeonCutsceneController : MonoBehaviour
{
    [Header("Timeline")]
    [Tooltip("Để trống = tự tìm PlayableDirector trên cùng GameObject hoặc trong children.")]
    [SerializeField] private PlayableDirector director;

    [Header("HUD")]
    [Tooltip("Ẩn theo tag HUD trước, sau đó fallback tên Canvas_Menu / UI_HP+...")]
    [SerializeField] private bool hideAllPlayerUi = true;

    [Tooltip("Bật nếu chỉ muốn ẩn một root (kéo tay). Nếu hideAllPlayerUi = true thì field này bị bỏ qua.")]
    [SerializeField] private GameObject singleHudRoot;

    [SerializeField] private bool useCanvasGroupInsteadOfSetActive;
    [SerializeField] private CanvasGroup singleHudCanvasGroup;

    [SerializeField] private bool unsubscribeOnDestroy = true;

    private readonly List<GameObject> _playerUiToRestore = new List<GameObject>();
    private readonly List<bool> _playerUiWasActive = new List<bool>();

    private bool _cutsceneActive;
    private bool _singleHudWasActive = true;

    private void Awake()
    {
        CacheDirector();
        if (director == null) return;

        director.played += OnDirectorPlayed;
        director.stopped += OnDirectorStopped;
    }

    private void Start()
    {
        // Director có thể đã Playing trước khi script enable (thứ tự Awake).
        if (director != null && director.state == PlayState.Playing && !_cutsceneActive)
            BeginCutsceneInternal();
    }

    private void OnDestroy()
    {
        if (_cutsceneActive)
            EndCutsceneInternal();

        if (unsubscribeOnDestroy && director != null)
        {
            director.played -= OnDirectorPlayed;
            director.stopped -= OnDirectorStopped;
        }
    }

    private void CacheDirector()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>() ?? GetComponentInChildren<PlayableDirector>(true);
    }

    private void OnDirectorPlayed(PlayableDirector d)
    {
        BeginCutsceneInternal();
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        EndCutsceneInternal();
    }

    private void BeginCutsceneInternal()
    {
        if (_cutsceneActive) return;
        _cutsceneActive = true;

        bool skipHide = DungeonPreEnterSession.ConsumeSkipHudHideNextEntry();
        if (skipHide)
            return;

        if (hideAllPlayerUi)
            HideAllPlayerUi();
        else
            HideSingleHud();
    }

    private void EndCutsceneInternal()
    {
        if (!_cutsceneActive) return;
        _cutsceneActive = false;

        if (hideAllPlayerUi)
            RestoreAllPlayerUi();
        else
            ShowSingleHud();

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.HideLoadingPanelIfAny();
    }

    private void HideSingleHud()
    {
        if (singleHudRoot == null) return;

        if (useCanvasGroupInsteadOfSetActive && singleHudCanvasGroup != null)
        {
            singleHudCanvasGroup.alpha = 0f;
            singleHudCanvasGroup.interactable = false;
            singleHudCanvasGroup.blocksRaycasts = false;
            return;
        }

        _singleHudWasActive = singleHudRoot.activeSelf;
        singleHudRoot.SetActive(false);
    }

    private void ShowSingleHud()
    {
        if (singleHudRoot == null) return;

        if (useCanvasGroupInsteadOfSetActive && singleHudCanvasGroup != null)
        {
            singleHudCanvasGroup.alpha = 1f;
            singleHudCanvasGroup.interactable = true;
            singleHudCanvasGroup.blocksRaycasts = true;
            return;
        }

        singleHudRoot.SetActive(_singleHudWasActive);
    }

    private void RememberAndSetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (_playerUiToRestore.Contains(go)) return;

        _playerUiToRestore.Add(go);
        _playerUiWasActive.Add(go.activeSelf);
        go.SetActive(active);
    }

    private static readonly string[] FallbackHudRootNames =
    {
        "AbilityIcons", "SkillBar", "UI_Skills",
        "Canvas_Menu",
        "UI_HP+Invetory_1.0", "UI_HP+Invetory_1", "UI_HP+Invetory",
        "UI_HP+Inventory_1.0", "UI_HP+Inventory"
    };

    /// <summary>
    /// Khớp prefab gốc, bản Instantiate <c>(Clone)</c>, hoặc tên biến thể UI_HP+Inventory (typo Invetory).
    /// </summary>
    private static bool MatchesFallbackHudRootName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        foreach (var n in FallbackHudRootNames)
        {
            if (objectName == n) return true;
            if (objectName.StartsWith(n + " (", System.StringComparison.Ordinal)) return true;
        }

        if (objectName.StartsWith("UI_HP", System.StringComparison.Ordinal) &&
            (objectName.IndexOf("Invetory", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             objectName.IndexOf("Inventory", System.StringComparison.OrdinalIgnoreCase) >= 0))
            return true;

        return false;
    }

    private void HideAllPlayerUi()
    {
        _playerUiToRestore.Clear();
        _playerUiWasActive.Clear();

        // Một pass: tag HUD + inactive (FindGameObjectsWithTag chỉ thấy active → DDOL HUD inactive bị sót).
        var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGos)
        {
            if (go == null || !go.scene.IsValid() || !go.scene.isLoaded) continue;

            bool isHudTag = false;
            try
            {
                isHudTag = go.CompareTag("HUD");
            }
            catch (UnityException)
            {
                continue;
            }

            if (isHudTag)
            {
                if (!HudSceneTagUtilities.IsDungeonHudUiRoot(go))
                    RememberAndSetActive(go, false);

                continue;
            }

            if (MatchesFallbackHudRootName(go.name))
                RememberAndSetActive(go, false);
        }
    }

    private void RestoreAllPlayerUi()
    {
        for (int i = 0; i < _playerUiToRestore.Count; i++)
        {
            var go = _playerUiToRestore[i];
            if (go != null && i < _playerUiWasActive.Count)
                go.SetActive(_playerUiWasActive[i]);
        }

        _playerUiToRestore.Clear();
        _playerUiWasActive.Clear();
    }
}
