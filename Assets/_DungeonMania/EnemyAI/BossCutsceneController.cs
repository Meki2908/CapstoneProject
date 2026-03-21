using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

/// <summary>
/// Khóa combat + ẩn HUD khi boss intro (Timeline), mở lại khi PlayableDirector dừng.
/// Gắn lên root prefab Demon (cùng cấp với child "Demon Timeline" có PlayableDirector).
/// Có thể gọi từ Timeline Signal: CutsceneStarted / CutsceneEnded.
/// </summary>
[DefaultExecutionOrder(100)]
public class BossCutsceneController : MonoBehaviour
{
    [Header("HUD — kéo root UI_HP+Inventory (Player)")]
    [Tooltip("Để trống sẽ thử tìm dưới object có tag Player (tên chứa UI_HP và Invetory).")]
    [SerializeField] private GameObject playerHudRoot;

    [SerializeField] private bool useCanvasGroupInsteadOfSetActive;
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Header("Timeline")]
    [Tooltip("Để trống = tự tìm PlayableDirector trong children (vd. Demon Timeline).")]
    [SerializeField] private PlayableDirector director;

    [Header("Tùy chọn")]
    [SerializeField] private bool lockOnAwake = true;
    [SerializeField] private bool disableHitColliderDuringCutscene = true;
    [SerializeField] private bool unsubscribeOnDestroy = true;

    [Header("Portal clip (URP PortalPlaneClipLit)")]
    [Tooltip("Khi timeline intro boss kết thúc: tắt clip trên mesh + tắt PortalPlaneClipBinder (không còn MPB mỗi frame).")]
    [SerializeField] private bool shutdownPortalClipWhenCutsceneEnds = true;
    [Tooltip("Để trống = tự tìm PortalPlaneClipBinder trong children (vd. Portal red).")]
    [SerializeField] private PortalPlaneClipBinder[] portalClipBinders;

    private readonly List<Behaviour> _behavioursToRestore = new List<Behaviour>();
    private readonly List<bool> _behaviourWasEnabled = new List<bool>();
    private readonly List<Collider> _collidersToRestore = new List<Collider>();
    private readonly List<bool> _colliderWasEnabled = new List<bool>();

    private EnemyScript _enemyScript;
    private NavMeshAgent _navMeshAgent;
    private bool _cutsceneActive;
    private bool _hudWasActive = true;

    private void Awake()
    {
        CacheDirector();
        CacheEnemyComponents();

        if (lockOnAwake)
            BeginCutsceneInternal();
    }

    private void OnDestroy()
    {
        if (unsubscribeOnDestroy && director != null)
            director.stopped -= OnDirectorStopped;
    }

    private void CacheDirector()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>() ?? GetComponentInChildren<PlayableDirector>(true);

        if (director != null)
            director.stopped += OnDirectorStopped;
    }

    private void CacheEnemyComponents()
    {
        _enemyScript = GetComponentInChildren<EnemyScript>(true);
        _navMeshAgent = GetComponentInChildren<NavMeshAgent>(true);
    }

    private void OnDirectorStopped(PlayableDirector d)
    {
        EndCutsceneInternal();
    }

    /// <summary>Gọi từ Timeline Signal đầu cutscene (nếu không dùng lockOnAwake).</summary>
    public void CutsceneStarted()
    {
        BeginCutsceneInternal();
    }

    /// <summary>Gọi từ Timeline Signal cuối cutscene (thường không cần nếu đã dùng stopped).</summary>
    public void CutsceneEnded()
    {
        EndCutsceneInternal();
    }

    private void BeginCutsceneInternal()
    {
        if (_cutsceneActive) return;
        _cutsceneActive = true;

        ResolveHudReference();
        HideHud();

        LockGameplay();
    }

    private void EndCutsceneInternal()
    {
        if (!_cutsceneActive) return;
        _cutsceneActive = false;

        ShowHud();
        UnlockGameplay();

        if (shutdownPortalClipWhenCutsceneEnds)
            ShutdownPortalClipBinders();
    }

    private void ShutdownPortalClipBinders()
    {
        PortalPlaneClipBinder[] binders = portalClipBinders;
        if (binders == null || binders.Length == 0)
            binders = GetComponentsInChildren<PortalPlaneClipBinder>(true);

        if (binders == null) return;

        foreach (var b in binders)
        {
            if (b != null)
                b.ShutdownPortalEffect(disableComponent: true);
        }
    }

    private void ResolveHudReference()
    {
        if (playerHudRoot != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        foreach (var t in player.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n.IndexOf("UI_HP", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                n.IndexOf("Invetory", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                playerHudRoot = t.gameObject;
                if (hudCanvasGroup == null)
                    hudCanvasGroup = playerHudRoot.GetComponent<CanvasGroup>();
                return;
            }
        }
    }

    private void HideHud()
    {
        if (playerHudRoot == null) return;

        if (useCanvasGroupInsteadOfSetActive && hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 0f;
            hudCanvasGroup.interactable = false;
            hudCanvasGroup.blocksRaycasts = false;
            return;
        }

        _hudWasActive = playerHudRoot.activeSelf;
        playerHudRoot.SetActive(false);
    }

    private void ShowHud()
    {
        if (playerHudRoot == null) return;

        if (useCanvasGroupInsteadOfSetActive && hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = 1f;
            hudCanvasGroup.interactable = true;
            hudCanvasGroup.blocksRaycasts = true;
            return;
        }

        playerHudRoot.SetActive(_hudWasActive);
    }

    private void LockGameplay()
    {
        _behavioursToRestore.Clear();
        _behaviourWasEnabled.Clear();
        _collidersToRestore.Clear();
        _colliderWasEnabled.Clear();

        Transform scope = _enemyScript != null ? _enemyScript.transform : transform;

        TryDisable<EnemyScript>(scope);
        TryDisable<EnemyAttack>(scope);
        TryDisable<EnemyState>(scope);
        TryDisable<EnemyDamage>(scope);
        TryDisable<TakeDamageTest>(scope);
        TryDisable<BossMultiSkill>(scope);
        TryDisable<EnemyStuckDetection>(scope);

        if (_navMeshAgent == null)
            _navMeshAgent = scope.GetComponent<NavMeshAgent>();
        if (_navMeshAgent != null && _navMeshAgent.enabled)
        {
            _navMeshAgent.enabled = false;
        }

        if (disableHitColliderDuringCutscene)
        {
            foreach (var c in scope.GetComponentsInChildren<Collider>(true))
            {
                if (c == null || !c.enabled || c.isTrigger) continue;
                _collidersToRestore.Add(c);
                _colliderWasEnabled.Add(true);
                c.enabled = false;
            }
        }
    }

    private void TryDisable<T>(Transform scope) where T : Behaviour
    {
        var comps = scope.GetComponentsInChildren<T>(true);
        foreach (var b in comps)
        {
            if (b == null || !b.enabled) continue;
            _behavioursToRestore.Add(b);
            _behaviourWasEnabled.Add(true);
            b.enabled = false;
        }
    }

    private void UnlockGameplay()
    {
        for (int i = 0; i < _behavioursToRestore.Count; i++)
        {
            var b = _behavioursToRestore[i];
            if (b == null) continue;

            if (b is EnemyScript es)
            {
                EnemyScript.suppressSpawnVfx = true;
                es.enabled = true;
                EnemyScript.suppressSpawnVfx = false;
            }
            else
            {
                b.enabled = true;
            }
        }

        _behavioursToRestore.Clear();
        _behaviourWasEnabled.Clear();

        if (_navMeshAgent != null)
            _navMeshAgent.enabled = true;

        for (int i = 0; i < _collidersToRestore.Count; i++)
        {
            if (_collidersToRestore[i] != null)
                _collidersToRestore[i].enabled = _colliderWasEnabled[i];
        }

        _collidersToRestore.Clear();
        _colliderWasEnabled.Clear();
    }
}
