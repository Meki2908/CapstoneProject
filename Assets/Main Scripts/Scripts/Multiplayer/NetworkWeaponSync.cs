using Fusion;
using UnityEngine;

/// <summary>
/// Gắn trên Player (cùng NetworkObject).
/// Đồng bộ loại vũ khí (WeaponType) mà người chơi đang cầm lên mạng.
/// Proxy nhận được sẽ tự động tải Prefab vũ khí tương ứng và đặt lên tay.
/// </summary>
public class NetworkWeaponSync : NetworkBehaviour
{
    // Liên kết với biến cục bộ để dễ theo dõi
    [Networked, OnChangedRender(nameof(OnWeaponTypeChanged))]
    public int NetworkedWeaponType { get; set; }

    private WeaponController _weaponController;

    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // Mình là chủ, đọc vũ khí mình đang cất vào file hoặc mặc định
            if (_weaponController != null && _weaponController.GetCurrentWeapon() != null)
            {
                NetworkedWeaponType = (int)_weaponController.GetCurrentWeapon().weaponType;
            }
        }
        else
        {
            // Mình là proxy, cường ép đồng bộ vũ khí theo NetworkedWeaponType
            ForceEquipByNetworkType();
        }
    }

    /// <summary>
    /// Chủ máy gọi hàm này khi họ bấm đổi vũ khí hoặc nhặt vũ khí mới.
    /// </summary>
    public void CmdUpdateWeaponType(WeaponType newType)
    {
        if (HasInputAuthority)
        {
            RPC_UpdateWeaponType((int)newType);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateWeaponType(int weaponType)
    {
        // Yêu cầu Server ghi đè biến mạng
        NetworkedWeaponType = weaponType;
    }

    private void OnWeaponTypeChanged()
    {
        // Chạy trên tất cả Client khi biến NetworkedWeaponType thay đổi
        if (!HasInputAuthority)
        {
            ForceEquipByNetworkType();
        }
    }

    private void ForceEquipByNetworkType()
    {
        if (_weaponController == null) return;
        
        var weaponType = (WeaponType)NetworkedWeaponType;
        if (weaponType == WeaponType.None) return;

        // Trích xuất ScriptableObject của vũ khí thông qua File cấu hình có sẵn
        var weaponSO = WeaponSelectionPersistence.ResolveWeaponSO(weaponType);
        if (weaponSO != null)
        {
            // Ép Proxy thay đổi vũ khí ngay lập tức
            _weaponController.EquipWeapon(weaponSO);
        }
    }
}
