using Fusion;
using UnityEngine;

/// <summary>
/// Gắn trên Player (cùng với NetworkObject).
/// Chuyên đồng bộ việc sinh ra các hiệu ứng (VFX), vòng sát thương (Hitbox) của vũ khí và kĩ năng
/// cho các mục tiêu là Proxy (người chơi remote từ xa).
/// </summary>
public class NetworkCombatSync : NetworkBehaviour
{
    private WeaponController _weaponController;
    private EquipmentSystem _equipmentSystem;
    private WeaponHitRunner _hitHandler;

    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();
        _equipmentSystem = GetComponent<EquipmentSystem>();
        _hitHandler = GetComponent<WeaponHitRunner>();
    }

    /// <summary>
    /// Khi Local Player bắt đầu tung 1 đòn chém thường (Basic Attack), họ gọi hàm này.
    /// Máy của họ tự chạy xử lý cục bộ rồi, nên hàm này chỉ báo cho các máy khác (Proxy) biết.
    /// </summary>
    public void RequestSyncBasicAttack(int hitIndex)
    {
        if (HasInputAuthority)
        {
            RPC_SyncBasicAttack(hitIndex);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.Proxies)] // Chỉ gửi cho các máy remote, vì máy local đã tự chạy rồi
    private void RPC_SyncBasicAttack(int hitIndex)
    {
        // Trên máy Remote (Proxy), Script Character.cs đã bị tắt, nên AttackState không chạy.
        // Kích hoạt WeaponHitRunner từ đây để sinh ra tia chém VFX và Hitbox!
        
        if (_hitHandler == null)
            _hitHandler = GetComponent<WeaponHitRunner>();

        if (_hitHandler == null || _weaponController == null || _weaponController.GetCurrentWeapon() == null) 
            return;

        // Bỏ qua nếu ko phải Remote Proxy (an toàn x2)
        if (HasInputAuthority) return;

        // Setup lại Handler
        _hitHandler.Bind(_weaponController.GetCurrentWeapon(), _equipmentSystem, transform, null, transform);
        
        // Spawn Hitbox/VFX theo nhịp (hitIndex)
        _hitHandler.StartHit(hitIndex);
    }

    /// <summary>
    /// Gửi RPC kích hoạt kỹ năng đặc biệt cục bộ (VFX Rìu, Triệu Hồi Mưa Sao Băng, Lốc Xoáy)
    /// </summary>
    public void RequestSyncSkillTrigger(string skillName)
    {
        if (HasInputAuthority)
        {
            RPC_SyncSkillTrigger(skillName);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.Proxies)]
    private void RPC_SyncSkillTrigger(string skillName)
    {
        if (HasInputAuthority) return;

        // Tùy theo tên Skill mà gọi các Timeline hoặc VFX tương ứng trên Proxy
        // Trong tương lai, bạn có thể gọi thẳng ultimateDirector.Play() tại đây
        Debug.Log($"[NetworkCombatSync] Áp dụng kĩ năng {skillName} cho Proxy {gameObject.name}");
        
        // Cụ thể sẽ móc nối với AxeSkill/SwordSkill tuỳ theo loại vũ khí đang cầm:
        var weaponType = _weaponController.GetCurrentWeapon().weaponType;

        if (weaponType == WeaponType.Axe)
        {
            var axeSkill = GetComponentInChildren<AxeSkill>(true);
            if (axeSkill != null) axeSkill.ExecuteSkillNetworkedProxy();
        }
        else if (weaponType == WeaponType.Sword)
        {
            var swordSkill = GetComponentInChildren<SwordSkills>(true);
            if (swordSkill != null) swordSkill.ExecuteSkillNetworkedProxy();
        }
        else if (weaponType == WeaponType.Mage)
        {
            var mageSkill = GetComponentInChildren<MageSkills>(true);
            if (mageSkill != null) mageSkill.ExecuteSkillNetworkedProxy();
        }
    }
}
