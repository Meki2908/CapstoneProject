using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

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

    private void HideAllPlayerUi()
    {
        _playerUiToRestore.Clear();
        _playerUiWasActive.Clear();

        try
        {
            var hudTagged = GameObject.FindGameObjectsWithTag("HUD");
            if (hudTagged != null && hudTagged.Length > 0)
            {
                foreach (var go in hudTagged)
                {
                    if (go == null) continue;
                    RememberAndSetActive(go, false);
                }

                return;
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning("[PreEnterDungeonCutscene] Tag \"HUD\" chưa có trong Tag Manager — dùng fallback tên.");
        }

        string[] fallbackNames =
        {
            "AbilityIcons", "SkillBar", "UI_Skills",
            "Canvas_Menu",
            "UI_HP+Invetory_1.0", "UI_HP+Invetory_1", "UI_HP+Invetory",
            "UI_HP+Inventory_1.0", "UI_HP+Inventory"
        };

        foreach (string n in fallbackNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
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
