using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Quest Direction Tracker — Vòng tròn + mũi tên dưới chân player.
/// Dùng World Space Canvas (hoạt động 100% trên mọi render pipeline).
/// </summary>
public class QuestDirectionTracker : MonoBehaviour
{
    [Header("=== Player ===")]
    public Transform playerTransform;

    [Header("=== Circle ===")]
    public float circleRadius = 0.7f;
    public float circleThickness = 0.04f;
    public int circleSegments = 64;

    [Header("=== Arrow ===")]
    public float arrowSize = 0.4f;

    [Header("=== Position ===")]
    public float arrowBaseDistance = 1.0f;
    public float arrowMoveAmount = 0.2f;
    public float arrowMoveSpeed = 1.5f;
    public float groundOffset = 0.15f;

    [Header("=== Visual ===")]
    public Color color = new Color(1f, 0.9f, 0f, 1f);

    [Header("=== Behavior ===")]
    public float hideDistance = 3f;
    public float searchInterval = 1f;

    Transform _player;
    Transform _target;
    float _searchTimer;

    Canvas _canvas;
    RawImage _circleImg;
    RawImage _arrowImg;
    GameObject _root;

    void Start()
    {
        _player = playerTransform != null ? playerTransform : FindPlayer();
        CreateUI();
        FindActiveMarker();
        Debug.Log($"[Tracker] START! player={_player != null}, gắn trên='{gameObject.name}'");

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
        if (_root) Destroy(_root);
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        _player = playerTransform != null ? playerTransform : FindPlayer();
        _target = null;
        FindActiveMarker();
    }

    void OnQuestChanged(int id) => StartCoroutine(DelaySearch());
    System.Collections.IEnumerator DelaySearch()
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
            if (_player == null) _player = playerTransform != null ? playerTransform : FindPlayer();
            FindActiveMarker();
        }
    }

    void LateUpdate()
    {
        if (_player == null || _target == null || _root == null)
        {
            if (_root) _root.SetActive(false);
            return;
        }

        float dist = Vector3.Distance(_player.position, _target.position);
        if (dist < hideDistance)
        {
            _root.SetActive(false);
            return;
        }

        _root.SetActive(true);

        // Vị trí dưới chân player
        Vector3 feetPos = _player.position;
        feetPos.y += groundOffset;
        _root.transform.position = feetPos;

        // Xoay canvas nằm ngang (flat trên mặt đất)
        _root.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // ── Hướng đến target ──
        Vector3 dir = _target.position - _player.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = _player.forward;
        dir.Normalize();

        // Debug mỗi 2 giây
        if (Time.frameCount % 120 == 0)
            Debug.Log($"[Tracker] Pointing to '{_target.name}' pos={_target.position}, player={_player.position}, dir={dir}, angle={Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg:F1}°");

        // ── Arrow: di chuyển xa gần + xoay về target ──
        float bob = Mathf.Sin(Time.time * arrowMoveSpeed) * arrowMoveAmount;
        float arrowDist = circleRadius + 0.05f + arrowBaseDistance + bob;

        // Tính góc từ forward (Z+) đến hướng target
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _arrowImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
        _arrowImg.rectTransform.localPosition = new Vector3(dir.x * arrowDist, dir.z * arrowDist, 0f);

        // ── Pulse ──
        float pulse = Mathf.Lerp(0.7f, 1f, (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f);
        Color c = color;
        c.a = pulse;
        _circleImg.color = c;
        _arrowImg.color = c;
    }

    // ═══════════════════════════════════════
    //  TẠO UI (World Space Canvas)
    // ═══════════════════════════════════════

    void CreateUI()
    {
        // Root object
        _root = new GameObject("QDT_Root");
        _root.SetActive(false);

        // Canvas World Space
        _canvas = _root.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 100;

        // Chặn EventSystem raycast
        var cg = _root.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Scale canvas: 1 unit = 1 meter
        _root.transform.localScale = Vector3.one;
        var rt = _root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(5f, 5f); // 5x5 meter area
        rt.pivot = new Vector2(0.5f, 0.5f);

        // ── Circle Image ──
        var circleGo = new GameObject("Circle");
        circleGo.transform.SetParent(_root.transform, false);
        _circleImg = circleGo.AddComponent<RawImage>();
        _circleImg.texture = GenerateCircleTexture(256, circleRadius, circleThickness);
        _circleImg.color = color;
        _circleImg.raycastTarget = false;
        var circleRT = _circleImg.rectTransform;
        float circleWorldSize = circleRadius * 2f + circleThickness;
        circleRT.sizeDelta = new Vector2(circleWorldSize, circleWorldSize);
        circleRT.localPosition = Vector3.zero;

        // ── Arrow Image ──
        var arrowGo = new GameObject("Arrow");
        arrowGo.transform.SetParent(_root.transform, false);
        _arrowImg = arrowGo.AddComponent<RawImage>();
        _arrowImg.texture = GenerateArrowTexture(128);
        _arrowImg.color = color;
        _arrowImg.raycastTarget = false;
        var arrowRT = _arrowImg.rectTransform;
        arrowRT.sizeDelta = new Vector2(arrowSize, arrowSize);
        arrowRT.localPosition = Vector3.zero;
    }

    // ═══════════════════════════════════════
    //  GENERATE TEXTURES
    // ═══════════════════════════════════════

    Texture2D GenerateCircleTexture(int size, float radius, float thickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        // Normalize radius/thickness to pixel space
        float rNorm = radius / (radius + thickness * 0.5f); // Normalized to [0,1]
        float halfThick = (thickness / (radius + thickness * 0.5f)) * 0.5f;
        float rInner = (rNorm - halfThick) * center;
        float rOuter = (rNorm + halfThick) * center;
        float aa = 1.5f; // antialiasing

        Color clear = new Color(1, 1, 1, 0);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 0f;
                if (dist >= rInner - aa && dist <= rOuter + aa)
                {
                    float innerEdge = Mathf.Clamp01((dist - (rInner - aa)) / aa);
                    float outerEdge = Mathf.Clamp01(((rOuter + aa) - dist) / aa);
                    alpha = Mathf.Min(innerEdge, outerEdge);
                }

                tex.SetPixel(x, y, alpha > 0 ? new Color(1, 1, 1, alpha) : clear);
            }
        }

        tex.Apply();
        return tex;
    }

    Texture2D GenerateArrowTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color clear = new Color(1, 1, 1, 0);

        // Arrow pointing UP (Y+)
        float cx = size * 0.5f;
        float tipY = size * 0.85f;
        float baseY = size * 0.15f;
        float bodyW = size * 0.15f;
        float headW = size * 0.35f;
        float headBaseY = size * 0.55f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = false;
                float fx = x;
                float fy = y;

                // Body rectangle
                if (fy >= baseY && fy <= headBaseY)
                {
                    if (Mathf.Abs(fx - cx) <= bodyW)
                        inside = true;
                }

                // Head triangle
                if (fy >= headBaseY && fy <= tipY)
                {
                    float t = (fy - headBaseY) / (tipY - headBaseY);
                    float halfW = Mathf.Lerp(headW, 0f, t);
                    if (Mathf.Abs(fx - cx) <= halfW)
                        inside = true;
                }

                tex.SetPixel(x, y, inside ? Color.white : clear);
            }
        }

        tex.Apply();
        return tex;
    }

    // ═══════════════════════════════════════
    //  FIND ACTIVE MARKER
    // ═══════════════════════════════════════

    void FindActiveMarker()
    {
        _target = null;
        var markers = FindObjectsByType<QuestMarker>(FindObjectsSortMode.None);
        if (QuestManager.Instance == null) return;
        
        float closestDist = float.MaxValue;
        Transform bestTarget = null;
        int matchedQuestID = -1;

        // Tìm marker đúng quest active + đúng step
        foreach (var m in markers)
        {
            if (m == null || !m.gameObject.activeInHierarchy) continue;

            var state = QuestManager.Instance.GetState(m.questID);
            if (state != QuestManager.QuestState.Active) continue;

            int currentStep = QuestManager.Instance.GetStepIndex(m.questID);
            if (m.showAtStep != currentStep) continue;

            // Tính khoảng cách từ player đến marker này
            float dist = _player != null ? Vector3.Distance(_player.position, m.transform.position) : 0f;
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = m.transform;
                matchedQuestID = m.questID;
            }
        }
        
        if (bestTarget != null)
        {
            _target = bestTarget;
            Debug.Log($"[Tracker] Matched quest={matchedQuestID}, closest marker='{bestTarget.name}' at {bestTarget.position} (dist: {closestDist:F1})");
        }
    }

    Transform FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }
}
