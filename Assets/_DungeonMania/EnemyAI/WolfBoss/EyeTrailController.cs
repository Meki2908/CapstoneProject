using UnityEngine;

public class EyeTrailController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    //  INSPECTOR — Tất cả đều Serialized để tuỳ chỉnh trong Editor
    // ═══════════════════════════════════════════════════════════════════════

    [Header("═══ Spread Settings ═══")]
    [Tooltip("Khoảng dịch ngang tối đa (units) mà trail toả ra so với vị trí gốc.\n" +
             "Để tạo góc 45°: đặt bằng spreadDistance.")]
    [SerializeField] public float spreadOffset = 0.6f;

    [Tooltip("Khoảng boss PHẢI DI CHUYỂN (units) để trail toả ra hết về hướng ngang.\n" +
             "Ví dụ: spreadDistance=1 → sau khi boss đi 1 unit, trail đã lệch spreadOffset.")]
    [SerializeField] public float spreadDistance = 0.6f;

    [Tooltip("Tốc độ lerp trail về vị trí gốc khi boss đứng yên (factor/s). Cao = về nhanh.")]
    [SerializeField] public float returnSpeed = 6f;

    [Tooltip("True  = trail bên TRÁI  → toả sang TRÁI (local -X).\n" +
             "False = trail bên PHẢI → toả sang PHẢI (local +X).")]
    [SerializeField] public bool isLeftSide = true;

    [Header("═══ Motion Threshold ═══")]
    [Tooltip("Tốc độ di chuyển tối thiểu (units/s) để kích hoạt spread.\n" +
             "Giá trị nhỏ = nhạy hơn với chuyển động nhỏ.")]
    [SerializeField] public float movementThreshold = 0.05f;

    [Tooltip("Transform dùng để đo chuyển động của Boss. Để trống = tự tìm root của hierarchy.")]
    [SerializeField] public Transform rootOverride;

    // ═══════════════════════════════════════════════════════════════════════
    //  RUNTIME
    // ═══════════════════════════════════════════════════════════════════════

    private Transform _root;            // Boss root transform (dùng để đo velocity)
    private Vector3   _lastRootPos;     // Vị trí root ở frame trước
    private float     _distTraveled;    // Khoảng đã đi trong pha spread hiện tại
    private Vector3   _baseLocalPos;    // Local position gốc khi Awake
    private float     _currentSideX;   // Offset local-X hiện tại (có dấu)
    private bool      _spreadDone;      // True khi đã hoàn thành pha spread (đang đi thẳng)

    // ═══════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        _baseLocalPos  = transform.localPosition;
        _currentSideX  = 0f;
        _distTraveled  = 0f;
        _spreadDone    = false;
    }

    private void Start()
    {
        // Tìm root: ưu tiên override, sau đó root của hierarchy, cuối cùng là parent
        if (rootOverride != null)
        {
            _root = rootOverride;
        }
        else
        {
            _root = transform.root;
            // Nếu root chính là object này thì dùng parent
            if (_root == transform && transform.parent != null)
                _root = transform.parent;
        }

        _lastRootPos = _root != null ? _root.position : transform.position;
    }

    private void LateUpdate()
    {
        if (_root == null) return;

        // ── Đo chuyển động của Boss Root trong frame này ──────────────────
        Vector3 curPos  = _root.position;
        Vector3 delta   = curPos - _lastRootPos;
        delta.y = 0f;                       // Bỏ trục Y (không tính nhảy/rơi)
        float moved = delta.magnitude;
        _lastRootPos = curPos;

        float sign = isLeftSide ? -1f : 1f; // Dấu hướng toả ra

        if (moved > movementThreshold * Time.deltaTime)
        {
            // ── Boss đang di chuyển ──────────────────────────────────────

            if (!_spreadDone)
            {
                // Phase 1: Toả dần ra (0 → spreadOffset) theo khoảng đi được
                _distTraveled += moved;
                float t = Mathf.Clamp01(_distTraveled / Mathf.Max(spreadDistance, 0.001f));

                // Linear ramp: offset tăng tuyến tính → góc spread cố định trong suốt pha này
                _currentSideX = sign * Mathf.Lerp(0f, spreadOffset, t);

                if (t >= 1f)
                    _spreadDone = true; // Chuyển sang phase 2
            }
            else
            {
                // Phase 2: Giữ nguyên offset → trail đi thẳng song song
                _currentSideX = sign * spreadOffset;
            }
        }
        else
        {
            // ── Boss đứng yên hoặc quá chậm → Reset về vị trí gốc ────────
            _distTraveled = 0f;
            _spreadDone   = false;

            // Lerp mềm về 0 để tránh trail giật cục
            _currentSideX = Mathf.Lerp(_currentSideX, 0f, returnSpeed * Time.deltaTime);
        }

        // ── Áp offset vào local position (chỉ trục X) ─────────────────────
        transform.localPosition = _baseLocalPos + new Vector3(_currentSideX, 0f, 0f);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DEBUG GIZMOS
    // ═══════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Hiển thị vị trí gốc và hướng toả trong Scene View
        Vector3 origin = Application.isPlaying ? (_baseLocalPos) : transform.localPosition;
        Vector3 worldOrigin = transform.parent != null
            ? transform.parent.TransformPoint(origin)
            : transform.position;

        // Điểm gốc (xanh lá)
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(worldOrigin, 0.05f);

        // Hướng toả (cyan = trái, magenta = phải)
        Gizmos.color = isLeftSide ? Color.cyan : Color.magenta;
        float sign = isLeftSide ? -1f : 1f;

        Vector3 localOffset = new Vector3(sign * spreadOffset, 0f, 0f);
        Vector3 worldSpread = transform.parent != null
            ? transform.parent.TransformPoint(origin + localOffset)
            : worldOrigin + new Vector3(sign * spreadOffset, 0f, 0f);

        Gizmos.DrawLine(worldOrigin, worldSpread);
        Gizmos.DrawSphere(worldSpread, 0.05f);

        // Label
        UnityEditor.Handles.color = isLeftSide ? Color.cyan : Color.magenta;
        UnityEditor.Handles.Label(worldSpread + Vector3.up * 0.1f,
            $"{(isLeftSide ? "Left" : "Right")} spread\n" +
            $"offset={spreadOffset:F2} dist={spreadDistance:F2}");
    }
#endif
}
