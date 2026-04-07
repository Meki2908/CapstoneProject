using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gắn trên player prefab (root, cùng NetworkObject).
/// Sync vital stats (HP, max HP, alive) cho tất cả clients.
///
/// Owner ghi → tất cả đọc.
/// Dùng để hiển thị HP bar trên đầu remote player.
/// <see cref="PlayerHealth"/> cập nhật giá trị mỗi khi HP thay đổi.
/// </summary>
[DefaultExecutionOrder(210)]
public class NetworkPlayerStats : NetworkBehaviour
{
    // ── NetworkVariables ──
    public readonly NetworkVariable<float> NetHP = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<float> NetMaxHP = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public readonly NetworkVariable<bool> NetIsAlive = new(true,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>Event cho UI (HP bar trên đầu remote player).</summary>
    public event System.Action<float, float> OnRemoteHealthChanged; // (currentHP, maxHP)

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Remote: listen for changes
            NetHP.OnValueChanged += OnHPChanged;
            NetMaxHP.OnValueChanged += OnMaxHPChanged;
        }
        else
        {
            // Owner: init values từ PlayerHealth
            SyncFromPlayerHealth();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
        {
            NetHP.OnValueChanged -= OnHPChanged;
            NetMaxHP.OnValueChanged -= OnMaxHPChanged;
        }
    }

    /// <summary>
    /// Owner gọi để cập nhật NetworkVariables từ PlayerHealth.
    /// Gọi mỗi khi HP thay đổi.
    /// </summary>
    public void UpdateHP(float currentHP, float maxHP, bool isAlive)
    {
        if (!IsOwner || !IsSpawned) return;

        NetHP.Value = currentHP;
        NetMaxHP.Value = maxHP;
        NetIsAlive.Value = isAlive;
    }

    private void SyncFromPlayerHealth()
    {
        var ph = GetComponentInChildren<PlayerHealth>(true);
        if (ph != null)
        {
            NetHP.Value = ph.CurrentHealth;
            NetMaxHP.Value = ph.MaxHealth;
            NetIsAlive.Value = ph.IsAlive;
        }
    }

    // ── Callbacks cho remote ──

    void OnHPChanged(float prev, float current)
    {
        OnRemoteHealthChanged?.Invoke(current, NetMaxHP.Value);
    }

    void OnMaxHPChanged(float prev, float current)
    {
        OnRemoteHealthChanged?.Invoke(NetHP.Value, current);
    }
}
