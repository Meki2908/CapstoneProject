using Fusion;
using UnityEngine;

/// <summary>
/// CC + Character nằm trên child "player", còn <see cref="NetworkObject"/> + NetworkTransform trên root.
///
/// Cải tiến so với bản cũ:
/// - Remote: dùng INTERPOLATION (SmoothDamp) thay vì snap trực tiếp
/// - Thêm buffer để smooth giữa các network snapshot
/// - Prediction cho local position (dùng CC movement, chỉ sync world rotation body)
///
/// Chỉ đồng bộ vị trí root với CC; không gán root.rotation theo thân nhân vật.
/// Hướng mesh giữ bằng localRotation của body: Inverse(root) * childWorldRot.
/// </summary>
[DefaultExecutionOrder(100)]
public class NetworkPlayerRootFollowBody : NetworkBehaviour
{
    // ── References ──
    Transform _body;
    Vector3 _localPos;

    // ── Networked ──
    [Networked] private Quaternion BodyWorldRotation { get; set; }

    // ── Remote Interpolation Settings ──
    [Header("Remote Position Interpolation")]
    [Tooltip("Khoảng cách tối đa để bắt đầu lerp (nếu remote quá xa thì teleport thẳng).")]
    [SerializeField] private float teleportThreshold = 5f;
    [Tooltip("Tốc độ lerp vị trí remote (cao = nhanh bắt kịp, thấp = mượt).")]
    [SerializeField] private float positionLerpSpeed = 12f;
    [Tooltip("Tốc độ lerp rotation remote (cao = nhanh xoay, thấp = mượt).")]
    [SerializeField] private float rotationLerpSpeed = 15f;

    // ── Remote Interpolation State ──
    private Vector3 _remotePosition;
    private Quaternion _remoteRotation;
    private Vector3 _positionVel;
    private Vector3 _rotationVel; // Quaternion smooth damp dùng Vector3 cho angular velocity
    private Vector3 _lastNetworkPosition;
    private bool _everHadValidRemote;

    void Awake()
    {
        var cc = GetComponentInChildren<CharacterController>(true);
        if (cc != null)
        {
            _body = cc.transform;
            _localPos = _body.localPosition;
        }
        else
        {
            Debug.LogWarning($"[RootFollowBody] No CharacterController found on {gameObject.name}!");
        }
    }

    public override void Spawned()
    {
        if (_body == null) return;

        _remotePosition = transform.position;
        _remoteRotation = transform.rotation;
        _lastNetworkPosition = transform.position;
        _everHadValidRemote = false;
    }

    void FixedUpdate()
    {
        if (_body == null) return;

        bool networkReady = Object != null && Object.IsValid;

        if (networkReady && !HasInputAuthority)
        {
            ApplyRemoteBodyRotation();
            return;
        }

        // ── LOCAL PLAYER: sync body rotation lên network ──
        Vector3 childWorldPos = _body.position;
        Quaternion childWorldRot = _body.rotation;

        Quaternion rootRot = transform.rotation;
        Vector3 rootPos = childWorldPos - rootRot * _localPos;

        transform.SetPositionAndRotation(rootPos, rootRot);
        _body.localPosition = _localPos;
        _body.localRotation = Quaternion.Inverse(rootRot) * childWorldRot;

        if (networkReady && HasInputAuthority)
            BodyWorldRotation = childWorldRot;
    }

    void LateUpdate()
    {
        if (_body == null) return;
        bool networkReady = Object != null && Object.IsValid;

        if (networkReady && !HasInputAuthority)
        {
            ApplyRemoteInterpolation();
        }
    }

    private void ApplyRemoteBodyRotation()
    {
        if (_body == null) return;

        if (!_everHadValidRemote)
        {
            _remotePosition = transform.position;
            _remoteRotation = transform.rotation;
            _lastNetworkPosition = transform.position;
            _everHadValidRemote = true;
        }

        Vector3 currentNetPos = transform.position;

        // Detect teleport
        if (Vector3.Distance(currentNetPos, _lastNetworkPosition) > teleportThreshold)
        {
            _remotePosition = currentNetPos;
            _remoteRotation = transform.rotation;
            _positionVel = Vector3.zero;
            _rotationVel = Vector3.zero;
            Debug.Log($"[RootFollow] Teleport detected: {_lastNetworkPosition} -> {currentNetPos}");
        }
        _lastNetworkPosition = currentNetPos;

        float dt = Time.deltaTime;
        _remotePosition = Vector3.SmoothDamp(_remotePosition, currentNetPos, ref _positionVel, 1f / positionLerpSpeed, Mathf.Infinity, dt);
        _remoteRotation = QuaternionSmoothDamp(_remoteRotation, transform.rotation, ref _rotationVel, 1f / rotationLerpSpeed, dt);

        transform.SetPositionAndRotation(_remotePosition, _remoteRotation);

        _body.localPosition = _localPos;
        _body.localRotation = Quaternion.Inverse(_remoteRotation) * BodyWorldRotation;
    }

    private void ApplyRemoteInterpolation()
    {
        if (!_everHadValidRemote) return;

        Vector3 currentNetPos = transform.position;

        if (Vector3.Distance(currentNetPos, _lastNetworkPosition) > teleportThreshold)
        {
            _remotePosition = currentNetPos;
            _remoteRotation = transform.rotation;
            _positionVel = Vector3.zero;
            _rotationVel = Vector3.zero;
        }
        _lastNetworkPosition = currentNetPos;

        float dt = Time.deltaTime;
        _remotePosition = Vector3.SmoothDamp(_remotePosition, currentNetPos, ref _positionVel, 1f / positionLerpSpeed, Mathf.Infinity, dt);
        _remoteRotation = QuaternionSmoothDamp(_remoteRotation, transform.rotation, ref _rotationVel, 1f / rotationLerpSpeed, dt);

        transform.SetPositionAndRotation(_remotePosition, _remoteRotation);
    }

    /// <summary>
    /// SmoothDamp cho Quaternion — tương tự SmoothDamp của Vector3 nhưng cho góc quay.
    /// Sử dụng quaternion slerp với damping factor.
    /// </summary>
    private static Quaternion QuaternionSmoothDamp(Quaternion current, Quaternion target, ref Vector3 vel, float smoothTime, float deltaTime)
    {
        // Chuyển sang góc Euler để smooth damp
        Vector3 currentEuler = current.eulerAngles;
        Vector3 targetEuler = target.eulerAngles;

        // Xử lý wrap-around (góc 0-360)
        Vector3 delta = targetEuler - currentEuler;
        for (int i = 0; i < 3; i++)
        {
            while (delta[i] > 180f) delta[i] -= 360f;
            while (delta[i] < -180f) delta[i] += 360f;
        }

        Vector3 smoothed = Vector3.SmoothDamp(currentEuler, currentEuler + delta, ref vel, 1f / smoothTime, Mathf.Infinity, deltaTime);

        return Quaternion.Euler(smoothed);
    }

    // ───────────────────── PUBLIC API ─────────────────────

    /// <summary>
    /// Force teleport remote player tới vị trí chỉ định.
    /// Dùng cho respawn, dungeon transition, etc.
    /// </summary>
    public void ForceTeleport(Vector3 position, Quaternion rotation)
    {
        _remotePosition = position;
        _remoteRotation = rotation;
        _lastNetworkPosition = position;
        _positionVel = Vector3.zero;
        _rotationVel = Vector3.zero;
        transform.SetPositionAndRotation(position, rotation);

        if (_body != null)
        {
            _body.localPosition = _localPos;
            _body.localRotation = Quaternion.Inverse(rotation) * BodyWorldRotation;
        }
    }
}
