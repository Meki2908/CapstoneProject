using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private WeaponSO currentWeapon;
    private DamageDealer currentDamageDealer;

    [Header("Auto-assignment")]
    [SerializeField] private WeaponController weaponController; // Reference to WeaponController

    public void BindHeldDamageDealer(DamageDealer dealer)
    {
        currentDamageDealer = dealer;
    }

    public void UnbindHeld()
    {
        currentDamageDealer = null;
    }

    public WeaponSO GetCurrentWeapon() => currentWeapon;
    public void SetCurrentWeapon(WeaponSO so) => currentWeapon = so;

    // Hooks cho Attack/Skill
    public void StartDealDamage() => currentDamageDealer?.StartDealDamage();
    public void EndDealDamage() => currentDamageDealer?.EndDealDamage();

    private void Start()
    {
        if (weaponController == null)
            weaponController = GetComponent<WeaponController>();

        // Đồng bộ ban đầu
        SyncWithWeaponController();

        // ĐĂNG KÝ nhận sự kiện đổi vũ khí (nếu có WeaponController)
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChangedHandler;
            weaponController.OnWeaponChanged += OnWeaponChangedHandler;
        }
    }

    private void OnDestroy()
    {
        if (weaponController != null)
        {
            weaponController.OnWeaponChanged -= OnWeaponChangedHandler;
        }
    }

    private void OnWeaponChangedHandler(WeaponSO so)
    {
        currentWeapon = so; // giữ đồng bộ để Skills có thể đọc từ EquipmentSystem
        // Có thể log nhẹ nếu cần:
        // Debug.Log($"[EquipmentSystem] Weapon changed to: {currentWeapon?.weaponName ?? "None"}");
    }

    private void SyncWithWeaponController()
    {
        if (weaponController == null) return;
        var controllerWeapon = weaponController.GetCurrentWeapon();
        if (controllerWeapon != currentWeapon)
        {
            currentWeapon = controllerWeapon;
        }
    }

    // Cho phép WeaponController gọi trực tiếp khi equip
    public void SyncWeapon(WeaponSO weapon)
    {
        currentWeapon = weapon;
    }
}