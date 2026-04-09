using Fusion;
using UnityEngine;

/// <summary>
/// Gắn trên player prefab (root, cùng NetworkObject).
/// Sync animator parameters cho remote players mượt mà, không độ trễ.
///
/// Cải tiến so với bản cũ:
/// - Sync 50Hz thay vì 20Hz (mỗi tick Fusion)
/// - Lerp mượt cho remote animation thay vì snap đột ngột
/// - Prediction cho local player
/// - Trigger animation tối ưu qua StateAuthority
/// </summary>
[DefaultExecutionOrder(200)]
public class NetworkAnimatorSync : NetworkBehaviour
{
    // ── Cached hashes ──
    static readonly int H_Speed = Animator.StringToHash("speed");
    static readonly int H_AttackSpeed = Animator.StringToHash("attackSpeed");
    static readonly int H_IsCrouch = Animator.StringToHash("isCrouch");

    // ── Networked Properties (owner write, all read) ──
    [Networked] private float NetSpeed { get; set; }
    [Networked] private float NetAttackSpeed { get; set; }
    [Networked] private NetworkBool NetIsCrouch { get; set; }

    // Trigger sử dụng networked counter để detect thay đổi
    [Networked] private int TriggerHash { get; set; }
    [Networked] private int TriggerCounter { get; set; }

    Animator _animator;

    // ── Remote Interpolation ──
    [Header("Interpolation Settings")]
    [Tooltip("Tốc độ lerp cho speed/attackSpeed (cao = nhanh bắt kịp, thấp = mượt hơn).")]
    [SerializeField] private float lerpSpeed = 50f; // Max tốc độ, không trễ
    [Tooltip("Tốc độ lerp cho isCrouch bool.")]
    [SerializeField] private float lerpCrouchSpeed = 60f; // Max tốc độ

    // Remote interpolation state
    private float _remoteSpeed;
    private float _remoteAttackSpeed;
    private bool _remoteCrouch;
    private float _remoteSpeedVel;
    private float _remoteAttackSpeedVel;
    private int _lastTriggerCounter;
    private ChangeDetector _changeDetector;

    // Prediction state cho local
    private float _predictedSpeed;
    private float _predictedAttackSpeed;
    private bool _predictedCrouch;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>(true);
        if (_animator == null)
            Debug.LogWarning($"[NetworkAnimatorSync] No Animator found on {gameObject.name}!");
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        _lastTriggerCounter = TriggerCounter;

        // Init remote state từ giá trị hiện tại của animator
        if (_animator != null)
        {
            _remoteSpeed = _animator.GetFloat(H_Speed);
            _remoteAttackSpeed = _animator.GetFloat(H_AttackSpeed);
            _remoteCrouch = _animator.GetBool(H_IsCrouch);
            _predictedSpeed = _remoteSpeed;
            _predictedAttackSpeed = _remoteAttackSpeed;
            _predictedCrouch = _remoteCrouch;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_animator == null) return;

        if (HasInputAuthority)
        {
            // ── Owner: đọc giá trị thực từ animator ──
            float realSpeed = _animator.GetFloat(H_Speed);
            float realAttackSpeed = _animator.GetFloat(H_AttackSpeed);
            bool realCrouch = _animator.GetBool(H_IsCrouch);

            // Cập nhật prediction state local
            _predictedSpeed = realSpeed;
            _predictedAttackSpeed = realAttackSpeed;
            _predictedCrouch = realCrouch;

            if (HasStateAuthority)
            {
                // Host player: ghi trực tiếp (có cả InputAuthority + StateAuthority)
                NetSpeed = realSpeed;
                NetAttackSpeed = realAttackSpeed;
                NetIsCrouch = realCrouch;
            }
            else
            {
                // BUG-1a fix: Client player gửi qua RPC thay vì ghi trực tiếp
                RPC_SyncAnimatorParams(realSpeed, realAttackSpeed, realCrouch);
            }
        }
        else
        {
            // ── Remote: cập nhật giá trị mạng vào interpolation state ──
            _remoteSpeed = NetSpeed;
            _remoteAttackSpeed = NetAttackSpeed;
            _remoteCrouch = NetIsCrouch;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SyncAnimatorParams(float speed, float attackSpeed, NetworkBool isCrouch)
    {
        NetSpeed = speed;
        NetAttackSpeed = attackSpeed;
        NetIsCrouch = isCrouch;
    }

    public override void Render()
    {
        if (_changeDetector == null || _animator == null) return;

        // Remote players: lerp mượt từng frame
        if (!HasInputAuthority)
        {
            float dt = Time.deltaTime;
            float currentSpeed = _animator.GetFloat(H_Speed);
            float currentAttackSpeed = _animator.GetFloat(H_AttackSpeed);
            bool currentCrouch = _animator.GetBool(H_IsCrouch);

            // Smooth lerp — dùng SmoothDamp cho mượt hơn Lerp thuần
            float newSpeed = Mathf.SmoothDamp(currentSpeed, _remoteSpeed, ref _remoteSpeedVel, 1f / lerpSpeed, Mathf.Infinity, dt);
            float newAttackSpeed = Mathf.SmoothDamp(currentAttackSpeed, _remoteAttackSpeed, ref _remoteAttackSpeedVel, 1f / lerpSpeed, Mathf.Infinity, dt);

            _animator.SetFloat(H_Speed, newSpeed);
            _animator.SetFloat(H_AttackSpeed, newAttackSpeed);

            // Crouch: dùng lerp cho bool (fade giữa true/false)
            float crouchTarget = _remoteCrouch ? 1f : 0f;
            float crouchCurrent = currentCrouch ? 1f : 0f;
            float crouchLerp = Mathf.Lerp(crouchCurrent, crouchTarget, Mathf.Clamp01(dt * lerpCrouchSpeed));
            bool newCrouch = crouchLerp > 0.5f;
            if (newCrouch != currentCrouch)
                _animator.SetBool(H_IsCrouch, newCrouch);
        }

        // ── Triggers: detect thay đổi ──
        if (TriggerCounter != _lastTriggerCounter)
        {
            _animator.SetTrigger(TriggerHash);
            _lastTriggerCounter = TriggerCounter;
        }
    }

    // ───────────────────── PUBLIC API ─────────────────────

    /// <summary>
    /// Gửi trigger animation tới tất cả clients.
    /// Gọi từ local player.
    /// </summary>
    public void SendTrigger(string triggerName)
    {
        if (!HasInputAuthority) return;
        int hash = Animator.StringToHash(triggerName);
        SendTriggerByHash(hash);
    }

    /// <summary>
    /// Gửi trigger bằng hash trực tiếp (tránh string allocation).
    /// </summary>
    public void SendTriggerByHash(int hash)
    {
        if (!HasInputAuthority) return;
        RPC_SendTrigger(hash);
    }

    // ───────────────────── RPC ─────────────────────

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_SendTrigger(int triggerHash)
    {
        TriggerHash = triggerHash;
        TriggerCounter++;
    }

    // ───────────────────── REMOTE STATE ACCESS ─────────────────────

    /// <summary>
    /// Đọc remote animation state (dùng cho debug UI hoặc effects).
    /// </summary>
    public float RemoteSpeed => _remoteSpeed;
    public float RemoteAttackSpeed => _remoteAttackSpeed;
    public bool RemoteCrouch => _remoteCrouch;

    /// <summary>
    /// Đọc predicted state của local player.
    /// </summary>
    public float LocalSpeed => _predictedSpeed;
    public float LocalAttackSpeed => _predictedAttackSpeed;
    public bool LocalCrouch => _predictedCrouch;
}
