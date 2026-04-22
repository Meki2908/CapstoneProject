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

    [Header("Cutscene (mở rộng)")]
    [Tooltip("Ẩn toàn bộ UI Player (ví dụ: Canvas_Menu / AbilityIcons / SkillBar / UI_Skills...).")]
    [SerializeField] private bool hideAllPlayerUI = true;

    [Tooltip("Khóa toàn bộ Enemy trong scene (trừ boss hiện tại) để không đánh/chạy trong cutscene.")]
    [SerializeField] private bool lockAllEnemiesInScene = true;

    [Tooltip("Khi lockAllEnemiesInScene bật: không khóa EnemyScript thuộc boss hiện tại.")]
    [SerializeField] private bool excludeBossEnemyFromGlobalLock = true;

    [Header("Portal clip (URP PortalPlaneClipLit)")]
    [Tooltip("Khi timeline intro boss kết thúc: tắt clip trên mesh + tắt PortalPlaneClipBinder (không còn MPB mỗi frame).")]
    [SerializeField] private bool shutdownPortalClipWhenCutsceneEnds = true;
    [Tooltip("Để trống = tự tìm PortalPlaneClipBinder trong children (vd. Portal red).")]
    [SerializeField] private PortalPlaneClipBinder[] portalClipBinders;

    private readonly List<Behaviour> _behavioursToRestore = new List<Behaviour>();
    private readonly List<bool> _behaviourWasEnabled = new List<bool>();
    private readonly List<Collider> _collidersToRestore = new List<Collider>();
    private readonly List<bool> _colliderWasEnabled = new List<bool>();
    private readonly List<GameObject> _playerUiToRestore = new List<GameObject>();
    private readonly List<bool> _playerUiWasActive = new List<bool>();

    // Thêm để quản lý trạng thái Rigidbody của quái và Input của Player
    private readonly List<Rigidbody> _rigidbodiesToRestore = new List<Rigidbody>();
    private readonly List<bool> _rbWasKinematic = new List<bool>();
    private UnityEngine.InputSystem.PlayerInput _playerInputToRestore;

    private EnemyScript _enemyScript;
    private NavMeshAgent _navMeshAgent;
    private bool _cutsceneActive;
    private bool _hudWasActive = true;

    private void Awake()
    {
        CacheDirector();
        CacheEnemyComponents();
    }

    private void OnEnable()
    {
        if (lockOnAwake && !_cutsceneActive)
            StartCoroutine(DelayedBeginCutscene());
    }

    private System.Collections.IEnumerator DelayedBeginCutscene()
    {
        // Chờ 1 frame để đảm bảo DungeonWaveManager đã spawn toàn bộ quái con
        yield return null; 
        if (!_cutsceneActive)
            BeginCutsceneInternal();
    }

    private void OnDisable()
    {
        if (_cutsceneActive)
            EndCutsceneInternal();
    }

    private void OnDestroy()
    {
        if (_cutsceneActive)
            EndCutsceneInternal();

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
        if (hideAllPlayerUI)
            HideAllPlayerUI();
        else
            HideHud();

        LockGameplay();
    }

    private void EndCutsceneInternal()
    {
        if (!_cutsceneActive) return;
        _cutsceneActive = false;

        if (hideAllPlayerUI)
            RestoreAllPlayerUI();
        else
            ShowHud();

        UnlockGameplay();

        if (shutdownPortalClipWhenCutsceneEnds)
            ShutdownPortalClipBinders();

        // Fix: Ép ẩn con trỏ chuột và kích hoạt lại camera gameplay sau khi Timeline Boss kết thúc
        MovementSystem.CameraCursor.ApplyGameplayCursorAfterUiClosed();
        CursorUIPriority.EndAllUiOverlays();
        if (MouseLockManager.Instance != null)
        {
            MouseLockManager.Instance.ClearAllLocksAndForceGameplay();
        }
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

    private void RememberAndSetActive(GameObject go, bool active)
    {
        if (go == null) return;
        if (_playerUiToRestore.Contains(go)) return; // avoid duplicates

        _playerUiToRestore.Add(go);
        _playerUiWasActive.Add(go.activeSelf);
        go.SetActive(active);
    }

    private void HideAllPlayerUI()
    {
        _playerUiToRestore.Clear();
        _playerUiWasActive.Clear();

        // Hide toàn cục theo tag HUD
        var hudTagged = GameObject.FindGameObjectsWithTag("HUD");
        if (hudTagged != null && hudTagged.Length > 0)
        {
            foreach (var go in hudTagged)
            {
                if (go == null) continue;
                if (HudSceneTagUtilities.IsDungeonHudUiRoot(go))
                    continue;
                RememberAndSetActive(go, false);
            }
            return;
        }

        // Fallback: nếu project chưa gắn tag HUD đầy đủ, dùng tên để ẩn
        string[] sceneFallbackUiNames = {
            "AbilityIcons", "SkillBar", "UI_Skills",
            "Canvas_Menu",
            "UI_HP+Invetory_1.0", "UI_HP+Invetory_1", "UI_HP+Invetory",
            "UI_HP+Inventory_1.0", "UI_HP+Inventory"
        };
        foreach (string n in sceneFallbackUiNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) RememberAndSetActive(go, false);
        }
    }

    private void RestoreAllPlayerUI()
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

    private void LockGameplay()
    {
        _behavioursToRestore.Clear();
        _behaviourWasEnabled.Clear();
        _collidersToRestore.Clear();
        _colliderWasEnabled.Clear();
        _rigidbodiesToRestore.Clear();
        _rbWasKinematic.Clear();

        // KHÓA PLAYER
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerInputToRestore = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (_playerInputToRestore != null)
            {
                _playerInputToRestore.enabled = false; // Cắt đứt hoàn toàn phím bấm
            }
            
            // Dừng đà trượt của Player
            var charController = player.GetComponent<CharacterController>();
            if (charController != null) charController.Move(Vector3.zero);
        }

        Transform bossScope = _enemyScript != null ? _enemyScript.transform : transform;
        DisableEnemyScope(bossScope);

        if (!lockAllEnemiesInScene)
            return;

        var allEnemies = Object.FindObjectsByType<EnemyScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var e in allEnemies)
        {
            if (e == null) continue;
            if (excludeBossEnemyFromGlobalLock && e == _enemyScript) continue;
            DisableEnemyScope(e.transform);

            // Đóng băng hoạt ảnh quái con
            var anims = e.GetComponentsInChildren<Animator>(true);
            foreach (var a in anims)
            {
                if (a == null) continue;
                _behavioursToRestore.Add(a);
                _behaviourWasEnabled.Add(a.enabled);
                a.enabled = false; // Freeze the animation on the current frame
            }
        }
    }

    private void DisableEnemyScope(Transform scope)
    {
        if (scope == null) return;

        // Ép dừng NavMeshAgent ngay lập tức trước khi tắt
        var agent = scope.GetComponentInChildren<NavMeshAgent>(true);
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // ĐÓNG BĂNG VẬT LÝ TUYỆT ĐỐI
        var rb = scope.GetComponentInChildren<Rigidbody>(true);
        if (rb != null)
        {
            _rigidbodiesToRestore.Add(rb);
            _rbWasKinematic.Add(rb.isKinematic);
            rb.linearVelocity = Vector3.zero; // Xóa đà trượt
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;      // Biến thành cục đá
        }

        TryDisable<EnemyScript>(scope);
        TryDisable<EnemyAttack>(scope);
        TryDisable<EnemyState>(scope);
        TryDisable<EnemyDamage>(scope);
        TryDisable<TakeDamageTest>(scope);
        TryDisable<BossMultiSkill>(scope);
        TryDisable<EnemyStuckDetection>(scope);
        TryDisable<NavMeshAgent>(scope);

        if (!disableHitColliderDuringCutscene) return;

        foreach (var c in scope.GetComponentsInChildren<Collider>(true))
        {
            if (c == null || !c.enabled || c.isTrigger) continue;
            _collidersToRestore.Add(c);
            _colliderWasEnabled.Add(true);
            c.enabled = false;
        }
    }

    private void TryDisable<T>(Transform scope) where T : Behaviour
    {
        var comps = scope.GetComponentsInChildren<T>(true);
        foreach (var b in comps)
        {
            if (b == null) continue;
            _behavioursToRestore.Add(b);
            _behaviourWasEnabled.Add(b.enabled);
            b.enabled = false;
        }
    }

    private void UnlockGameplay()
    {
        for (int i = 0; i < _behavioursToRestore.Count; i++)
        {
            var b = _behavioursToRestore[i];
            if (b == null) continue;

            bool wasEnabled = _behaviourWasEnabled.Count > i && _behaviourWasEnabled[i];
            if (b is EnemyScript es)
            {
                if (wasEnabled)
                {
                    EnemyScript.suppressSpawnVfx = true;
                    es.enabled = true;
                    EnemyScript.suppressSpawnVfx = false;
                }
                else
                {
                    es.enabled = false;
                }
            }
            else
            {
                b.enabled = wasEnabled;
            }
        }

        _behavioursToRestore.Clear();
        _behaviourWasEnabled.Clear();

        for (int i = 0; i < _collidersToRestore.Count; i++)
        {
            if (_collidersToRestore[i] != null)
                _collidersToRestore[i].enabled = _colliderWasEnabled[i];
        }

        _collidersToRestore.Clear();
        _colliderWasEnabled.Clear();

        // Mở khóa Vật lý cho quái
        for (int i = 0; i < _rigidbodiesToRestore.Count; i++)
        {
            if (_rigidbodiesToRestore[i] != null)
                _rigidbodiesToRestore[i].isKinematic = _rbWasKinematic[i];
        }
        _rigidbodiesToRestore.Clear();
        _rbWasKinematic.Clear();

        // Mở khóa Player
        if (_playerInputToRestore != null)
        {
            _playerInputToRestore.enabled = true;
            _playerInputToRestore = null;
        }

        // FIX CHUỘT TUYỆT ĐỐI (Dùng lệnh lõi của Windows/Unity)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
