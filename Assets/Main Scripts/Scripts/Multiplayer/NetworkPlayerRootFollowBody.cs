using Fusion;
using UnityEngine;

/// <summary>
/// CC + Character nằm trên child "player", còn <see cref="NetworkObject"/> trên root.
///
/// Cải tiến v3 — FULL POSITION SYNC:
/// - [Networked] NetRootPosition + NetRootRotation → đồng bộ vị trí qua mạng
/// - Host: ghi [Networked] trực tiếp (có StateAuthority)
/// - Client: gửi RPC tới host → host ghi [Networked]
/// - Remote: đọc [Networked] → SmoothDamp interpolation
///
/// Không phụ thuộc NetworkTransform — tự sync position hoàn toàn.
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

    // ── Networked: position + rotation sync ──
    [Networked] private Vector3 NetRootPosition { get; set; }
    [Networked] private Quaternion NetRootRotation { get; set; }
    [Networked] private Quaternion BodyWorldRotation { get; set; }

    // ── Remote Interpolation Settings ──
    [Header("Remote Position Interpolation")]
    [Tooltip("Khoảng cách tối đa để bắt đầu lerp (nếu remote quá xa thì teleport thẳng).")]
    [SerializeField] private float teleportThreshold = 5f;
    [Tooltip("Tốc độ lerp vị trí remote (cao = nhanh bắt kịp, thấp = mượt).")]
    [SerializeField] private float positionLerpSpeed = 50f; // Chỉnh lên Max (50f) để không còn độ trễ
    [Tooltip("Tốc độ lerp rotation remote (cao = nhanh xoay, thấp = mượt).")]
    [SerializeField] private float rotationLerpSpeed = 60f; // Chỉnh lên Max (60f) để xoay tức thời

    // ── Remote Interpolation State ──
    private Vector3 _remotePosition;
    private Quaternion _remoteRotation;
    private Vector3 _positionVel;
    private Vector3 _rotationVel;
    private Vector3 _lastNetworkPosition;
    private bool _everHadValidRemote;

    // ── RPC throttle (client → host) ──
    private float _lastRpcTime;
    private const float RPC_MIN_INTERVAL = 0.02f; // 50Hz max

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

        // Init position from current transform
        if (HasStateAuthority)
        {
            NetRootPosition = transform.position;
            NetRootRotation = transform.rotation;
            BodyWorldRotation = _body.rotation;
        }

        _remotePosition = transform.position;
        _remoteRotation = transform.rotation;
        _lastNetworkPosition = transform.position;
        _everHadValidRemote = false;
        _lastRpcTime = 0f;

        Debug.Log($"[RootFollowBody] Spawned — HasInputAuthority={HasInputAuthority}, HasStateAuthority={HasStateAuthority}, pos={transform.position}");
    }

    /// <summary>
    /// FixedUpdate (Unity) — local player: sync CC child position → root transform.
    /// Chạy mỗi physics frame để CC movement mượt.
    /// </summary>
    void FixedUpdate()
    {
        if (_body == null) return;

        bool networkReady = Object != null && Object.IsValid;

        // Remote player: không làm gì trong FixedUpdate (interpolation ở LateUpdate)
        if (networkReady && !HasInputAuthority)
            return;

        // LOCAL PLAYER: chỉ chạy khi HasInputAuthority = true
        if (!HasInputAuthority)
            return;

        Vector3 childWorldPos = _body.position;
        Quaternion childWorldRot = _body.rotation;

        Quaternion rootRot = transform.rotation;
        Vector3 rootPos = childWorldPos - rootRot * _localPos;

        transform.SetPositionAndRotation(rootPos, rootRot);
        _body.localPosition = _localPos;
        _body.localRotation = Quaternion.Inverse(rootRot) * childWorldRot;

        // BẮT BUỘC ĐẨY VỊ TRÍ LÊN MẠNG TRONG FIXED UPDATE:
        // Cụ thể cho Client Player không có Prediction:
        if (!HasStateAuthority && Runner != null)
        {
            // Client player: gửi RPC tới host (throttled bằng thời gian thực)
            float now = Time.realtimeSinceStartup;
            if (now - _lastRpcTime >= RPC_MIN_INTERVAL)
            {
                _lastRpcTime = now;
                RPC_SyncTransform(rootPos, rootRot, childWorldRot);
            }
        }
    }

    /// <summary>
    /// FixedUpdateNetwork (Fusion tick) — Cập nhật [Networked] an toàn cho Host.
    /// Không bao giờ ghi [Networked] property trong Unity Update/FixedUpdate vì Fusion sẽ Rollback!
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (_body == null) return;

        // Cho Local Player của Host:
        if (HasStateAuthority && HasInputAuthority)
        {
            NetRootPosition = transform.position;
            NetRootRotation = transform.rotation;
            BodyWorldRotation = _body.rotation;
        }
    }

    /// <summary>
    /// Client → Host: gửi position/rotation. Host ghi vào [Networked] → tự sync tới tất cả.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SyncTransform(Vector3 pos, Quaternion rot, Quaternion bodyRot)
    {
        NetRootPosition = pos;
        NetRootRotation = rot;
        BodyWorldRotation = bodyRot;
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

    /// <summary>
    /// Remote player: đọc [Networked] position → SmoothDamp → apply.
    /// </summary>
    private void ApplyRemoteInterpolation()
    {
        // Đọc vị trí từ [Networked] properties (KHÔNG dùng transform.position)
        Vector3 targetPos = NetRootPosition;
        Quaternion targetRot = NetRootRotation;

        // First valid remote position
        if (!_everHadValidRemote)
        {
            if (targetPos == Vector3.zero && NetRootPosition == Vector3.zero)
                return; // Chưa có data từ network

            _remotePosition = targetPos;
            _remoteRotation = targetRot;
            _lastNetworkPosition = targetPos;
            _everHadValidRemote = true;

            // Snap ngay lần đầu
            transform.SetPositionAndRotation(targetPos, targetRot);
            if (_body != null)
            {
                _body.localPosition = _localPos;
                _body.localRotation = Quaternion.Inverse(targetRot) * BodyWorldRotation;
            }
            return;
        }

        // Detect teleport
        if (Vector3.Distance(targetPos, _lastNetworkPosition) > teleportThreshold)
        {
            _remotePosition = targetPos;
            _remoteRotation = targetRot;
            _positionVel = Vector3.zero;
            _rotationVel = Vector3.zero;
        }
        _lastNetworkPosition = targetPos;

        float dt = Time.deltaTime;
        _remotePosition = Vector3.SmoothDamp(_remotePosition, targetPos, ref _positionVel, 1f / positionLerpSpeed, Mathf.Infinity, dt);
        _remoteRotation = QuaternionSmoothDamp(_remoteRotation, targetRot, ref _rotationVel, 1f / rotationLerpSpeed, dt);

        transform.SetPositionAndRotation(_remotePosition, _remoteRotation);

        // Apply body rotation
        if (_body != null)
        {
            _body.localPosition = _localPos;
            _body.localRotation = Quaternion.Inverse(_remoteRotation) * BodyWorldRotation;
        }
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

        // Cập nhật [Networked] nếu có quyền
        if (HasStateAuthority)
        {
            NetRootPosition = position;
            NetRootRotation = rotation;
        }

        if (_body != null)
        {
            _body.localPosition = _localPos;
            bool canReadBodyRot = Object != null && Object.IsValid;
            Quaternion bodyRot = canReadBodyRot ? BodyWorldRotation : rotation;
            _body.localRotation = Quaternion.Inverse(rotation) * bodyRot;
        }
    }
}
