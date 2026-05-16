using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

/// <summary>
/// Portal chuyển scene / mở UI chọn dungeon khi player bước vào trigger.
/// </summary>
public class ScenePortal : MonoBehaviour
{
    static string s_wiredSceneName;

    [Header("=== Debug ===")]
    [Tooltip("Bật log chi tiết trong Console (lọc: ScenePortal).")]
    public bool enableDebugLogs = true;

    [Header("=== UI REFERENCES ===")]
    [Tooltip("Kéo Canvas chọn Scene vào đây (ưu tiên).")]
    public GameObject portalCanvas;

    [Tooltip("Tên GameObject canvas dungeon — dùng khi portalCanvas chưa gán.")]
    public string fallbackCanvasName;

    [Header("=== SETTINGS ===")]
    public string playerTag = "Player";
    public float teleportDelay = 0.5f;

    [Header("=== EFFECTS ===")]
    public ParticleSystem portalEffect;

    bool _playerInRange;
    bool _portalUiOpen;
    readonly HashSet<int> _localColliderIdsInside = new HashSet<int>();
    int _localCharacterInstanceId;

    static ScenePortal s_activePortal;

    void Awake()
    {
        string sceneName = gameObject.scene.name;
        if (s_wiredSceneName != sceneName)
        {
            WireMissingDungeonGates();
            s_wiredSceneName = sceneName;
        }

        InferFallbackCanvasNameFromHierarchy();
        EnsureCanvasResolved();

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;

        Dbg(
            $"Awake | gatePath={GetHierarchyPath()} | collider={(col != null ? col.GetType().Name : "NONE")} " +
            $"trigger={(col != null && col.isTrigger)} enabled={(col != null && col.enabled)} " +
            $"fallbackCanvas='{fallbackCanvasName}' portalCanvas={(portalCanvas != null ? portalCanvas.name : "NULL")} " +
            $"canvasActive={(portalCanvas != null && portalCanvas.activeSelf)}");
    }

    void Start()
    {
        if (portalCanvas == null)
        {
            EnsureCanvasResolved();
            if (portalCanvas == null && enableDebugLogs)
                LogAllDungeonCanvasesInScene("Start: vẫn chưa resolve được canvas");
        }

        if (portalCanvas != null)
            portalCanvas.SetActive(false);

        if (portalEffect != null)
            portalEffect.Play();

        Dbg($"Start | portalCanvas={(portalCanvas != null ? portalCanvas.name : "NULL")} activeAfterHide={(portalCanvas != null && portalCanvas.activeSelf)}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!TryValidatePlayerColliderForEnter(other, out Character character, out string rejectReason))
        {
            Dbg($"OnTriggerEnter REJECTED | reason={rejectReason} other='{other.name}'");
            return;
        }

        int colliderId = other.GetInstanceID();
        if (!_localColliderIdsInside.Add(colliderId))
        {
            Dbg($"OnTriggerEnter DUPLICATE | collider='{other.name}' id={colliderId} tracked={_localColliderIdsInside.Count}");
            return;
        }

        if (_localCharacterInstanceId == 0)
            _localCharacterInstanceId = character.GetInstanceID();

        _playerInRange = true;

        if (_portalUiOpen)
            return;

        Dbg("OnTriggerEnter → OpenPortalUI (latch)");
        OpenPortalUI();
    }

    void OnTriggerExit(Collider other)
    {
        if (!TryValidatePlayerColliderForExit(other, out string rejectReason))
        {
            Dbg($"OnTriggerExit REJECTED | reason={rejectReason} other='{other.name}'");
            return;
        }

        int colliderId = other.GetInstanceID();
        bool removed = _localColliderIdsInside.Remove(colliderId);
        if (!removed)
        {
            Dbg($"OnTriggerExit UNTRACKED | collider='{other.name}' id={colliderId} tracked={_localColliderIdsInside.Count}");
            return;
        }

        if (_localColliderIdsInside.Count > 0)
        {
            Dbg($"OnTriggerExit PARTIAL | collider='{other.name}' id={colliderId} remaining={_localColliderIdsInside.Count}");
            return;
        }

        _playerInRange = false;
        _localCharacterInstanceId = 0;
        Dbg("OnTriggerExit | left gate volume (UI stays until player dismisses)");
    }

    public void EnsureCanvasResolved()
    {
        if (portalCanvas != null)
        {
            Dbg($"EnsureCanvasResolved | đã có portalCanvas='{portalCanvas.name}'");
            return;
        }

        InferFallbackCanvasNameFromHierarchy();

        if (string.IsNullOrWhiteSpace(fallbackCanvasName))
        {
            Dbg("EnsureCanvasResolved | fallbackCanvasName rỗng — không suy được từ hierarchy");
            return;
        }

        portalCanvas = FindCanvasByName(fallbackCanvasName);
        if (portalCanvas == null)
        {
            Debug.LogWarning($"[ScenePortal] Không tìm thấy canvas '{fallbackCanvasName}' cho '{name}'.", this);
            if (enableDebugLogs)
                LogAllDungeonCanvasesInScene("EnsureCanvasResolved failed");
        }
        else
        {
            Dbg($"EnsureCanvasResolved | tìm thấy '{portalCanvas.name}' activeInHierarchy={portalCanvas.activeInHierarchy} parentActive={(portalCanvas.transform.parent == null || portalCanvas.transform.parent.gameObject.activeInHierarchy)}");
        }
    }

    public void OpenPortalUI()
    {
        if (_portalUiOpen)
            return;

        EnsureCanvasResolved();
        if (portalCanvas == null)
        {
            Dbg("OpenPortalUI ABORT | portalCanvas == null");
            return;
        }

        CloseOtherDungeonCanvases(portalCanvas);

        _portalUiOpen = true;
        s_activePortal = this;

        CursorUIPriority.BeginUiOverlay();
        portalCanvas.SetActive(true);

        var gateFlow = DungeonGateUiFlow.FindForCanvas(portalCanvas);
        gateFlow?.ResetToDifficultySelect();

        Dbg(
            $"OpenPortalUI OK | canvas='{portalCanvas.name}' activeSelf={portalCanvas.activeSelf} " +
            $"activeInHierarchy={portalCanvas.activeInHierarchy} childCount={portalCanvas.transform.childCount}");
    }

    /// <summary>Đóng UI cổng và reset latch — gọi từ nút X / Cancel hoặc khi canvas bị tắt.</summary>
    public void ClosePortalUI()
    {
        DismissPortalUI();
    }

    /// <summary>Gắn nút đóng UI trên Inspector: DungeonGateUiFlow.CloseDungeonUi hoặc gọi trực tiếp.</summary>
    public void DismissPortalUI()
    {
        if (!_portalUiOpen)
            return;

        _portalUiOpen = false;

        if (s_activePortal == this)
            s_activePortal = null;

        if (portalCanvas != null && portalCanvas.activeSelf)
            portalCanvas.SetActive(false);

        CursorUIPriority.EndUiOverlay();

        // Reset latch cứng để tránh kẹt trigger count khi player có nhiều collider con.
        _playerInRange = false;
        _localColliderIdsInside.Clear();
        _localCharacterInstanceId = 0;

        Dbg(
            $"DismissPortalUI | latch reset canvas='{(portalCanvas != null ? portalCanvas.name : "null")}' " +
            $"inRange={_playerInRange} trackedColliders={_localColliderIdsInside.Count}");
    }

    /// <summary>Khi dungeon canvas bị SetActive(false) từ nút đóng — reset latch (không gọi SetActive lặp).</summary>
    public static void NotifyDungeonCanvasClosed(GameObject dungeonCanvasRoot)
    {
        if (dungeonCanvasRoot == null || s_activePortal == null)
            return;

        if (s_activePortal.portalCanvas != dungeonCanvasRoot)
            return;

        if (!s_activePortal._portalUiOpen)
            return;

        s_activePortal._portalUiOpen = false;
        CursorUIPriority.EndUiOverlay();

        // Đóng từ bên ngoài ScenePortal (SetActive false trực tiếp) cũng phải reset latch cứng.
        s_activePortal._playerInRange = false;
        s_activePortal._localColliderIdsInside.Clear();
        s_activePortal._localCharacterInstanceId = 0;

        s_activePortal = null;
    }

    public void LoadSelectedScene(string sceneName)
    {
        Debug.Log($"[ScenePortal] Đang chuyển đến Scene: {sceneName} bằng hệ thống Mạng!");

        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            PlayerWorldData.ReturnPosition = transform.position + transform.forward * 2f;
            PlayerWorldData.HasReturnPoint = true;

            Time.timeScale = 1f;
            StartCoroutine(TeleportRoutine(sceneName));
        }
        else
        {
            Debug.LogError($"[ScenePortal] Scene '{sceneName}' chưa được thêm vào Build Settings!");
        }
    }

    bool TryValidatePlayerColliderForEnter(Collider other, out Character character, out string rejectReason)
    {
        character = null;
        rejectReason = null;

        if (other == null)
        {
            rejectReason = "other collider null";
            return false;
        }

        if (!other.CompareTag(playerTag))
        {
            rejectReason = $"tag mismatch (cần '{playerTag}', nhận '{other.tag}')";
            return false;
        }

        character = other.GetComponentInParent<Character>();
        if (character == null)
        {
            rejectReason = "không có Character trên parent";
            return false;
        }

        if (character.Runner != null && character.Runner.IsRunning)
        {
            if (Character.Local == null)
            {
                rejectReason = "Fusion đang chạy nhưng Character.Local == null";
                return false;
            }

            if (Character.Local != character)
            {
                rejectReason = $"Fusion: không phải local player (local={Character.Local.name}, other={character.name})";
                return false;
            }
        }

        return true;
    }

    bool TryValidatePlayerColliderForExit(Collider other, out string rejectReason)
    {
        rejectReason = null;

        if (other == null)
        {
            rejectReason = "other collider null";
            return false;
        }

        if (!other.CompareTag(playerTag))
        {
            rejectReason = $"tag mismatch (cần '{playerTag}', nhận '{other.tag}')";
            return false;
        }

        Character character = other.GetComponentInParent<Character>();
        if (character == null)
        {
            rejectReason = "không có Character trên parent";
            return false;
        }

        if (_localCharacterInstanceId != 0 && character.GetInstanceID() != _localCharacterInstanceId)
        {
            rejectReason = $"character mismatch localId={_localCharacterInstanceId} otherId={character.GetInstanceID()}";
            return false;
        }

        return true;
    }

    static void WireMissingDungeonGates()
    {
        int added = 0;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            added += TryAddScenePortalOnDemonGate(root.transform);

        Debug.Log($"[ScenePortal] WireMissingDungeonGates | scene={SceneManager.GetActiveScene().name} addedScenePortalCount={added}");
    }

    static int TryAddScenePortalOnDemonGate(Transform node)
    {
        if (node == null)
            return 0;

        int added = 0;
        if (node.name.Contains("Demon_Portal"))
        {
            var questBeacon = FindChildByName(node, "QuestBeacon");
            if (questBeacon == null)
                Debug.LogWarning($"[ScenePortal] Demon_Portal '{node.name}' không có QuestBeacon.");
            else if (questBeacon.GetComponent<ScenePortal>() == null)
            {
                var col = questBeacon.GetComponent<Collider>();
                if (col == null)
                    Debug.LogWarning($"[ScenePortal] QuestBeacon '{questBeacon.name}' không có Collider.");
                else
                {
                    var sp = questBeacon.gameObject.AddComponent<ScenePortal>();
                    sp.fallbackCanvasName = "Dungeon Canvas Demon";
                    sp.enableDebugLogs = true;
                    added++;
                    Debug.Log($"[ScenePortal] Đã AddComponent ScenePortal lên Demon QuestBeacon '{questBeacon.name}'");
                }
            }
        }

        for (int i = 0; i < node.childCount; i++)
            added += TryAddScenePortalOnDemonGate(node.GetChild(i));

        return added;
    }

    static Transform FindChildByName(Transform parent, string childName)
    {
        foreach (var t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName)
                return t;
        }
        return null;
    }

    void InferFallbackCanvasNameFromHierarchy()
    {
        if (!string.IsNullOrWhiteSpace(fallbackCanvasName))
            return;

        Transform t = transform;
        while (t != null)
        {
            string n = t.name;
            if (n.Contains("DamLay"))
            {
                fallbackCanvasName = "Dungeon Canvas DamLay ";
                Dbg($"InferFallbackCanvasName | từ '{n}' → '{fallbackCanvasName}'");
                return;
            }
            if (n.Contains("Samac"))
            {
                fallbackCanvasName = "Dungeon Canvas Samac";
                Dbg($"InferFallbackCanvasName | từ '{n}' → '{fallbackCanvasName}'");
                return;
            }
            if (n.Contains("Demon"))
            {
                fallbackCanvasName = "Dungeon Canvas Demon";
                Dbg($"InferFallbackCanvasName | từ '{n}' → '{fallbackCanvasName}'");
                return;
            }
            t = t.parent;
        }

        Dbg("InferFallbackCanvasName | không khớp DamLay/Samac/Demon trong hierarchy");
    }

    static GameObject FindCanvasByName(string canvasName)
    {
        string target = canvasName.Trim();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null)
                continue;
            if (t.name.Trim() == target)
                return t.gameObject;
        }
        return null;
    }

    void LogAllDungeonCanvasesInScene(string context)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[ScenePortal] {context} | scene={SceneManager.GetActiveScene().name}");
        int count = 0;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || !t.name.Contains("Dungeon Canvas"))
                continue;
            count++;
            sb.AppendLine(
                $"  - '{t.name}' activeSelf={t.gameObject.activeSelf} inHierarchy={t.gameObject.activeInHierarchy} path={GetTransformPath(t)}");
        }
        if (count == 0)
            sb.AppendLine("  (không có object nào tên chứa 'Dungeon Canvas')");
        Debug.Log(sb.ToString(), this);
    }

    static void CloseOtherDungeonCanvases(GameObject except)
    {
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t == except.transform)
                continue;
            string n = t.name;
            if (!n.StartsWith("Dungeon Canvas"))
                continue;
            if (t.gameObject.activeSelf)
                t.gameObject.SetActive(false);
        }
    }

    void Dbg(string message)
    {
        if (!enableDebugLogs)
            return;
        Debug.Log($"[ScenePortal] {message}", this);
    }

    string GetHierarchyPath()
    {
        return GetTransformPath(transform);
    }

    static string GetTransformPath(Transform t)
    {
        if (t == null)
            return "(null)";
        var sb = new StringBuilder(t.name);
        while (t.parent != null)
        {
            t = t.parent;
            sb.Insert(0, t.name + "/");
        }
        return sb.ToString();
    }

    IEnumerator TeleportRoutine(string sceneName)
    {
        ClosePortalUI();
        yield return new WaitForSeconds(teleportDelay);

        NetworkRunner runner = FindFirstObjectByType<NetworkRunner>();

        if (runner != null && runner.IsServer)
        {
            int buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneName);
            if (buildIndex >= 0)
                runner.LoadScene(SceneRef.FromIndex(buildIndex));
            else
                Debug.LogError($"[ScenePortal] Không tìm thấy Build Index cho Scene: {sceneName}");
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
