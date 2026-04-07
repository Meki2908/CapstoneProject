using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gắn trên player prefab (root, cùng NetworkObject).
/// Sync animator parameters cho remote players để thấy animation di chuyển, tấn công, etc.
///
/// Owner đọc Animator params → ghi vào NetworkVariables.
/// Remote đọc NetworkVariables → apply lên Animator.
///
/// CHÚ Ý: Triggers được gửi qua ServerRpc → ClientRpc vì NetworkVariable
/// không phù hợp cho one-shot events.
/// </summary>
[DefaultExecutionOrder(200)]
public class NetworkAnimatorSync : NetworkBehaviour
{
    // ── Cached hashes ──
    static readonly int H_Speed = Animator.StringToHash("speed");
    static readonly int H_AttackSpeed = Animator.StringToHash("attackSpeed");
    static readonly int H_IsCrouch = Animator.StringToHash("isCrouch");

    // Trigger hashes
    static readonly int H_DrawWeapon = Animator.StringToHash("drawWeapon");
    static readonly int H_SheathWeapon = Animator.StringToHash("sheathWeapon");
    static readonly int H_Jump = Animator.StringToHash("jump");
    static readonly int H_SprintJump = Animator.StringToHash("sprintJump");
    static readonly int H_Land = Animator.StringToHash("land");
    static readonly int H_Move = Animator.StringToHash("move");
    static readonly int H_Attack = Animator.StringToHash("attack");
    static readonly int H_GetHit = Animator.StringToHash("gethit");
    static readonly int H_Die = Animator.StringToHash("die");
    static readonly int H_HardStop = Animator.StringToHash("hardStop");
    static readonly int H_Dash = Animator.StringToHash("dash");

    // ── NetworkVariables (owner write, all read) ──
    private readonly NetworkVariable<float> _netSpeed = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<float> _netAttackSpeed = new(1f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<bool> _netIsCrouch = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    Animator _animator;
    float _syncInterval = 0.05f; // 20 Hz
    float _syncTimer;

    // ── Previous values for change detection (triggers) ──
    bool _prevIsCrouch;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>(true);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Remote: subscribe to value changes
            _netSpeed.OnValueChanged += OnSpeedChanged;
            _netAttackSpeed.OnValueChanged += OnAttackSpeedChanged;
            _netIsCrouch.OnValueChanged += OnIsCrouchChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
        {
            _netSpeed.OnValueChanged -= OnSpeedChanged;
            _netAttackSpeed.OnValueChanged -= OnAttackSpeedChanged;
            _netIsCrouch.OnValueChanged -= OnIsCrouchChanged;
        }
    }

    void Update()
    {
        if (!IsSpawned || _animator == null) return;

        if (IsOwner)
        {
            _syncTimer += Time.deltaTime;
            if (_syncTimer >= _syncInterval)
            {
                _syncTimer = 0f;
                SyncOwnerToNetwork();
            }
        }
    }

    // ───────────────────── OWNER → NETWORK ─────────────────────

    void SyncOwnerToNetwork()
    {
        _netSpeed.Value = _animator.GetFloat(H_Speed);
        _netAttackSpeed.Value = _animator.GetFloat(H_AttackSpeed);
        _netIsCrouch.Value = _animator.GetBool(H_IsCrouch);
    }

    // ───────────────────── OWNER: Public trigger send (gọi từ state machine) ─────────────────────

    /// <summary>
    /// Gọi khi owner muốn gửi trigger animation tới tất cả clients.
    /// Wrapper: tự set trigger local + gửi RPC.
    /// </summary>
    public void SendTrigger(string triggerName)
    {
        if (!IsOwner || !IsSpawned) return;
        int hash = Animator.StringToHash(triggerName);
        SendTriggerServerRpc(hash);
    }

    /// <summary>
    /// Overload: gọi bằng hash trực tiếp.
    /// </summary>
    public void SendTriggerByHash(int hash)
    {
        if (!IsOwner || !IsSpawned) return;
        SendTriggerServerRpc(hash);
    }

    // ───────────────────── RPCs cho Triggers ─────────────────────

    [ServerRpc]
    void SendTriggerServerRpc(int triggerHash)
    {
        // Server relay tới tất cả clients (trừ owner — owner đã set local)
        SetTriggerClientRpc(triggerHash);
    }

    [ClientRpc]
    void SetTriggerClientRpc(int triggerHash)
    {
        if (IsOwner) return; // Owner đã có local trigger
        if (_animator != null)
            _animator.SetTrigger(triggerHash);
    }

    // ───────────────────── REMOTE: NETWORK → ANIMATOR ─────────────────────

    void OnSpeedChanged(float prev, float current)
    {
        if (_animator != null)
            _animator.SetFloat(H_Speed, current);
    }

    void OnAttackSpeedChanged(float prev, float current)
    {
        if (_animator != null)
            _animator.SetFloat(H_AttackSpeed, current);
    }

    void OnIsCrouchChanged(bool prev, bool current)
    {
        if (_animator != null)
            _animator.SetBool(H_IsCrouch, current);
    }
}
