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
    
    // Core parameters for weapon & skills
    [Networked] private int NetWeaponType { get; set; }
    [Networked] private int NetSkillIndex { get; set; }

    // Enemy specific
    [Networked] private NetworkBool NetRun { get; set; }
    [Networked] private NetworkBool NetHit { get; set; }
    [Networked] private NetworkBool NetAttack { get; set; }

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

    private float _predictedSpeed;
    private float _predictedAttackSpeed;
    private bool _predictedCrouch;

    // Client RPC throttling state
    private float _lastSentSpeed;
    private float _lastSentAttackSpeed;
    private bool _lastSentCrouch;
    private bool _lastSentRun;
    private bool _lastSentHit;
    private bool _lastSentAttack;
    private int _lastSentWeaponType;
    private int _lastSentSkillIndex;

    // Cache existence of parameters
    private bool _hasSpeed, _hasAttackSpeed, _hasCrouch, _hasRun, _hasHit, _hasAttack, _hasWeaponType, _hasSkillIndex;
    static readonly int H_Run = Animator.StringToHash("run");
    static readonly int H_Hit = Animator.StringToHash("hit");
    static readonly int H_Attack = Animator.StringToHash("attack");
    static readonly int H_WeaponType = Animator.StringToHash("weaponType");
    static readonly int H_SkillIndex = Animator.StringToHash("skillIndex");

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>(true);
        if (_animator == null)
            Debug.LogWarning($"[NetworkAnimatorSync] No Animator found on {gameObject.name}!");
        else
        {
            _hasSpeed = HasParameter("speed");
            _hasAttackSpeed = HasParameter("attackSpeed");
            _hasCrouch = HasParameter("isCrouch");
            _hasRun = HasParameter("run");
            _hasHit = HasParameter("hit");
            _hasAttack = HasParameter("attack");
            _hasWeaponType = HasParameter("weaponType");
            _hasSkillIndex = HasParameter("skillIndex");
        }
    }

    private bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        _lastTriggerCounter = TriggerCounter;

        // Init remote state từ giá trị hiện tại của animator
        if (_animator != null)
        {
            if (_hasSpeed) _remoteSpeed = _animator.GetFloat(H_Speed);
            if (_hasAttackSpeed) _remoteAttackSpeed = _animator.GetFloat(H_AttackSpeed);
            if (_hasCrouch) _remoteCrouch = _animator.GetBool(H_IsCrouch);
            
            _predictedSpeed = _remoteSpeed;
            _predictedAttackSpeed = _remoteAttackSpeed;
            _predictedCrouch = _remoteCrouch;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_animator == null) return;

        if (HasInputAuthority || HasStateAuthority) // Host/Owner reads and syncs
        {
            // ── Owner: đọc giá trị thực tế animator ──
            float realSpeed = _hasSpeed ? _animator.GetFloat(H_Speed) : 0f;
            float realAttackSpeed = _hasAttackSpeed ? _animator.GetFloat(H_AttackSpeed) : 0f;
            bool realCrouch = _hasCrouch && _animator.GetBool(H_IsCrouch);
            bool realRun = _hasRun && _animator.GetBool(H_Run);
            bool realHit = _hasHit && _animator.GetBool(H_Hit);
            bool realAttack = _hasAttack && _animator.GetBool(H_Attack);
            int realWeaponType = _hasWeaponType ? _animator.GetInteger(H_WeaponType) : 0;
            int realSkillIndex = _hasSkillIndex ? _animator.GetInteger(H_SkillIndex) : 0;

            // Cập nhật prediction state local
            _predictedSpeed = realSpeed;
            _predictedAttackSpeed = realAttackSpeed;
            _predictedCrouch = realCrouch;

            if (HasStateAuthority) 
            {
                // Nếu mình là Host (hoặc AI), ghi trực tiếp luôn (khỏi qua RPC)
                NetSpeed = realSpeed; NetAttackSpeed = realAttackSpeed; NetIsCrouch = realCrouch;
                NetRun = realRun; NetHit = realHit; NetAttack = realAttack;
                NetWeaponType = realWeaponType; NetSkillIndex = realSkillIndex;
            }
            else if (HasInputAuthority)
            {
                // Client throttle: chỉ gửi RPC nếu thay đổi đáng kể hoặc bị thay booleans
                if (Mathf.Abs(_lastSentSpeed - realSpeed) > 0.05f ||
                    Mathf.Abs(_lastSentAttackSpeed - realAttackSpeed) > 0.05f ||
                    _lastSentCrouch != realCrouch || _lastSentRun != realRun ||
                    _lastSentHit != realHit || _lastSentAttack != realAttack ||
                    _lastSentWeaponType != realWeaponType || _lastSentSkillIndex != realSkillIndex)
                {
                    _lastSentSpeed = realSpeed; _lastSentAttackSpeed = realAttackSpeed;
                    _lastSentCrouch = realCrouch; _lastSentRun = realRun; _lastSentHit = realHit; _lastSentAttack = realAttack;
                    _lastSentWeaponType = realWeaponType; _lastSentSkillIndex = realSkillIndex;
                    RPC_SyncAnimatorParams(realSpeed, realAttackSpeed, realCrouch, realRun, realHit, realAttack, realWeaponType, realSkillIndex);
                }
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
    private void RPC_SyncAnimatorParams(float speed, float attackSpeed, NetworkBool isCrouch, NetworkBool run, NetworkBool hit, NetworkBool attack, int weaponType, int skillIndex)
    {
        NetSpeed = speed;
        NetAttackSpeed = attackSpeed;
        NetIsCrouch = isCrouch;
        NetRun = run;
        NetHit = hit;
        NetAttack = attack;
        NetWeaponType = weaponType;
        NetSkillIndex = skillIndex;
    }

    public override void Render()
    {
        if (_changeDetector == null || _animator == null) return;

        // ── THỰC HIỆN ĐỒNG BỘ TRIGGER (Chạy trên tất cả các Client nhờ Render) ──
        if (TriggerCounter != _lastTriggerCounter)
        {
            _animator.SetTrigger(TriggerHash);
            _lastTriggerCounter = TriggerCounter;
        }

        // Remote players: lerp mượt từng frame
        if (!HasInputAuthority && !HasStateAuthority)
        {
            float dt = Time.deltaTime;

            if (_hasSpeed)
            {
                float currentSpeed = _animator.GetFloat(H_Speed);
                float newSpeed = Mathf.SmoothDamp(currentSpeed, _remoteSpeed, ref _remoteSpeedVel, 1f / lerpSpeed, Mathf.Infinity, dt);
                _animator.SetFloat(H_Speed, newSpeed);
            }

            if (_hasAttackSpeed)
            {
                float currentAttackSpeed = _animator.GetFloat(H_AttackSpeed);
                float newAttackSpeed = Mathf.SmoothDamp(currentAttackSpeed, _remoteAttackSpeed, ref _remoteAttackSpeedVel, 1f / lerpSpeed, Mathf.Infinity, dt);
                _animator.SetFloat(H_AttackSpeed, newAttackSpeed);
            }

            if (_hasCrouch)
            {
                bool currentCrouch = _animator.GetBool(H_IsCrouch);
                float crouchTarget = _remoteCrouch ? 1f : 0f;
                float crouchCurrent = currentCrouch ? 1f : 0f;
                float crouchLerp = Mathf.Lerp(crouchCurrent, crouchTarget, Mathf.Clamp01(dt * lerpCrouchSpeed));
                bool newCrouch = crouchLerp > 0.5f;
                if (newCrouch != currentCrouch) _animator.SetBool(H_IsCrouch, newCrouch);
            }

            // Sync Enemy Bools (Hard snap because they evaluate instantly)
            if (_hasRun && _animator.GetBool(H_Run) != NetRun) _animator.SetBool(H_Run, NetRun);
            if (_hasHit && _animator.GetBool(H_Hit) != NetHit) _animator.SetBool(H_Hit, NetHit);
            if (_hasAttack && _animator.GetBool(H_Attack) != NetAttack) _animator.SetBool(H_Attack, NetAttack);

            // Sync Core Integers instantly
            if (_hasWeaponType && _animator.GetInteger(H_WeaponType) != NetWeaponType) _animator.SetInteger(H_WeaponType, NetWeaponType);
            if (_hasSkillIndex && _animator.GetInteger(H_SkillIndex) != NetSkillIndex) _animator.SetInteger(H_SkillIndex, NetSkillIndex);
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
        
        if (HasStateAuthority)
        {
            // Host writes directly without RPC overhead
            TriggerHash = hash;
            TriggerCounter++;
        }
        else
        {
            // Client sends RPC to Host
            RPC_SendTrigger(hash);
        }
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
