using Fusion;
using UnityEngine;

/// <summary>
/// Gắn trên player prefab (root, cùng NetworkObject).
/// Sync vital stats (HP, max HP, alive) cho tất cả clients.
///
/// Cải tiến so với bản cũ:
/// - Smooth HP interpolation trên remote clients (không nhảy đột ngột)
/// - Bắt đầu server authority cho HP
/// - Thêm visual feedback khi HP thay đổi nhanh
/// - Tối ưu ChangeDetector
/// </summary>
[DefaultExecutionOrder(210)]
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerStats : NetworkBehaviour
{
    // ── Networked Properties ──
    [Networked] public float NetHP { get; set; }
    [Networked] public float NetMaxHP { get; set; }
    [Networked] public NetworkBool NetIsAlive { get; set; }

    /// <summary>Event khi remote HP thay đổi (dùng cho HP bar UI).</summary>
    public event System.Action<float, float> OnRemoteHealthChanged; // (currentHP, maxHP)

    private ChangeDetector _changeDetector;

    // ── Remote HP Interpolation ──
    [Header("HP Interpolation")]
    [Tooltip("Tốc độ lerp HP (cao = nhanh bắt kịp server, thấp = mượt hơn).")]
    [SerializeField] private float hpLerpSpeed = 8f;

    private float _displayHP;
    private float _displayMaxHP;
    private float _hpLerpVel;

    private PlayerHealth _autoHealthSync;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);

        // Init display HP từ networked value
        _displayHP = NetHP;
        _displayMaxHP = NetMaxHP;

        if (HasStateAuthority)
        {
            SyncFromPlayerHealth();
        }

        // Auto-bind với PlayerHealth
        if (_autoHealthSync == null)
        {
            _autoHealthSync = GetComponentInChildren<PlayerHealth>(true);
            if (_autoHealthSync != null)
            {
                _autoHealthSync.OnHealthChanged += OnHealthChangedHandler;
                _autoHealthSync.OnPlayerDied += OnPlayerDiedHandler;
                Debug.Log($"[NetStats] Auto-bound to PlayerHealth on {gameObject.name}");
            }
        }
    }

    private void OnHealthChangedHandler(float currentHP)
    {
        if (_autoHealthSync != null)
            UpdateHP(_autoHealthSync.CurrentHealth, _autoHealthSync.MaxHealth, _autoHealthSync.IsAlive);
    }

    private void OnPlayerDiedHandler()
    {
        if (_autoHealthSync != null)
            UpdateHP(0f, _autoHealthSync.MaxHealth, false);
    }

    void OnDestroy()
    {
        if (_autoHealthSync != null)
        {
            _autoHealthSync.OnHealthChanged -= OnHealthChangedHandler;
            _autoHealthSync.OnPlayerDied -= OnPlayerDiedHandler;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Remote: cập nhật giá trị mạng vào display state
        if (!HasInputAuthority)
        {
            _displayHP = NetHP;
            _displayMaxHP = NetMaxHP;
        }
    }

    public override void Render()
    {
        if (_changeDetector == null) return;

        // Remote: smooth lerp HP mỗi frame
        if (!HasInputAuthority)
        {
            float dt = Time.deltaTime;
            float targetHP = NetHP;
            float targetMaxHP = NetMaxHP;

            // Smooth HP lerp
            float newHP = Mathf.SmoothDamp(_displayHP, targetHP, ref _hpLerpVel, 1f / hpLerpSpeed, Mathf.Infinity, dt);

            // MaxHP thường ít thay đổi nên không cần lerp phức tạp
            if (!Mathf.Approximately(_displayMaxHP, targetMaxHP))
                _displayMaxHP = targetMaxHP;

            // Chỉ fire event khi HP thay đổi đáng kể (tránh spam)
            if (Mathf.Abs(newHP - _displayHP) > 0.01f || Mathf.Abs(_displayMaxHP - targetMaxHP) > 0.01f)
            {
                _displayHP = newHP;
                OnRemoteHealthChanged?.Invoke(_displayHP, _displayMaxHP);
            }
        }
    }

    /// <summary>
    /// Owner gọi để cập nhật HP. Client gửi RPC tới host.
    /// </summary>
    public void UpdateHP(float currentHP, float maxHP, bool isAlive)
    {
        if (HasStateAuthority)
        {
            // Host: update trực tiếp
            NetHP = currentHP;
            NetMaxHP = maxHP;
            NetIsAlive = isAlive;
        }
        else if (HasInputAuthority)
        {
            // Client: gửi RPC tới host
            RPC_RequestUpdateHP(currentHP, maxHP, isAlive);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestUpdateHP(float hp, float maxHP, bool alive)
    {
        NetHP = hp;
        NetMaxHP = maxHP;
        NetIsAlive = alive;
    }

    private void SyncFromPlayerHealth()
    {
        var ph = GetComponentInChildren<PlayerHealth>(true);
        if (ph != null)
        {
            NetHP = ph.CurrentHealth;
            NetMaxHP = ph.MaxHealth;
            NetIsAlive = ph.IsAlive;
            _displayHP = NetHP;
            _displayMaxHP = NetMaxHP;
        }
    }

    // ───────────────────── PUBLIC API ─────────────────────

    /// <summary>
    /// Lấy HP hiển thị (đã lerp) của remote player.
    /// Dùng cho HP bar UI.
    /// </summary>
    public float DisplayHP => _displayHP;
    public float DisplayMaxHP => _displayMaxHP;

    /// <summary>
    /// Lấy HP thực từ network (chưa lerp).
    /// </summary>
    public float NetworkHP => NetHP;
    public float NetworkMaxHP => NetMaxHP;
    public bool NetworkIsAlive => NetIsAlive;
}
