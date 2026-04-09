using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quest Direction Tracker — Vòng tròn vàng + mũi tên chỉ hướng dưới chân player.
/// Mũi tên di chuyển xa gần liên tục.
/// 
/// ★ Tự tìm QuestMarker đang active → chỉ về đó
/// ★ Dùng LineRenderer + Sprites/Default (URP safe)
/// ★ Gắn lên Player
/// </summary>
public class QuestDirectionTracker : MonoBehaviour
{
    [Header("=== Circle ===")]
    [Range(0.3f, 2f)] public float circleRadius = 0.7f;
    [Range(0.02f, 0.15f)] public float circleWidth = 0.06f;
    [Range(16, 64)] public int circleSegments = 32;

    [Header("=== Arrow ===")]
    [Range(0.3f, 1.5f)] public float arrowBodyLength = 0.4f;
    [Range(0.1f, 0.5f)] public float arrowBodyWidth  = 0.15f;
    [Range(0.2f, 0.8f)] public float arrowHeadWidth   = 0.35f;
    [Range(0.1f, 0.5f)] public float arrowHeadLength  = 0.25f;

    [Header("=== Position ===")]
    [Range(0.5f, 3f)] public float arrowBaseDistance = 1.0f;
    [Range(0.1f, 0.5f)] public float arrowMoveAmount = 0.25f;
    [Range(0.5f, 3f)] public float arrowMoveSpeed = 1.5f;
    [Range(0.01f, 0.2f)] public float groundOffset = 0.08f;

    [Header("=== Visual ===")]
    public Color color = new Color(1f, 1f, 0f, 1f); // Yellow
    [Range(1f, 5f)] public float pulseSpeed = 2f;

    [Header("=== Behavior ===")]
    [Range(1f, 10f)] public float hideDistance = 3f;
    [Range(0.5f, 5f)] public float searchInterval = 1f;

    // ─── Runtime ────────────────────────────────────────────
    Transform _player;
    Transform _target;
    LineRenderer _circleLine;
    LineRenderer _arrowLine;
    Material _mat;
    float _searchTimer;
    GameObject _circleObj;
    GameObject _arrowObj;

    // ═══════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════

    void Start()
    {
        _player = FindPlayer();
        BuildVisuals();
        FindActiveMarker();

        SceneManager.sceneLoaded += OnSceneLoaded;
        QuestManager.OnQuestAccepted += OnQuestChanged;
        QuestManager.OnQuestStepAdvanced += OnQuestChanged;
        QuestManager.OnQuestCompleted += OnQuestChanged;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        QuestManager.OnQuestAccepted -= OnQuestChanged;
        QuestManager.OnQuestStepAdvanced -= OnQuestChanged;
        QuestManager.OnQuestCompleted -= OnQuestChanged;
        if (_mat != null) Destroy(_mat);
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        _player = FindPlayer();
        _target = null;
        FindActiveMarker();
    }

    void OnQuestChanged(int id)
    {
        StartCoroutine(DelayedSearch());
    }

    System.Collections.IEnumerator DelayedSearch()
    {
        yield return null;
        FindActiveMarker();
    }

    void Update()
    {
        _searchTimer += Time.deltaTime;
        if (_searchTimer >= searchInterval)
        {
            _searchTimer = 0f;
            if (_player == null) _player = FindPlayer();
            FindActiveMarker();
        }
        UpdateVisuals();
    }

    // ═══════════════════════════════════════════════════════════
    //  BUILD VISUALS
    // ═══════════════════════════════════════════════════════════

    void BuildVisuals()
    {
        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        _mat = new Material(shader);
        _mat.color = color;

        // ── Circle ──
        _circleObj = new GameObject("TrackerCircle");
        _circleObj.transform.SetParent(transform);
        _circleLine = _circleObj.AddComponent<LineRenderer>();
        SetupLine(_circleLine, circleWidth);
        _circleLine.loop = true;
        _circleLine.positionCount = circleSegments;
        _circleLine.enabled = false;

        // ── Arrow ──
        _arrowObj = new GameObject("TrackerArrow");
        _arrowObj.transform.SetParent(transform);
        _arrowLine = _arrowObj.AddComponent<LineRenderer>();
        SetupLine(_arrowLine, arrowBodyWidth);
        _arrowLine.loop = true;
        _arrowLine.positionCount = 7; // Arrow polygon
        _arrowLine.enabled = false;

        // Arrow width: uniform (polygon outline)
        _arrowLine.widthMultiplier = arrowBodyWidth * 0.5f;
    }

    void SetupLine(LineRenderer lr, float width)
    {
        lr.material = _mat;
        lr.startColor = color;
        lr.endColor = color;
        lr.widthMultiplier = width;
        lr.useWorldSpace = true;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.allowOcclusionWhenDynamic = false;
    }

    // ═══════════════════════════════════════════════════════════
    //  FIND ACTIVE MARKER
    // ═══════════════════════════════════════════════════════════

    void FindActiveMarker()
    {
        _target = null;
        var markers = FindObjectsByType<QuestMarker>(FindObjectsSortMode.None);
        foreach (var m in markers)
        {
            if (m == null || !m.gameObject.activeInHierarchy) continue;
            if (m.markerObject == null) continue;

            bool isVisible;
            if (m.markerObject == m.gameObject)
            {
                var r = m.GetComponentInChildren<Renderer>();
                isVisible = r != null && r.enabled;
            }
            else
            {
                isVisible = m.markerObject.activeInHierarchy;
            }

            if (isVisible)
            {
                _target = m.transform;
                return;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  UPDATE VISUALS
    // ═══════════════════════════════════════════════════════════

    void UpdateVisuals()
    {
        bool show = _player != null && _target != null;

        if (show)
        {
            float dist = Vector3.Distance(_player.position, _target.position);
            if (dist < hideDistance) show = false;
        }

        if (!show)
        {
            if (_circleLine) _circleLine.enabled = false;
            if (_arrowLine) _arrowLine.enabled = false;
            return;
        }

        _circleLine.enabled = true;
        _arrowLine.enabled = true;

        // ── Direction ──
        Vector3 dir = (_target.position - _player.position);
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) dir = _player.forward;
        dir.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
        float groundY = _player.position.y + groundOffset;

        // ── Pulse ──
        float pulse = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        Color c = color;
        c.a = pulse;
        _circleLine.startColor = c;
        _circleLine.endColor = c;
        _arrowLine.startColor = c;
        _arrowLine.endColor = c;

        // ══════════════════════════════════
        //  CIRCLE — dưới chân player
        // ══════════════════════════════════
        Vector3 circleCenter = _player.position;
        circleCenter.y = groundY;

        for (int i = 0; i < circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * circleRadius;
            float z = Mathf.Sin(angle) * circleRadius;
            _circleLine.SetPosition(i, circleCenter + new Vector3(x, 0, z));
        }

        // ══════════════════════════════════
        //  ARROW — phía trước, di chuyển xa gần
        // ══════════════════════════════════
        float moveOffset = Mathf.Sin(Time.time * arrowMoveSpeed) * arrowMoveAmount;
        float arrowDist = circleRadius + 0.15f + arrowBaseDistance + moveOffset;

        Vector3 arrowCenter = circleCenter + dir * arrowDist;

        // 7 points tạo hình mũi tên (polygon loop):
        //
        //        6
        //       / \
        //      5   0
        //      |   |
        //      4   1
        //      |   |
        //      3───2
        //
        float hw = arrowBodyWidth * 0.5f;   // Half body width
        float hhw = arrowHeadWidth * 0.5f;  // Half head width
        float bodyLen = arrowBodyLength;
        float headLen = arrowHeadLength;

        Vector3 bodyBottom = arrowCenter - dir * bodyLen * 0.5f;
        Vector3 bodyTop = arrowCenter + dir * bodyLen * 0.5f;
        Vector3 tipPoint = bodyTop + dir * headLen;

        Vector3[] pts = new Vector3[7];
        pts[0] = bodyTop + right * hw;       // Body top-right
        pts[1] = bodyBottom + right * hw;    // Body bottom-right
        pts[2] = bodyBottom - right * hw;    // Body bottom-left
        pts[3] = bodyBottom - right * hw;    // Body bottom-left (same, for clean corner)
        pts[4] = bodyTop - right * hw;       // Body top-left
        pts[5] = bodyTop - right * hhw;      // Head base-left (wider)
        pts[6] = tipPoint;                    // Tip

        // Sửa lại để head rộng hơn body:
        pts[0] = bodyTop + right * hhw;      // Head base-right
        pts[5] = bodyTop - right * hhw;      // Head base-left

        // Insert body points correctly (rectangle + triangle)
        // Bottom-right → Bottom-left → Top-left → Head-left → Tip → Head-right → Top-right
        pts[0] = bodyBottom + right * hw;       // Bottom-right
        pts[1] = bodyBottom - right * hw;       // Bottom-left
        pts[2] = bodyTop - right * hw;          // Top-left (body)
        pts[3] = bodyTop - right * hhw;         // Head base-left (wider)
        pts[4] = tipPoint;                       // Tip
        pts[5] = bodyTop + right * hhw;         // Head base-right (wider)
        pts[6] = bodyTop + right * hw;          // Top-right (body)

        // Set Y = ground
        for (int i = 0; i < 7; i++) pts[i].y = groundY;

        _arrowLine.positionCount = 7;
        for (int i = 0; i < 7; i++)
            _arrowLine.SetPosition(i, pts[i]);
    }

    // ═══════════════════════════════════════════════════════════

    Transform FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }
}
